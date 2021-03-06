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
        public static void Cast(int client, Necromancy necromancy)
        {
            switch (necromancy)
            {
                case Necromancy.Animate_Dead:
                    Event(client, 15, 0x65);
                    break;
                case Necromancy.Blood_Oath:
                    Event(client, 15, 0x66);
                    break;
                case Necromancy.Corpse_Skin:
                    Event(client, 15, 0x67);
                    break;
                case Necromancy.Curse_Weapon:
                    Event(client, 15, 0x68);
                    break;
                case Necromancy.Evil_Omen:
                    Event(client, 15, 0x69);
                    break;
                case Necromancy.Horrific_Beast:
                    Event(client, 15, 0x6A);
                    break;
                case Necromancy.Lich_Form:
                    Event(client, 15, 0x6B);
                    break;
                case Necromancy.Mind_Rot:
                    Event(client, 15, 0x6C);
                    break;
                case Necromancy.Pain_Spike:
                    Event(client, 15, 0x6D);
                    break;
                case Necromancy.Poison_Strike:
                    Event(client, 15, 0x6E);
                    break;
                case Necromancy.Strangle:
                    Event(client, 15, 0x6F);
                    break;
                case Necromancy.Summon_Familiar:
                    Event(client, 15, 0x70);
                    break;
                case Necromancy.Vampiric_Embrace:
                    Event(client, 15, 0x71);
                    break;
                case Necromancy.Vengeful_Spirit:
                    Event(client, 15, 0x72);
                    break;
                case Necromancy.Wither:
                    Event(client, 15, 0x73);
                    break;
                case Necromancy.Wraith_Form:
                    Event(client, 15, 0x74);
                    break;
                case Necromancy.Exorcism:
                    Event(client, 15, 0x75);
                    break;
            }
        }
    }
}