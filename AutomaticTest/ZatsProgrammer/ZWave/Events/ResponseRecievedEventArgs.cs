// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Events
{
    /// <summary>
    /// Provides data for <see cref="ISessionLayer.ResponseReceived"/> event.
    /// </summary>
    public class ResponseReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="commandType">The command type.</param>
        /// <param name="frameBuffer">The frame buffer.</param>
        public ResponseReceivedEventArgs(byte[] data, byte commandType, byte[] frameBuffer, bool isSubstituted)
        {
            Data = data;
            CommandType = commandType;
            FrameBuffer = frameBuffer;
            IsSubstituted = isSubstituted;
        }
        /// <summary>
        /// The data.
        /// </summary>
        public byte[] Data;
        /// <summary>
        /// Command type.
        /// </summary>
        public byte CommandType;
        /// <summary>
        /// Frame buffer.
        /// </summary>
        public byte[] FrameBuffer;
        /// <summary>
        /// Frame buffer.
        /// </summary>
        public bool IsSubstituted;

    }
}
