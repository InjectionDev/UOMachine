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

using System.Collections.Generic;
using System.Threading;
using UOMachine.Utility;

namespace UOMachine
{
    public sealed class MobileCollection
    {
        private const int myDefaultCapacity = 64;
        private ItemCollection myLinkedItemCollection;
        private object mySyncRoot;
        private Dictionary<int, Mobile> myMobileList;
        public delegate void dCollectionChanged(int newCount);
        public event dCollectionChanged CollectionChangedEvent;


        public MobileCollection(ItemCollection linkedItemCollection)
        {
            mySyncRoot = this;
            myMobileList = new Dictionary<int, Mobile>(myDefaultCapacity);
            myLinkedItemCollection = linkedItemCollection;
        }

        public MobileCollection(int capacity, ItemCollection linkedItemCollection)
        {
            mySyncRoot = this;
            myMobileList = new Dictionary<int, Mobile>(capacity);
            myLinkedItemCollection = linkedItemCollection;
        }

        private void OnCollectionChanged()
        {
            dCollectionChanged handler = CollectionChangedEvent;
            if (handler != null)
            {
                ThreadPool.QueueUserWorkItem(delegate { handler(myMobileList.Count); });
            }
        }

        /// <summary>
        /// Find first mobile that matches specified ID.
        /// </summary>
        /// <returns>True on success.</returns>
        public bool FindMobile(int id, out Mobile mobile)
        {
            lock (mySyncRoot)
            {
                foreach (Mobile m in myMobileList.Values)
                {
                    if (m.ID == id)
                    {
                        mobile = m;
                        return true;
                    }
                }
            }
            mobile = null;
            return false;
        }

        public bool FindMobileByLayerSerial(Layer layer, int serial, out Mobile mobile)
        {
            lock (mySyncRoot)
            {
                foreach (Mobile m in myMobileList.Values)
                {
                    Item item;
                    if (m.GetEquippedItem( layer, out item ))
                    {
                        if (item.Serial == serial)
                        {
                            mobile = m;
                            return true;
                        }
                    }
                }
            }
            mobile = null;
            return false;
        }

        public bool FindMobiles(int id, out Mobile[] mobiles)
        {
            List<Mobile> mobileList = new List<Mobile>();
            lock (mySyncRoot)
            {
                foreach (Mobile m in myMobileList.Values)
                    if (m.ID == id) mobileList.Add(m);
            }
            if (mobileList.Count > 0)
            {
                mobiles = mobileList.ToArray();
                return true;
            }
            mobiles = null;
            return false;
        }

        public bool FindMobiles(int[] ids, out Mobile[] mobiles)
        {
            List<Mobile> mobileList = new List<Mobile>();
            lock (mySyncRoot)
            {
                foreach (Mobile m in myMobileList.Values)
                {
                    foreach (int i in ids)
                    {
                        if (m.ID == i && !mobileList.Contains(m)) mobileList.Add(m);
                    }
                }
            }
            if (mobileList.Count > 0)
            {
                mobiles = mobileList.ToArray();
                return true;
            }
            mobiles = null;
            return false;
        }

        public void Add(Mobile mobile)
        {
            lock (mySyncRoot)
            {
                if (!myMobileList.ContainsKey(mobile.Serial))
                {
                    myMobileList.Add(mobile.Serial, mobile);
                    OnCollectionChanged();
                }
            }
        }

        public bool GetMobile(int serial, out Mobile mobile)
        {
            return myMobileList.TryGetValue(serial, out mobile);
        }

        public Mobile[] GetMobiles()
        {
            lock (mySyncRoot)
            {
                Mobile[] mobileArray = new Mobile[myMobileList.Values.Count];
                myMobileList.Values.CopyTo(mobileArray, 0);
                return mobileArray;
            }
        }

        /// <summary>
        /// Remove Mobile with specified serial.
        /// </summary>
        /// <returns>True if Mobile is found and removed.</returns>
        public bool Remove(int serial)
        {
            lock (mySyncRoot)
            {
                Mobile m;
                if (myMobileList.TryGetValue(serial, out m))
                {
                    myMobileList.Remove(serial);
                    //myLinkedItemCollection.RemoveByOwner(serial);
                    foreach (int s in m.GetAllLayers())
                        if (s != 0) myLinkedItemCollection.Remove(s);
                    this.OnCollectionChanged();
                    return true;
                }
            }
            return false;
        }

        public void RemoveByDistance(int maxDistance, int x, int y)
        {
            bool changed = false;
            Mobile[] mobileArray = this.GetMobiles();
            for (int i = 0; i < mobileArray.Length; i++)
            {
                if (mobileArray[i] == null)
                    continue;

                double d = UOMath.Distance(x, y, mobileArray[i].X, mobileArray[i].Y);
                if (d > maxDistance)
                {
                    myMobileList.Remove(mobileArray[i].Serial);
                    myLinkedItemCollection.RemoveByOwner(mobileArray[i].Serial);
                    /*foreach (int serial in mobileArray[i].GetAllLayers())
                    {
                        if (serial != 0) myLinkedItemCollection.Remove(serial);
                    }*/
                    changed = true;
                }
            }
            if (changed) this.OnCollectionChanged();
        }
    }
}