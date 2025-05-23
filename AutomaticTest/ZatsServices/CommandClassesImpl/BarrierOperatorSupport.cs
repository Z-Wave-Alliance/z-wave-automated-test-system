using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Utils;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;
using Utils.UI.Enums;
using Newtonsoft.Json;
using static System.TimeZoneInfo;
using static Zats.Services.ZipResult;
using ZatsServices_netcore.CommandClassesImpl.Definitions.Interfaces;
using Zats.Services.CommandClassesImpl;

// ToDo: Supervision Report Status 'Working' for Barrier Operator Set

namespace Zats.Services
{
    [CommandClassName("Barrier Operator Command Class")]
    [CommandClassId(COMMAND_CLASS_BARRIER_OPERATOR.ID)]
    [MinVersion(1)]
    [MaxVersion(1)]
    public class BarrierOperatorSupport : CommandClassSupport
    {
        private BarrierOperatorSupportData BarrierOperatorSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return BarrierOperatorSupportData; }
            set { BarrierOperatorSupportData = (BarrierOperatorSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private readonly BackgroundWorker TransitionTimer = new BackgroundWorker();
        private string TransitionTimerName = "";
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        private NodeTag CmdSrcNode = NodeTag.Empty;

        public BarrierOperatorSupport()
            : this(new BarrierOperatorSupportData() { })
        { }

        public BarrierOperatorSupport(BarrierOperatorSupportData barrierOperatorSupportData)
            : base(COMMAND_CLASS_BARRIER_OPERATOR.ID)
        {
            BarrierOperatorSupportData = barrierOperatorSupportData;
            BarrierOperatorSupportData.SupportClassObject = this;

            TransitionTimer.DoWork += new DoWorkEventHandler(TransitionTimerDoWork);
            TransitionTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(TransitionTimerRunWorkerCompleted);
            TransitionTimer.WorkerSupportsCancellation = true;
            TransitionTimer.WorkerReportsProgress = false;
        }

        public override void StartEmulation()
        {
            BarrierOperatorSupportData.SupportClassObject = this;
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

                ResultForSv = SvResultNames.Undefined;
                if (isHandlingAllowed && command != null && command.Length > 1 && BarrierOperatorSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareBarrierOperatorReport();
                    }
                    else if (command[1] == COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_SET.ID)
                    {
                        ResultForSv = HandleBarrierOperatorSetCmd(command, sendSrcNode);
                    }
                    else if (command[1] == COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_SIGNAL_SUPPORTED_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareBarrierOperatorSignalSupportedReport();
                    }
                    else if (command[1] == COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_EVENT_SIGNALING_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareBarrierOperatorEventSignalReportCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_EVENT_SIGNAL_SET.ID)
                    {
                        ResultForSv = HandleBarrierOperatorEventSignalSetCmd(command, sendSrcNode);
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

        private byte[] PrepareBarrierOperatorSignalSupportedReport()
        {
            COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_SIGNAL_SUPPORTED_REPORT rpt = new COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_SIGNAL_SUPPORTED_REPORT() { };

            List<byte> supportedSubsystemTypes = new List<byte>();
            foreach (BarrierOperatorSubsystem subsystem in BarrierOperatorSupportData.SupportedSubsystemTypes)
            {
                supportedSubsystemTypes.Add((byte)subsystem.SubsystemType);
            }
            if (supportedSubsystemTypes.Count > 0)
            {
                rpt.bitMask = CommandClassSupportUtils.CreateBitmaskFromList(supportedSubsystemTypes, 1);
            }
            if (rpt.bitMask.Count == 0)
            {
                rpt.bitMask = new List<byte> { 0x00 };
            }
            return rpt;
        }
        private byte[] PrepareBarrierOperatorEventSignalReport(BarrierOperatorSubsystemTypes requestedSubsystemType)
        {
            COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_EVENT_SIGNALING_REPORT rpt = new COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_EVENT_SIGNALING_REPORT() { };
            if (requestedSubsystemType == BarrierOperatorSubsystemTypes.AudibleNotification || requestedSubsystemType == BarrierOperatorSubsystemTypes.VisualNotification)
            {
                if (BarrierOperatorSupportData.SupportedSubsystemTypes.Find(x => x.SubsystemType == requestedSubsystemType) is BarrierOperatorSubsystem curSubsystem)
                {
                    rpt.subsystemType  = (byte) requestedSubsystemType;
                    rpt.subsystemState = (byte) curSubsystem.SubsystemState;
                    return rpt;
                }
            }
            return null;
        }
        private byte[] PrepareBarrierOperatorEventSignalReportCmd(byte[] command)
        {
            COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_EVENT_SIGNALING_GET getCmd = command;
            BarrierOperatorSubsystemTypes requestedSubsystemType = (BarrierOperatorSubsystemTypes) (byte) getCmd.subsystemType;

            return PrepareBarrierOperatorEventSignalReport(requestedSubsystemType);
        }

        public byte[] PrepareBarrierOperatorReport()
        {
            COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_REPORT rpt = new COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_REPORT() { };
          //ReportBarrierState(BarrierOperatorSupportData.CurrentState);
            rpt.state = (byte)BarrierOperatorSupportData.CurrentState;
            return rpt;
        }

        public SvResultNames HandleBarrierOperatorSignalSet(BarrierOperatorSubsystemTypes requestedSubsystemType, BarrierOperatorSubsystemStateNames requestedSubsystemState, bool updateUI, NodeTag srcNode)
        {
            if (requestedSubsystemState == BarrierOperatorSubsystemStateNames.Off || requestedSubsystemState == BarrierOperatorSubsystemStateNames.On)
            {
                if (requestedSubsystemType == BarrierOperatorSubsystemTypes.AudibleNotification || requestedSubsystemType == BarrierOperatorSubsystemTypes.VisualNotification)
                {
                    if (BarrierOperatorSupportData.SupportedSubsystemTypes.Find(x => x.SubsystemType == requestedSubsystemType) is BarrierOperatorSubsystem curSubsystem)
                    {
                        curSubsystem.SubsystemState = requestedSubsystemState;
                        if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                        {
                            SendUnsolicitedReport(PrepareBarrierOperatorReport(), srcNode);
                        }
                        if (updateUI)
                        {
                            SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
                        }
                        return SvResultNames.Successful;
                    }
                }
            }
            return SvResultNames.Failed;
        }
        public SvResultNames HandleBarrierOperatorEventSignalSetCmd(byte[] command, NodeTag srcNode)
        {
            COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_EVENT_SIGNAL_SET setCmd = command;
            BarrierOperatorSubsystemTypes requestedSubsystemType = (BarrierOperatorSubsystemTypes)(byte)setCmd.subsystemType;
            BarrierOperatorSubsystemStateNames requestedSubsystemState = (BarrierOperatorSubsystemStateNames)(byte)setCmd.subsystemState;

            return HandleBarrierOperatorSignalSet(requestedSubsystemType, requestedSubsystemState, true, srcNode);
        }

        public SvResultNames HandleBarrierOperatorSet(BarrierOperatorTargetStateNames requestedOperation, NodeTag srcNode)
        {
            TransitionTimerName = string.Format("{0} CC @ {1}: Transition Simulation", CommandClassShortName, EndPointName);

            ReportBarrierState(BarrierOperatorSupportData.CurrentState);

            if (requestedOperation == BarrierOperatorTargetStateNames.TargetClose)
            {
                if (BarrierOperatorSupportData.CurrentState == BarrierOperatorStateNames.Closed)
                {
                    ReportBarrierState(BarrierOperatorStateNames.Closed);
                    return SvResultNames.Successful;
                }
                if (BarrierOperatorSupportData.CurrentState == BarrierOperatorStateNames.Open)
                {
                    BarrierOperatorSupportData.CurrentState = BarrierOperatorStateNames.Closing;
                    BarrierOperatorSupportData.TargetState = BarrierOperatorStateNames.Closed;
                    CmdSrcNode = srcNode;
                    TransitionTimerStart();
                    return SvResultNames.Successful;
                }
            }
            if (requestedOperation == BarrierOperatorTargetStateNames.TargetOpen)
            {
                if (BarrierOperatorSupportData.CurrentState == BarrierOperatorStateNames.Open)
                {
                    ReportBarrierState(BarrierOperatorStateNames.Open);
                    return SvResultNames.Successful;
                }
                if (BarrierOperatorSupportData.CurrentState == BarrierOperatorStateNames.Stopped
                 || BarrierOperatorSupportData.CurrentState == BarrierOperatorStateNames.Closed)
                {
                    BarrierOperatorSupportData.CurrentState = BarrierOperatorStateNames.Opening;
                    BarrierOperatorSupportData.TargetState = BarrierOperatorStateNames.Open;
                    ReportBarrierState(BarrierOperatorStateNames.Opening);
                    CmdSrcNode = srcNode;
                    TransitionTimerStart();
                    return SvResultNames.Successful;
                }
                if (BarrierOperatorSupportData.CurrentState == BarrierOperatorStateNames.Closing)
                {
                    TransitionTimerCancel();
                    BarrierOperatorSupportData.CurrentState = BarrierOperatorStateNames.Stopped;
                    BarrierOperatorSupportData.TargetState = BarrierOperatorStateNames.Stopped;
                    ReportBarrierState(BarrierOperatorStateNames.Stopped);
                    return SvResultNames.Successful;
                }
            }
            CommandClassSupportUtils.ReportAddLog(CommandClassSupportData.LogLevel, 
                        CommandClassShortName + " CC @ " + EndPointName + ": Operator Set Command '{0}' (0x{1:X2}) ignored. Current state: '{2}'.",
                        requestedOperation, (byte) requestedOperation, BarrierOperatorSupportData.CurrentState);
            return SvResultNames.Failed;
        }
        public SvResultNames HandleBarrierOperatorSetCmd(byte[] command, NodeTag srcNode)
        {
            COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_SET barrierOperatorSetCmd = command;
            BarrierOperatorTargetStateNames requestedOperation = (BarrierOperatorTargetStateNames)(byte)barrierOperatorSetCmd.targetValue;

            return HandleBarrierOperatorSet(requestedOperation, srcNode);
        }

        #region Transition Timer
        private void TransitionTimerStart()
        {
            BarrierOperatorSupportData.CancelPendingTransition = TransitionTimerReset;
            TransitionTimer.RunWorkerAsync();
        }
        private void TransitionTimerDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            int transitionTimeMs = BarrierOperatorSupportData.TransitionTimeSec * 1000;
            int iStepWidthMs = 500;

            if (transitionTimeMs > 0)
            {
                ReportOperationProgress(TransitionTimerName, ReportTimerStateNames.Started, BarrierOperatorSupportData.TransitionTimeSec);
                int remainingTransitionTimeMs = transitionTimeMs;
                BarrierOperatorSupportData.RemainingTransitionTime = remainingTransitionTimeMs / 1000;
                while (remainingTransitionTimeMs > 0)
                {
                    Thread.Sleep(iStepWidthMs);
                    if (Result.State == ActionStates.Cancelled)
                    {
                        TransitionTimerCancel();
                    }
                    if (bw.CancellationPending)
                    {
                        ReportOperationProgress(TransitionTimerName, ReportTimerStateNames.CancelledRemainSec, remainingTransitionTimeMs / 1000);
                        e.Cancel = true;
                        break;
                    }
                    remainingTransitionTimeMs -= iStepWidthMs;
                    BarrierOperatorSupportData.RemainingTransitionTime = remainingTransitionTimeMs / 1000;
                    BarrierOperatorSupportData.CurrentTransitionPosition = 100 * remainingTransitionTimeMs / transitionTimeMs;
                    if (BarrierOperatorSupportData.CurrentState == BarrierOperatorStateNames.Opening)
                    {
                        BarrierOperatorSupportData.CurrentTransitionPosition = 100 - BarrierOperatorSupportData.CurrentTransitionPosition;
                    }
                    CommandClassSupportUtils.ReportAddLog(CommandClassSupportData.LogLevel, 
                                "{0} is running. Position is 0x{1:X2} = {1} % opened.", 
                                TransitionTimerName, BarrierOperatorSupportData.CurrentTransitionPosition);
                    ReportOperationProgress(TransitionTimerName, ReportTimerStateNames.RunRemainMs, remainingTransitionTimeMs);
                }
                if (!e.Cancel)
                {
                    ReportOperationProgress(TransitionTimerName, ReportTimerStateNames.ExpiredSec, BarrierOperatorSupportData.TransitionTimeSec);
                    if (BarrierOperatorSupportData.CurrentState == BarrierOperatorStateNames.Closing)
                    {
                        BarrierOperatorSupportData.CurrentState = BarrierOperatorStateNames.Closed;
                        ReportBarrierState(BarrierOperatorStateNames.Closed);
                    }
                    if (BarrierOperatorSupportData.CurrentState == BarrierOperatorStateNames.Opening)
                    {
                        BarrierOperatorSupportData.CurrentState = BarrierOperatorStateNames.Open;
                        ReportBarrierState(BarrierOperatorStateNames.Open);
                    }
                }
                BarrierOperatorSupportData.RemainingTransitionTime = 0;
                BarrierOperatorSupportData.TargetState = BarrierOperatorSupportData.CurrentState;

                if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                {
                    DeviceRestart(CmdSrcNode);
                    SendUnsolicitedReport(PrepareBarrierOperatorReport(), CmdSrcNode);
                    SupportDelegates?.DeviceSleep(this, ReportWakeupTimeout);
                }
                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
            }
        }
        private void TransitionTimerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _resetEvent.Set();
            }
        }
        private void TransitionTimerCancel()
        {
            BarrierOperatorSupportData.CancelPendingTransition?.Invoke();
            BarrierOperatorSupportData.CancelPendingTransition = null;
        }

        // This method MUST only be called via <CommandClass>SupportData.CancelPending<CommandName>.Invoke();
        private void TransitionTimerReset()
        {
            if (TransitionTimer.IsBusy)
            {
                ReportOperationProgress(TransitionTimerName, ReportTimerStateNames.Reset, 0);
                TransitionTimer.CancelAsync();
                _resetEvent.WaitOne();
            }
        }
        #endregion // Transition Timer

        private void ReportOperationProgress(string timerName, ReportTimerStateNames timerState, int timerValue)
        {
            CommandClassSupportUtils.ReportTimerState(CommandClassSupportData.LogLevel, timerName, timerState, timerValue);
        }
        private void ReportBarrierState(BarrierOperatorStateNames stateName)
        {
            CommandClassSupportUtils.ReportAddLog(CommandClassSupportData.LogLevel, 
                        CommandClassShortName + " CC @ " + EndPointName + ": Current state is '{0}'.", stateName);
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
                case COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        supervisionReport.status = (byte)SvStatusNames.Success;
                    }
                    break;
                case COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_EVENT_SIGNAL_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        supervisionReport.status = (byte)SvStatusNames.Success;
                    }
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }


        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is BarrierOperatorSupportData newData)
            {
                List<byte[]> signalReports = new List<byte[]>();

                bool identicalSupportedSubsystems = true;
                List<BarrierOperatorSubsystem> subsystemsRemoved = BarrierOperatorSupportData.SupportedSubsystemTypes.FindAll(x => newData.SupportedSubsystemTypes.Find(y => y.SubsystemType == x.SubsystemType) == null);
              //List<BarrierOperatorSubsystem> subsystemsAdded   = newData.SupportedSubsystemTypes.FindAll(x => BarrierOperatorSupportData.SupportedSubsystemTypes.Find(y => y.Type == x.Type) == null);
                if (subsystemsRemoved.Count > 0 /*|| subsystemsAdded.Count > 0*/)
                {
                    identicalSupportedSubsystems = false;
                    subsystemsRemoved.ForEach(x => BarrierOperatorSupportData.SupportedSubsystemTypes.Remove(x));
                }
                foreach (BarrierOperatorSubsystem newSupportedSubsystem in newData.SupportedSubsystemTypes)
                {
                    bool isSubsystemStateChanged = false;
                    if (BarrierOperatorSupportData.SupportedSubsystemTypes.Find(x => x.SubsystemType == newSupportedSubsystem.SubsystemType) is BarrierOperatorSubsystem curSupportedSubsystem)
                    {
                        if (curSupportedSubsystem.SubsystemState != newSupportedSubsystem.SubsystemState)
                        {
                            HandleBarrierOperatorSignalSet(newSupportedSubsystem.SubsystemType, newSupportedSubsystem.SubsystemState, false, NodeTag.Empty);
                            isSubsystemStateChanged = true;
                        }
                    }
                    else
                    {
                        identicalSupportedSubsystems = false;
                        isSubsystemStateChanged = true;
                        BarrierOperatorSupportData.SupportedSubsystemTypes.Add(newSupportedSubsystem);
                    }
                    if (isSubsystemStateChanged && ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    {
                        signalReports.Add(PrepareBarrierOperatorEventSignalReport(newSupportedSubsystem.SubsystemType));
                    }
                }

                bool identicalState = true;
                if (BarrierOperatorSupportData.CurrentState != newData.CurrentState
                    || BarrierOperatorSupportData.TargetState != newData.TargetState
                    || BarrierOperatorSupportData.TransitionTimeSec != newData.TransitionTimeSec
                    || BarrierOperatorSupportData.RemainingTransitionTime != newData.RemainingTransitionTime)
                {
                    identicalState = false;
                }

                if (!identicalSupportedSubsystems)
                {
                    if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        SendUnsolicitedReport(PrepareBarrierOperatorSignalSupportedReport(), NodeTag.Empty);
                    }
                }

                foreach (byte[] r in signalReports)
                {
                    SendUnsolicitedReport(r, NodeTag.Empty);
                }

                if (!identicalState)
                {
                    BarrierOperatorSupportData.CurrentState = newData.CurrentState;
                    BarrierOperatorSupportData.TargetState = newData.TargetState;
                    BarrierOperatorSupportData.TransitionTimeSec = newData.TransitionTimeSec;
                    BarrierOperatorSupportData.RemainingTransitionTime = newData.RemainingTransitionTime;

                    if (BarrierOperatorSupportData.CurrentState != BarrierOperatorSupportData.TargetState)
                    {
                        if (BarrierOperatorSupportData.TargetState == BarrierOperatorStateNames.Closed)
                        {
                            HandleBarrierOperatorSet(BarrierOperatorTargetStateNames.TargetClose, NodeTag.Empty);
                        }
                        if (BarrierOperatorSupportData.TargetState == BarrierOperatorStateNames.Open)
                        {
                            HandleBarrierOperatorSet(BarrierOperatorTargetStateNames.TargetOpen, NodeTag.Empty);
                        }
                    }
                }

                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class BarrierOperatorSubsystem : INestedItem
    {
        public BarrierOperatorSubsystemTypes SubsystemType { get; set; } = BarrierOperatorSubsystemTypes.Reserved;
        [CustomDataParameter(1, "State", CommandClassesImpl.UiControl.ListComboBox, CommandClassesImpl.InputType.Enum, EditType.Config /*| EditType.View*/ | EditType.Edit, typeof(BarrierOperatorSubsystemStateNames))]
        public BarrierOperatorSubsystemStateNames SubsystemState { get; set; } = BarrierOperatorSubsystemStateNames.Off;

        // only used for EDE UI. see interface for useage notes.
        [JsonIgnore]
        public int NestedId
        {
            get => (byte)SubsystemType;
            set { }
        }

        /*public BarrierOperatorSubsystem()
        {
        }*/
        public BarrierOperatorSubsystem(BarrierOperatorSubsystemTypes type, BarrierOperatorSubsystemStateNames state)
        {
            SubsystemType = type;
            SubsystemState = state;
        }
        public static List<BarrierOperatorSubsystemTypes> GetAllSupportedTypes()
        {
            return new List<BarrierOperatorSubsystemTypes>()
            {
                BarrierOperatorSubsystemTypes.AudibleNotification,
                BarrierOperatorSubsystemTypes.VisualNotification,
            };
        }
    };

    public class BarrierOperatorSupportData : CommandClassSupportData, IBasicMapping, INodeItem
    {
        [JsonIgnore]
        public IEnumerable<INestedItem> ChildItems => SupportedSubsystemTypes;

        // Internals accessible from TestCases or EDE
        public ushort TransitionTimeSec { get; set; } = 5;
        public int RemainingTransitionTime { get; set; } = 0;

        public int CurrentTransitionPosition { get; set; } = 0;                                             // Closed = 0; only valid during a running transition

        [ParameterSeparator(1, "Barrier Operator Report")]
        [XmlDataParameter(1, 0x66, 0x03, new byte[] { 0x00 }, EditType.Config | EditType.Edit)]
        public BarrierOperatorStateNames CurrentState { get; set; } = BarrierOperatorStateNames.Closed;     // Closed = 0x00

        [ParameterSeparator(1, "Barrier Operator Set")]
        [XmlDataParameter(1, 0x66, 0x01, new byte[] { 0x00 }, EditType.Config | EditType.Edit)]
        public BarrierOperatorStateNames TargetState  { get; set; } = BarrierOperatorStateNames.Closed;     // Closed = 0x00

        [JsonIgnore]
        public CancelPendingOperationDelegate CancelPendingTransition = null;

        // Supported Subsystem types: Audible (Bit 0) and Visual (Bit 1) Subsystems are allowed to support
        // Internal Default: none
        // Possible values for .Type: BarrierOperatorSubsystemTypes.AudibleNotification, BarrierOperatorSubsystemTypes.VisualNotification
        [ParameterSeparator(1, "Barrier Operator Event Signaling Report")]
        [NestedControl(1, "SupportedSubsystemTypes")]
        public List<BarrierOperatorSubsystem> SupportedSubsystemTypes { get; set; } = new List<BarrierOperatorSubsystem>();

        // Basic CC Mapping
        [JsonIgnore]
        public BarrierOperatorSupport SupportClassObject;
        bool IBasicMapping.BasicSet(byte value, NodeTag srcNode)
        {
            if (SupportClassObject.Version == 1)
            {
                COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_SET barrierOperatorSetCmd = new COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_SET() { };
                barrierOperatorSetCmd.targetValue = value;
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0} CC @ {1}: Basic Set with value 0x{2:X2} accepted.", 
                            SupportClassObject.CommandClassShortName, SupportClassObject.EndPointName, value);
                return SupportClassObject.HandleBarrierOperatorSetCmd(barrierOperatorSetCmd, srcNode) == SvResultNames.Successful;
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0} CC @ {1}: Basic Set with value 0x{2:X2} ignored.", 
                        SupportClassObject.CommandClassShortName, SupportClassObject.EndPointName, value);
            return false;
        }
        COMMAND_CLASS_BASIC_V2.BASIC_REPORT IBasicMapping.BasicV2Get()
        {
            COMMAND_CLASS_BASIC_V2.BASIC_REPORT rpt = new COMMAND_CLASS_BASIC_V2.BASIC_REPORT() { };
            rpt.currentValue = (byte)(CurrentState == BarrierOperatorStateNames.Closed ? 0x00 : 0xFF);
            rpt.targetValue = (byte) TargetState;
            rpt.duration = (byte) RemainingTransitionTime;
            return rpt;
        }
    }
    public enum BarrierOperatorStateNames : byte
    {
        [Description("0x00 Closed")]
        Closed = 0x00,
//      StoppedAtExactPositionNearClosed = 0x01,
//      StoppedAtExactPositionMiddle = 0x32,
//      StoppedAtExactPositionNearOpen = 0x63,
        [Description("0xFC Closing")]
        Closing = 0xFC,
        [Description("0xFD Stopped")]
        Stopped = 0xFD,
        [Description("0xFE Opening")]
        Opening = 0xFE,
        [Description("0xFF Open")]
        Open = 0xFF,
    };
    public enum BarrierOperatorSubsystemStateNames : byte
    {
        [Description("0x00 Off")]
        Off = 0x00,
        [Description("0xFF On")]
        On = 0xFF,
    };
    public enum BarrierOperatorTargetStateNames : byte
    {
        [Description("0x00 Target close")]
        TargetClose = 0x00,
        [Description("0xFF Target open")]
        TargetOpen = 0xFF,
    };
    public enum BarrierOperatorSubsystemTypes : byte
    {
        [Description("0x00 Reserved")]
        Reserved = 0x00,
        [Description("0x01 Audible notification")]
        AudibleNotification = 0x01,
        [Description("0x02 Visual notification")]
        VisualNotification = 0x02,
        // 0x03 ... 0xFF reserved
    }
}
