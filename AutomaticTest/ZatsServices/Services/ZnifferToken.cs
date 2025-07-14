// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using Utils;
using Utils.Threading;
using ZWave;
using ZWave.ZnifferApplication;
using ZWave.ZnifferApplication.Operations;

namespace Zats.Services
{
    public class ZnifferToken
    {
        public class ExpectData : _ActionToken
        {
            public SecurityManager SecurityManager { get; set; }
            public ExpectData(ActionToken token, SecurityManager securityManager)
                : base(token)
            {
                SecurityManager = securityManager;
                Token.CustomObject = new ConcurrentList<FilterObject>();
            }

            public ExpectData SetUpAckWatch(Node? sourceNodeId, Node? destNodeId)
            {
                return SetUpWatch(sourceNodeId, destNodeId, null, null, HeaderTypes.TRANSFER_ACKNOWLEDGE, null, false);
            }

            public ExpectData SetUpAckWatch(Node? sourceNodeId, Node? destNodeId, byte[] homeId, bool? isLowPower)
            {
                return SetUpWatch(sourceNodeId, destNodeId, homeId, null, HeaderTypes.TRANSFER_ACKNOWLEDGE, isLowPower, false);
            }

            public ExpectData SetUpAllAckWatch(Node? sourceNodeId, Node? destNodeId)
            {
                return SetUpWatch(sourceNodeId, destNodeId, null, null, HeaderTypes.ALL_ACKNOWLEDGE, null, false);
            }

            public ExpectData SetUpWatch(Node? sourceNodeId, Node? destNodeId, byte[] cmd)
            {
                return SetUpWatch(sourceNodeId, destNodeId, null, cmd, null, null, false);
            }

            public ExpectData SetUpWatch(Node? sourceNodeId, Node? destNodeId, byte[] cmd, byte[] frameType, bool compareCmdPayload)
            {
                return SetUpWatch(sourceNodeId, destNodeId, null, cmd, frameType, null, compareCmdPayload);
            }

            public ExpectData SetUpWatch(byte[] cmd, byte[] frameType, bool? isLowPower, bool compareCmdPayload)
            {
                return SetUpWatch(null, null, null, cmd, frameType, isLowPower, compareCmdPayload);
            }

            public ExpectData SetUpWatch(Node? sourceNodeId, Node? destNodeId, byte[] homeId, byte[] cmd, byte[] frameType, bool? isLowPower, bool compareCmdPayload)
            {
                return SetUpWatch(sourceNodeId, destNodeId, homeId, cmd, frameType, isLowPower, compareCmdPayload, null);
            }

            public ExpectData SetUpWatch(Node? sourceNodeId, Node? destNodeId, byte[] homeId, byte[] cmd, byte[] frameType, bool? isLowPower, bool compareCmdPayload, int expectedCount)
            {
                return SetUpWatch(sourceNodeId, destNodeId, homeId, cmd, frameType, isLowPower, compareCmdPayload, expectedCount, null);
            }

            public ExpectData SetUpWatch(Node? sourceNodeId, Node? destNodeId, byte? sourceHopId, byte? destHopId, byte[] homeId, byte[] cmd, byte[] frameType)
            {
                return SetUpWatch(sourceNodeId, destNodeId, sourceHopId, destHopId, homeId, cmd, frameType, null, false, 1, null);
            }

            public ExpectData SetUpWatch(Node? sourceNodeId, Node? destNodeId, byte[] homeId, byte[] cmd, byte[] frameType, bool? isLowPower, bool compareCmdPayload, bool? isSecure)
            {
                return SetUpWatch(sourceNodeId, destNodeId, homeId, cmd, frameType, isLowPower, compareCmdPayload, 1, isSecure);
            }

            public ExpectData SetUpWatch(Node? sourceNodeId, Node? destNodeId, byte[] homeId, byte[] cmd, byte[] frameType, bool? isLowPower, bool compareCmdPayload, int expectedCount, bool? isSecure)
            {
                return SetUpWatch(sourceNodeId, destNodeId, null, null, homeId, cmd, frameType, isLowPower, compareCmdPayload, expectedCount, isSecure);
            }

            public ExpectData SetUpWatch(Node? sourceNodeId, Node? destNodeId, byte? sourceHopId, byte? destHopId, byte[] homeId, byte[] cmd, byte[] frameType, bool? isLowPower, bool compareCmdPayload, int expectedCount, bool? isSecure)
            {
                FilterObject filter = new FilterObject();
                return SetUpFilter(filter, null, sourceNodeId, destNodeId, sourceHopId, destHopId, null, homeId, cmd, frameType, isLowPower, compareCmdPayload, expectedCount);
            }

            public ExpectData SetUpWatch(Node? sourceNodeId, Node? destNodeId, byte? sourceHopId, byte? destHopId, byte[] repeaters, byte[] homeId, byte[] cmd, byte[] frameType)
            {
                FilterObject filter = new FilterObject();
                return SetUpFilter(filter, null, sourceNodeId, destNodeId, sourceHopId, destHopId, repeaters, homeId, cmd, frameType, null, false, 0);
            }

            public ExpectData SetUpWatch(Func<ZnifferDataItem, bool> filterCallback, Node? sourceNodeId, Node? destNodeId, byte? sourceHopId, byte? destHopId, byte[] repeaters, byte[] homeId, byte[] cmd, byte[] frameType)
            {
                FilterObject filter = new FilterObject();
                return SetUpFilter(filter, (x) => filterCallback(new ZnifferDataItem(x, SecurityManager)), sourceNodeId, destNodeId, sourceHopId, destHopId, repeaters, homeId, cmd, frameType, null, false, 0);
            }

            private ExpectData SetUpFilter(FilterObject filter, Func<DataItem, bool> filterCallback, Node? sourceNode, Node? destNode, byte? sourceHopId, byte? destHopId, byte[] repeaters, byte[] homeId, byte[] cmd, byte[] frameType, bool? isLowPower, bool compareCmdPayload, int expectedCount)
            {
                if (filterCallback != null)
                {
                    filter.FilterCallback = filterCallback;
                    filter.IsDisplayFilterCallback = true;
                }
                if (sourceNode != null)
                {
                    filter.FilterSource = new[] { sourceNode.Value.Id };
                    filter.IsDisplaySource = true;
                }
                if (destNode != null)
                {
                    filter.FilterDestination = new[] { destNode.Value.Id };
                    filter.IsDisplayDestination = true;
                }

                if (sourceHopId != null)
                {
                    filter.FilterSourceHop = new byte[] { (byte)sourceHopId };
                    filter.IsDisplaySourceHop = true;
                }

                if (destHopId != null)
                {
                    filter.FilterDestinationHop = new byte[] { (byte)destHopId };
                    filter.IsDisplayDestinationHop = true;
                }

                if (repeaters != null)
                {
                    var list = new List<ByteIndex[]>();
                    list.Add(new ByteIndex[repeaters.Length]);
                    for (int i = 0; i < repeaters.Length; i++)
                    {
                        list[0][i] = new ByteIndex(repeaters[i], 0xFF, Presence.Value);
                    }
                    filter.FilterRepeaters = list;
                    filter.IsDisplayRepeaters = true;
                }

                if (homeId != null)
                {
                    var list = new List<ByteIndex[]>();
                    list.Add(new ByteIndex[4]);
                    for (int i = 0; i < 4; i++)
                    {
                        if (homeId.Length > i)
                        {
                            list[0][i] = new ByteIndex(homeId[i], 0xFF, Presence.Value);
                        }
                        else
                        {
                            list[0][i] = ByteIndex.AnyValue;
                        }
                    }
                    filter.FilterHomeId = list;
                    filter.IsDisplayHomeId = true;
                }

                if (isLowPower != null)
                {
                    filter.FilterIsLow = isLowPower;
                    filter.IsDisplayIsLow = true;
                }

                if (cmd != null && cmd.Length > 0)
                {
                    if (filter.FilterPayload == null)
                    {
                        filter.FilterPayload = new FilterPayloadObject();
                    }
                    filter.FilterPayload.Indexes = new List<ByteIndex[]>();
                    int length = (compareCmdPayload || cmd.Length < 2) ? cmd.Length : 2;
                    filter.FilterPayload.Indexes.Add(new ByteIndex[length]);
                    for (int i = 0; i < length; i++)
                    {
                        if (i == 1 && cmd[0] == COMMAND_CLASS_TRANSPORT_SERVICE_V2.ID)
                        {
                            filter.FilterPayload.Indexes[0][i] = new ByteIndex(cmd[i], 0xF8, Presence.Value);
                        }
                        else
                        {
                            filter.FilterPayload.Indexes[0][i] = new ByteIndex(cmd[i], 0xFF, Presence.Value);
                        }
                    }
                    filter.IsDisplayPayload = true;
                }

                if (expectedCount > 0)
                    filter.ExpectedFramesCount = expectedCount;

                filter.FilterData = frameType;
                ((ConcurrentList<FilterObject>)Token.CustomObject).Add(filter);
                return this;
            }

            public ExpectData SetUpWatch(byte[] cmd)
            {
                return SetUpWatch(null, null, null, cmd, null, null, false);
            }

            public ExpectData SetUpWatch(byte[] cmd, byte[] frameType, bool compareCmdPayload)
            {
                return SetUpWatch(null, null, null, cmd, frameType, null, compareCmdPayload);
            }

            public ExpectData SetUpWatchS2(Node? sourceNodeId, Node? destNodeId, byte[] homeId, byte[] cmd, byte? extType, bool? isExtEncrypted, int expectedCount)
            {
                var filter = new FilterObject();
                if (filter.FilterPayload == null)
                {
                    filter.FilterPayload = new FilterPayloadObject();
                }
                filter.FilterPayload.CipherExtensionType = extType;
                filter.FilterPayload.CipherExtensionEncrypted = isExtEncrypted;
                return SetUpFilter(filter, null, sourceNodeId, destNodeId, null, null, null, homeId, cmd,
                    null, null, false, expectedCount);
            }

            public bool IsStateActive
            {
                get
                {
                    return Token.IsStateActive;
                }
            }

            public ZnifferResult.ExpectData WaitCompletedSignal()
            {
                return new ZnifferResult.ExpectData((ExpectDataResult)Token.WaitCompletedSignal(), SecurityManager);
            }
        }

        public class CollectData : _ActionToken
        {
            public SecurityManager SecurityManager { get; set; }
            public CollectData(ActionToken token, SecurityManager securityManager)
                : base(token)
            {
                SecurityManager = securityManager;
                Token.CustomObject = new ConcurrentList<FilterObject>();
            }

            public void ClearWatchList()
            {
                (Token.CustomObject as ConcurrentList<FilterObject>).Clear();
            }

            public CollectData SetUpAckWatch(Node? sourceNodeId, Node? destNodeId)
            {
                return SetUpWatch(sourceNodeId, destNodeId, null, null, HeaderTypes.TRANSFER_ACKNOWLEDGE, null, false);
            }

            public CollectData SetUpAckWatch(Node? sourceNodeId, Node? destNodeId, byte[] homeId, bool? isLowPower)
            {
                return SetUpWatch(sourceNodeId, destNodeId, homeId, null, HeaderTypes.TRANSFER_ACKNOWLEDGE, isLowPower, false);
            }

            public CollectData SetUpAllAckWatch(Node? sourceNodeId, Node? destNodeId)
            {
                return SetUpWatch(sourceNodeId, destNodeId, null, null, HeaderTypes.ALL_ACKNOWLEDGE, null, false);
            }

            public CollectData SetUpWatch(Node? sourceNodeId, Node? destNodeId, byte[] cmd, byte[] frameType, bool compareCmdPayload)
            {
                return SetUpWatch(sourceNodeId, destNodeId, null, cmd, frameType, null, compareCmdPayload);
            }

            public CollectData SetUpWatch(byte[] cmd, byte[] frameType, bool? isLowPower, bool compareCmdPayload)
            {
                return SetUpWatch(null, null, null, cmd, frameType, isLowPower, compareCmdPayload);
            }

            public CollectData SetUpWatch(Node? sourceNodeId, Node? destNodeId, byte[] homeId, byte[] cmd, byte[] frameType, bool? isLowPower, bool compareCmdPayload)
            {
                return SetUpWatch(sourceNodeId, destNodeId, homeId, cmd, frameType, isLowPower, compareCmdPayload, null);
            }

            public CollectData SetUpWatch(Node? sourceNodeId, Node? destNodeId, byte[] homeId, byte[] cmd, byte[] frameType, bool? isLowPower, bool compareCmdPayload, bool? isSecure)
            {
                return SetUpWatch(sourceNodeId, destNodeId, null, null, null, homeId, cmd, frameType, isLowPower, compareCmdPayload, null);
            }

            public CollectData SetUpWatch(Node? sourceNode, Node? destNode, byte? sourceHopId, byte? destHopId, byte[] repeaters, byte[] homeId, byte[] cmd, byte[] frameType, bool? isLowPower, bool compareCmdPayload, bool? isSecure)
            {
                FilterObject filter = new FilterObject();
                if (sourceNode != null)
                {
                    filter.FilterSource = new[] { sourceNode.Value.Id };
                    filter.IsDisplaySource = true;
                }
                if (destNode != null)
                {
                    filter.FilterDestination = new[] { destNode.Value.Id };
                    filter.IsDisplayDestination = true;
                }

                if (sourceHopId != null)
                {
                    filter.FilterSourceHop = new byte[] { (byte)sourceHopId };
                    filter.IsDisplaySourceHop = true;
                }

                if (destHopId != null)
                {
                    filter.FilterDestinationHop = new byte[] { (byte)destHopId };
                    filter.IsDisplayDestinationHop = true;
                }

                if (repeaters != null)
                {
                    var list = new List<ByteIndex[]>();
                    list.Add(new ByteIndex[repeaters.Length]);
                    for (int i = 0; i < repeaters.Length; i++)
                    {
                        list[0][i] = new ByteIndex(repeaters[i], 0xFF, Presence.Value);
                    }
                    filter.FilterRepeaters = list;
                    filter.IsDisplayRepeaters = true;
                }

                if (homeId != null)
                {
                    var list = new List<ByteIndex[]>();
                    list.Add(new ByteIndex[4]);
                    for (int i = 0; i < 4; i++)
                    {
                        if (homeId.Length > i)
                        {
                            list[0][i] = new ByteIndex(homeId[i], 0xFF, Presence.Value);
                        }
                        else
                        {
                            list[0][i] = ByteIndex.AnyValue;
                        }
                    }
                    filter.FilterHomeId = list;
                    filter.IsDisplayHomeId = true;
                }

                if (isLowPower != null)
                {
                    filter.FilterIsLow = isLowPower;
                    filter.IsDisplayIsLow = true;
                }

                if (cmd != null && cmd.Length > 0)
                {
                    filter.FilterPayload = new FilterPayloadObject();
                    filter.FilterPayload.Indexes = new List<ByteIndex[]>();
                    int length = (compareCmdPayload || cmd.Length < 2) ? cmd.Length : 2;
                    filter.FilterPayload.Indexes.Add(new ByteIndex[length]);
                    for (int i = 0; i < length; i++)
                    {
                        filter.FilterPayload.Indexes[0][i] = new ByteIndex(cmd[i], 0xFF, Presence.Value);
                    }
                    filter.IsDisplayPayload = true;
                }

                filter.FilterData = frameType;
                ((ConcurrentList<FilterObject>)Token.CustomObject).Add(filter);
                return this;
            }

            public CollectData SetUpWatch(byte[] cmd, byte[] frameType, bool compareCmdPayload)
            {
                return SetUpWatch(null, null, null, cmd, frameType, null, compareCmdPayload);
            }

            public ZnifferResult.CollectData WaitCompletedSignal()
            {
                Token.SetCompletedSignal();
                Token.Complete();
                return new ZnifferResult.CollectData((CollectDataResult)Token.WaitCompletedSignal(), SecurityManager);
            }

            public bool IsStateActive
            {
                get
                {
                    return Token.IsStateActive;
                }
            }

            public ZnifferResult.CollectData GetCurrentResultAndReset()
            {
                CollectDataResult result = (CollectDataResult)Token.Result;
                CollectDataResult ret = new CollectDataResult(result.MaxCapacity);
                ret.DataItems = result.ReturnAndReset();
                return new ZnifferResult.CollectData(ret, SecurityManager);
            }

            public void SetUpWatch(Node? sourceNodeId, Node? destNodeId, byte[] cmd)
            {
                SetUpWatch(sourceNodeId, destNodeId, cmd, null, false);
            }
        }
    }
}
