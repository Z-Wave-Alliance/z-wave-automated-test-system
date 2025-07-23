// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using ZWave.BasicApplication.Devices;

namespace Zats.Services.SampleApplication.SiliconLabs.IOProviders
{
    class DoorLockKeyPad : SampleApplicationIOBase, IDoorLookKeypadProvider
    {
        public DoorLockKeyPad(DeviceHostClient deviceHostClient)
            : base(deviceHostClient)
        {
        }

        public void ToggleDoorHandle()
        {
            if (_IsModernSeries)
            {
                IOHandler.ToggleButton(0x01);
            }
            else
            {
                IOHandler.ToggleButton(0x04);
            }
        }

        public void ToggleDoorHandle(byte duration)
        {
            if (_IsModernSeries)
            {
                IOHandler.HoldButton(0x01, duration);
            }
            else
            {
                IOHandler.HoldButton(0x04, duration);
            }
        }

        public void ToggleKeyPad()
        {
            if (_IsModernSeries)
            {
                IOHandler.ToggleButton(0x04);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public void BatteryReport()
        {
            if (_IsModernSeries)
            {
                IOHandler.ToggleButton(0x03);
            }
            else
            {
                Reset();
            }
        }

        public bool? IsLatchOpen()
        {
            return !IOHandler.GetLedState(0x01);
        }

        public bool? IsBoltLocked()
        {
            return _IsModernSeries ? !IOHandler.GetLedState(0x04) : !IOHandler.GetLedState(0x02);
        }
    }
}