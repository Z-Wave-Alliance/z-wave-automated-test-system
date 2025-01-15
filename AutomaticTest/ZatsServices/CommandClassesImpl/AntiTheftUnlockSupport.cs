// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
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
    public class AntiTheftUnlockSupport : CommandClassSupport
    {
        private AntiTheftSupportData AntiTheftSupportData { get; set; }

        private readonly string CommandClassName = "Anti-Theft Unlock CC";
        private string EndPointName = "";
        private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public AntiTheftUnlockSupport()
            : this(new AntiTheftSupportData() { })
        { }

        public AntiTheftUnlockSupport(AntiTheftSupportData antiTheftSupportData)
            : base(COMMAND_CLASS_ANTITHEFT_UNLOCK.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 1;
            AntiTheftSupportData = antiTheftSupportData;
            LogLevel = AntiTheftSupportData.LogLevel;
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

                EndPointName = ReceivedAchData.DstNode.EndPointId == 0 ? " @ Root" : string.Format(" @ EP {0}", ReceivedAchData.DstNode.EndPointId);
                ResultForSv = SvResultNames.Undefined;
                if (isResponseAllowed && command != null && command.Length > 1 && AntiTheftSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_ANTITHEFT_UNLOCK.ANTITHEFT_UNLOCK_STATE_GET.ID)
                    {
                        rpt = PrepareStateReport();
                    }
                    else if (command[1] == COMMAND_CLASS_ANTITHEFT_UNLOCK.ANTITHEFT_UNLOCK_SET.ID)
                    {
                        ResultForSv = HandleUnlockSetCommand(command);
                    }

                    if (rpt != null)
                    {
                        ApiOperation sendData = new SendDataExOperation(Network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command);
                }
            }
        }

        private byte[] PrepareStateReport()
        {
            COMMAND_CLASS_ANTITHEFT_UNLOCK.ANTITHEFT_UNLOCK_STATE_REPORT rpt = new COMMAND_CLASS_ANTITHEFT_UNLOCK.ANTITHEFT_UNLOCK_STATE_REPORT() { };
            rpt.properties1.state = (byte) AntiTheftSupportData.State;
            rpt.properties1.restricted = (byte) (AntiTheftSupportData.IsRestricted ? 0x01 : 0x00);
            rpt.properties1.antiTheftHintLength = (byte) AntiTheftSupportData.AntiTheftHint.Count;
            rpt.antiTheftHint = AntiTheftSupportData.AntiTheftHint;
            rpt.manufacturerId = CommandClassSupportUtils.UInt64ToByteField(AntiTheftSupportData.ManufacturerId, 2);
            rpt.zWaveAllianceLockingEntityId = CommandClassSupportUtils.UInt64ToByteField(AntiTheftSupportData.LockingEntityId, 2);
            return rpt;
        }

        public SvResultNames HandleUnlockSetCommand(byte[] command)
        {
            COMMAND_CLASS_ANTITHEFT_UNLOCK.ANTITHEFT_UNLOCK_SET setCmd = command;

            if (setCmd.magicCode.Count > 0 && AntiTheftSupportData.MagicCode.Count > 0)
            {
                if (setCmd.magicCode.SequenceEqual(AntiTheftSupportData.MagicCode))
                {
                    AntiTheftSupportData.State = AntiTheftStateNames.Unlocked;
                    AntiTheftSupportData.MagicCode = new List<byte> { };
                    AntiTheftSupportData.ManufacturerId = 0;
                    AntiTheftSupportData.AntiTheftHint = new List<byte> { };
                    AntiTheftSupportData.LockingEntityId = 0;
                    AntiTheftSupportData.IsRestricted = false;
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Unlock Set command accepted. Magic Code: {2}.",
                                CommandClassName, EndPointName, setCmd.magicCode.ToHexString());
                    return SvResultNames.Successful;
                }
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Unlock Set command ignored. Received Magic Code: {2} - Stored Magic Code: {3}.",
                        CommandClassName, EndPointName, setCmd.magicCode.ToHexString(), AntiTheftSupportData.MagicCode.ToHexString());
            return SvResultNames.Failed;
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, 
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != Id)
            {
                supervisionReport.status = (byte) SvStatusNames.NoSupport;
                return;
            }

            supervisionReport.status = (byte) SvStatusNames.Fail;  // FAIL as default
            switch (supervisionGet.encapsulatedCommand[1])
            {
                case COMMAND_CLASS_ANTITHEFT_UNLOCK.ANTITHEFT_UNLOCK_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }
    }
}
