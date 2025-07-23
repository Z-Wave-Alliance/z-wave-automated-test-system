// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Threading;
using ZWave.TextApplication;

namespace Zats.Services.SampleApplication.SiliconLabs.CLIProviders
{
    class SensorPIR : SampleApplicationCLIBase, ISensorPirProvider
    {
        public SensorPIR(TextDevice device) : base(device)
        { }

        public void ActivatePirEvent()
        {
            SendCommand("motion_detected activate", 1000);
            Thread.Sleep(20);
            SendCommand("motion_detected deactivate", 1000);
        }

        public void BatteryReport()
        {
            SendCommand("battery_report", 1000);
        }
    }
}