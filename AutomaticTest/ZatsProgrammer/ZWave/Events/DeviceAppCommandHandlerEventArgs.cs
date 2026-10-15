// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Events
{
    /// <summary>
    /// Provides data for <see cref="ZWave.Devices.IDevice.ApplicationCommandHandlerEvent"/> event.
    /// </summary>
    public class DeviceAppCommandHandlerEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAppCommandHandlerEventArgs"/> class.
        /// </summary>
        /// <param name="frameBuffer">The frame buffer.</param>
        /// <param name="sourceNodeId">The source node id.</param>
        /// <param name="commandClassKey">The command class key.</param>
        /// <param name="commandKey">The command key.</param>
        public DeviceAppCommandHandlerEventArgs(byte[] commandBuffer, byte sourceNodeId, byte commandClassKey, byte commandKey, bool isBroadcast, bool isSubstituted)
            : this(commandBuffer, sourceNodeId, null, commandClassKey, commandKey, isBroadcast, isSubstituted)
        {

        }

        public DeviceAppCommandHandlerEventArgs(byte[] commandBuffer, byte sourceNodeId, byte[] destNodeIds, byte commandClassKey, byte commandKey, bool isBroadcast, bool isSubstituted)
        {
            CommandBuffer = commandBuffer;
            SourceNodeId = sourceNodeId;
            CommandClassKey = commandClassKey;
            CommandKey = commandKey;
            IsBroadcast = isBroadcast;
            DestNodeIds = destNodeIds;
            IsSubstituted = isSubstituted;
        }

        public byte[] GetCommandValue()
        {
            byte[] ret = null;
            if (CommandBuffer == null)
                ret = new byte[] { CommandClassKey, CommandKey };
            else
            {
                ret = new byte[2 + CommandBuffer.Length];
                ret[0] = CommandClassKey;
                ret[1] = CommandKey;
                Array.Copy(CommandBuffer, 0, ret, 2, CommandBuffer.Length);
            }
            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsBroadcast;
        /// <summary>
        /// 
        /// </summary>
        public byte[] CommandBuffer;
        /// <summary>
        /// 
        /// </summary>
        public byte SourceNodeId;
        /// <summary>
        /// 
        /// </summary>
        public byte[] DestNodeIds;
        /// <summary>
        /// 
        /// </summary>
        public byte CommandClassKey;
        /// <summary>
        /// 
        /// </summary>
        public byte CommandKey;
        /// <summary>
        /// 
        /// </summary>
        public bool IsSubstituted;
    }
}
