﻿/* Copyright (C) 2009 Matthew Geyer
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
        /// Get mobile from specified client.
        /// </summary>
        /// <param name="client">Target client.</param>
        /// <param name="serial">Serial of mobile to retrieve.</param>
        /// <param name="mobile">Mobile (out).</param>
        /// <returns>True on success.</returns>
        public static bool GetMobile(int client, int serial, out Mobile mobile)
        {
            return ClientInfoCollection.GetMobile(client, serial, out mobile);
        }
    }
}