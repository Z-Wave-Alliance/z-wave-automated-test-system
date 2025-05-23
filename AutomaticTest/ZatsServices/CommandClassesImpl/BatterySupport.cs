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
    [CommandClassName("Battery Command Class")]
    [CommandClassId(COMMAND_CLASS_BATTERY.ID)]
    [MinVersion(1)]
    [MaxVersion(3)]
    public class BatterySupport : CommandClassSupport
    {
        private BatterySupportData BatterySupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return BatterySupportData; }
            set { BatterySupportData = (BatterySupportData)value; }
        }

        private readonly BackgroundWorker BatteryLevelTimer = new BackgroundWorker();
        private string BatteryLevelTimerName = "";
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        private bool IsTimerStarted = false;   // This value is maintained by BatterySupport(). Do not change!

        public BatterySupport()
            : this(new BatterySupportData() { })
        { }

        public BatterySupport(BatterySupportData batterySupportData)
            : base(COMMAND_CLASS_BATTERY.ID)
        {
            BatterySupportData = batterySupportData;

            BatterySupportData.BatteryTemperatureScale = 0; // only 0 (Celsius) is defined
            if (BatterySupportData.BatteryTemperatureSize == 0)
            {
                BatterySupportData.BatteryTemperaturePrecision = 0;
            }

            if (BatterySupportData.SimulationBatteryLowReport)
            {
                if (!IsTimerStarted)
                {
                    BatteryLevelTimer.DoWork += new DoWorkEventHandler(BatteryLevelTimerDoWork);
                    BatteryLevelTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BatteryLevelTimerRunWorkerCompleted);
                    BatteryLevelTimer.WorkerSupportsCancellation = true;
                    BatteryLevelTimer.WorkerReportsProgress = false;
                    BatteryLevelTimerStart();
                    IsTimerStarted = true;
                }
            }
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

                if (isHandlingAllowed && command != null && command.Length > 1 && BatterySupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_BATTERY.BATTERY_GET.ID)
                    {
                        rpt = PrepareBatteryLevelReport(false);
                    }
                    else if (command[1] == COMMAND_CLASS_BATTERY_V2.BATTERY_HEALTH_GET.ID)
                    {
                        rpt = PrepareBatteryHealthReport();
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

        private byte[] PrepareBatteryLevelReport(bool addLowLevelWarning)
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
                if (addLowLevelWarning)
                    if (BatterySupportData.BatteryLevel <= BatterySupportData.WarningBatteryLevel)
                        rpt.batteryLevel = 0xFF;   // battery low warning
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
                if (addLowLevelWarning)
                    if (BatterySupportData.BatteryLevel <= BatterySupportData.WarningBatteryLevel)
                        rpt.batteryLevel = 0xFF;   // battery low warning
                return rpt;
            }
            return null;
        }

        private byte[] PrepareBatteryHealthReport()
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

        #region BatteryLevel Timer
        // The BatteryLevel Timer simulates a discharging and charging rechargeable battery or a discharging and replaced non-rechargeable battery.
        private void BatteryLevelTimerStart()
        {
            // BatterySupportData.CancelPendingRuntimeTimer = BatteryLevelTimerReset;
            BatteryLevelTimer.RunWorkerAsync();   // the BatteryLevel Timer runs infinitely, it cannot be cancelled
        }
        private void BatteryLevelTimerDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            bool batteryLowSent = false;
            CommandClassSupportUtils.ReportTimerState(BatterySupportData.LogLevel, BatteryLevelTimerName, ReportTimerStateNames.Started, 0);
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
                CommandClassSupportUtils.ReportAddLog(BatterySupportData.LogLevel, 
                            "{0} CC @ {1}: Battery Level: {2} = 0x{2:X2}", CommandClassShortName, EndPointName, BatterySupportData.BatteryLevel);
                if (BatterySupportData.BatteryLevel <= BatterySupportData.WarningBatteryLevel 
                 && BatterySupportData.BatteryLevel > BatterySupportData.ReplaceBatteryLevel)
                {
                    BatterySupportData.ReplaceRechargeStatus = BatteryReplaceRechargeStatusNames.Soon;
                    byte[] rpt = PrepareBatteryLevelReport(true);
                    //rpt[2] = 0xFF;  // battery low warning -> will now be set by 'true' parameter of PrepareBatteryLevelReport()
                    if (rpt != null && !batteryLowSent)
                    {
                        //CommandClassSupportUtils.AddUnsolicitedFrame(rpt, 1, TxOptions, (byte) ReceivedAchData.SecurityScheme);
                        DeviceRestart(NodeTag.Empty);
                        SendUnsolicitedReport(rpt, NodeTag.Empty);
                        SupportDelegates?.DeviceSleep(this, ReportWakeupTimeout);
                        CommandClassSupportUtils.ReportAddLog(BatterySupportData.LogLevel, 
                                    "{0} CC @ {1}: Battery Low Report added to queue.", CommandClassShortName, EndPointName);
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
                        CommandClassSupportUtils.ReportAddLog(BatterySupportData.LogLevel, "{0} CC @ {1}: Start of charging.", CommandClassShortName, EndPointName);
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
                        CommandClassSupportUtils.ReportAddLog(BatterySupportData.LogLevel, "{0} CC @ {1}: End of charging.", CommandClassShortName, EndPointName);
                    }
                }
                else
                {
                    if (BatterySupportData.BatteryLevel == BatterySupportData.ReplaceBatteryLevel)
                    {
                        BatterySupportData.BatteryLevel = 100;
                        BatterySupportData.ReplaceRechargeStatus = BatteryReplaceRechargeStatusNames.Ok;
                        batteryLowSent = false;
                        CommandClassSupportUtils.ReportAddLog(BatterySupportData.LogLevel, "{0} CC @ {1}: Battery replaced.", CommandClassShortName, EndPointName);
                    }
                }
                if (Result.State == ActionStates.Cancelled)
                {
                    BatteryLevelTimerReset();
                }
                Thread.Sleep(1000);
                if (bw.CancellationPending)
                {
                    CommandClassSupportUtils.ReportAddLog(BatterySupportData.LogLevel, "{0} CC @ {1}: BatteryLevelTimer is stopping.", CommandClassShortName, EndPointName);
                    e.Cancel = true;
                    break;
                }
            }
        }

        private void BatteryLevelTimerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _resetEvent.Set();
            }
        }
        private void BatteryLevelTimerReset() // the BatteryLevel Timer runs infinitely, it cannot be cancelled
        {
            if (BatteryLevelTimer.IsBusy)
            {
                CommandClassSupportUtils.ReportTimerState(BatterySupportData.LogLevel, BatteryLevelTimerName, ReportTimerStateNames.Reset, 0);
                BatteryLevelTimer.CancelAsync();
                _resetEvent.WaitOne();
            }
        }
        #endregion // BatteryLevel Timer

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, 
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            supervisionReport.status = (byte) SvStatusNames.NoSupport;
            return;
        }


        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is BatterySupportData newData)
            {
                BatteryLevelTimerName = string.Format("{0} CC @ {1}: BatteryLevel Simulation Timer", CommandClassShortName, EndPointName);

                if (BatterySupportData.SimulationBatteryLowReport != newData.SimulationBatteryLowReport
                 || BatterySupportData.SimulationChargingStepWidth != newData.SimulationChargingStepWidth
                 || BatterySupportData.SimulationDischargingStepWidth != newData.SimulationDischargingStepWidth
                 || BatterySupportData.WarningBatteryLevel != newData.WarningBatteryLevel
                 || BatterySupportData.ReplaceBatteryLevel != newData.ReplaceBatteryLevel)
                {
                    BatterySupportData.SimulationBatteryLowReport = newData.SimulationBatteryLowReport;
                    BatterySupportData.SimulationChargingStepWidth = newData.SimulationChargingStepWidth;
                    BatterySupportData.SimulationDischargingStepWidth = newData.SimulationDischargingStepWidth;
                    BatterySupportData.WarningBatteryLevel = newData.WarningBatteryLevel;
                    BatterySupportData.ReplaceBatteryLevel = newData.ReplaceBatteryLevel;
                    // Any report ??
                }
                if (BatterySupportData.BatteryLevel != newData.BatteryLevel
                 || BatterySupportData.ReplaceRechargeStatus != newData.ReplaceRechargeStatus
                 || BatterySupportData.LowFluid != newData.LowFluid
                 || BatterySupportData.Overheating != newData.Overheating
                 || BatterySupportData.IsBackupBattery != newData.IsBackupBattery
                 || BatterySupportData.IsRechargeable != newData.IsRechargeable
                 || BatterySupportData.BatteryChargingStatus != newData.BatteryChargingStatus
                 || BatterySupportData.Disconnected != newData.Disconnected
                 || BatterySupportData.LowTemperatureStatus != newData.LowTemperatureStatus)
                {
                    BatterySupportData.BatteryLevel = newData.BatteryLevel;
                    BatterySupportData.ReplaceRechargeStatus = newData.ReplaceRechargeStatus;
                    BatterySupportData.LowFluid = newData.LowFluid;
                    BatterySupportData.Overheating = newData.Overheating;
                    BatterySupportData.IsBackupBattery = newData.IsBackupBattery;
                    BatterySupportData.IsRechargeable = newData.IsRechargeable;
                    BatterySupportData.BatteryChargingStatus = newData.BatteryChargingStatus;
                    BatterySupportData.Disconnected = newData.Disconnected;
                    BatterySupportData.LowTemperatureStatus = newData.LowTemperatureStatus;
                    if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    { 
                        // V2+: Battery Low notification SHOULD only be sent as an unsolicited report to the lifeline destinations
                        SendUnsolicitedReport(PrepareBatteryLevelReport(true), NodeTag.Empty);
                    }
                }
                if (BatterySupportData.MaximumCapacity != newData.MaximumCapacity
                 || BatterySupportData.BatteryTemperature != newData.BatteryTemperature
                 || BatterySupportData.BatteryTemperatureScale != newData.BatteryTemperatureScale
                 || BatterySupportData.BatteryTemperatureSize != newData.BatteryTemperatureSize
                 || BatterySupportData.BatteryTemperaturePrecision != newData.BatteryTemperaturePrecision)
                {
                    BatterySupportData.MaximumCapacity = newData.MaximumCapacity;
                    BatterySupportData.BatteryTemperature = newData.BatteryTemperature;
                    BatterySupportData.BatteryTemperatureScale = newData.BatteryTemperatureScale;
                    BatterySupportData.BatteryTemperatureSize = newData.BatteryTemperatureSize;
                    BatterySupportData.BatteryTemperaturePrecision = newData.BatteryTemperaturePrecision;
                    if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    {
                        SendUnsolicitedReport(PrepareBatteryHealthReport(), NodeTag.Empty);
                    }
                }
                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class BatterySupportData : CommandClassSupportData
    {
        // Internals accessible from TestCases or EDE
        public bool SimulationBatteryLowReport { get; set; } = false;
        public byte SimulationChargingStepWidth { get; set; } = 10;
        public byte SimulationDischargingStepWidth { get; set; } = 1;
        public byte WarningBatteryLevel { get; set; } = 20;
        public byte ReplaceBatteryLevel { get; set; } = 10;

        // Battery Level Report V1+
        [ParameterSeparator(1, "Battery Level Report")]
        [XmlDataParameter(1, 0x80, 0x03, new byte[] { 0x00 }, EditType.Config | EditType.Edit)]
        public byte BatteryLevel { get; set; } = 100;

        // Battery Level Report V2+

        // ReplaceRechargeStatus: Bit0=0 + Bit1=0: ok; Bit0=1 + Bit1=0: replace soon; Bit0=1 + Bit1=1: replace now; Bit0=0 + Bit1=1: not allowed
        [XmlDataParameter(2, 0x80, 0x03, new byte[] { 0x01, 0x00 }, EditType.Config | EditType.Edit)]
        public BatteryReplaceRechargeStatusNames ReplaceRechargeStatus { get; set; } = BatteryReplaceRechargeStatusNames.Ok;

        [XmlDataParameter(2, 0x80, 0x03, new byte[] { 0x01, 0x01 }, EditType.Config | EditType.Edit)]
        public bool LowFluid { get; set; } = false;

        [XmlDataParameter(2, 0x80, 0x03, new byte[] { 0x01, 0x02 }, EditType.Config | EditType.Edit)]
        public bool Overheating { get; set; } = false;

        [XmlDataParameter(2, 0x80, 0x03, new byte[] { 0x01, 0x03 }, EditType.Config | EditType.Edit)]
        public bool IsBackupBattery { get; set; } = false;

        [XmlDataParameter(2, 0x80, 0x03, new byte[] { 0x01, 0x04 }, EditType.Config | EditType.Edit)]
        public bool IsRechargeable { get; set; } = false;

        // BatteryChargingStatus: 0x00 = Discharging; 0x01 = Charging; 0x02 = Maintaining
        [XmlDataParameter(2, 0x80, 0x03, new byte[] { 0x01, 0x05 }, EditType.Config | EditType.Edit)]
        public BatteryChargingStatusNames BatteryChargingStatus { get; set; } = BatteryChargingStatusNames.Discharging;

        [XmlDataParameter(2, 0x80, 0x03, new byte[] { 0x02, 0x00 }, EditType.Config | EditType.Edit)]
        public bool Disconnected { get; set; } = false;

        [XmlDataParameter(3, 0x80, 0x03, new byte[] { 0x02, 0x01 }, EditType.Config | EditType.Edit)]
        // Battery Level Report V3+
        public bool LowTemperatureStatus { get; set; } = false;

        // Battery Health Report V2+
        [ParameterSeparator(2, "Battery Health Report")]
        [XmlDataParameter(2, 0x80, 0x05, new byte[] { 0x00 }, EditType.Config | EditType.Edit)]
        public byte MaximumCapacity { get; set; } = 0xFF;

        [XmlDataParameter(2, 0x80, 0x05, new byte[] { 0x01, 0x00 }, EditType.Config | EditType.Edit)]
        public byte BatteryTemperatureSize { get; set; } = 1;       // 0 = unknown; 1, 2, 4 = size

        [XmlDataParameter(2, 0x80, 0x05, new byte[] { 0x01, 0x01 }, EditType.Config | EditType.Edit)]
        public byte BatteryTemperatureScale { get; set; } = 0;      // 0 = Celsius; 1..3 Reserved

        [XmlDataParameter(2, 0x80, 0x05, new byte[] { 0x01, 0x02 }, EditType.Config | EditType.Edit)]
        public byte BatteryTemperaturePrecision { get; set; } = 0;  // 0..7

        [CustomDataParameter(2, "Battery Temperature", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Double, EditType.Config | EditType.Edit)]
        public double BatteryTemperature { get; set; } = 0.0;
    }

    public enum BatteryChargingStatusNames : byte
    {
        [Description("0x00 Discharging")]
        Discharging = 0x00,
        [Description("0x01 Charging")]
        Charging = 0x01,
        [Description("0x02 Maintaining")]
        Maintaining = 0x02,
    }
    public enum BatteryReplaceRechargeStatusNames : byte
    {
        [Description("0x00 Ok")]
        Ok = 0x00,
        [Description("0x01 Soon")]
        Soon = 0x01,
        [Description("0x03 Now")]
        Now = 0x03,
    }
}
