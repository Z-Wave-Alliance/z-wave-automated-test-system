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

// ToDo: Supervision Report Status 'Working' for ModeSet (optional)
// Basic Mapping for Thermostat HVAC DT and Thermostat Setback DT is not supported

namespace Zats.Services
{
    public class ThermostatModeSupport : CommandClassSupport
    {
        private ThermostatModeSupportData ThermostatModeSupportData { get; set; }

        public readonly string CommandClassName = "Thermostat Mode CC";
        public string EndPointName = "";
        private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public ThermostatModeSupport()
            : this(new ThermostatModeSupportData() { })
        { }

        public ThermostatModeSupport(ThermostatModeSupportData thermostatModeSupportData)
            : base(COMMAND_CLASS_THERMOSTAT_MODE.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 3;
            ThermostatModeSupportData = thermostatModeSupportData;
            ThermostatModeSupportData.SupportClassObject = this;
            LogLevel = ThermostatModeSupportData.LogLevel;
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
                if (isResponseAllowed && command != null && command.Length > 1 && ThermostatModeSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_THERMOSTAT_MODE.THERMOSTAT_MODE_GET.ID)
                    {
                        rpt = PrepareReport();
                    }
                    else if (command[1] == COMMAND_CLASS_THERMOSTAT_MODE.THERMOSTAT_MODE_SUPPORTED_GET.ID)
                    {
                        rpt = PrepareSupportedReport();
                    }
                    else if (command[1] == COMMAND_CLASS_THERMOSTAT_MODE.THERMOSTAT_MODE_SET.ID)
                    {
                        ResultForSv = HandleModeSet(command);
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

        public SvResultNames HandleModeSet(byte[] command)
        {
            ThermostatModes[] highestAllowedModes = new ThermostatModes[3] { ThermostatModes.AUTO_CHANGEOVER, ThermostatModes.AWAY, ThermostatModes.FULL_POWER };
            byte modeByte = 0xFF;

            if (Version > 3)
                return SvResultNames.Failed; 
            if (Version <= 2)
                modeByte = ((COMMAND_CLASS_THERMOSTAT_MODE.THERMOSTAT_MODE_SET)command).properties1.mode;
            if (Version == 3)
                modeByte = ((COMMAND_CLASS_THERMOSTAT_MODE_V3.THERMOSTAT_MODE_SET)command).properties1.mode;
            ThermostatModes modeName = (ThermostatModes) modeByte;

            if (modeName <= highestAllowedModes[Version - 1] && modeName != ThermostatModes.RESERVED1)
            {
                if (ThermostatModeSupportData.ModesSupportedList.Contains(modeName))
                {
                    if (modeName != ThermostatModes.RESUME_ON) // No resume (!= 0x05)
                    { 
                        ThermostatModeSupportData.CurrentMode = modeName;
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Mode set to {2,2:#0} (0x{2:X2}) = '{3}'.",
                                    CommandClassName, EndPointName, modeByte, modeName);
                        if (modeName != ThermostatModes.OFF)
                            ThermostatModeSupportData.LastActiveMode = modeName;
                        return SvResultNames.Successful;
                    }
                    if (ThermostatModeSupportData.LastActiveMode != ThermostatModes.OFF) // Resume possible?
                    {
                        ThermostatModeSupportData.CurrentMode = ThermostatModeSupportData.LastActiveMode;
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Resuming to Mode {2,2:#0} (0x{2:X2}) = '{3}'.", 
                                    CommandClassName, EndPointName, (byte) ThermostatModeSupportData.CurrentMode, ThermostatModeSupportData.CurrentMode);
                        return SvResultNames.Successful;
                    }
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Resuming not possible, LastActiveMode is {2,2:#0} (0x{2:X2}).", 
                                CommandClassName, EndPointName, (byte) ThermostatModeSupportData.LastActiveMode);
                    return SvResultNames.Failed;
                }
            }
            if (Version == 3 && modeName == ThermostatModes.MANUFACTURER_SPECIFIC)
            {
                COMMAND_CLASS_THERMOSTAT_MODE_V3.THERMOSTAT_MODE_SET setCommand = command;
                ThermostatModeSupportData.CurrentMode = modeName;
                ThermostatModeSupportData.ManufacturerData = (List<byte>)setCommand.manufacturerData;
                ThermostatModeSupportData.LastActiveMode = modeName;
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Mode set to {2,2:#0} (0x{2:X2} = '{3}') with data: '{4}'.", 
                            CommandClassName, EndPointName, modeByte, modeName, BitConverter.ToString(ThermostatModeSupportData.ManufacturerData.ToArray()));
                return SvResultNames.Successful;
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Set with mode {2} = 0x{2:X2} ignored.", CommandClassName, EndPointName, modeByte);
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
                case COMMAND_CLASS_THERMOSTAT_MODE.THERMOSTAT_MODE_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }

    }

    public class ThermostatModeSupportData : IBasicMapping
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
        public ThermostatModes LastActiveMode { get; set; } = 0x00;
        // V1+ Report
        // CurrentMode: Allowed modes: 0x00...0x0A (V1); 0x00...0x0D (V2); 0x00...0x0D, 0x0F, 0x1F (V3)
        public ThermostatModes CurrentMode { get; set; } = 0x00;

        // V1+ Supported Report
        // ModesSupportedBitmask: Allowed masks: from 0x00 to { 0xFF, 0x07 } (V1);
        //                                       from 0x00 to { 0xFF, 0x3F } (V2);
        //                                       from 0x00 to { 0xFF, 0x3F } and { 0xFF, 0xBF } and to { 0xFF, 0xBF, 0x00, 0x00, 0x80 } (V5)
        public List<ThermostatModes> ModesSupportedList { get; set; } = new List<ThermostatModes>
        {
            ThermostatModes.OFF,                    // 0x00  V1+
            ThermostatModes.HEAT,                   // 0x01  V1+
            //ThermostatModes.COOL,                   // 0x02  V1+
            ThermostatModes.AUTO,                   // 0x03  V1+
            //ThermostatModes.AUXILIARY,              // 0x04  V1+
            ThermostatModes.RESUME_ON,              // 0x05  V1+
            //ThermostatModes.FAN,                    // 0x06  V1+
            //ThermostatModes.FURNACE,                // 0x07  V1+

            //ThermostatModes.DRY,                    // 0x08  V1+
            //ThermostatModes.MOIST,                  // 0x09  V1+
            ThermostatModes.AUTO_CHANGEOVER,        // 0x0A  V1+
            //ThermostatModes.ENERGY_HEAT,            // 0x0B  V2+
            //ThermostatModes.ENERGY_COOL,            // 0x0C  V2+
            //ThermostatModes.AWAY,                   // 0x0D  V2+
            //ThermostatModes.RESERVED1               // 0x0E  not allowed
            //ThermostatModes.FULL_POWER,             // 0x0F  V3+
            //ThermostatModes.MANUFACTURER_SPECIFIC,  // 0x1F  V3+
        };
        //public List<byte> ModesSupportedBitmask { get; set; } = new List<byte> { 0xFF, 0x0F };
        // V3 Set / Report
        public List<byte> ManufacturerData { get; set; } = new List<byte> { };

        // Basic CC Mapping
        public ThermostatModeSupport SupportClassObject;
        // Basic Mapping: Value 0x00 maps to BasicSetOffMode and Value 0xFF maps to BasicSetOnMode.
        // Z-Wave Plus V1: BasicSetOffMode is interpreted as Energy Saving Mode and BasicSetOnMode is interpreted as Comfort Mode.
        // Z-Wave Plus V2: BasicSetOffMode and BasicSetOnMode will be set directly.
        // Allowed values for BasicSetOnMode  (ZWPv2): 0x01, 0x02, 0x03. Mode MUST be supported in ModesSupportedBitmask!
        // Allowed values for BasicSetOffMode (ZWPv2): 0x00 (V1+), 0x0B, 0x0C, 0x0D (V2+). Mode MUST be supported in ModesSupportedBitmask!
        public byte BasicZWavePlusVersion { get; set; } = 2;    // Allowed: 1 and 2
        public ThermostatModes BasicSetOnMode { get; set; } = ThermostatModes.HEAT;
        public ThermostatModes BasicSetOffMode { get; set; } = ThermostatModes.OFF;
        bool IBasicMapping.BasicSet(byte value)
        {
            if (SupportClassObject.Version <= 3)
            {
                COMMAND_CLASS_THERMOSTAT_MODE_V3.THERMOSTAT_MODE_SET setCmd = new COMMAND_CLASS_THERMOSTAT_MODE_V3.THERMOSTAT_MODE_SET() { };
                switch (value)
                {
                    case 0x00: setCmd.properties1.mode = (byte) BasicSetOffMode; break;
                    case 0xFF: setCmd.properties1.mode = (byte) BasicSetOnMode;  break;
                    default:
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Basic Set with value 0x{2:X2} ignored.", 
                                    SupportClassObject.CommandClassName, SupportClassObject.EndPointName, value);
                        return false;
                }
                setCmd.properties1.noOfManufacturerDataFields = 0;  // also matches Reserved field in V1/V2
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Basic Set with value 0x{2:X2} accepted.", 
                            SupportClassObject.CommandClassName, SupportClassObject.EndPointName, value);
                return SupportClassObject.HandleModeSet(setCmd) == SvResultNames.Successful;
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Basic Set with value 0x{2:X2} ignored.", 
                        SupportClassObject.CommandClassName, SupportClassObject.EndPointName, value);
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
                if (CurrentMode >= ThermostatModes.HEAT && CurrentMode <= ThermostatModes.AUTO)
                    rpt.currentValue = 0xFF;
            }
            rpt.targetValue = 0;
            rpt.duration = 0;
            return rpt;
        }

/*        public void SetSupportedModes(List<ThermostatModes> modes)
        {
            uint modesBitMask = 0;

            foreach(ThermostatModes mode in modes)
            {
                modesBitMask |= 1U << (int)mode;
            }

            ModesSupportedBitmask = new List<byte>(BitConverter.GetBytes(modesBitMask));

        }*/

/*        public List<ThermostatModes> GetSupportedModes()
        {
            List<ThermostatModes> supportedModes = new List<ThermostatModes>();

            for(int i = 0; i < ModesSupportedBitmask.Count * 8; i++)
            {
                if ((ModesSupportedBitmask[i / 8] & (1 << (i % 8))) > 0)
                {
                    supportedModes.Add((ThermostatModes)i);
                }
            }

            return supportedModes;
        }*/

    }

    public enum ThermostatModes : byte
    {
        OFF =               0x00,
        HEAT =              0x01,
        COOL =              0x02,
        AUTO =              0x03,
        AUXILIARY =         0x04,
        RESUME_ON =         0x05,
        FAN =               0x06,
        FURNACE =           0x07, 
        DRY =               0x08,
        MOIST =             0x09,
        AUTO_CHANGEOVER =   0x0A,
        ENERGY_HEAT =       0x0B,
        ENERGY_COOL =       0x0C,
        AWAY =              0x0D,
        RESERVED1 =         0x0E,
        FULL_POWER =        0x0F,
        MANUFACTURER_SPECIFIC = 0x1F
    };
}
