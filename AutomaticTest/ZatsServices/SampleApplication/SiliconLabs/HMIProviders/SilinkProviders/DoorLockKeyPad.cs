// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using ZWave.TextApplication;

namespace Zats.Services.SampleApplication.SiliconLabs.SilinkProviders
{
    class DoorLockKeyPad : SampleApplicationSilinkBase, IDoorLookKeypadProvider
    {
        public DoorLockKeyPad(TextDevice devBoardDevice) : base(devBoardDevice)
        { }


        public void ToggleDoorHandle()
        {
            _silinkAdminIOHandler.HoldButton((byte)SilinkButtons.BTN_0, 2);
        }

        public void ToggleDoorHandle(byte duration)
        {
            _silinkAdminIOHandler.HoldButton((byte)SilinkButtons.BTN_0, duration);
        }

        public void ToggleKeyPad()
        {
            _silinkAdminIOHandler.ToggleButton((byte)SilinkButtons.BTN_0);
        }

        /// <summary>
        /// Medium click on the WSTK BTN 0 button to simulate battery report.
        /// </summary>
        public void BatteryReport()
        {
            _silinkAdminIOHandler.HoldButton((byte)SilinkButtons.BTN_0, 0);
        }

        public bool? IsLatchOpen()
        {
            throw new NotImplementedException();
        }

        public bool? IsBoltLocked()
        {
            throw new NotImplementedException();
        }
    }
}