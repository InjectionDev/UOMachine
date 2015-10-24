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

using System;
using System.Threading;
using System.Collections.Generic;
using UOMachine.Tree;
using UOMachine.Utility;
using UOMachine.Data;

namespace UOMachine
{
    public static class ClientInfoCollection
    {
        public static ClientInfo[] ClientList = new ClientInfo[32];

        private static int myCount;
        /// <summary>
        /// Number of active UO clients.
        /// </summary>
        public static int Count
        {
            get { return Thread.VolatileRead(ref myCount); }
            set { Interlocked.Exchange(ref myCount, value); }
        }

        internal static void Dispose()
        {
            ClientInfo ci;
            for (int x = 0; x < ClientList.Length; x++)
                if (GetClient(x, out ci)) ci.Dispose();
            ClientList = new ClientInfo[32];
        }

        /// <summary>
        /// Get number of running UO clients currently managed by UO Machine.
        /// </summary>
        public static int ActiveClients()
        {
            ClientInfo ci;
            int count = 0;
            for (int x = 0; x < ClientList.Length; x++)
                if (GetClient(x, out ci)) count++;
            return count;
        }

        /// <summary>
        /// Return array of indices corresponding to active clients.
        /// </summary>
        public static int[] ActiveClientIndices()
        {
            ClientInfo ci;
            List<int> activeList = new List<int>(32);
            for (int x = 0; x < ClientList.Length; x++)
                if (GetClient(x, out ci)) activeList.Add(x);
            return activeList.ToArray();
        }

        public static bool AddClient(ClientInfo clientInfo, out int instance)
        {
            if (Count >= ClientList.Length)
            {
                throw new ApplicationException("Maximum number of simultaneous clients exceeded.");
            }

            for (int x = 0; x < ClientList.Length; x++)
            {
                ClientInfo ci = ThreadHelper.VolatileRead<ClientInfo>(ref ClientList[x]);
                if (ci == null)
                {
                    Interlocked.Exchange<ClientInfo>(ref ClientList[x], clientInfo);
                    TreeViewUpdater.AddPlaceHolder(x, clientInfo.ProcessID);
                    Count++;
                    instance = x;
                    return true;
                }
            }
            instance = -1;
            return false;
        }

        /// <summary>
        /// Get ClientInfo by specified index.
        /// </summary>
        /// <param name="index">Client index.</param>
        /// <param name="clientInfo">ClientInfo (out)</param>
        /// <returns>True if clientInfo is not null.</returns>
        public static bool GetClient(int index, out ClientInfo clientInfo)
        {
            if (index < 0 || index > 31)
            {
                clientInfo = null;
                return false;
            }
            clientInfo = ThreadHelper.VolatileRead<ClientInfo>(ref ClientList[index]);
            return clientInfo == null ? false : true;
        }

        public static void RemoveByPid(int pid)
        {
            ClientInfo ci;
            for (int x = 0; x < 32; x++)
            {
                if (GetClient(x, out ci) && ci.ProcessID == pid)
                {
                    RemoveAt(x);
                }
            }
        }

        public static void RemoveAt(int index)
        {
            ClientInfo ci;
            if (GetClient(index, out ci))
            {
                ci.Dispose();
                Interlocked.Exchange<ClientInfo>(ref ClientList[index], null);
                Interlocked.Decrement(ref myCount);
            }
        }

        public static int GetZ(int index)
        {
            ClientInfo ci;
            if (GetClient(index, out ci))
            {
                byte[] memBytes = new byte[1];
                Memory.Read(ci.Handle, ci.ZAddress, memBytes, true);
                return (sbyte)memBytes[0];
            }
            return int.MaxValue;
        }

        /// <summary>
        /// Get client index which matches specified process ID.
        /// </summary>
        public static bool GetPIDOwner(int processID, out int client)
        {
            ClientInfo ci;
            for (int x = 0; x < ClientList.Length; x++)
            {
                if (GetClient(x, out ci) && ci.ProcessID == processID)
                {
                    client = x;
                    return true;
                }
            }
            client = -1;
            return false;
        }

        /// <summary>
        /// Get client index which matches specified IPC index.
        /// </summary>
        public static bool GetIPCOwner(int IPCClient, out int client)
        {
            ClientInfo ci;
            for (int x = 0; x < ClientList.Length; x++)
            {
                if (GetClient(x, out ci) && ci.IPCServerIndex == IPCClient)
                {
                    client = x;
                    return true;
                }
            }
            client = -1;
            return false;
        }

        /// <summary>
        /// Get mobile from specified client.
        /// </summary>
        /// <param name="index">Client index.</param>
        /// <param name="serial">Serial of mobile to get.</param>
        /// <param name="mobile">Mobile (out).</param>
        /// <returns>False if not found.</returns>
        public static bool GetMobile(int index, int serial, out Mobile mobile)
        {
            ClientInfo ci;
            if (GetClient(index, out ci))
                return ci.Mobiles.GetMobile(serial, out mobile);
            mobile = null;
            return false;
        }

        /// <summary>
        /// Get PlayerMobile from specified client.
        /// </summary>
        /// <param name="index">Target client.</param>
        /// <param name="playerMobile">PlayerMobile (out).</param>
        public static bool GetPlayer(int index, out PlayerMobile playerMobile)
        {
            ClientInfo ci;
            if (GetClient(index, out ci) && ci.Player != null && ci.Player.Serial != 0)
            {
                playerMobile = ci.Player;
                return true;
            }
            playerMobile = null;
            return false;
        }

        /// <summary>
        /// Find mobile with matching ID in specified client.
        /// </summary>
        /// <returns>True on success.</returns>
        public static bool FindMobile(int index, int id, out Mobile mobile)
        {
            ClientInfo ci;
            if (GetClient(index, out ci))
                return ci.Mobiles.FindMobile(id, out mobile);
            mobile = null;
            return false;
        }

        /// <summary>
        /// Find all mobiles with matching ID in specified client.
        /// </summary>
        /// <returns>True on success.</returns>
        public static bool FindMobiles(int index, int id, out Mobile[] mobiles)
        {
            ClientInfo ci;
            if (GetClient(index, out ci))
                return ci.Mobiles.FindMobiles(id, out mobiles);
            mobiles = null;
            return false;
        }

        /// <summary>
        /// Find all mobiles with matching IDs in specified client.
        /// </summary>
        /// <returns>True on success.</returns>
        public static bool FindMobiles(int index, int[] ids, out Mobile[] mobiles)
        {
            ClientInfo ci;
            if (GetClient(index, out ci))
                return ci.Mobiles.FindMobiles(ids, out mobiles);
            mobiles = null;
            return false;
        }

        /// <summary>
        /// Get item from specified client.
        /// </summary>
        /// <param name="index">Client index.</param>
        /// <param name="serial">Serial of item to get.</param>
        /// <param name="item">Item (out).</param>
        /// <returns>False if not found.</returns>
        public static bool GetItem(int index, int serial, out Item item)
        {
            ClientInfo ci;
            if (GetClient(index, out ci))
                return ci.Items.GetItem(serial, out item);
            item = null;
            return false;
        }

        /// <summary>
        /// Find item with matching ID in specified client.
        /// </summary>
        /// <returns>True on success.</returns>
        public static bool FindItem(int index, int id, out Item item)
        {
            ClientInfo ci;
            if (GetClient(index, out ci))
                return ci.Items.FindItem(id, out item);
            item = null;
            return false;
        }

        /// <summary>
        /// Find all items with matching ID in specified client.
        /// </summary>
        /// <returns>True on success.</returns>
        public static bool FindItems(int index, int id, out Item[] items)
        {
            ClientInfo ci;
            if (GetClient(index, out ci))
                return ci.Items.FindItems(id, out items);
            items = null;
            return false;
        }

        /// <summary>
        /// Find all items with matching IDs in specified client.
        /// </summary>
        /// <returns>True on success.</returns>
        public static bool FindItems(int index, int[] ids, out Item[] items)
        {
            ClientInfo ci;
            if (GetClient(index, out ci))
                return ci.Items.FindItems(ids, out items);
            items = null;
            return false;
        }

        /// <summary>
        /// Find all items at specified coordinates.
        /// </summary>
        /// <param name="index">Client index.</param>
        /// <param name="x">X Coordinate.</param>
        /// <param name="y">Y Coordinate.</param>
        /// <param name="items">Array of items (out).</param>
        /// <returns>True if items found, otherwise false.</returns>
        public static bool FindItems(int index, int x, int y, out Item[] items)
        {
            ClientInfo ci;
            if (GetClient(index, out ci))
                return ci.Items.FindItems(x, y, out items);
            items = null;
            return false;
        }

        public static void AddItem(int index, Item item)
        {
            ClientInfo ci;
            if (GetClient(index, out ci)) ci.Items.Add(item);
        }

        public static void AddItems(int index, Item[] items)
        {
            ClientInfo ci;
            if (GetClient(index, out ci)) ci.Items.Add(items);
        }

        public static void UpdateStamina(int index, int serial, int maxStamina, int stamina)
        {
            ClientInfo ci;
            if (!GetClient(index, out ci)) return;

            if (ci.Player.Serial == serial)
            {
                ci.Player.MaxStamina = maxStamina;
                ci.Player.Stamina = stamina;
            }
            else
            {
                Mobile m;
                if (ci.Mobiles.GetMobile(serial, out m))
                {
                    try
                    {
                        m.Stamina = stamina;

                    m.MaxStamina = maxStamina;
                    }
                    catch { }

                }
            }
        }

        public static void UpdateMana(int index, int serial, int maxMana, int mana)
        {
            ClientInfo ci;
            if (!GetClient(index, out ci)) return;

            if (ci.Player.Serial == serial)
            {
                ci.Player.MaxMana = maxMana;
                ci.Player.Mana = mana;
            }
            else
            {
                try
                {
                    Mobile m;
                    if (ci.Mobiles.GetMobile(serial, out m))
                    {
                        m.MaxMana = maxMana;
                        m.Mana = mana;
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public static void UpdateHealth(int index, int serial, int maxHealth, int health)
        {
            ClientInfo ci;
            if (!GetClient(index, out ci)) return;

            if (ci.Player.Serial == serial)
            {
                ci.Player.MaxHealth = maxHealth;
                ci.Player.Health = health;
            }
            else
            {
                Mobile m;
                if (ci.Mobiles.GetMobile(serial, out m))
                {
                    m.MaxHealth = maxHealth;
                    m.Health = health;
                }
            }
        }

        public static void UpdateLabel(int index, int serial, string name, string text)
        {
            ClientInfo ci;
            if (!GetClient(index, out ci)) return;

            if (UOMath.IsMobile(serial))
            {
                Mobile m;
                if (ci.Mobiles.GetMobile(serial, out m))
                {
                    m.Name = name;
                    m.Label = text;
                }
            }
            else
            {
                Item i;
                if (ci.Items.GetItem(serial, out i))
                {
                    i.Name = UOMachine.Data.Cliloc.GetLocalString(name);
                    i.Label = text;
                }
            }
        }

        public static void UpdateProperties(int index, int serial, string name, Property[] properties, string propertyText)
        {
            ClientInfo ci;
            if (GetClient(index, out ci))
            {
                if (UOMath.IsMobile(serial))
                {
                    if (ci.Player.Serial == serial)
                    {
                        ci.Player.Name = name;
                        ci.Player.Properties = properties;
                        ci.Player.PropertyText = propertyText;
                        TreeViewUpdater.EditPlayerNode(index, ClientList[index].Player);
                    }
                    else
                    {
                        Mobile m;
                        if (ci.Mobiles.GetMobile(serial, out m))
                        {
                            m.Name = name;
                            m.Properties = properties;
                            m.PropertyText = propertyText;
                        }
                    }
                }
                else
                {
                    Item i;
                    if (ci.Items.GetItem(serial, out i))
                    {
                        i.Name = Cliloc.GetLocalString(name);
                        i.Properties = properties;
                        i.PropertyText = propertyText;
                    }
                }
            }
        }
    }
}