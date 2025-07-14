// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.Devices;
using Utils;
using System;

namespace Zats.Services
{
    public class MultiChannelAssociationSupportModify : MultiChannelAssociationSupport
    {
        private AssociationSupportDataModify AssociationSupportData { get; set; }

        //private readonly string CommandClassName = "Multi Channel Association CC";
        //private string EndPointName = "";
        //private readonly int LogLevel = 0;
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public MultiChannelAssociationSupportModify(AssociationSupportDataModify associationSupportData)
            : base((AssociationSupportDataModify)associationSupportData)
        {
            MinCCVersion = 2;
            MaxCCVersion = 4;
            AssociationSupportData = associationSupportData;
            //LogLevel = associationSupportData.LogLevel;
        }

        protected override void OnHandledDelayed(DataReceivedUnit ou)
        {
            ou.SetNextActionItems();
            if (DstNode.EndPointId == ReceivedAchData.DstNode.EndPointId)
            {
                var sendDstNode = ReceivedAchData.DstNode;
                var sendSrcNode = ReceivedAchData.SrcNode;
                byte[] command = ReceivedAchData.Command;
                var scheme = (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme;
                bool isResponseAllowed = IsResponseAllowed(Network, command, scheme, Secure);

                ResultForSv = SvResultNames.Undefined;
                if (isResponseAllowed && command != null && command.Length > 1 && AssociationSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_GET.ID)
                    {
#if REPORT
                        COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_GET cmd = command;
                        AssociationGroupEx requestedGroup = AssociationSupportData.AssociationGroups.Find(group => group.GroupId == cmd.groupingIdentifier);

                        if (requestedGroup == null)
                        {
                            requestedGroup = AssociationSupportData.AssociationGroups[0];
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
                        IActionItem[] actionItems = PrepareMCAssociationReports(command);
                        ou.SetNextActionItems(actionItems);
                    }
                    else if (command[1] == COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_GROUPINGS_GET.ID)
                    {
                        rpt = new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_GROUPINGS_REPORT()
                        {
                            supportedGroupings = Convert.ToByte(AssociationSupportData.AssociationGroups.Count)
                        };
                    }
                    else if (command[1] == COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REMOVE.ID)
                    {
                        ResultForSv = HandleMultiChannelAssociationRemove(command);
                    }
                    else if (command[1] == COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_SET.ID)
                    {
                        ResultForSv = HandleMultiChannelAssociationSet(command);
                    }

                    if (rpt != null)
                    {
                        ApiOperation sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command);
                }
            }
        }

        private IActionItem[] PrepareMCAssociationReports(byte[] command)
        {
            COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_GET cmd = command;
            AssociationGroupExModify requestedGroup = AssociationSupportData.AssociationGroups.Find(group => group.GroupId == cmd.groupingIdentifier);

            if (requestedGroup == null)
            {
                requestedGroup = AssociationSupportData.AssociationGroups[0];
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

            Action<List<byte>, AssociationGroupExModify> CheckAndCreateFrameHeader = (curReportBytes, reqGroup) =>
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
            for (int reportsToFollow = allReportsBytesList.Count - 1; reportsToFollow >= 0; reportsToFollow--)
            {
                byte[] currentReport = allReportsBytesList[allReportsBytesList.Count - reportsToFollow - 1];
                currentReport[4] = (byte)reportsToFollow;
                actionItems.Add(new SendDataExOperation(_network, ReceivedAchData.DstNode, ReceivedAchData.SrcNode, currentReport, TxOptions, TxSecOptions, (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme, TxOptions2));
            }
            return actionItems.ToArray();
        }

        private SvResultNames HandleMultiChannelAssociationSet(byte[] command)
        {
            SvResultNames success = SvResultNames.Successful;

            COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_SET cmd = command;
            AssociationGroupExModify requestedGroup = AssociationSupportData.AssociationGroups.Find(group => group.GroupId == cmd.groupingIdentifier);

            if (requestedGroup != null)
            {
                foreach (byte associateNodeId in cmd.nodeId)
                {
                    if (requestedGroup.AssociatedNodeIds.Count == requestedGroup.MaxNodesSupported)
                    {
                        success = SvResultNames.Failed;
                        break;
                    }

                    AssociationGroupExModify.NodeIdEx nodeIdEx = requestedGroup.AssociatedNodeIds.Find(id => id.NodeId == associateNodeId && !id.IsMultiChannel);
                    if (nodeIdEx == null)
                    {
                        requestedGroup.AssociatedNodeIds.Add(new AssociationGroupExModify.NodeIdEx(associateNodeId, 0x00, false));
                    }
                }

                foreach (COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_SET.TVG tvg in cmd.vg)
                {
                    if (requestedGroup.AssociatedNodeIds.Count == requestedGroup.MaxNodesSupported)
                    {
                        success = SvResultNames.Failed;
                        break;
                    }

                    AssociationGroupExModify.NodeIdEx nodeIdEx = requestedGroup.AssociatedNodeIds.Find(
                        id => id.NodeId == tvg.multiChannelNodeId && id.EndPoint == (tvg.properties1.endPoint | tvg.properties1.bitAddress << 7));
                    if (nodeIdEx == null)
                    {
                        if (Version > 2 || tvg.properties1.endPoint != 0x00)
                        {
                            requestedGroup.AssociatedNodeIds.Add(new AssociationGroupExModify.NodeIdEx(tvg.multiChannelNodeId, (byte)(tvg.properties1.endPoint | tvg.properties1.bitAddress << 7), true));
                        }
                    }
                }
            }
            else
            {
                success = SvResultNames.Failed;
            }
            return success;
        }

        private SvResultNames HandleMultiChannelAssociationRemove(byte[] command)
        {
            SvResultNames success = SvResultNames.Failed;

            COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REMOVE cmd = command;
            AssociationGroupExModify requestedGroup = AssociationSupportData.AssociationGroups.Find(group => group.GroupId == cmd.groupingIdentifier);

            if (requestedGroup != null)
            {
                if (cmd.nodeId.Count != 0)
                {
                    foreach (byte associateNodeId in cmd.nodeId)
                    {
                        AssociationGroupExModify.NodeIdEx nodeIdEx = requestedGroup.AssociatedNodeIds.Find(id => id.NodeId == associateNodeId && !id.IsMultiChannel);
                        if (nodeIdEx != null)
                        {
                            requestedGroup.AssociatedNodeIds.Remove(nodeIdEx);
                        }
                    }
                    success = SvResultNames.Successful;
                }

                if (cmd.vg.Count != 0)
                {
                    foreach (COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REMOVE.TVG tvg in cmd.vg)
                    {
                        AssociationGroupExModify.NodeIdEx nodeIdEx = requestedGroup.AssociatedNodeIds.Find(
                            id => id.NodeId == tvg.multiChannelNodeId && id.EndPoint == (tvg.properties1.endPoint | tvg.properties1.bitAddress << 7));
                        if (nodeIdEx != null)
                        {
                            requestedGroup.AssociatedNodeIds.Remove(nodeIdEx);
                        }
                    }
                    success = SvResultNames.Successful;
                }

                if (cmd.nodeId.Count == 0 && cmd.vg.Count == 0)
                {
                    requestedGroup.AssociatedNodeIds.Clear();
                    success = SvResultNames.Successful;
                }
            }
            else if (cmd.groupingIdentifier == 0x00)
            {
                foreach (AssociationGroupExModify group in AssociationSupportData.AssociationGroups)
                {
                    if (cmd.nodeId.Count != 0)
                    {
                        foreach (byte associateNodeId in cmd.nodeId)
                        {
                            AssociationGroupExModify.NodeIdEx nodeIdEx = group.AssociatedNodeIds.Find(id => id.NodeId == associateNodeId && !id.IsMultiChannel);
                            if (nodeIdEx != null)
                            {
                                group.AssociatedNodeIds.Remove(nodeIdEx);
                            }
                        }
                    }

                    if (cmd.vg.Count != 0)
                    {
                        foreach (COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REMOVE.TVG tvg in cmd.vg)
                        {
                            AssociationGroupExModify.NodeIdEx nodeIdEx = group.AssociatedNodeIds.Find(
                                id => id.NodeId == tvg.multiChannelNodeId && id.EndPoint == (tvg.properties1.endPoint | tvg.properties1.bitAddress << 7));
                            if (nodeIdEx != null)
                            {
                                group.AssociatedNodeIds.Remove(nodeIdEx);
                            }
                        }
                    }

                    if (cmd.nodeId.Count == 0 && cmd.vg.Count == 0)
                    {
                        group.AssociatedNodeIds.Clear();
                    }
                }
                success = SvResultNames.Successful;
            }
            return success;
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
            if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != Id)
            {
                supervisionReport.status = (byte)SvStatusNames.NoSupport;
                return;
            }

            supervisionReport.status = (byte)SvStatusNames.Fail;  // FAIL as default
            switch (supervisionGet.encapsulatedCommand[1])
            {
                case COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte)SvStatusNames.Success;
                    break;
                case COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REMOVE.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte)SvStatusNames.Success;
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }
    }
}
