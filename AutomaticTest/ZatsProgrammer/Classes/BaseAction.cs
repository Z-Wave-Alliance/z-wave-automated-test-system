// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;
using ZWave.Devices;
using ZWave.Programmer.Controllers;

namespace ZWave.Programmer.Classes
{
    /// <summary>
    /// Base class for actions. Provides methods that extends <see cref="ControllerManager"/> 
    /// </summary>
    public class BaseAction
    {
        internal ControllerManager ControllerManager { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseAction"/> class.
        /// </summary>
        /// <param name="controllerManager">The controller manager.</param>
        public BaseAction(ControllerManager controllerManager)
        {
            ControllerManager = controllerManager;
        }

        public bool CheckDeviceChipTypeCompatibility(IProgrammableDevice device)
        {
            return ControllerManager.DocumentModel.SelectedChipType == device.ChipType;
        }
    }
}
