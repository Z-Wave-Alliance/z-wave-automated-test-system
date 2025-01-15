// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.ComponentModel;
using System.Threading;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;

// ToDo: Support of WakeUpRequest bit of Supervision V2 command (not yet supported by SupervisionCallback method)
// ToDo: Callback from any received command to restart Minimum WakeUp Time (10 s)
// No CTT script for Wake Up V3 available

namespace Zats.Services
{
    public class WakeUpSupport : CommandClassSupport
    {
        private WakeUpSupportData WakeUpSupportData { get; set; }

        private readonly string CommandClassName = "Wake Up CC";
        private string EndPointName = "";
        private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private readonly BackgroundWorker WakeUpTimer = new BackgroundWorker();
        private string WakeUpTimerName = "";
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        private uint RuntimeElapsed = 0;
        private uint NextWakeUpTime = 0;
        private uint NextGoSleepTime = 0;

        public WakeUpSupport()
            : this(new WakeUpSupportData() { })
        { }

        public WakeUpSupport(WakeUpSupportData wakeUpSupportData)
            : base(COMMAND_CLASS_WAKE_UP.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 3;
            WakeUpSupportData = wakeUpSupportData;
            LogLevel = WakeUpSupportData.LogLevel;

            if (!WakeUpSupportData.IsTimerStarted)
            {
                WakeUpTimer.DoWork += new DoWorkEventHandler(WakeUpTimer_DoWork);
                WakeUpTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(WakeUpTimer_RunWorkerCompleted);
                WakeUpTimer.WorkerSupportsCancellation = true;
                WakeUpTimer.WorkerReportsProgress = false;
                WakeUpTimer.RunWorkerAsync();
                WakeUpSupportData.IsTimerStarted = true;
            }

            if (WakeUpSupportData.WakeUpInterval != 0 && WakeUpSupportData.SimulateWakeUp)
            {
                NextWakeUpTime = RuntimeElapsed + WakeUpSupportData.WakeUpInterval;
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
                WakeUpTimerName = string.Format("{0}{1}: WakeUp Timer", CommandClassName, EndPointName);
                ResultForSv = SvResultNames.Undefined;
                if (isResponseAllowed && command != null && command.Length > 1 && WakeUpSupportData != null)
                {
                    byte[] rpt = null;
                    // Simulate Sleep/Awake cycle for WakeUp CC commands only (requires SimulateWakeUp==true)
                    //if (NextGoSleepTime > 0) // is node awaken?
                    {
                        //NextGoSleepTime = RuntimeElapsed + 10 + 2;
                        //CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Node stays awake at {2} for ~{3} seconds until {4}",
                        //            CommandClassName, EndPointName, RuntimeElapsed, 10 + 2, NextGoSleepTime);
                        if (command[1] == COMMAND_CLASS_WAKE_UP_V2.WAKE_UP_INTERVAL_CAPABILITIES_GET.ID)
                        {
                            rpt = PrepareCapabilitiesReport();
                        }
                        else if (command[1] == COMMAND_CLASS_WAKE_UP.WAKE_UP_INTERVAL_GET.ID)
                        {
                            rpt = PrepareIntervalReport();
                        }
                        else if (command[1] == COMMAND_CLASS_WAKE_UP.WAKE_UP_INTERVAL_SET.ID)
                        {
                            ResultForSv = HandleWakeUpSet(command);
                        }
                        else if (command[1] == COMMAND_CLASS_WAKE_UP.WAKE_UP_NO_MORE_INFORMATION.ID)
                        {
                            if (NextGoSleepTime > 0) // is node awaken?
                            {
                                NextGoSleepTime = 0;
                                if (WakeUpSupportData.WakeUpInterval != 0)
                                {
                                    NextWakeUpTime = RuntimeElapsed + WakeUpSupportData.WakeUpInterval;
                                }
                                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: No More Info - Node sleeps from {2} for {3} seconds until {4}",
                                            CommandClassName, EndPointName, RuntimeElapsed, WakeUpSupportData.WakeUpInterval, NextWakeUpTime);
                            }
                        }
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

        private byte[] PrepareCapabilitiesReport()
        {
            if (Version == 2)
            {
                COMMAND_CLASS_WAKE_UP_V2.WAKE_UP_INTERVAL_CAPABILITIES_REPORT rpt = new COMMAND_CLASS_WAKE_UP_V2.WAKE_UP_INTERVAL_CAPABILITIES_REPORT() { };
                rpt.minimumWakeUpIntervalSeconds = CommandClassSupportUtils.UInt64ToByteField(WakeUpSupportData.MinimumWakeUpInterval, 3);
                rpt.maximumWakeUpIntervalSeconds = CommandClassSupportUtils.UInt64ToByteField(WakeUpSupportData.MaximumWakeUpInterval, 3);
                rpt.defaultWakeUpIntervalSeconds = CommandClassSupportUtils.UInt64ToByteField(WakeUpSupportData.DefaultWakeUpInterval, 3);
                rpt.wakeUpIntervalStepSeconds = CommandClassSupportUtils.UInt64ToByteField(WakeUpSupportData.WakeUpIntervalStep, 3);
                return rpt;
            }
            if (Version >= 3)
            {
                COMMAND_CLASS_WAKE_UP_V3.WAKE_UP_INTERVAL_CAPABILITIES_REPORT rpt = new COMMAND_CLASS_WAKE_UP_V3.WAKE_UP_INTERVAL_CAPABILITIES_REPORT() { };
                rpt.minimumWakeUpIntervalSeconds = CommandClassSupportUtils.UInt64ToByteField(WakeUpSupportData.MinimumWakeUpInterval, 3);
                rpt.maximumWakeUpIntervalSeconds = CommandClassSupportUtils.UInt64ToByteField(WakeUpSupportData.MaximumWakeUpInterval, 3);
                rpt.defaultWakeUpIntervalSeconds = CommandClassSupportUtils.UInt64ToByteField(WakeUpSupportData.DefaultWakeUpInterval, 3);
                rpt.wakeUpIntervalStepSeconds = CommandClassSupportUtils.UInt64ToByteField(WakeUpSupportData.WakeUpIntervalStep, 3);
                rpt.properties1.wakeUpOnDemand = (byte)(WakeUpSupportData.WakeUpOnDemandSupport ? 0x01 : 0x00);
                return rpt;
            }
            return null;
        }
        private byte[] PrepareIntervalReport()
        {
            COMMAND_CLASS_WAKE_UP.WAKE_UP_INTERVAL_REPORT rpt = new COMMAND_CLASS_WAKE_UP.WAKE_UP_INTERVAL_REPORT()
            {
                nodeid = WakeUpSupportData.WakeUpDestinationNodeId,
                seconds = CommandClassSupportUtils.UInt64ToByteField(WakeUpSupportData.WakeUpInterval, 3)
            };
            return rpt;
        }

        private void WakeUpTimer_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            CommandClassSupportUtils.ReportTimerState(LogLevel, WakeUpTimerName, ReportTimerStateNames.Started, RuntimeElapsed);
            while (true)
            {
                RuntimeElapsed++;
                if (NextWakeUpTime > 0 && NextWakeUpTime <= RuntimeElapsed)
                {
                    NextWakeUpTime = 0;
                    NextGoSleepTime = RuntimeElapsed + 10;
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Node awakened at {2} for {3} seconds until {4}",
                                CommandClassName, EndPointName, RuntimeElapsed, 10, NextGoSleepTime);

                    if (WakeUpSupportData.SendWakeUpNotification)
                    { 
                        COMMAND_CLASS_WAKE_UP_V2.WAKE_UP_NOTIFICATION rpt = new COMMAND_CLASS_WAKE_UP_V2.WAKE_UP_NOTIFICATION() { };
                        CommandClassSupportUtils.AddUnsolicitedFrame(rpt, new byte[] { WakeUpSupportData.WakeUpDestinationNodeId }, TxOptions, (byte) Network.GetHighestSecurityScheme()/*ReceivedAchData.SecurityScheme*/);
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Wake Up Notification added to queue", CommandClassName, EndPointName);
                    }
                }
                if (NextGoSleepTime > 0 && NextGoSleepTime <= RuntimeElapsed)
                {
                    NextGoSleepTime = 0;
                    if (WakeUpSupportData.WakeUpInterval != 0)
                    { 
                        NextWakeUpTime = RuntimeElapsed + WakeUpSupportData.WakeUpInterval;
                    }
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Node sleeps from {2} for {3} seconds until {4}", 
                                CommandClassName, EndPointName, RuntimeElapsed, WakeUpSupportData.WakeUpInterval, NextWakeUpTime);
                }
                Thread.Sleep(1000);
                if (bw.CancellationPending)
                {
                    CommandClassSupportUtils.ReportTimerState(LogLevel, WakeUpTimerName, ReportTimerStateNames.CancelledElapsedSec, RuntimeElapsed);
                    e.Cancel = true;
                    break;
                }
            }
            if (!e.Cancel)
            {
                CommandClassSupportUtils.ReportTimerState(LogLevel, WakeUpTimerName, ReportTimerStateNames.ExpiredSec, RuntimeElapsed);
            }
        }

        private void WakeUpTimer_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _resetEvent.Set();
            }
        }
        private void WakeUpTimer_Reset()
        {
            if (WakeUpTimer.IsBusy)
            {
                WakeUpTimer.CancelAsync();
                _resetEvent.WaitOne();
                CommandClassSupportUtils.ReportTimerState(LogLevel, WakeUpTimerName, ReportTimerStateNames.Reset, 0);
            }
        }

        private SvResultNames HandleWakeUpSet(byte[] command)
        {
            COMMAND_CLASS_WAKE_UP.WAKE_UP_INTERVAL_SET wakeUpIntervalSetCmd = command;
            uint wakeUpInterval;

            WakeUpSupportData.WakeUpDestinationNodeId = wakeUpIntervalSetCmd.nodeid;

            wakeUpInterval = Convert.ToUInt32(CommandClassSupportUtils.ByteFieldToUInt64(wakeUpIntervalSetCmd.seconds, 3));
            if (Version == 1)
            {
                WakeUpSupportData.WakeUpInterval = wakeUpInterval;
            }
            if (Version >= 2)
            {
                if ( (wakeUpInterval == 0)
                || (wakeUpInterval >= WakeUpSupportData.MinimumWakeUpInterval && wakeUpInterval <= WakeUpSupportData.MaximumWakeUpInterval) )
                {
                    WakeUpSupportData.WakeUpInterval = wakeUpInterval;
                }
            }

            NextWakeUpTime = 0;
            if (WakeUpSupportData.WakeUpInterval != 0 && WakeUpSupportData.SimulateWakeUp)
            {
                NextWakeUpTime = RuntimeElapsed + WakeUpSupportData.WakeUpInterval;
            }

            return (WakeUpSupportData.WakeUpInterval == wakeUpInterval) ? SvResultNames.Successful : SvResultNames.Failed;
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
                case COMMAND_CLASS_WAKE_UP.WAKE_UP_INTERVAL_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }
    }

    public class WakeUpSupportData
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
        public bool SimulateWakeUp { get; set; } = false;           // Wake Up Timer simulation (log only): false = off, true = on 
        public bool SendWakeUpNotification { get; set; } = false;   // True: requires SimulateWakeUp==true to send the notification
        public bool IsTimerStarted { get; set; } = false;           // This value is maintained by WakeUpSupport(). Do not change!
        // V1+
        public uint WakeUpInterval { get; set; } = 60;
        public byte WakeUpDestinationNodeId { get; set; } = 0;      // Initialize with unknown destination
        // V2+
        public uint MinimumWakeUpInterval { get; set; } = 5;
        public uint MaximumWakeUpInterval { get; set; } = 100000;
        public uint DefaultWakeUpInterval { get; set; } = 60;
        public uint WakeUpIntervalStep { get; set; } = 1;
        // V3+
        public bool WakeUpOnDemandSupport { get; set; } = false;
    }
}
