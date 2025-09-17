// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using ZWave.BasicApplication.Devices;

namespace Zats.Services.SampleApplication.SiliconLabs.IOProviders

{
    class PowerStrip : SampleApplicationIOBase, IPowerStripProvider
    {
        public PowerStrip(DeviceHostClient deviceHostClient)
            : base(deviceHostClient)
        {
        }

        public void DimHold(int duration)
        {
            if (_IsModernSeries)
            {
                IOHandler.HoldButton(0x03, (byte)duration);
            }
            else
            {
                IOHandler.HoldButton(0x04, (byte)duration);
            }
        }

        public void DimOnOff()
        {
            if (_IsModernSeries)
            {
                IOHandler.ToggleButton(0x03);
            }
            else
            {
                IOHandler.ToggleButton(0x04);
            }
        }

        public void ToggleNotify()
        {
            if (_IsModernSeries)
            {
                IOHandler.ToggleButton(0x04);
            }
            else
            {
                IOHandler.ToggleButton(0x06);
            }
        }

        public void OnOff()
        {
            if (_IsModernSeries)
            {
                IOHandler.ToggleButton(0x01);
            }
            else
            {
                IOHandler.ToggleButton(0x02);
            }
        }

        public bool? IsStatusOn()
        {
            return IOHandler.GetLedState(0x01);
        }

        public bool? IsDimStatusOn()
        {
            return !_IsModernSeries ? IOHandler.GetLedState(0x02) : throw new NotSupportedException("Not supported LED interface for 700s series");
        }
    }
}