// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
using System.Collections.Generic;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.Devices;
using Utils;
using System;
using ZWave.Xml.Application;
using static UicApplication.Clusters.SmartStart.List;
using static Zats.Services.AssociationGroupEx;
using System.Threading;

namespace Zats.Services
{
    [CommandClassName("Multi Channel Association Command Class")]
    [CommandClassId(COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.ID)]
    [MinVersion(2)]
    [MaxVersion(5)] // Security Learning is not yet implemented
                    // (to be used in MCAv4+ for sending controlling
                    // and in MCAv5+ also for sending supporting commands
                    // to associated destinations).
    public class MultiChannelAssociationSupport : AssociationSupportBase
    {
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return AssociationSupportData; }
            set { AssociationSupportData = (AssociationSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public MultiChannelAssociationSupport()
            : this(new AssociationSupportData() { })
        { }

        public MultiChannelAssociationSupport(AssociationSupportData associationSupportData)
            : base(COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.ID)
        {
            AssociationSupportData = associationSupportData;
        }

        protected override void OnHandledDelayed(DataReceivedUnit ou)
        {
            ou.SetNextActionItems();

            if (MatchesDestinationEndPoint())
            {
                var sendDstNode = new NodeTag(ReceivedAchData.DstNode.Id, DstNode.EndPointId);
                var sendSrcNode = ReceivedAchData.SrcNode;
                byte[] command = ReceivedAchData.Command;
                var scheme = (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme;
                bool isHandlingAllowed = IsHandlingAllowed(Network, command, scheme, Secure);

                ResultForSv = SvResultNames.Undefined;
                if (isHandlingAllowed && command != null && command.Length > 1 && AssociationSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_GET.ID && !IsSupervised())
                    {
                        if (IsAnsweringAllowed())
                        {
#if REPORT
                            COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_GET cmd = command;
                            AssociationGroupEx requestedGroup = AssociationSupportData.AssociationGroups.Find(group => group.GroupId == cmd.groupingIdentifier);

                            if (requestedGroup == null)
                            {
                                requestedGroup = AssociationSupportData.AssociationGroups[0]; // Empty list is not handled!
                            }
                            // It is not possible to implement ReportsToFollow easier, because the constructor 
                            // of MULTI_CHANNEL_ASSOCIATION_REPORT adds the Marker byte into *EACH* report. 
                            // The Marker member is private and not accessible.
                            // The default implementation below therefore does not support ReportsToFollow.
                            /*COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V2.MULTI_CHANNEL_ASSOCIATION_REPORT rpt = new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V2.MULTI_CHANNEL_ASSOCIATION_REPORT()
                            {
                                groupingIdentifier = requestedGroup.GroupId,
                                nodeId = requestedGroup.GetAssociatedNodeIds(),
                                maxNodesSupported = requestedGroup.MaxNodesSupported,
                                reportsToFollow = 0,
                                vg = requestedGroup.GetTVGs()
                            };*/


                            List<IActionItem> actionItems = new List<IActionItem>();
                            List<byte[]> allReportsBytesList = new List<byte[]>();
                            List<COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V2.MULTI_CHANNEL_ASSOCIATION_REPORT.TVG> vg = requestedGroup.GetTVGs();
                            List<byte> currentReportBytes = new List<byte>();
                            const int maxPayLoad = 24;  // Leave room for the Marker and at last one Two-byte Node ID

                            // For PC Controller tests only (set Group.MaxNodeSupported to >60 for this test):
                            // 8E 01 01  01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 10 11 12 13 14 15 16 
                            // 8E 01 01  17 18 19 1A 1B 1C 1D 1E 1F 20 00 04 04 05 05 06 06 07 07 08 08 09 09 
                            // 8E 01 01  00 0A 0A 0B 0B 0C 0C 0D 0D 0E 0E 0F 0F 10 10 11 11 12 12 13 13 14 14 15 15

                            // Marker at frame begin (maxPayLoad=24)
                            // 8E 01 01  01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 10 11 12 13 14 15 16 17 18 19 1A 
                            // 8E 01 01  1B 1C 1D 1E 1F 20 21 22 23 24 25 26 00 04 04 05 05 06 06 07 07 08 08 09 09 

                            Action<List<byte>, AssociationGroupEx> CheckAndCreateFrameHeader = (curReportBytes, reqGroup) => 
                            {
                                if (curReportBytes.Count == 0)
                                {
                                    curReportBytes.Add(COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.ID);
                                    curReportBytes.Add(COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REPORT.ID);
                                    curReportBytes.Add(reqGroup.GroupId);
                                    curReportBytes.Add(reqGroup.MaxNodesSupported);
                                    curReportBytes.Add(0); // leave reportsToFollow empty
                                }
                            };

                            // Create 1st Frame Header
                            CheckAndCreateFrameHeader(currentReportBytes, requestedGroup);
                            // Add all (Non-Multi Channel) NodeIDs
                            foreach (byte node in requestedGroup.GetAssociatedNodeIds())
                            {
                                CheckAndCreateFrameHeader(currentReportBytes, requestedGroup);
                                currentReportBytes.Add(node);
                                // If frame is full, add it to frame list
                                if (currentReportBytes.Count >= maxPayLoad)
                                {
                                    allReportsBytesList.Add(currentReportBytes.ToArray());
                                    currentReportBytes.Clear();
                                }
                            }
                            // Check for Marker necessity
                            if (vg.Count > 0)
                            {
                                CheckAndCreateFrameHeader(currentReportBytes, requestedGroup);
                                currentReportBytes.Add(0x00); // Marker
                            }
                            // Add all Multi Channel NodeIDs
                            foreach (COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V2.MULTI_CHANNEL_ASSOCIATION_REPORT.TVG currentVg in vg)
                            {
                                if (currentReportBytes.Count == 0)
                                {
                                    CheckAndCreateFrameHeader(currentReportBytes, requestedGroup);
                                    currentReportBytes.Add(0x00); // Marker
                                }
                                currentReportBytes.Add(currentVg.multiChannelNodeId);
                                currentReportBytes.Add(currentVg.properties1);
                                // If frame is full, add it to frame list
                                if (currentReportBytes.Count >= maxPayLoad)
                                {
                                    allReportsBytesList.Add(currentReportBytes.ToArray());
                                    currentReportBytes.Clear();
                                }
                            }
                            // add last started frame to frame list
                            if (currentReportBytes.Count > 0)
                            {
                                allReportsBytesList.Add(currentReportBytes.ToArray());
                            }

                            // fill all reportsToFollow fields and create an IActionItem from each frame data
                            for (int reportsToFollow = allReportsBytesList.Count - 1; reportsToFollow >= 0;  reportsToFollow--)
                            {
                                byte[] currentReport = allReportsBytesList[allReportsBytesList.Count - reportsToFollow - 1];
                                currentReport[4] = (byte) reportsToFollow;
                                actionItems.Add(new SendDataExOperation(sendDstNode, sendSrcNode, currentReport, TxOptions, TxSecOptions, scheme, TxOptions2));
                            }
#endif
                            IActionItem[] actionItems = PrepareMCAssociationReportsCmd(command);
                            ou.SetNextActionItems(actionItems);
                        }
                    }
                    else if (command[1] == COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_GROUPINGS_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareMCAssociationGroupingsReport();
                    }
                    else if (command[1] == COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REMOVE.ID)
                    {
                        ResultForSv = HandleMultiChannelAssociationRemoveCmd(command, sendSrcNode);
                    }
                    else if (command[1] == COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_SET.ID)
                    {
                        ResultForSv = HandleMultiChannelAssociationSetCmd(command, sendSrcNode);
                    }

                    if (rpt != null && IsAnsweringAllowed())
                    {
                        ApiOperation sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command, sendDstNode.EndPointId);
                }
            }
        }

        private IActionItem[] PrepareMCAssociationReportsCmd(byte[] command)
        {
            COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_GET cmd = command;
            byte requestedGroupId = cmd.groupingIdentifier;

            List<byte[]> reportList = PrepareMCAssociationReports(requestedGroupId);
            IActionItem[] actionItems = new IActionItem[reportList.Count];
            for (int i = 0; i < reportList.Count; i++)
            {
                actionItems[i] = new SendDataExOperation(_network, ReceivedAchData.DstNode, ReceivedAchData.SrcNode, reportList[i], TxOptions, TxSecOptions, (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme, TxOptions2);
            }
            return actionItems;
        }

        private SvResultNames HandleMultiChannelAssociationSet(byte requestedGroupId, IList<byte> nodeId,
                List<COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_SET.TVG> vg, bool updateUI, NodeTag srcNode)
        {
            SvResultNames success = SvResultNames.Successful;

            AssociationGroupEx requestedGroup = AssociationSupportData.AssociationGroups.Find(group => group.GroupId == requestedGroupId);
            if (requestedGroup != null)
            {
                foreach (byte associateNodeId in nodeId)
                {
                    if (requestedGroup.AssociatedNodeIds.Count == requestedGroup.MaxNodesSupported)
                    {
                        success = SvResultNames.Failed;
                        break;
                    }

                    AssociationGroupEx.NodeIdEx nodeIdEx = requestedGroup.AssociatedNodeIds.Find(id => id.NodeId == associateNodeId && !id.IsMultiChannel);
                    if (nodeIdEx == null)
                    {
                        requestedGroup.AssociatedNodeIds.Add(new AssociationGroupEx.NodeIdEx(associateNodeId, 0x00, false));
                    }
                }

                foreach (COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_SET.TVG tvg in vg)
                {
                    if (requestedGroup.AssociatedNodeIds.Count == requestedGroup.MaxNodesSupported)
                    {
                        success = SvResultNames.Failed;
                        break;
                    }

                    AssociationGroupEx.NodeIdEx nodeIdEx = requestedGroup.AssociatedNodeIds.Find(
                        id => id.NodeId == tvg.multiChannelNodeId && id.EndPoint == (tvg.properties1.endPoint | tvg.properties1.bitAddress << 7));
                    if (nodeIdEx == null)
                    {
                        if (Version > 2 || tvg.properties1.endPoint != 0x00)
                        {
                            requestedGroup.AssociatedNodeIds.Add(new AssociationGroupEx.NodeIdEx(tvg.multiChannelNodeId, (byte)(tvg.properties1.endPoint | tvg.properties1.bitAddress << 7), true));
                        }
                    }
                }
                // Multi Channel Association has no mandatory Lifeline Reports
                //if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                //{
                //    List<byte[]> reports = PrepareMCAssociationReports(requestedGroupId);
                //    foreach (byte[] r in reports)
                //    {
                //        SendUnsolicitedReport(r, srcNode);
                //    }
                //}
                if (updateUI)
                {
                    SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
                }
            }
            else
            {
                success = SvResultNames.Failed;
            }
            return success;

        }
        private SvResultNames HandleMultiChannelAssociationSetCmd(byte[] command, NodeTag srcNode)
        {
            COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_SET cmd = command;
            return HandleMultiChannelAssociationSet(cmd.groupingIdentifier, cmd.nodeId, cmd.vg, true, srcNode);
        }

        private SvResultNames HandleMultiChannelAssociationRemove(byte requestedGroupId, IList<byte> nodeId,
                List<COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REMOVE.TVG> vg, bool updateUI, NodeTag srcNode)
        {
            SvResultNames success = SvResultNames.Failed;

            AssociationGroupEx requestedGroup = AssociationSupportData.AssociationGroups.Find(group => group.GroupId == requestedGroupId);
            if (requestedGroup != null)
            {
                if (nodeId.Count != 0)
                {
                    foreach (byte associateNodeId in nodeId)
                    {
                        AssociationGroupEx.NodeIdEx nodeIdEx = requestedGroup.AssociatedNodeIds.Find(id => id.NodeId == associateNodeId && !id.IsMultiChannel);
                        if (nodeIdEx != null)
                        {
                            requestedGroup.AssociatedNodeIds.Remove(nodeIdEx);
                        }
                    }
                    success = SvResultNames.Successful;
                }

                if (vg.Count != 0)
                {
                    foreach (COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REMOVE.TVG tvg in vg)
                    {
                        AssociationGroupEx.NodeIdEx nodeIdEx = requestedGroup.AssociatedNodeIds.Find(
                            id => id.NodeId == tvg.multiChannelNodeId && id.EndPoint == (tvg.properties1.endPoint | tvg.properties1.bitAddress << 7));
                        if (nodeIdEx != null)
                        {
                            requestedGroup.AssociatedNodeIds.Remove(nodeIdEx);
                        }
                    }
                    success = SvResultNames.Successful;
                }

                if (nodeId.Count == 0 && vg.Count == 0)
                {
                    requestedGroup.AssociatedNodeIds.Clear();
                    success = SvResultNames.Successful;
                }
            }
            else if (requestedGroupId == 0x00)
            {
                foreach (AssociationGroupEx group in AssociationSupportData.AssociationGroups)
                {
                    if (nodeId.Count != 0)
                    {
                        foreach (byte associateNodeId in nodeId)
                        {
                            AssociationGroupEx.NodeIdEx nodeIdEx = group.AssociatedNodeIds.Find(id => id.NodeId == associateNodeId && !id.IsMultiChannel);
                            if (nodeIdEx != null)
                            {
                                group.AssociatedNodeIds.Remove(nodeIdEx);
                            }
                        }
                    }

                    if (vg.Count != 0)
                    {
                        foreach (COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REMOVE.TVG tvg in vg)
                        {
                            AssociationGroupEx.NodeIdEx nodeIdEx = group.AssociatedNodeIds.Find(
                                id => id.NodeId == tvg.multiChannelNodeId && id.EndPoint == (tvg.properties1.endPoint | tvg.properties1.bitAddress << 7));
                            if (nodeIdEx != null)
                            {
                                group.AssociatedNodeIds.Remove(nodeIdEx);
                            }
                        }
                    }

                    if (nodeId.Count == 0 && vg.Count == 0)
                    {
                        group.AssociatedNodeIds.Clear();
                    }
                }
                success = SvResultNames.Successful;
            }
            if (success == SvResultNames.Successful)
            {
                // Multi Channel Association has no mandatory Lifeline Reports
                //if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                //{
                //    List<byte[]> reports = PrepareMCAssociationReports(requestedGroupId);
                //    foreach (byte[] r in reports)
                //    {
                //        SendUnsolicitedReport(r, srcNode);
                //    }
                //}
                if (updateUI)
                {
                    SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
                }
            }
            return success;
        }
        private SvResultNames HandleMultiChannelAssociationRemoveCmd(byte[] command, NodeTag srcNode)
        {
            COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REMOVE cmd = command;
            return HandleMultiChannelAssociationRemove(cmd.groupingIdentifier, cmd.nodeId, cmd.vg, true, srcNode);
        }

        /*private byte[] EncapData(byte[] data, byte destinationEndPoint)
        {
            if (destinationEndPoint > 0)
            {
                COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP multiChannelCmd = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP
                {
                    commandClass = data[0],
                    command = data[1],
                    parameter = new List<byte>()
                };
                for (int i = 2; i < data.Length; i++)
                {
                    multiChannelCmd.parameter.Add(data[i]);
                }
                multiChannelCmd.properties1.res = 0;
                multiChannelCmd.properties1.sourceEndPoint = 0;
                multiChannelCmd.properties2.bitAddress = 0;
                multiChannelCmd.properties2.destinationEndPoint = destinationEndPoint;
                data = multiChannelCmd;
            }
            return data;
        }*/

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet,
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != CommandClassId)
            {
                supervisionReport.status = (byte) SvStatusNames.NoSupport;
                return;
            }

            supervisionReport.status = (byte) SvStatusNames.Fail;  // FAIL as default
            switch (supervisionGet.encapsulatedCommand[1])
            {
                case COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    }
                    break;
                case COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REMOVE.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    }
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }
    }
}
