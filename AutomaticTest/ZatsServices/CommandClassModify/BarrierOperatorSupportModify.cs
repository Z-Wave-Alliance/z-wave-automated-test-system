// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
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

// ToDo: Supervision Report Status 'Working' for Barrier Operator Set

namespace Zats.Services
{
    public class BarrierOperatorSupportModify : CommandClassSupport
    {
        private BarrierOperatorSupportData BarrierOperatorSupportData { get; set; }

        public readonly string CommandClassName = "Barrier Operator CC";
        public string EndPointName = "";
        private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private readonly BackgroundWorker TransitionTimer = new BackgroundWorker();
        private string TransitionTimerName = "";
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        public BarrierOperatorSupportModify()
            : this(new BarrierOperatorSupportData() { })
        { }

        public BarrierOperatorSupportModify(BarrierOperatorSupportData barrierOperatorSupportData)
            : base(COMMAND_CLASS_BARRIER_OPERATOR.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 1;
            BarrierOperatorSupportData = barrierOperatorSupportData;
            BarrierOperatorSupportData.SupportClassObject = barrierOperatorSupportData.SupportClassObject;
            LogLevel = BarrierOperatorSupportData.LogLevel;

            TransitionTimer.DoWork += new DoWorkEventHandler(TransitionTimer_DoWork);
            TransitionTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Timer_RunWorkerCompleted);
            TransitionTimer.WorkerSupportsCancellation = true;
            TransitionTimer.WorkerReportsProgress = false;
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

                EndPointName = ReceivedAchData.DstNode.EndPointId == 0 ? " @ Root" : string.Format(" @ EP {0}", ReceivedAchData.DstNode.EndPointId);
                TransitionTimerName = string.Format("{0}{1}: Transition Simulation", CommandClassName, EndPointName);
                ResultForSv = SvResultNames.Undefined;
                if (isHandlingAllowed && command != null && command.Length > 1 && BarrierOperatorSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_GET.ID)
                    {
                        rpt = PrepareBarrierOperatorReport();
                    }
                    else if (command[1] == COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_SET.ID)
                    {
                        ResultForSv = HandleBarrierOperatorSet(command);
                    }
                    else if (command[1] == COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_SIGNAL_SUPPORTED_GET.ID)
                    {
                        rpt = PrepareBarrierOperatorSignalSupportedReport();
                    }
                    else if (command[1] == COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_EVENT_SIGNALING_GET.ID)
                    {
                        rpt = PrepareBarrierOperatorSignalReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_EVENT_SIGNAL_SET.ID)
                    {
                        ResultForSv = HandleBarrierOperatorSignalSet(command);
                    }

                    if (rpt != null)
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
            COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_SIGNAL_SUPPORTED_REPORT rpt = new COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_SIGNAL_SUPPORTED_REPORT();
            //{ bitMask = CommandClassSupportUtils.CreateBitmaskFromList(BarrierOperatorSupportData.SupportedSubsystemTypes, 1) };

            //This helper function is buggy, but I do not dear change it since is being used widely and by cert.
            //rpt.bitMask = CommandClassSupportUtils.CreateBitmaskFromList(BarrierOperatorSupportData.SupportedSubsystemTypes, 1);

            //Hardcoding it for now....
            rpt.bitMask = new List<byte>() { 0x03 };

            return rpt;
        }
        private byte[] PrepareBarrierOperatorSignalReport(byte[] command)
        {
            COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_EVENT_SIGNALING_GET getCmd = command;
            COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_EVENT_SIGNALING_REPORT rpt = new COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_EVENT_SIGNALING_REPORT() { };

            try
            {
                rpt.subsystemState = (byte)BarrierOperatorSupportData.SupportedSubsystemTypes.First(x => (byte)x.SubsystemType == getCmd.subsystemType.Value).SubsystemState;
            }
            catch
            { 
                rpt.subsystemState = 0xFE;
            }
            
            rpt.subsystemType = getCmd.subsystemType;
            return rpt;
        }

        public SvResultNames HandleBarrierOperatorSignalSet(byte[] command)
        {
            COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_EVENT_SIGNAL_SET setCmd = command;
            BarrierOperatorSubsystemTypes requestedSubsystemType  = (BarrierOperatorSubsystemTypes)  (byte) setCmd.subsystemType;
            BarrierOperatorSubsystemStateNames requestedSubsystemState = (BarrierOperatorSubsystemStateNames) (byte) setCmd.subsystemState;

            if (requestedSubsystemState == BarrierOperatorSubsystemStateNames.Off || requestedSubsystemState == BarrierOperatorSubsystemStateNames.On)
            { 
                if (BarrierOperatorSupportData.SupportedSubsystemTypes.FirstOrDefault(x => x.SubsystemType == requestedSubsystemType) is BarrierOperatorSubsystem requestedSubsystem)
                {
                    requestedSubsystem.SubsystemState = requestedSubsystemState;
                    return SvResultNames.Successful;
                }
            }

            return SvResultNames.Failed;
        }

        public byte[] PrepareBarrierOperatorReport()
        {
            COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_REPORT rpt = new COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_REPORT() { };
          //ReportBarrierState(BarrierOperatorSupportData.CurrentState);
            rpt.state = (byte) BarrierOperatorSupportData.CurrentState;
            return rpt;
        }

        public SvResultNames HandleBarrierOperatorSet(byte[] command)
        {
            COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_SET barrierOperatorSetCmd = command;
            BarrierOperatorTargetStateNames requestedOperation = (BarrierOperatorTargetStateNames) (byte) barrierOperatorSetCmd.targetValue;

            ReportBarrierState(BarrierOperatorSupportData.CurrentState);

            if (requestedOperation == BarrierOperatorTargetStateNames.TargetClose)
            {
                if (BarrierOperatorSupportData.CurrentState == BarrierOperatorStateNames.Open)
                {
                    BarrierOperatorSupportData.CurrentState = BarrierOperatorStateNames.Closing;
                    BarrierOperatorSupportData.TargetState = BarrierOperatorStateNames.Closed;
                    TransitionTimer.RunWorkerAsync();
                    return SvResultNames.Successful;
                }
            }
            if (requestedOperation == BarrierOperatorTargetStateNames.TargetOpen)
            {
                if (BarrierOperatorSupportData.CurrentState == BarrierOperatorStateNames.Stopped
                 || BarrierOperatorSupportData.CurrentState == BarrierOperatorStateNames.Closed)
                {
                    BarrierOperatorSupportData.CurrentState = BarrierOperatorStateNames.Opening;
                    BarrierOperatorSupportData.TargetState = BarrierOperatorStateNames.Open;
                    ReportBarrierState(BarrierOperatorStateNames.Opening);
                    TransitionTimer.RunWorkerAsync();
                    return SvResultNames.Successful;
                }
                if (BarrierOperatorSupportData.CurrentState == BarrierOperatorStateNames.Closing)
                {
                    TransitionTimer_Reset();
                    BarrierOperatorSupportData.CurrentState = BarrierOperatorStateNames.Stopped;
                    BarrierOperatorSupportData.TargetState = BarrierOperatorStateNames.Stopped;
                    ReportBarrierState(BarrierOperatorStateNames.Stopped);
                    return SvResultNames.Successful;
                }
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, CommandClassName + EndPointName + ": Operator Set Command '{0}' (0x{1:X2}) ignored. Current state: '{2}'.",
                        requestedOperation, (byte) requestedOperation, BarrierOperatorSupportData.CurrentState);
            return SvResultNames.Failed;
        }
        private void TransitionTimer_DoWork(object sender, DoWorkEventArgs e)
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
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "{0} is running. Position is 0x{1:X2} = {1} % opened.", 
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
            }
        }
        private void Timer_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _resetEvent.Set();
            }
        }
        private void TransitionTimer_Reset()
        {
            if (TransitionTimer.IsBusy)
            {
                TransitionTimer.CancelAsync();
                _resetEvent.WaitOne();
                ReportOperationProgress(TransitionTimerName, ReportTimerStateNames.Reset, 0);
            }
        }
        private void ReportOperationProgress(string timerName, ReportTimerStateNames timerState, int timerValue)
        {
            CommandClassSupportUtils.ReportTimerState(LogLevel, timerName, timerState, timerValue);
        }
        private void ReportBarrierState(BarrierOperatorStateNames stateName)
        {
            CommandClassSupportUtils.ReportAddLog(LogLevel, CommandClassName + EndPointName + ": Current state is '{0}'.", stateName);
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
                case COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
                case COMMAND_CLASS_BARRIER_OPERATOR.BARRIER_OPERATOR_EVENT_SIGNAL_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }

        // Satisfy abstract members
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return null; }
            set {; }
        }

        public override void UpdateSupportData(CommandClassSupportData data)
        {

        }
    }
}
