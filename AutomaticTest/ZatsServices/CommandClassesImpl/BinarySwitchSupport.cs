// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
// SPDX-FileCopyrightText: Z-Wave Alliance https://z-wavealliance.org
using Newtonsoft.Json;
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
using ZWave.Xml.Application;

// ToDo: Supervision Report Status 'Working' for Switch Binary Set

namespace Zats.Services
{
    [CommandClassName("Binary Switch Command Class")]
    [CommandClassId(COMMAND_CLASS_SWITCH_BINARY.ID)]
    [MinVersion(1)]
    [MaxVersion(2)]
    public class BinarySwitchSupport : CommandClassSupport
    {
        private BinarySwitchSupportData BinarySwitchSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        { 
            get { return BinarySwitchSupportData; }
            set { BinarySwitchSupportData = (BinarySwitchSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private readonly BackgroundWorker DurationTimer = new BackgroundWorker();
        private string DurationTimerName = "";
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        public byte RemainingDurationTimeSec = 0;

        private NodeTag CmdSrcNode = NodeTag.Empty;

        public BinarySwitchSupport()
            : this(new BinarySwitchSupportData() { })
        { }

        public BinarySwitchSupport(BinarySwitchSupportData binarySwitchSupportData)
            : base(COMMAND_CLASS_SWITCH_BINARY.ID)
        {
            BinarySwitchSupportData = binarySwitchSupportData;
            BinarySwitchSupportData.SupportClassObject = this;

            DurationTimer.DoWork += new DoWorkEventHandler(DurationTimerDoWork);
            DurationTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(DurationTimerRunWorkerCompleted);
            DurationTimer.WorkerSupportsCancellation = true;
            DurationTimer.WorkerReportsProgress = false;
        }

        public override void StartEmulation()
        {
            BinarySwitchSupportData.SupportClassObject = this;
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
                if (isHandlingAllowed && command != null && command.Length > 1 && BinarySwitchSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_SWITCH_BINARY.SWITCH_BINARY_SET.ID)
                    {
                        ResultForSv = HandleBinarySwitchSetCmd(command, sendSrcNode);
                    }
                    else if (command[1] == COMMAND_CLASS_SWITCH_BINARY.SWITCH_BINARY_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareBinarySwitchReport();
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

        public byte[] PrepareBinarySwitchReport()
        {
            if (Version == 1)
            {
                COMMAND_CLASS_SWITCH_BINARY.SWITCH_BINARY_REPORT rpt = new COMMAND_CLASS_SWITCH_BINARY.SWITCH_BINARY_REPORT() { };
                rpt.value = BinarySwitchSupportData.CurrentState;
                return rpt;
            }
            if (Version == 2)
            {
                COMMAND_CLASS_SWITCH_BINARY_V2.SWITCH_BINARY_REPORT rpt = new COMMAND_CLASS_SWITCH_BINARY_V2.SWITCH_BINARY_REPORT() { };
                rpt.currentValue = BinarySwitchSupportData.CurrentState;
                rpt.targetValue = BinarySwitchSupportData.TargetState;
                if (BinarySwitchSupportData.TargetState == BinarySwitchSupportData.CurrentState)
                {
                    rpt.duration = 0x00;
                }
                else
                {
                    rpt.duration = BinarySwitchSupportData.Duration;
                }
                return rpt;
            }
            return null;
        }

        public SvResultNames HandleBinarySwitchSet(byte targetValue, byte duration, bool updateUI, NodeTag srcNode)
        {
            CmdSrcNode = srcNode;
            SvResultNames success = SvResultNames.Successful;
            DurationTimerName = string.Format("{0} CC @ {1}: Duration Simulation Timer", CommandClassShortName, EndPointName);

            if (Version == 1)
            {
                if (targetValue > 0x63 && targetValue != 0xFF)
                {
                    success = SvResultNames.Failed;
                }
                else
                {
                    bool changeFlag = targetValue != BinarySwitchSupportData.CurrentState;
                    if (targetValue == 0x00)
                    {
                        BinarySwitchSupportData.CurrentState = 0x00;
                    }
                    else
                    {
                        BinarySwitchSupportData.CurrentState = 0xFF;
                    }
                    if (changeFlag)
                    {
                        if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                        {
                            SendUnsolicitedReport(PrepareBinarySwitchReport(), srcNode);
                        }
                        if (updateUI)
                        {
                            SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about the current state
                        }
                    }
                }
            }
            if (Version == 2)
            {
                if (targetValue > 0x63 && targetValue != 0xFF)
                {
                    success = SvResultNames.Failed;
                }
                else
                {
                    DurationTimerCancel();

                    if (duration == 0xFF)
                    {
                        BinarySwitchSupportData.Duration = BinarySwitchSupportData.FactoryDefaultDuration;
                    }
                    else
                    {
                        BinarySwitchSupportData.Duration = duration;
                    }

                    if (targetValue == 0x00)
                    {
                        if (BinarySwitchSupportData.CurrentState != 0x00) // already at target value?
                        {
                            BinarySwitchSupportData.TargetState = 0x00;
                            DurationTimerStart();
                        }
                        else
                        {
                            ReportOperationState();
                        }
                    }
                    else
                    {
                        if (BinarySwitchSupportData.CurrentState != 0xFF) // already at target value?
                        {
                            BinarySwitchSupportData.TargetState = 0xFF;
                            DurationTimerStart();
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

        public SvResultNames HandleBinarySwitchSetCmd(byte[] command, NodeTag srcNode)
        {
            SvResultNames success = SvResultNames.Failed;

            if (Version == 1)
            {
                COMMAND_CLASS_SWITCH_BINARY.SWITCH_BINARY_SET binarySwitchSetCmd = command;

                return HandleBinarySwitchSet(binarySwitchSetCmd.switchValue, 0, true, srcNode);
            }
            if (Version == 2)
            {
                COMMAND_CLASS_SWITCH_BINARY_V2.SWITCH_BINARY_SET binarySwitchSetCmd = command;

                return HandleBinarySwitchSet(binarySwitchSetCmd.targetValue, binarySwitchSetCmd.duration, true, srcNode);
            }
            return success;
        }

        #region Duration Timer
        private void DurationTimerStart()
        {
            BinarySwitchSupportData.CancelPendingSwitchSet += DurationTimerReset;
            DurationTimer.RunWorkerAsync();
        }
        private void DurationTimerDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            uint durationTime = CommandClassSupportUtils.DurationToSeconds(BinarySwitchSupportData.Duration);
            bool changeFlag = BinarySwitchSupportData.TargetState != BinarySwitchSupportData.CurrentState;

            RemainingDurationTimeSec = 0;
            if (durationTime > 0 && !BinarySwitchSupportData.IgnoreDuration)
            {
                CommandClassSupportUtils.ReportTimerState(BinarySwitchSupportData.LogLevel, 
                            DurationTimerName, ReportTimerStateNames.Started, durationTime);
                for (uint seconds = durationTime; seconds > 0; seconds--)
                {
                    RemainingDurationTimeSec = (byte) seconds;
                    Thread.Sleep(1000);
                    if (Result.State == ActionStates.Cancelled)
                    {
                        DurationTimerCancel();
                    }
                    if (bw.CancellationPending)
                    {
                        CommandClassSupportUtils.ReportTimerState(BinarySwitchSupportData.LogLevel, 
                                    DurationTimerName, ReportTimerStateNames.CancelledRemainSec, RemainingDurationTimeSec);
                        e.Cancel = true;
                        break;
                    }
                }
                if (!e.Cancel)
                {
                    CommandClassSupportUtils.ReportTimerState(BinarySwitchSupportData.LogLevel, 
                                DurationTimerName, ReportTimerStateNames.ExpiredSec, durationTime);
                }
            }
            RemainingDurationTimeSec = 0;
            BinarySwitchSupportData.CurrentState = BinarySwitchSupportData.TargetState;
            BinarySwitchSupportData.Duration = 0x00;
            ReportOperationState();

            if (changeFlag)
            {
                if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                {
                    DeviceRestart(CmdSrcNode);
                    SendUnsolicitedReport(PrepareBinarySwitchReport(), CmdSrcNode);
                    SupportDelegates?.DeviceSleep(this, ReportWakeupTimeout);
                }
                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE)
            }
        }
        private void DurationTimerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _resetEvent.Set();
            }
        }
        private void DurationTimerCancel()
        {
            BinarySwitchSupportData.CancelPendingSwitchSet?.Invoke();
            BinarySwitchSupportData.CancelPendingSwitchSet = null;
        }

        // This method MUST only be called via <CommandClass>SupportData.CancelPending<CommandName>.Invoke();
        private void DurationTimerReset()
        {
            if (DurationTimer.IsBusy)
            {
                CommandClassSupportUtils.ReportTimerState(BinarySwitchSupportData.LogLevel,
                            DurationTimerName, ReportTimerStateNames.Reset, 0);
                DurationTimer.CancelAsync();
                _resetEvent.WaitOne();
            }
        }
        #endregion // Duration Timer

        private void ReportOperationState()
        {
            CommandClassSupportUtils.ReportAddLog(BinarySwitchSupportData.LogLevel, 
                        "{0} CC @ {1}: Current state: 0x{2:X2} - Target state: 0x{3:X2} - Duration: 0x{4:X2}",
                        CommandClassShortName, EndPointName, 
                        BinarySwitchSupportData.TargetState, 
                        BinarySwitchSupportData.CurrentState, 
                        BinarySwitchSupportData.Duration);
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, 
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            {
                if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != CommandClassId)
                {
                    supervisionReport.status = (byte) SvStatusNames.NoSupport;
                    return;
                }

                supervisionReport.status = (byte) SvStatusNames.Fail;  // FAIL as default
                switch (supervisionGet.encapsulatedCommand[1])
                {
                    case COMMAND_CLASS_SWITCH_BINARY.SWITCH_BINARY_SET.ID:
                        if (ResultForSv == SvResultNames.Successful)
                        {
                            supervisionReport.status = (byte) SvStatusNames.Success;
                        }
                        break;
                }
                ResultForSv = SvResultNames.Undefined;
            }
        }


        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is BinarySwitchSupportData newData)
            {
                if (BinarySwitchSupportData.CurrentState != newData.CurrentState
                 || BinarySwitchSupportData.TargetState != newData.TargetState
                 || BinarySwitchSupportData.TargetState != newData.CurrentState)
                {
                    if (Version == 1)
                    {
                        HandleBinarySwitchSet(newData.CurrentState, newData.Duration, false, NodeTag.Empty);
                    }
                    if (Version >= 2)
                    {
                        BinarySwitchSupportData.CurrentState = newData.CurrentState;
                        HandleBinarySwitchSet(newData.TargetState, newData.Duration, false, NodeTag.Empty);
                    }
                }
                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about the current state
            }
        }
    }

    public class BinarySwitchSupportData : CommandClassSupportData, IBasicMapping
    {
        // Internals accessible from TestCases or EDE

        [CustomDataParameter(2, "Factory Default Duration", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Int_0_254, EditType.Config)]
        public byte FactoryDefaultDuration { get; set; } = 3;

        // Probably not needed in UI
        // [CustomDataParameter(1, "Ignore Duration", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config)]
        public bool IgnoreDuration { get; set; } = false;

        [JsonIgnore]
        public CancelPendingOperationDelegate CancelPendingSwitchSet = null;

        // V1+
        [ParameterSeparator(1, "Binary Switch Report")]
        [XmlDataParameter(1, 1, 0x25, 0x03, new byte[] { 0x00 }, EditType.Config | EditType.Edit)]
        public byte State
        {
            get { return CurrentState; }
            set { CurrentState = value; }
        }
        // V2+
        [XmlDataParameter(2, 0x25, 0x03, new byte[] { 0x00 }, EditType.Config/* | EditType.Edit*/)]
        public byte CurrentState { get; set; } = 0x00;

        [XmlDataParameter(2, 0x25, 0x03, new byte[] { 0x01 }, EditType.Config | EditType.Edit)]
        public byte TargetState { get; set; } = 0x00;

        [XmlDataParameter(2, 0x25, 0x03, new byte[] { 0x02 }, EditType.Config | EditType.Edit)]
        public byte Duration { get; set; } = 3;

        // Basic CC Mapping
        [JsonIgnore]
        public BinarySwitchSupport SupportClassObject;
        bool IBasicMapping.BasicSet(byte value, NodeTag srcNode)
        {
            if (SupportClassObject.Version <= 2)
            {
                COMMAND_CLASS_SWITCH_BINARY.SWITCH_BINARY_SET binarySwitchSetCmd = new COMMAND_CLASS_SWITCH_BINARY.SWITCH_BINARY_SET() { };
                binarySwitchSetCmd.switchValue = value;
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0} CC @ {1}: Basic Set with value 0x{2:X2} accepted.",
                            SupportClassObject.CommandClassShortName, SupportClassObject.EndPointName, value);
                return SupportClassObject.HandleBinarySwitchSetCmd(binarySwitchSetCmd, srcNode) == SvResultNames.Successful ? true : false;
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0} CC @ {1}: Basic Set with value 0x{2:X2} ignored.",
                        SupportClassObject.CommandClassShortName, SupportClassObject.EndPointName, value);
            return false;
        }
        COMMAND_CLASS_BASIC_V2.BASIC_REPORT IBasicMapping.BasicV2Get()
        {
            COMMAND_CLASS_BASIC_V2.BASIC_REPORT rpt = new COMMAND_CLASS_BASIC_V2.BASIC_REPORT() { };
            rpt.currentValue = CurrentState;
            rpt.targetValue = TargetState;
            rpt.duration = CommandClassSupportUtils.SecondsToDuration(SupportClassObject.RemainingDurationTimeSec);
            return rpt;
        }
    }
}
