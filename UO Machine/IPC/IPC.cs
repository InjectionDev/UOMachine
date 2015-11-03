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

using System.Windows.Forms;

namespace UOMachine.IPC
{
    public enum MouseButton : byte
    {
        None,
        Left,
        Middle,
        Right,
        XButton1,
        XButton2
    }

    public enum PacketType: byte
    {
        Client,
        Server
    }

    public enum FunctionType : byte
    {
        Receive,
        Send
    }

    public delegate void dPing(int clientID);
    public delegate void dPingResponse(int clientID);
    public delegate void dException(int clientID, string message);
    public delegate void dMessage(int clientID, string message);
    public delegate void dClientID(int clientID, int PID);
    public delegate void dClientVersion(int clientID, int version);
    public delegate void dFunctionPointer(int clientID, uint address, FunctionType functionType);
    public delegate void dIncomingPacket(int clientID, byte[] data);
    public delegate void dOutgoingPacket(int clientID, byte[] data);
    public delegate void dMouseMove(int clientID, int x, int y);
    public delegate void dMouseDown(int clientID, int x, int y, MouseButtons button);
    public delegate void dMouseUp(int clientID, int x, int y, MouseButtons button);
    public delegate void dMouseClick(int clientID, int x, int y, MouseButtons button);
    public delegate void dMouseDblClick(int clientID, int x, int y, MouseButtons button);
    public delegate void dMouseWheel(int clientID, int x, int y, sbyte delta);
    public delegate void dKeyDown(int clientID, Keys key);
    public delegate void dKeyUp(int clientID, Keys key);
    public delegate void dSendPacket(int caveAddress, PacketType packetType, byte[] data);
    public delegate void dAddSendFilter(byte packetID, PacketFilterCondition[] conditions);
    public delegate void dAddRecvFilter(byte packetID, PacketFilterCondition[] conditions);
    public delegate void dRemoveSendFilter(byte packetID);
    public delegate void dRemoveRecvFilter(byte packetID);
    public delegate void dClearSendFilter();
    public delegate void dClearRecvFilter();
    public delegate void dInstallSendHook();
    public delegate void dInstallRecvHook();
    public delegate void dUninstallSendHook();
    public delegate void dUninstallRecvHook();
    public delegate void dAddGumpResponseFilter( uint serial, uint gumpid );
    //public delegate void dRemoveGumpResponseFilter( int serial, int gumpid );
    //public delegate void dClearGumpResponseFilter();

    public enum Command : byte
    {
        ClearRecvFilter,     //sent by server           size:  1 byte
        ClearSendFilter,     //sent by server           size:  1 byte
        InstallRecvHook,     //sent by server           size:  1 byte
        InstallSendHook,     //sent by server           size:  1 byte
        Ping,                //sent by client/server    size:  1 byte
        PingResponse,        //sent by client/server    size:  1 byte
        UninstallRecvHook,   //sent by server           size:  1 byte
        UninstallSendHook,   //sent by server           size:  1 byte
 
        AddRecvFilter,       //sent by server           size:  2 bytes
        AddSendFilter,       //sent by server           size:  2 bytes
        RemoveRecvFilter,    //sent by server           size:  2 bytes
        RemoveSendFilter,    //sent by server           size:  2 bytes

        ClientID,            //sent by client           size:  5 bytes
        ClientVersion,       //sent by client           size:  5 bytes
        KeyDown,             //sent by client           size:  5 bytes
        KeyUp,               //sent by client           size:  5 bytes

        FunctionPointer,     //sent by client           size:  6 bytes

        MouseMove,           //sent by client           size:  9 bytes

        MouseWheel,          //sent by client           size: 10 bytes
        MouseDblClick,       //sent by client           size: 10 bytes
        MouseClick,          //sent by client           size: 10 bytes
        MouseDown,           //sent by client           size: 10 bytes
        MouseUp,             //sent by client           size: 10 bytes

        AddGumpResponseFilter, //sent by server           size: 13 bytes
        // See Data.cs, > 23 is hard coded for variable length.
        CallGumpFunction,    //sent by server           size: 13 bytes

        Exception,           //sent by client           size: variable
        Message,             //sent by client           size: variable
        IncomingPacket,      //sent by client           size: variable
        OutgoingPacket,      //sent by client           size: variable
        SendPacket,          //sent by server           size: variable
        AddRecvFilterConditional,
        AddSendFilterConditional,

        //RemoveGumpResponseFilter,
        //ClearGumpResponseFilter

        // variable size command format: byte command, ushort length, byte[] data
    }

}