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

// ToDo: Supervision Report Status 'Working' for FanModeSet (optional)

namespace Zats.Services
{
    [CommandClassName("Thermostat Fan Mode Command Class")]
    [CommandClassId(COMMAND_CLASS_THERMOSTAT_FAN_MODE.ID)]
    [MinVersion(1)]
    [MaxVersion(5)]
    public class ThermostatFanModeSupport : CommandClassSupport
    {
        private ThermostatFanModeSupportData ThermostatFanModeSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return ThermostatFanModeSupportData; }
            set { ThermostatFanModeSupportData = (ThermostatFanModeSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private ThermostatFanModes[] HighestAllowedFanModes = new ThermostatFanModes[5]
        {   ThermostatFanModes.High, ThermostatFanModes.Medium,
            ThermostatFanModes.HumidityCirculation, ThermostatFanModes.Quiet,
            ThermostatFanModes.ExternalCirculation
        };

        public ThermostatFanModeSupport()
            : this(new ThermostatFanModeSupportData() { })
        { }

        public ThermostatFanModeSupport(ThermostatFanModeSupportData thermostatFanModeSupportData)
            : base(COMMAND_CLASS_THERMOSTAT_FAN_MODE.ID)
        {
            ThermostatFanModeSupportData = thermostatFanModeSupportData;
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
                if (isHandlingAllowed && command != null && command.Length > 1 && ThermostatFanModeSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_THERMOSTAT_FAN_MODE.THERMOSTAT_FAN_MODE_GET.ID)
                    {
                        rpt = PrepareReport();
                    }
                    else if (command[1] == COMMAND_CLASS_THERMOSTAT_FAN_MODE.THERMOSTAT_FAN_MODE_SUPPORTED_GET.ID)
                    {
                        rpt = PrepareSupportedReport();
                    }
                    else if (command[1] == COMMAND_CLASS_THERMOSTAT_FAN_MODE.THERMOSTAT_FAN_MODE_SET.ID)
                    {
                        ResultForSv = HandleFanModeSetCmd(command, sendSrcNode);
                    }

                    if (rpt != null && IsResponseAllowed())
                    {
                        ApiOperation sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command, sendDstNode.EndPointId);
                }
            }
        }

        private byte[] PrepareReport()
        {
            if (Version <= 2)
            {
                COMMAND_CLASS_THERMOSTAT_FAN_MODE.THERMOSTAT_FAN_MODE_REPORT rpt = new COMMAND_CLASS_THERMOSTAT_FAN_MODE.THERMOSTAT_FAN_MODE_REPORT() { };
                rpt.properties1.fanMode = (byte) ThermostatFanModeSupportData.CurrentFanMode;
                return rpt;
            }
            if (Version >= 3 && Version <= 5)
            {
                COMMAND_CLASS_THERMOSTAT_FAN_MODE_V3.THERMOSTAT_FAN_MODE_REPORT rpt = new COMMAND_CLASS_THERMOSTAT_FAN_MODE_V3.THERMOSTAT_FAN_MODE_REPORT() { };
                rpt.properties1.fanMode = (byte) ThermostatFanModeSupportData.CurrentFanMode;
                rpt.properties1.off = (byte) (ThermostatFanModeSupportData.Off ? 0x01 : 0x00);
                return rpt;
            }
            return null;
        }
        private byte[] PrepareSupportedReport()
        {
            if (Version >= 1 && Version <= 5)
            {
                COMMAND_CLASS_THERMOSTAT_FAN_MODE.THERMOSTAT_FAN_MODE_SUPPORTED_REPORT rpt = new COMMAND_CLASS_THERMOSTAT_FAN_MODE.THERMOSTAT_FAN_MODE_SUPPORTED_REPORT() { };
                rpt.bitMask = CommandClassSupportUtils.CreateBitmaskFromList(ThermostatFanModeSupportData.FanModesSupportedList, 0);
                return rpt;
            }
            return null;
        }

        private SvResultNames HandleFanModeSet(ThermostatFanModes fanMode, bool off, bool enforceUnsupportedMode, NodeTag srcNode)
        {
            if (Version == 1)
            {
                if (fanMode <= HighestAllowedFanModes[Version - 1])
                {
                    if (ThermostatFanModeSupportData.FanModesSupportedList.Contains(fanMode) || enforceUnsupportedMode)
                    {
                        ThermostatFanModeSupportData.CurrentFanMode = fanMode;
                        CommandClassSupportUtils.ReportAddLog(ThermostatFanModeSupportData.LogLevel, 
                                    "{0} CC @ {1}: Fan Mode set to {2,2:#0} (0x{2:X2}) = {3}.",
                                    CommandClassShortName, EndPointName, (byte) fanMode, fanMode);
                        if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                        {
                            SendUnsolicitedReport(PrepareReport(), srcNode);
                        }
                        SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
                        return SvResultNames.Successful;
                    }
                }
            }
            if (Version >= 2 && Version <= 5)
            {
                if (fanMode <= HighestAllowedFanModes[Version - 1])
                {
                    if (ThermostatFanModeSupportData.FanModesSupportedList.Contains(fanMode) || enforceUnsupportedMode)
                    {
                        ThermostatFanModeSupportData.CurrentFanMode = fanMode;
                        ThermostatFanModeSupportData.Off = off;
                        CommandClassSupportUtils.ReportAddLog(ThermostatFanModeSupportData.LogLevel, 
                                    "{0} CC @ {1}: Fan Mode set to {2,2:#0} (0x{2:X2}) = {3}, Off is {4}.",
                                    CommandClassShortName, EndPointName, (byte) fanMode, fanMode, off);
                        if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                        {
                            SendUnsolicitedReport(PrepareReport(), srcNode);
                        }
                        SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
                        return SvResultNames.Successful;
                    }
                }
            }
            return SvResultNames.Failed;
        }
        private SvResultNames HandleFanModeSetCmd(byte[] command, NodeTag srcNode)
        {
            if (Version == 1)
            {
                COMMAND_CLASS_THERMOSTAT_FAN_MODE.THERMOSTAT_FAN_MODE_SET setCommand = command;
                return HandleFanModeSet((ThermostatFanModes)setCommand.properties1.fanMode, false, false, srcNode);
            }
            if (Version >= 2 && Version <= 5)
            {
                COMMAND_CLASS_THERMOSTAT_FAN_MODE_V2.THERMOSTAT_FAN_MODE_SET setCommand = command;
                return HandleFanModeSet((ThermostatFanModes)setCommand.properties1.fanMode, setCommand.properties1.off != 0x00, false, srcNode);
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
                case COMMAND_CLASS_THERMOSTAT_FAN_MODE.THERMOSTAT_FAN_MODE_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        supervisionReport.status = (byte)SvStatusNames.Success;
                    }
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }


        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is ThermostatFanModeSupportData newData)
            {
                if (ThermostatFanModeSupportData.CurrentFanMode != newData.CurrentFanMode
                 || ThermostatFanModeSupportData.Off != newData.Off)
                {
                    HandleFanModeSet(newData.CurrentFanMode, newData.Off, true, NodeTag.Empty);
                    //SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) -> done in HandleFanModeSet()
                }

                if (!CommandClassSupportUtils.ListElementsEqual(ThermostatFanModeSupportData.FanModesSupportedList, newData.FanModesSupportedList))
                {
                    ThermostatFanModeSupportData.FanModesSupportedList = newData.FanModesSupportedList;
                    if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        SendUnsolicitedReport(PrepareSupportedReport(), NodeTag.Empty);
                    }
                }
                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class ThermostatFanModeSupportData : CommandClassSupportData
    {
        // Internals accessible from TestCases or EDE
        // V1+ Supported Report

        // V1+ Report
        // CurrentFanMode: Allowed modes from 0x00 to: 0x03 (V1); 0x05 (V2); 0x07 (V3); 0x0A (V4); 0x0B (V5)
        [ParameterSeparator(1, "Thermostat Fan Mode Report")]
        [XmlDataParameter(1, 0x44, 0x03, new byte[] { 0x00, 0x00 }, EditType.Config | EditType.Edit)]
        public ThermostatFanModes CurrentFanMode { get; set; } = ThermostatFanModes.AutoLow;

        // V2+ Report
        [XmlDataParameter(2, 0x44, 0x03, new byte[] { 0x00, 0x02 }, EditType.Config | EditType.Edit)]
        public bool Off { get; set; } = false;

        [ParameterSeparator(1, "Thermostat Fan Mode Supported Report")]
        [XmlDataParameter(1, 0x44, 0x05, new byte[] { 0x00 }, EditType.Config)]
        public List<ThermostatFanModes> FanModesSupportedList { get; set; } = new List<ThermostatFanModes>();
    }
    public enum ThermostatFanModes : byte
    {
        [Description("0x00 Auto low")]
        AutoLow = 0x00,                 // V1+
        [Description("0x01 Low")]
        Low = 0x01,                     // V1+
        [Description("0x02 Auto high")]
        AutoHigh = 0x02,                // V1+
        [Description("0x03 High")]
        High = 0x03,                    // V1+
        [Description("0x04 Auto medium")]
        AutoMedium = 0x04,              // V2+
        [Description("0x05 Medium")]
        Medium = 0x05,                  // V2+
        [Description("0x06 Circulation")]
        Circulation = 0x06,             // V3+
        [Description("0x07 Humidity circulation")]
        HumidityCirculation = 0x07,     // V3+
        [Description("0x08 Left and Right")]
        LeftAndRight = 0x08,            // V4+
        [Description("0x09 Up and Down")]
        UpAndDown = 0x09,               // V4+
        [Description("0x0A Quiet")]
        Quiet = 0x0A,                   // V4+
        [Description("0x0B External circulation")]
        ExternalCirculation = 0x0B,     // V5++
    }
}
