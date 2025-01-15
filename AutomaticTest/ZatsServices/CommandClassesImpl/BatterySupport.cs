// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Renci.SshNet;
using System;
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

// ToDo: BatteryLow Notification? Temporary implemented only if simulation is active.

namespace Zats.Services
{
    public class BatterySupport : CommandClassSupport
    {
        private BatterySupportData BatterySupportData { get; set; }

        private readonly string CommandClassName = "Battery CC";
        private string EndPointName = "";
        private readonly int LogLevel;  // shortcut for SupportData.LogLevel

        private readonly BackgroundWorker BatteryLevelTimer = new BackgroundWorker();
        private readonly string BatteryLevelTimerName = "Battery CC: BatteryLevel Simulation Timer";
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        public BatterySupport()
            : this(new BatterySupportData() { })
        { }

        public BatterySupport(BatterySupportData batterySupportData)
            : base(COMMAND_CLASS_BATTERY.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 3;
            BatterySupportData = batterySupportData;
            LogLevel = BatterySupportData.LogLevel;

            BatterySupportData.BatteryTemperatureScale = 0; // only 0 (Celsius) is defined
            if (BatterySupportData.BatteryTemperatureSize == 0)
            {
                BatterySupportData.BatteryTemperaturePrecision = 0;
            }

            if (BatterySupportData.SimulationBatteryLowReport)
            {
                BatteryLevelTimer.DoWork += new DoWorkEventHandler(BatteryLevelTimer_DoWork);
                BatteryLevelTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BatteryLevelTimer_RunWorkerCompleted);
                BatteryLevelTimer.WorkerSupportsCancellation = true;
                BatteryLevelTimer.WorkerReportsProgress = false;
                BatteryLevelTimer.RunWorkerAsync();
            }
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
                if (isResponseAllowed && command != null && command.Length > 1 && BatterySupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_BATTERY.BATTERY_GET.ID)
                    {
                        rpt = PrepareReport();
                    }
                    else if (command[1] == COMMAND_CLASS_BATTERY_V2.BATTERY_HEALTH_GET.ID)
                    {
                        rpt = PrepareHealthReport();
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
            if (Version == 1)
            {
                COMMAND_CLASS_BATTERY.BATTERY_REPORT rpt = new COMMAND_CLASS_BATTERY.BATTERY_REPORT() { };
                rpt.batteryLevel = BatterySupportData.BatteryLevel;
                if (BatterySupportData.BatteryLevel <= BatterySupportData.WarningBatteryLevel)
                {
                    rpt.batteryLevel = 0xFF;   // battery low warning V1
                }
                return rpt;
            }
            if (Version == 2)
            {
                COMMAND_CLASS_BATTERY_V2.BATTERY_REPORT rpt = new COMMAND_CLASS_BATTERY_V2.BATTERY_REPORT() { };
                rpt.batteryLevel = BatterySupportData.BatteryLevel;
                rpt.properties1.replaceRecharge = (byte) BatterySupportData.ReplaceRechargeStatus;
                rpt.properties1.lowFluid = (byte) (BatterySupportData.LowFluid ? 0x01 : 0x00);
                rpt.properties1.overheating = (byte) (BatterySupportData.Overheating ? 0x01 : 0x00);
                rpt.properties1.backupBattery = (byte) (BatterySupportData.IsBackupBattery ? 0x01 : 0x00);
                rpt.properties1.rechargeable = (byte) (BatterySupportData.IsRechargeable ? 0x01 : 0x00);
                rpt.properties1.chargingStatus = (byte) BatterySupportData.BatteryChargingStatus;
                rpt.properties2.disconnected = (byte) (BatterySupportData.Disconnected ? 0x01 : 0x00);
                // V2+: Battery Low notification SHOULD only be sent as an unsolicited report to the lifeline destinations
                //if (BatterySupportData.BatteryLevel <= BatterySupportData.WarningBatteryLevel)
                //    rpt.batteryLevel = 0xFF;   // battery low warning
                return rpt;
            }
            if (Version == 3)
            {
                COMMAND_CLASS_BATTERY_V3.BATTERY_REPORT rpt = new COMMAND_CLASS_BATTERY_V3.BATTERY_REPORT() { };
                rpt.batteryLevel = BatterySupportData.BatteryLevel;
                rpt.properties1.replaceRecharge = (byte) BatterySupportData.ReplaceRechargeStatus;
                rpt.properties1.lowFluid = (byte) (BatterySupportData.LowFluid ? 0x01 : 0x00);
                rpt.properties1.overheating = (byte) (BatterySupportData.Overheating ? 0x01 : 0x00);
                rpt.properties1.backupBattery = (byte) (BatterySupportData.IsBackupBattery ? 0x01 : 0x00);
                rpt.properties1.rechargeable = (byte) (BatterySupportData.IsRechargeable ? 0x01 : 0x00);
                rpt.properties1.chargingStatus = (byte) BatterySupportData.BatteryChargingStatus;
                rpt.properties2.disconnected = (byte) (BatterySupportData.Disconnected ? 0x01 : 0x00);
                rpt.properties2.lowTemperatureStatus = (byte) (BatterySupportData.LowTemperatureStatus ? 0x01 : 0x00);
                // V2+: Battery Low notification SHOULD only be sent as an unsolicited report to the lifeline destinations
                //if (BatterySupportData.BatteryLevel <= BatterySupportData.WarningBatteryLevel)
                //    rpt.batteryLevel = 0xFF;   // battery low warning
                return rpt;
            }
            return null;
        }

        private byte[] PrepareHealthReport()
        {
            if (Version >= 2)
            {
                COMMAND_CLASS_BATTERY_V2.BATTERY_HEALTH_REPORT rpt = new COMMAND_CLASS_BATTERY_V2.BATTERY_HEALTH_REPORT() { };
                rpt.maximumCapacity = BatterySupportData.MaximumCapacity;
                rpt.properties1.scale = BatterySupportData.BatteryTemperatureScale;
                rpt.properties1.size = BatterySupportData.BatteryTemperatureSize;
                rpt.properties1.precision = BatterySupportData.BatteryTemperaturePrecision;
                rpt.batteryTemperature = CommandClassSupportUtils.ValueToByteField(
                                            BatterySupportData.BatteryTemperature,
                                            BatterySupportData.BatteryTemperatureSize,
                                            BatterySupportData.BatteryTemperaturePrecision);
                return rpt;
            }
            return null;
        }

        // This Timer simulates a discharging and charging rechargeable battery or a discharging and replaced non-rechargeable battery
        private void BatteryLevelTimer_DoWork(object sender, DoWorkEventArgs e)
        {
            bool batteryLowSent = false;
            CommandClassSupportUtils.ReportTimerState(LogLevel, BatteryLevelTimerName, ReportTimerStateNames.Started, 0);
            while (true)    // infinite loop
            {
                if (BatterySupportData.BatteryLevel <= BatterySupportData.SimulationDischargingStepWidth)
                {
                    BatterySupportData.BatteryLevel = 1;
                }
                else
                {
                    BatterySupportData.BatteryLevel -= BatterySupportData.SimulationDischargingStepWidth;
                }
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Battery Level: {2} = 0x{2:X2}", CommandClassName, EndPointName, BatterySupportData.BatteryLevel);
                if (BatterySupportData.BatteryLevel <= BatterySupportData.WarningBatteryLevel && BatterySupportData.BatteryLevel > BatterySupportData.ReplaceBatteryLevel)
                {
                    BatterySupportData.ReplaceRechargeStatus = BatteryReplaceRechargeStatusNames.Soon;
                    byte[] rpt = PrepareReport();
                    rpt[2] = 0xFF;  // battery low warning
                    if (rpt != null && !batteryLowSent)
                    {
                        CommandClassSupportUtils.AddUnsolicitedFrame(rpt, 1, TxOptions, (byte) ReceivedAchData.SecurityScheme);
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Battery Low Report added to queue.", CommandClassName, EndPointName);
                        batteryLowSent = true;
                    }
                }
                if (BatterySupportData.BatteryLevel <= BatterySupportData.ReplaceBatteryLevel)
                    BatterySupportData.ReplaceRechargeStatus = BatteryReplaceRechargeStatusNames.Now;

                if (BatterySupportData.IsRechargeable)
                {
                    if (BatterySupportData.BatteryChargingStatus == BatteryChargingStatusNames.Discharging && BatterySupportData.BatteryLevel == BatterySupportData.ReplaceBatteryLevel)
                    {
                        BatterySupportData.BatteryChargingStatus = BatteryChargingStatusNames.Charging;
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Start of charging.", CommandClassName, EndPointName);
                    }
                    if (BatterySupportData.BatteryChargingStatus == BatteryChargingStatusNames.Charging && BatterySupportData.BatteryLevel < 100)
                    {
                        BatterySupportData.BatteryLevel += BatterySupportData.SimulationChargingStepWidth;
                    }
                    if (BatterySupportData.BatteryChargingStatus == BatteryChargingStatusNames.Charging && BatterySupportData.BatteryLevel > 100)
                    {
                        BatterySupportData.BatteryLevel = 100;
                        BatterySupportData.BatteryChargingStatus = BatteryChargingStatusNames.Discharging;
                        batteryLowSent = false;
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: End of charging.", CommandClassName, EndPointName);
                    }
                }
                else
                {
                    if (BatterySupportData.BatteryLevel == BatterySupportData.ReplaceBatteryLevel)
                    {
                        BatterySupportData.BatteryLevel = 100;
                        BatterySupportData.ReplaceRechargeStatus = BatteryReplaceRechargeStatusNames.Ok;
                        batteryLowSent = false;
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Battery replaced.", CommandClassName, EndPointName);
                    }
                }
                Thread.Sleep(1000);
            }
        }

        private void BatteryLevelTimer_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _resetEvent.Set();
            }
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, 
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            supervisionReport.status = (byte) SvStatusNames.NoSupport;
            return;
        }
    }

    public class BatterySupportData
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
        public bool SimulationBatteryLowReport { get; set; } = false;
        public byte SimulationChargingStepWidth { get; set; } = 10;
        public byte SimulationDischargingStepWidth { get; set; } = 1;
        public byte WarningBatteryLevel { get; set; } = 20;
        public byte ReplaceBatteryLevel { get; set; } = 10;
        // V1+
        public byte BatteryLevel { get; set; } = 100;
        // V2+
        // BatteryChargingStatus: 0x00 = Discharging, 0x01 = Charging, 0x02 = Maintaining
        public BatteryChargingStatusNames BatteryChargingStatus { get; set; } = BatteryChargingStatusNames.Discharging; 
        public bool IsRechargeable { get; set; } = false;
        public bool IsBackupBattery { get; set; } = false;
        public bool Overheating { get; set; } = false;
        public bool LowFluid { get; set; } = false;
        // ReplaceRechargeStatus: Bit0=0 + Bit1=0: ok; Bit0=1 + Bit1=0: replace soon; Bit0=1 + Bit1=1: replace now; Bit0=0 + Bit1=1: not allowed
        public BatteryReplaceRechargeStatusNames ReplaceRechargeStatus { get; set; } = BatteryReplaceRechargeStatusNames.Ok;
        public bool Disconnected { get; set; } = false;
        // V3+
        public bool LowTemperatureStatus { get; set; } = false;
        // Health Report V2+
        public byte MaximumCapacity { get; set; } = 0xFF;
        public double BatteryTemperature { get; set; } = 0.0;
        public byte BatteryTemperatureScale { get; set; } = 0;      // 0 = Celsius; 1..3 Reserved
        public byte BatteryTemperatureSize { get; set; } = 1;       // 0 = unknown; 1, 2, 4 = size
        public byte BatteryTemperaturePrecision { get; set; } = 0;  // 0..7
    }

    public enum BatteryChargingStatusNames : byte
    {
        Discharging = 0x00,
        Charging = 0x01,
        Maintaining = 0x02,
    }
    public enum BatteryReplaceRechargeStatusNames : byte
    {
        Ok = 0x00,
        Soon = 0x01,
        Now = 0x03,
    }
}
