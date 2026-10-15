// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
using Newtonsoft.Json;
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
    [CommandClassName("Thermostat Setback Command Class")]
    [CommandClassId(COMMAND_CLASS_THERMOSTAT_SETBACK.ID)]
    [MinVersion(1)]
    [MaxVersion(1)]
    public class ThermostatSetbackSupport : CommandClassSupport
    {
        private ThermostatSetbackSupportData ThermostatSetbackSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return ThermostatSetbackSupportData; }
            set { ThermostatSetbackSupportData = (ThermostatSetbackSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public ThermostatSetbackSupport()
            : this(new ThermostatSetbackSupportData() { })
        { }

        public ThermostatSetbackSupport(ThermostatSetbackSupportData thermostatSetbackSupportData)
            : base(COMMAND_CLASS_THERMOSTAT_SETBACK.ID)
        {
            ThermostatSetbackSupportData = thermostatSetbackSupportData;
            ThermostatSetbackSupportData.SupportClassObject = this;
        }

        public override void StartEmulation()
        {
            ThermostatSetbackSupportData.SupportClassObject = this;
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
                if (isHandlingAllowed && command != null && command.Length > 1 && ThermostatSetbackSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_THERMOSTAT_SETBACK.THERMOSTAT_SETBACK_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareSetbackReport();
                    }
                    else if (command[1] == COMMAND_CLASS_THERMOSTAT_SETBACK.THERMOSTAT_SETBACK_SET.ID)
                    {
                        ResultForSv = HandleSetbackSetCmd(command, sendSrcNode);
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

        public SvResultNames HandleSetbackSet(SetbackTypeNames newSetbackType, byte newSetbackState, NodeTag srcNode)
        {
            if (Version >= 1)
            {
                if (newSetbackType == SetbackTypeNames.NoOverride 
                 || newSetbackType == SetbackTypeNames.TemporaryOverride 
                 || newSetbackType == SetbackTypeNames.PermanentOverride)
                {
                    if (newSetbackState <= 0x7A || newSetbackState >= 0x80)
                    {
                        ThermostatSetbackSupportData.SetbackType = newSetbackType;
                        ThermostatSetbackSupportData.SetbackState = newSetbackState;
                        CommandClassSupportUtils.ReportAddLog(ThermostatSetbackSupportData.LogLevel,
                                    "{0} CC @ {1}: Setback Type = 0x{2:X2} '{3}' - State = 0x{4:X2}).",
                                    CommandClassShortName, EndPointName, (byte)newSetbackType, newSetbackType, newSetbackState);
                        if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                        {
                            SendUnsolicitedReport(PrepareSetbackReport(), srcNode);
                        }
                        SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
                        return SvResultNames.Successful;
                    }
                }
            }
            return SvResultNames.Failed;
        }
        public SvResultNames HandleSetbackSetCmd(byte[] command, NodeTag srcNode)
        {
            if (Version >= 1)
            {
                COMMAND_CLASS_THERMOSTAT_SETBACK.THERMOSTAT_SETBACK_SET setCommand = command;
                return HandleSetbackSet((SetbackTypeNames)setCommand.properties1.setbackType, setCommand.setbackState, srcNode);
            }
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
                case COMMAND_CLASS_THERMOSTAT_SETBACK.THERMOSTAT_SETBACK_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    }
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }


        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is ThermostatSetbackSupportData newData)
            {
                if (ThermostatSetbackSupportData.SetbackType != newData.SetbackType
                 || ThermostatSetbackSupportData.SetbackState != newData.SetbackState)
                {
                    HandleSetbackSet(newData.SetbackType, newData.SetbackState, NodeTag.Empty);    // report and inform (EDE) -> HandleSetbackSet()
                }
                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }

    }

    public class ThermostatSetbackSupportData : CommandClassSupportData, IBasicMapping
    {
        // Internals accessible from TestCases or EDE

        // V1+ Report
        // Setback Type: 0x00 = No override; 0x01 = Temporary override; 0x02 = Permanent override; 0x03 = Reserved
        [ParameterSeparator(1, "Thermostat Setback Report")]
        [XmlDataParameter(1, 0x47, 0x03, new byte[] { 0x00, 0x00 }, EditType.Config | EditType.Edit)]
        public SetbackTypeNames SetbackType { get; set; } = SetbackTypeNames.NoOverride;
        // Setback State: 0x00 = 0 degrees setback; 0x79 = Frost protection; 0x7A = Energy Saving Mode; 0x7B...0x7E = Reserved; 0x7F = Unused State; 
        //                0x01... 0x78 and 0x80...0xFF = +/- 0.1 degrees (Kelvin) is added to/subtracted from the setpoint
        [XmlDataParameter(1, 0x47, 0x03, new byte[] { 0x01 }, EditType.Config | EditType.Edit)]
        public byte SetbackState { get; set; } = 0x00;

        // Basic CC Mapping
        [JsonIgnore]
        public ThermostatSetbackSupport SupportClassObject;
        bool IBasicMapping.BasicSet(byte value, NodeTag srcNode)
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
                return SupportClassObject.HandleSetbackSetCmd(setCmd, srcNode) == SvResultNames.Successful;
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
        [Description("0x00 No override")]
        NoOverride = 0x00,
        [Description("0x01 Temporary override")]
        TemporaryOverride = 0x01,
        [Description("0x02 Permanent pverride")]
        PermanentOverride = 0x02,
        [Description("0x03 Reserved")]
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
        public static string SetbackStateToString(byte setbackState)
        {
            string stateString;

            if (setbackState >= 0 && setbackState <= 0x78 || setbackState >= 0x80 && setbackState <= 0xFF)
            {
                sbyte signedSetbackState = unchecked((sbyte)setbackState);
                stateString = (signedSetbackState / 10.0f).ToString() + " Kelvin (0x" + setbackState.ToString("X2") + ")";
            }
            else if (Enum.IsDefined(typeof(SetbackStateNames), setbackState))
            {
                stateString = ((SetbackStateNames)setbackState).ToString();
            }
            else
            {
                stateString = "reserved (0x" + setbackState.ToString("X2") + ")";
            }

            return stateString;
        }
    }
}
