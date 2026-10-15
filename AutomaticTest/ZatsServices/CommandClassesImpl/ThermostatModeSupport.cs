// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Utils;
using Utils.UI.Enums;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;

// ToDo: Supervision Report Status 'Working' for ModeSet (optional)
// Basic Mapping for Thermostat HVAC DT and Thermostat Setback DT is not supported

namespace Zats.Services
{
    [CommandClassName("Thermostat Mode Command Class")]
    [CommandClassId(COMMAND_CLASS_THERMOSTAT_MODE.ID)]
    [MinVersion(1)]
    [MaxVersion(3)]
    public class ThermostatModeSupport : CommandClassSupport
    {
        private ThermostatModeSupportData ThermostatModeSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return ThermostatModeSupportData; }
            set { ThermostatModeSupportData = (ThermostatModeSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public ThermostatModeSupport()
            : this(new ThermostatModeSupportData() { })
        { }

        public ThermostatModeSupport(ThermostatModeSupportData thermostatModeSupportData)
            : base(COMMAND_CLASS_THERMOSTAT_MODE.ID)
        {
            ThermostatModeSupportData = thermostatModeSupportData;
            ThermostatModeSupportData.SupportClassObject = this;
        }

        public override void StartEmulation()
        {
            ThermostatModeSupportData.SupportClassObject = this;
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
                if (isHandlingAllowed && command != null && command.Length > 1 && ThermostatModeSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_THERMOSTAT_MODE.THERMOSTAT_MODE_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareReport();
                    }
                    else if (command[1] == COMMAND_CLASS_THERMOSTAT_MODE.THERMOSTAT_MODE_SUPPORTED_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareSupportedReport();
                    }
                    else if (command[1] == COMMAND_CLASS_THERMOSTAT_MODE.THERMOSTAT_MODE_SET.ID)
                    {
                        ResultForSv = HandleModeSetCmd(command, sendSrcNode);
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

        private byte[] PrepareReport()
        {
            if (Version <= 2)
            {
                COMMAND_CLASS_THERMOSTAT_MODE.THERMOSTAT_MODE_REPORT rpt = new COMMAND_CLASS_THERMOSTAT_MODE.THERMOSTAT_MODE_REPORT() { };
                rpt.properties1.mode = (byte) ThermostatModeSupportData.CurrentMode;
                return rpt;
            }
            if (Version == 3)
            {
                COMMAND_CLASS_THERMOSTAT_MODE_V3.THERMOSTAT_MODE_REPORT rpt = new COMMAND_CLASS_THERMOSTAT_MODE_V3.THERMOSTAT_MODE_REPORT() { };
                rpt.properties1.mode = (byte) ThermostatModeSupportData.CurrentMode;
                rpt.properties1.noOfManufacturerDataFields = 0;
                if (ThermostatModeSupportData.CurrentMode == ThermostatModes.MANUFACTURER_SPECIFIC)
                {
                    rpt.properties1.noOfManufacturerDataFields = (byte) ThermostatModeSupportData.ManufacturerData.Count;
                    rpt.manufacturerData = ThermostatModeSupportData.ManufacturerData;
                }
                return rpt;
            }
            return null;
        }

        private byte[] PrepareSupportedReport()
        {
            if (Version >= 1 && Version <= 3)
            {
                COMMAND_CLASS_THERMOSTAT_MODE.THERMOSTAT_MODE_SUPPORTED_REPORT rpt = new COMMAND_CLASS_THERMOSTAT_MODE.THERMOSTAT_MODE_SUPPORTED_REPORT() { };
                rpt.bitMask = CommandClassSupportUtils.CreateBitmaskFromList(ThermostatModeSupportData.ModesSupportedList, 0);
                return rpt;
            }
            return null;
        }

        public SvResultNames HandleModeSet(ThermostatModes modeName, List<byte> manufacturerData, bool enforceUnsupportedMode, NodeTag srcNode)
        {
            ThermostatModes[] highestAllowedModes = new ThermostatModes[3] { ThermostatModes.AUTO_CHANGEOVER, ThermostatModes.AWAY, ThermostatModes.FULL_POWER };

            if (modeName <= highestAllowedModes[Version - 1] && modeName != ThermostatModes.RESERVED1)
            {
                if (ThermostatModeSupportData.ModesSupportedList.Contains(modeName) || enforceUnsupportedMode)
                {
                    if (modeName != ThermostatModes.RESUME_ON) // No resume (!= 0x05)
                    {
                        ThermostatModeSupportData.CurrentMode = modeName;
                        CommandClassSupportUtils.ReportAddLog(ThermostatModeSupportData.LogLevel,
                                    "{0} CC @ {1}: Mode set to {2,2:#0} (0x{2:X2}) = '{3}'.",
                                    CommandClassShortName, EndPointName, (byte) modeName, modeName);
                        if (modeName != ThermostatModes.OFF)
                        {
                            ThermostatModeSupportData.LastActiveMode = modeName;
                        }
                        if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                        {
                            SendUnsolicitedReport(PrepareReport(), srcNode);
                        }
                        SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
                        return SvResultNames.Successful;
                    }
                    if (ThermostatModeSupportData.LastActiveMode != ThermostatModes.OFF) // Resume possible?
                    {
                        ThermostatModeSupportData.CurrentMode = ThermostatModeSupportData.LastActiveMode;
                        CommandClassSupportUtils.ReportAddLog(ThermostatModeSupportData.LogLevel,
                                    "{0} CC @ {1}: Resuming to Mode {2,2:#0} (0x{2:X2}) = '{3}'.",
                                    CommandClassShortName, EndPointName, (byte) ThermostatModeSupportData.CurrentMode, ThermostatModeSupportData.CurrentMode);
                        if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                        {
                            SendUnsolicitedReport(PrepareReport(), srcNode);
                        }
                        SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
                        return SvResultNames.Successful;
                    }
                    CommandClassSupportUtils.ReportAddLog(ThermostatModeSupportData.LogLevel,
                                "{0} CC @ {1}: Resuming not possible, LastActiveMode is {2,2:#0} (0x{2:X2}).",
                                CommandClassShortName, EndPointName, (byte) ThermostatModeSupportData.LastActiveMode);
                    return SvResultNames.Failed;
                }
            }
            if (Version == 3 && modeName == ThermostatModes.MANUFACTURER_SPECIFIC)
            {
                if (ThermostatModeSupportData.ModesSupportedList.Contains(modeName) || enforceUnsupportedMode)
                {
                    ThermostatModeSupportData.CurrentMode = modeName;
                    ThermostatModeSupportData.ManufacturerData = manufacturerData;
                    ThermostatModeSupportData.LastActiveMode = modeName;
                    CommandClassSupportUtils.ReportAddLog(ThermostatModeSupportData.LogLevel,
                                "{0} CC @ {1}: Mode set to {2,2:#0} (0x{2:X2} = '{3}') with data: '{4}'.",
                                CommandClassShortName, EndPointName, (byte) modeName, modeName, BitConverter.ToString(ThermostatModeSupportData.ManufacturerData.ToArray()));
                    if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    {
                        SendUnsolicitedReport(PrepareReport(), srcNode);
                    }
                    SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
                    return SvResultNames.Successful;
                }
            }
            CommandClassSupportUtils.ReportAddLog(ThermostatModeSupportData.LogLevel,
                        "{0} CC @ {1}: Set with mode {2} = 0x{2:X2} ignored.", CommandClassShortName, EndPointName, modeName);
            return SvResultNames.Failed;
        }
        public SvResultNames HandleModeSetCmd(byte[] command, NodeTag srcNode)
        {
            byte modeByte = 0xFF;
            List<byte> manufacturerData = new List<byte>();

            if (Version <= 2)
            {
                modeByte = ((COMMAND_CLASS_THERMOSTAT_MODE.THERMOSTAT_MODE_SET)command).properties1.mode;
            }
            if (Version == 3)
            {
                modeByte = ((COMMAND_CLASS_THERMOSTAT_MODE_V3.THERMOSTAT_MODE_SET)command).properties1.mode;
            }
            ThermostatModes modeName = (ThermostatModes) modeByte;
            if (Version == 3 && modeName == ThermostatModes.MANUFACTURER_SPECIFIC)
            {
                COMMAND_CLASS_THERMOSTAT_MODE_V3.THERMOSTAT_MODE_SET setCommand = command;
                manufacturerData = (List<byte>) setCommand.manufacturerData;
            }
            return HandleModeSet(modeName, manufacturerData, false, srcNode);
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
                case COMMAND_CLASS_THERMOSTAT_MODE.THERMOSTAT_MODE_SET.ID:
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
            if (data is ThermostatModeSupportData newData)
            {
                if (ThermostatModeSupportData.LastActiveMode != newData.LastActiveMode
                 || ThermostatModeSupportData.CurrentMode != newData.CurrentMode
                 || !ThermostatModeSupportData.ManufacturerData.SequenceEqual(newData.ManufacturerData))
                {
                    HandleModeSet(newData.CurrentMode, newData.ManufacturerData, true, NodeTag.Empty);
                }

                if (!CommandClassSupportUtils.ListElementsEqual(ThermostatModeSupportData.ModesSupportedList, newData.ModesSupportedList))
                {
                    ThermostatModeSupportData.ModesSupportedList = newData.ModesSupportedList;
                    if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        SendUnsolicitedReport(PrepareSupportedReport(), NodeTag.Empty);
                    }
                }
                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class ThermostatModeSupportData : CommandClassSupportData, IBasicMapping
    {
        // Internals accessible from TestCases or EDE
        public ThermostatModes LastActiveMode { get; set; } = ThermostatModes.OFF;
        // V1+ Report
        // CurrentMode: Allowed modes: 0x00...0x0A (V1); 0x00...0x0D (V2); 0x00...0x0D, 0x0F, 0x1F (V3)
        [ParameterSeparator(1, "Thermostat Mode Report")]
        [XmlDataParameter(1, 0x40, 0x03, new byte[] { 0x00, 0x00 }, EditType.Config | EditType.Edit)]
        public ThermostatModes CurrentMode { get; set; } = ThermostatModes.OFF;

        //public List<byte> ModesSupportedBitmask { get; set; } = new List<byte> { 0xFF, 0x0F };
        // V3 Set / Report
        [XmlDataParameter(3, 0x40, 0x03, new byte[] { 0x01 }, EditType.Config | EditType.Edit)]
        public List<byte> ManufacturerData { get; set; } = new List<byte>();


        // V1+ Supported Report
        // ModesSupportedBitmask: Allowed masks: from 0x00 to { 0xFF, 0x07 } (V1);
        //                                       from 0x00 to { 0xFF, 0x3F } (V2);
        //                                       from 0x00 to { 0xFF, 0x3F } and { 0xFF, 0xBF } and to { 0xFF, 0xBF, 0x00, 0x00, 0x80 } (V5)
        [ParameterSeparator(1, "Thermostat Mode Supported Report")]
        [XmlDataParameter(1, 0x40, 0x05, new byte[] { 0x00 }, EditType.Config)]
        public List<ThermostatModes> ModesSupportedList { get; set; } = new List<ThermostatModes>();

        // Basic CC Mapping
        [JsonIgnore]
        public ThermostatModeSupport SupportClassObject;
        // Basic Mapping: Value 0x00 maps to BasicSetOffMode and Value 0xFF maps to BasicSetOnMode.
        // Z-Wave Plus V1: BasicSetOffMode is interpreted as Energy Saving Mode and BasicSetOnMode is interpreted as Comfort Mode.
        // Z-Wave Plus V2: BasicSetOffMode and BasicSetOnMode will be set directly.
        // Allowed values for BasicSetOnMode  (ZWPv2): 0x01, 0x02, 0x03. Mode MUST be supported in ModesSupportedList!
        // Allowed values for BasicSetOffMode (ZWPv2): 0x00 (V1+), 0x0B, 0x0C, 0x0D (V2+). Mode MUST be supported in ModesSupportedList!
        public byte BasicZWavePlusVersion { get; set; } = 2;    // Allowed: 1 and 2
        public ThermostatModes BasicSetOnMode { get; set; } = ThermostatModes.HEAT;
        public ThermostatModes BasicSetOffMode { get; set; } = ThermostatModes.OFF;
        bool IBasicMapping.BasicSet(byte value, NodeTag srcNode)
        {
            if (SupportClassObject.Version <= 3)
            {
                COMMAND_CLASS_THERMOSTAT_MODE_V3.THERMOSTAT_MODE_SET setCmd = new COMMAND_CLASS_THERMOSTAT_MODE_V3.THERMOSTAT_MODE_SET() { };
                switch (value)
                {
                    case 0x00: setCmd.properties1.mode = (byte) BasicSetOffMode; break;
                    case 0xFF: setCmd.properties1.mode = (byte) BasicSetOnMode;  break;
                    default:
                        if (!ModesSupportedList.Contains((ThermostatModes)value))
                        {
                            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0} CC @ {1}: Basic Set with value 0x{2:X2} ignored.",
                                        SupportClassObject.CommandClassShortName, SupportClassObject.EndPointName, value);
                            return false;
                        }
                        setCmd.properties1.mode = value; break;
                }
                setCmd.properties1.noOfManufacturerDataFields = 0;  // also matches Reserved field in V1/V2
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0} CC @ {1}: Basic Set with value 0x{2:X2} accepted.", 
                            SupportClassObject.CommandClassShortName, SupportClassObject.EndPointName, value);
                return SupportClassObject.HandleModeSetCmd(setCmd, srcNode) == SvResultNames.Successful;
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0} CC @ {1}: Basic Set with value 0x{2:X2} ignored.", 
                        SupportClassObject.CommandClassShortName, SupportClassObject.EndPointName, value);
            return false;
        }
        COMMAND_CLASS_BASIC_V2.BASIC_REPORT IBasicMapping.BasicV2Get()
        {
            COMMAND_CLASS_BASIC_V2.BASIC_REPORT rpt = new COMMAND_CLASS_BASIC_V2.BASIC_REPORT() { };
            rpt.currentValue = 0x00;
            if (BasicZWavePlusVersion == 1)
            {
                if (CurrentMode == BasicSetOnMode)
                    rpt.currentValue = 0xFF;
            }
            else
            {
                if (CurrentMode == BasicSetOnMode)
                    rpt.currentValue = 0xFF;
                else
                    rpt.currentValue = (byte)CurrentMode;
            }
            rpt.targetValue = 0;
            rpt.duration = 0;
            return rpt;
        }
    }

    public enum ThermostatModes : byte
    {
        [Description("0x00 Off")]
        OFF =               0x00,
        [Description("0x01 Heat")]
        HEAT =              0x01,
        [Description("0x02 Cool")]
        COOL =              0x02,
        [Description("0x03 Auto")]
        AUTO =              0x03,
        [Description("0x04 Auxiliary")]
        AUXILIARY =         0x04,
        [Description("0x05 Resume on")]
        RESUME_ON =         0x05,
        [Description("0x06 Fan")]
        FAN =               0x06,
        [Description("0x07 Furnace")]
        FURNACE =           0x07,
        [Description("0x08 Dry")]
        DRY =               0x08,
        [Description("0x09 Moist")]
        MOIST =             0x09,
        [Description("0x0A Auto changeover")]
        AUTO_CHANGEOVER =   0x0A,
        [Description("0x0B Energy heat")]
        ENERGY_HEAT =       0x0B,
        [Description("0x0C Energy cool")]
        ENERGY_COOL =       0x0C,
        [Description("0x0D Away")]
        AWAY =              0x0D,
        [Description("0x0E Reserved 1")]
        RESERVED1 =         0x0E,
        [Description("0x0F Full power")]
        FULL_POWER =        0x0F,
        [Description("0x1F Manufacturer specific")]
        MANUFACTURER_SPECIFIC = 0x1F
    };
}
