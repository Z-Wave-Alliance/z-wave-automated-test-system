// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using ZWave.BasicApplication.Devices;

namespace Zats.Services.SampleApplication.SiliconLabs.IOProviders
{
    class WallController : SampleApplicationIOBase, IWallControllerProvider
    {
        public WallController(DeviceHostClient deviceHostClient)
            : base(deviceHostClient)
        {
        }


        public void Key01()
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

        public void Key02()
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

        public void HoldKey02(byte duration)
        {
            if (_IsModernSeries)
            {
                IOHandler.HoldButton(0x03, duration);
            }
            else
            {
                IOHandler.HoldButton(0x04, duration);
            }
        }

        public void Key03()
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

        //HoldButton
        public void HoldKey03(byte duration)
        {
            if (_IsModernSeries)
            {
                IOHandler.HoldButton(0x04, duration);
            }
            else
            {
                IOHandler.HoldButton(0x06, duration);
            }
        }
    }
}