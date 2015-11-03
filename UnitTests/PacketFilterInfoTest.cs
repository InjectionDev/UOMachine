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

namespace UnitTests
{
    [TestClass]
    public class PacketFilterInfoTest
    {
        PacketFilter myRecvFilter = new PacketFilter();

        [TestCategory( "NoData" ), TestMethod]
        public void Test()
        {
            
            ServerInstance si = new ServerInstance( UOM.ServerName, false, 0, 32 );

            ClientInstance ci = new ClientInstance( UOM.ServerName, false );
            ci.AddRecvFilterEvent += Ci_AddRecvFilterEvent;

            si.SendCommand( Command.AddRecvFilterConditional, new PacketFilterInfo(0xF3, new PacketFilterCondition[] { new PacketFilterCondition( 0, new byte[] { 0xF3 }, 1 ) } ).Serialize());

            while (true) ;

        }

        private void Ci_AddRecvFilterEvent( byte packetID, PacketFilterCondition[] conditions )
        {
            myRecvFilter.Add( packetID, conditions );
        }
    }
}
