using System;
using System.ComponentModel;
using System.Threading;
using Zats.Services.CommandClassesImpl;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.Devices;
using static Zats.Services.AssociationGroupEx;

// ToDo: Support of WakeUpRequest bit of Supervision V2 command (not yet supported by SupervisionCallback method)
// ToDo: Callback from any received command to restart Minimum WakeUp Time (10 s)
// No CTT script for Wake Up V3 available

namespace Zats.Services
{
    [CommandClassName("Wake Up Command Class")]
    [CommandClassId(COMMAND_CLASS_WAKE_UP.ID)]
    [MinVersion(1)]
    [MaxVersion(2)]
    public class WakeUpSupport : CommandClassSupport
    {
        private WakeUpSupportData WakeUpSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return WakeUpSupportData; }
            set
            {
                WakeUpSupportData = (WakeUpSupportData)value;

                if (WakeUpSupportData.WakeUpInterval != 0 && WakeUpSupportData.SimulateWakeUp)
                {
                    NextWakeUpTime = RuntimeElapsed + WakeUpSupportData.WakeUpInterval;
                }
            }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private readonly BackgroundWorker RuntimeTimer = new BackgroundWorker();
        private string RuntimeTimerName = "";
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        private uint RuntimeElapsed = 0;
        private bool IsTimerStarted = false;   // This value is maintained by WakeUpSupport(). Do not change!
        private uint NextWakeUpTime = 0;
        private uint NextGoSleepTime = 0;

        public WakeUpSupport()
            : this(new WakeUpSupportData() { })
        { }

        public WakeUpSupport(WakeUpSupportData wakeUpSupportData)
            : base(COMMAND_CLASS_WAKE_UP.ID)
        {
            WakeUpSupportData = wakeUpSupportData;
        }

        public override void StartEmulation()
        {
            if (WakeUpSupportData.WakeUpInterval != 0 && WakeUpSupportData.SimulateWakeUp)
            {
                NextWakeUpTime = RuntimeElapsed + WakeUpSupportData.WakeUpInterval;
            }

            if (!IsTimerStarted)
            {
                RuntimeTimer.DoWork += new DoWorkEventHandler(RuntimeTimerDoWork);
                RuntimeTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(RuntimeTimerRunWorkerCompleted);
                RuntimeTimer.WorkerSupportsCancellation = true;
                RuntimeTimer.WorkerReportsProgress = false;
                RuntimeTimerStart();
                IsTimerStarted = true;
            }
        }

        public override void ResetEmulation()
        {
            WakeUpSupportData.WakeUpInterval = WakeUpSupportData.DefaultWakeUpInterval;
            WakeUpSupportData.WakeUpDestinationNodeId = 0;
            RuntimeTimerReset();
            SupportDelegates?.DeviceDataUpdate?.Invoke(this);
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

                RuntimeTimerName = string.Format("{0} CC @ {1}: Runtime Timer", CommandClassShortName, EndPointName);
                ResultForSv = SvResultNames.Undefined;
                if (isHandlingAllowed && command != null && command.Length > 1 && WakeUpSupportData != null)
                {
                    byte[] rpt = null;
                    // Simulate Sleep/Awake cycle for WakeUp CC commands only (requires SimulateWakeUp==true)
                    //if (NextGoSleepTime > 0) // is node awaken?
                    {
                        //NextGoSleepTime = RuntimeElapsed + 10 + 2;
                        //CommandClassSupportUtils.ReportAddLog(LogLevel, "{0} CC @ {1}: Node stays awake at {2} for ~{3} seconds until {4}",
                        //            CommandClassShortName, EndPointName, RuntimeElapsed, 10 + 2, NextGoSleepTime);
                        if (command[1] == COMMAND_CLASS_WAKE_UP_V2.WAKE_UP_INTERVAL_CAPABILITIES_GET.ID)
                        {
                            rpt = PrepareCapabilitiesReport();
                        }
                        else if (command[1] == COMMAND_CLASS_WAKE_UP.WAKE_UP_INTERVAL_GET.ID)
                        {
                            rpt = PrepareWakeUpIntervalReport();
                        }
                        else if (command[1] == COMMAND_CLASS_WAKE_UP.WAKE_UP_INTERVAL_SET.ID)
                        {
                            ResultForSv = HandleWakeUpSetCmd(command, sendSrcNode);
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
                                CommandClassSupportUtils.ReportAddLog(WakeUpSupportData.LogLevel, 
                                            "{0} CC @ {1}: No More Info - Node sleeps from {2} for {3} seconds until {4}",
                                            CommandClassShortName, EndPointName, 
                                            RuntimeElapsed, WakeUpSupportData.WakeUpInterval, NextWakeUpTime);
                            }
                            SupportDelegates?.DeviceSleep(this, 30);
                        }
                    }

                    if (rpt != null && IsResponseAllowed())
                    {
                        ApiOperation sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command, sendDstNode.EndPointId);
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
        private byte[] PrepareWakeUpIntervalReport()
        {
            COMMAND_CLASS_WAKE_UP.WAKE_UP_INTERVAL_REPORT rpt = new COMMAND_CLASS_WAKE_UP.WAKE_UP_INTERVAL_REPORT()
            {
                nodeid = WakeUpSupportData.WakeUpDestinationNodeId,
                seconds = CommandClassSupportUtils.UInt64ToByteField(WakeUpSupportData.WakeUpInterval, 3)
            };
            return rpt;
        }

        #region Runtime Timer
        private void RuntimeTimerStart()
        {
            // WakeUpSupportData.CancelPendingRuntimeTimer = RuntimeTimerReset;
            RuntimeTimer.RunWorkerAsync();   // the Runtime Timer runs infinitely or until Result.State == ActionStates.Cancelled
        }
        private void RuntimeTimerDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            CommandClassSupportUtils.ReportTimerState(WakeUpSupportData.LogLevel, RuntimeTimerName, ReportTimerStateNames.Started, RuntimeElapsed);
            while (true)
            {
                RuntimeElapsed++;
                if (NextWakeUpTime > 0 && NextWakeUpTime <= RuntimeElapsed)
                {
                    NextWakeUpTime = 0;
                    NextGoSleepTime = RuntimeElapsed + 10;
                    CommandClassSupportUtils.ReportAddLog(WakeUpSupportData.LogLevel, 
                                "{0} CC @ {1}: Node awakened at {2} for {3} seconds until {4}",
                                CommandClassShortName, EndPointName, RuntimeElapsed, 10, NextGoSleepTime);

                    if (WakeUpSupportData.SimulateWakeUp)
                    {
                        COMMAND_CLASS_WAKE_UP_V2.WAKE_UP_NOTIFICATION rpt = new COMMAND_CLASS_WAKE_UP_V2.WAKE_UP_NOTIFICATION() { };
                        DeviceRestart(NodeTag.Empty);
                        SendUnsolicitedReport(rpt, new NodeIdEx(WakeUpSupportData.WakeUpDestinationNodeId, 0, false), default(NodeTag));
                        SupportDelegates?.DeviceSleep(this, ReportWakeupTimeout);
                        CommandClassSupportUtils.ReportAddLog(WakeUpSupportData.LogLevel, 
                                    "{0} CC @ {1}: Wake Up Notification added to queue", CommandClassShortName, EndPointName);
                    }
                    //SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
                }
                if (NextGoSleepTime > 0 && NextGoSleepTime <= RuntimeElapsed)
                {
                    NextGoSleepTime = 0;
                    if (WakeUpSupportData.WakeUpInterval != 0)
                    { 
                        NextWakeUpTime = RuntimeElapsed + WakeUpSupportData.WakeUpInterval;
                    }
                    CommandClassSupportUtils.ReportAddLog(WakeUpSupportData.LogLevel, 
                                "{0} CC @ {1}: Node sleeps from {2} for {3} seconds until {4}", 
                                CommandClassShortName, EndPointName, 
                                RuntimeElapsed, WakeUpSupportData.WakeUpInterval, NextWakeUpTime);
                    //SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
                }
                if (Result.State == ActionStates.Cancelled)
                {
                    RuntimeTimerReset();
                }
                Thread.Sleep(1000);
                if (bw.CancellationPending)
                {
                    CommandClassSupportUtils.ReportTimerState(WakeUpSupportData.LogLevel, RuntimeTimerName, ReportTimerStateNames.CancelledElapsedSec, RuntimeElapsed);
                    e.Cancel = true;
                    break;
                }
            }
            if (!e.Cancel)
            {
                CommandClassSupportUtils.ReportTimerState(WakeUpSupportData.LogLevel, RuntimeTimerName, ReportTimerStateNames.ExpiredSec, RuntimeElapsed);
            }
        }

        private void RuntimeTimerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _resetEvent.Set();
            }
        }
        private void RuntimeTimerReset() // the Runtime Timer runs infinitely or until Result.State == ActionStates.Cancelled
        {
            if (RuntimeTimer.IsBusy)
            {
                CommandClassSupportUtils.ReportTimerState(WakeUpSupportData.LogLevel, RuntimeTimerName, ReportTimerStateNames.Reset, 0);
                RuntimeTimer.CancelAsync();
                _resetEvent.WaitOne();
                IsTimerStarted = false;
            }
        }
        #endregion // Runtime Timer

        private SvResultNames HandleWakeUpSet(byte wakeUpDestinationNodeId, uint wakeUpIntervalSec, NodeTag srcNode)
        {
            WakeUpSupportData.WakeUpDestinationNodeId = wakeUpDestinationNodeId;

            if (Version == 1)
            {
                WakeUpSupportData.WakeUpInterval = wakeUpIntervalSec;
            }
            if (Version >= 2)
            {
                if ( (wakeUpIntervalSec == 0)
                || (wakeUpIntervalSec >= WakeUpSupportData.MinimumWakeUpInterval && wakeUpIntervalSec <= WakeUpSupportData.MaximumWakeUpInterval) )
                {
                    WakeUpSupportData.WakeUpInterval = wakeUpIntervalSec;
                }
                else
                {
                    SupportDelegates?.LogMessage?.Invoke(LogLocation.Frontend, LogLevel.Error, "New Wake Up Interval has not been set (not between minimum and maximum interval)!");
                }
            }

            NextWakeUpTime = 0;
            if (WakeUpSupportData.WakeUpInterval != 0 && WakeUpSupportData.SimulateWakeUp)
            {
                NextWakeUpTime = RuntimeElapsed + WakeUpSupportData.WakeUpInterval;
            }

            if (WakeUpSupportData.WakeUpInterval == wakeUpIntervalSec)
            {
                if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                {
                    SendUnsolicitedReport(PrepareWakeUpIntervalReport(), srcNode);
                }
                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
                return SvResultNames.Successful;
            }
            return SvResultNames.Failed;
        }
        private SvResultNames HandleWakeUpSetCmd(byte[] command, NodeTag srcNode)
        {
            COMMAND_CLASS_WAKE_UP.WAKE_UP_INTERVAL_SET wakeUpIntervalSetCmd = command;
            uint wakeUpIntervalSec = Convert.ToUInt32(CommandClassSupportUtils.ByteFieldToUInt64(wakeUpIntervalSetCmd.seconds, 3));
            return HandleWakeUpSet(wakeUpIntervalSetCmd.nodeid, wakeUpIntervalSec, srcNode);
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
                case COMMAND_CLASS_WAKE_UP.WAKE_UP_INTERVAL_SET.ID:
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
            if (data is WakeUpSupportData newData)
            {
                if (newData.TriggerWakeUpNotification == true)  // UI-Button 'Wake Up', ignore all other data newData members
                {
                    COMMAND_CLASS_WAKE_UP_V2.WAKE_UP_NOTIFICATION rpt = new COMMAND_CLASS_WAKE_UP_V2.WAKE_UP_NOTIFICATION() { };
                    CommandClassSupportUtils.ReportAddLog(WakeUpSupportData.LogLevel,
                                "{0} CC @ {1}: Wake Up Notification to NodeId {2} (triggered) prepared",
                                CommandClassShortName, EndPointName, WakeUpSupportData.WakeUpDestinationNodeId);
                    SendUnsolicitedReport(rpt, new NodeIdEx(WakeUpSupportData.WakeUpDestinationNodeId, 0, false), default(NodeTag));
                    CommandClassSupportUtils.ReportAddLog(WakeUpSupportData.LogLevel,
                                "{0} CC @ {1}: Wake Up Notification to NodeId {2} (triggered) sent",
                                CommandClassShortName, EndPointName, WakeUpSupportData.WakeUpDestinationNodeId);
                    //CommandClassSupportUtils.AddUnsolicitedFrame(rpt, new byte[] { WakeUpSupportData.WakeUpDestinationNodeId }, TxOptions, (byte)Network.GetHighestSecurityScheme()/*ReceivedAchData.SecurityScheme*/);
                    //CommandClassSupportUtils.ReportAddLog(WakeUpSupportData.LogLevel,
                    //            "{0} CC @ {1}: Wake Up Notification to NodeId {2} added to queue",
                    //            CommandClassShortName, EndPointName, WakeUpSupportData.WakeUpDestinationNodeId);
                }
                else
                {
                    RuntimeTimerName = string.Format("{0} CC @ {1}: Runtime Timer", CommandClassShortName, EndPointName);

                    //if (WakeUpSupportData.SimulateWakeUp != newData.SimulateWakeUp 
                    // || WakeUpSupportData.SendWakeUpNotification != newData.SendWakeUpNotification)
                    {
                        WakeUpSupportData.SimulateWakeUp = newData.SimulateWakeUp;
                    }

                    if (WakeUpSupportData.WakeUpDestinationNodeId != newData.WakeUpDestinationNodeId
                     || WakeUpSupportData.WakeUpInterval != newData.WakeUpInterval)
                    {
                        HandleWakeUpSet(newData.WakeUpDestinationNodeId, newData.WakeUpInterval, NodeTag.Empty);
                    }

                    if (WakeUpSupportData.MinimumWakeUpInterval != newData.MinimumWakeUpInterval
                     || WakeUpSupportData.MaximumWakeUpInterval != newData.MaximumWakeUpInterval
                     || WakeUpSupportData.DefaultWakeUpInterval != newData.DefaultWakeUpInterval
                     || WakeUpSupportData.WakeUpIntervalStep != newData.WakeUpIntervalStep
                     || WakeUpSupportData.WakeUpOnDemandSupport != newData.WakeUpOnDemandSupport)
                    {
                        WakeUpSupportData.MinimumWakeUpInterval = newData.MinimumWakeUpInterval;
                        WakeUpSupportData.MaximumWakeUpInterval = newData.MaximumWakeUpInterval;
                        WakeUpSupportData.DefaultWakeUpInterval = newData.DefaultWakeUpInterval;
                        WakeUpSupportData.WakeUpIntervalStep = newData.WakeUpIntervalStep;
                        WakeUpSupportData.WakeUpOnDemandSupport = newData.WakeUpOnDemandSupport;
                        if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                        {
                            SendUnsolicitedReport(PrepareCapabilitiesReport(), NodeTag.Empty);
                        }
                    }
                }
                // safety mechanism: make sure that trigger flag is still false
                WakeUpSupportData.TriggerWakeUpNotification = false;

                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class WakeUpSupportData : CommandClassSupportData
    {
        // Internals accessible from TestCases or EDE
        [CustomDataParameter(1, "Simulate Wake Up", UiControl.CheckBox, InputType.Boolean, EditType.Config)]
        public bool SimulateWakeUp { get; set; } = true;                // Wake Up Timer simulation (log only): false = off, true = on 

        [CustomDataParameterEventTrigger]
        [ParameterSeparator(1, "Wake Up Notification")]
        [CustomDataParameter(1, "Trigger Wake Up Notification", UiControl.Button, InputType.Boolean, EditType.View | EditType.Edit)]
        public bool TriggerWakeUpNotification { get; set; } = false;    // Set this to true to trigger one manual WakeUpNotification (EDE only)

        [ParameterSeparator(1, "Wake Up Interval Report")]
        // Wake Up Interval Report V1+
        [XmlDataParameter(1, 0x84, 0x06, new byte[] { 0x00 }, EditType.Config | EditType.Edit)]
        public uint WakeUpInterval { get; set; } = 60;

        [XmlDataParameter(1, 0x84, 0x06, new byte[] { 0x01 }, EditType.Config | EditType.Edit)]
        public byte WakeUpDestinationNodeId { get; set; } = 0;          // Initialize with unknown destination
        // Wake Up Capabilities Report V2+

        [ParameterSeparator(2, "Wake Up Interval Capabilities Report")]
        [XmlDataParameter(2, 0x84, 0x0A, new byte[] { 0x00 }, EditType.Config)]
        public uint MinimumWakeUpInterval { get; set; } = 5;

        [XmlDataParameter(2, 0x84, 0x0A, new byte[] { 0x01 }, EditType.Config)]
        public uint MaximumWakeUpInterval { get; set; } = 100000;

        [XmlDataParameter(2, 0x84, 0x0A, new byte[] { 0x02 }, EditType.Config)]
        public uint DefaultWakeUpInterval { get; set; } = 60;

        [XmlDataParameter(2, 0x84, 0x0A, new byte[] { 0x03 }, EditType.Config)]
        public uint WakeUpIntervalStep { get; set; } = 1;

        // Wake Up Capabilities Report V3+
        [XmlDataParameter(3, 0x84, 0x0A, new byte[] { 0x04, 0x00 }, EditType.Config)]
        public bool WakeUpOnDemandSupport { get; set; } = false;
    }
}
