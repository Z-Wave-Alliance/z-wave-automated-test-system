// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using ZWave.BasicApplication.Devices;

namespace Zats.Services.SampleApplication.SiliconLabs.IOProviders
{
    class SwitchOnOff : SampleApplicationIOBase, ISwitchOnOffProvider
    {
        public SwitchOnOff(DeviceHostClient deviceHostClient)
            : base(deviceHostClient)
        {
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

        public void HoldKey01(byte duration)
        {
            if (_IsModernSeries)
            {
                IOHandler.HoldButton(0x01, duration);
            }
            else
            {
                IOHandler.HoldButton(0x02, duration);
            }
        }

        public bool? IsStatusOn()
        {
            return _IsModernSeries ? !IOHandler.GetLedState(0x01) : IOHandler.GetLedState(0x02); // Reverse result for series1 and newer (!) Led off - no bright
        }
    }
}