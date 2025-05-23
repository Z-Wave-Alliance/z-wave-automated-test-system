using System;
using System.ComponentModel;
using System.Threading;
using Utils;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;

namespace Zats.Services
{
    [CommandClassName("Basic Command Class")]
    [CommandClassId(COMMAND_CLASS_BASIC.ID)]
    [MinVersion(1)]
    [MaxVersion(2)]
    public class BasicSupport : CommandClassSupport
    {
        private BasicSupportData BasicSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return BasicSupportData; }
            set { BasicSupportData = (BasicSupportData)value; }
        }

        //private string EndPointName = "";
        //private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public BasicSupport()
            : this(new BasicSupportData() { }, null)
        { }

        public BasicSupport(BasicSupportData basicSupportData, IBasicMapping mappedSupportData)
            : base(COMMAND_CLASS_BASIC.ID)
        {
            BasicSupportData = basicSupportData;
            BasicSupportData.MappedSupportData = mappedSupportData;
            //LogLevel = BasicSupportData.LogLevel;
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
                if (isHandlingAllowed && command != null && command.Length > 1 && BasicSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_BASIC.BASIC_SET.ID)
                    {
                        ResultForSv = HandleBasicSet(command, sendSrcNode);
                    }
                    else if (command[1] == COMMAND_CLASS_BASIC.BASIC_GET.ID)
                    {
                        rpt = PrepareBasicReport();
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

        private byte[] PrepareBasicReport()
        {
            if (BasicSupportData.MappedSupportData != null)
            {
                // BasicV2Get() MUST return a COMMAND_CLASS_BASIC_V2.BASIC_REPORT 
                // containing value/currentValue, targetValue or 0x00, remaining duration or 0x00
                COMMAND_CLASS_BASIC_V2.BASIC_REPORT rptV2 = BasicSupportData.MappedSupportData.BasicV2Get();
                if (rptV2 != null)
                { 
                    if (Version == 1)
                    {
                        COMMAND_CLASS_BASIC.BASIC_REPORT rpt = new COMMAND_CLASS_BASIC.BASIC_REPORT() { };
                        rpt.value = rptV2.currentValue;
                        return rpt;
                    }
                    if (Version == 2)
                    {
                        return rptV2;
                    }
                }
            }
            return null;
        }

        private SvResultNames HandleBasicSet(byte[] command, NodeTag srcNode)
        {
            if (BasicSupportData.MappedSupportData != null)
            {
                COMMAND_CLASS_BASIC.BASIC_SET setCmd = command;
                return BasicSupportData.MappedSupportData.BasicSet(setCmd.value, srcNode) ? SvResultNames.Successful : SvResultNames.Failed;
            }
            return SvResultNames.Failed;
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet,
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            {
                if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != CommandClassId)
                {
                    supervisionReport.status = (byte) SvStatusNames.NoSupport;
                    return;
                }

                supervisionReport.status = (byte) SvStatusNames.Fail;  // FAIL as default
                switch (supervisionGet.encapsulatedCommand[1])
                {
                    case COMMAND_CLASS_BASIC.BASIC_SET.ID:
                        if (ResultForSv == SvResultNames.Successful)
                            supervisionReport.status = (byte) SvStatusNames.Success;
                        break;
                }
                ResultForSv = SvResultNames.Undefined;
            }
        }


        public override void UpdateSupportData(CommandClassSupportData data)
        {
            // @todo: placeholder - still needs to be implemented
        }
    }

    public class BasicSupportData : CommandClassSupportData
    {
        public IBasicMapping MappedSupportData { get; set; } = null;
    }
}
