// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Exceptions
{
    /// <summary>
    /// The exception that is thrown when a error occured in <see cref="ITransportLayer"></see>.
    /// </summary>
    public class TransportLayerException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransportLayerException"/> class.
        /// </summary>
        public TransportLayerException()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransportLayerException"/> class.
        /// </summary>
        /// <param name="describe">The describe.</param>
        /// <param name="innerException">The inner exception.</param>
        public TransportLayerException(string describe, Exception innerException)
            : base(describe, innerException)
        {
            // Add any type-specific logic for inner exceptions.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransportLayerException"/> class.
        /// </summary>
        /// <param name="describe">The describe.</param>
        public TransportLayerException(string describe) : base(describe) { }

        //  protected ITransportLayerException(SerializationInfo info, StreamingContext context) : base(info, context) {}
    }
}
