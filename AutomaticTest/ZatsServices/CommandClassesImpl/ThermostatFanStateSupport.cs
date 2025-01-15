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
    public class ThermostatFanStateSupport : CommandClassSupport
    {
        private ThermostatFanStateSupportData ThermostatFanStateSupportData { get; set; }

        private readonly string CommandClassName = "Thermostat Fan State CC";
        private string EndPointName = "";
        private readonly int LogLevel;  // shortcut for SupportData.LogLevel

        public ThermostatFanStateSupport()
            : this(new ThermostatFanStateSupportData() { })
        { }

        public ThermostatFanStateSupport(ThermostatFanStateSupportData thermostatFanStateSupportData)
            : base(COMMAND_CLASS_THERMOSTAT_FAN_STATE.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 2;
            ThermostatFanStateSupportData = thermostatFanStateSupportData;
            LogLevel = ThermostatFanStateSupportData.LogLevel;
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
                if (isResponseAllowed && command != null && command.Length > 1 && ThermostatFanStateSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_THERMOSTAT_FAN_STATE.THERMOSTAT_FAN_STATE_GET.ID)
                    {
                        rpt = PrepareReport();
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
            if (Version >= 1 && Version <= 2)
            {
                COMMAND_CLASS_THERMOSTAT_FAN_STATE.THERMOSTAT_FAN_STATE_REPORT rpt = new COMMAND_CLASS_THERMOSTAT_FAN_STATE.THERMOSTAT_FAN_STATE_REPORT() { };
                rpt.properties1.fanOperatingState = (byte) ThermostatFanStateSupportData.CurrentFanState;
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Fan State is {2} = '{3}'.",
                            CommandClassName, EndPointName, (byte) ThermostatFanStateSupportData.CurrentFanState, ThermostatFanStateSupportData.CurrentFanState);
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

    }

    public class ThermostatFanStateSupportData
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
        // V1+ Report
        // CurrentFanState: Allowed states from 0 to: 2=RunningHigh (V1); 8=QuietCirculationMode (V2)
        public ThermostatFanStateNames CurrentFanState { get; set; } = ThermostatFanStateNames.Off;
    }

    public enum ThermostatFanStateNames : byte
    {
        Off = 0x00,
        Idle = 0x00,
        Running = 0x01,
        RunningLow = 0x01,
        RunningHigh = 0x02,
        RunningMedium = 0x03,
        CirculationMode = 0x04,
        HumidityCirculationMode = 0x05,
        RightLeftCirculationMode = 0x06,
        UpDownCirculationMode = 0x07,
        QuietCirculationMode = 0x08,
    }
}
