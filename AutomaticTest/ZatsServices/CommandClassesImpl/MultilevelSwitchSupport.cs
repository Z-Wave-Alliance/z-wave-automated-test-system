using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Threading;
using Utils;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;
using ZWave.Xml.Application;

// ToDo: Implementation of Secondary Switch Type action (timer) in StartLevelChange and StopLevelChange
// ToDo: Supervision Report Status 'Working'

namespace Zats.Services
{
    [CommandClassName("Multilevel Switch Command Class")]
    [CommandClassId(COMMAND_CLASS_SWITCH_MULTILEVEL.ID)]
    [MinVersion(1)]
    [MaxVersion(4)]
    public class MultilevelSwitchSupport : CommandClassSupport
    {
        private MultilevelSwitchSupportData MultilevelSwitchSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return MultilevelSwitchSupportData; }
            set { MultilevelSwitchSupportData = (MultilevelSwitchSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private readonly BackgroundWorker Transition1Timer = new BackgroundWorker();
        private string Transition1TimerName = "";
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        private readonly byte OnValue = 0x63;   // Value of 100% = ON
        private bool IsLevelChange1 = false;
        public uint RemainingTransition1DurationTimeSec = 0;
        private uint Transition1DurationSec = 0;
        private bool Down1 = false;
        private bool Decr2 = false;

        private NodeTag CmdSrcNode = NodeTag.Empty;

        public MultilevelSwitchSupport()
            : this(new MultilevelSwitchSupportData() { })
        { }

        public MultilevelSwitchSupport(MultilevelSwitchSupportData multilevelSwitchSupportData)
                : base(COMMAND_CLASS_SWITCH_MULTILEVEL.ID)
        {
            MultilevelSwitchSupportData = multilevelSwitchSupportData;
            MultilevelSwitchSupportData.SupportClassObject = this;

            Transition1Timer.DoWork += new DoWorkEventHandler(Transition1TimerDoWork);
            Transition1Timer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(TransitionTimerRunWorkerCompleted);
            Transition1Timer.WorkerSupportsCancellation = true;
            Transition1Timer.WorkerReportsProgress = false;
        }

        public override void StartEmulation()
        {
            MultilevelSwitchSupportData.SupportClassObject = this;
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
                if (isHandlingAllowed && command != null && command.Length > 1 && MultilevelSwitchSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_SWITCH_MULTILEVEL.SWITCH_MULTILEVEL_SET.ID)
                    {
                        ResultForSv = HandleMultilevelSwitchSetCmd(command, sendSrcNode);
                    }
                    else if (command[1] == COMMAND_CLASS_SWITCH_MULTILEVEL.SWITCH_MULTILEVEL_GET.ID)
                    {
                        rpt = PrepareMultilevelSwitchReport();
                    }
                    else if (command[1] == COMMAND_CLASS_SWITCH_MULTILEVEL.SWITCH_MULTILEVEL_START_LEVEL_CHANGE.ID)
                    {
                        ResultForSv = HandleStartLevelChangeCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SWITCH_MULTILEVEL.SWITCH_MULTILEVEL_STOP_LEVEL_CHANGE.ID)
                    {
                        ResultForSv = HandleStopLevelChangeCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SWITCH_MULTILEVEL_V3.SWITCH_MULTILEVEL_SUPPORTED_GET.ID)
                    {
                        rpt = PrepareSupportedReport();
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

        public byte[] PrepareMultilevelSwitchReport()
        {
            if (Version <= 3)
            {
                COMMAND_CLASS_SWITCH_MULTILEVEL.SWITCH_MULTILEVEL_REPORT rpt = new COMMAND_CLASS_SWITCH_MULTILEVEL.SWITCH_MULTILEVEL_REPORT() { };
                rpt.value = MultilevelSwitchSupportData.CurrentValuePrimary;
                return rpt;
            }
            if (Version == 4)
            {
                COMMAND_CLASS_SWITCH_MULTILEVEL_V4.SWITCH_MULTILEVEL_REPORT rpt = new COMMAND_CLASS_SWITCH_MULTILEVEL_V4.SWITCH_MULTILEVEL_REPORT() { };
                rpt.currentValue = MultilevelSwitchSupportData.CurrentValuePrimary;
                rpt.targetValue = MultilevelSwitchSupportData.TargetValuePrimary;
                rpt.duration = CommandClassSupportUtils.SecondsToDuration(RemainingTransition1DurationTimeSec);
                return rpt;
            }
            return null;
        }

        private byte[] PrepareSupportedReport()
        {
            if (Version >= 3)
            {
                COMMAND_CLASS_SWITCH_MULTILEVEL_V3.SWITCH_MULTILEVEL_SUPPORTED_REPORT rpt = new COMMAND_CLASS_SWITCH_MULTILEVEL_V3.SWITCH_MULTILEVEL_SUPPORTED_REPORT() { };
                rpt.properties1.primarySwitchType = (byte)MultilevelSwitchSupportData.PrimarySwitchType;
                rpt.properties2.secondarySwitchType = (byte)MultilevelSwitchSupportData.SecondarySwitchType;
                return rpt;
            }
            return null;
        }

        public SvResultNames HandleMultilevelSwitchSet(byte value, byte duration, NodeTag srcNode)
        {
            CmdSrcNode = srcNode;
            SvResultNames success = SvResultNames.Successful;
            Transition1TimerName = string.Format("{0} CC @ {1}: Transition Timer (Primary)", CommandClassShortName, EndPointName);

            if (Version <= 4)
            {
                if (value > OnValue && value != 0xFF)
                {
                    CommandClassSupportUtils.ReportAddLog(MultilevelSwitchSupportData.LogLevel,
                                "{0} CC @ {1}: Set ignored - invalid value 0x{2:X2} = {2}",
                                CommandClassShortName, EndPointName, value);
                    success = SvResultNames.Failed;
                }
                else
                {
                    IsLevelChange1 = false;
                    Transition1TimerCancel();

                    if (Version == 1 || duration == 0xFF)
                    {
                        MultilevelSwitchSupportData.Duration = MultilevelSwitchSupportData.FactoryDefaultSetDuration;
                    }
                    else
                    {
                        MultilevelSwitchSupportData.Duration = duration;
                    }

                    MultilevelSwitchSupportData.TargetValuePrimary = MultilevelSwitchSupportData.CurrentValuePrimary;
                    if (value == 0xFF)
                    {
                        if (MultilevelSwitchSupportData.LastSetValuePrimary != 0x00)
                        {
                            MultilevelSwitchSupportData.TargetValuePrimary = MultilevelSwitchSupportData.LastSetValuePrimary;
                        }
                    }
                    else
                    {
                        MultilevelSwitchSupportData.TargetValuePrimary = value;
                    }
                    if (MultilevelSwitchSupportData.TargetValuePrimary != MultilevelSwitchSupportData.CurrentValuePrimary)
                    {
                        Down1 = MultilevelSwitchSupportData.TargetValuePrimary < MultilevelSwitchSupportData.CurrentValuePrimary ? true : false;
                        Transition1TimerStart();
                    }
                    else
                    {
                        ReportOperationStatePrimary();
                    }
                }
            }
            return success;
        }

        public SvResultNames HandleMultilevelSwitchSetCmd(byte[] command, NodeTag srcNode)
        {
            if (Version <= 4)
            {
                COMMAND_CLASS_SWITCH_MULTILEVEL_V2.SWITCH_MULTILEVEL_SET multilevelSwitchSetCmd = command;
                return HandleMultilevelSwitchSet(multilevelSwitchSetCmd.value, multilevelSwitchSetCmd.duration, srcNode);
            }
            return SvResultNames.Failed;
        }

        #region LevelChange
        public SvResultNames HandleStartLevelChangeCmd(byte[] command)
        {
            SvResultNames success = SvResultNames.Successful;

            if (Version <= 2)
            {
                COMMAND_CLASS_SWITCH_MULTILEVEL_V2.SWITCH_MULTILEVEL_START_LEVEL_CHANGE multilevelSwitchStartLevelCmd = command;

                if (multilevelSwitchStartLevelCmd.properties1.ignoreStartLevel == 0x00
                 && multilevelSwitchStartLevelCmd.startLevel > OnValue)
                {
                    CommandClassSupportUtils.ReportAddLog(MultilevelSwitchSupportData.LogLevel,
                                "{0} CC @ {1}: StartLevelChange ignored - invalid StartLevel 0x{2:X2} = {2}",
                                CommandClassShortName, EndPointName, (byte)multilevelSwitchStartLevelCmd.startLevel);
                    success = SvResultNames.Failed;
                }
                else
                {
                    StartLevelChangePrimary(multilevelSwitchStartLevelCmd.properties1.upDown,
                                            multilevelSwitchStartLevelCmd.startLevel,
                                            multilevelSwitchStartLevelCmd.properties1.ignoreStartLevel,
                                            multilevelSwitchStartLevelCmd.dimmingDuration);
                }
            }
            if (Version >= 3)
            {
                COMMAND_CLASS_SWITCH_MULTILEVEL_V3.SWITCH_MULTILEVEL_START_LEVEL_CHANGE multilevelSwitchStartLevelCmd = command;

                if (multilevelSwitchStartLevelCmd.properties1.ignoreStartLevel == 0x00
                 && multilevelSwitchStartLevelCmd.startLevel > OnValue)
                {
                    CommandClassSupportUtils.ReportAddLog(MultilevelSwitchSupportData.LogLevel,
                                "{0} CC @ {1}: StartLevelChange ignored - invalid StartLevel 0x{2:X2} = {2}",
                                CommandClassShortName, EndPointName, (byte)multilevelSwitchStartLevelCmd.startLevel);
                    success = SvResultNames.Failed;
                }
                else
                {
                    if (multilevelSwitchStartLevelCmd.properties1.upDown != 0x03)
                    {
                        StartLevelChangePrimary(multilevelSwitchStartLevelCmd.properties1.upDown,
                                                multilevelSwitchStartLevelCmd.startLevel,
                                                multilevelSwitchStartLevelCmd.properties1.ignoreStartLevel,
                                                multilevelSwitchStartLevelCmd.dimmingDuration);
                    }
                    if (MultilevelSwitchSupportData.SecondarySwitchType != 0x00 && multilevelSwitchStartLevelCmd.properties1.incDec != 0x03)
                    {
                        Decr2 = multilevelSwitchStartLevelCmd.properties1.incDec == 0x00 ? false : true;

                        MultilevelSwitchSupportData.Duration = MultilevelSwitchSupportData.FactoryDefaultLevelDuration;
                        if (multilevelSwitchStartLevelCmd.dimmingDuration != 0xFF)
                        {
                            MultilevelSwitchSupportData.Duration = multilevelSwitchStartLevelCmd.dimmingDuration;
                        }
                        MultilevelSwitchSupportData.StepSizeSecondary = multilevelSwitchStartLevelCmd.stepSize;
                        CommandClassSupportUtils.ReportAddLog(MultilevelSwitchSupportData.LogLevel,
                                    "{0} CC @ {1}: LevelChange [Secondary]: Duration = 0x{2:X2} = {3} sec - StepSize = {4} - Decr2 = {5}",
                                    CommandClassShortName,
                                    EndPointName,
                                    MultilevelSwitchSupportData.Duration,
                                    CommandClassSupportUtils.DurationToSeconds(MultilevelSwitchSupportData.Duration),
                                    MultilevelSwitchSupportData.StepSizeSecondary,
                                    Decr2);

                        MultilevelSwitchSupportData.TargetValueSecondary = MultilevelSwitchSupportData.CurrentValueSecondary;
                        if (Decr2)
                        {
                            if (MultilevelSwitchSupportData.CurrentValueSecondary - MultilevelSwitchSupportData.StepSizeSecondary >= 0)
                            {
                                MultilevelSwitchSupportData.TargetValueSecondary = (byte)(MultilevelSwitchSupportData.TargetValueSecondary - MultilevelSwitchSupportData.StepSizeSecondary);
                            }
                            else
                            {
                                MultilevelSwitchSupportData.TargetValueSecondary = 0x00;
                            }
                        }

                        else
                        {
                            if (MultilevelSwitchSupportData.CurrentValueSecondary + MultilevelSwitchSupportData.StepSizeSecondary <= OnValue)
                            {
                                MultilevelSwitchSupportData.TargetValueSecondary = (byte)(MultilevelSwitchSupportData.TargetValueSecondary + MultilevelSwitchSupportData.StepSizeSecondary);
                            }
                            else
                            {
                                MultilevelSwitchSupportData.TargetValueSecondary = OnValue;
                            }
                        }
                        ReportOperationStateSecondary();
                        if (MultilevelSwitchSupportData.TargetValueSecondary != MultilevelSwitchSupportData.CurrentValueSecondary)
                        {
                            // ToDo: Start a Timer to respect the Duration field
                            CommandClassSupportUtils.ReportAddLog(MultilevelSwitchSupportData.LogLevel,
                                        "{0} CC @ {1}: Duration ignored (not implemented).",
                                        CommandClassShortName, EndPointName);
                            MultilevelSwitchSupportData.CurrentValueSecondary = MultilevelSwitchSupportData.TargetValueSecondary; // preliminary
                            ReportOperationStateSecondary();
                        }
                    }
                }
            }
            return success;
        }

        public SvResultNames HandleStopLevelChangeCmd(byte[] command)
        {
            SvResultNames success = SvResultNames.Successful;
            CommandClassSupportUtils.ReportAddLog(MultilevelSwitchSupportData.LogLevel,
                        "{0} CC @ {1}: Stop Level Change accepted.", CommandClassName, EndPointName);
            Transition1TimerCancel();
            ReportOperationStatePrimary();
            ReportOperationStateSecondary();
            return success;
        }

        private void StartLevelChangePrimary(byte upDown, byte startLevel, byte ignoreStartLevel, byte dimmingDuration)
        {
            IsLevelChange1 = true;
            Transition1TimerCancel();

            Down1 = upDown == 0x00 ? false : true;
            MultilevelSwitchSupportData.TargetValuePrimary = Down1 ? (byte)0x00 : OnValue;

            if (ignoreStartLevel == 0x00 && startLevel <= OnValue)
            {
                MultilevelSwitchSupportData.CurrentValuePrimary = startLevel;
            }

            if (Version == 1 || dimmingDuration == 0xFF)
            {
                MultilevelSwitchSupportData.Duration = MultilevelSwitchSupportData.FactoryDefaultLevelDuration;
            }
            else
            {
                MultilevelSwitchSupportData.Duration = dimmingDuration;
            }
            CommandClassSupportUtils.ReportAddLog(MultilevelSwitchSupportData.LogLevel,
                        "{0} CC @ {1}: LevelChange [Primary]: StartLevel = {2} - Ignore = {3} - Duration = 0x{4:X2} = {5} sec - Down1 = {6}",
                        CommandClassName,
                        EndPointName,
                        startLevel,
                        ignoreStartLevel,
                        MultilevelSwitchSupportData.Duration,
                        CommandClassSupportUtils.DurationToSeconds(MultilevelSwitchSupportData.Duration),
                        Down1);
            Transition1TimerStart();
        }
        #endregion // LevelChange

        #region Transition 1 Timer
        private void Transition1TimerStart()
        {
            MultilevelSwitchSupportData.CancelPendingTransition1 = Transition1TimerReset;
            Transition1Timer.RunWorkerAsync();
        }
        private void Transition1TimerDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            int numberOfSteps;
            int sleepTimePerStepMs;
            uint durationOffOnSec;
            uint remainingDurationTimeMs;
            uint transitionDurationMs;

            numberOfSteps = Math.Abs(MultilevelSwitchSupportData.TargetValuePrimary - MultilevelSwitchSupportData.CurrentValuePrimary);

            if (IsLevelChange1)
            {
                durationOffOnSec = CommandClassSupportUtils.DurationToSeconds(MultilevelSwitchSupportData.Duration);
                Transition1DurationSec = Convert.ToUInt32(durationOffOnSec * numberOfSteps / OnValue);
                transitionDurationMs = Convert.ToUInt32(1000 * durationOffOnSec * numberOfSteps / OnValue);
            }
            else
            {
                Transition1DurationSec = CommandClassSupportUtils.DurationToSeconds(MultilevelSwitchSupportData.Duration);
                transitionDurationMs = Transition1DurationSec * 1000;
                durationOffOnSec = Transition1DurationSec; // dummy for log
            }

            if (durationOffOnSec > 0 && numberOfSteps != 0)
            {
                sleepTimePerStepMs = Convert.ToInt32(transitionDurationMs / numberOfSteps);
                remainingDurationTimeMs = transitionDurationMs;
                RemainingTransition1DurationTimeSec = transitionDurationMs / 1000;

                CommandClassSupportUtils.ReportAddLog(MultilevelSwitchSupportData.LogLevel,
                            "sleepTimePerStepMs: {0}, numberOfSteps = {1}, durationOffOn = {2}, TransitionDurationSec = {3}, transitionDurationMs = {4}, Down = {5}",
                            sleepTimePerStepMs, numberOfSteps, durationOffOnSec,
                            Transition1DurationSec, transitionDurationMs, Down1);
                CommandClassSupportUtils.ReportTimerState(MultilevelSwitchSupportData.LogLevel,
                            Transition1TimerName, ReportTimerStateNames.Started, durationOffOnSec);
                for (int i = numberOfSteps; i > 0; i--)
                {
                    ReportOperationStatePrimary();

                    if (Result.State == ActionStates.Cancelled)
                    {
                        Transition1TimerCancel();
                    }
                    Thread.Sleep(sleepTimePerStepMs);

                    if (bw.CancellationPending)
                    {
                        CommandClassSupportUtils.ReportTimerState(MultilevelSwitchSupportData.LogLevel,
                                    Transition1TimerName, ReportTimerStateNames.CancelledRemainSec, RemainingTransition1DurationTimeSec);
                        //if (IsLevelChange1) // always hold Current and Target identically, if a transition stops.
                        {
                            MultilevelSwitchSupportData.TargetValuePrimary = MultilevelSwitchSupportData.CurrentValuePrimary;
                            if (Version >= 3)
                            {
                                MultilevelSwitchSupportData.CurrentValueSecondary = MultilevelSwitchSupportData.TargetValueSecondary; // preliminary
                              //Transition2TimerCancel();
                            }
                        }
                        e.Cancel = true;
                        break;
                    }

                    remainingDurationTimeMs -= (uint)sleepTimePerStepMs;
                    RemainingTransition1DurationTimeSec = remainingDurationTimeMs / 1000;

                    if (Down1)
                    {
                        if (MultilevelSwitchSupportData.CurrentValuePrimary > MultilevelSwitchSupportData.TargetValuePrimary)
                        {
                            MultilevelSwitchSupportData.CurrentValuePrimary--;
                        }
                    }
                    else
                    {
                        if (MultilevelSwitchSupportData.CurrentValuePrimary < MultilevelSwitchSupportData.TargetValuePrimary)
                        {
                            MultilevelSwitchSupportData.CurrentValuePrimary++;
                        }
                    }
                }
                if (!e.Cancel)
                {
                    CommandClassSupportUtils.ReportTimerState(MultilevelSwitchSupportData.LogLevel,
                                Transition1TimerName, ReportTimerStateNames.ExpiredSec, Transition1DurationSec);
                    ReportOperationStatePrimary();
                }
            }
            else
            {
                //CommandClassSupportUtils.ReportTimerState(LogLevel, Transition1TimerName, ReportTimerStateNames.ExpiredSec, 0);
                ReportOperationStatePrimary();
                MultilevelSwitchSupportData.CurrentValuePrimary = MultilevelSwitchSupportData.TargetValuePrimary;
            }
            if (MultilevelSwitchSupportData.CurrentValuePrimary != 0x00)
            {
                //if (!IsLevelChange1)
                {
                    MultilevelSwitchSupportData.LastSetValuePrimary = MultilevelSwitchSupportData.CurrentValuePrimary;
                }
            }
            RemainingTransition1DurationTimeSec = 0x00;
            MultilevelSwitchSupportData.Duration = 0x00;

            if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
            {
                DeviceRestart(CmdSrcNode);
                SendUnsolicitedReport(PrepareMultilevelSwitchReport(), CmdSrcNode);
                SupportDelegates?.DeviceSleep(this, ReportWakeupTimeout);
            }
            SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
        }

        private void TransitionTimerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _resetEvent.Set();
            }
        }
        private void Transition1TimerCancel()
        {
            MultilevelSwitchSupportData.CancelPendingTransition1?.Invoke();
            MultilevelSwitchSupportData.CancelPendingTransition1 = null;
        }

        // This method MUST only be called via <CommandClass>SupportData.CancelPending<CommandName>.Invoke();
        private void Transition1TimerReset()
        {
            if (Transition1Timer.IsBusy)
            {
                CommandClassSupportUtils.ReportTimerState(MultilevelSwitchSupportData.LogLevel,
                            Transition1TimerName, ReportTimerStateNames.Reset, 0);
                Transition1Timer.CancelAsync();
                _resetEvent.WaitOne();
            }
        }
        #endregion // Transition 1 Timer

        private void ReportOperationStatePrimary()
        {
            CommandClassSupportUtils.ReportAddLog(MultilevelSwitchSupportData.LogLevel,
                        "{0} CC @ {1}: [Primary] Current value: 0x{2:X2} - Target value: 0x{3:X2} - Duration: 0x{4:X2} = {5} sec - Remaining: {6} sec",
                        CommandClassShortName,
                        EndPointName,
                        MultilevelSwitchSupportData.CurrentValuePrimary,
                        MultilevelSwitchSupportData.TargetValuePrimary,
                        MultilevelSwitchSupportData.Duration,
                        CommandClassSupportUtils.DurationToSeconds(MultilevelSwitchSupportData.Duration),
                        RemainingTransition1DurationTimeSec);
        }
        private void ReportOperationStateSecondary()
        {
            CommandClassSupportUtils.ReportAddLog(MultilevelSwitchSupportData.LogLevel,
                        "{0} CC @ {1}: [Secondary] Current value: 0x{2:X2} - Target value: 0x{3:X2} - Duration: 0x{4:X2} = {5} sec",
                        CommandClassShortName,
                        EndPointName,
                        MultilevelSwitchSupportData.CurrentValueSecondary,
                        MultilevelSwitchSupportData.TargetValueSecondary,
                        MultilevelSwitchSupportData.Duration,
                        CommandClassSupportUtils.DurationToSeconds(MultilevelSwitchSupportData.Duration));
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet,
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            {
                if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != CommandClassId)
                {
                    supervisionReport.status = (byte)SvStatusNames.NoSupport;
                    return;
                }

                supervisionReport.status = (byte)SvStatusNames.Fail;  // FAIL as default
                switch (supervisionGet.encapsulatedCommand[1])
                {
                    case COMMAND_CLASS_SWITCH_MULTILEVEL.SWITCH_MULTILEVEL_SET.ID:
                        if (ResultForSv == SvResultNames.Successful)
                        {
                            supervisionReport.status = (byte)SvStatusNames.Success;
                        }
                        break;
                    case COMMAND_CLASS_SWITCH_MULTILEVEL.SWITCH_MULTILEVEL_START_LEVEL_CHANGE.ID:
                        if (ResultForSv == SvResultNames.Successful)
                        {
                            supervisionReport.status = (byte)SvStatusNames.Success;
                        }
                        break;
                    case COMMAND_CLASS_SWITCH_MULTILEVEL.SWITCH_MULTILEVEL_STOP_LEVEL_CHANGE.ID:
                        if (ResultForSv == SvResultNames.Successful)
                        {
                            supervisionReport.status = (byte)SvStatusNames.Success;
                        }
                        break;
                }
                ResultForSv = SvResultNames.Undefined;
            }
        }


        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is MultilevelSwitchSupportData newData)
            {
                bool identicalCapabilities = true;
                if (MultilevelSwitchSupportData.FactoryDefaultSetDuration != newData.FactoryDefaultSetDuration
                 || MultilevelSwitchSupportData.FactoryDefaultLevelDuration != newData.FactoryDefaultLevelDuration
                 || MultilevelSwitchSupportData.PrimarySwitchType != newData.PrimarySwitchType
                 || MultilevelSwitchSupportData.SecondarySwitchType != newData.SecondarySwitchType)
                {
                    identicalCapabilities = false;
                }
                bool identicalSwitchTypes = true;
                if (MultilevelSwitchSupportData.FactoryDefaultSetDuration != newData.FactoryDefaultSetDuration
                 || MultilevelSwitchSupportData.FactoryDefaultLevelDuration != newData.FactoryDefaultLevelDuration
                 || MultilevelSwitchSupportData.PrimarySwitchType != newData.PrimarySwitchType
                 || MultilevelSwitchSupportData.SecondarySwitchType != newData.SecondarySwitchType)
                {
                    identicalSwitchTypes = false;
                }
                bool identicalState = true;
                if (MultilevelSwitchSupportData.CurrentValuePrimary != newData.CurrentValuePrimary
                 || MultilevelSwitchSupportData.TargetValuePrimary != newData.TargetValuePrimary
                 || MultilevelSwitchSupportData.CurrentValueSecondary != newData.CurrentValueSecondary
                 || MultilevelSwitchSupportData.TargetValueSecondary != newData.TargetValueSecondary)
                {
                    identicalState = false;
                }

                if (!identicalCapabilities)
                {
                    MultilevelSwitchSupportData.FactoryDefaultSetDuration = newData.FactoryDefaultSetDuration;
                    MultilevelSwitchSupportData.FactoryDefaultLevelDuration = newData.FactoryDefaultLevelDuration;
                    //if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    //    SendUnsolicitedReport(PrepareSupportedReport());
                }
                if (!identicalSwitchTypes)
                {
                    MultilevelSwitchSupportData.PrimarySwitchType = newData.PrimarySwitchType;
                    MultilevelSwitchSupportData.SecondarySwitchType = newData.SecondarySwitchType;
                    if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        SendUnsolicitedReport(PrepareSupportedReport(), NodeTag.Empty);
                    }
                }
                if (!identicalState)
                {
                    MultilevelSwitchSupportData.CurrentValueSecondary = newData.CurrentValueSecondary;
                    MultilevelSwitchSupportData.TargetValueSecondary = newData.TargetValueSecondary;
                    MultilevelSwitchSupportData.Duration = newData.Duration;
                    RemainingTransition1DurationTimeSec = CommandClassSupportUtils.DurationToSeconds(MultilevelSwitchSupportData.Duration); // necessary for Unsolicited Report
                    if (Version <= 3)
                    {
                        //MultilevelSwitchSupportData.CurrentValuePrimary = newData.CurrentValuePrimary;
                        //MultilevelSwitchSupportData.TargetValuePrimary = newData.TargetValuePrimary;
                        HandleMultilevelSwitchSet(newData.CurrentValuePrimary, 0, NodeTag.Empty);
                    }
                    if (Version == 4)
                    {
                        MultilevelSwitchSupportData.CurrentValuePrimary = newData.CurrentValuePrimary;
                        MultilevelSwitchSupportData.TargetValuePrimary = newData.TargetValuePrimary;
                        if (MultilevelSwitchSupportData.CurrentValuePrimary != MultilevelSwitchSupportData.TargetValuePrimary)
                        {
                            HandleMultilevelSwitchSet(MultilevelSwitchSupportData.TargetValuePrimary, MultilevelSwitchSupportData.Duration, NodeTag.Empty);
                        }
                    }
                }
                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class MultilevelSwitchSupportData : CommandClassSupportData, IBasicMapping
    {
        // Internals accessible from TestCases or EDE
        [CustomDataParameter(1, "Factory Default Set Duration", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Int_0_254, EditType.Config)]
        public byte FactoryDefaultSetDuration { get; set; } = 0;

        [CustomDataParameter(1, "Factory Default Level Duration", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Int_0_254, EditType.Config)]
        public byte FactoryDefaultLevelDuration { get; set; } = 50; // from 0 to 99

        [JsonIgnore]
        public CancelPendingOperationDelegate CancelPendingTransition1 = null;

        // V1+
        public byte LastSetValuePrimary { get; set; } = 0x05; // this value is affected by Set commands and completed Level Change commands

        [ParameterSeparator(1, "Multilevel Switch Report")]
        [XmlDataParameter(1, 3, 0x26, 0x03, new byte[] { 0x00 }, EditType.Config | EditType.Edit)]
        public byte ValuePrimary
        {
            get { return CurrentValuePrimary; }
            set { CurrentValuePrimary = value; }
        }

        // 4+
        [XmlDataParameter(4, 0x26, 0x03, new byte[] { 0x00 }, EditType.Config /*| EditType.Edit*/)]
        public byte CurrentValuePrimary { get; set; } = 0x00;
        [XmlDataParameter(4, 0x26, 0x03, new byte[] { 0x01 }, EditType.Config | EditType.Edit)]
        public byte TargetValuePrimary { get; set; } = 0x00;

        [XmlDataParameter(4, 0x26, 0x03, new byte[] { 0x02 }, EditType.Config | EditType.Edit)]
        public byte Duration { get; set; } = 0x00;
        // V3+

        [ParameterSeparator(3, "Multilevel Switch Supported Report")]
        [XmlDataParameter(3, 0x26, 0x07, new byte[] { 0x00, 0x00 }, EditType.Config)]
        public SwitchTypeNames PrimarySwitchType { get; set; } = SwitchTypeNames.DownUp;        // 0x02 = Down (0x00) / Up (0x63/0xFF)

        [XmlDataParameter(3, 0x26, 0x07, new byte[] { 0x01, 0x00 }, EditType.Config)]
        public SwitchTypeNames SecondarySwitchType { get; set; } = SwitchTypeNames.CloseOpen;   // 0x03 = Close (0x00) / Open (0x63/0xFF)
        public byte CurrentValueSecondary { get; set; } = 0x00;
        public byte TargetValueSecondary { get; set; } = 0x00;
        public byte StepSizeSecondary { get; set; } = 0x00;

        // Basic CC Mapping
        [JsonIgnore]
        public MultilevelSwitchSupport SupportClassObject;
        bool IBasicMapping.BasicSet(byte value, NodeTag srcNode)
        {
            if (SupportClassObject.Version <= 4)
            {
                COMMAND_CLASS_SWITCH_MULTILEVEL_V2.SWITCH_MULTILEVEL_SET multilevelSwitchSetCmd = new COMMAND_CLASS_SWITCH_MULTILEVEL_V2.SWITCH_MULTILEVEL_SET() { };
                multilevelSwitchSetCmd.value = value;
                multilevelSwitchSetCmd.duration = 0x00;
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0} CC @ {1}: Basic Set with value 0x{2:X2} accepted.", 
                            SupportClassObject.CommandClassShortName, SupportClassObject.EndPointName, value);
                return SupportClassObject.HandleMultilevelSwitchSetCmd(multilevelSwitchSetCmd, srcNode) == SvResultNames.Successful ? true : false;
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0} CC @ {1}: Basic Set with value 0x{2:X2} ignored.", 
                        SupportClassObject.CommandClassShortName, SupportClassObject.EndPointName, value);
            return false;
        }
        COMMAND_CLASS_BASIC_V2.BASIC_REPORT IBasicMapping.BasicV2Get()
        {
            COMMAND_CLASS_BASIC_V2.BASIC_REPORT rpt = new COMMAND_CLASS_BASIC_V2.BASIC_REPORT() { };
            rpt.currentValue = CurrentValuePrimary;
            rpt.targetValue = TargetValuePrimary;
            rpt.duration = CommandClassSupportUtils.SecondsToDuration(SupportClassObject.RemainingTransition1DurationTimeSec);
            return rpt;
        }
    }

    public enum SwitchTypeNames : byte
    {
        Undefined      = 0x00,
        OffOn          = 0x01,
        DownUp         = 0x02,
        CloseOpen      = 0x03,
        Clockwise      = 0x04,
        LeftRight      = 0x05,
        ReverseForward = 0x06,
        PullPush       = 0x07,
    }
}
