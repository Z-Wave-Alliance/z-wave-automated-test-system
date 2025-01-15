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

namespace Zats.Services
{
    public class ThermostatOperatingStateSupport : CommandClassSupport
    {
        private ThermostatOperatingStateSupportData ThermostatOperatingStateSupportData { get; set; }

        //private readonly static string CommandClassName = "Thermostat Operating State CC";
        //private string EndPointName = "";
        //private readonly int LogLevel;  // shortcut for SupportData.LogLevel

        public ThermostatOperatingStateSupport()
            : this(new ThermostatOperatingStateSupportData() { })
        { }

        public ThermostatOperatingStateSupport(ThermostatOperatingStateSupportData thermostatOperatingStateSupportData)
            : base(COMMAND_CLASS_THERMOSTAT_OPERATING_STATE.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 2;
            ThermostatOperatingStateSupportData = thermostatOperatingStateSupportData;
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

                if (isResponseAllowed && command != null && command.Length > 1 && ThermostatOperatingStateSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_THERMOSTAT_OPERATING_STATE.THERMOSTAT_OPERATING_STATE_GET.ID)
                    {
                        rpt = PrepareStateReport();
                    }
                    else if (command[1] == COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_SUPPORTED_GET.ID)
                    {
                        rpt = PrepareSupportedReport();
                    }
                    else if (command[1] == COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_GET.ID)
                    {
                        COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_REPORT[] stateReports = CreateLoggingReports(command);
                        IActionItem[] actionItems = new IActionItem[stateReports.Length];
                        for (int r = 0; r < stateReports.Length; r++)
                        {
#if FLA_TEST_REPORTSTOFOLLOW_SEGMENTATION
                            // A frame will be segmented if it has more than 34 data bytes (CC + command + 32 bytes) at Application Layer
                            //                                             CC command reportstofollow data
                            byte[] longestNonSegmentedFrame = new byte[] { 0x42, 0x06, 0x00,          1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31 };
                            byte[] shortestSegmentedFrame   = new byte[] { 0x42, 0x06, 0x00,          1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32 };
                            actionItems[r] = new SendDataExOperation(sendDstNode, sendSrcNode, longestNonSegmentedFrame, TxOptions, TxSecOptions, scheme, TxOptions2);
#endif
                            actionItems[r] = new SendDataExOperation(_network, sendDstNode, sendSrcNode, stateReports[r], TxOptions, TxSecOptions, scheme, TxOptions2);
                        }
                        ou.SetNextActionItems(actionItems);
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

        private byte[] PrepareStateReport()
        {
            if (Version == 1)
            { 
                COMMAND_CLASS_THERMOSTAT_OPERATING_STATE.THERMOSTAT_OPERATING_STATE_REPORT rpt = new COMMAND_CLASS_THERMOSTAT_OPERATING_STATE.THERMOSTAT_OPERATING_STATE_REPORT() { };
                rpt.properties1.operatingState = (byte) ThermostatOperatingStateSupportData.OperatingState;
                return rpt;
            }
            if (Version == 2)
            {
                COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_REPORT rpt = new COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_REPORT() { };
                rpt.properties1.operatingState = (byte) ThermostatOperatingStateSupportData.OperatingState;
                return rpt;
            }
            return null;
        }

        private byte[] PrepareSupportedReport()
        {
            if (Version == 2)
            {
                COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_LOGGING_SUPPORTED_REPORT rpt = new COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_LOGGING_SUPPORTED_REPORT() { };
                //rpt.bitMask = ThermostatOperatingStateSupportData.LoggingSupportedBitmask;
                rpt.bitMask = CommandClassSupportUtils.CreateBitmaskFromList(ThermostatOperatingStateSupportData.LoggingSupportedList, 0);
                rpt.bitMask[0] &= 0xFE;     // Bit 0 in Bit Mask 1 is not allocated to any Operating State and MUST be set to zero
                return rpt;
            }
            return null;
        }

        private COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_REPORT[] CreateLoggingReports(byte[] command)
        {
            COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_GET loggingGetCmd = (COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_GET)command;
            List<byte> requestedStates = (List<byte>) loggingGetCmd.bitMask;
            byte[] requestedStatesB = requestedStates.ToArray();

            List<COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_REPORT.TVG1> statesList = new List<COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_REPORT.TVG1>();

            for (byte state = 0x01; state <= 0x0B; state++) // ignore state 0x00 (Idle)
            {
                int index = state / 8;
                if (index < requestedStatesB.Length/* && index < ThermostatOperatingStateSupportData.LoggingSupportedBitmask.Count*/)
                {
                    int bit = 0x01 << (state % 8);
                    if ((requestedStatesB[index] & bit) != 0)
                    {
                        if (ThermostatOperatingStateSupportData.LoggingSupportedList.Contains((ThermostatOperatingStateNames) state))
                        //if ((ThermostatOperatingStateSupportData.LoggingSupportedBitmask[index] & bit) != 0)
                        {
                            COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_REPORT.TVG1 tmp = new COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_REPORT.TVG1();
                            tmp.properties1.operatingStateLogType = state;
                            tmp.usageTodayHours = ThermostatOperatingStateSupportData.OperatingStateLogs[state].UsageTodayHours;
                            tmp.usageTodayMinutes = ThermostatOperatingStateSupportData.OperatingStateLogs[state].UsageTodayMinutes;
                            tmp.usageYesterdayHours = ThermostatOperatingStateSupportData.OperatingStateLogs[state].UsageYesterdayHours;
                            tmp.usageYesterdayMinutes = ThermostatOperatingStateSupportData.OperatingStateLogs[state].UsageYesterdayMinutes;
                            statesList.Add(tmp);
                        }
                    }
                }
            }

            // statesList is now ready for all requested and supported operating states. For each state fitting 
            // one frame the frame data are created and stored into a separate list for use as actionItem data.
            byte statesPerFrame = (31 / 5);             // 31 Bytes per frame are available for vg1. Each vg1 has 5 bytes.
            byte reports = (byte)(((statesList.Count - 1) / statesPerFrame) + 1);

            COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_REPORT[] stateReports = new COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_REPORT[reports];

            for (int r = 0; r < reports; r++)
            {
                COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_REPORT rpt = new COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_REPORT() { };
                rpt.reportsToFollow = (byte)(reports - r - 1);
                for (int v = 0; v < statesPerFrame; v++)
                {
                    if (r * statesPerFrame + v < statesList.Count)
                    {
                        rpt.vg1.Add(statesList[r * statesPerFrame + v]);
                    }
                }
                stateReports[r] = rpt;
            }
            return stateReports;
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, 
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            supervisionReport.status = (byte) SvStatusNames.NoSupport;
            return;
        }
    }

    public class OperatingStateLog
    {
        public byte UsageTodayHours { get; set; } = 0;
        public byte UsageTodayMinutes { get; set; } = 0;
        public byte UsageYesterdayHours { get; set; } = 0;
        public byte UsageYesterdayMinutes { get; set; } = 0;
    };

    public class ThermostatOperatingStateSupportData
    {
        // V1+ Report
        // Operating State - valid values: V1: 0x00 to 0x06; V2: 0x00 to 0x0B; 0x00 = IDLE
        public ThermostatOperatingStateNames OperatingState { get; set; } = ThermostatOperatingStateNames.Idle;

        // V2+ Supported Report
        public List<ThermostatOperatingStateNames> LoggingSupportedList { get; set; } = new List<ThermostatOperatingStateNames>
        {
            ThermostatOperatingStateNames.Idle,             // V1+, not announced in SupportedBitmask
            ThermostatOperatingStateNames.Heating,          // V1+
            ThermostatOperatingStateNames.Cooling,          // V1+
            ThermostatOperatingStateNames.FanOnly,          // V1+
            ThermostatOperatingStateNames.PendingHeat,      // V1+
            ThermostatOperatingStateNames.PendingCool,      // V1+
            ThermostatOperatingStateNames.VentEconomizer,   // V1+
            ThermostatOperatingStateNames.AuxHeating,       // V2+
            ThermostatOperatingStateNames.Heating2ndStage,  // V2+
            ThermostatOperatingStateNames.Cooling2ndStage,  // V2+
            ThermostatOperatingStateNames.AuxHeat2ndStage,  // V2+
            ThermostatOperatingStateNames.AuxHeat3rdStage,  // V2+
        };
        // LoggingSupportedBitmask: Allowed masks: from { 0x00 } to { 0xFF, 0x0F } (V2)
//        public List<byte> LoggingSupportedBitmask { get; set; } = new List<byte> { 0x00 };
        // V2+ Logging Report
        public OperatingStateLog[] OperatingStateLogs { get; set; } = new OperatingStateLog[12]
        {
            new OperatingStateLog(), new OperatingStateLog(), new OperatingStateLog(), new OperatingStateLog(), 
            new OperatingStateLog(), new OperatingStateLog(), new OperatingStateLog(), new OperatingStateLog(), 
            new OperatingStateLog(), new OperatingStateLog(), new OperatingStateLog(), new OperatingStateLog()
        };
    }

    public enum ThermostatOperatingStateNames : byte
    {
        Idle = 0x00,                // V1+
        Heating = 0x01,             // V1+
        Cooling = 0x02,             // V1+
        FanOnly = 0x03,             // V1+
        PendingHeat = 0x04,         // V1+
        PendingCool = 0x05,         // V1+
        VentEconomizer = 0x06,      // V1+
        AuxHeating = 0x07,          // V2+
        Heating2ndStage = 0x08,     // V2+
        Cooling2ndStage = 0x09,     // V2+
        AuxHeat2ndStage = 0x0A,     // V2+
        AuxHeat3rdStage = 0x0B,     // V2+
    }
}
