/* Copyright (C) 2014 John Scott
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
#define FILTER_TEST

#if FILTER_TEST

using System;
using UOMachine;
using UOMachine.IPC;

namespace UOMachine.Macros
{
    public static partial class MacroEx
    {
        public static void AddSendFilter(int client, byte packetID)
        {
            AddSendFilter( client, packetID, null );
        }

        public static void AddSendFilter( int client, byte packetID, PacketFilterCondition[] conditions )
        {
            ClientInfo ci;
            if (ClientInfoCollection.GetClient( client, out ci ))
            {
                PacketFilterInfo pfi = new PacketFilterInfo( packetID, conditions );
                byte[] bytes = pfi.Serialize();
                Network.SendCommand( ci.IPCServerIndex, Command.AddSendFilterConditional, bytes );
            }
        }

        public static void RemoveSendFilter(int client, byte packetID)
        {
            ClientInfo ci;
            if (ClientInfoCollection.GetClient(client, out ci))
                Network.SendCommand(ci.IPCServerIndex, Command.RemoveSendFilter, packetID);
        }

        public static void ClearSendFilter(int client, byte packetID)
        {
            ClientInfo ci;
            if (ClientInfoCollection.GetClient(client, out ci))
                Network.SendCommand(ci.IPCServerIndex, Command.ClearSendFilter);
        }
    }
}
#endif