// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Events
{
    /// <summary>
    /// Provides data for <see cref="IFrameLayer.ExceptionReceived"/> event.
    /// </summary>
    public class ExceptionReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="innerException">The inner exception.</param>
        public ExceptionReceivedEventArgs(byte[] data, Exception innerException)
        {
            Data = data;
            InnerException = innerException;
        }
        /// <summary>
        /// The data.
        /// </summary>
        public byte[] Data;
        /// <summary>
        /// The InnerException.
        /// </summary>
        public Exception InnerException;
    }
}
