// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using ZWave.Events;

namespace ZWave
{
    /// <summary>
    /// Provides the features required to support communication with connected Z-Wave Device.
    /// </summary>
    public interface ITransportLayer
    {
        /// <summary>
        /// Opens the specified communication interface.
        /// </summary>
        /// <param name="deviceName">Name of the device (communication interface).</param>
        void Open(string deviceName);

        /// <summary>
        /// Closes the opened communication interface.
        /// </summary>
        void Close();

        /// <summary>
        /// Writes the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        int Write(byte[] buffer);

        /// <summary>
        /// Determines whether communication interface is open.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if communication interface is open; otherwise, <c>false</c>.
        /// </returns>
        bool IsOpen();

        /// <summary>
        /// Gets the installed communication interfaces names.
        /// </summary>
        /// <returns></returns>
        IList<string> GetDeviceNames();

        /// <summary>
        /// Occurs when Data received.
        /// </summary>
        event DataReceivedEventHandler DataReceived;
    }
}
