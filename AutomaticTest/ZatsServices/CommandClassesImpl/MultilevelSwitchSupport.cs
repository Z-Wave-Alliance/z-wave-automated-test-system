// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
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

// ToDo: Implementation of Secondary Switch Type action (timer) in StartLevelChange and StopLevelChange
// ToDo: Supervision Report Status 'Working'

namespace Zats.Services
{
    public class MultilevelSwitchSupport : CommandClassSupport
    {
        private MultilevelSwitchSupportData MultilevelSwitchSupportData { get; set; }

        public readonly string CommandClassName = "Multilevel Switch CC";
        public string EndPointName = "";
        private readonly int LogLevel;  // shortcut for SupportData.LogLevel
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

        public MultilevelSwitchSupport()
            : this(new MultilevelSwitchSupportData() { })
        { }

        public MultilevelSwitchSupport(MultilevelSwitchSupportData multilevelSwitchSupportData)
                : base(COMMAND_CLASS_SWITCH_MULTILEVEL.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 4;
            MultilevelSwitchSupportData = multilevelSwitchSupportData;
            MultilevelSwitchSupportData.SupportClassObject = this;
            LogLevel = MultilevelSwitchSupportData.LogLevel;

            Transition1Timer.DoWork += new DoWorkEventHandler(Transition1Timer_DoWork);
            Transition1Timer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(TransitionTimer_RunWorkerCompleted);
            Transition1Timer.WorkerSupportsCancellation = true;
            Transition1Timer.WorkerReportsProgress = false;
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
                Transition1TimerName = string.Format("{0}{1}: Transition Timer (Primary)", CommandClassName, EndPointName);
                ResultForSv = SvResultNames.Undefined;
                if (isResponseAllowed && command != null && command.Length > 1 && MultilevelSwitchSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_SWITCH_MULTILEVEL.SWITCH_MULTILEVEL_SET.ID)
                    {
                        ResultForSv = HandleMultilevelSwitchSet(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SWITCH_MULTILEVEL.SWITCH_MULTILEVEL_GET.ID)
                    {
                        rpt = PrepareReport();
                    }
                    else if (command[1] == COMMAND_CLASS_SWITCH_MULTILEVEL.SWITCH_MULTILEVEL_START_LEVEL_CHANGE.ID)
                    {
                        ResultForSv = HandleStartLevelChange(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SWITCH_MULTILEVEL.SWITCH_MULTILEVEL_STOP_LEVEL_CHANGE.ID)
                    {
                        ResultForSv = HandleStopLevelChange(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SWITCH_MULTILEVEL_V3.SWITCH_MULTILEVEL_SUPPORTED_GET.ID)
                    {
                        rpt = PrepareSupportedReport();
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

        public byte[] PrepareReport()
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
                rpt.properties1.primarySwitchType = (byte) MultilevelSwitchSupportData.PrimarySwitchType;
                rpt.properties2.secondarySwitchType = (byte) MultilevelSwitchSupportData.SecondarySwitchType;
                return rpt;
            }
            return null;
        }

        public SvResultNames HandleMultilevelSwitchSet(byte[] command)
        {
            SvResultNames success = SvResultNames.Successful;

            if (Version <= 4)
            {
                COMMAND_CLASS_SWITCH_MULTILEVEL_V2.SWITCH_MULTILEVEL_SET multilevelSwitchSetCmd = command;

                if (multilevelSwitchSetCmd.value > OnValue && multilevelSwitchSetCmd.value != 0xFF)
                {
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Set ignored - invalid value 0x{2:X2} = {2}",
                                CommandClassName, EndPointName, (byte) multilevelSwitchSetCmd.value);
                    success = SvResultNames.Failed;
                }
                else
                {
                    IsLevelChange1 = false;
                    if (Version == 1 || multilevelSwitchSetCmd.duration == 0xFF)
                    {
                        MultilevelSwitchSupportData.Duration = MultilevelSwitchSupportData.FactoryDefaultSetDuration;
                    }
                    else
                    {
                        MultilevelSwitchSupportData.Duration = multilevelSwitchSetCmd.duration;
                    }

                    MultilevelSwitchSupportData.TargetValuePrimary = MultilevelSwitchSupportData.CurrentValuePrimary;
                    if (multilevelSwitchSetCmd.value == 0xFF)
                    {
                        if (MultilevelSwitchSupportData.LastSetValuePrimary != 0x00)
                        {
                            MultilevelSwitchSupportData.TargetValuePrimary = MultilevelSwitchSupportData.LastSetValuePrimary;
                        }
                    }
                    else
                    {
                        MultilevelSwitchSupportData.TargetValuePrimary = multilevelSwitchSetCmd.value;
                    }
                    if (MultilevelSwitchSupportData.TargetValuePrimary != MultilevelSwitchSupportData.CurrentValuePrimary)
                    {
                        Down1 = MultilevelSwitchSupportData.TargetValuePrimary < MultilevelSwitchSupportData.CurrentValuePrimary ? true : false;
                        Transition1Timer_Reset();
                        Transition1Timer.RunWorkerAsync();
                    }
                    else
                    {
                        ReportOperationStatePrimary();
                    }
                }
            }
            return success;
        }

        public SvResultNames HandleStartLevelChange(byte[] command)
        {
            SvResultNames success = SvResultNames.Successful;

            if (Version <= 2)
            {
                COMMAND_CLASS_SWITCH_MULTILEVEL_V2.SWITCH_MULTILEVEL_START_LEVEL_CHANGE multilevelSwitchStartLevelCmd = command;

                if (multilevelSwitchStartLevelCmd.properties1.ignoreStartLevel == 0x00
                 && multilevelSwitchStartLevelCmd.startLevel > OnValue)
                {
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: StartLevelChange ignored - invalid StartLevel 0x{2:X2} = {2}",
                            CommandClassName, EndPointName, (byte) multilevelSwitchStartLevelCmd.startLevel);
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
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: StartLevelChange ignored - invalid StartLevel 0x{2:X2} = {2}",
                            CommandClassName, EndPointName, (byte) multilevelSwitchStartLevelCmd.startLevel);
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
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: LevelChange [Secondary]: Duration = 0x{2:X2} = {3} sec - StepSize = {4} - Decr2 = {5}",
                                CommandClassName, 
                                EndPointName,
                                MultilevelSwitchSupportData.Duration,
                                CommandClassSupportUtils.DurationToSeconds(MultilevelSwitchSupportData.Duration),
                                MultilevelSwitchSupportData.StepSizeSecondary,
                                Decr2);

                        MultilevelSwitchSupportData.TargetValueSecondary = MultilevelSwitchSupportData.CurrentValueSecondary;
                        if (Decr2)
                        {
                            if (MultilevelSwitchSupportData.CurrentValueSecondary - MultilevelSwitchSupportData.StepSizeSecondary >= 0)
                                MultilevelSwitchSupportData.TargetValueSecondary = (byte)(MultilevelSwitchSupportData.TargetValueSecondary - MultilevelSwitchSupportData.StepSizeSecondary);
                            else
                                MultilevelSwitchSupportData.TargetValueSecondary = 0x00;
                        }
                    
                        else
                        {
                            if (MultilevelSwitchSupportData.CurrentValueSecondary + MultilevelSwitchSupportData.StepSizeSecondary <= OnValue)
                                MultilevelSwitchSupportData.TargetValueSecondary = (byte)(MultilevelSwitchSupportData.TargetValueSecondary + MultilevelSwitchSupportData.StepSizeSecondary);
                            else
                                MultilevelSwitchSupportData.TargetValueSecondary = OnValue;
                        }
                        ReportOperationStateSecondary();
                        if (MultilevelSwitchSupportData.TargetValueSecondary != MultilevelSwitchSupportData.CurrentValueSecondary)
                        {
                            // ToDo: Start a Timer to respect the Duration field
                            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Duration ignored (not implemented).", CommandClassName, EndPointName);
                            MultilevelSwitchSupportData.CurrentValueSecondary = MultilevelSwitchSupportData.TargetValueSecondary; // preliminary
                            ReportOperationStateSecondary();
                        }
                    }
                }
            }
            return success;
        }

        public SvResultNames HandleStopLevelChange(byte[] command)
        {
            SvResultNames success = SvResultNames.Successful;
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Stop Level Change accepted.", CommandClassName, EndPointName);
            Transition1Timer_Reset();
            MultilevelSwitchSupportData.TargetValuePrimary = MultilevelSwitchSupportData.CurrentValuePrimary;
            if (Version >= 3)
            {
                MultilevelSwitchSupportData.CurrentValueSecondary = MultilevelSwitchSupportData.TargetValueSecondary; // preliminary
                //Transition2Timer_Reset();
            }
            ReportOperationStatePrimary();
            ReportOperationStateSecondary();
            return success;
        }

        private void StartLevelChangePrimary(byte upDown, byte startLevel, byte ignoreStartLevel, byte dimmingDuration)
        {
            IsLevelChange1 = true;

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
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: LevelChange [Primary]: StartLevel = {2} - Ignore = {3} - Duration = 0x{4:X2} = {5} sec - Down1 = {6}",
                    CommandClassName,
                    EndPointName,
                    startLevel,
                    ignoreStartLevel,
                    MultilevelSwitchSupportData.Duration,
                    CommandClassSupportUtils.DurationToSeconds(MultilevelSwitchSupportData.Duration),
                    Down1);
            Transition1Timer_Reset();
            Transition1Timer.RunWorkerAsync();
        }

        private void Transition1Timer_DoWork(object sender, DoWorkEventArgs e)
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

                CommandClassSupportUtils.ReportAddLog(LogLevel, "sleepTimePerStepMs: {0}, numberOfSteps = {1}, durationOffOn = {2}, TransitionDurationSec = {3}, transitionDurationMs = {4}, Down = {5}", sleepTimePerStepMs, numberOfSteps, durationOffOnSec, Transition1DurationSec, transitionDurationMs, Down1);
                CommandClassSupportUtils.ReportTimerState(LogLevel, Transition1TimerName, ReportTimerStateNames.Started, durationOffOnSec);
                for (int i = numberOfSteps; i > 0; i--)
                {
                    ReportOperationStatePrimary();

                    Thread.Sleep(sleepTimePerStepMs);

                    if (bw.CancellationPending)
                    {
                        CommandClassSupportUtils.ReportTimerState(LogLevel, Transition1TimerName, ReportTimerStateNames.CancelledRemainSec, RemainingTransition1DurationTimeSec);
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
                    CommandClassSupportUtils.ReportTimerState(LogLevel, Transition1TimerName, ReportTimerStateNames.ExpiredSec, Transition1DurationSec);
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
        }

        private void TransitionTimer_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _resetEvent.Set();
            }
        }
        private void Transition1Timer_Reset()
        {
            if (Transition1Timer.IsBusy)
            {
                Transition1Timer.CancelAsync();
                _resetEvent.WaitOne();
                CommandClassSupportUtils.ReportTimerState(LogLevel, Transition1TimerName, ReportTimerStateNames.Reset, 0);
            }
        }

        private void ReportOperationStatePrimary()
        {
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: [Primary] Current value: 0x{2:X2} - Target value: 0x{3:X2} - Duration: 0x{4:X2} = {5} sec - Remaining: {6} sec",
                CommandClassName,
                EndPointName,
                MultilevelSwitchSupportData.CurrentValuePrimary, 
                MultilevelSwitchSupportData.TargetValuePrimary, 
                MultilevelSwitchSupportData.Duration,
                CommandClassSupportUtils.DurationToSeconds(MultilevelSwitchSupportData.Duration),
                RemainingTransition1DurationTimeSec);
        }
        private void ReportOperationStateSecondary()
        {
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: [Secondary] Current value: 0x{2:X2} - Target value: 0x{3:X2} - Duration: 0x{4:X2} = {5} sec",
                CommandClassName,
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
                if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != Id)
                {
                    supervisionReport.status = (byte) SvStatusNames.NoSupport;
                    return;
                }

                supervisionReport.status = (byte) SvStatusNames.Fail;  // FAIL as default
                switch (supervisionGet.encapsulatedCommand[1])
                {
                    case COMMAND_CLASS_SWITCH_MULTILEVEL.SWITCH_MULTILEVEL_SET.ID:
                        if (ResultForSv == SvResultNames.Successful)
                            supervisionReport.status = (byte) SvStatusNames.Success;
                        break;
                    case COMMAND_CLASS_SWITCH_MULTILEVEL.SWITCH_MULTILEVEL_START_LEVEL_CHANGE.ID:
                        if (ResultForSv == SvResultNames.Successful)
                            supervisionReport.status = (byte) SvStatusNames.Success;
                        break;
                    case COMMAND_CLASS_SWITCH_MULTILEVEL.SWITCH_MULTILEVEL_STOP_LEVEL_CHANGE.ID:
                        if (ResultForSv == SvResultNames.Successful)
                            supervisionReport.status = (byte) SvStatusNames.Success;
                        break;
                }
                ResultForSv = SvResultNames.Undefined;
            }
        }
    }

    public class MultilevelSwitchSupportData : IBasicMapping
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
        public byte FactoryDefaultSetDuration { get; set; } = 0;
        public byte FactoryDefaultLevelDuration { get; set; } = 50; // from 0 to 99

        // V1+
        public byte LastSetValuePrimary { get; set; } = 0x05; // this value is affected by Set commands and completed Level Change commands
        public byte CurrentValuePrimary { get; set; } = 0x00;
        // V2+
        public byte TargetValuePrimary { get; set; } = 0x00;
        public byte Duration { get; set; } = 0x00;
        // V3+
        public SwitchTypeNames PrimarySwitchType { get; set; } = SwitchTypeNames.DownUp;        // 0x02 = Down (0x00) / Up (0x63/0xFF)
        public SwitchTypeNames SecondarySwitchType { get; set; } = SwitchTypeNames.CloseOpen;   // 0x03 = Close (0x00) / Open (0x63/0xFF)
        public byte CurrentValueSecondary { get; set; } = 0x00;
        public byte TargetValueSecondary { get; set; } = 0x00;
        public byte StepSizeSecondary { get; set; } = 0x00;

        // Basic CC Mapping
        public MultilevelSwitchSupport SupportClassObject;
        bool IBasicMapping.BasicSet(byte value)
        {
            if (SupportClassObject.Version <= 4)
            {
                COMMAND_CLASS_SWITCH_MULTILEVEL_V2.SWITCH_MULTILEVEL_SET multilevelSwitchSetCmd = new COMMAND_CLASS_SWITCH_MULTILEVEL_V2.SWITCH_MULTILEVEL_SET() { };
                multilevelSwitchSetCmd.value = value;
                multilevelSwitchSetCmd.duration = 0x00;
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Basic Set with value 0x{2:X2} accepted.", 
                            SupportClassObject.CommandClassName, SupportClassObject.EndPointName, value);
                return SupportClassObject.HandleMultilevelSwitchSet(multilevelSwitchSetCmd) == SvResultNames.Successful ? true : false;
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Basic Set with value 0x{2:X2} ignored.", 
                        SupportClassObject.CommandClassName, SupportClassObject.EndPointName, value);
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
