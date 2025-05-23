using System.Collections.Generic;
using System.Text;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Enums;
using ZWave.Devices;
using Utils;
using ZWave;
using ZWave.BasicApplication;
using static Zats.Services.AssociationGroupEx;
using System;

namespace Zats.Services
{
    [CommandClassName("Association Group Information Command Class")]
    [CommandClassId(COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ID)]
    [MinVersion(1)]
    [MaxVersion(3)]
    public class AssociationGroupInfoSupport : AssociationSupportBase
    {
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return AssociationSupportData; }
            set { AssociationSupportData = (AssociationSupportData)value; }
        }

        public AssociationGroupInfoSupport(AssociationSupportData associationSupportData)
            : base(COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ID)
        {
            AssociationSupportData = associationSupportData;
        }

        public AssociationGroupInfoSupport()
            : this(new AssociationSupportData() { })
        { }

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

                if (isHandlingAllowed && command != null && command.Length > 1 && AssociationSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_NAME_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareGroupNameReportCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_INFO_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareGroupInfoReportCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_COMMAND_LIST_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareGroupCommandListReportCmd(command);
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

        private byte[] PrepareGroupNameReportCmd(byte[] command)
        {
            COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_NAME_GET associationNameGetCmd = command;
            return PrepareGroupNameReport(associationNameGetCmd.groupingIdentifier);
        }

        private byte[] PrepareGroupInfoReportCmd(byte[] command)
        {
            COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_INFO_GET associationInfoGetCmd = command;
            return PrepareGroupInfoReport(associationInfoGetCmd.groupingIdentifier, associationInfoGetCmd.properties1.listMode);
        }

        private byte[] PrepareGroupCommandListReportCmd(byte[] command)
        {
            COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ASSOCIATION_GROUP_COMMAND_LIST_GET associationCommandListGetCmd = command;
            return PrepareGroupCommandListReport(associationCommandListGetCmd.groupingIdentifier);
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet,
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            supervisionReport.status = (byte) SvStatusNames.NoSupport;
            return;
        }
    }
}
