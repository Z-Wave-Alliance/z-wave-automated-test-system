// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;
using ZWave;
using ZWave.Events;
using ZWave.Enums;
using System.Threading;
using ZWave.Exceptions;
using ZWave.Logging;
using System.ComponentModel;

namespace ZWave.SerialPortSession
{
    public class SessionLayer : ISessionLayer
    {
        private const byte MIN_SEQUENCE_NUMBER = 1;
        private const byte MAX_SEQUENCE_NUMBER = 127;


        IFrameLayer mFrameLayer = null;

        private byte mSequenceNumber = MIN_SEQUENCE_NUMBER;
        public byte SequenceNumber
        {
            get
            {
                if (IsAutoIncrementSequenceNumber)
                {
                    mSequenceNumber++;
                }
                if (mSequenceNumber == MAX_SEQUENCE_NUMBER)
                {
                    mSequenceNumber = MIN_SEQUENCE_NUMBER;
                }
                return mSequenceNumber;
            }
        }
        public byte CurrentSequenceNumber
        {
            get
            {
                return mSequenceNumber;
            }
        }

        #region ISessionLayerEx Members

        private bool mIsReady = false;
        /// <summary>
        /// Gets a value indicating whether this instance is ready.
        /// </summary>
        /// <value><c>true</c> if this instance is ready; otherwise, <c>false</c>.</value>
        public bool IsReady
        {
            get
            {
                return mIsReady;
            }
            set
            {
                mIsReady = value;
            }
        }

        private bool mIsAutoIncrementSequenceNumber = true;
        /// <summary>
        /// Gets or sets a value indicating whether this instance is auto increment sequence number.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is auto increment sequence number; otherwise, <c>false</c>.
        /// </value>
        public bool IsAutoIncrementSequenceNumber
        {
            get { return mIsAutoIncrementSequenceNumber; }
            set
            {
                if (mIsAutoIncrementSequenceNumber && mIsAutoIncrementSequenceNumber != value)
                {
                    mSequenceNumber++;
                }
                mIsAutoIncrementSequenceNumber = value;
            }
        }

        private TimeSpan mRequestTimeout = new TimeSpan(0, 0, 3);
        /// <summary>
        /// Gets or sets the request timeout.
        /// </summary>
        /// <value>The request timeout.</value>
        public TimeSpan RequestTimeout
        {
            get
            {
                return mRequestTimeout;
            }
            set
            {
                mRequestTimeout = value;
            }
        }


        /// <summary>
        /// Closes this instance.
        /// </summary>
        public void Close()
        {
            mFrameLayer.FrameReceived -= new FrameReceivedEventHandler(mFrameLayer_FrameReceived);
            mFrameLayer.Close();
        }

        private Action<ResponseReceivedEventArgs> responseReceived;
        public Action<ResponseReceivedEventArgs> ResponseReceived
        {
            get { return responseReceived; }
            set { responseReceived = value; }
        }

        private Action<object> acknowledgeReceived;
        public Action<object> AcknowledgeReceived
        {
            get { return acknowledgeReceived; }
            set { acknowledgeReceived = value; }
        }

        /// <summary>
        /// Opens the specified frame layer.
        /// </summary>
        /// <param name="frameLayer">The frame layer.</param>
        public void Init(IFrameLayer frameLayer)
        {
            mFrameLayer = frameLayer;
        }
        string pName = "";
        /// <summary>
        /// Opens the specified port name.
        /// </summary>
        /// <param name="portName">Name of the port.</param>
        public void Open(string portName)
        {
            pName = portName;
            mFrameLayer.Open(portName);
            mFrameLayer.FrameReceived += new FrameReceivedEventHandler(mFrameLayer_FrameReceived);
            mFrameLayer.ExceptionReceived += new ExceptionReceivedEventHandler(mFrameLayer_ExceptionReceived);
        }

        void mFrameLayer_ExceptionReceived(ExceptionReceivedEventArgs args)
        {
            innerException = args.InnerException;
        }

        void mFrameLayer_FrameReceived(FrameReceivedEventArgs args)
        {
            FrameReceivedEventArgs frArgs = args;
            if (waitResponses && responses.Count < waitResponsesCount)
            {
                if (frArgs.CommandType == responseCommand)
                {
                    responses.Add(frArgs.Data);
                }
                if (responses.Count == waitResponsesCount)
                {
                    //Tools._writeDebugDiagnosticMessage("asyncFrameReceived.Set responses:" + responses.Count + " waitResponsesCount: " + waitResponsesCount.ToString(), true, true);
                    SetRequest();
                }
            }
            RaiseResponseReceived(frArgs);
        }

        private void RaiseResponseReceived(FrameReceivedEventArgs args)
        {
            if (ResponseReceived != null)
            {
                //System.Diagnostics.Debug.WriteLine("Session Layer. Event RaiseResponseReceivedInternal Raised - " + pName);
                try
                {
                    ResponseReceived(new ResponseReceivedEventArgs(args.Data, args.CommandType, args.FrameBuffer, args.IsSubstituted));
                }
                catch (RequestTimeoutException)
                {
                    System.Diagnostics.Debug.WriteLine("RequestTimeoutException");
                }
            }
        }

        private byte responseCommand = 0;
        private Exception innerException = null;
        ManualResetEvent asyncFrameReceived = new ManualResetEvent(false);
        private void ResetRequest()
        {
            asyncFrameReceived.Reset();
            innerException = null;
            //Tools._writeDebugDiagnosticMessage("***RESET!", true, true, 3);
        }

        private void SetRequest()
        {
            asyncFrameReceived.Set();
            //Tools._writeDebugDiagnosticMessage("***SET!", true, true, 3);
        }

        private void EndRequest(int timeout)
        {
            Exception ex = null;
            //Tools._writeDebugDiagnosticMessage("***WAIT!", true, true, 3);
            if (!asyncFrameReceived.WaitOne(timeout, true))
            {
                //Tools._writeDebugDiagnosticMessage("***FALSE!", true, true, 3);
                waitResponses = false;
                responses.Clear();
                ex = new RequestTimeoutException();
            }
            else
            {
                //Tools._writeDebugDiagnosticMessage("***TRUE!", true, true, 3);
            }

            if (innerException != null)
            {
                ex = innerException;
            }

            if (ex != null)
            {
                throw ex;
            }
        }
        private void EndRequest()
        {
            EndRequest((int)RequestTimeout.TotalMilliseconds);
        }

        /// <summary>
        /// Executes the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public byte[] ExecuteRequest(byte[] request)
        {
            responses.Clear();
            waitResponses = true;
            responseCommand = request[0];
            waitResponsesCount = 1;
            ResetRequest();
            mFrameLayer.Write(request);
            EndRequest();
            if (responses.Count > 0)
                return responses[0];
            else return null;
        }


        /// <summary>
        /// Sends the request.
        /// </summary>
        /// <param name="commandType">Type of the command.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public byte[] ExecuteRequest(byte commandType, params byte[] parameters)
        {
            responses.Clear();
            waitResponses = true;
            responseCommand = commandType;
            waitResponsesCount = 1;
            IDataFrame frame = mFrameLayer.CreateDataFrame(FrameTypes.Request, commandType, parameters);
            ResetRequest();
            mFrameLayer.Write(frame);
            EndRequest();
            if (responses.Count > 0)
                return responses[0];
            else return null;
        }

        private List<byte[]> responses = new List<byte[]>();
        private bool waitResponses = false;
        private int waitResponsesCount;
        /// <summary>
        /// Executes the request.
        /// </summary>
        /// <param name="commandType">Type of the command.</param>
        /// <param name="responseCount">The responses count.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public List<byte[]> ExecuteRequest(byte commandType, int responseCount, int timeout, params byte[] parameters)
        {
            responses.Clear();
            waitResponses = true;
            waitResponsesCount = responseCount;
            responseCommand = commandType;
            IDataFrame frame = mFrameLayer.CreateDataFrame(FrameTypes.Request, commandType, parameters);
            ResetRequest();
            mFrameLayer.Write(frame);
            EndRequest(timeout);
            waitResponses = false;
            return responses;
        }
        /// <summary>
        /// Executes the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="responseCount">The responses count.</param>
        /// <returns></returns>
        public List<byte[]> ExecuteRequest(byte[] request, int responseCount, int timeout)
        {
            responses.Clear();
            waitResponses = true;
            waitResponsesCount = responseCount;
            responseCommand = request[0];
            ResetRequest();
            mFrameLayer.Write(request);
            EndRequest(timeout);
            waitResponses = false;
            return responses;
        }


        /// <summary>
        /// Executes the non request.
        /// </summary>
        /// <param name="commandType">Type of the command.</param>
        /// <param name="parameters">The parameters.</param>
        public void ExecuteNonRequest(byte commandType, params byte[] parameters)
        {
            responseCommand = 0;
            IDataFrame frame = mFrameLayer.CreateDataFrame(FrameTypes.Request, commandType, parameters);
            mFrameLayer.Write(frame);
        }
        /// <summary>
        /// Sends the request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void ExecuteNonRequest(byte[] request)
        {
            responseCommand = 0;
            mFrameLayer.Write(request);
        }

        #endregion
    }
}
