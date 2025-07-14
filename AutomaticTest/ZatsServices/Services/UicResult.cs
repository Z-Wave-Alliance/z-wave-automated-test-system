// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;
using System.Net;
using UicApplication.Operations;
using ZWave.UicApplication;
using UicApplication.Clusters;
using System;

namespace Zats.Services
{
    public class UicResult
    {
        public class mqttMessage
        {
            public mqttMessage(string _topic, string _payload)
            {
                topic = _topic;
                payload = _payload;
            }
            public string topic { get; set; }
            public string payload { get; set; }
        }
        public class RequestData : _ActionResult
        {
            public byte[] ReceivedData { get; private set; }
            public byte[] ReceivedHeader { get; private set; }
            public bool IsAckReceived { get; private set; }
            public bool IsNAckReceived { get; private set; }
            public bool IsBusyReceived { get; set; }
           
        }
        public class ExpectData : _ActionResult
        {
            public string ReceivedTopic { get; set; }
            public string ReceivedPayload { get; set; }
            public DateTime timeStamp { get; set; }
            public ExpectData(ExpectDataResult value)
            {
                timeStamp = value.StopTimestamp;
                ReceivedTopic = value.mqttReceivedTopic;
                ReceivedPayload = value.mqttReceivedPayload;
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
            }
        }
        public class ListenData : _ActionResult
        {
            public List<mqttMessage> mqttMssg { get; set; }
            public DateTime timeStamp { get; set; }
            public ListenData(ListenDataResult value)
            {
                timeStamp = value.StopTimestamp;
                mqttMssg = new List<mqttMessage>();
                foreach (var mssg in value.mqttMessages)
                {
                    mqttMssg.Add(new mqttMessage(mssg.mqttReceivedTopic, mssg.mqttReceivedPayload));
                }
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
            }
        }
        public class Inclusion : _ActionResult
        {
            public string ReceivedTopic { get; set; }
            public string ReceivedPayload { get; set; }
            public string UnId { get; set; }
            public string Security { get; set; }
            public int Id { get; set; }
            public NodeInterviewResult NodeInterviewResult { get; set; }

            public Inclusion(InclusionResult value)
                : base(value)
            {
                Security = value.Security;
                ReceivedTopic = value.mqttReceivedTopic;
                ReceivedPayload = value.mqttReceivedPayload;
                UnId = value.UnId;
                if (UnId == null)
                {
                    Id = 0;
                }
                else
                {
                    string id_str = String.Empty;
                    string[] unidSplit = UnId.Split('-');
                    if (unidSplit[0] == "zb")
                    {
                        id_str = UnId.Split('-')[1];
                        Int32.TryParse(id_str, out int Id);
                    }
                    else
                    {
                        id_str = UnId.Split('-')[2];
                        Id = id_str.ToByteArray().ToNumber();
                    }
                }
            }

            public Inclusion(bool IsStateCompleted)
                : base(IsStateCompleted)
            {
            }
        }
        public class Exclusion : _ActionResult
        {
            public string ReceivedTopic { get; set; }
            public string ReceivedPayload { get; set; }
            public string UnId { get; set; }
            public Exclusion(ExclusionResult value)
                : base(value)
            {
                ReceivedTopic = value.mqttReceivedTopic;
                ReceivedPayload = value.mqttReceivedPayload;
                UnId = value.UnId;
            }
            public Exclusion(bool IsStateCompleted)
                : base(IsStateCompleted)
            {
            }
        }
        public class GetNodes : _ActionResult
        {
            public List<string> UnId { get; set; }
            public byte[] HomeId { get; set; }
            public GetNodes(GetNodesResult value)
                : base(value)
            {
                UnId = value.UnIds;
                HomeId = UnId[0].Split('-')[1].ToByteArray();
            }
        }
        public class NodeInterviewResult
        {
            public GroupsInformation GroupsInformation { get; set; }
            public OnOffInformation OnOffInformation { get; set; }
        }
        public class GroupsInformation
        {
            public List<string> SupportedCommands { get; set; }
        }
        public class OnOffInformation
        {
            public List<string> SupportedCommands { get; set; }
        }
    }
}
