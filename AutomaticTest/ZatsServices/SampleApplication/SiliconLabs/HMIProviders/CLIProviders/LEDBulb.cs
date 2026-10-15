// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using ZWave.TextApplication;

namespace Zats.Services.SampleApplication.SiliconLabs.CLIProviders
{
    class LEDBulb : SampleApplicationCLIBase, ILEDBulbProvider
    {
        public LEDBulb(TextDevice device) : base(device)
        { }


        public void DimHold(int duration)
        {
            throw new NotImplementedException("DimHold is not implemented for LEDBulb Firmware");
        }

        public void DimOnOff()
        {
            throw new NotImplementedException("DimOnOff is not implemented for LEDBulb Firmware");
        }

        public void ToggleNotify()
        {
            throw new NotImplementedException("ToggleNotify is not implemented for LEDBulb Firmware");
        }

        public void OnOff()
        {
            throw new NotImplementedException("OnOff is not implemented for LEDBulb Firmware");
        }

        public bool? IsStatusOn()
        {
            throw new NotImplementedException("IsStatusOn is not implemented for LEDBulb Firmware");
        }

        public bool? IsDimStatusOn()
        {
            throw new NotImplementedException("IsDimStatusOn is not implemented for LEDBulb Firmware");
        }
    }
}