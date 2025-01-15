// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;
using ZWave;
using ZWave.Devices;
using ZWave.SerialPortApplication.Devices;
using ZWave.Enums;
using System.IO;

namespace ZWave.SerialPortApplication
{
    public class ApplicationLayer: IApplicationLayer
    {
        

        ISessionLayer mSessionLayer = null;
        #region IApplicationLayer Members

        /// <summary>
        /// Inits the specified session layer.
        /// </summary>
        /// <param name="sessionLayer">The session layer.</param>
        public void Init(ISessionLayer sessionLayer)
        {
            mSessionLayer = sessionLayer;
        }

        /// <summary>
        /// Creates the device.
        /// </summary>
        /// <returns></returns>
        public IDevice CreateDevice()
        {
            Device device = new Device(mSessionLayer);
            return device;
        }

        public IFlashSettings CreateFlashSettings()
        {
            return new FlashSettings();
        }

        #endregion
    }
}
