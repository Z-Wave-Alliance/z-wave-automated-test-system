// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
// SPDX-FileCopyrightText: 2025 Z-Wave Alliance https://z-wavealliance.org
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Enums;
using ZWave.Devices;
using Utils;
using System;
using ZatsServices_netcore.CommandClassesImpl.Definitions.Interfaces;
using ZWave.Xml.Application;
using static Zats.Services.AssociationGroupEx;
using static UicApplication.Clusters.SmartStart.List;
//using static Zats.Services.COMMAND_CLASS_ZIP_V3_2.IMA_REPORT;
using Newtonsoft.Json;

namespace Zats.Services
{
    [CommandClassName("Association Command Class")]
    [CommandClassId(COMMAND_CLASS_ASSOCIATION.ID)]
    [MinVersion(1)]
    [MaxVersion(4)] // Security Learning is not yet implemented
                    // (to be used in Av3+ for sending controlling
                    // and in Av4+ also for sending supporting commands
                    // to associated destinations).
    public class AssociationSupport : AssociationSupportBase
    {
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return AssociationSupportData; }
            set { AssociationSupportData = (AssociationSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public AssociationSupport()
            : this(new AssociationSupportData() { })
            { }

        public AssociationSupport(AssociationSupportData associationSupportData)
            : base(COMMAND_CLASS_ASSOCIATION.ID)
        {
            AssociationSupportData = associationSupportData;
        }

        public override void ResetEmulation()
        {
            AssociationSupportData.ClearGroupNodeIds();
            SupportDelegates?.DeviceDataUpdate?.Invoke(this);
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
                    if (command[1] == COMMAND_CLASS_ASSOCIATION.ASSOCIATION_GET.ID && !IsSupervised())
                    {
                        if (IsAnsweringAllowed())
                        {
                            IActionItem[] actionItems = PrepareAssociationReportsCmd(command);
                            ou.SetNextActionItems(actionItems);
                        }
                    }
                    else if (command[1] == COMMAND_CLASS_ASSOCIATION.ASSOCIATION_SET.ID)
                    {
                        ResultForSv = HandleAssociationSetCmd(command, sendSrcNode);
                    }
                    else if (command[1] == COMMAND_CLASS_ASSOCIATION.ASSOCIATION_REMOVE.ID)
                    {
                        ResultForSv = HandleAssociationRemoveCmd(command, sendSrcNode);
                    }
                    else if (command[1] == COMMAND_CLASS_ASSOCIATION.ASSOCIATION_GROUPINGS_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareAssociationGroupingsReport();
                    }
                    else if (command[1] == COMMAND_CLASS_ASSOCIATION_V2.ASSOCIATION_SPECIFIC_GROUP_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareAssociationSpecificGroupReport();
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
		ITimeoutItem timeoutItem;
        // Unsolicited Frames: Configure and start one common timer, coded in CommandClassSupportUtils
        protected override void CreateWorkflow()
        {
            // call base workflow initialization
            base.CreateWorkflow();
            timeoutItem = new TimeInterval(GetNextCounter(), CommandClassId, CommandClassSupportUtils.TickerIntervalMs);
            // modify start action unit and add first time interval
            StartActionUnit startUnit = (StartActionUnit)ActionUnits.First(x => x is StartActionUnit);
            startUnit.AddNextActionItems(timeoutItem);
            // add handler for time interval with OnTickerIntervalElapsed function
            ActionUnits.Add(new TimeElapsedUnit(timeoutItem, OnIntervalElapsed, 0, timeoutItem));
        }

        private void OnIntervalElapsed(TimeElapsedUnit ou)
        {
            CommandClassSupportUtils.OnTickerIntervalElapsed(ou);
            ou.AddNextActionItems(new TimeInterval(GetNextCounter(), 1, CommandClassSupportUtils.TickerIntervalMs));
        }

        private byte[] PrepareAssociationSpecificGroupReport()
        {
            if (Version >= 2)
            {
                COMMAND_CLASS_ASSOCIATION_V2.ASSOCIATION_SPECIFIC_GROUP_REPORT rpt = new COMMAND_CLASS_ASSOCIATION_V2.ASSOCIATION_SPECIFIC_GROUP_REPORT();
                rpt.group = 1;
                return rpt;
            }
            return null;
        }
        private IActionItem[] PrepareAssociationReportsCmd(byte[] command)
        {
            COMMAND_CLASS_ASSOCIATION.ASSOCIATION_GET associationGetCmd = command;
            byte requestedGroupId = associationGetCmd.groupingIdentifier;

            List<byte[]> reportList = PrepareAssociationReports(requestedGroupId);
            IActionItem[] actionItems = new IActionItem[reportList.Count];
            for (int i = 0; i < reportList.Count; i++)
            {
                actionItems[i] = new SendDataExOperation(_network, ReceivedAchData.DstNode, ReceivedAchData.SrcNode, reportList[i], TxOptions, TxSecOptions, (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme, TxOptions2);
            }
            return actionItems;
#if _OLDSTYLE
            AssociationGroupEx requestedGroup = AssociationSupportData.AssociationGroups.Find(group => group.GroupId == requestedGroupId);

            if (AssociationSupportData.AssociationGroups.Count == 0)
            {
                return null; //??
            }

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
#endif // _OLDSTYLE
        }

        private SvResultNames HandleAssociationSet(byte requestedGroupId, IList<byte> nodeId, bool updateUI, NodeTag srcNode)
        {
            SvResultNames success = SvResultNames.Successful;

            AssociationGroupEx requestedGroup = AssociationSupportData.AssociationGroups.Find(group => group.GroupId == requestedGroupId);
            if (requestedGroup != null)
            {
                foreach (byte associateNodeId in nodeId)
                {
                    AssociationGroupEx.NodeIdEx nodeIdEx = requestedGroup.AssociatedNodeIds.Find(id => id.NodeId == associateNodeId && !id.IsMultiChannel);
                    if (nodeIdEx == null)
                    {
                        if (requestedGroup.AssociatedNodeIds.Count == requestedGroup.MaxNodesSupported)
                        {
                            success = SvResultNames.Failed;
                            break;
                        }
                        requestedGroup.AssociatedNodeIds.Add(new AssociationGroupEx.NodeIdEx(associateNodeId, 0x00, false));
                    }
                }
                // Association has no mandatory Lifeline Reports
                //if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                //{
                //    List<byte[]> reports = PrepareAssociationReports(requestedGroupId);
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
        private SvResultNames HandleAssociationSetCmd(byte[] command, NodeTag srcNode)
        {
            COMMAND_CLASS_ASSOCIATION.ASSOCIATION_SET associationSetCmd = command;
            return HandleAssociationSet(associationSetCmd.groupingIdentifier, associationSetCmd.nodeId, true, srcNode);
        }

        private SvResultNames HandleAssociationRemove(byte requestedGroupId, IList<byte> nodeId, bool updateUI, NodeTag srcNode)
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
                if (nodeId.Count != 0)
                {
                    foreach (byte associateNodeId in nodeId)
                    {
                        foreach (AssociationGroupEx group in AssociationSupportData.AssociationGroups)
                        {
                            AssociationGroupEx.NodeIdEx nodeIdEx = group.AssociatedNodeIds.Find(id => id.NodeId == associateNodeId && !id.IsMultiChannel);
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
                    foreach (AssociationGroupEx group in AssociationSupportData.AssociationGroups)
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

            if (success == SvResultNames.Successful)
            {
                // Association has no mandatory Lifeline Reports
                //if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                //{
                //    List<byte[]> reports = PrepareAssociationReports(requestedGroupId);
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
        private SvResultNames HandleAssociationRemoveCmd(byte[] command, NodeTag srcNode)
        {
            COMMAND_CLASS_ASSOCIATION.ASSOCIATION_REMOVE associationRemoveCmd = command;
            return HandleAssociationRemove(associationRemoveCmd.groupingIdentifier, associationRemoveCmd.nodeId, true, srcNode);
        }

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
                case COMMAND_CLASS_ASSOCIATION.ASSOCIATION_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    }
                    break;
                case COMMAND_CLASS_ASSOCIATION.ASSOCIATION_REMOVE.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    }
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }
    }

    public class AgiProfile
    {
        /// <summary>
        /// Profile identifier MSB is 'Profile1'
        /// </summary>
        public byte MSB { get; set; }
        /// <summary>
        /// Profile identifier LSB is 'Profile2'
        /// </summary>
        public byte LSB { get; set; }

        public AgiProfile()
        {

        }

        /// <summary>
        /// AGI Profile identifier
        /// </summary>
        /// <param name="msb">MSB is 'Profile1'</param>
        /// <param name="lsb">LSB is 'Profile2'</param>
        public AgiProfile(byte msb, byte lsb)
        {
            MSB = msb;
            LSB = lsb;
        }

        public AgiProfile(short profile)
        {
            MSB = (byte) (0xFF & (profile>>8));
            LSB = (byte) (0xFF & profile);
        }


        public static implicit operator byte[](AgiProfile profile)
        {
            return new byte[] { profile.MSB, profile.LSB };
        }

        public static implicit operator short(AgiProfile profile)
        {
            return (short)((profile.MSB<<8) | profile.LSB);
        }

        public static implicit operator AgiProfile(short profile)
        {
            return new AgiProfile(profile);
        }
        
        public static implicit operator AgiProfile(byte[] profile)
        {
            if (profile!=null && profile.Length==2)
            {
                return new AgiProfile(profile[0], profile[1]);
            }
            return null;
        }
    }

    public class AgiCommandClassAndCommandId : INestedItem
    {
        public byte CommandClassId { get; set; }
        public byte CommandId { get; set; }

        // only used for EDE UI. see interface for useage notes.
        public int NestedId { get; set; }

        public AgiCommandClassAndCommandId()
        {
        }

        public AgiCommandClassAndCommandId(byte commandClassId, byte commandId)
        {
            CommandClassId = commandClassId;
            CommandId = commandId;
        }   


        public static implicit operator byte[](AgiCommandClassAndCommandId commandClassAndCommandId)
        {
            return new byte[] { commandClassAndCommandId.CommandClassId, commandClassAndCommandId.CommandId };
        }

    }

    public class AssociationGroupEx : INestedItem
    {
        public class NodeIdEx : INestedItem
        {
            public byte NodeId { get; set; }
            public byte EndPoint { get; set; }
            public bool IsMultiChannel { get; set; }

            // only used for EDE UI. see interface for useage notes.
            public int NestedId { get; set; }

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
        public List<NodeIdEx> AssociatedNodeIds { get; set; } = new List<NodeIdEx>();
        public List<AgiCommandClassAndCommandId> CommandClassAndCommandList { get; set; } = new List<AgiCommandClassAndCommandId>();
        public AgiProfile Profile { get; set; }

        // only used for EDE UI. see interface for useage notes.
        [JsonIgnore]
        public int NestedId
        {
            get => GroupId;
            set
            {
                GroupId = (byte)value;
            }
        }

        public AssociationGroupEx()
        {

        }

        public AssociationGroupEx(byte groupId, byte maxNodesSupported, string groupName, AgiProfile profile, 
                                  List<AgiCommandClassAndCommandId> commandClassAndCommandList)
        {
            GroupId = groupId;
            MaxNodesSupported = maxNodesSupported;
            GroupName = groupName;
            Profile = profile;
            CommandClassAndCommandList = commandClassAndCommandList;

            AssociatedNodeIds = new List<NodeIdEx>();
        }

        public AssociationGroupEx(byte groupId, byte maxNodesSupported, string groupName, AgiProfile profile, byte[] commandClassAndCommandList) :
            this(groupId, maxNodesSupported, groupName, profile, ConvertCommandClassAndCommandIds(commandClassAndCommandList))
        {

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
                            bitAddress = (byte) (nodeIdEx.EndPoint > 0x80 ? 1 : 0), endPoint = nodeIdEx.EndPoint
                        }
                    });
                }
            }
            return tvgs;
        }
    
        public byte[] GetCommandClassAndCommandIds()
        {
            List<byte> ret = new List<byte>();
            CommandClassAndCommandList.ForEach(x => ret.AddRange((byte[]) x));
            return ret.ToArray();
        }

        public static List<AgiCommandClassAndCommandId> ConvertCommandClassAndCommandIds(byte[] commandClassAndCommandList)
        {
            List<AgiCommandClassAndCommandId> commandClassAndCommandIds = new List<AgiCommandClassAndCommandId>();
            if (commandClassAndCommandList != null)
            {
                for (int i = 0; i < commandClassAndCommandList.Length - 1; i += 2)
                {
                    commandClassAndCommandIds.Add(new AgiCommandClassAndCommandId(commandClassAndCommandList[i], commandClassAndCommandList[i + 1]));
                }
            }
            return commandClassAndCommandIds;
        }
    }
}
