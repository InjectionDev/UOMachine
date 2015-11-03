/* Copyright (C) 2015 John Scott
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

using System.Text;
using System.IO;

namespace UOMachine.Data
{
    public class PacketReader
    {
        private byte[] m_Data;
        private int m_Size;
        private int m_Index;

        public PacketReader(byte[] data, int size, bool fixedSize)
        {
            m_Data = data;
            m_Size = size;
            m_Index = fixedSize ? 1 : 3;
        }

        public int Index
        {
            get { return m_Index; }
        }

        public byte[] Data
        {
            get { return m_Data; }
        }

        public int Size
        {
            get { return m_Size; }
        }

        public int Seek(int offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin: 
                    m_Index = offset; 
                    break;
                case SeekOrigin.Current: 
                    m_Index += offset; 
                    break;
                case SeekOrigin.End: 
                    m_Index = m_Size - offset; 
                    break;
            }

            return m_Index;
        }

        public int ReadInt32()
        {
            if ((m_Index + 4) > m_Size)
                return 0;

            return (m_Data[m_Index++] << 24)
                 | (m_Data[m_Index++] << 16)
                 | (m_Data[m_Index++] << 8)
                 | m_Data[m_Index++];
        }

        public short ReadInt16()
        {
            if ((m_Index + 2) > m_Size)
                return 0;

            return (short)((m_Data[m_Index++] << 8) | m_Data[m_Index++]);
        }

        public byte ReadByte()
        {
            if ((m_Index + 1) > m_Size)
                return 0;

            return m_Data[m_Index++];
        }

        public byte[] ReadByteArray(int length)
        {
            byte[] bytes = new byte[length];
            for (int i = 0; i < length;i++)
            {
                bytes[i] = ReadByte();
            }
            return bytes;
        }

        public uint ReadUInt32()
        {
            if ((m_Index + 4) > m_Size)
                return 0;

            return (uint)((m_Data[m_Index++] << 24) | (m_Data[m_Index++] << 16) | (m_Data[m_Index++] << 8) | m_Data[m_Index++]);
        }

        public ushort ReadUInt16()
        {
            if ((m_Index + 2) > m_Size)
                return 0;

            return (ushort)((m_Data[m_Index++] << 8) | m_Data[m_Index++]);
        }

        public sbyte ReadSByte()
        {
            if ((m_Index + 1) > m_Size)
                return 0;

            return (sbyte)m_Data[m_Index++];
        }

        public bool ReadBoolean()
        {
            if ((m_Index + 1) > m_Size)
                return false;

            return (m_Data[m_Index++] != 0);
        }

        public string ReadUnicodeStringLE()
        {
            StringBuilder sb = new StringBuilder();

            int c;

            while ((m_Index + 1) < m_Size && (c = (m_Data[m_Index++] | (m_Data[m_Index++] << 8))) != 0)
                sb.Append((char)c);

            return sb.ToString();
        }

        public string ReadUnicodeStringLESafe(int fixedLength)
        {
            int bound = m_Index + (fixedLength << 1);
            int end = bound;

            if (bound > m_Size)
                bound = m_Size;

            StringBuilder sb = new StringBuilder();

            int c;

            while ((m_Index + 1) < bound && (c = (m_Data[m_Index++] | (m_Data[m_Index++] << 8))) != 0)
            {
                if (IsSafeChar(c))
                    sb.Append((char)c);
            }

            m_Index = end;

            return sb.ToString();
        }

        public string ReadUnicodeStringLESafe()
        {
            StringBuilder sb = new StringBuilder();

            int c;

            while ((m_Index + 1) < m_Size && (c = (m_Data[m_Index++] | (m_Data[m_Index++] << 8))) != 0)
            {
                if (IsSafeChar(c))
                    sb.Append((char)c);
            }

            return sb.ToString();
        }

        public string ReadUnicodeStringSafe()
        {
            StringBuilder sb = new StringBuilder();

            int c;

            while ((m_Index + 1) < m_Size && (c = ((m_Data[m_Index++] << 8) | m_Data[m_Index++])) != 0)
            {
                if (IsSafeChar(c))
                    sb.Append((char)c);
            }

            return sb.ToString();
        }

        public string ReadUnicodeString()
        {
            StringBuilder sb = new StringBuilder();

            int c;

            while ((m_Index + 1) < m_Size && (c = ((m_Data[m_Index++] << 8) | m_Data[m_Index++])) != 0)
                sb.Append((char)c);

            return sb.ToString();
        }

        public bool IsSafeChar(int c)
        {
            return (c >= 0x20 && c < 0xFFFE);
        }


        public string ReadString()
        {
            StringBuilder sb = new StringBuilder();

            int c;

            while (m_Index < m_Size && (c = m_Data[m_Index++]) != 0)
                sb.Append((char)c);

            return sb.ToString();
        }

        public string ReadStringSafe()
        {
            StringBuilder sb = new StringBuilder();

            int c;

            while (m_Index < m_Size && (c = m_Data[m_Index++]) != 0)
            {
                if (IsSafeChar(c))
                    sb.Append((char)c);
            }

            return sb.ToString();
        }

        public string ReadUnicodeStringSafe(int fixedLength)
        {
            int bound = m_Index + (fixedLength << 1);
            int end = bound;

            if (bound > m_Size)
                bound = m_Size;

            StringBuilder sb = new StringBuilder();

            int c;

            while ((m_Index + 1) < bound && (c = ((m_Data[m_Index++] << 8) | m_Data[m_Index++])) != 0)
            {
                if (IsSafeChar(c))
                    sb.Append((char)c);
            }

            m_Index = end;

            return sb.ToString();
        }

        public string ReadUnicodeString(int fixedLength)
        {
            int bound = m_Index + (fixedLength << 1);
            int end = bound;

            if (bound > m_Size)
                bound = m_Size;

            StringBuilder sb = new StringBuilder();

            int c;

            while ((m_Index + 1) < bound && (c = ((m_Data[m_Index++] << 8) | m_Data[m_Index++])) != 0)
                sb.Append((char)c);

            m_Index = end;

            return sb.ToString();
        }

        public string ReadStringSafe(int fixedLength)
        {
            int bound = m_Index + fixedLength;
            int end = bound;

            if (bound > m_Size)
                bound = m_Size;

            StringBuilder sb = new StringBuilder();

            int c;

            while (m_Index < bound && (c = m_Data[m_Index++]) != 0)
            {
                if (IsSafeChar(c))
                    sb.Append((char)c);
            }

            m_Index = end;

            return sb.ToString();
        }

        public string ReadString(int fixedLength)
        {
            int bound = m_Index + fixedLength;
            int end = bound;

            if (bound > m_Size)
                bound = m_Size;

            StringBuilder sb = new StringBuilder();

            int c;

            while (m_Index < bound && (c = m_Data[m_Index++]) != 0)
                sb.Append((char)c);

            m_Index = end;

            return sb.ToString();
        }
    }
}
