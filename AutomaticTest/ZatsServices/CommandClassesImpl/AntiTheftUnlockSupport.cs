using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Utils;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;
using Utils.UI.Enums;

// ToDo: Supervision status 'Working' for Set Command ?
// ToDo: Change NIF if in restricted mode

namespace Zats.Services
{
    [CommandClassName("Anti-Theft Unlock Command Class")]
    [CommandClassId(COMMAND_CLASS_ANTITHEFT_UNLOCK.ID)]
    [MinVersion(1)]
    [MaxVersion(1)]
    public class AntiTheftUnlockSupport : AntiTheftSupportBase
    {
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return AntiTheftSupportData; }
            set { AntiTheftSupportData = (AntiTheftSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public AntiTheftUnlockSupport()
            : this(new AntiTheftSupportData() { })
        { }

        public AntiTheftUnlockSupport(AntiTheftSupportData antiTheftSupportData)
            : base(COMMAND_CLASS_ANTITHEFT_UNLOCK.ID)
        {
            AntiTheftSupportData = antiTheftSupportData;
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
                if (isHandlingAllowed && command != null && command.Length > 1 && AntiTheftSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_ANTITHEFT_UNLOCK.ANTITHEFT_UNLOCK_STATE_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareUnlockStateReport();
                    }
                    else if (command[1] == COMMAND_CLASS_ANTITHEFT_UNLOCK.ANTITHEFT_UNLOCK_SET.ID)
                    {
                        ResultForSv = HandleUnlockSetCmd(command);
                    }

                    if (rpt != null && IsAnsweringAllowed())
                    {
                        ApiOperation sendData = new SendDataExOperation(Network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command, sendDstNode.EndPointId);
                }
            }
        }

        public SvResultNames HandleUnlockSetCmd(byte[] command)
        {
            COMMAND_CLASS_ANTITHEFT_UNLOCK.ANTITHEFT_UNLOCK_SET setCmd = command;

            if (setCmd.magicCode.Count > 0 && AntiTheftSupportData.MagicCode.Count > 0)
            {
                if (setCmd.magicCode.SequenceEqual(AntiTheftSupportData.MagicCode))
                {
                    AntiTheftSupportData.State = AntiTheftStateNames.Unlocked;
                    AntiTheftSupportData.MagicCode = new List<byte>();
                    AntiTheftSupportData.ManufacturerId = 0;
                    AntiTheftSupportData.AntiTheftHint = new List<byte>();
                    AntiTheftSupportData.LockingEntityId = 0;
                    AntiTheftSupportData.IsRestricted = false;
                    CommandClassSupportUtils.ReportAddLog(AntiTheftSupportData.LogLevel, 
                                "{0} CC @ {1}: Unlock Set command accepted. Magic Code: {2}.",
                                CommandClassShortName, EndPointName, setCmd.magicCode.ToHexString());
                    return SvResultNames.Successful;
                }
            }
            CommandClassSupportUtils.ReportAddLog(AntiTheftSupportData.LogLevel, 
                        "{0} CC @ {1}: Unlock Set command ignored. Received Magic Code: {2} - Stored Magic Code: {3}.",
                        CommandClassShortName, EndPointName, setCmd.magicCode.ToHexString(), AntiTheftSupportData.MagicCode.ToHexString());
            return SvResultNames.Failed;
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
                case COMMAND_CLASS_ANTITHEFT_UNLOCK.ANTITHEFT_UNLOCK_SET.ID:
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
