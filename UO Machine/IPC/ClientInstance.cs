/* Copyright (C) 2009 Matthew Geyer
 * 
 * This file is part of UO Machine.
 * 
 * UO Machine is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * UO Machine is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with UO Machine.  If not, see <http://www.gnu.org/licenses/>. */

using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Text;
using UOMachine.Utility;

namespace UOMachine.IPC
{
    public sealed class ClientInstance : IDisposable
    {
        public event dPing PingEvent;
        public event dPingResponse PingResponseEvent;
        public event dSendPacket SendPacketEvent;
        public event dAddSendFilter AddSendFilterEvent;
        public event dAddRecvFilter AddRecvFilterEvent;
        public event dRemoveSendFilter RemoveSendFilterEvent;
        public event dRemoveRecvFilter RemoveRecvFilterEvent;
        public event dClearSendFilter ClearSendFilterEvent;
        public event dClearRecvFilter ClearRecvFilterEvent;
        public event dInstallSendHook InstallSendHookEvent;
        public event dInstallRecvHook InstallRecvHookEvent;
        public event dUninstallSendHook UninstallSendHookEvent;
        public event dUninstallRecvHook UninstallRecvHookEvent;

        private const int myBuffSize = 131072;
        private MemoryStream myMemoryStream;
        private object myMemoryStreamLock;
        private Thread myThread;
        private NamedPipeClientStream myNamedPipeClientStream;
        private object myNamedPipeClientStreamLock;
        private byte[] myBuffer;
        private long myReadPos;

        private bool myThreadRunning;
        private bool ThreadRunning
        {
            get { return ThreadHelper.VolatileRead(ref myThreadRunning); }
            set { myThreadRunning = value; }
        }

        /// <summary>
        /// Create an IPC client and attempt to connect to server.
        /// </summary>
        /// <param name="serverName">
        /// Name of server to connect to.
        /// </param>
        /// <param name="writeThrough">
        /// If true writes will bypass system cache and go straight to the pipe.
        /// </param>
        public ClientInstance(string serverName, bool writeThrough)
        {
            myBuffer = new byte[myBuffSize];
            myMemoryStream = new MemoryStream(myBuffSize);
            myMemoryStreamLock = new object();
            myThreadRunning = true;
            myThread = new Thread(new ThreadStart(ProcessClientStream));
            myThread.Start();
            myNamedPipeClientStream = new NamedPipeClientStream(
                ".",
                serverName,
                PipeDirection.InOut,
                writeThrough ? PipeOptions.Asynchronous | PipeOptions.WriteThrough : PipeOptions.Asynchronous);
            myNamedPipeClientStreamLock = new object();
            try { myNamedPipeClientStream.Connect(); }
            catch { return; }
            myNamedPipeClientStream.BeginRead(myBuffer, 0, myBuffer.Length, new AsyncCallback(OnClientReceive), null);
        }

        /// <summary>
        /// Process incoming data from the IPC connection.
        /// </summary>
        private void ProcessClientStream()
        {
            byte[] message;

            while (ThreadRunning)
            {
                lock (myMemoryStreamLock)
                {
                    try
                    {
                        while ((message = Data.GetMessage(myMemoryStream, ref myReadPos)) != null)
                        {
                            ProcessMessage(message);
                        }
                    }
                    finally { Monitor.Wait(myMemoryStreamLock); }
                }
            }
        }

        /// <summary>
        /// Process IPC message and fire corresponding event.
        /// </summary>
        private void ProcessMessage(byte[] message)
        {
            Command command = (Command)message[0];
            switch (command)
            {
                case Command.Ping:
                    dPing ping = PingEvent;
                    if (ping != null)
                        ThreadPool.QueueUserWorkItem( delegate { ping( 0 ); } );
                    return;
                case Command.PingResponse:
                    dPingResponse pingResponse = PingResponseEvent;
                    if (pingResponse != null)
                        ThreadPool.QueueUserWorkItem( delegate { pingResponse( 0 ); } );
                    return;
                case Command.SendPacket:
                    dSendPacket sendPacket = SendPacketEvent;
                    if (sendPacket != null)
                    {
                        int caveAddress = BitConverter.ToInt32( message, 3 );
                        PacketType packetType = (PacketType) message[7];
                        byte[] packet = new byte[message.Length - 8];
                        Buffer.BlockCopy( message, 8, packet, 0, packet.Length );
                        ThreadPool.QueueUserWorkItem( delegate { sendPacket( caveAddress, packetType, packet ); } );
                    }
                    return;
                case Command.AddSendFilter:
                    {
                        dAddSendFilter addSendFilter = AddSendFilterEvent;
                        if (addSendFilter != null)
                            ThreadPool.QueueUserWorkItem( delegate { addSendFilter( message[1], null ); } );
                        return;
                    }
                case Command.AddSendFilterConditional:
                    {
                        dAddSendFilter addSendFilter = AddSendFilterEvent;
                        if (addSendFilter != null)
                        {
                            PacketFilterInfo pfi = PacketFilterInfo.Deserialize( message );
                            ThreadPool.QueueUserWorkItem( delegate { addSendFilter( (byte)pfi.PacketID, pfi.Conditions ); } );
                        }
                        return;
                    }
                case Command.AddRecvFilter:
                    {
                        dAddRecvFilter addRecvFilter = AddRecvFilterEvent;
                        if (addRecvFilter != null)
                            ThreadPool.QueueUserWorkItem( delegate { addRecvFilter( message[1], null ); } );
                        return;
                    }
                case Command.AddRecvFilterConditional:
                    {
                        dAddRecvFilter addRecvFilter = AddRecvFilterEvent;
                        if (addRecvFilter != null)
                        {
                            PacketFilterInfo pfi = PacketFilterInfo.Deserialize( message );
                            ThreadPool.QueueUserWorkItem( delegate { addRecvFilter( (byte) pfi.PacketID, pfi.Conditions ); } );
                        }
                        return;
                    }
                case Command.RemoveRecvFilter:
                    dRemoveRecvFilter removeRecvFilter = RemoveRecvFilterEvent;
                    if (removeRecvFilter != null)
                        ThreadPool.QueueUserWorkItem( delegate { removeRecvFilter( message[1] ); } );
                    return;
                case Command.RemoveSendFilter:
                    dRemoveSendFilter removeSendFilter = RemoveSendFilterEvent;
                    if (removeSendFilter != null)
                        ThreadPool.QueueUserWorkItem( delegate { removeSendFilter( message[1] ); } );
                    return;
                case Command.ClearSendFilter:
                    dClearSendFilter clearSendFilter = ClearSendFilterEvent;
                    if (clearSendFilter != null)
                        ThreadPool.QueueUserWorkItem( delegate { clearSendFilter(); } );
                    return;
                case Command.ClearRecvFilter:
                    dClearRecvFilter clearRecvFilter = ClearRecvFilterEvent;
                    if (clearRecvFilter != null)
                        ThreadPool.QueueUserWorkItem( delegate { clearRecvFilter(); } );
                    return;
                case Command.InstallSendHook:
                    dInstallSendHook installSendHook = InstallSendHookEvent;
                    if (installSendHook != null)
                        ThreadPool.QueueUserWorkItem( delegate { installSendHook(); } );
                    return;
                case Command.InstallRecvHook:
                    dInstallRecvHook installRecvHook = InstallRecvHookEvent;
                    if (installRecvHook != null)
                        ThreadPool.QueueUserWorkItem( delegate { installRecvHook(); } );
                    return;
                case Command.UninstallSendHook:
                    dUninstallSendHook uninstallSendHook = UninstallSendHookEvent;
                    if (uninstallSendHook != null)
                        ThreadPool.QueueUserWorkItem( delegate { uninstallSendHook(); } );
                    return;
                case Command.UninstallRecvHook:
                    dUninstallRecvHook uninstallRecvHook = UninstallRecvHookEvent;
                    if (uninstallRecvHook != null)
                        ThreadPool.QueueUserWorkItem( delegate { uninstallRecvHook(); } );
                    return;
            }
        }

        private void OnClientReceive(IAsyncResult asyncResult)
        {
            try
            {
                int received = myNamedPipeClientStream.EndRead( asyncResult );
                if (received > 0)
                {
                    lock (myMemoryStreamLock)
                    {
                        myMemoryStream.Write( myBuffer, 0, received );
                        Monitor.Pulse( myMemoryStreamLock );
                    }
                }
                myNamedPipeClientStream.BeginRead( myBuffer, 0, myBuffer.Length, new AsyncCallback( OnClientReceive ), null );
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
        }

        /// <summary>
        /// Send single-byte IPC message to connected IPC server.
        /// </summary>
        /// <param name="command">
        /// IPC.Command to send.
        /// </param>
        public void SendCommand(Command command)
        {
            lock (myNamedPipeClientStreamLock)
            {
                if (myNamedPipeClientStream.IsConnected)
                {
                    try { myNamedPipeClientStream.WriteByte((byte)command); }
                    catch (IOException) { }
                }
            }
        }

        /// <summary>
        /// Send 2 byte IPC message to connected IPC server.
        /// </summary>
        /// <param name="command">
        /// IPC.Command to send.</param>
        /// <param name="data">Data to send.</param>
        public void SendCommand(Command command, byte data)
        {
            lock (myNamedPipeClientStreamLock)
            {
                if (myNamedPipeClientStream.IsConnected)
                {
                    byte[] message = new byte[2];
                    message[0] = (byte)command;
                    message[1] = data;
                    try { myNamedPipeClientStream.Write(message, 0, 2); }
                    catch (IOException) { }
                }
            }
        }

        /// <summary>
        /// Send variable length IPC message to connected IPC server.
        /// </summary>
        /// <param name="command">
        /// IPC.Command to send.
        /// </param>
        /// <param name="data">Data to send.</param>
        public void SendCommand(Command command, byte[] data)
        {
            lock (myNamedPipeClientStreamLock)
            {
                if (myNamedPipeClientStream.IsConnected)
                {
                    byte[] message = new byte[data.Length + 3];
                    message[0] = (byte)command;
                    message[1] = (byte)message.Length;
                    message[2] = (byte)(message.Length >> 8);
                    Buffer.BlockCopy(data, 0, message, 3, data.Length);
                    try { myNamedPipeClientStream.Write(message, 0, message.Length); }
                    catch (IOException) { }
                }
            }
        }

        /// <summary>
        /// Send variable length IPC message to connected IPC server.
        /// </summary>
        /// <param name="command">
        /// IPC.Command to send.
        /// </param>
        /// <param name="messageString">Command argument.</param>
        public void SendCommand(Command command, string messageString)
        {
            lock (myNamedPipeClientStreamLock)
            {
                if (myNamedPipeClientStream.IsConnected)
                {
                    byte[] stringBytes = UnicodeEncoding.Unicode.GetBytes(messageString);
                    byte[] message = new byte[stringBytes.Length + 3];
                    message[0] = (byte)command;
                    message[1] = (byte)message.Length;
                    message[2] = (byte)(message.Length >> 8);
                    Buffer.BlockCopy(stringBytes, 0, message, 3, stringBytes.Length);
                    try { myNamedPipeClientStream.Write(message, 0, message.Length); }
                    catch (IOException) { }
                }
            }
        }

        /// <summary>
        /// Send 5 byte IPC message to connected IPC server.
        /// </summary>
        /// <param name="command">
        /// IPC.Command to send.
        /// </param>
        /// <param name="data">Data to send.</param>
        public void SendCommand(Command command, int data)
        {
            lock (myNamedPipeClientStreamLock)
            {
                if (myNamedPipeClientStream.IsConnected)
                {
                    byte[] message = new byte[5];
                    message[0] = (byte)command;
                    message[1] = (byte)data;
                    message[2] = (byte)(data >> 8);
                    message[3] = (byte)(data >> 16);
                    message[4] = (byte)(data >> 24);
                    try { myNamedPipeClientStream.Write(message, 0, 5); }
                    catch (IOException) { }
                }
            }
        }

        /// <summary>
        /// Send 6 byte IPC message to connected IPC server.
        /// </summary>
        /// <param name="command">
        /// IPC.Command to send.
        /// </param>
        /// <param name="data">Data to send.</param>
        /// <param name="data2">Data to send.</param>
        public void SendCommand(Command command, int data, byte data2)
        {
            lock (myNamedPipeClientStreamLock)
            {
                if (myNamedPipeClientStream.IsConnected)
                {
                    byte[] message = new byte[6];
                    message[0] = (byte)command;
                    message[1] = (byte)data;
                    message[2] = (byte)(data >> 8);
                    message[3] = (byte)(data >> 16);
                    message[4] = (byte)(data >> 24);
                    message[5] = data2;
                    try { myNamedPipeClientStream.Write(message, 0, 6); }
                    catch (IOException) { }
                }
            }
        }

        /// <summary>
        /// Send 9 byte IPC message to connected IPC server.
        /// </summary>
        /// <param name="command">
        /// IPC.Command to send.
        /// </param>
        /// <param name="data1">Data to send.</param>
        /// <param name="data2">Data to send.</param>
        public void SendCommand(Command command, int data1, int data2)
        {
            lock (myNamedPipeClientStreamLock)
            {
                if (myNamedPipeClientStream.IsConnected)
                {
                    byte[] message = new byte[9];
                    message[0] = (byte)command;
                    message[1] = (byte)data1;
                    message[2] = (byte)(data1 >> 8);
                    message[3] = (byte)(data1 >> 16);
                    message[4] = (byte)(data1 >> 24);
                    message[5] = (byte)data2;
                    message[6] = (byte)(data2 >> 8);
                    message[7] = (byte)(data2 >> 16);
                    message[8] = (byte)(data2 >> 24);
                    try { myNamedPipeClientStream.Write(message, 0, 9); }
                    catch (IOException) { }
                }
            }
        }

        /// <summary>
        /// Send 10 byte IPC message to connected IPC server.
        /// </summary>
        /// <param name="command">
        /// IPC.Command to send.
        /// </param>
        /// <param name="data1">
        /// Command argument.
        /// </param>
        /// <param name="data2">Command argument.</param>
        /// <param name="data3">Command argument.</param>
        public void SendCommand(Command command, int data1, int data2, byte data3)
        {
            lock (myNamedPipeClientStreamLock)
            {
                if (myNamedPipeClientStream.IsConnected)
                {
                    byte[] message = new byte[10];
                    message[0] = (byte)command;
                    message[1] = (byte)data1;
                    message[2] = (byte)(data1 >> 8);
                    message[3] = (byte)(data1 >> 16);
                    message[4] = (byte)(data1 >> 24);
                    message[5] = (byte)data2;
                    message[6] = (byte)(data2 >> 8);
                    message[7] = (byte)(data2 >> 16);
                    message[8] = (byte)(data2 >> 24);
                    message[9] = data3;
                    try { myNamedPipeClientStream.Write(message, 0, 10); }
                    catch (IOException) { }
                }
            }
        }

        public void Dispose()
        {
            myThreadRunning = false;
            lock (myMemoryStreamLock) { Monitor.Pulse( myMemoryStreamLock ); }

            if (myThread.IsAlive)
            {
                myThread.Abort();
                myThread.Join();
            }

            if (myNamedPipeClientStream != null)
            {
                myNamedPipeClientStream.Dispose();
            }

            if (myMemoryStream != null)
                myMemoryStream.Dispose();
        }
    }
}