#define FILTER_TEST

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.IO;
using System.IO.Pipes;
using EasyHook;
using UOMachine;
using UOMachine.IPC;
using System.Diagnostics;
using System.Security;

namespace ClientHook
{
    public struct Packet
    {
        public int length;
        public byte[] data;
    }

    public class GumpResponseFilter : IEquatable<GumpResponseFilter>
    {
        private uint m_Serial;
        private uint m_GumpID;

        public uint Serial
        {
            get { return m_Serial; }
        }

        public uint GumpID
        {
            get { return m_GumpID; }
        }

        public GumpResponseFilter(uint serial, uint gumpid)
        {
            m_Serial = serial;
            m_GumpID = gumpid;
        }

        public bool Equals( GumpResponseFilter other )
        {
            if (( this.Serial == other.Serial ) && ( this.GumpID == other.GumpID ))
                return true;
            return false;
        }
    }

    public sealed class Main : EasyHook.IEntryPoint
    {
        private static Queue<Packet> mySendClientQueue, mySendServerQueue;
        private static object mySendClientLock, mySendServerLock;
        private static int myPID, myThreadID, myDateStamp;
        private static dSendRecv myRecvDelegate, mySendDelegate;
        private static bool[] myRecvFilter, mySendFilter;
        private static ClientInstance myClientInstance;
        private static IntPtr myServerSendBuffer, myClientSendBuffer;
        private static byte[] myServerBufferAddress, myClientBufferAddress;
        private static List<GumpResponseFilter> myGumpResponseFilter;

        private const int SOCKET_ERROR = -1;

        [UnmanagedFunctionPointer( CallingConvention.StdCall, SetLastError = true )]
        delegate int dSendRecv( IntPtr buf, int len );

        public static int SendHook( IntPtr buf, int len )
        {
            byte[] buffer = new byte[len];
            Marshal.Copy( buf, buffer, 0, len );
#if FILTER_TEST
            if (buf.ToInt32() != myServerSendBuffer.ToInt32())
            {
                myClientInstance.SendCommand(Command.OutgoingPacket, buffer);
                if (mySendFilter[buffer[0]])
                    return 1;
            }

            if (buffer[0] == 0xB1)
            {
                bool found = false;
                int serial = buffer[3] << 24 | buffer[4] << 16 | buffer[5] << 8 | buffer[6];
                int gumpid = buffer[7] << 24 | buffer[8] << 16 | buffer[9] << 8 | buffer[10];

                myClientInstance.SendCommand( Command.Message, String.Format( "Gump Response: 0x{0:x} 0x{1:x}", serial, (uint) serial ) );

                foreach (GumpResponseFilter gf in myGumpResponseFilter)
                {
                    if ((gf.Serial == (uint)serial) && (gf.GumpID == (uint)gumpid))
                        found = true;

                    if (found)
                        break;
                }

                if (found)
                    return 1;
            }
#else
            myClientInstance.SendCommand(Command.OutgoingPacket, buffer);
#endif
            return 0;
        }

        public static int ReceiveHook( IntPtr buf, int len )
        {
            byte[] buffer = new byte[len];
            Marshal.Copy( buf, buffer, 0, len );
#if FILTER_TEST
            if (buf.ToInt32() != myClientSendBuffer.ToInt32())
            {
                myClientInstance.SendCommand( Command.IncomingPacket, buffer );
                if (myRecvFilter[buffer[0]])
                    return 1;
            }
#else
            myClientInstance.SendCommand( Command.IncomingPacket, buffer );
#endif
            return 0;
        }

        private static unsafe int GetDateStamp()
        {
            byte* address = (byte*)0x40003C;
            int offset = address[1] << 8 | address[0];
            address = (byte*)0x400000 + offset + 8;
            return address[3] << 24 | address[2] << 16 | address[1] << 8 | address[0];
        }

        public Main( RemoteHooking.IContext InContext, string serverName )
        {
            mySendClientQueue = new Queue<Packet>();
            mySendClientLock = new object();
            mySendServerQueue = new Queue<Packet>();
            mySendServerLock = new object();
            myRecvFilter = new bool[256];
            mySendFilter = new bool[256];
            myRecvDelegate = new dSendRecv( ReceiveHook );
            mySendDelegate = new dSendRecv( SendHook );
            myPID = RemoteHooking.GetCurrentProcessId();
            myThreadID = RemoteHooking.GetCurrentThreadId();
            myDateStamp = GetDateStamp();
            myServerSendBuffer = Marshal.AllocHGlobal( 65536 );
            myClientSendBuffer = Marshal.AllocHGlobal( 65536 );
            myServerBufferAddress = BitConverter.GetBytes( myServerSendBuffer.ToInt32() );
            myClientBufferAddress = BitConverter.GetBytes( myClientSendBuffer.ToInt32() );

            myGumpResponseFilter = new List<GumpResponseFilter>();

            myClientInstance = new ClientInstance( serverName, true );
            myClientInstance.SendCommand( Command.ClientID, myPID );
            myClientInstance.SendPacketEvent += new dSendPacket( myClientInstance_sendPacketEvent );
            myClientInstance.PingEvent += new dPing( myClientInstance_pingEvent );
            myClientInstance.AddRecvFilterEvent += new dAddRecvFilter( myClientInstance_addRecvFilterEvent );
            myClientInstance.AddSendFilterEvent += new dAddSendFilter( myClientInstance_addSendFilterEvent );
            myClientInstance.RemoveRecvFilterEvent += new dRemoveRecvFilter( myClientInstance_removeRecvFilterEvent );
            myClientInstance.RemoveSendFilterEvent += new dRemoveSendFilter( myClientInstance_removeSendFilterEvent );
            myClientInstance.ClearRecvFilterEvent += new dClearRecvFilter( myClientInstance_clearRecvFilterEvent );
            myClientInstance.ClearSendFilterEvent += new dClearSendFilter( myClientInstance_clearSendFilterEvent );
            myClientInstance.AddGumpResponseFilterEvent += new dAddGumpResponseFilter( myClientInstance_addGumpResponseFilterEvent );

            myClientInstance.SendCommand( Command.Message, "ClientHook Main()" );
        }

        ~Main()
        {
            if (myServerSendBuffer != IntPtr.Zero)
                Marshal.FreeHGlobal( myServerSendBuffer );
            if (myClientSendBuffer != IntPtr.Zero)
                Marshal.FreeHGlobal( myClientSendBuffer );
        }

        public void Run( RemoteHooking.IContext InContext, string serverName )
        {
            try
            {
                myClientInstance.SendCommand( Command.ClientVersion, myDateStamp );
                IntPtr functionPtr = Marshal.GetFunctionPointerForDelegate( myRecvDelegate );
                myClientInstance.SendCommand( Command.FunctionPointer, functionPtr.ToInt32(), 0 );
                functionPtr = Marshal.GetFunctionPointerForDelegate( mySendDelegate );
                myClientInstance.SendCommand( Command.FunctionPointer, functionPtr.ToInt32(), 1 );
                MessageHook.Initialize( myClientInstance );
            }
            catch (Exception X)
            {
                string message = "<Exception : " + X.Message + "> <Stack trace: " + X.StackTrace + ">";
                myClientInstance.SendCommand( Command.Exception, message );
                return;
            }

            while (true)
            {
                Thread.Sleep( 1000 );
            }
        }

        private static void myClientInstance_addSendFilterEvent( byte packetID )
        {
            mySendFilter[packetID] = true;
        }

        private static void myClientInstance_addRecvFilterEvent( byte packetID )
        {
            myRecvFilter[packetID] = true;
        }

        private static void myClientInstance_removeSendFilterEvent( byte packetID )
        {
            mySendFilter[packetID] = false;
        }

        private static void myClientInstance_removeRecvFilterEvent( byte packetID )
        {
            myRecvFilter[packetID] = false;
        }

        private static void myClientInstance_clearSendFilterEvent()
        {
            mySendFilter.Initialize();
        }

        private static void myClientInstance_clearRecvFilterEvent()
        {
            myRecvFilter.Initialize();
        }

        private static void myClientInstance_pingEvent( int clientID )
        {
            myClientInstance.SendCommand( Command.PingResponse );
        }

        private static void myClientInstance_addGumpResponseFilterEvent( uint serial, uint gumpid )
        {
            if (myGumpResponseFilter == null)
                myGumpResponseFilter = new List<GumpResponseFilter>();

            GumpResponseFilter grf = new GumpResponseFilter( serial, gumpid );

            if (!myGumpResponseFilter.Contains( grf ))
                myGumpResponseFilter.Add( grf );
        }

        private static unsafe void myClientInstance_sendPacketEvent( int caveAddress, PacketType packetType, byte[] data )
        {
            byte* cave = (byte*)caveAddress;
            switch (packetType)
            {
                case PacketType.Client:
                    lock (mySendClientLock)
                    {
                        Packet p;
                        p.length = data.Length;
                        p.data = new byte[data.Length];
                        Buffer.BlockCopy( data, 0, p.data, 0, data.Length );
                        mySendClientQueue.Enqueue( p );

                        try
                        {
                            while (mySendClientQueue.Count > 0)
                            {
                                if ((byte)cave[8] == (byte)0)
                                {
                                    p = mySendClientQueue.Dequeue();
                                    Marshal.Copy( p.data, 0, myClientSendBuffer, p.length );
                                    cave[0] = myClientBufferAddress[0];
                                    cave[1] = myClientBufferAddress[1];
                                    cave[2] = myClientBufferAddress[2];
                                    cave[3] = myClientBufferAddress[3];
                                    cave[4] = (byte)(p.length & 0xFF);
                                    cave[5] = (byte)((p.length >> 8) & 0xFF);
                                    cave[6] = (byte)((p.length >> 16) & 0xFF);
                                    cave[7] = (byte)((p.length >> 24) & 0xFF);
                                    cave[8] = 0x01;
                                }
                                if (mySendClientQueue.Count == 0)
                                    break;
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                    break;
                case PacketType.Server:
                    //lock (mySendServerLock)
                    //{
                    //    Packet p;
                    //    p.length = data.Length;
                    //    p.data = new byte[data.Length];
                    //    Buffer.BlockCopy( data, 0, p.data, 0, data.Length );
                    //    mySendServerQueue.Enqueue( p );

                    //    try
                    //    {
                    //        while (mySendServerQueue.Count > 0)
                    //        {
                    //            if ((byte)cave[4] == (byte)0)
                    //            {
                    //                p = mySendServerQueue.Dequeue();
                    //                Marshal.Copy( p.data, 0, myServerSendBuffer, p.length );
                    //                cave[0] = myServerBufferAddress[0];
                    //                cave[1] = myServerBufferAddress[1];
                    //                cave[2] = myServerBufferAddress[2];
                    //                cave[3] = myServerBufferAddress[3];
                    //                cave[4] = 0x01;
                    //            }
                    //            if (mySendServerQueue.Count == 0)
                    //                break;
                    //        }
                    //    }
                    //    catch (Exception e)
                    //    {
                    //    }
                    //}
                    lock (mySendServerLock)
                    {
                        Marshal.Copy(data, 0, myServerSendBuffer, data.Length);
                        cave[0] = myServerBufferAddress[0];
                        cave[1] = myServerBufferAddress[1];
                        cave[2] = myServerBufferAddress[2];
                        cave[3] = myServerBufferAddress[3];
                        cave[4] = 0x01;
                        while (cave[4] != 0x00) ;
                    }
                    break;
            }
        }
    }
}
