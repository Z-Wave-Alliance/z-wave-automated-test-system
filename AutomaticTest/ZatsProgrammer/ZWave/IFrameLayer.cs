// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;
using ZWave.Events;
using ZWave.Enums;
using ZWave.Logging;
using System.ComponentModel;

namespace ZWave
{
    /// <summary>
    /// Provides the features required to support Data Frame manipulation.
    /// </summary>
    public interface IFrameLayer
    {
        /// <summary>
        /// Represents the method that will handle the exception received event.
        /// </summary>
        event ExceptionReceivedEventHandler ExceptionReceived;
        /// <summary>
        /// Represents the method that will handle the acknowledge received event.
        /// </summary>
        event FrameReceivedEventHandler AcknowledgeReceived;
        /// <summary>
        /// Represents the method that will handle the frame received event.
        /// </summary>
        event FrameReceivedEventHandler FrameReceived;
        /// <summary>
        /// Inits the instance with the specified Transport layer.
        /// </summary>
        /// <param name="transportLayer">The transport layer.</param>
        /// <param name="logDataSource">The log data source.</param>
        void Init(ITransportLayer transportLayer, LogPacketDelegate logDataSource);
        /// <summary>
        /// Closes this instance.
        /// </summary>
        void Close();
        /// <summary>
        /// Writes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        int Write(byte[] data);
        /// <summary>
        /// Writes the specified frame.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <returns></returns>
        int Write(IDataFrame frame);
        /// <summary>
        /// Writes the specified frame.
        /// </summary>
        /// <param name="frame">The frame.</param>
        void WriteAsync(IDataFrame frame);
        /// <summary>
        /// Opens the specified port name.
        /// </summary>
        /// <param name="portName">Name of the port.</param>
        void Open(string portName);
        /// <summary>
        /// Creates the data frame.
        /// </summary>
        /// <param name="frameType">Type of the frame.</param>
        /// <param name="commandType">Type of the command.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        IDataFrame CreateDataFrame(FrameTypes frameType, byte commandType, params byte[] parameters);
        /// <summary>
        /// Gets or sets a value indicating whether [retransmission queue locked].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [retransmission queue locked]; otherwise, <c>false</c>.
        /// </value>
        bool RetransmissionQueueLocked { get; set; }
        bool IsRemoteSerialApi { get; set; }
    }
}
