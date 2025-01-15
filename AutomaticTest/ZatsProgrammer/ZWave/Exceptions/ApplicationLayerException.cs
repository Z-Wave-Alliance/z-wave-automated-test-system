// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Exceptions
{
    /// <summary>
    /// The exception that is thrown when a error occured in <see cref="IApplicationLayer"></see>.
    /// </summary>
    public class ApplicationLayerException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationLayerException"/> class.
        /// </summary>
        public ApplicationLayerException() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationLayerException"/> class.
        /// </summary>
        /// <param name="describe">The describe.</param>
        public ApplicationLayerException(string describe) : base(describe) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationLayerException"/> class.
        /// </summary>
        /// <param name="describe">The describe.</param>
        /// <param name="innerException">The inner exception.</param>
        public ApplicationLayerException(string describe, Exception innerException)
            : base(describe, innerException) { }
    }
}
