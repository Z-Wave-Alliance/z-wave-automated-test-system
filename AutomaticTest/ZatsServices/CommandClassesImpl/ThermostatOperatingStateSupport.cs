// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
// SPDX-FileCopyrightText: Z-Wave Alliance https://z-wavealliance.org
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Policy;
using System.Threading;
using Utils;
using Utils.UI.Enums;
using ZatsServices_netcore.CommandClassesImpl.Definitions.Interfaces;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;
using Newtonsoft.Json;
using Zats.Services.CommandClassesImpl;

namespace Zats.Services
{
    [CommandClassName("Thermostat Operating State Command Class")]
    [CommandClassId(COMMAND_CLASS_THERMOSTAT_OPERATING_STATE.ID)]
    [MinVersion(1)]
    [MaxVersion(2)]
    public class ThermostatOperatingStateSupport : CommandClassSupport
    {
        private ThermostatOperatingStateSupportData ThermostatOperatingStateSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return ThermostatOperatingStateSupportData; }
            set { ThermostatOperatingStateSupportData = (ThermostatOperatingStateSupportData)value; }
        }

        public ThermostatOperatingStateSupport()
            : this(new ThermostatOperatingStateSupportData() { })
        { }

        public ThermostatOperatingStateSupport(ThermostatOperatingStateSupportData thermostatOperatingStateSupportData)
            : base(COMMAND_CLASS_THERMOSTAT_OPERATING_STATE.ID)
        {
            ThermostatOperatingStateSupportData = thermostatOperatingStateSupportData;
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

                if (isHandlingAllowed && command != null && command.Length > 1 && ThermostatOperatingStateSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_THERMOSTAT_OPERATING_STATE.THERMOSTAT_OPERATING_STATE_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareStateReport();
                    }
                    else if (command[1] == COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_SUPPORTED_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareSupportedReport();
                    }
                    else if (command[1] == COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_GET.ID && !IsSupervised())
                    {
                        if (IsAnsweringAllowed())
                        {
                            COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_REPORT[] stateReports = CreateLoggingReportsCmd(command);
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
                List<ThermostatOperatingStateNames> supportedStateLogs = new List<ThermostatOperatingStateNames>();
                foreach (OperatingStateLog newOperatingStateLog in ThermostatOperatingStateSupportData.OperatingStateLogs)
                {
                    supportedStateLogs.Add(newOperatingStateLog.OperatingState);
                }
                rpt.bitMask = CommandClassSupportUtils.CreateBitmaskFromList(supportedStateLogs, 0);
                if (rpt.bitMask.Count != 0)
                {
                    rpt.bitMask[0] &= 0xFE;     // Bit 0 in Bit Mask 1 is not allocated to any Operating State and MUST be set to zero
                }
                return rpt;
            }
            return null;
        }

        private COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_REPORT[] CreateLoggingReports(List<byte> requestedStates)
        {
            List<COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_REPORT.TVG1> statesList = new List<COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_REPORT.TVG1>();

            for (byte state = 0x01; state <= 0x0B; state++) // ignore state 0x00 (Idle)
            {
                if (requestedStates.Contains(state)/* && ThermostatOperatingStateSupportData.LoggingSupportedList.Contains((ThermostatOperatingStateNames)state)*/)
                {
                    if (ThermostatOperatingStateSupportData.OperatingStateLogs.Find(x => (byte) x.OperatingState == state) is OperatingStateLog currentStateLog)
                    {
                        COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_REPORT.TVG1 tmp = new COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_REPORT.TVG1();
                        tmp.properties1.operatingStateLogType = (byte) currentStateLog.OperatingState;
                        tmp.usageTodayHours = currentStateLog.UsageTodayHours;
                        tmp.usageTodayMinutes = currentStateLog.UsageTodayMinutes;
                        tmp.usageYesterdayHours = currentStateLog.UsageYesterdayHours;
                        tmp.usageYesterdayMinutes = currentStateLog.UsageYesterdayMinutes;
                        statesList.Add(tmp);
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
        private COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_REPORT[] CreateLoggingReportsCmd(byte[] command)
        {
            COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_GET loggingGetCmd = (COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_GET)command;
            List<byte> requestedStates = (List<byte>) loggingGetCmd.bitMask;
            return CreateLoggingReports(requestedStates);
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, 
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            supervisionReport.status = (byte) SvStatusNames.NoSupport;
            return;
        }


        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is ThermostatOperatingStateSupportData newData)
            {
                bool isStateChanged = ThermostatOperatingStateSupportData.OperatingState != newData.OperatingState;

                bool isSupportedListChanged = false;

                bool isStateLogChanged = false;
                List<byte> changedStateLogs = new List<byte>();
                List<OperatingStateLog> stateLogsRemoved = ThermostatOperatingStateSupportData.OperatingStateLogs.FindAll(x => newData.OperatingStateLogs.Find(y => y.OperatingState == x.OperatingState) == null);
              //List<OperatingStateLog> stateLogsAdded   = newData.OperatingStateLogs.FindAll(x => ThermostatOperatingStateSupportData.OperatingStateLogs.Find(y => y.OperatingState == x.OperatingState) == null);
                if (stateLogsRemoved.Count > 0 /*|| stateLogsAdded.Count > 0*/)
                {
                    isSupportedListChanged = true;
                    isStateLogChanged = true;
                    stateLogsRemoved.ForEach(x => ThermostatOperatingStateSupportData.OperatingStateLogs.Remove(x));
                }
                foreach (OperatingStateLog newOperatingStateLog in newData.OperatingStateLogs)
                {
                    if (ThermostatOperatingStateSupportData.OperatingStateLogs.Find(x => x.OperatingState == newOperatingStateLog.OperatingState) is OperatingStateLog curOperatingStateLog)
                    {
                        if (curOperatingStateLog.UsageTodayHours != newOperatingStateLog.UsageTodayHours
                         || curOperatingStateLog.UsageTodayMinutes != newOperatingStateLog.UsageTodayMinutes
                         || curOperatingStateLog.UsageYesterdayHours != newOperatingStateLog.UsageYesterdayHours
                         || curOperatingStateLog.UsageYesterdayMinutes != newOperatingStateLog.UsageYesterdayMinutes)
                        {
                            isStateLogChanged = true;
                            changedStateLogs.Add((byte) newOperatingStateLog.OperatingState);
                            curOperatingStateLog.UsageTodayHours = newOperatingStateLog.UsageTodayHours;
                            curOperatingStateLog.UsageTodayMinutes = newOperatingStateLog.UsageTodayMinutes;
                            curOperatingStateLog.UsageYesterdayHours = newOperatingStateLog.UsageYesterdayHours;
                            curOperatingStateLog.UsageYesterdayMinutes = newOperatingStateLog.UsageYesterdayMinutes;
                        }
                    }
                    else
                    {
                        isStateLogChanged = true;
                        changedStateLogs.Add((byte) newOperatingStateLog.OperatingState);
                        isSupportedListChanged = true;
                        ThermostatOperatingStateSupportData.OperatingStateLogs.Add(newOperatingStateLog);
                    }
                }

                if (isStateChanged)
                {
                    ThermostatOperatingStateSupportData.OperatingState = newData.OperatingState;
                    if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    {
                        SendUnsolicitedReport(PrepareStateReport(), NodeTag.Empty);
                    }
                }
                if (isSupportedListChanged)
                {
                    if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        SendUnsolicitedReport(PrepareSupportedReport(), NodeTag.Empty);
                    }
                }
                if (isStateLogChanged)
                {
                    if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    {
                        COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_REPORT[] stateReports;
                        stateReports = CreateLoggingReports(changedStateLogs);
                        foreach (COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.THERMOSTAT_OPERATING_STATE_LOGGING_REPORT stateReport in stateReports)
                        {
                            // For a test of ReportsToFollow and Report Segmentation see Z-Wave area above.
                            SendUnsolicitedReport(stateReport, NodeTag.Empty);
                        }
                    }
                }
                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class OperatingStateLog : INestedItem
    {
        //[CustomDataParameter(2, "Log for Operating State:", CommandClassesImpl.UiControl.ListComboBox, CommandClassesImpl.InputType.Enum, EditType.Config /*| EditType.View*/ | EditType.Edit, typeof(ThermostatOperatingStateNames))]
        public ThermostatOperatingStateNames OperatingState { get; set; } = ThermostatOperatingStateNames.Idle;
        [CustomDataParameter(2, "Usage Today Hours", UiControl.TextBox, InputType.Int_0_24, EditType.Config /*| EditType.View*/ | EditType.Edit)]
        public byte UsageTodayHours { get; set; } = 0;
        [CustomDataParameter(2, "Usage Today Minutes", UiControl.TextBox, InputType.Int_0_59, EditType.Config /*| EditType.View*/ | EditType.Edit)]
        public byte UsageTodayMinutes { get; set; } = 0;
        [CustomDataParameter(2, "Usage Yesterday Hours", UiControl.TextBox, InputType.Int_0_24, EditType.Config /*| EditType.View*/ | EditType.Edit)]
        public byte UsageYesterdayHours { get; set; } = 0;
        [CustomDataParameter(2, "Usage Yesterday Minutes", UiControl.TextBox, InputType.Int_0_59, EditType.Config /*| EditType.View*/ | EditType.Edit)]
        public byte UsageYesterdayMinutes { get; set; } = 0;

        // only used for EDE UI. see interface for useage notes.
        [JsonIgnore]
        public int NestedId
        {
            get => (byte) OperatingState;
            set { }
        }

        public OperatingStateLog()
        {
        }

        public OperatingStateLog(byte state)
        {
            OperatingState = (ThermostatOperatingStateNames) state;
        }
        public static List<ThermostatOperatingStateNames> GetAllSupportedStates()
        {
            List<ThermostatOperatingStateNames> allSupportedStates = null;
            /*try
            {
                allSupportedStates = JsonUtils.Load<List<ThermostatOperatingStateNames>>(sensorTypesJsonFile);
            }
            catch*/
            {
                allSupportedStates = new List<ThermostatOperatingStateNames>();
                for (byte state = 0x00; state <= 0x0B; state++)
                {
                    allSupportedStates.Add((ThermostatOperatingStateNames) state);
                }
            }
            return allSupportedStates;
        }
    };

    public class ThermostatOperatingStateSupportData : CommandClassSupportData, INodeItem
    {
        [JsonIgnore]
        public IEnumerable<INestedItem> ChildItems => OperatingStateLogs;

        // V1+ Report
        // Operating State - valid values: V1: 0x00 to 0x06; V2: 0x00 to 0x0B; 0x00 = IDLE
        [ParameterSeparator(1, "Thermostat Operating State Report")]
        [XmlDataParameter(1, 0x42, 0x03, new byte[] { 0x00, 0x00 }, EditType.Config | EditType.Edit)]
        public ThermostatOperatingStateNames OperatingState { get; set; } = ThermostatOperatingStateNames.Idle;

        // V2+ Supported Report
        // LoggingSupportedBitmask: will be created at runtime from List<OperatingStateLog> OperatingStateLogs

        // V2+ Logging Report - 12 Operating States (including Idle) are defined (2022A)
        // Z-Wave / CTT: Add a least one Operating State to work properly
        [ParameterSeparator(2, "Thermostat Operating State Logging Report")]
        [NestedControl(2, "OperatingStateLogs")]
        public List<OperatingStateLog> OperatingStateLogs { get; set; } = new List<OperatingStateLog>();
    }

    public enum ThermostatOperatingStateNames : byte
    {
        [Description("0x00 Idle")]
        Idle = 0x00,                // V1+
        [Description("0x01 Heating")]
        Heating = 0x01,             // V1+
        [Description("0x02 Cooling")]
        Cooling = 0x02,             // V1+
        [Description("0x03 Fan only")]
        FanOnly = 0x03,             // V1+
        [Description("0x04 Pending heat")]
        PendingHeat = 0x04,         // V1+
        [Description("0x05 Pending cool")]
        PendingCool = 0x05,         // V1+
        [Description("0x06 Vent economizer")]
        VentEconomizer = 0x06,      // V1+
        [Description("0x07 Aux heating")]
        AuxHeating = 0x07,          // V2+
        [Description("0x08 Heating 2nd stage")]
        Heating2ndStage = 0x08,     // V2+
        [Description("0x09 Cooling 2nd stage")]
        Cooling2ndStage = 0x09,     // V2+
        [Description("0x0A Aux heating 2nd stage")]
        AuxHeat2ndStage = 0x0A,     // V2+
        [Description("0x0B Aux heating 3rd stage")]
        AuxHeat3rdStage = 0x0B,     // V2+
    }
}
