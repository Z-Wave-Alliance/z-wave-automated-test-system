// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Threading;
using Utils;
using Utils.UI.Enums;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;
using System.Collections;
using System.Collections.Concurrent;
using static Zats.Services.AssociationGroupEx;
using System.Text;
using ZatsServices_netcore.CommandClassesImpl.Definitions.Interfaces;
using Newtonsoft.Json;

namespace Zats.Services
{
    public class AssociationSupportBase : CommandClassSupport
    {
        public AssociationSupportData AssociationSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return AssociationSupportData; }
            set { AssociationSupportData = (AssociationSupportData)value; }
        }

        /*public AssociationSupportBase()
            : this(new AssociationSupportData() { })
        { }*/

        /*public AssociationSupportBase(AssociationSupportData associationSupportData)
            : base(COMMAND_CLASS_ASSOCIATION.ID)
        {
            AssociationSupportData = associationSupportData;
        }*/
        public AssociationSupportBase(byte commandClassId)
            : base(commandClassId)
        { }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet,
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        { }


        // Common methods for AssociationGroupInfo / Association / MultiChannelAssociation
        public byte[] PrepareGroupNameReport(byte requestedGroupId)
        {
            AssociationGroupEx requestedGroup = AssociationSupportData.AssociationGroups.Find(group => group.GroupId == requestedGroupId);
            byte[] rpt = null;

            if (requestedGroup != null)
            {
                rpt = new COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_NAME_REPORT()
                {
                    groupingIdentifier = requestedGroup.GroupId,
                    lengthOfName = (byte)Encoding.UTF8.GetByteCount(requestedGroup.GroupName),
                    name = Encoding.UTF8.GetBytes(requestedGroup.GroupName)
                };
            }
            return rpt;
        }
        public byte[] PrepareGroupInfoReport(byte requestedGroupId, byte requestedListMode)
        {
            AssociationGroupEx requestedGroup = AssociationSupportData.AssociationGroups.Find(group => group.GroupId == requestedGroupId);
            byte[] rpt = null;

            if (AssociationSupportData.AssociationGroups.Count == 0)
            {
                return rpt;
            }

            if (requestedGroup == null)
            {
                requestedGroup = AssociationSupportData.AssociationGroups[0];
            }

            if (requestedListMode == 0)
            {
                rpt = new COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_INFO_REPORT()
                {
                    properties1 = new COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_INFO_REPORT.Tproperties1()
                    {
                        listMode = requestedListMode,
                        groupCount = 0x01,
                    },
                    vg1 = new List<COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_INFO_REPORT.TVG1>()
                    {
                        new COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_INFO_REPORT.TVG1()
                        {
                            groupingIdentifier = requestedGroup.GroupId,
                            mode = 0,
                            profile1 = requestedGroup.Profile.MSB,
                            profile2 = requestedGroup.Profile.LSB,
                        }
                    }
                };
            }
            else
            {
                List<COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_INFO_REPORT.TVG1> groupList = new List<COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_INFO_REPORT.TVG1>();

                for (int i = 0; i < AssociationSupportData.AssociationGroups.Count; i++)
                {
                    groupList.Add(
                        new COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_INFO_REPORT.TVG1()
                        {
                            groupingIdentifier = AssociationSupportData.AssociationGroups[i].GroupId,
                            mode = 0,
                            profile1 = AssociationSupportData.AssociationGroups[i].Profile.MSB,
                            profile2 = AssociationSupportData.AssociationGroups[i].Profile.LSB
                        });
                }

                rpt = new COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_INFO_REPORT()
                {
                    properties1 = new COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_INFO_REPORT.Tproperties1()
                    {
                        listMode = requestedListMode,
                        groupCount = (byte)groupList.Count
                    },
                    vg1 = groupList
                };
            }
            return rpt;
        }
        public byte[] PrepareGroupCommandListReport(byte requestedGroupId)
        {
            AssociationGroupEx requestedGroup = AssociationSupportData.AssociationGroups.Find(group => group.GroupId == requestedGroupId);
            byte[] rpt = null;

            if (requestedGroup != null)
            {
                rpt = new COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_COMMAND_LIST_REPORT()
                {
                    groupingIdentifier = requestedGroup.GroupId,
                    listLength = (byte)(requestedGroup.CommandClassAndCommandList.Count * 2),
                    command = requestedGroup.GetCommandClassAndCommandIds()
                };
            }
            return rpt;
        }

        public byte[] PrepareAssociationGroupingsReport()
        {
            COMMAND_CLASS_ASSOCIATION.ASSOCIATION_GROUPINGS_REPORT rpt = new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_GROUPINGS_REPORT();
            rpt.supportedGroupings = Convert.ToByte(AssociationSupportData.AssociationGroups.Count);
            return rpt;
        }
        public List<byte[]> PrepareAssociationReports(byte requestedGroupId)
        {
            List<byte[]> reportList = new List<byte[]>();

            if (AssociationSupportData.AssociationGroups.Count == 0)
            {
                return reportList;
            }

            AssociationGroupEx requestedGroup = AssociationSupportData.AssociationGroups.Find(group => group.GroupId == requestedGroupId);
            if (requestedGroup == null)
            {
                requestedGroup = AssociationSupportData.AssociationGroups[0];
            }

            COMMAND_CLASS_ASSOCIATION.ASSOCIATION_REPORT rpt;
            IList<byte> nodeIds = requestedGroup.GetAssociatedNodeIds();
            byte reportsToFollow = (byte)((nodeIds.Count - 1) / 29);     // 29 Bytes per frame are available for the nodeid field

            rpt = new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_REPORT();
            rpt.groupingIdentifier = requestedGroup.GroupId;
            rpt.maxNodesSupported = requestedGroup.MaxNodesSupported;
            foreach (byte node in nodeIds)
            {
                rpt.nodeid.Add(node);
                if (rpt.nodeid.Count == 29)
                {
                    rpt.reportsToFollow = reportsToFollow;
                    reportList.Add(rpt);
                    reportsToFollow--;
                    rpt = new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_REPORT();
                    rpt.groupingIdentifier = requestedGroup.GroupId;
                    rpt.maxNodesSupported = requestedGroup.MaxNodesSupported;
                }
            }
            if (rpt.nodeid.Count != 0 || nodeIds.Count == 0)
            {
                rpt.reportsToFollow = reportsToFollow;
                reportList.Add(rpt);
            }
            return reportList;
        }

        public byte[] PrepareMCAssociationGroupingsReport()
        {
            COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_GROUPINGS_REPORT rpt = new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_GROUPINGS_REPORT();
            rpt.supportedGroupings = Convert.ToByte(AssociationSupportData.AssociationGroups.Count);
            return rpt;
        }
        public List<byte[]> PrepareMCAssociationReports(byte requestedGroupId)
        {
            List<byte[]> reportList = new List<byte[]>();

            if (AssociationSupportData.AssociationGroups.Count == 0)
            {
                return reportList;
            }

            AssociationGroupEx requestedGroup = AssociationSupportData.AssociationGroups.Find(group => group.GroupId == requestedGroupId);
            if (requestedGroup == null)
            {
                requestedGroup = AssociationSupportData.AssociationGroups[0];
            }
            // ZATS 2020: It is not possible to implement ReportsToFollow easier, because the 
            // constructor of MULTI_CHANNEL_ASSOCIATION_REPORT adds the Marker byte into *EACH* report. 
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

            // fill all reportsToFollow fields and create a report from each frame data
            for (int reportsToFollow = allReportsBytesList.Count - 1; reportsToFollow >= 0; reportsToFollow--)
            {
                byte[] currentReport = allReportsBytesList[allReportsBytesList.Count - reportsToFollow - 1];
                currentReport[4] = (byte)reportsToFollow;
                reportList.Add(currentReport);
            }
            return reportList;
        }

        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is AssociationSupportData newData)
            {
                List<byte[]> associationReports = new List<byte[]>();
                bool isMCA = false;

                // Initially check all current and provided AG for MCA support to create the reports in the correct type later.
                // EDE-287: Send MCA Report anyway if the last EP association is removed in favor of a Node ID association.
                foreach (AssociationGroupEx currentAg in AssociationSupportData.AssociationGroups)
                {
                    foreach (NodeIdEx nodeIdEx in currentAg.AssociatedNodeIds)
                    {
                        if (nodeIdEx.IsMultiChannel)
                        {
                            isMCA = true;
                        }
                    }
                }
                foreach (AssociationGroupEx newAg in newData.AssociationGroups)
                {
                    foreach (NodeIdEx nodeIdEx in newAg.AssociatedNodeIds)
                    {
                        if (nodeIdEx.IsMultiChannel)
                        {
                            isMCA = true;
                        }
                    }
                }

                List<AssociationGroupEx> agRemoved = AssociationSupportData.AssociationGroups.FindAll(x => newData.AssociationGroups.Find(y => y.GroupId == x.GroupId) == null);
                List<AssociationGroupEx> agAdded = newData.AssociationGroups.FindAll(x => AssociationSupportData.AssociationGroups.Find(y => y.GroupId == x.GroupId) == null);
                if (agRemoved.Count > 0 /*|| agAdded.Count > 0*/)
                {
                    agRemoved.ForEach(x => AssociationSupportData.AssociationGroups.Remove(x));
                }

                foreach (AssociationGroupEx newAg in newData.AssociationGroups)
                {
                    bool isAgChanged = false;
                    bool isGroupnameChanged = false;
                    bool isGroupinfoChanged = false;
                    bool isCclistChanged = false;
                    if (AssociationSupportData.AssociationGroups.Find(x => x.GroupId == newAg.GroupId) is AssociationGroupEx currentAg)
                    {
                        // It is an existing AG, check all their components for changes
                        if (currentAg.MaxNodesSupported != newAg.MaxNodesSupported)
                        {
                            isAgChanged = true;
                        }
                        if (currentAg.GroupName != newAg.GroupName)
                        {
                            isAgChanged = true;
                            if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                            {
                                associationReports.Add(PrepareGroupNameReport(newAg.GroupId));
                            }
                        }
                        if (currentAg.Profile.MSB != newAg.Profile.MSB
                         || currentAg.Profile.LSB != newAg.Profile.LSB)
                        {
                            isAgChanged = true;
                            if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                            {
                                associationReports.Add(PrepareGroupInfoReport(newAg.GroupId, 0));
                            }
                        }

                        List<NodeIdEx> nodeRemoved = currentAg.AssociatedNodeIds.FindAll(x => newAg.AssociatedNodeIds.Find(y => y.NodeId == x.NodeId) == null);
                        List<NodeIdEx> nodeAdded = newAg.AssociatedNodeIds.FindAll(x => currentAg.AssociatedNodeIds.Find(y => y.NodeId == x.NodeId) == null);
                        if (nodeRemoved.Count > 0 || nodeAdded.Count > 0)
                        {
                            isAgChanged = true;
                        }
                        else
                        {
                            foreach (NodeIdEx nodeIdEx in newAg.AssociatedNodeIds)
                            {
                                if (currentAg.AssociatedNodeIds.Find(x => x.NodeId == nodeIdEx.NodeId) is NodeIdEx currentNode)
                                {
                                    if (currentNode.EndPoint != nodeIdEx.EndPoint
                                     || currentNode.IsMultiChannel != nodeIdEx.IsMultiChannel)
                                    {
                                        isAgChanged = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    isAgChanged = true;
                                    break;
                                }
                            }
                        }

                        List<AgiCommandClassAndCommandId> ccRemoved = currentAg.CommandClassAndCommandList.FindAll(x => newAg.CommandClassAndCommandList.Find(y => y.CommandClassId == x.CommandClassId && y.CommandId == x.CommandId) == null);
                        List<AgiCommandClassAndCommandId> ccAdded = newAg.CommandClassAndCommandList.FindAll(x => currentAg.CommandClassAndCommandList.Find(y => y.CommandClassId == x.CommandClassId && y.CommandId == x.CommandId) == null);
                        if (ccRemoved.Count > 0 || ccAdded.Count > 0)
                        {
                            isAgChanged = true;
                            isCclistChanged = true;
                        }
                        else
                        {
                            foreach (AgiCommandClassAndCommandId newC in newAg.CommandClassAndCommandList)
                            {
                                if (currentAg.CommandClassAndCommandList.Find(x => x.CommandClassId == newC.CommandClassId && x.CommandId == newC.CommandId) == null)
                                {
                                    isAgChanged = true;
                                    isCclistChanged = true;
                                }
                            }
                        }

                        if (isAgChanged)
                        {
                            AssociationSupportData.AssociationGroups.Remove(currentAg);
                            AssociationSupportData.AssociationGroups.Add(newAg);  // apply new or changed AG
                        }
                    }
                    else // It is a new AG, add it
                    {
                        isAgChanged = true;
                        isGroupnameChanged = true;
                        isGroupinfoChanged = true;
                        isCclistChanged = true;
                        AssociationSupportData.AssociationGroups.Add(newAg);  // apply new or changed AG
                    }

                    // Parsing done. Now prepare the several AGI reports...
                    if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    {
                        if (isGroupnameChanged)
                        {
                            associationReports.Add(PrepareGroupNameReport(newAg.GroupId));
                        }
                        if (isGroupinfoChanged)
                        {
                            associationReports.Add(PrepareGroupInfoReport(newAg.GroupId, 0));
                        }
                        if (isCclistChanged)
                        {
                            associationReports.Add(PrepareGroupCommandListReport(newAg.GroupId));
                        }
                    }

                    // ... and the [MC] Association Report(s).
                    if (isAgChanged && ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    {
                        associationReports.AddRange(isMCA ? PrepareMCAssociationReports(newAg.GroupId) : PrepareAssociationReports(newAg.GroupId));
                    }
                }

                // Send the [MC] Association Supported Groupings Report
                if (agRemoved.Count != agAdded.Count)
                {
                    SendUnsolicitedReport(isMCA ? PrepareMCAssociationGroupingsReport() : PrepareAssociationGroupingsReport(), NodeTag.Empty);
                }
                // Send all prepared reports
                foreach (byte[] rpt in associationReports)
                {
                    SendUnsolicitedReport(rpt, NodeTag.Empty);
                }

                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }


    public class AssociationSupportData : CommandClassSupportData, INodeItem
    {
        // Internals accessible from TestCases or EDE
        [NestedControl(1, "Association Groups")]
        public List<AssociationGroupEx> AssociationGroups { get; set; } = new List<AssociationGroupEx>();

        // Access to Association Groups for other supporting Command Classes
        public static AssociationSupportData SupportDataObject;
        public AssociationSupportData()
        {
            SupportDataObject = this;
        }

        [JsonIgnore]
        public IEnumerable<INestedItem> ChildItems => AssociationGroups;

        public void ClearGroupNodeIds()
        {
            foreach(AssociationGroupEx associationGroupEx in AssociationGroups)
            {
                associationGroupEx.AssociatedNodeIds.Clear();
            }
        }

        public List<AssociationGroupEx> FindGroupByCommand(byte commandClassId, byte commandId)
        {
            List <AssociationGroupEx> relatedGroups = new List<AssociationGroupEx>();

            foreach (AssociationGroupEx group in AssociationGroups)
            {
                if (group.CommandClassAndCommandList.Find(x => x.CommandClassId == commandClassId && x.CommandId == commandId) != null)
                {
                    relatedGroups.Add(group);
                }
            }
            return relatedGroups;
        }
    }
}
