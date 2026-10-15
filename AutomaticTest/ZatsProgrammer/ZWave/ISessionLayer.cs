// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;
using ZWave.Enums;
using ZWave.Events;
using ZWave.Logging;
using System.ComponentModel;

namespace ZWave
{
    /// <summary>
    /// Provides the features required to support communication sessions.
    /// </summary>
    public interface ISessionLayer
    {
        /// <summary>
        /// Represents the method that will handle the response received event.
        /// </summary>
        Action<ResponseReceivedEventArgs> ResponseReceived
        {
            get;
            set;
        }

        /// <summary>
        /// Represents the method that will handle the ack received event.
        /// </summary>
        Action<object> AcknowledgeReceived
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the request timeout.
        /// </summary>
        /// <value>The request timeout.</value>
        TimeSpan RequestTimeout
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the sequence number.
        /// </summary>
        /// <value>The sequence number.</value>
        byte SequenceNumber
        {
            get;
        }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is auto increment sequence number.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is auto increment sequence number; otherwise, <c>false</c>.
        /// </value>
        bool IsAutoIncrementSequenceNumber
        {
            get;
            set;
        }
        /// <summary>
        /// Gets a value indicating whether this instance is ready.
        /// </summary>
        /// <value><c>true</c> if this instance is ready; otherwise, <c>false</c>.</value>
        bool IsReady { get;}
        /// <summary>
        /// Inits the instance with the specified frame layer.
        /// </summary>
        /// <param name="frameLayer">The frame layer.</param>
        /// <param name="logDataSource">The log data source.</param>
        void Init(IFrameLayer frameLayer);
        /// <summary>
        /// Closes this instance.
        /// </summary>
        void Close();
        /// <summary>
        /// Executes the non request.
        /// </summary>
        /// <param name="request">The request.</param>
        void ExecuteNonRequest(byte[] request);
        /// <summary>
        /// Executes the non request.
        /// </summary>
        /// <param name="commandType">Type of the command.</param>
        /// <param name="parameters">The parameters.</param>
        void ExecuteNonRequest(byte commandType, params byte[] parameters);
        /// <summary>
        /// Opens the specified port name.
        /// </summary>
        /// <param name="portName">Name of the port.</param>
        void Open(string portName);
        /// <summary>
        /// Sends the request.
        /// </summary>
        /// <param name="commandType">Type of the command.</param>
        /// <param name="parameters">The parameters.</param>
        byte[] ExecuteRequest(byte commandType, params byte[] parameters);
        /// <summary>
        /// Executes the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        byte[] ExecuteRequest(byte[] request);
        /// <summary>
        /// Executes the request.
        /// </summary>
        /// <param name="commandType">Type of the command.</param>
        /// <param name="responseCount">The responses count.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        List<byte[]> ExecuteRequest(byte commandType, int responseCount, int timeout, params byte[] parameters);
        /// <summary>
        /// Executes the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="responseCount">The responses count.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        List<byte[]> ExecuteRequest(byte[] request, int responseCount, int timeout);
    }
}
