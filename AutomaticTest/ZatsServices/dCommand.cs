// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>

namespace Zats.Services
{
    public struct dCommand
    {
        public string Command;
        public dCommand(string command)
        {
            Command = command;
        }
    }

    public static class D_CMD
    {
        public static dCommand Empty = new dCommand("");
        public static dCommand Help = new dCommand("help");
        public static dCommand Reset = new dCommand("reset");
        public static dCommand LearnModeStart = new dCommand("lms");
        public static dCommand LearnModeEnd = new dCommand("lme");
        public static dCommand OTA_StartUpdate = new dCommand("otas");
        public static dCommand WatchdogReset = new dCommand("wdr");
        public static dCommand applicationState = new dCommand("ast");
        public static dCommand getPortPin = new dCommand("getpin");
        public static dCommand ZW = new dCommand("ZW");
        public static dCommand Key1Down = new dCommand("k1d");
        public static dCommand Key1Up = new dCommand("k1u");
        public static dCommand Key1Press = new dCommand("k1p");
        public static dCommand Key1Hold = new dCommand("k1h");
        public static dCommand Key1TripplePress = new dCommand("k1t");
        public static dCommand Key1Hold10Sec = new dCommand("k1h10");
        public static dCommand Key2Down = new dCommand("k2d");
        public static dCommand Key2Up = new dCommand("k2u");
        public static dCommand Key2Press = new dCommand("k2p");
        public static dCommand Key2Hold = new dCommand("k2h");
        public static dCommand Key2TripplePress = new dCommand("k2t");
        public static dCommand Key2Hold10Sec = new dCommand("k2h10");
        public static dCommand Key3Down = new dCommand("k3d");
        public static dCommand Key3Up = new dCommand("k3u");
        public static dCommand Key3Press = new dCommand("k3p");
        public static dCommand Key3Hold = new dCommand("k3h");
        public static dCommand Key3TripplePress = new dCommand("k3t");
        public static dCommand Key3Hold10Sec = new dCommand("k3h10");
        public static dCommand Key4Down = new dCommand("k4d");
        public static dCommand Key4Up = new dCommand("k4u");
        public static dCommand Key4Press = new dCommand("k4p");
        public static dCommand Key4Hold = new dCommand("k4h");
        public static dCommand Key4TripplePress = new dCommand("k4t");
        public static dCommand Key4Hold10Sec = new dCommand("k4h10");
        public static dCommand Key5Down = new dCommand("k5d");
        public static dCommand Key5Up = new dCommand("k5u");
        public static dCommand Key5Press = new dCommand("k5p");
        public static dCommand Key5Hold = new dCommand("k5h");
        public static dCommand Key5TripplePress = new dCommand("k5t");
        public static dCommand Key5Hold10Sec = new dCommand("k5h10");
        public static dCommand Key6Down = new dCommand("k6d");
        public static dCommand Key6Up = new dCommand("k6u");
        public static dCommand Key6Press = new dCommand("k6p");
        public static dCommand Key6Hold = new dCommand("k6h");
        public static dCommand Key6TripplePress = new dCommand("k6t");
        public static dCommand Key6Hold10Sec = new dCommand("k6h10");
        public static dCommand Csa = new dCommand("csa");
        public static dCommand CsaStaticDoorLock = new dCommand("csa 3408061449");
        public static dCommand UserCode = new dCommand("c 0000");
    }
}
