// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Events
{
    /// <summary>
    /// Represents the method that handles a <see cref="ITransportLayer.DataReceived"/> event.
    /// </summary>
    public delegate void DataReceivedEventHandler(DataReceivedEventArgs args);
}
