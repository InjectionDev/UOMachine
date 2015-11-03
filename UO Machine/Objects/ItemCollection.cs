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
using System.Collections.Generic;
using System.Threading;
using UOMachine.Utility;

namespace UOMachine
{
    public sealed class ItemCollection
    {
        private const int myDefaultCapacity = 125;
        private readonly int myClient;
        private Dictionary<int, Item> myItemList; //int = serial
        private object mySyncRoot;
        internal delegate void dCollectionChanged(int newCount);
        internal event dCollectionChanged CollectionChangedEvent;

        public readonly int Serial;

        internal ItemCollection(int client, int serial) : this(client, serial, myDefaultCapacity) { }

        internal ItemCollection(int client, int serial, int capacity)
        {
            myClient = client;
            mySyncRoot = this;
            myItemList = new Dictionary<int, Item>(capacity);
            this.Serial = serial;
        }

        private void OnCollectionChanged()
        {
            dCollectionChanged handler = CollectionChangedEvent;
            if (handler != null)
            {
                ThreadPool.QueueUserWorkItem(delegate { handler(myItemList.Count); });
            }
        }

        public int GetTotalItemCount()
        {
            int count = 0;
            lock (mySyncRoot)
            {
                foreach (Item item in this.myItemList.Values)
                {
                    count++;
                    if (item.IsContainer)
                        count += item.Container.GetTotalItemCount();
                }
            }
            return count;
        }

        /// <summary>
        /// Get item with specified serial.
        /// </summary>
        /// <param name="serial">Serial of item to retrieve.</param>
        /// <param name="item">Item (out).</param>
        /// <returns>Null if no match is found.</returns>
        public bool GetItem(int serial, out Item item)
        {
            lock (mySyncRoot)
            {
                try
                {
                    foreach (Item i in this.myItemList.Values)
                    {
                        if (i.Serial == serial)
                        {
                            item = i;
                            return true;
                        }
                        if (i.IsContainer)
                        {
                            Item i2;
                            if (i.Container.GetItem(serial, out i2))
                            {
                                item = i2;
                                return true;
                            }
                        }
                    }
                }
                catch (Exception) { }
            }
            item = null;
            return false;
        }

        /// <summary>
        /// Get all items contained within this container.
        /// </summary>
        public Item[] GetItems()
        {
            lock (mySyncRoot)
            {
                Item[] itemArray = new Item[myItemList.Values.Count];
                myItemList.Values.CopyTo(itemArray, 0);
                return itemArray;
            }
        }

        /// <summary>
        /// Search container for first item with matching id (graphic).
        /// </summary>
        /// <param name="id"></param>
        /// <param name="item">Item (out).</param>
        /// <returns>True on success.</returns>
        public bool FindItem(int id, out Item item)
        {
            Item tempItem;
            lock (mySyncRoot)
            {
                foreach (Item i in myItemList.Values)
                {
                    if (i.ID == id)
                    {
                        item = i;
                        return true;
                    }
                    if (i.IsContainer && i.Container.FindItem(id, out tempItem))
                    {
                        item = tempItem;
                        return true;
                    }
                }
                item = null;
                return false;
            }
        }

        /// <summary>
        /// Get array of all items that match specified id.
        /// </summary>
        /// <returns>True on success.</returns>
        public bool FindItems(int id, out Item[] items)
        {
            Item[] tempList;
            List<Item> itemList = new List<Item>(32);
            lock (mySyncRoot)
            {
                foreach (Item i in myItemList.Values)
                {
                    if (i.ID == id) itemList.Add(i);
                    if (i.IsContainer)
                        if (i.Container.FindItems(id, out tempList))
                            itemList.AddRange(tempList);
                }
            }
            if (itemList.Count > 0)
            {
                items = itemList.ToArray();
                return true;
            }
            items = null;
            return false;
        }

        /// <summary>
        /// Get array of all items that match specified ids.
        /// </summary>
        /// <returns>True on success.</returns>
        public bool FindItems(int[] ids, out Item[] items)
        {
            Item[] tempList;
            List<Item> itemList = new List<Item>(32);
            lock (mySyncRoot)
            {
                foreach (Item i in myItemList.Values)
                {
                    foreach (int x in ids)
                    {
                        if (i.ID == x && !itemList.Contains(i)) itemList.Add(i);
                        if (i.IsContainer)
                        {
                            if (i.Container.FindItems(ids, out tempList))
                            {
                                foreach (Item i2 in tempList)
                                {
                                    if (!itemList.Contains(i2)) itemList.Add(i2);
                                }
                            }
                        }
                    }
                }
            }
            if (itemList.Count > 0)
            {
                items = itemList.ToArray();
                return true;
            }
            items = null;
            return false;
        }

        /// <summary>
        /// Get array of all items at specified coordinates.
        /// </summary>
        /// <param name="x">X Coordinate</param>
        /// <param name="y">Y Coordinate</param>
        /// <param name="items">Array of found items (out).</param>
        /// <returns>True if array counts items, otherwise false.</returns>
        public bool FindItems(int x, int y, out Item[] items)
        {
            List<Item> itemList = new List<Item>(32);

            lock (mySyncRoot)
            {
                foreach (Item i in myItemList.Values)
                {
                    if (i.X == x && i.Y == y && !itemList.Contains(i)) itemList.Add(i);
                }
            }
            if (itemList.Count > 0)
            {
                items = itemList.ToArray();
                return true;
            }
            items = null;
            return false;
        }

        internal void Add(Item item)
        {
            bool changed = false;
            lock (mySyncRoot)
            {
                try
                {
                if (!this.myItemList.ContainsKey(item.Serial))
                {
                    this.myItemList.Add(item.Serial, item);
                    changed = true;
                }
                else this.myItemList[item.Serial] = item;
            }
                catch (Exception) { }
            }
            if (changed) OnCollectionChanged();
        }

        internal void Add(Item[] items)
        {
            bool changed = false;
            lock (mySyncRoot)
            {
                foreach (Item i in items)
                {
                    if (!this.myItemList.ContainsKey(i.Serial))
                    {
                        this.myItemList.Add(i.Serial, i);
                        changed = true;
                    }
                }
            }
            if (changed) OnCollectionChanged();
        }

        internal void RemoveByOwner(int serial)
        {
            bool changed = false;
            Item[] items = this.GetItems();
            for (int x = 0; x < items.Length; x++)
            {
                if (items[x] != null && items[x].Owner == serial)
                {
                    if (items[x].IsContainer) items[x].Container.myItemList.Clear();
                    this.myItemList.Remove(items[x].Serial);
                    changed = true;
                }
                else if (items[x] != null && items[x].IsContainer) items[x].Container.RemoveByOwner(serial);
            }
            if (changed) OnCollectionChanged();
        }

        internal void RemoveByDistance(int maxDistance, int x, int y)
        {
            Item[] items = this.GetItems();
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && items[i].Owner == 0 && UOMath.Distance(x, y, items[i].X, items[i].Y) > maxDistance)
                {
                    this.Remove(items[i].Serial);
                }
            }
        }

        internal bool Remove(int serial)
        {
            lock (mySyncRoot)
            {
                if (this.myItemList.ContainsKey(serial))
                {
                    this.myItemList.Remove(serial);
                    OnCollectionChanged();
                    return true;
                }
                try
                {
                    foreach (Item i in this.myItemList.Values)
                    {
                        if (i.IsContainer)
                        {
                            if (i.Container.Remove(serial)) return true;
                        }
                    }
                }
                catch (Exception) { }
            }
            return false;
        }
    }
}