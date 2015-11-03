using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UOMachine;
using ClientHook;
using UOMachine.Data;
using UOMachine.IPC;
using System.Threading;

namespace UnitTests
{
    [TestClass]
    public class PacketFilterTests
    {
        PacketFilter myRecvFilter = new PacketFilter();
        object m_FinishedLock = new object();
        object m_CountLock = new object();
        int count = 0;

        [TestCategory( "NoData" ), TestCategory( "NoData\\PacketFilter" ), TestMethod]
        public void PacketFilterTest()
        {

            ServerInstance si = new ServerInstance( UOM.ServerName, false, 0, 32 );
            ClientInstance ci = new ClientInstance( UOM.ServerName, false );
            ci.AddRecvFilterEvent += Ci_AddRecvFilterEvent;
            ci.SendPacketEvent += Ci_SendPacketEvent;

            si.SendCommand( Command.AddRecvFilterConditional, new PacketFilterInfo( 0xFF, new PacketFilterCondition[] { new PacketFilterCondition( 2, new byte[] { 0x12, 0x34, 0x56, 0x78 }, 4 ) } ).Serialize() );

            byte[] packet = new byte[]
            {
                0xFF,
                0x01,
                0x12,
                0x34,
                0x56,
                0x78,
            };

            byte[] packet2 = new byte[]
            {
                0xFF,
                0x00,
                0x78,
                0x56,
                0x34,
                0x12,
            };

            si.SendCommand( Command.SendPacket, 0, (byte) PacketType.Server, packet );
            si.SendCommand( Command.SendPacket, 0, (byte) PacketType.Server, packet2 );

            lock (m_FinishedLock)
            {
                Monitor.Wait( m_FinishedLock );
            }

            ci.Dispose();
            si.Dispose();
        }

        private void Ci_SendPacketEvent( int caveAddress, PacketType packetType, byte[] data )
        {
            switch (data[1])
            {
                case 1:
                    Assert.IsTrue( myRecvFilter.MatchFilter( data ) );
                    break;
                case 0:
                    Assert.IsFalse( myRecvFilter.MatchFilter( data ) );
                    break;
            }
            
            if (++count == 2)
            {
                lock (m_FinishedLock)
                    Monitor.Pulse( m_FinishedLock );
            }

        }

        private void Ci_AddRecvFilterEvent( byte packetID, PacketFilterCondition[] conditions )
        {
            myRecvFilter.Add( packetID, conditions );
        }
    }
}
