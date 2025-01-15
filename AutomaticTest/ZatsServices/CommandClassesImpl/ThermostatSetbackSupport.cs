// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
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

// ToDo: Supervision Report Status 'Working' for SetbackSet (optional)

namespace Zats.Services
{
    public class ThermostatSetbackSupport : CommandClassSupport
    {
        private ThermostatSetbackSupportData ThermostatSetbackSupportData { get; set; }

        public readonly string CommandClassName = "Thermostat Setback CC";
        public string EndPointName = "";
        private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public ThermostatSetbackSupport()
            : this(new ThermostatSetbackSupportData() { })
        { }

        public ThermostatSetbackSupport(ThermostatSetbackSupportData thermostatSetbackSupportData)
            : base(COMMAND_CLASS_THERMOSTAT_SETBACK.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 1;
            ThermostatSetbackSupportData = thermostatSetbackSupportData;
            ThermostatSetbackSupportData.SupportClassObject = this;
            LogLevel = ThermostatSetbackSupportData.LogLevel;
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
                if (isResponseAllowed && command != null && command.Length > 1 && ThermostatSetbackSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_THERMOSTAT_SETBACK.THERMOSTAT_SETBACK_GET.ID)
                    {
                        rpt = PrepareSetbackReport();
                    }
                    else if (command[1] == COMMAND_CLASS_THERMOSTAT_SETBACK.THERMOSTAT_SETBACK_SET.ID)
                    {
                        ResultForSv = HandleSetbackSet(command);
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

        private byte[] PrepareSetbackReport()
        {
            if (Version >= 1)
            {
                COMMAND_CLASS_THERMOSTAT_SETBACK.THERMOSTAT_SETBACK_REPORT rpt = new COMMAND_CLASS_THERMOSTAT_SETBACK.THERMOSTAT_SETBACK_REPORT() { };
                rpt.properties1.setbackType = (byte) ThermostatSetbackSupportData.SetbackType;
                rpt.setbackState = ThermostatSetbackSupportData.SetbackState;
                return rpt;
            }
            return null;
        }

        public SvResultNames HandleSetbackSet(byte[] command)
        {
            if (Version >= 1)
            {
                COMMAND_CLASS_THERMOSTAT_SETBACK.THERMOSTAT_SETBACK_SET setCommand = command;
                SetbackTypeNames setbackType = (SetbackTypeNames ) setCommand.properties1.setbackType;
                byte setbackState = setCommand.setbackState;
                if (setbackType == SetbackTypeNames.NoOverride || setbackType == SetbackTypeNames.TemporaryOverride || setbackType == SetbackTypeNames.PermanentOverride)
                {
                    if (setbackState <= 0x7A || setbackState >= 0x80)
                    {
                        ThermostatSetbackSupportData.SetbackType = setbackType;
                        ThermostatSetbackSupportData.SetbackState = setbackState;
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Setback Type = 0x{2:X2} '{3}' - State = 0x{4:X2}).",
                                        CommandClassName, EndPointName, (byte) setbackType, setbackType, setbackState);
                        return SvResultNames.Successful;
                    }
                }
            }
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
                case COMMAND_CLASS_THERMOSTAT_SETBACK.THERMOSTAT_SETBACK_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }

    }

    public class ThermostatSetbackSupportData : IBasicMapping
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;

        // V1+ Report
        // Setback Type: 0x00 = No override; 0x01 = Temporary override; 0x02 = Permanent override; 0x03 = Reserved
        public SetbackTypeNames SetbackType { get; set; } = SetbackTypeNames.NoOverride;
        // Setback State: 0x80...0xFF & 0x00... 0x78 = setback; 0x79 = Frost protection;
        //                0x7A = Energy Saving Mode; 0x7B...0x7E = Reserved; 0x7F = Unused State
        public byte SetbackState { get; set; } = 0x00;

        // Basic CC Mapping
        public ThermostatSetbackSupport SupportClassObject;
        bool IBasicMapping.BasicSet(byte value)
        {
            if (SupportClassObject.Version <= 1)
            {
                COMMAND_CLASS_THERMOSTAT_SETBACK.THERMOSTAT_SETBACK_SET setCmd = new COMMAND_CLASS_THERMOSTAT_SETBACK.THERMOSTAT_SETBACK_SET() { };
                switch (value)
                {
                    case 0x00: setCmd.setbackState = 0x7A; setCmd.properties1.setbackType = (byte) SetbackTypeNames.NoOverride; break;
                    case 0xFF: setCmd.setbackState = 0x00; setCmd.properties1.setbackType = (byte) SetbackTypeNames.NoOverride; break;
                    default:
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Basic Set with value 0x{2:X2} ignored.", 
                                    SupportClassObject.CommandClassName, SupportClassObject.EndPointName, value);
                        return false;
                }
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Basic Set with value 0x{2:X2} accepted.", 
                            SupportClassObject.CommandClassName, SupportClassObject.EndPointName, value);
                return SupportClassObject.HandleSetbackSet(setCmd) == SvResultNames.Successful;
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Basic Set with value 0x{2:X2} ignored.", 
                        SupportClassObject.CommandClassName, SupportClassObject.EndPointName, value);
            return false;
        }
        COMMAND_CLASS_BASIC_V2.BASIC_REPORT IBasicMapping.BasicV2Get()
        {
            COMMAND_CLASS_BASIC_V2.BASIC_REPORT rpt = new COMMAND_CLASS_BASIC_V2.BASIC_REPORT() { };
            rpt.currentValue = (byte) (SetbackState == 0x7A ? 0x00 : 0xFF);
            rpt.targetValue = 0;
            rpt.duration = 0;
            return rpt;
        }
    }

    public enum SetbackTypeNames : byte
    { 
        NoOverride = 0x00, 
        TemporaryOverride = 0x01,
        PermanentOverride = 0x02,
        Reserved = 0x03
    }

    public enum SetbackStateNames : byte
    {
        // 0x00 ... 0x78 positive setback values in 1/10 degrees
        // 0x80 ... 0xFF negative setback values in 1/10 degrees
        FrostProtection = 0x79,
        EnergySavingMode = 0x7A,
        // 0x7B ... 0x7E reserved states
        Unused = 0x7F
    }

    public static class ThermostatSetbackUtils
    {
        public static string SetbackStateToString(object state)
        {
            string stateString;
            byte setbackState = (byte)Convert.ToSByte(state);

            if (setbackState >= 0 && setbackState <= 0x78 || setbackState >= 0x80 && setbackState <= 0xFF)
            {
                stateString = ((sbyte)setbackState / 10.0f).ToString() + " (0x" + setbackState.ToString("X2") + ")";
            }
            else if (Enum.IsDefined(typeof(SetbackStateNames), state))
            {
                stateString = ((SetbackStateNames)state).ToString();
            }
            else
            {
                stateString = "reserved (0x" + setbackState.ToString("X2") + ")";
            }

            return stateString;
        }
    }
}
