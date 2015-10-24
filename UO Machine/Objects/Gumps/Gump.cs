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
using System.IO;
using System.Text;
using UOMachine.Data;
using UOMachine.Utility;

namespace UOMachine
{
    public enum GumpButtonType
    {
        Page = 0,
        Reply = 1
    }

    public class Gump
    {
        public readonly string Layout;
        public readonly GumpPage[] Pages;

        public string[] Strings
        {
            get { return myStrings.ToArray(); }
            set
            {
                myStrings = new List<string>();
                for (int i = 0; i < value.Length; i++)
                {
                    myStrings.Add( value[i] );
                }
            }
        }

        public GumpElement[] GumpElements
        {
            get { return myElements.ToArray(); }
            set
            {
                myElements = new List<GumpElement>();
                for (int i = 0; i < value.Length;i++)
                {
                    myElements.Add( value[i] );
                }
            }
        }
        public readonly int Client;
        public int ID;
        public int Serial;
        public int X;
        public int Y;

        private List<GumpElement> myElements;
        private List<string> myStrings;

        private bool myClosable;
        public bool Closable
        {
            get { return myClosable; }
            set { myClosable = value; }
        }

        private bool myResizable;
        public bool Resizable
        {
            get { return myResizable; }
            set { myResizable = value; }
        }

        private bool myDisposable;
        public bool Disposable
        {
            get { return myDisposable; }
            set { myDisposable = value; }
        }

        private bool myMovable;
        public bool Movable
        {
            get { return myMovable; }
            set { myMovable = value; }
        }

        public Gump( int client, int x, int y, int ID, int serial, string layout, string[] strings, GumpElement[] elements, GumpPage[] pages )
        {
            this.Client = client;
            this.X = x;
            this.Y = y;
            this.ID = ID;
            this.Serial = serial;
            this.Layout = layout;
            this.Strings = strings;
            this.GumpElements = elements;
            this.Pages = pages;
            foreach (GumpPage gp in pages)
            {
                gp.ParentGump = this;
            }
        }

        /// <summary>
        /// Get array of GumpElements which match the specified ElementType from all pages.
        /// </summary>
        public GumpElement[] GetElementsByType( ElementType type )
        {
            List<GumpElement> elementList = new List<GumpElement>();
            if (this.GumpElements != null)
            {
                foreach (GumpElement ge in this.GumpElements)
                {
                    if (ge.Type == type)
                    {
                        elementList.Add( ge );
                    }
                }
            }

            if (this.Pages != null)
            {
                foreach (GumpPage p in this.Pages)
                {
                    foreach (GumpElement ge in p.GumpElements)
                    {
                        if (ge.Type == type)
                        {
                            elementList.Add( ge );
                        }
                    }
                }
            }
            return elementList.ToArray();
        }

        /// <summary>
        /// Get the GumpElement with the specified ID.  Searches all pages/elements.
        /// </summary>
        /// <returns>True on success.</returns>
        public bool GetElementByID( int ID, out GumpElement element )
        {
            if (this.GumpElements != null)
            {
                foreach (GumpElement ge in this.GumpElements)
                {
                    if (ge.ElementID == ID)
                    {
                        element = ge;
                        return true;
                    }
                }
            }

            if (this.Pages != null)
            {
                foreach (GumpPage p in this.Pages)
                {
                    foreach (GumpElement ge in p.GumpElements)
                    {
                        if (ge.ElementID == ID)
                        {
                            element = ge;
                            return true;
                        }
                    }
                }
            }

            element = null;
            return false;
        }

        /// <summary>
        /// Get the GumpElement nearest to the specified GumpElement.
        /// </summary>
        /// <returns>True on success.</returns>
        public bool GetNearestElement( GumpElement source, out GumpElement element )
        {
            GumpElement nearest = null;
            double closest = 0, distance;
            if (source.ParentPage != null)
            {
                return source.ParentPage.GetNearestElement( source, out element );
            }

            foreach (GumpElement ge in this.GumpElements)
            {
                if (ge == source) continue;
                distance = UOMath.Distance( source.X, source.Y, ge.X, ge.Y );
                if (nearest == null)
                {
                    closest = distance;
                    nearest = ge;
                }
                else
                {
                    if (distance < closest)
                    {
                        closest = distance;
                        nearest = ge;
                    }
                }
            }
            element = nearest;
            return nearest != null;
        }

        /// <summary>
        /// Get nearest GumpElement to source, but only if it's ElementType is contained in the include list.
        /// </summary>
        /// <param name="source">Source GumpElement</param>
        /// <param name="includeTypes">Array of ElementTypes which specifies valid GumpElements to search.</param>
        /// <param name="element">GumpElement (out).</param>
        /// <returns>True on success.</returns>
        public bool GetNearestElement( GumpElement source, ElementType[] includeTypes, out GumpElement element )
        {
            GumpElement nearest = null;
            double closest = 0, distance;
            if (source.ParentPage != null)
            {
                return source.ParentPage.GetNearestElement( source, includeTypes, out element );
            }
            bool found;
            foreach (GumpElement ge in this.GumpElements)
            {
                if (ge == source) continue;
                found = false;
                foreach (ElementType et in includeTypes)
                {
                    if (ge.Type == et)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) continue;
                distance = UOMath.Distance( source.X, source.Y, ge.X, ge.Y );
                if (nearest == null)
                {
                    closest = distance;
                    nearest = ge;
                }
                else
                {
                    if (distance < closest)
                    {
                        closest = distance;
                        nearest = ge;
                    }
                }
            }
            element = nearest;
            return nearest != null;
        }

        public void Close()
        {
            Macros.MacroEx.CloseClientGump( this.Client, this.ID );
            Macros.MacroEx.GumpButtonClick( this.Client, this.Serial, this.ID, 0 );
        }

        public override int GetHashCode()
        {
            return (int) CRC32.GetHash( (uint) this.ID, this.Layout );
        }

        // NEW

        public Gump( int x, int y, int serial, int ID )
        {
            this.X = x;
            this.Y = y;
            this.Serial = serial;
            this.ID = ID;
            myElements = new List<GumpElement>();
            myStrings = new List<string>();
        }

        public void Add( GumpElement e )
        {
            e.ParentGump = this;
            myElements.Add( e );
        }

        public void AddPage( int page )
        {
            GumpElement ge = new GumpElement();
            ge.Type = ElementType.page;
            ge.PageNumber = page;
            Add( ge );
        }

        public void AddAlphaRegion( int x, int y, int width, int height )
        {
            GumpElement ge = new GumpElement();
            ge.Type = ElementType.checkertrans;
            ge.X = x;
            ge.Y = y;
            ge.Width = width;
            ge.Height = height;
            Add( ge );
        }

        public void AddBackground( int x, int y, int width, int height, int gumpID )
        {
            GumpElement ge = new GumpElement();
            ge.Type = ElementType.resizepic;
            ge.X = x;
            ge.Y = y;
            ge.Width = width;
            ge.Height = height;
            ge.ElementID = gumpID;
            Add( ge );
        }

        public void AddButton( int x, int y, int normalID, int pressedID, int buttonID, GumpButtonType type, int param )
        {
            GumpElement ge = new GumpElement();
            ge.Type = ElementType.button;
            ge.X = x;
            ge.Y = y;
            ge.InactiveID = normalID;
            ge.ActiveID = pressedID;
            ge.ButtonType = (int) type;
            ge.ElementID = buttonID;
            ge.Param = param;
            Add( ge );
        }

        public void AddCheck( int x, int y, int inactiveID, int activeID, bool initialState, int switchID )
        {
            GumpElement ge = new GumpElement();
            ge.Type = ElementType.checkbox;
            ge.X = x;
            ge.Y = y;
            ge.InactiveID = inactiveID;
            ge.ActiveID = activeID;
            ge.InitialState = initialState;
            ge.ElementID = switchID;
            Add( ge );
        }

        public void AddGroup( int group )
        {
            GumpElement ge = new GumpElement();
            ge.Type = ElementType.group;
            ge.Group = group;
            Add( ge );
        }

        public void AddTooltip( int number )
        {
            GumpElement ge = new GumpElement();
            ge.Type = ElementType.tooltip;
            ge.Cliloc = number;
            Add( ge );
        }

        public void AddHtml( int x, int y, int width, int height, string text, bool background, bool scrollbar )
        {
            GumpElement ge = new GumpElement();
            ge.Type = ElementType.htmlgump;
            ge.X = x;
            ge.Y = y;
            ge.Width = width;
            ge.Height = height;
            ge.Text = text;
            ge.ScrollBar = scrollbar;
            ge.Background = background;
            Add( ge );
        }

        public void AddHtmlLocalized( int x, int y, int width, int height, int number, bool background, bool scrollbar )
        {
            GumpElement ge = new GumpElement();
            ge.Type = ElementType.xmfhtmlgump;
            ge.X = x;
            ge.Y = y;
            ge.Width = width;
            ge.Height = height;
            ge.Cliloc = number;
            ge.Background = background;
            ge.ScrollBar = scrollbar;
            Add( ge );
        }

        public void AddHtmlLocalized( int x, int y, int width, int height, int number, int color, bool background, bool scrollbar )
        {
            GumpElement ge = new GumpElement();
            ge.Type = ElementType.xmfhtmlgumpcolor;
            ge.X = x;
            ge.Y = y;
            ge.Width = width;
            ge.Height = height;
            ge.Cliloc = number;
            ge.Hue = color;
            ge.Background = background;
            ge.ScrollBar = scrollbar;
            Add( ge );
        }

        public void AddHtmlLocalized( int x, int y, int width, int height, int number, string args, int color, bool background, bool scrollbar )
        {
            GumpElement ge = new GumpElement();
            ge.Type = ElementType.xmfhtmltok;
            ge.X = x;
            ge.Y = y;
            ge.Width = width;
            ge.Height = height;
            ge.Cliloc = number;
            ge.Args = args;
            ge.Hue = color;
            ge.Background = background;
            ge.ScrollBar = scrollbar;
            Add( ge );
        }

        public void AddImage( int x, int y, int gumpID )
        {
            AddImage( x, y, gumpID, 0 );
        }

        public void AddImage( int x, int y, int gumpID, int hue )
        {
            GumpElement ge = new GumpElement();
            ge.Type = ElementType.gumppic;
            ge.X = x;
            ge.Y = y;
            ge.ElementID = gumpID;
            ge.Hue = hue;
            Add( ge );
        }

        public void AddImageTiled( int x, int y, int width, int height, int gumpID )
        {
            GumpElement ge = new GumpElement();
            ge.Type = ElementType.gumppictiled;
            ge.X = x;
            ge.Y = y;
            ge.Width = width;
            ge.Height = height;
            ge.ElementID = gumpID;
            Add( ge );
        }

        public void AddImageTiledButton( int x, int y, int normalID, int pressedID, int buttonID, GumpButtonType type, int param, int itemID, int hue, int width, int height )
        {
            AddImageTiledButton( x, y, normalID, pressedID, buttonID, type, param, itemID, hue, width, height, -1 );
        }

        public void AddImageTiledButton( int x, int y, int normalID, int pressedID, int buttonID, GumpButtonType type, int param, int itemID, int hue, int width, int height, int localizedTooltip )
        {
            GumpElement ge = new GumpElement();
            ge.Type = ElementType.buttontileart;
            ge.X = x;
            ge.Y = y;
            ge.InactiveID = normalID;
            ge.ActiveID = pressedID;
            ge.ElementID = buttonID;
            ge.ButtonType = (int) type;
            ge.Param = param;
            ge.ItemID = itemID;
            ge.Hue = hue;
            ge.Height = height;
            ge.Width = width;
            ge.Cliloc = localizedTooltip;
            Add( ge );
        }

        public void AddItem( int x, int y, int itemID )
        {
            GumpElement ge = new GumpElement();
            ge.Type = ElementType.tilepic;
            ge.X = x;
            ge.Y = y;
            ge.ItemID = itemID;
            Add( ge );
        }

        public void AddItem( int x, int y, int itemID, int hue )
        {
            GumpElement ge = new GumpElement();
            ge.Type = ElementType.tilepic;
            ge.X = x;
            ge.Y = y;
            ge.ItemID = itemID;
            ge.Hue = hue;
            Add( ge );
        }

        public void AddLabel( int x, int y, int hue, string text )
        {
            GumpElement ge = new GumpElement();
            ge.Type = ElementType.text;
            ge.X = x;
            ge.Y = y;
            ge.Hue = hue;
            ge.Text = text;
            Add( ge );
        }

        public void AddLabelCropped( int x, int y, int width, int height, int hue, string text )
        {
            GumpElement ge = new GumpElement();
            ge.Type = ElementType.croppedtext;
            ge.X = x;
            ge.Y = y;
            ge.Width = width;
            ge.Height = height;
            ge.Hue = hue;
            ge.Text = text;
            Add( ge );
        }

        public void AddRadio( int x, int y, int inactiveID, int activeID, bool initialState, int switchID )
        {
            GumpElement ge = new GumpElement();
            ge.Type = ElementType.radio;
            ge.X = x;
            ge.Y = y;
            ge.InactiveID = inactiveID;
            ge.ActiveID = activeID;
            ge.InitialState = initialState;
            ge.ElementID = switchID;
            Add( ge );
        }

        public void AddTextEntry( int x, int y, int width, int height, int hue, int entryID, string initialText )
        {
            GumpElement ge = new GumpElement();
            ge.Type = ElementType.textentry;
            ge.X = x;
            ge.Y = y;
            ge.Width = width;
            ge.Height = height;
            ge.Hue = hue;
            ge.ElementID = entryID;
            ge.Text = initialText;
            Add( ge );
        }

        public void AddTextEntry( int x, int y, int width, int height, int hue, int entryID, string initialText, int size )
        {
            GumpElement ge = new GumpElement();
            ge.Type = ElementType.textentrylimited;
            ge.X = x;
            ge.Y = y;
            ge.Width = width;
            ge.Height = height;
            ge.Hue = hue;
            ge.ElementID = entryID;
            ge.Text = initialText;
            ge.Size = size;
            Add( ge );
        }

        public void AddItemProperty( int serial )
        {
            GumpElement ge = new GumpElement();
            ge.Type = ElementType.itemproperty;
            ge.Serial = serial;
            Add( ge );
        }

        public int Intern( string value )
        {
            int indexOf = myStrings.IndexOf( value );

            if (indexOf >= 0)
            {
                return indexOf;
            }
            else
            {
                myStrings.Add( value );
                return myStrings.Count - 1;
            }
        }

        internal static byte[] StringToBuffer( string str )
        {
            return Encoding.ASCII.GetBytes( str );
        }

        private static byte[] m_BeginLayout = StringToBuffer( "{ " );
        private static byte[] m_EndLayout = StringToBuffer( " }" );

        private static byte[] m_NoMove = StringToBuffer( "{ nomove }" );
        private static byte[] m_NoClose = StringToBuffer( "{ noclose }" );
        private static byte[] m_NoDispose = StringToBuffer( "{ nodispose }" );
        private static byte[] m_NoResize = StringToBuffer( "{ noresize }" );

        private static byte[] m_True = Gump.StringToBuffer( " 1" );
        private static byte[] m_False = Gump.StringToBuffer( " 0" );

        private static byte[] m_BeginTextSeparator = Gump.StringToBuffer( " @" );
        private static byte[] m_EndTextSeparator = Gump.StringToBuffer( "@" );

        public byte[] Compile()
        {
            IGumpWriter disp;

            disp = new GumpWriter( this );

            if (!myMovable)
                disp.AppendLayout( m_NoMove );

            if (!myClosable)
                disp.AppendLayout( m_NoClose );

            if (!myDisposable)
                disp.AppendLayout( m_NoDispose );

            if (!myResizable)
                disp.AppendLayout( m_NoResize );

            int count = GumpElements.Length;
            GumpElement e;

            for (int i = 0; i < count; ++i)
            {
                e = GumpElements[i];

                disp.AppendLayout( m_BeginLayout );
                e.AppendTo( disp );
                disp.AppendLayout( m_EndLayout );
            }

            List<string> strings = new List<string>();
            if (Strings != null)
            {
                for (int i = 0; i < Strings.Length; i++)
                {
                    strings.Add( Strings[i] );
                }
            }

            disp.WriteStrings( strings );

            disp.Flush();

            return disp.ToArray();
        }

        internal interface IGumpWriter
        {
            int TextEntries { get; set; }
            int Switches { get; set; }

            void AppendLayout( bool val );
            void AppendLayout( int val );
            void AppendLayoutNS( int val );
            void AppendLayout( string text );
            void AppendLayout( byte[] buffer );
            void WriteStrings( List<string> strings );
            void Flush();
            byte[] ToArray();
        }

        internal class GumpWriter : IGumpWriter
        {
            private byte[] m_Buffer = new byte[48];

            private int m_Switches;
            public int Switches
            {
                get
                {
                    return m_Switches;
                }

                set
                {
                    m_Switches = value;
                }
            }

            private int m_TextEntries;
            public int TextEntries
            {
                get
                {
                    return m_TextEntries;
                }

                set
                {
                    m_TextEntries = value;
                }
            }

            private int m_LayoutLength;
            private int m_StringsLength;
            private int m_PacketLength;

            private PacketWriter m_Stream;

            public GumpWriter(Gump g)
            {
                m_Stream = new PacketWriter( 4096 );
                m_Buffer[0] = (byte) ' ';
                m_Stream.Write( (byte) 0xB0 );
                m_Stream.Write( (short) 0 );
                m_Stream.Write( (int) g.Serial );
                m_Stream.Write( (int) g.ID );
                m_Stream.Write( (int) g.X );
                m_Stream.Write( (int) g.Y );
                m_Stream.Write( (ushort) 0xFFFF );
            }

            public void AppendLayout( byte[] buffer )
            {
                int length = buffer.Length;
                m_Stream.Write( buffer, 0, length );
                m_LayoutLength += length;
            }

            public void AppendLayout( string text )
            {
                AppendLayout( m_BeginTextSeparator );

                int length = text.Length;
                m_Stream.WriteAsciiFixed( text, length );
                m_LayoutLength += length;

                AppendLayout( m_EndTextSeparator );
            }

            public void AppendLayout( int val )
            {
                string toString = val.ToString();
                int bytes = System.Text.Encoding.ASCII.GetBytes( toString, 0, toString.Length, m_Buffer, 1 ) + 1;

                m_Stream.Write( m_Buffer, 0, bytes );
                m_LayoutLength += bytes;
            }

            public void AppendLayout( bool val )
            {
                AppendLayout( val ? m_True : m_False );
            }

            public void AppendLayoutNS( int val )
            {
                string toString = val.ToString();
                int bytes = System.Text.Encoding.ASCII.GetBytes( toString, 0, toString.Length, m_Buffer, 1 );

                m_Stream.Write( m_Buffer, 1, bytes );
                m_LayoutLength += bytes;
            }

            public void Flush()
            {
                int length = 23 + ( m_LayoutLength + m_StringsLength );
                m_Stream.Seek( 1, SeekOrigin.Begin );
                m_Stream.Write( (short) length );
                m_PacketLength = length;
            }

            public void WriteStrings( List<string> text )
            {
                m_Stream.Seek( 19, SeekOrigin.Begin );
                m_Stream.Write( (ushort) m_LayoutLength );
                m_Stream.Seek( 0, SeekOrigin.End );

                m_Stream.Write( (ushort) text.Count );

                for (int i = 0; i < text.Count; ++i)
                {
                    string v = text[i];

                    if (v == null)
                        v = String.Empty;

                    int length = (ushort) v.Length;
                    m_StringsLength += (length*2)+2;

                    m_Stream.Write( (ushort) length );
                    m_Stream.WriteBigUniFixed( v, length );
                }
            }

            public byte[] ToArray()
            {
                byte[] packet = new byte[m_PacketLength];
                Buffer.BlockCopy( m_Stream.ToArray(), 0, packet, 0, m_PacketLength );
                return packet;
            }
        }
    }
}