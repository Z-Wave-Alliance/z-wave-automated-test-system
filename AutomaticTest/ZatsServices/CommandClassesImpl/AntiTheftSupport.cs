// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
// SPDX-FileCopyrightText: 2025 Z-Wave Alliance https://z-wavealliance.org
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

    [CommandClassName("Anti-Theft Command Class")]
    [CommandClassId(COMMAND_CLASS_ANTITHEFT_V3.ID)]
    [MinVersion(3)]
    [MaxVersion(3)]
    public class AntiTheftSupport : AntiTheftSupportBase
    {
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return AntiTheftSupportData; }
            set { AntiTheftSupportData = (AntiTheftSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public AntiTheftSupport()
            : this(new AntiTheftSupportData() { })
        { }

        public AntiTheftSupport(AntiTheftSupportData antiTheftSupportData)
            : base(COMMAND_CLASS_ANTITHEFT_V3.ID)
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
                    if (command[1] == COMMAND_CLASS_ANTITHEFT.ANTITHEFT_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareStateReport();
                    }
                    else if (command[1] == COMMAND_CLASS_ANTITHEFT.ANTITHEFT_SET.ID)
                    {
                        ResultForSv = HandleSetCommandCmd(command);
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

        public SvResultNames HandleSetCommand(AntiTheftStateNames newState, IList <byte> magicCode, byte numberOfMagicCodeBytes, 
                                              ushort manufacturerId, IList<byte> antiTheftHint, ushort zWaveAllianceLockingEntityId)
        {
            AntiTheftStateNames currentState = AntiTheftSupportData.State;

            if (Version == 3)
            {
                if (currentState == AntiTheftStateNames.Unlocked && newState == AntiTheftStateNames.Locked)
                {
                    if (zWaveAllianceLockingEntityId == 0)
                    {
                        CommandClassSupportUtils.ReportAddLog(AntiTheftSupportData.LogLevel, 
                                    "{0} CC @ {1}: Set command (lock) ignored. Invalid Locking Entity ID '{2}'.",
                                    CommandClassShortName, EndPointName, zWaveAllianceLockingEntityId.ToByteArray().ToHexString());
                        return SvResultNames.Failed;
                    }
                    if (numberOfMagicCodeBytes < 1
                     || numberOfMagicCodeBytes > 10
                     || magicCode.Count != numberOfMagicCodeBytes)
                    {
                        CommandClassSupportUtils.ReportAddLog(AntiTheftSupportData.LogLevel, 
                                    "{0} CC @ {1}: Set command (lock) ignored. Received Magic Code: '{2}' - Received Length: {3}.",
                                    CommandClassShortName, EndPointName, magicCode.ToHexString(), numberOfMagicCodeBytes);
                        return SvResultNames.Failed;
                    }
                    AntiTheftSupportData.State = newState;
                    AntiTheftSupportData.MagicCode = magicCode;
                    AntiTheftSupportData.ManufacturerId = manufacturerId;
                    AntiTheftSupportData.AntiTheftHint = antiTheftHint;
                    AntiTheftSupportData.LockingEntityId = zWaveAllianceLockingEntityId;
                    CommandClassSupportUtils.ReportAddLog(AntiTheftSupportData.LogLevel, 
                                "{0} CC @ {1}: Set command (lock) accepted. New Magic Code: {2}.",
                                CommandClassShortName, EndPointName, magicCode.ToHexString());
                    return SvResultNames.Successful;
                }

                if (currentState == AntiTheftStateNames.Locked && newState == AntiTheftStateNames.Unlocked)
                {
                    if (AntiTheftSupportData.MagicCode.Count > 0
                     && magicCode.SequenceEqual(AntiTheftSupportData.MagicCode))
                    {
                        AntiTheftSupportData.State = newState;
                        AntiTheftSupportData.MagicCode = new List<byte>();
                        AntiTheftSupportData.ManufacturerId = 0;
                        AntiTheftSupportData.AntiTheftHint = new List<byte>();
                        AntiTheftSupportData.LockingEntityId = 0;
                        AntiTheftSupportData.IsRestricted = false;
                        CommandClassSupportUtils.ReportAddLog(AntiTheftSupportData.LogLevel, 
                                    "{0} CC @ {1}: Set command (unlock) accepted. Magic Code: {2}.",
                                    CommandClassShortName, EndPointName, magicCode.ToHexString());
                        return SvResultNames.Successful;
                    }
                    else
                    {
                        CommandClassSupportUtils.ReportAddLog(AntiTheftSupportData.LogLevel, 
                                    "{0} CC @ {1}: Set command (unlock) ignored. Received Magic Code: {2} - Stored Magic Code: {3}.",
                                    CommandClassShortName, EndPointName, magicCode.ToHexString(), AntiTheftSupportData.MagicCode.ToHexString());
                        return SvResultNames.Failed;
                    }

                }
                //AntiTheftSupportData.Restricted = AntiTheftRestrictNames.Unrestricted;
                if (currentState == newState)
                {
                    CommandClassSupportUtils.ReportAddLog(AntiTheftSupportData.LogLevel, 
                                "{0} CC @ {1}: Set command ignored (already at requested state '{2}').", 
                                CommandClassShortName, EndPointName, currentState);
                    return SvResultNames.Successful;
                }
            }
            CommandClassSupportUtils.ReportAddLog(AntiTheftSupportData.LogLevel, 
                        "{0} CC @ {1}: Set command ignored (Version < 3).", CommandClassShortName, EndPointName);
            return SvResultNames.Failed;
        }
        public SvResultNames HandleSetCommandCmd(byte[] command)
        {
            if (Version == 3)
            {
                COMMAND_CLASS_ANTITHEFT_V3.ANTITHEFT_SET setCmd = command;
                AntiTheftStateNames newState = setCmd.properties1.enable == 0 ? AntiTheftStateNames.Unlocked : AntiTheftStateNames.Locked;

                return HandleSetCommand(newState, setCmd.magicCode, setCmd.properties1.numberOfMagicCodeBytes, 
                                        (ushort) CommandClassSupportUtils.ByteFieldToUInt64(setCmd.manufacturerId, 2), 
                                        setCmd.antiTheftHintByte,
                                        (ushort)CommandClassSupportUtils.ByteFieldToUInt64(setCmd.zWaveAllianceLockingEntityId, 2));
            }
            CommandClassSupportUtils.ReportAddLog(AntiTheftSupportData.LogLevel,
                        "{0} CC @ {1}: Set command ignored (Version < 3).", CommandClassShortName, EndPointName);
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
                case COMMAND_CLASS_ANTITHEFT_V3.ANTITHEFT_SET.ID:
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
