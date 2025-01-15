// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Exceptions
{
    /// <summary>
    /// The exception that is thrown when a error occured in <see cref="ISessionLayer"></see>.
    /// </summary>
    public class SessionLayerException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SessionLayerException"/> class.
        /// </summary>
        public SessionLayerException()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionLayerException"/> class.
        /// </summary>
        /// <param name="describe">The describe.</param>
        /// <param name="innerException">The inner exception.</param>
        public SessionLayerException(string describe, Exception innerException)
            : base(describe, innerException)
        {
            // Add any type-specific logic for inner exceptions.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionLayerException"/> class.
        /// </summary>
        /// <param name="describe">The describe.</param>
        public SessionLayerException(string describe) : base(describe) { }

        
    }
}
