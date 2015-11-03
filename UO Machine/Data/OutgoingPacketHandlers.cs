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

using System;
using System.Collections.Generic;
using UOMachine.Events;

namespace UOMachine.Data
{
    internal static class OutgoingPacketHandlers
    {
        private static PacketHandler[] m_Handlers;
        private static PacketHandler[] m_ExtendedHandlers;

        internal static void Initialize()
        {
            m_Handlers = new PacketHandler[0x100];
            m_ExtendedHandlers = new PacketHandler[0x100];

            Register(0x02,  7, new OnPacketReceive(OnMoveRequested));
            Register(0x06,  5, new OnPacketReceive(OnUseItemRequested));
            Register(0x07,  7, new OnPacketReceive(OnDragItemRequested));
            Register(0x08, 15, new OnPacketReceive(OnDropItemRequested));
            Register(0x3A,  0, new OnPacketReceive(OnSkillLockChanged));
            Register(0x6C, 19, new OnPacketReceive(OnTargetSent));
            Register(0xB1,  0, new OnPacketReceive(OnGumpButtonPressed));
            Register(0xBF,  0, new OnPacketReceive(OnExtendedCommand));
            Register(0xAD,  0, new OnPacketReceive(OnUnicodeSpeech));

            RegisterExtended(0x1A, 0, new OnPacketReceive(OnStatLockChanged));
        }

        private static void OnUnicodeSpeech( int client, PacketReader reader )
        {
            byte messagetype = reader.ReadByte();
            int color = reader.ReadInt16();
            int font = reader.ReadInt16();
            string lang = reader.ReadString( 4 );
            string text;

            int[] keywords;

            if ((messagetype & 0xC0) != 0)
            {
                int value = reader.ReadInt16();
                int count = ( value & 0xFFF0 ) >> 4;
                int hold = value & 0xF;

                if (count < 0 || count > 50)
                    return;

                List<int> keyList = new List<int>();

                for (int i = 0; i < count; ++i)
                {
                    int speechID;

                    if (( i & 1 ) == 0)
                    {
                        hold <<= 8;
                        hold |= reader.ReadByte();
                        speechID = hold;
                        hold = 0;
                    }
                    else
                    {
                        value = reader.ReadInt16();
                        speechID = ( value & 0xFFF0 ) >> 4;
                        hold = value & 0xF;
                    }

                    if (!keyList.Contains( speechID ))
                        keyList.Add( speechID );
                }

                text = reader.ReadStringSafe();

                keywords = keyList.ToArray();
            }
            else
            {
                keywords = new int[0];
                text = reader.ReadUnicodeStringSafe();
            }
            OutgoingPackets.OnUnicodeSpeech( client, messagetype, color, font, lang, keywords, text );
        }

        private static void OnMoveRequested(int client, PacketReader reader)
        {
            int direction2 = reader.ReadByte() & 0x07;
            int sequence2 = reader.ReadByte();
            OutgoingPackets.OnMoveRequested(client, direction2, sequence2);
        }

        private static void OnUseItemRequested(int client, PacketReader reader)
        {
            int serial6 = reader.ReadInt32();
            OutgoingPackets.OnUseItemRequested(client, serial6);
        }

        private static void OnDragItemRequested(int client, PacketReader reader)
        {
            int serial7 = reader.ReadInt32();
            int amount7 = reader.ReadInt16();
            OutgoingPackets.OnDragItemRequested(client, serial7, amount7);
        }

        private static void OnDropItemRequested(int client, PacketReader reader)
        {
            const int oldLen08 = 0x0E;
            const int newLen08 = 0x0F;
            if (reader.Size != oldLen08 && reader.Size != newLen08)
                return;
            int serial8 = reader.ReadInt32();
            int x8 = reader.ReadInt16();
            int y8 = reader.ReadInt16();
            int z8 = reader.ReadSByte();
            int container8;
            if (reader.Size == newLen08)
                reader.ReadByte(); // Grid location
            container8 = reader.ReadInt32();
            OutgoingPackets.OnDropItemRequested(client, serial8, x8, y8, z8, container8);
        }

        private static void OnSkillLockChanged(int client, PacketReader reader)
        {
            int skillID3a = reader.ReadInt16();
            LockStatus lockStatus3a = (LockStatus)reader.ReadByte();
            OutgoingPackets.OnSkillLockChanged(client, skillID3a, lockStatus3a);
        }

        private static void OnTargetSent(int client, PacketReader reader)
        {
            int type6c = reader.ReadByte();
            int charSerial6c = reader.ReadInt32();
            bool checkCrime6c = reader.ReadByte() == 1;
            int serial6c = reader.ReadInt32();
            int x6c = reader.ReadUInt16();
            int y6c = reader.ReadUInt16();
            int z6c = reader.ReadUInt16();
            int id6c = reader.ReadUInt16();
            OutgoingPackets.OnTargetSent(client, type6c, checkCrime6c, serial6c, x6c, y6c, z6c, id6c, reader.Data);
        }

        private static void OnGumpButtonPressed(int client, PacketReader reader)
        {
            //gump choice, only button & switches are processed
            int serialb1 = reader.ReadInt32();
            int gumpb1 = reader.ReadInt32();
            int buttonb1 = reader.ReadInt32();
            int[] switchvaluesb1 = new int[0];
            if (gumpb1 != 461)
            {
                int switchesb1 = reader.ReadInt32();
                switchvaluesb1 = new int[switchesb1];

                for (int xb1 = 0; xb1 < switchesb1; xb1++)
                {
                    switchvaluesb1[xb1] = reader.ReadInt32();
                }
            }
            OutgoingPackets.OnGumpButtonPressed(client, serialb1, gumpb1, buttonb1, switchvaluesb1);
        }

        private static void OnExtendedCommand(int client, PacketReader reader)
        {
            int command = reader.ReadInt16();

            PacketHandler handler = GetExtendedHandler(command);
            if (handler != null)
                handler.OnReceive(client, reader);
        }

        private static void OnStatLockChanged(int client, PacketReader reader)
        {
            int type = reader.ReadByte();
            int value = reader.ReadByte();
            OutgoingPackets.OnStatLockChanged(client, type, value);
        }

        private static void Register(int packetId, int length, OnPacketReceive onReceive)
        {
            m_Handlers[packetId] = new PacketHandler(packetId, length, onReceive);
        }

        private static void RegisterExtended(int packetId, int length, OnPacketReceive onReceive)
        {
            m_ExtendedHandlers[packetId] = new PacketHandler(packetId, length, onReceive);
        }

        internal static PacketHandler GetHandler(int packetId)
        {
            return m_Handlers[packetId];
        }

        private static PacketHandler GetExtendedHandler(int packetId)
        {
            return m_ExtendedHandlers[packetId];
        }
    }
}