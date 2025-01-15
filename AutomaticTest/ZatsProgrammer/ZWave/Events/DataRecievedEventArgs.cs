// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Events
{
    /// <summary>
    /// Provides data for <see cref="ITransportLayer.DataReceived"/> event.
    /// </summary>
    public class DataReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="data">The data.</param>
        public DataReceivedEventArgs(byte[] data)
        {
            Data = data;
        }
        /// <summary>
        /// The data.
        /// </summary>
        public byte[] Data;
    }
}
