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
        public static void AddGumpResponseFilter( int client, uint serial, uint gumpid )
        {
            ClientInfo ci;
            if (ClientInfoCollection.GetClient( client, out ci ))
            {
                byte[] bserial = new byte[4];
                byte[] bgumpid = new byte[4];
                bserial[0] = (byte) ( serial >> 24 );
                bserial[1] = (byte) ( serial >> 16 );
                bserial[2] = (byte) ( serial >> 8 );
                bserial[3] = (byte) ( serial );
                bgumpid[0] = (byte) ( gumpid >> 24 );
                bgumpid[1] = (byte) ( gumpid >> 16 );
                bgumpid[2] = (byte) ( gumpid >> 8 );
                bgumpid[3] = (byte) ( gumpid );

                AddSendFilter( client, 0xB1, new PacketFilterCondition[] { new PacketFilterCondition( 3, bserial, 4 ), new PacketFilterCondition( 7, bgumpid, 4 ) } );
            }
        }

        //public static void RemoveGumpResponseFilter( int client, int serial, int gumpid )
        //{
        //    ClientInfo ci;
        //    if (ClientInfoCollection.GetClient( client, out ci ))
        //        Network.SendCommand( ci.IPCServerIndex, Command.RemoveGumpResponseFilter, client, serial, gumpid );
        //}

        //public static void ClearGumpResponseFilter ( int client )
        //{
        //    ClientInfo ci;
        //    if (ClientInfoCollection.GetClient( client, out ci ))
        //        Network.SendCommand( ci.IPCServerIndex, Command.ClearGumpResponseFilter );
        //}
    }
}
#endif