// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using ZWave.BasicApplication.Devices;

namespace Zats.Services.SampleApplication.SiliconLabs.IOProviders
{
    class MultilevelSensor : SampleApplicationIOBase, IMultilevelSensorProvider
    {
        public MultilevelSensor(DeviceHostClient deviceHostClient)
            : base(deviceHostClient)
        {
        }

        /// <summary>
        ///Trigger a movement detection (simulated by button BTN2).
        ///Hold for at least 1 second and release
        /// </summary>
        public void ActivateEvent()
        {
            if (_IsModernSeries)
            {

                if (_chipSeries == ChipSeries.ZGM230S_BRD4205B.ToString())
                {
                    IOHandler.HoldButton(0x01, 0x01);
                }
                else
                {
                    IOHandler.HoldButton(0x03, 0x03);
                }
            }
            else
            {
                //ToggleButton(0x02);
                throw new NotImplementedException("Unsupported by 500s");
            }
        }

        /// <summary>
        /// Sends Battery Report (only if the device is not sleeping)
        /// </summary>
        public void BatteryReport()
        {
            if (_IsModernSeries)
            {
                IOHandler.ToggleButton(0x01);
            }
            else
            {
                IOHandler.ToggleButton(0x05);
            }
        }

        /// <summary>
        /// LED1.
        /// Blinks with 1 Hz when learn mode is active.
        /// Used for Indicator Command Class.
        /// </summary>
        /// <returns></returns>
        public bool? IsStatusOn()
        {
            return _IsModernSeries ? IOHandler.GetLedState(0x01) : IOHandler.GetLedState(0x02);
        }
    }
}