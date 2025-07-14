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
    public class AntiTheftSupport : CommandClassSupport
    {
        private AntiTheftSupportData AntiTheftSupportData { get; set; }

        private readonly string CommandClassName = "Anti-Theft CC";
        private string EndPointName = "";
        private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public AntiTheftSupport()
            : this(new AntiTheftSupportData() { })
        { }

        public AntiTheftSupport(AntiTheftSupportData antiTheftSupportData)
            : base(COMMAND_CLASS_ANTITHEFT_V3.ID)
        {
            MinCCVersion = 3;
            MaxCCVersion = 3;
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
                    if (command[1] == COMMAND_CLASS_ANTITHEFT.ANTITHEFT_GET.ID)
                    {
                        rpt = PrepareStateReport();
                    }
                    else if (command[1] == COMMAND_CLASS_ANTITHEFT.ANTITHEFT_SET.ID)
                    {
                        ResultForSv = HandleSetCommand(command);
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
            COMMAND_CLASS_ANTITHEFT_V3.ANTITHEFT_REPORT rpt = new COMMAND_CLASS_ANTITHEFT_V3.ANTITHEFT_REPORT() { };
            rpt.antiTheftProtectionStatus = (byte) (AntiTheftSupportData.State == AntiTheftStateNames.Locked ? 0x02 : 0x01);
            // Restricted Mode (0x03) cannot be recognized automatically
            if (AntiTheftSupportData.IsRestricted)
                rpt.antiTheftProtectionStatus = 0x03;
            rpt.manufacturerId = CommandClassSupportUtils.UInt64ToByteField(AntiTheftSupportData.ManufacturerId, 2);
            rpt.antiTheftHintNumberBytes = (byte) AntiTheftSupportData.AntiTheftHint.Count;
            rpt.antiTheftHintByte = AntiTheftSupportData.AntiTheftHint;
            rpt.zWaveAllianceLockingEntityId = CommandClassSupportUtils.UInt64ToByteField(AntiTheftSupportData.LockingEntityId, 2);
            return rpt;
        }

        public SvResultNames HandleSetCommand(byte[] command)
        {
            COMMAND_CLASS_ANTITHEFT_V3.ANTITHEFT_SET setCmd = command;
            AntiTheftStateNames newState = setCmd.properties1.enable == 0 ? AntiTheftStateNames.Unlocked : AntiTheftStateNames.Locked;
            AntiTheftStateNames currentState = AntiTheftSupportData.State;

            if (Version == 3)
            {
                if (currentState == AntiTheftStateNames.Unlocked && newState == AntiTheftStateNames.Locked)
                {
                    if (CommandClassSupportUtils.ByteFieldToUInt64(setCmd.zWaveAllianceLockingEntityId, 2) == 0
                     || setCmd.zWaveAllianceLockingEntityId.Length == 0)
                    {
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Set command (lock) ignored. Invalid Locking Entity ID '{2}'.",
                                    CommandClassName, EndPointName, setCmd.zWaveAllianceLockingEntityId.ToHexString());
                        return SvResultNames.Failed;
                    }
                    if (setCmd.properties1.numberOfMagicCodeBytes < 1
                     || setCmd.properties1.numberOfMagicCodeBytes > 10
                     || setCmd.magicCode.Count != setCmd.properties1.numberOfMagicCodeBytes)
                    {
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Set command (lock) ignored. Received Magic Code: '{2}' - Received Length: {3}.",
                                    CommandClassName, EndPointName, setCmd.magicCode.ToHexString(), setCmd.properties1.numberOfMagicCodeBytes);
                        return SvResultNames.Failed;
                    }
                    AntiTheftSupportData.State = newState;
                    AntiTheftSupportData.MagicCode = setCmd.magicCode;
                    AntiTheftSupportData.ManufacturerId = (ushort) CommandClassSupportUtils.ByteFieldToUInt64(setCmd.manufacturerId, 2);
                    AntiTheftSupportData.AntiTheftHint = setCmd.antiTheftHintByte;
                    AntiTheftSupportData.LockingEntityId = (ushort) CommandClassSupportUtils.ByteFieldToUInt64(setCmd.zWaveAllianceLockingEntityId, 2);
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Set command (lock) accepted. New Magic Code: {2}.",
                                CommandClassName, EndPointName, setCmd.magicCode.ToHexString());
                    return SvResultNames.Successful;
                }

                if (currentState == AntiTheftStateNames.Locked && newState == AntiTheftStateNames.Unlocked)
                {
                    if (AntiTheftSupportData.MagicCode.Count > 0
                     && setCmd.magicCode.SequenceEqual(AntiTheftSupportData.MagicCode))
                    {
                        AntiTheftSupportData.State = newState;
                        AntiTheftSupportData.MagicCode = new List<byte> { };
                        AntiTheftSupportData.ManufacturerId = 0;
                        AntiTheftSupportData.AntiTheftHint = new List<byte> { };
                        AntiTheftSupportData.LockingEntityId = 0;
                        AntiTheftSupportData.IsRestricted = false;
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Set command (unlock) accepted. Magic Code: {2}.",
                                    CommandClassName, EndPointName, setCmd.magicCode.ToHexString());
                        return SvResultNames.Successful;
                    }
                    else
                    {
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Set command (unlock) ignored. Received Magic Code: {2} - Stored Magic Code: {3}.",
                                    CommandClassName, EndPointName, setCmd.magicCode.ToHexString(), AntiTheftSupportData.MagicCode.ToHexString());
                        return SvResultNames.Failed;
                    }

                }
                //AntiTheftSupportData.Restricted = AntiTheftRestrictNames.Unrestricted;
                if (currentState == newState)
                {
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Set command ignored (already at requested state '{2}').", 
                                CommandClassName, EndPointName, currentState);
                    return SvResultNames.Successful;
                }
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Set command ignored (Version < 3).", CommandClassName, EndPointName);
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
                case COMMAND_CLASS_ANTITHEFT_V3.ANTITHEFT_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }
    }

    public class AntiTheftSupportData
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
        public AntiTheftStateNames State { get; set; } = AntiTheftStateNames.Unlocked;
        public bool IsRestricted { get; set; } = false;

        public IList<byte> MagicCode { get; set; } = new List<byte> { };
        public IList<byte> AntiTheftHint { get; set; } = new List<byte> { };
        public ushort ManufacturerId { get; set; } = 0x0000;
        public ushort LockingEntityId { get; set; } = 0x0000;

    }

    public enum AntiTheftStateNames : byte
    {
        Unlocked = 0,
        Locked   = 1,
    }
}
