// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Exceptions
{
    /// <summary>
    /// The exception that is thrown when a error occured in <see cref="IFrameLayer"></see>.
    /// </summary>
    public class FrameLayerException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FrameLayerException"/> class.
        /// </summary>
        public FrameLayerException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FrameLayerException"/> class.
        /// </summary>
        /// <param name="describe">The describe.</param>
        public FrameLayerException(string describe) : base(describe) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FrameLayerException"/> class.
        /// </summary>
        /// <param name="describe">The describe.</param>
        /// <param name="innerException">The inner exception.</param>
        public FrameLayerException(string describe, Exception innerException)
            : base(describe, innerException)
        {
            // Add any type-specific logic for inner exceptions.
        }
    }
}
