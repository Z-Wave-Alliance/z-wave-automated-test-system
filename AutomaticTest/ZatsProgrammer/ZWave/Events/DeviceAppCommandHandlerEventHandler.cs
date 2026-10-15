// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Events
{
    /// <summary>
    /// Represents the method that handles a <see cref="ZWave.Devices.IDevice.ApplicationCommandHandlerEvent"/> event.
    /// </summary>
    public delegate void DeviceAppCommandHandlerEventHandler(DeviceAppCommandHandlerEventArgs args);
}
