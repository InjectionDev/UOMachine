using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UOMachine;
using ClientHook;
using UOMachine.Data;

namespace UnitTests
{
    [TestClass]
    public class GumpTest
    {
        public class TestGump : Gump
        {
            public TestGump() : base( 100, 100, -1, -1 )
            {
                Movable = true;
                Disposable = true;
                Resizable = true;
                Closable = true;
                AddPage( 0 );
                AddBackground( 0, 0, 100, 100, 0x13BE );
                AddAlphaRegion( 10, 10, 80, 80 );
            }
        }

        [TestCategory( "NoData" ), TestCategory( "NoData\\GumpWriter" ), TestMethod]
        public void GumpWriterTest()
        {
            //TODO: More extensive testing

            TestGump g = new TestGump();

            byte[] packet = g.Compile();

            string layout = "{ page 0 }{ resizepic 0 0 5054 100 100 }{ checkertrans 10 10 80 80 }";

            PacketWriter pw = new PacketWriter();
            pw.Write( (byte) 0xB0 );
            pw.Write( (short) (23+layout.Length) );
            pw.Write( (int) -1 );
            pw.Write( (int) -1 );
            pw.Write( (int) 100 );
            pw.Write( (int) 100 );
            pw.Write( (short) layout.Length );
            pw.WriteAsciiFixed( layout, layout.Length );
            pw.Write( (short) 0 );

            byte[] packet2 = pw.ToArray();

            Assert.AreEqual( packet.Length, packet2.Length );

            for (int i = 0; i < packet.Length;i++)
            {
                Assert.AreEqual( packet[i], packet2[i], String.Format("No match at position {0}", i ) );
            }

            List<GumpResponseFilter> grf = new List<GumpResponseFilter>();

            grf.Add( new GumpResponseFilter( 0xFFFFFFFF, 0xFFFFFFFF ) );

            bool contains = grf.Contains( new GumpResponseFilter( 0xFFFFFFFF, 0xFFFFFFFF ) );
            Assert.IsTrue( contains );

            grf.Clear();
            contains = grf.Contains( new GumpResponseFilter( 0xFFFFFFFF, 0xFFFFFFFF ) );
            Assert.IsFalse( contains );

        }
    }
}
