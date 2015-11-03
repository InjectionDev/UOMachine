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


namespace UOMachine.Macros
{
    public static partial class Macro
    {
        /// <summary>
        /// Get first mobile with matching ID from specified client.
        /// </summary>
        /// <param name="client">Target client.</param>
        /// <param name="id">ID of mobile to find.</param>
        /// <param name="mobile">Mobile (out).</param>
        /// <returns>True on success.</returns>
        public static bool FindMobile(int client, int id, out Mobile mobile)
        {
            return ClientInfoCollection.FindMobile(client, id, out mobile);
        }

        /// <summary>
        /// Get Mobile with specified serial on specified layer.
        /// </summary>
        /// <param name="client">Target client.</param>
        /// <param name="layer">Layer to search on.</param>
        /// <param name="serial">Item serial to find.</param>
        /// <param name="mobile">Mobile (out).</param>
        /// <returns>True on success.</returns>
        public static bool FindMobileByLayerSerial(int client, Layer layer, int serial, out Mobile mobile)
        {
            return ClientInfoCollection.FindMobileByLayerSerial( client, layer, serial, out mobile );
        }
    }
}