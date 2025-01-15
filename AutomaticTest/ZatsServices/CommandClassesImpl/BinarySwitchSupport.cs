// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils;
using Utils.UI.Enums;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;

// ToDo: Supervision Report Status 'Working' for Switch Binary Set

namespace Zats.Services
{
    public class BinarySwitchSupport : CommandClassSupport
    {
        private BinarySwitchSupportData BinarySwitchSupportData { get; set; }

        public readonly string CommandClassName = "Binary Switch CC";
        public string EndPointName = "";
        private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private readonly BackgroundWorker DurationTimer = new BackgroundWorker();
        private string DurationTimerName = "";
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        public byte RemainingDurationTimeSec = 0;

        public BinarySwitchSupport()
            : this(new BinarySwitchSupportData() { })
        { }

        public BinarySwitchSupport(BinarySwitchSupportData binarySwitchSupportData)
            : base(COMMAND_CLASS_SWITCH_BINARY.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 2;
            BinarySwitchSupportData = binarySwitchSupportData;
            BinarySwitchSupportData.SupportClassObject = this;
            LogLevel = BinarySwitchSupportData.LogLevel;

            DurationTimer.DoWork += new DoWorkEventHandler(DurationTimer_DoWork);
            DurationTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(DurationTimer_RunWorkerCompleted);
            DurationTimer.WorkerSupportsCancellation = true;
            DurationTimer.WorkerReportsProgress = false;
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
                DurationTimerName = string.Format("{0}{1}: Duration Simulation Timer", CommandClassName, EndPointName);
                ResultForSv = SvResultNames.Undefined;
                if (isResponseAllowed && command != null && command.Length > 1 && BinarySwitchSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_SWITCH_BINARY.SWITCH_BINARY_SET.ID)
                    {
                        ResultForSv = HandleBinarySwitchSet(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SWITCH_BINARY.SWITCH_BINARY_GET.ID)
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

        public byte[] PrepareReport()
        {
            if (Version == 1)
            {
                COMMAND_CLASS_SWITCH_BINARY.SWITCH_BINARY_REPORT rpt = new COMMAND_CLASS_SWITCH_BINARY.SWITCH_BINARY_REPORT() { };
                rpt.value = BinarySwitchSupportData.State;
                return rpt;
            }
            if (Version == 2)
            {
                COMMAND_CLASS_SWITCH_BINARY_V2.SWITCH_BINARY_REPORT rpt = new COMMAND_CLASS_SWITCH_BINARY_V2.SWITCH_BINARY_REPORT() { };
                rpt.currentValue = BinarySwitchSupportData.State;
                rpt.targetValue = BinarySwitchSupportData.TargetState;
                if (BinarySwitchSupportData.TargetState == BinarySwitchSupportData.State)
                    rpt.duration = 0x00;
                else
                    rpt.duration = BinarySwitchSupportData.Duration;
                return rpt;
            }
            return null;
        }

        public SvResultNames HandleBinarySwitchSet(byte[] command)
        {
            SvResultNames success = SvResultNames.Successful;

            if (Version == 1)
            {
                COMMAND_CLASS_SWITCH_BINARY.SWITCH_BINARY_SET binarySwitchSetCmd = command;

                if (binarySwitchSetCmd.switchValue > 0x63 && binarySwitchSetCmd.switchValue != 0xFF)
                {
                    success = SvResultNames.Failed;
                }
                else
                {
                    if (binarySwitchSetCmd.switchValue == 0x00)
                    {
                        BinarySwitchSupportData.State = 0x00;
                    }
                    else
                    {
                        BinarySwitchSupportData.State = 0xFF;
                    }
                }
            }
            if (Version == 2)
            {
                COMMAND_CLASS_SWITCH_BINARY_V2.SWITCH_BINARY_SET binarySwitchSetCmd = command;

                if (binarySwitchSetCmd.targetValue > 0x63 && binarySwitchSetCmd.targetValue != 0xFF)
                {
                    success = SvResultNames.Failed;
                }
                else
                {
                    if (binarySwitchSetCmd.duration == 0xFF)
                    {
                        BinarySwitchSupportData.Duration = BinarySwitchSupportData.FactoryDefaultDuration;
                    }
                    else
                    {
                        BinarySwitchSupportData.Duration = binarySwitchSetCmd.duration;
                    }

                    DurationTimer_Reset();
                    if (binarySwitchSetCmd.targetValue == 0x00)
                    {
                        if (BinarySwitchSupportData.State != 0x00) // already at target value?
                        {
                            BinarySwitchSupportData.TargetState = 0x00;
                            DurationTimer.RunWorkerAsync();
                        }
                        else
                        {
                            ReportOperationState();
                        }
                    }
                    else
                    {
                        if (BinarySwitchSupportData.State != 0xFF) // already at target value?
                        {
                            BinarySwitchSupportData.TargetState = 0xFF;
                            DurationTimer.RunWorkerAsync();
                        }
                        else
                        {
                            ReportOperationState();
                        }
                    }
                }
            }
            return success;
        }

        private void DurationTimer_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            uint durationTime = CommandClassSupportUtils.DurationToSeconds(BinarySwitchSupportData.Duration);

            RemainingDurationTimeSec = 0;
            if (durationTime > 0 && !BinarySwitchSupportData.IgnoreDuration)
            {
                CommandClassSupportUtils.ReportTimerState(LogLevel, DurationTimerName, ReportTimerStateNames.Started, durationTime);
                for (uint seconds = durationTime; seconds > 0; seconds--)
                {
                    RemainingDurationTimeSec = (byte) seconds;
                    Thread.Sleep(1000);
                    if (bw.CancellationPending)
                    {
                        CommandClassSupportUtils.ReportTimerState(LogLevel, DurationTimerName, ReportTimerStateNames.CancelledRemainSec, RemainingDurationTimeSec);
                        e.Cancel = true;
                        break;
                    }
                }
                if (!e.Cancel)
                {
                    CommandClassSupportUtils.ReportTimerState(LogLevel, DurationTimerName, ReportTimerStateNames.ExpiredSec, durationTime);
                }
            }
            RemainingDurationTimeSec = 0;
            BinarySwitchSupportData.State = BinarySwitchSupportData.TargetState;
            BinarySwitchSupportData.Duration = 0x00;
            ReportOperationState();
        }

        private void DurationTimer_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _resetEvent.Set();
            }
        }
        private void DurationTimer_Reset()
        {
            if (DurationTimer.IsBusy)
            {
                DurationTimer.CancelAsync();
                _resetEvent.WaitOne();
                CommandClassSupportUtils.ReportTimerState(LogLevel, DurationTimerName, ReportTimerStateNames.Reset, 0);
            }
        }

        private void ReportOperationState()
        {
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Current state: 0x{2:X2} - Target state: 0x{3:X2} - Duration: 0x{4:X2}",
                CommandClassName, 
                EndPointName, 
                BinarySwitchSupportData.TargetState, 
                BinarySwitchSupportData.State, 
                BinarySwitchSupportData.Duration);
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
                    case COMMAND_CLASS_SWITCH_BINARY.SWITCH_BINARY_SET.ID:
                        if (ResultForSv == SvResultNames.Successful)
                            supervisionReport.status = (byte) SvStatusNames.Success;
                        break;
                }
                ResultForSv = SvResultNames.Undefined;
            }
        }
    }

    public class BinarySwitchSupportData : IBasicMapping
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
        public byte FactoryDefaultDuration { get; set; } = 3;
        public bool IgnoreDuration { get; set; } = false;

        // V1+
        public byte State { get; set; } = 0x00;
        // V2+
        public byte TargetState { get; set; } = 0x00;
        public byte Duration { get; set; } = 3;

        // Basic CC Mapping
        public BinarySwitchSupport SupportClassObject;
        bool IBasicMapping.BasicSet(byte value)
        {
            if (SupportClassObject.Version <= 2)
            {
                COMMAND_CLASS_SWITCH_BINARY.SWITCH_BINARY_SET binarySwitchSetCmd = new COMMAND_CLASS_SWITCH_BINARY.SWITCH_BINARY_SET() { };
                binarySwitchSetCmd.switchValue = value;
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Basic Set with value 0x{2:X2} accepted.", 
                            SupportClassObject.CommandClassName, SupportClassObject.EndPointName, value);
                return SupportClassObject.HandleBinarySwitchSet(binarySwitchSetCmd) == SvResultNames.Successful ? true : false;
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Basic Set with value 0x{2:X2} ignored.", 
                        SupportClassObject.CommandClassName, SupportClassObject.EndPointName, value);
            return false;
        }
        COMMAND_CLASS_BASIC_V2.BASIC_REPORT IBasicMapping.BasicV2Get()
        {
            COMMAND_CLASS_BASIC_V2.BASIC_REPORT rpt = new COMMAND_CLASS_BASIC_V2.BASIC_REPORT() { };
            rpt.currentValue = State;
            rpt.targetValue = TargetState;
            rpt.duration = CommandClassSupportUtils.SecondsToDuration(SupportClassObject.RemainingDurationTimeSec);
            return rpt;
        }
    }
}
