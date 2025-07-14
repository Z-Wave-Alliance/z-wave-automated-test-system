// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;
using System.Text;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Enums;
using ZWave.Devices;
using Utils;
using ZWave;
using ZWave.BasicApplication;

namespace Zats.Services
{
    public class AssociationGroupInfoSupportModify : AssociationGroupInfoSupport
    {
        private AssociationSupportDataModify AssociationSupportData { get; set; }

        //private readonly string CommandClassName = "Association Group Info CC";
        //private string EndPointName = "";
        //private readonly int LogLevel;  // shortcut for SupportData.LogLevel

        public AssociationGroupInfoSupportModify(AssociationSupportDataModify associationSupportData)
            : base(associationSupportData)
        {
            MinCCVersion = 1;
            MaxCCVersion = 3;
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

                if (isResponseAllowed && command != null && command.Length > 1 && AssociationSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_NAME_GET.ID)
                    {
                        rpt = PrepareGroupNameReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_INFO_GET.ID)
                    {
                        rpt = PrepareGroupInfoReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_COMMAND_LIST_GET.ID)
                    {
                        rpt = PrepareGroupCommandListReport(command);
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

        private byte[] PrepareGroupNameReport(byte[] command)
        {
            COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_NAME_GET associationNameGetCmd = command;
            byte requestedGroupId = associationNameGetCmd.groupingIdentifier;
            AssociationGroupExModify requestedGroup = AssociationSupportData.AssociationGroups.Find(group => group.GroupId == requestedGroupId);
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

        private byte[] PrepareGroupInfoReport(byte[] command)
        {
            COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_INFO_GET associationInfoGetCmd = command;
            byte requestedGroupId = associationInfoGetCmd.groupingIdentifier;
            AssociationGroupExModify requestedGroup = AssociationSupportData.AssociationGroups.Find(group => group.GroupId == requestedGroupId);
            byte[] rpt = null;

            if (requestedGroup == null)
            {
                requestedGroup = AssociationSupportData.AssociationGroups[0];
            }

            if (associationInfoGetCmd.properties1.listMode == 0)
            {
                rpt = new COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_INFO_REPORT()
                {
                    properties1 = new COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_INFO_REPORT.Tproperties1()
                    {
                        listMode = associationInfoGetCmd.properties1.listMode,
                        groupCount = 0x01
                    },
                    vg1 = new List<COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_INFO_REPORT.TVG1>()
                    {
                        new COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_INFO_REPORT.TVG1()
                        {
                            groupingIdentifier = requestedGroup.GroupId,
                            mode = 0,
                            profile1 = (byte) (requestedGroup.Profile >> 8),
                            profile2 = (byte) (requestedGroup.Profile & 0xFF)
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
                            profile1 = (byte)(AssociationSupportData.AssociationGroups[i].Profile >> 8),
                            profile2 = (byte)(AssociationSupportData.AssociationGroups[i].Profile & 0xFF)
                        });
                }

                rpt = new COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_INFO_REPORT()
                {
                    properties1 = new COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_INFO_REPORT.Tproperties1()
                    {
                        listMode = associationInfoGetCmd.properties1.listMode,
                        groupCount = (byte)groupList.Count
                    },
                    vg1 = groupList
                };
            }
            return rpt;
        }

        private byte[] PrepareGroupCommandListReport(byte[] command)
        {
            COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_COMMAND_LIST_GET associationCommandListGetCmd = command;
            byte requestedGroupId = associationCommandListGetCmd.groupingIdentifier;
            AssociationGroupExModify requestedGroup = AssociationSupportData.AssociationGroups.Find(group => group.GroupId == requestedGroupId);
            byte[] rpt = null;

            if (requestedGroup != null)
            {
                rpt = new COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_COMMAND_LIST_REPORT()
                {
                    groupingIdentifier = requestedGroup.GroupId,
                    listLength = (byte)requestedGroup.CommandClassAndCommandList.Length,
                    command = requestedGroup.CommandClassAndCommandList
                };
            }
            return rpt;
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet,
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            supervisionReport.status = (byte) SvStatusNames.NoSupport;
            return;
        }
    }
}
