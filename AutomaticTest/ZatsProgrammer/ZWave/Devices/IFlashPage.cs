// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Devices
{
    /// <summary>
    /// This interface provides properties and methods that allow the client program using Flash Pages <see cref="IDeviceFlash"></see>.
    /// </summary>
    public interface IFlashPage
    {
        /// <summary>
        /// Gets or sets the <see cref="IDeviceFlash"/>.
        /// </summary>
        /// <value>The flash.</value>
        IDeviceFlash Flash
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the buffer.
        /// </summary>
        /// <value>The buffer.</value>
        byte[] Buffer { get; set; }
        /// <summary>
        /// Gets or sets the address.
        /// </summary>
        /// <value>The address.</value>
        int Address { get; set; }

        /// <summary>
        /// Writes the page.
        /// </summary>
        /// <param name="isAsic">if set to <c>true</c> is Asic.</param>
        /// <param name="verify">if set to <c>true</c> verify.</param>
        /// <returns></returns>
        bool Write(bool isAsic, bool verify);
        /// <summary>
        /// Compares the specified page.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <returns></returns>
        bool Compare(IFlashPage page, bool ignoreCalibrationValue);
    }
}
