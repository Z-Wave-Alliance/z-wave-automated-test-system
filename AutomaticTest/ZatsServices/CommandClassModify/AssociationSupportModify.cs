using System.Collections.Generic;
using System.Text;
using System.Linq;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Enums;
using ZWave.Devices;
using Utils;
using System;

namespace Zats.Services
{
    public class AssociationSupportModify : AssociationSupport
    {
        private AssociationSupportDataModify AssociationSupportData { get; set; }
        private SvResultNames ResultForSv = SvResultNames.Undefined;
        private int _groups = 1;

        public AssociationSupportModify(AssociationSupportDataModify associationSupportData)
            : base(associationSupportData)
        {
            MinCCVersion = 1;
            MaxCCVersion = 3;
            AssociationSupportData = associationSupportData;
            //LogLevel = associationSupportData.LogLevel;
        }

        public AssociationSupportModify(AssociationSupportDataModify associationSupportData, int groups)
            : base(associationSupportData)
        {
            MinCCVersion = 1;
            MaxCCVersion = 3;
            _groups = groups;
            AssociationSupportData = associationSupportData;
            //LogLevel = associationSupportData.LogLevel;
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
                    if (command[1] == COMMAND_CLASS_ASSOCIATION.ASSOCIATION_GET.ID)
                    {
                        IActionItem[] actionItems = PrepareAssociationReports(command);
                        ou.SetNextActionItems(actionItems);
                    }
                    else if (command[1] == COMMAND_CLASS_ASSOCIATION.ASSOCIATION_SET.ID)
                    {
                        ResultForSv = HandleAssociationSet(command);
                    }
                    else if (command[1] == COMMAND_CLASS_ASSOCIATION.ASSOCIATION_REMOVE.ID)
                    {
                        ResultForSv = HandleAssociationRemove(command);
                    }
                    else if (command[1] == COMMAND_CLASS_ASSOCIATION.ASSOCIATION_GROUPINGS_GET.ID)
                    {
                        rpt = new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_GROUPINGS_REPORT()
                        {
                            supportedGroupings = Convert.ToByte(AssociationSupportData.AssociationGroups.Count)
                        };
                    }
                    else if (command[1] == COMMAND_CLASS_ASSOCIATION_V2.ASSOCIATION_SPECIFIC_GROUP_GET.ID && Version >= 2)
                    {
                        rpt = new COMMAND_CLASS_ASSOCIATION_V2.ASSOCIATION_SPECIFIC_GROUP_REPORT()
                        {
                            group = 1
                        };
                    }

                    if (rpt != null)
                    {
                        ApiOperation sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command, sendDstNode.EndPointId);
                }
            }
        }

        private IActionItem[] PrepareAssociationReports(byte[] command)
        {
            COMMAND_CLASS_ASSOCIATION.ASSOCIATION_GET associationGetCmd = command;
            byte requestedGroupId = associationGetCmd.groupingIdentifier;
            AssociationGroupExModify requestedGroup = AssociationSupportData.AssociationGroups.Find(group => group.GroupId == requestedGroupId);

            if (requestedGroup == null)
            {
                requestedGroup = AssociationSupportData.AssociationGroups[0];
            }
            COMMAND_CLASS_ASSOCIATION.ASSOCIATION_REPORT rpt = new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_REPORT()
            {
                groupingIdentifier = requestedGroup.GroupId,
                //nodeid = requestedGroup.GetAssociatedNodeIds(),
                maxNodesSupported = requestedGroup.MaxNodesSupported
            };

            int idx = 0;
            IList<byte> nodeIds = requestedGroup.GetAssociatedNodeIds();
            byte reportsToFollow = (byte)((nodeIds.Count - 1) / 29);     // 29 Bytes per frame are available for the nodeid field
            IActionItem[] actionItems = new IActionItem[reportsToFollow + 1];
            rpt.nodeid.Clear();
            foreach (byte node in nodeIds)
            {
                rpt.nodeid.Add(node);
                if (rpt.nodeid.Count == 29)
                {
                    rpt.reportsToFollow = reportsToFollow;
                    actionItems[idx / 29] = new SendDataExOperation(_network, ReceivedAchData.DstNode, ReceivedAchData.SrcNode, rpt, TxOptions, TxSecOptions, (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme, TxOptions2);
                    reportsToFollow--;
                    rpt.nodeid.Clear();
                }
                idx++;
            }
            if (rpt.nodeid.Count != 0 || nodeIds.Count == 0)
            {
                rpt.reportsToFollow = reportsToFollow;
                actionItems[idx / 29] = new SendDataExOperation(_network, ReceivedAchData.DstNode, ReceivedAchData.SrcNode, rpt, TxOptions, TxSecOptions, (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme, TxOptions2);
            }
            return actionItems;
        }
        ITimeoutItem timeoutItem;
        // Unsolicited Frames: Configure and start one common timer, coded in CommandClassSupportUtils

        private SvResultNames HandleAssociationSet(byte[] command)
        {
            SvResultNames success = SvResultNames.Successful;

            COMMAND_CLASS_ASSOCIATION.ASSOCIATION_SET associationSetCmd = command;
            byte requestedGroupId = associationSetCmd.groupingIdentifier;
            AssociationGroupExModify requestedGroup = AssociationSupportData.AssociationGroups.Find(group => group.GroupId == requestedGroupId);

            if (requestedGroup != null)
            {
                foreach (byte associateNodeId in associationSetCmd.nodeId)
                {
                    AssociationGroupExModify.NodeIdEx nodeIdEx = requestedGroup.AssociatedNodeIds.Find(id => id.NodeId == associateNodeId && !id.IsMultiChannel);
                    if (nodeIdEx == null)
                    {
                        if (requestedGroup.AssociatedNodeIds.Count == requestedGroup.MaxNodesSupported)
                        {
                            success = SvResultNames.Failed;
                            break;
                        }
                        requestedGroup.AssociatedNodeIds.Add(new AssociationGroupExModify.NodeIdEx(associateNodeId, 0x00, false));
                    }
                }
            }
            else
            {
                success = SvResultNames.Failed;
            }
            return success;
        }

        private SvResultNames HandleAssociationRemove(byte[] command)
        {
            SvResultNames success = SvResultNames.Failed;

            COMMAND_CLASS_ASSOCIATION.ASSOCIATION_REMOVE associationRemoveCmd = command;
            byte requestedGroupId = associationRemoveCmd.groupingIdentifier;
            AssociationGroupExModify requestedGroup = AssociationSupportData.AssociationGroups.Find(group => group.GroupId == requestedGroupId);

            if (requestedGroup != null)
            {
                if (associationRemoveCmd.nodeId.Count != 0)
                {
                    foreach (byte associateNodeId in associationRemoveCmd.nodeId)
                    {
                        AssociationGroupExModify.NodeIdEx nodeIdEx = requestedGroup.AssociatedNodeIds.Find(id => id.NodeId == associateNodeId && !id.IsMultiChannel);
                        if (nodeIdEx != null)
                        {
                            requestedGroup.AssociatedNodeIds.Remove(nodeIdEx);
                        }
                    }
                    success = SvResultNames.Successful;
                }
                else
                {
                    for (int i = requestedGroup.AssociatedNodeIds.Count - 1; i >= 0; i--)
                    {
                        if (!requestedGroup.AssociatedNodeIds[i].IsMultiChannel)
                        {
                            requestedGroup.AssociatedNodeIds.RemoveAt(i);
                        }
                    }
                    success = SvResultNames.Successful;
                }
            }
            else if (requestedGroupId == 0x00 && Version >= 2)
            {
                if (associationRemoveCmd.nodeId.Count != 0)
                {
                    foreach (byte associateNodeId in associationRemoveCmd.nodeId)
                    {
                        foreach (AssociationGroupExModify group in AssociationSupportData.AssociationGroups)
                        {
                            AssociationGroupExModify.NodeIdEx nodeIdEx = group.AssociatedNodeIds.Find(id => id.NodeId == associateNodeId && !id.IsMultiChannel);
                            if (nodeIdEx != null)
                            {
                                group.AssociatedNodeIds.Remove(nodeIdEx);
                            }
                        }
                    }
                    success = SvResultNames.Successful;
                }
                else
                {
                    foreach (AssociationGroupExModify group in AssociationSupportData.AssociationGroups)
                    {
                        for (int i = group.AssociatedNodeIds.Count - 1; i >= 0; i--)
                        {
                            if (!group.AssociatedNodeIds[i].IsMultiChannel)
                            {
                                group.AssociatedNodeIds.RemoveAt(i);
                            }
                        }
                    }
                    success = SvResultNames.Successful;
                }
            }
            return success;
        }

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
                case COMMAND_CLASS_ASSOCIATION.ASSOCIATION_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte)SvStatusNames.Success;
                    break;
                case COMMAND_CLASS_ASSOCIATION.ASSOCIATION_REMOVE.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte)SvStatusNames.Success;
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }
    }

    public class AssociationGroupExModify
    {
        public class NodeIdEx
        {
            public byte NodeId { get; }
            public byte EndPoint { get; }
            public bool IsMultiChannel { get; }

            public NodeIdEx(byte nodeId, byte endPoint, bool isMultiChannel)
            {
                NodeId = nodeId;
                EndPoint = endPoint;
                IsMultiChannel = isMultiChannel;
            }
        }
        public byte GroupId { get; set; }
        public byte MaxNodesSupported { get; set; }
        public string GroupName { get; set; }
        public List<NodeIdEx> AssociatedNodeIds { get; set; }
        public byte[] CommandClassAndCommandList { get; set; }
        public short Profile { get; set; }


        public AssociationGroupExModify(byte groupId, byte maxNodesSupported, string groupName, short profile, byte[] commandClassAndCommandList)
        {
            GroupId = groupId;
            MaxNodesSupported = maxNodesSupported;
            GroupName = groupName;
            Profile = profile;
            CommandClassAndCommandList = commandClassAndCommandList;
            
            AssociatedNodeIds = new List<NodeIdEx>();
        }

        public List<byte> GetAssociatedNodeIds()
        {
            List<byte> ids = new List<byte>();

            foreach (NodeIdEx nodeIdEx in AssociatedNodeIds)
            {
                if (!nodeIdEx.IsMultiChannel)
                {
                    ids.Add(nodeIdEx.NodeId);
                }
            }

            return ids;
        }

        public List<COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V2.MULTI_CHANNEL_ASSOCIATION_REPORT.TVG> GetTVGs()
        {
            List<COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V2.MULTI_CHANNEL_ASSOCIATION_REPORT.TVG> tvgs = new List<COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V2.MULTI_CHANNEL_ASSOCIATION_REPORT.TVG>();

            foreach (NodeIdEx nodeIdEx in AssociatedNodeIds)
            {
                if (nodeIdEx.IsMultiChannel)
                {
                    tvgs.Add(new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V2.MULTI_CHANNEL_ASSOCIATION_REPORT.TVG
                    {
                        multiChannelNodeId = nodeIdEx.NodeId,
                        properties1 = new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V2.MULTI_CHANNEL_ASSOCIATION_REPORT.TVG.Tproperties1
                        {
                            bitAddress = (byte)(nodeIdEx.EndPoint > 0x80 ? 1 : 0),
                            endPoint = nodeIdEx.EndPoint
                        }
                    });
                }
            }
            return tvgs;
        }
    }

    public class AssociationSupportDataModify: AssociationSupportData
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
        public List<AssociationGroupExModify> AssociationGroups { get; set; } = new List<AssociationGroupExModify>();

        // Access to Association Groups for other supporting Command Classes
        public static AssociationSupportDataModify SupportDataObject;
        public AssociationSupportDataModify()
        {
            SupportDataObject = this;
        }
    }
}
