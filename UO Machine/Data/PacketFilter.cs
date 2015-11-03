using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UOMachine.Data;

namespace UOMachine
{
    public class PacketFilter
    {
        private List<PacketFilterInfo> m_Filters;

        public void Initialize()
        {
            m_Filters = new List<PacketFilterInfo>();
        }

        public void Add( byte packet, PacketFilterCondition[] contraints )
        {
            if (m_Filters == null)
                m_Filters = new List<PacketFilterInfo>();

            m_Filters.Add( new PacketFilterInfo( packet, contraints ) );
        }

        public void Remove( byte packet )
        {
            for (int i = 0; i < m_Filters.Count; i++)
            {
                if (m_Filters[i].PacketID == packet)
                    m_Filters.Remove( m_Filters[i] );
            }
        }

        public bool MatchFilter( byte[] packet )
        {
            if (m_Filters == null)
                return false;

            bool result = false;

            for (int i = 0; i < m_Filters.Count; i++)
            {
                if (packet[0] == m_Filters[i].PacketID)
                {
                    if (m_Filters[i].Conditions == null)
                    {
                        // No condition so just match packetid
                        result = true;
                    }
                    else
                    {
                        foreach (PacketFilterCondition fc in m_Filters[i].Conditions)
                        {
                            if (( fc.Position + fc.Length ) > packet.Length)
                            {
                                result = false;
                                continue;
                            }

                            byte[] tmp = new byte[fc.Length];
                            Buffer.BlockCopy( packet, fc.Position, tmp, 0, fc.Length );

                            if (!tmp.SequenceEqual( fc.Bytes ))
                            {
                                result = false;
                                break;
                            }
                            else
                                result = true;
                        }
                    }
                }

                if (result)
                    break;
            }
            return result;
        }
    }

    public class PacketFilterCondition
    {
        private int m_Position;
        private byte[] m_Bytes;
        private int m_Length;

        public int Position
        {
            get
            {
                return m_Position;
            }

            set
            {
                m_Position = value;
            }
        }

        public byte[] Bytes
        {
            get
            {
                return m_Bytes;
            }

            set
            {
                m_Bytes = value;
            }
        }

        public int Length
        {
            get
            {
                return m_Length;
            }

            set
            {
                m_Length = value;
            }
        }

        public PacketFilterCondition( int position, byte[] bytes, int length )
        {
            m_Position = position;
            m_Bytes = bytes;
            m_Length = length;
        }
    }

    public class PacketFilterInfo
    {
        private int m_PacketID;
        private PacketFilterCondition[] m_Conditions;

        public int PacketID
        {
            get
            {
                return m_PacketID;
            }

            set
            {
                m_PacketID = value;
            }
        }

        public PacketFilterCondition[] Conditions
        {
            get
            {
                return m_Conditions;
            }

            set
            {
                m_Conditions = value;
            }
        }

        public PacketFilterInfo( int packetid, PacketFilterCondition[] conditions )
        {
            m_PacketID = packetid;
            m_Conditions = conditions;
        }

        public byte[] Serialize()
        {
            PacketWriter pw = new PacketWriter();
            pw.Write( (byte) m_PacketID );
            if (Conditions == null)
                pw.Write( (short) 0 );
            else
            {
                pw.Write( (short) Conditions.Length );
                for (int i = 0; i < Conditions.Length; i++)
                {
                    pw.Write( (short) Conditions[i].Position );
                    pw.Write( (short) Conditions[i].Bytes.Length );
                    pw.Write( Conditions[i].Bytes, 0, Conditions[i].Bytes.Length );
                    pw.Write( (short) Conditions[i].Length );
                }
            }
            return pw.ToArray();
        }

        public static PacketFilterInfo Deserialize( byte[] bytes )
        {
            PacketReader reader = new PacketReader( bytes, bytes.Length, false );
            byte pid = reader.ReadByte();
            int numconditions = reader.ReadInt16();

            List<PacketFilterCondition> conditions = new List<PacketFilterCondition>();

            for (int i = 0; i < numconditions; i++)
            {
                int pos = reader.ReadInt16();
                int blen = reader.ReadInt16();
                byte[] bytes2 = new byte[blen];
                bytes2 = reader.ReadByteArray( blen );
                int len = reader.ReadInt16();
                conditions.Add( new PacketFilterCondition( pos, bytes2, len ) );
            }

            if (conditions.Count == 0)
                return new PacketFilterInfo( pid, null );

            return new PacketFilterInfo( pid, conditions.ToArray() );
        }
    }
}

