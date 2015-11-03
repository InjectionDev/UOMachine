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
using UOMachine.Utility;

namespace UOMachine.Events
{
    /// <summary>
    /// Collection of thread safe events which provide notifications for specific outgoing packets.
    /// </summary>
    public static class OutgoingPackets
    {
        public delegate void dMoveRequested(int client, int direction, int sequence);
        public delegate void dTargetSent(int client, int targetType, bool checkCrime, int serial, int x, int y, int z, int ID, byte[] packet);
        public delegate void dUseItemRequested(int client, int serial);
        public delegate void dSkillLockChanged(int client, int skillID, LockStatus lockStatus);
        public delegate void dStatLockChanged(int client, int stat, int status);
        public delegate void dDragItemRequested(int client, int serial, int amount);
        public delegate void dDropItemRequested(int client, int serial, int x, int y, int z, int containerSerial);
        public delegate void dGumpButtonPressed(int client, int serial, int gumpID, int buttonID, int[] switches);
        public delegate void dUnicodeSpeech( int client, int messagetype, int color, int font, string lang, int[] keyworkds, string text );

        private static object myGumpButtonPressedLock = new object();
        private static event dGumpButtonPressed myGumpButtonPressedEvent;
        internal static event dGumpButtonPressed InternalGumpButtonPressedEvent;
        public static event dGumpButtonPressed GumpButtonPressedEvent
        {
            add { lock (myGumpButtonPressedLock) { myGumpButtonPressedEvent += value; } }
            remove { lock (myGumpButtonPressedLock) { myGumpButtonPressedEvent -= value; } }
        }

        private static object myDropItemRequestedLock = new object();
        private static event dDropItemRequested myDropItemRequestedEvent;
        internal static event dDropItemRequested InternalDropItemRequestedEvent;
        public static event dDropItemRequested DropItemRequestedEvent
        {
            add { lock (myDropItemRequestedLock) { myDropItemRequestedEvent += value; } }
            remove { lock (myDropItemRequestedLock) { myDropItemRequestedEvent -= value; } }
        }

        private static object myDragItemRequestedLock = new object();
        private static event dDragItemRequested myDragItemRequestedEvent;
        internal static event dDragItemRequested InternalDragItemRequestedEvent;
        public static event dDragItemRequested DragItemRequestedEvent
        {
            add { lock (myDragItemRequestedLock) { myDragItemRequestedEvent += value; } }
            remove { lock (myDragItemRequestedLock) { myDragItemRequestedEvent -= value; } }
        }

        private static object myStatLockChangedLock = new object();
        private static event dStatLockChanged myStatLockChangedEvent;
        internal static event dStatLockChanged InternalStatLockChangedEvent;
        public static event dStatLockChanged StatLockChangedEvent
        {
            add { lock (myStatLockChangedLock) { myStatLockChangedEvent += value; } }
            remove { lock (myStatLockChangedLock) { myStatLockChangedEvent -= value; } }
        }

        private static object mySkillLockChangedLock = new object();
        private static event dSkillLockChanged mySkillLockChangedEvent;
        internal static event dSkillLockChanged InternalSkillLockChangedEvent;
        public static event dSkillLockChanged SkillLockChangedEvent
        {
            add { lock (mySkillLockChangedLock) { mySkillLockChangedEvent += value; } }
            remove { lock (mySkillLockChangedLock) { mySkillLockChangedEvent -= value; } }
        }

        private static object myUseItemRequestedLock = new object();
        private static event dUseItemRequested myUseItemRequestedEvent;
        internal static event dUseItemRequested InternalUseItemRequestedEvent;
        public static event dUseItemRequested UseItemRequestedEvent
        {
            add { lock (myUseItemRequestedLock) { myUseItemRequestedEvent += value; } }
            remove { lock (myUseItemRequestedLock) { myUseItemRequestedEvent -= value; } }
        }

        private static object myTargetSentLock = new object();
        private static event dTargetSent myTargetSentEvent;
        internal static event dTargetSent InternalTargetSentEvent;
        public static event dTargetSent TargetSentEvent
        {
            add { lock (myTargetSentLock) { myTargetSentEvent += value; } }
            remove { lock (myTargetSentLock) { myTargetSentEvent -= value; } }
        }

        private static object myMoveRequestedLock = new object();
        private static event dMoveRequested myMoveRequestedEvent;
        internal static event dMoveRequested InternalMoveRequestedEvent;
        public static event dMoveRequested MoveRequestedEvent
        {
            add { lock (myMoveRequestedLock) { myMoveRequestedEvent += value; } }
            remove { lock (myMoveRequestedLock) { myMoveRequestedEvent -= value; } }
        }

        private static object myUnicodeSpeechLock = new object();
        private static event dUnicodeSpeech myUnicodeSpeechEvent;
        internal static event dUnicodeSpeech InternalUnicodeSpeechEvent;
        public static event dUnicodeSpeech UnicodeSpeechEvent
        {
            add {  lock (myUnicodeSpeechLock) { myUnicodeSpeechEvent += value; } }
            remove { lock (myUnicodeSpeechLock) { myUnicodeSpeechEvent -= value; } }
        }

        /// <summary>
        /// Reset all public events.
        /// </summary>
        public static void ClearEvents()
        {
            lock (myMoveRequestedLock) { myMoveRequestedEvent = null; }
            lock (myTargetSentLock) { myTargetSentEvent = null; }
            lock (myUseItemRequestedLock) { myUseItemRequestedEvent = null; }
            lock (mySkillLockChangedLock) { mySkillLockChangedEvent = null; }
            lock (myStatLockChangedLock) { myStatLockChangedEvent = null; }
            lock (myDragItemRequestedLock) { myDragItemRequestedEvent = null; }
            lock (myDropItemRequestedLock) { myDropItemRequestedEvent = null; }
            lock (myGumpButtonPressedLock) { myGumpButtonPressedEvent = null; }
            lock (myUnicodeSpeechLock) { myUnicodeSpeechEvent = null; }
        }

        internal static void OnUnicodeSpeech(int client, int messagetype, int color, int font, string lang, int[] keywords, string text)
        {
            dUnicodeSpeech handler = InternalUnicodeSpeechEvent;
            if (handler != null) handler( client, messagetype, color, font, lang, keywords, text );
            lock (myUnicodeSpeechLock)
            {
                handler = myUnicodeSpeechEvent;
                if (handler != null) handler( client, messagetype, color, font, lang, keywords, text );
            }
        }

        internal static void OnMoveRequested(int client, int direction, int sequence)
        {
            dMoveRequested handler = InternalMoveRequestedEvent;
            if (handler != null) handler(client, direction, sequence);
            lock (myMoveRequestedLock)
            {
                handler = myMoveRequestedEvent;
                try
                {
                    if (handler != null) handler(client, direction, sequence);
                }
                catch (Exception ex)
                {
                    Log.LogMessage(ex);
                }
            }
        }

        internal static void OnTargetSent(int client, int targetType, bool checkCrime, int serial, int x, int y, int z, int ID, byte[] packet)
        {
            dTargetSent handler = InternalTargetSentEvent;
            if (handler != null) handler(client, targetType, checkCrime, serial, x, y, z, ID, packet);
            lock (myTargetSentLock)
            {
                handler = myTargetSentEvent;
                try
                {
                    if (handler != null) handler(client, targetType, checkCrime, serial, x, y, z, ID, packet);
                }
                catch (Exception ex)
                {
                    Log.LogMessage(ex);
                }
            }
        }

        internal static void OnUseItemRequested(int client, int serial)
        {
            dUseItemRequested handler = InternalUseItemRequestedEvent;
            if (handler != null) handler(client, serial);
            lock (myUseItemRequestedLock)
            {
                handler = myUseItemRequestedEvent;
                try
                {
                    if (handler != null) handler(client, serial);
                }
                catch (Exception x)
                {
                    Log.LogMessage(x);
                }
            }
        }

        internal static void OnSkillLockChanged(int client, int skillID, LockStatus lockStatus)
        {
            dSkillLockChanged handler = InternalSkillLockChangedEvent;
            if (handler != null) handler(client, skillID, lockStatus);
            lock (mySkillLockChangedLock)
            {
                handler = mySkillLockChangedEvent;
                try
                {
                    if (handler != null) handler(client, skillID, lockStatus);
                }
                catch (Exception x)
                {
                    Log.LogMessage(x);
                }
            }
        }

        internal static void OnStatLockChanged(int client, int stat, int status)
        {
            dStatLockChanged handler = InternalStatLockChangedEvent;
            if (handler != null) handler(client, stat, status);
            lock (myStatLockChangedLock)
            {
                handler = myStatLockChangedEvent;
                try
                {
                    if (handler != null) handler(client, stat, status);
                }
                catch (Exception x)
                {
                    Log.LogMessage(x);
                }
            }
        }

        internal static void OnDragItemRequested(int client, int serial, int amount)
        {
            dDragItemRequested handler = InternalDragItemRequestedEvent;
            if (handler != null) handler(client, serial, amount);
            lock (myDragItemRequestedLock)
            {
                handler = myDragItemRequestedEvent;
                try
                {
                    if (handler != null) handler(client, serial, amount);
                }
                catch (Exception x)
                {
                    Log.LogMessage(x);
                }
            }
        }

        internal static void OnDropItemRequested(int client, int serial, int x, int y, int z, int containerSerial)
        {
            dDropItemRequested handler = InternalDropItemRequestedEvent;
            if (handler != null) handler(client, serial, x, y, z, containerSerial);
            lock (myDropItemRequestedLock)
            {
                handler = myDropItemRequestedEvent;
                try
                {
                    if (handler != null) handler(client, serial, x, y, z, containerSerial);
                }
                catch (Exception ex)
                {
                    Log.LogMessage(ex);
                }
            }
        }

        internal static void OnGumpButtonPressed(int client, int serial, int gumpID, int buttonID, int[] switches)
        {
            dGumpButtonPressed handler = InternalGumpButtonPressedEvent;
            if (handler != null) handler(client, serial, gumpID, buttonID, switches);
            lock (myGumpButtonPressedLock)
            {
                handler = myGumpButtonPressedEvent;
                try
                {
                    if (handler != null) handler(client, serial, gumpID, buttonID, switches);
                }
                catch (Exception ex)
                {
                    Log.LogMessage(ex);
                }
            }
        }

    }
}