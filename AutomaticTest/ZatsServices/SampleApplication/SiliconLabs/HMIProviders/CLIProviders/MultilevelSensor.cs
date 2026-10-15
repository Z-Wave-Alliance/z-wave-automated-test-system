// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using ZWave.TextApplication;

namespace Zats.Services.SampleApplication.SiliconLabs.CLIProviders
{
    class MultilevelSensor : SampleApplicationCLIBase, IMultilevelSensorProvider
    {
        public MultilevelSensor(TextDevice device) : base(device)
        { }

        /// <summary>
        ///Trigger a movement detection (simulated by button BTN2).
        ///Hold for at least 1 second and release
        /// </summary>
        public void ActivateEvent()
        {
            SendCommand("send_reports", TIMEOUT_MS);
        }

        /// <summary>
        /// Sends Battery Report (only if the device is not sleeping)
        /// </summary>
        public void BatteryReport()
        {
            SendCommand("send_reports", TIMEOUT_MS);
        }

        /// <summary>
        /// LED1.
        /// Blinks with 1 Hz when learn mode is active.
        /// Used for Indicator Command Class.
        /// </summary>
        /// <returns></returns>
        public bool? IsStatusOn()
        {
            // TODO: Implement this method
            throw new NotImplementedException("I am not sure what is the use case here!");
        }
    }
}