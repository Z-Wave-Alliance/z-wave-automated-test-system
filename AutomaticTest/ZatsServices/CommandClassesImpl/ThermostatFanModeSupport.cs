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

// ToDo: Supervision Report Status 'Working' for FanModeSet (optional)

namespace Zats.Services
{
    public class ThermostatFanModeSupport : CommandClassSupport
    {
        private ThermostatFanModeSupportData ThermostatFanModeSupportData { get; set; }

        private readonly string CommandClassName = "Thermostat Fan Mode CC";
        private string EndPointName = "";
        private readonly int LogLevel;  // shortcut for SupportData.LogLevel
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
            MinCCVersion = 1;
            MaxCCVersion = 5;
            ThermostatFanModeSupportData = thermostatFanModeSupportData;
            LogLevel = ThermostatFanModeSupportData.LogLevel;
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
                if (isResponseAllowed && command != null && command.Length > 1 && ThermostatFanModeSupportData != null)
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
                        ResultForSv = HandleFanModeSet(command);
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
        private SvResultNames HandleFanModeSet(byte[] command)
        {
            ThermostatFanModes[] highestAllowedFanModes = new ThermostatFanModes[5] 
            {   ThermostatFanModes.High, ThermostatFanModes.Medium, 
                ThermostatFanModes.HumidityCirculation, ThermostatFanModes.Quiet, 
                ThermostatFanModes.ExternalCirculation
            };
            if (Version == 1)
            {
                COMMAND_CLASS_THERMOSTAT_FAN_MODE.THERMOSTAT_FAN_MODE_SET setCommand = command;
                ThermostatFanModes fanMode = (ThermostatFanModes) setCommand.properties1.fanMode;
                if (fanMode <= HighestAllowedFanModes[Version - 1])
                {
                    if (ThermostatFanModeSupportData.FanModesSupportedList.Contains(fanMode))
                    {
                        ThermostatFanModeSupportData.CurrentFanMode = fanMode;
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Fan Mode set to {2,2:#0} (0x{2:X2}) = {3}.", 
                                    CommandClassName, EndPointName, (byte) fanMode, fanMode);
                        return SvResultNames.Successful;
                    }
                }
            }
            if (Version >= 2 && Version <= 5)
            {
                COMMAND_CLASS_THERMOSTAT_FAN_MODE_V2.THERMOSTAT_FAN_MODE_SET setCommand = command;
                ThermostatFanModes fanMode = (ThermostatFanModes) setCommand.properties1.fanMode;
                byte off = setCommand.properties1.off;
                if (fanMode <= HighestAllowedFanModes[Version - 1])
                {
                    if (ThermostatFanModeSupportData.FanModesSupportedList.Contains(fanMode))
                    {
                        ThermostatFanModeSupportData.CurrentFanMode = fanMode;
                        ThermostatFanModeSupportData.Off = off != 0x00;
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Fan Mode set to {2,2:#0} (0x{2:X2}) = {3}, Off is {4}.", 
                                    CommandClassName, EndPointName, (byte) fanMode, fanMode, off);
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
                case COMMAND_CLASS_THERMOSTAT_FAN_MODE.THERMOSTAT_FAN_MODE_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }

    }

    public class ThermostatFanModeSupportData
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
        // V1+ Supported Report
        public List<ThermostatFanModes> FanModesSupportedList { get; set; } = new List<ThermostatFanModes> 
        {
            ThermostatFanModes.AutoLow,             // 0x00  V1+
            ThermostatFanModes.Low,                 // 0x01  V1+
            ThermostatFanModes.AutoHigh,            // 0x02  V1+
            ThermostatFanModes.High,                // 0x03  V1+
            ThermostatFanModes.AutoMedium,          // 0x04  V2+
            ThermostatFanModes.Medium,              // 0x05  V2+
            ThermostatFanModes.Circulation,         // 0x06  V3+
            ThermostatFanModes.HumidityCirculation, // 0x07  V3+
            ThermostatFanModes.LeftAndRight,        // 0x08  V4+
            ThermostatFanModes.UpAndDown,           // 0x09  V4+
            ThermostatFanModes.Quiet,               // 0x0A  V4+
            ThermostatFanModes.ExternalCirculation, // 0x0B  V5+
        };
        // V1+ Report
        // CurrentFanMode: Allowed modes from 0x00 to: 0x03 (V1); 0x05 (V2); 0x07 (V3); 0x0A (V4); 0x0B (V5)
        public ThermostatFanModes CurrentFanMode { get; set; } = ThermostatFanModes.AutoLow;
        // V2+ Report
        public bool Off { get; set; } = false;
    }
    public enum ThermostatFanModes : byte
    {
        AutoLow = 0x00,                 // V1+
        Low = 0x01,                     // V1+
        AutoHigh = 0x02,                // V1+
        High = 0x03,                    // V1+
        AutoMedium = 0x04,              // V2+
        Medium = 0x05,                  // V2+
        Circulation = 0x06,             // V3+
        HumidityCirculation = 0x07,     // V3+
        LeftAndRight = 0x08,            // V4+
        UpAndDown = 0x09,               // V4+
        Quiet = 0x0A,                   // V4+
        ExternalCirculation = 0x0B,     // V5++
    }
}
