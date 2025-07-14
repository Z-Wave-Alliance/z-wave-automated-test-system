// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ZWave.ZipApplication.Operations;

namespace Zats.Services
{
    public class ZipResult
    {
        public class Test
        {
            public int Total { get; set; }
            public int Fails { get; set; }

            public int SuccessPercentRate
            {
                get
                {
                    double k = Total - Fails;
                    return Total > 0 ? (int)(k * 100 / Total) : 0;
                }
            }

            public void Add(bool isSuccess)
            {
                Total++;
                if (!isSuccess)
                    Fails++;
            }

            internal void Reset()
            {
                Total = 0;
                Fails = 0;
            }
        }

        public class Ping
        {
            public int Total { get; private set; }
            public int Fails { get; private set; }
            public double mAverageSuccessReplyTimeMs;
            public int AverageSuccessReplyTimeMs
            {
                get { return (int)mAverageSuccessReplyTimeMs; }
            }
            public double SuccessPercentRate
            {
                get
                {
                    double k = Total - Fails;
                    return Total > 0 ? k * 100 / Total : 0;
                }
            }

            public void AddReply(bool isSuccess, int replyTimeMs)
            {
                Total++;
                if (!isSuccess)
                    Fails++;
                else
                {
                    double success = Total - Fails;
                    if (mAverageSuccessReplyTimeMs == 0)
                    {
                        mAverageSuccessReplyTimeMs = replyTimeMs;
                    }
                    else
                    {
                        double k = (success - 1) / success;
                        double m = replyTimeMs / success;
                        mAverageSuccessReplyTimeMs = k * mAverageSuccessReplyTimeMs + m;
                    }
                }
            }

            public void Reset()
            {
                Total = 0;
                Fails = 0;
                mAverageSuccessReplyTimeMs = 0;
            }
        }

        public class GetId : _ActionResult
        {
            public byte NodeId { get; private set; }
            public byte[] HomeId { get; private set; }
            public GetId(GetIdResult value)
                : base(value)
            {
                NodeId = value.NodeId;
                HomeId = value.HomeId;
            }
        }

        public class GetNodeList : RequestData
        {
            public byte ControllerId { get; private set; }
            public byte[] Nodes { get; private set; }
            public byte Status { get; private set; }

            public GetNodeList(GetNodeListResult value)
                : base(value)
            {
                ControllerId = value.ControllerId;
                if (value.Nodes != null)
                {
                    Nodes = value.Nodes.Select(x => (byte)x.Id).ToArray();
                }
                Status = value.Status;
            }
        }

        public class GetIpAddress : _ActionResult
        {
            public byte NodeId { get; private set; }
            public byte[] HomeId { get; private set; }
            public IPAddress IpAddress { get; private set; }

            public GetIpAddress(GetIpAddressResult value)
                : base(value)
            {
                NodeId = value.NodeId;
                HomeId = value.HomeId;
                IpAddress = value.IpAddress;
            }
        }

        public class AddNode : RequestData
        {
            public byte Status { get; set; }
            public byte NodeId { get; set; }
            public Node Node { get; set; }
            public byte Basic { get; set; }
            public byte Generic { get; set; }
            public byte Specific { get; set; }
            public byte[] SupportedCommandClasses { get; set; }
            public byte[] SecurelySupportedCommandClasses { get; set; }

            public AddNode(AddNodeResult value)
                : base(value)
            {
                InitWithResult(value);
            }

            public AddNode(AddNodeResult value, bool isStateCompleted) : base(value, isStateCompleted)
            {
                InitWithResult(value);
            }

            private void InitWithResult(AddNodeResult value)
            {
                Status = value.Status;
                NodeId = (byte)value.Node.Id;
                Node = value.Node;
                Basic = value.NodeInfo.Basic;
                Generic = value.NodeInfo.Generic;
                Specific = value.NodeInfo.Specific;
                SupportedCommandClasses = value.CommandClasses;
                SecurelySupportedCommandClasses = value.SecureCommandClasses;
            }
        }

        public class RemoveNode : RequestData
        {
            public byte Status { get; set; }
            public Node NodeId { get; set; }

            public RemoveNode(RemoveNodeResult value)
                : base(value)
            {
                NodeId = value.Node;
                Status = value.Status;
            }
        }

        public class LearnMode : RequestData
        {
            public byte Status { get; set; }
            public byte NodeId { get; set; }

            public LearnMode(SetLearnModeResult value)
                : base(value)
            {
                NodeId = value.NodeId;
                Status = value.Status;
            }
        }

        public class SetDefault : RequestData
        {
            public byte Status { get; private set; }

            public SetDefault(SetDefaultResult value)
                : base(value)
            {
                Status = value.Status;
            }
        }

        public class FirmwareBackup
        {
            public byte PrepareStatus { get; set; }
            public byte[] Data { get; set; }
        }

        public class FirmwareUpdate
        {
            public byte RequestStatus { get; set; }
            public byte ReportStatus { get; set; }
        }

        public class SendData : _ActionResult
        {
            public DateTime timeStamp { get; set; }
            public bool IsAckReceived { get; private set; }
            public bool IsNAckReceived { get; private set; }
            public bool IsBusyReceived { get; set; }
            public bool IsWaitingNackReceived { get; set; }

            /// <summary>
            /// Raw data. Can be cast to COMMAND_CLASS_ZIP.COMMAND_ZIP_PACKET
            /// </summary>
            public byte[] AckDataBuffer { get; private set; }
            public byte[] NackDataBuffer { get; private set; }

            public SendData(SendDataResult value)
                : base(value)
            {
                timeStamp = value.StopTimestamp;
                IsAckReceived = value.IsAckReceived;
                IsNAckReceived = value.IsNAckReceived;
                IsBusyReceived = value.IsBusyReceived;
                IsWaitingNackReceived = value.IsWnackReceived;
                AckDataBuffer = value.AckDataBuffer;
                NackDataBuffer = value.NackDataBuffer;
            }
        }

        public class RequestData : _ActionResult
        {
            public byte[] ReceivedData { get; private set; }
            public byte[] ReceivedHeader { get; private set; }
            public bool IsAckReceived { get; private set; }
            public bool IsNAckReceived { get; private set; }
            public bool IsBusyReceived { get; set; }

            public RequestData(RequestDataResult value)
                : base(value)
            {
                InitWithResult(value);
            }

            public RequestData(RequestDataResult value, bool isStateCompleted) : base(isStateCompleted)
            {
                InitWithResult(value);
            }

            private void InitWithResult(RequestDataResult value)
            {
                ReceivedData = value.ReceivedData;
                ReceivedHeader = value.ReceivedHeader;
                IsAckReceived = value.IsAckReceived;
                IsNAckReceived = value.IsNAckReceived;
                IsBusyReceived = value.IsBusyReceived;
            }
        }

        public class ExpectData : _ActionResult
        {
            public byte[] ReceivedData { get; private set; }
            public COMMAND_CLASS_ZIP_V5.COMMAND_ZIP_PACKET ReceivedPacket { get; set; }
            public List<byte[]> ReceivedDataList { get; set; }
            public DateTime timeStamp { get; set; }
            public ExpectData(ExpectDataResult value)
                : base(value)
            {
                timeStamp = value.StopTimestamp;
                ReceivedData = value.ReceivedData;
                ReceivedPacket = new byte[0];
                if (value.ReceivedPacket != null)
                {
                    ReceivedPacket = (byte[])value.ReceivedPacket;
                }
                ReceivedDataList = value.ReceivedDataList;
            }
        }

        public class Void : _ActionResult
        {
            public Void(AddNodeResult value)
                : base(value)
            {
            }
        }
    }
}
