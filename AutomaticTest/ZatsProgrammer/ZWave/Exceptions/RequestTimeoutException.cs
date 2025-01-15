// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Exceptions
{
    /// <summary>
    /// The exception that is thrown when the time allotted for a reauest has expired.
    /// </summary>
    public class RequestTimeoutException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTimeoutException"/> class.
        /// </summary>
        public RequestTimeoutException()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTimeoutException"/> class.
        /// </summary>
        /// <param name="describe">The describe.</param>
        /// <param name="innerException">The inner exception.</param>
        public RequestTimeoutException(string describe, Exception innerException)
            : base(describe, innerException)
        {
            // Add any type-specific logic for inner exceptions.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestTimeoutException"/> class.
        /// </summary>
        /// <param name="describe">The describe.</param>
        public RequestTimeoutException(string describe) : base(describe) { }
    }
}
