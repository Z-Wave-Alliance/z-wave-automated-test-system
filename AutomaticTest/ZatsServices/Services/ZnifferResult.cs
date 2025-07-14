// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;
using System.Linq;
using ZWave.ZnifferApplication;
using ZWave.ZnifferApplication.Operations;

namespace Zats.Services
{
    public class ZnifferResult
    {
        public class ExpectData : _ActionResult
        {
            public List<ZnifferDataItem> Frames { get; private set; }
            public int FramesDelta { get; private set; }
            public ExpectData(CollectDataResult value, SecurityManager securityManager)
                : base(value)
            {
                ElapsedMs = value.ElapsedMs;
                Frames = new List<ZnifferDataItem>();
                if (value.DataItems != null && value.DataItems.Count > 0)
                {
                    FramesDelta = (int)(value.DataItems[value.DataItems.Count - 1].CreatedAt - value.DataItems[0].CreatedAt).TotalMilliseconds;
                    for (int i = 0; i < value.DataItems.Count; i++)
                    {
                        Frames.Add(new ZnifferDataItem(value.DataItems[i], securityManager));
                    }
                }
            }
        }

        public class CollectData : _ActionResult
        {
            public List<ZnifferDataItem> Frames { get; private set; }
            public int FramesDelta { get; private set; }
            public CollectData(CollectDataResult value, SecurityManager securityManager)
                : base(value)
            {
                ElapsedMs = value.ElapsedMs;
                Frames = new List<ZnifferDataItem>();
                if (value.DataItems != null && value.DataItems.Count > 0)
                {
                    FramesDelta = (int)(value.DataItems[value.DataItems.Count - 1].CreatedAt - value.DataItems[0].CreatedAt).TotalMilliseconds;
                    for (int i = 0; i < value.DataItems.Count; i++)
                    {
                        Frames.Add(new ZnifferDataItem(value.DataItems[i], securityManager));
                    }
                }
            }

            public IEnumerable<ZnifferDataItem> FilterFrames(byte srcNodeId, byte destNodeId, byte cmdId)
            {
                if (Frames == null)
                {
                    return null;
                }

                return Frames.Where(
                    frame => frame.Source == srcNodeId && frame.Destination == destNodeId && frame.Payload[1] == cmdId);
            }
        }
    }

    public class ZnifferDataItem
    {
        DataItem _dataItem;
        private readonly byte[] _originalPayload;
        public byte[] OriginalPayload
        {
            get { return _originalPayload; }
        }

        private readonly byte[] _payload;
        public byte[] Payload
        {
            get { return _payload; }
        }

        public byte[] NetworkKey
        {
            get;
            set;
        }

        public List<COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION.TVG1> Extensions
        {
            get;
            set;
        }

        public Node Source
        {
            get { return _dataItem.Source; }
        }

        public Node[] Destinations
        {
            get { return _dataItem.Destinations != null ? _dataItem.Destinations.Select(x => new Node(x)).ToArray() : new Node[0]; }
        }

        public Node Destination
        {
            get { return _dataItem.Destination; }
        }

        public byte[] HomeId
        {
            get { return _dataItem.HomeId ?? (new byte[] { }); }
        }

        public byte[] DataBuffer
        {
            get { return _dataItem.DataBuffer ?? (new byte[] { }); }
        }

        public HeaderTypesEx HeaderType
        {
            get { return (HeaderTypesEx)_dataItem.HeaderType; }
        }

        public int SequenceNumber
        {
            get { return _dataItem.SequenceNumber; }
        }

        public byte[] Repeaters
        {
            get { return _dataItem.Repeaters ?? (new byte[] { }); }
        }

        public bool IsLow
        {
            get { return _dataItem.IsLow; }
        }

        public bool IsAck
        {
            get { return _dataItem.IsAck; }
        }

        public byte RoutesCount
        {
            get { return _dataItem.RoutesCount; }
        }

        public byte Speed
        {
            get { return _dataItem.Speed; }
        }

        public byte Channel
        {
            get { return _dataItem.Channel; }
        }

        public byte Frequency
        {
            get { return _dataItem.Frequency; }
        }

        public ushort WakeupCounter
        {
            get { return _dataItem.WakeupCounter; }
        }

        public bool IsSecure
        {
            get { return _dataItem.IsSubstituted; }
        }

        public int Delta
        {
            get { return _dataItem.Delta; }
        }

        public byte SourceHopId
        {
            get { return _dataItem.SrcHop; }
        }

        public byte DestHopId
        {
            get { return _dataItem.DestHop; }
        }

        public System.DateTime CreatedAt
        {
            get { return _dataItem.CreatedAt; }
        }

        public byte Rssi
        {
            get { return _dataItem.Rssi; }
        }

        public ZnifferDataItem(DataItem dataItem, SecurityManager securityManager)
        {
            _dataItem = dataItem;
            _payload = _dataItem.Payload;
            if (_dataItem.GetCipherVersion() >= 0)
            {
                var cipher = _dataItem.Decrypt(securityManager);
                if (cipher.IsDecrypted)
                {
                    _originalPayload = cipher.Payload;
                    NetworkKey = cipher.Key;
                    if (cipher.S2Extensions != null)
                    {
                        Extensions = cipher.S2Extensions.ExtensionsList.
                            Concat(cipher.S2Extensions.EncryptedExtensionsList).
                            Select(x => new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION.TVG1()
                            {
                                extensionLength = x.extensionLength.Value,
                                extension = x.extension,
                                properties1 = new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION.TVG1.Tproperties1()
                                {
                                    critical = x.properties1.critical,
                                    moreToFollow = x.properties1.moreToFollow,
                                    type = x.properties1.type
                                }
                            }).
                            ToList();
                    }
                }
                else
                {
                    _originalPayload = _dataItem.LastPayload;
                }
            }
            else
            {
                //just copy payload as not encrypted
                _originalPayload = _payload;
            }
        }
    }
}
