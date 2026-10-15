// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using ZWave.Enums;
namespace ZWave
{
    /// <summary>
    /// Provides the features required to manipulate frame data.
    /// </summary>
    public interface IDataFrame
    {
        /// <summary>
        /// Adds the payload.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        bool AddPayload(byte[] data);
        /// <summary>
        /// Adds the payload.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        bool AddPayload(byte data); 
        /// <summary>
        /// Gets payload (parameters)
        /// </summary>
        /// <returns>The payload</returns>
        byte[] GetPayload();
        /// <summary>
        /// Gets or sets the Serial API Command Key.
        /// </summary>
        /// <value>The command.</value>
        byte Command { get; set; }
        /// <summary>
        /// Gets or sets the Frame Type.
        /// </summary>
        /// <value>The type.</value>
        FrameTypes Type { get; set; }
        /// <summary>
        /// Gets the frame buffer.
        /// </summary>
        /// <returns></returns>
        byte[] GetFrameBuffer();
    }
}
