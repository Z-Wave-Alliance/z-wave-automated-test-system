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
    [CommandClassName("Thermostat Fan State Command Class")]
    [CommandClassId(COMMAND_CLASS_THERMOSTAT_FAN_STATE.ID)]
    [MinVersion(1)]
    [MaxVersion(2)]
    public class ThermostatFanStateSupport : CommandClassSupport
    {
        private ThermostatFanStateSupportData ThermostatFanStateSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return ThermostatFanStateSupportData; }
            set { ThermostatFanStateSupportData = (ThermostatFanStateSupportData)value; }
        }

        public ThermostatFanStateSupport()
            : this(new ThermostatFanStateSupportData() { })
        { }

        public ThermostatFanStateSupport(ThermostatFanStateSupportData thermostatFanStateSupportData)
            : base(COMMAND_CLASS_THERMOSTAT_FAN_STATE.ID)
        {
            ThermostatFanStateSupportData = thermostatFanStateSupportData;
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

                if (isHandlingAllowed && command != null && command.Length > 1 && ThermostatFanStateSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_THERMOSTAT_FAN_STATE.THERMOSTAT_FAN_STATE_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareFanStateReport();
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

        private byte[] PrepareFanStateReport()
        {
            if (Version >= 1 && Version <= 2)
            {
                COMMAND_CLASS_THERMOSTAT_FAN_STATE.THERMOSTAT_FAN_STATE_REPORT rpt = new COMMAND_CLASS_THERMOSTAT_FAN_STATE.THERMOSTAT_FAN_STATE_REPORT() { };
                rpt.properties1.fanOperatingState = (byte) ThermostatFanStateSupportData.CurrentFanState;
                CommandClassSupportUtils.ReportAddLog(ThermostatFanStateSupportData.LogLevel, 
                            "{0} CC @ {1}: Fan State is {2} = '{3}'.",
                            CommandClassShortName, EndPointName, 
                            (byte) ThermostatFanStateSupportData.CurrentFanState, ThermostatFanStateSupportData.CurrentFanState);
                return rpt;
            }
            return null;
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, 
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            supervisionReport.status = (byte) SvStatusNames.NoSupport;
            return;
        }


        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is ThermostatFanStateSupportData newData)
            {
                if (ThermostatFanStateSupportData.CurrentFanState != newData.CurrentFanState)
                {
                    ThermostatFanStateSupportData.CurrentFanState = newData.CurrentFanState;
                    if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    {
                        SendUnsolicitedReport(PrepareFanStateReport(), NodeTag.Empty);
                    }
                }
                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class ThermostatFanStateSupportData : CommandClassSupportData
    {
        // Internals accessible from TestCases or EDE
        // V1+ Report
        // CurrentFanState: Allowed states: 0..2=RunningHigh (V1); 0..8=QuietCirculationMode (V2)
        [ParameterSeparator(1, "Thermostat Fan State Report")]
        [XmlDataParameter(1, 0x45, 0x03, new byte[] { 0x00, 0x00 }, EditType.Config | EditType.Edit)]
        public ThermostatFanStateNames CurrentFanState { get; set; } = ThermostatFanStateNames.Off;
    }

    public enum ThermostatFanStateNames : byte
    {
        [Description("0x00 Off")]
        Off = 0x00,                         // V1+
        [Description("0x00 Idle")]
        Idle = 0x00,                        // V1+
        [Description("0x01 Running")]
        Running = 0x01,                     // V1+
        [Description("0x01 Running low")]
        RunningLow = 0x01,                  // V1+
        [Description("0x02 Running high")]
        RunningHigh = 0x02,                 // V1+
        [Description("0x03 Running medium")]
        RunningMedium = 0x03,               // V2+
        [Description("0x04 Circulation mode")]
        CirculationMode = 0x04,             // V2+
        [Description("0x05 Humidity circulation mode")]
        HumidityCirculationMode = 0x05,     // V2+
        [Description("0x06 Right left circulation mode")]
        RightLeftCirculationMode = 0x06,    // V2+
        [Description("0x07 Up down circulation mode")]
        UpDownCirculationMode = 0x07,       // V2+
        [Description("0x08 Quiet circulation mode")]
        QuietCirculationMode = 0x08,        // V2+
    }
}
