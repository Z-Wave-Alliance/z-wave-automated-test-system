// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Events
{
    /// <summary>
    /// Provides data for  <see cref="IFrameLayer.FrameReceived"/> event.
    /// </summary>
    public class FrameReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FrameReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="isAck">if set to <c>true</c> is acknoledgement.</param>
        /// <param name="commandType">Tthe command type.</param>
        /// <param name="frameBuffer">The frame buffer.</param>
        public FrameReceivedEventArgs(byte[] data, bool isAck, byte commandType, byte[] frameBuffer)
        {
            Data = data;
            IsACK = isAck;
            CommandType = commandType;
            FrameBuffer = frameBuffer;
        }
        /// <summary>
        /// The data.
        /// </summary>
        public byte[] Data;
        /// <summary>
        /// Is acknoledgement.
        /// </summary>
        public bool IsACK;
        /// <summary>
        /// Command type.
        /// </summary>
        public byte CommandType;
        /// <summary>
        /// Frame buffer.
        /// </summary>
        public byte[] FrameBuffer;
        /// <summary>
        /// Is Substituted.
        /// </summary>
        public bool IsSubstituted;
    }
}
