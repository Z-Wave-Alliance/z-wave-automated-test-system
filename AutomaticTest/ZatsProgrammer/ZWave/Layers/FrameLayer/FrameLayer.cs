// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;
using ZWave.Events;
using System.Collections;
using ZWave.Enums;
using System.Threading;
using ZWave.Logging;
using System.ComponentModel;
using ZWave.Framework;

namespace ZWave.ProgrammerFrame
{
    class FrameLayer : IFrameLayer
    {

        private bool mRetransmissionQueueLocked;
        public bool RetransmissionQueueLocked
        {
            get { return mRetransmissionQueueLocked; }
            set { mRetransmissionQueueLocked = value; }
        }
        

        private const int ACK_WAIT_TIME = 2000; // How long in ms to wait for an ack
        private const ushort MAX_FRAME_SIZE = 512*8;
        private const byte MIN_FRAME_SIZE = 3;

        private ITransportLayer mTransportLayer = null;
        private LogPacketDelegate mLogDataSource;
        private FrameReceiveState mParserState;
        private DataFrame mCurrentDataFrame;
        private ushort frameLength = 0;


        #region IFrameLayer Members

        public event FrameReceivedEventHandler FrameReceived;
        public event FrameReceivedEventHandler AcknowledgeReceived;
        public event ExceptionReceivedEventHandler ExceptionReceived;

        public void Init(ITransportLayer transportLayer, LogPacketDelegate logDataSource)
        {
            mTransportLayer = transportLayer;
            mLogDataSource = logDataSource;
        }


        public void Close()
        {
            mTransportLayer.DataReceived -= new DataReceivedEventHandler(mTransportLayer_DataReceived);
            mTransportLayer.Close();
        }

        public int Write(byte[] data)
        {
            int ret = -1;
            try
            {
                ret = mTransportLayer.Write(data);
            }
            catch (Exception ex)
            {
                ExceptionReceived?.Invoke(new ExceptionReceivedEventArgs(data, ex));
            }
            return ret;
        }

        public IDataFrame CreateDataFrame(FrameTypes frameType, byte commandType, params byte[] parameters)
        {
            DataFrame frame = new DataFrame();
            frame.Command = commandType;
            frame.Type = frameType;
            if (parameters != null)
                frame.AddPayload(parameters);
            return frame;
        }

        public int Write(IDataFrame frame)
        {
            AddLogPacket(frame, true, null);
            int ret = Write(((DataFrame)frame).GetFrameBuffer());
            return ret;
        }

        private void AddLogPacket(IDataFrame frame, bool isPut, string comment)
        {
            //string app = GetApplicationString(frame.Command);
            //if (!string.IsNullOrEmpty(comment))
            //{
            //    app += " " + comment;
            //}

            mLogDataSource(
                new LogPacket(
                frame.GetPayload(),
                isPut,
                frame.Type == FrameTypes.Request,
                Tools.CurrentDateTime,
                frame.Command,
                typeof(ProgrammerCommandTypes)));
        }

        private string GetApplicationString(byte commandKey)
        {
            ProgrammerCommandTypes type = (ProgrammerCommandTypes)commandKey;
            return type.ToString();
        }

        public void Open(string portName)
        {
            mTransportLayer.Open(portName);
            mTransportLayer.DataReceived += new DataReceivedEventHandler(mTransportLayer_DataReceived);
        }

        public bool IsRemoteSerialApi
        {
            get { return false; }
            set { }
        }

        #endregion

        private void mTransportLayer_DataReceived(DataReceivedEventArgs e)
        {
            //mLogDataSource.Add(new LogPacket(e.Data, false, Tools.CurrentDateTime, ""));
            foreach (byte var in e.Data)
            {
                ParseRawData(var);
            }
        }
        private bool ParseRawData(byte buffer)
        {

            switch (mParserState)
            {
                case FrameReceiveState.FRS_SOF_HUNT:
                    if (HeaderTypes.StartOfFrame == (HeaderTypes)buffer)
                    {
                        mParserState = FrameReceiveState.FRS_LENGTH_H;
                    }
                    // Acknowledge received from peer
                    else if (HeaderTypes.Acknowledge == (HeaderTypes)buffer)
                    {
                        mLogDataSource(new LogPacket(new byte[] { buffer }, false, false, Tools.CurrentDateTime, buffer, typeof(HeaderTypes)));
                        // Remove the last frame from the retransmission stack
                        AcknowledgeReceived?.Invoke(new FrameReceivedEventArgs(new byte[] { (byte)HeaderTypes.Acknowledge }, false, 0x00, new byte[] { 0x00 }));
                    }
                    // Not Acknowledge received from peer
                    else if (HeaderTypes.NotAcknowledged == (HeaderTypes)buffer)
                    {
                        mLogDataSource(new LogPacket(new byte[] { buffer }, false, false, Tools.CurrentDateTime, buffer, typeof(HeaderTypes)));
                    }
                    // CAN frame received - peer dropped a data frame transmitted by us
                    else if (HeaderTypes.Can == (HeaderTypes)buffer)
                    {
                        mLogDataSource(new LogPacket(new byte[] { buffer }, false, false, Tools.CurrentDateTime, buffer, typeof(HeaderTypes)));
                        // Do noting... just wait for the retransmission timer to kick-in
                    }
                    break;
                case FrameReceiveState.FRS_LENGTH_H:
                    {
                        frameLength = buffer;
                        frameLength <<= 8;
                        mParserState = FrameReceiveState.FRS_LENGTH_L;
                    }
                    break;
                case FrameReceiveState.FRS_LENGTH_L:
                    frameLength |= buffer;
                    if (frameLength < MIN_FRAME_SIZE || frameLength > MAX_FRAME_SIZE)
                    {
                        mParserState = FrameReceiveState.FRS_SOF_HUNT;
                    }
                    else
                    {
                        mCurrentDataFrame = new DataFrame((ushort)(frameLength - 4)); // Payload size is excluding len, type & cmd field
                        mParserState = FrameReceiveState.FRS_TYPE;
                    }
                    break;

                case FrameReceiveState.FRS_TYPE:
                    mCurrentDataFrame.Type = (FrameTypes)buffer;
                    if (mCurrentDataFrame.Type == FrameTypes.Request ||
                      mCurrentDataFrame.Type == FrameTypes.Response)
                    {
                        mParserState = FrameReceiveState.FRS_COMMAND;
                    }
                    else
                    {
                        mParserState = FrameReceiveState.FRS_SOF_HUNT;
                    }
                    break;

                case FrameReceiveState.FRS_COMMAND:
                    mCurrentDataFrame.Command = buffer;
                    if (mCurrentDataFrame.IsPayloadFull())
                        mParserState = FrameReceiveState.FRS_CHECKSUM;
                    else
                        mParserState = FrameReceiveState.FRS_DATA;
                    break;

                case FrameReceiveState.FRS_DATA:
                    if (!mCurrentDataFrame.AddPayload(buffer))
                    {
                        mParserState = FrameReceiveState.FRS_SOF_HUNT;
                    }
                    else if (mCurrentDataFrame.IsPayloadFull())
                    {
                        mParserState = FrameReceiveState.FRS_CHECKSUM;
                    }
                    break;

                case FrameReceiveState.FRS_CHECKSUM:
                    // Frame received successfully -> Send acknowledge (ACK)
                    if (mCurrentDataFrame.IsChecksumValid(buffer))
                    {

                        AddLogPacket(mCurrentDataFrame, false, null);
                        TransmitACK();
                        FrameReceived?.Invoke(new FrameReceivedEventArgs(mCurrentDataFrame.GetPayloadBuffer(), false, mCurrentDataFrame.Command, mCurrentDataFrame.GetFrameBuffer()));

                    }
                    // Frame receive failed -> Send NAK
                    else
                    {
                        TransmitNAK();
                    }

                    mParserState = FrameReceiveState.FRS_SOF_HUNT;
                    break;

                case FrameReceiveState.FRS_RX_TIMEOUT:
                default:
                    mParserState = FrameReceiveState.FRS_SOF_HUNT;
                    break;

            }

            return true;
        }

        private void TransmitACK()
        {
            mLogDataSource(new LogPacket(new byte[] { (byte)HeaderTypes.Acknowledge }, true, false, Tools.CurrentDateTime, (byte)HeaderTypes.Acknowledge, typeof(HeaderTypes)));
            mTransportLayer.Write(new byte[] { (byte)HeaderTypes.Acknowledge });
        }

        private void TransmitNAK()
        {
            mLogDataSource(new LogPacket(new byte[] { (byte)HeaderTypes.NotAcknowledged }, true, false, Tools.CurrentDateTime, (byte)HeaderTypes.NotAcknowledged, typeof(HeaderTypes)));
            mTransportLayer.Write(new byte[] { (byte)HeaderTypes.NotAcknowledged });
        }

        public void WriteAsync(IDataFrame frame)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }
}
