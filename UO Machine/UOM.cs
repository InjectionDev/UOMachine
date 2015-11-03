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
using UOMachine.IPC;
using UOMachine.Utility;
using UOMachine.Data;
using UOMachine.Events;
using UOMachine.Resources;
using EasyHook;
using System.Diagnostics;
using System.IO;

namespace UOMachine
{
    public static class UOM
    {
        private delegate void dShutDown();
        public delegate void dClientListChanged();
        private static object myClientListChangedLock = new object();
        private static event dClientListChanged myClientListChangedEvent;
        public static string StartupPath = Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location );

        private static MainWindow myMainWindow;

        public static event dClientListChanged ClientListChangedEvent
        {
            add { lock (myClientListChangedLock) { myClientListChangedEvent += value; } }
            remove { lock (myClientListChangedLock) { myClientListChangedEvent -= value; } }
        }
        internal const string ServerName = "UO Machine";
        private static string[] mySkillNames;

        private static void myShutDown()
        {
            System.Windows.Application.Current.Shutdown();
        }

        internal static void ShutDown()
        {
            dShutDown shutDown = new dShutDown( myShutDown );
            System.Windows.Application.Current.Dispatcher.Invoke( shutDown, null );
        }

        internal static bool Initialize( MainWindow mainWindow )
        {
            if (File.Exists( Path.Combine( StartupPath, "Updater_New.exe" ) ))
            {
                try
                {
                    File.Copy( Path.Combine( StartupPath, "Updater_New.exe" ), Path.Combine( StartupPath, "Updater.exe" ), true );
                }
                catch (Exception)
                {
                }
            }

            string[] renameFiles = Directory.GetFiles( StartupPath, "*.new", SearchOption.AllDirectories );
            foreach (string renameFile in renameFiles)
            {
                string path = Path.GetDirectoryName( renameFile );
                string origFile = Path.Combine( path, Path.GetFileNameWithoutExtension( renameFile ) );
                try
                {
                    File.Delete( origFile );
                    File.Move( renameFile, origFile );
                }
                catch (Exception)
                {
                }
            }

            if (!MainWindow.CurrentOptions.IsValid()) return false;
            //try { Config.Register( "UOM hooks", Path.Combine( StartupPath, "UOMachine.exe" ), Path.Combine( StartupPath, "ClientHook.dll" ) ); }
            //catch (Exception ex)
            //{
            //    Utility.Log.LogMessage( ex );
            //    /* Ensure EasyHook files are in output directory if you get this error */
            //    System.Windows.MessageBox.Show( Strings.ErrorwithGACinstallation );
            //    return false;
            //}
            myMainWindow = mainWindow;
            InternalEventHandler.IncomingPacketHandler.Initialize();
            InternalEventHandler.OutgoingPacketHandler.Initialize();
            Network.Initialize();
            InternalEventHandler.IPCHandler.Initialize();
            mySkillNames = Skills.GetSkills( MainWindow.CurrentOptions.UOFolder );
            TileData.Initialize( MainWindow.CurrentOptions.UOFolder );
            Map.Initialize( MainWindow.CurrentOptions.UOFolder, MainWindow.CurrentOptions.CacheLevel );
            Art.Initialize( MainWindow.CurrentOptions.UOFolder );
            Cliloc.Initialize( MainWindow.CurrentOptions.UOFolder );
            NamespaceToAssembly.Initialize();
            IncomingPacketHandlers.Initialize();
            OutgoingPacketHandlers.Initialize();
            return true;
        }

        internal static void Dispose()
        {
            ScriptCompiler.StopScript();
            ClientInfoCollection.Dispose();
            Log.Dispose();
            InternalEventHandler.IPCHandler.Dispose();
            IPC.Network.Dispose();
        }

        internal static void SetStatusLabel( string text )
        {
            if (myMainWindow != null)
            {
                MainWindow.UpdateLabel( myMainWindow.labelStatus, UOMachine.Resources.Strings.Status + " : " + text );
            }
        }

        internal static void OnClientExit( object sender, EventArgs e )
        {
            try
            {
                Process exiting = (Process)sender;
                ClientInfoCollection.RemoveByPid( exiting.Id );
                dClientListChanged handler = null;
                lock (myClientListChangedLock) { handler = myClientListChangedEvent; }
                if (handler != null) ThreadPool.QueueUserWorkItem( delegate { handler(); } );
                SetStatusLabel( Strings.Clientexited );
            }
            catch { }
        }

        /// <summary>
        /// Get array of indices corresponding to active clients.
        /// </summary>
        public static int[] ActiveClientIndices()
        {
            return ClientInfoCollection.ActiveClientIndices();
        }

        /// <summary>
        /// Get number of running UO clients currently managed by UO Machine.
        /// </summary>
        public static int ActiveClients()
        {
            return ClientInfoCollection.ActiveClients();
        }

        /// <summary>
        /// Translate skill ID to skill name according to UO installation defined in options.
        /// Returns empty string on error.
        /// </summary>
        public static string GetSkillName( int skillID )
        {
            if (skillID >= 0 && skillID <= mySkillNames.Length - 1)
                return mySkillNames[skillID];
            return "";
        }

        /// <summary>
        /// Get string array of all skill names for UO installation defined in options.
        /// Array returned will be indexed by skill ID.
        /// </summary>
        public static string[] GetSkillNames()
        {
            return (string[])mySkillNames.Clone();
        }
    }
}
