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


namespace UOMachine
{
    public class GumpElement
    {
        private Gump myParentGump;
        public Gump ParentGump
        {
            get
            {
                if (myParentGump != null)
                    return myParentGump;
                else
                {
                    if (myParentPage != null)
                        return myParentPage.ParentGump;
                }
                return null;
            }
            set { myParentGump = value; }
        }

        private GumpPage myParentPage;
        public GumpPage ParentPage
        {
            get { return myParentPage; }
            set { myParentPage = value; }
        }

        private ElementType myType;
        public ElementType Type
        {
            get { return myType; }
            set { myType = value; }
        }

        private int myX;
        public int X
        {
            get { return myX; }
            set { myX = value; }
        }

        private int myY;
        public int Y
        {
            get { return myY; }
            set { myY = value; }
        }

        private int myElementID;
        public int ElementID
        {
            get { return myElementID; }
            set { myElementID = value; }
        }

        private int myInactiveID;
        public int InactiveID
        {
            get { return myInactiveID; }
            set { myInactiveID = value; }
        }

        private int myActiveID;
        public int ActiveID
        {
            get { return myActiveID; }
            set { myActiveID = value; }
        }

        private int myGroup;
        public int Group
        {
            get { return myGroup; }
            set { myGroup = value; }
        }

        private bool myInitialState;
        public bool InitialState
        {
            get { return myInitialState; }
            set { myInitialState = value; }
        }

        private int myButtonType;
        public int ButtonType
        {
            get { return myButtonType; }
            set { myButtonType = value; }
        }

        private int myParam;
        public int Param
        {
            get { return myParam; }
            set { myParam = value; }
        }

        private int myItemID;
        public int ItemID
        {
            get { return myItemID; }
            set { myItemID = value; }
        }

        private int myTooltip;
        public int Tooltip
        {
            get { return myTooltip; }
            set { myTooltip = value; }
        }

        private int myHue;
        public int Hue
        {
            get { return myHue; }
            set { myHue = value; }
        }

        private int myWidth;
        public int Width
        {
            get { return myWidth; }
            set { myWidth = value; }
        }

        private int myHeight;
        public int Height
        {
            get { return myHeight; }
            set { myHeight = value; }
        }

        private int mySize;
        public int Size
        {
            get { return mySize; }
            set { mySize = value; }
        }

        private int myCliloc;
        public int Cliloc
        {
            get { return myCliloc; }
            set { myCliloc = value; }
        }

        private bool myBackground;
        public bool Background
        {
            get { return myBackground; }
            set { myBackground = value; }
        }

        private bool myScrollBar;
        public bool ScrollBar
        {
            get { return myScrollBar; }
            set { myScrollBar = value; }
        }

        private string myText;
        public string Text
        {
            get { return myText; }
            set { myText = value; }
        }

        private string myArgs;
        public string Args
        {
            get { return myArgs; }
            set { myArgs = value; }
        }

        private int mySerial;
        public int Serial
        {
            get { return mySerial; }
            set { mySerial = value; }
        }

        private int myPageNumber;
        public int PageNumber
        {
            get { return myPageNumber; }
            set { myPageNumber = value; }
        }

        /// <summary>
        /// Get nearest GumpElement.
        /// </summary>
        /// <returns>True on success.</returns>
        public bool GetNearestElement(out GumpElement element)
        {
            if (myParentPage != null)
            {
                return myParentPage.GetNearestElement(this, out element);
            }
            element = null;
            return false;
        }

        /// <summary>
        /// Get nearest GumpElement, but only if it's ElementType is contained in the include list.
        /// </summary>
        /// <param name="includeTypes">Array of ElementTypes which specifies valid GumpElements to search.</param>
        /// <param name="element">GumpElement (out).</param>
        /// <returns>True on success.</returns>
        public bool GetNearestElement(ElementType[] includeTypes, out GumpElement element)
        {
            if (myParentPage != null)
            {
                return myParentPage.GetNearestElement(this, includeTypes, out element);
            }
            element = null;
            return false;
        }

        public void Click()
        {
            Gump g = this.ParentGump;
            if (g != null && g.ID != 461 && myType == ElementType.button)
            {
                Macros.MacroEx.CloseClientGump(g.Client, g.ID);
                Macros.MacroEx.GumpButtonClick(g.Client, g.Serial, g.ID, this.ElementID);
            }
        }

        internal void AppendTo(Gump.IGumpWriter disp)
        {
            switch (Type)
            {
                case ElementType.textentrylimited:
                    {
                        disp.AppendLayout( Gump.StringToBuffer( "textentrylimited" ) );
                        disp.AppendLayout( myX );
                        disp.AppendLayout( myY );
                        disp.AppendLayout( myWidth );
                        disp.AppendLayout( myHeight );
                        disp.AppendLayout( myHue );
                        disp.AppendLayout( myElementID );
                        disp.AppendLayout( ParentGump.Intern( myText ) );
                        disp.AppendLayout( mySize );
                        disp.TextEntries++;
                        break;
                    }
                case ElementType.textentry:
                    {
                        disp.AppendLayout( Gump.StringToBuffer( "textentry" ) );
                        disp.AppendLayout( myX );
                        disp.AppendLayout( myY );
                        disp.AppendLayout( myWidth );
                        disp.AppendLayout( myHeight );
                        disp.AppendLayout( myHue );
                        disp.AppendLayout( myElementID );
                        disp.AppendLayout( ParentGump.Intern( myText ) );
                        disp.TextEntries++;
                        break;
                    }
                case ElementType.radio:
                    {
                        disp.AppendLayout( Gump.StringToBuffer( "radio" ) );
                        disp.AppendLayout( myX );
                        disp.AppendLayout( myY );
                        disp.AppendLayout( myInactiveID );
                        disp.AppendLayout( myActiveID );
                        disp.AppendLayout( myInitialState );
                        disp.AppendLayout( myElementID );
                        disp.Switches++;
                        break;
                    }
                case ElementType.croppedtext:
                    {
                        disp.AppendLayout( Gump.StringToBuffer( "croppedtext" ) );
                        disp.AppendLayout( myX );
                        disp.AppendLayout( myY );
                        disp.AppendLayout( myWidth );
                        disp.AppendLayout( myHeight );
                        disp.AppendLayout( myHue );
                        disp.AppendLayout( ParentGump.Intern( myText ) );
                        break;
                    }
                case ElementType.buttontileart:
                    {
                        disp.AppendLayout( Gump.StringToBuffer("buttontileart") );
                        disp.AppendLayout( myX );
                        disp.AppendLayout( myY );
                        disp.AppendLayout( myInactiveID );
                        disp.AppendLayout( myActiveID );
                        disp.AppendLayout( myButtonType );
                        disp.AppendLayout( myParam );
                        disp.AppendLayout( myElementID );
                        disp.AppendLayout( myItemID );
                        disp.AppendLayout( myHue );
                        disp.AppendLayout( myWidth );
                        disp.AppendLayout( myHeight );

                        if (myCliloc != -1)
                        {
                            disp.AppendLayout( Gump.StringToBuffer( " }{ tooltip" ));
                            disp.AppendLayout( myCliloc );
                        }
                        break;
                    }
                case ElementType.tilepic:
                case ElementType.tilepichue:
                    {
                        disp.AppendLayout( Gump.StringToBuffer( myHue == 0 ? "tilepic" : "tilepichue" ) );
                        disp.AppendLayout( myX );
                        disp.AppendLayout( myY );
                        disp.AppendLayout( myItemID );

                        if (myHue != 0)
                            disp.AppendLayout( myHue );
                        break;
                    }
                case ElementType.itemproperty:
                    {
                        disp.AppendLayout( Gump.StringToBuffer( "itemproperty" ) );
                        disp.AppendLayout( mySerial );
                        break;
                    }
                case ElementType.gumppictiled:
                    {
                        disp.AppendLayout( Gump.StringToBuffer( "gumppictiled" ) );
                        disp.AppendLayout( myX );
                        disp.AppendLayout( myY );
                        disp.AppendLayout( myWidth );
                        disp.AppendLayout( myHeight );
                        disp.AppendLayout( myElementID );
                        break;
                    }
                case ElementType.gumppic:
                    {
                        disp.AppendLayout( Gump.StringToBuffer( "gumppic" ) );
                        disp.AppendLayout( myX );
                        disp.AppendLayout( myY );
                        disp.AppendLayout( myElementID );

                        if (myHue != 0)
                        {
                            disp.AppendLayout( Gump.StringToBuffer( " hue=" ) );
                            disp.AppendLayoutNS( myHue );
                        }
                        break;
                    }
                case ElementType.xmfhtmlgump:
                    {
                        disp.AppendLayout( Gump.StringToBuffer( "xmfhtmlgump" ) );
                        disp.AppendLayout( myX );
                        disp.AppendLayout( myY );
                        disp.AppendLayout( myWidth );
                        disp.AppendLayout( myHeight );
                        disp.AppendLayout( myCliloc );
                        disp.AppendLayout( myBackground );
                        disp.AppendLayout( myScrollBar );
                        break;
                    }
                case ElementType.xmfhtmlgumpcolor:
                    {
                        disp.AppendLayout( Gump.StringToBuffer( "xmfhtmlgumpcolor" ) );
                        disp.AppendLayout( myX );
                        disp.AppendLayout( myY );
                        disp.AppendLayout( myWidth );
                        disp.AppendLayout( myHeight );
                        disp.AppendLayout( myCliloc );
                        disp.AppendLayout( myBackground );
                        disp.AppendLayout( myScrollBar );
                        disp.AppendLayout( myHue );
                        break;
                    }
                case ElementType.xmfhtmltok:
                    {
                        disp.AppendLayout( Gump.StringToBuffer( "xmfhtmltok" ) );
                        disp.AppendLayout( myX );
                        disp.AppendLayout( myY );
                        disp.AppendLayout( myWidth );
                        disp.AppendLayout( myHeight );
                        disp.AppendLayout( myBackground );
                        disp.AppendLayout( myScrollBar );
                        disp.AppendLayout( myHue );
                        disp.AppendLayout( myCliloc );
                        disp.AppendLayout( myArgs );
                        break;
                    }
                case ElementType.htmlgump:
                    {
                        disp.AppendLayout( Gump.StringToBuffer( "htmlgump" ) );
                        disp.AppendLayout( myX );
                        disp.AppendLayout( myY );
                        disp.AppendLayout( myWidth );
                        disp.AppendLayout( myHeight );
                        disp.AppendLayout( ParentGump.Intern( myText ) );
                        disp.AppendLayout( myBackground );
                        disp.AppendLayout( myScrollBar );
                        break;
                    }
                case ElementType.tooltip:
                    {
                        disp.AppendLayout( Gump.StringToBuffer( "tooltip" ) );
                        disp.AppendLayout( myCliloc );
                        break;
                    }
                case ElementType.group:
                    {
                        disp.AppendLayout( Gump.StringToBuffer( "group" ) );
                        disp.AppendLayout( myGroup );
                        break;
                    }
                case ElementType.resizepic:
                    {
                        disp.AppendLayout( Gump.StringToBuffer( "resizepic" ) );
                        disp.AppendLayout( myX );
                        disp.AppendLayout( myY );
                        disp.AppendLayout( myElementID );
                        disp.AppendLayout( myWidth );
                        disp.AppendLayout( myHeight );
                        break;
                    }
                case ElementType.checkertrans:
                    {
                        disp.AppendLayout( Gump.StringToBuffer( "checkertrans" ) );
                        disp.AppendLayout( myX );
                        disp.AppendLayout( myY );
                        disp.AppendLayout( myWidth );
                        disp.AppendLayout( myHeight );
                        break;
                    }
                case ElementType.page:
                    {
                        disp.AppendLayout( Gump.StringToBuffer( "page" ) );
                        disp.AppendLayout( myPageNumber );
                        break;
                    }
                case ElementType.button:
                    {
                        disp.AppendLayout( Gump.StringToBuffer("button") );
                        disp.AppendLayout( myX );
                        disp.AppendLayout( myY );
                        disp.AppendLayout( myInactiveID );
                        disp.AppendLayout( myActiveID );
                        disp.AppendLayout( myButtonType );
                        disp.AppendLayout( myParam );
                        disp.AppendLayout( myElementID );
                        break;
                    }
                case ElementType.text:
                    {
                        disp.AppendLayout( Gump.StringToBuffer( "text" ) );
                        disp.AppendLayout( myX );
                        disp.AppendLayout( myY );
                        disp.AppendLayout( myHue );
                        disp.AppendLayout( ParentGump.Intern( myText ) );
                        break;
                    }
                default:
                    break;
            }
        }
    }
}