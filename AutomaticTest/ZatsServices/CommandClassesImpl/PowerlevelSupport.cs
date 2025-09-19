// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Z-Wave Alliance https://z-wavealliance.org
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Utils;
using Utils.UI.Enums;
using Zats.Services.CommandClassesImpl;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;

// ToDo: Supervision Report Status 'Working' for SetbackSet (optional)

namespace Zats.Services
{
    [CommandClassName("Powerlevel Command Class")]
    [CommandClassId(COMMAND_CLASS_POWERLEVEL.ID)]
    [MinVersion(1)]
    [MaxVersion(1)]
    public class PowerlevelSupport : CommandClassSupport
    {
        private PowerlevelSupportData PowerlevelSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return PowerlevelSupportData; }
            set { PowerlevelSupportData = (PowerlevelSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private readonly BackgroundWorker TimeoutTimer = new BackgroundWorker();
        private string TimeoutTimerName = "";
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        public byte RemainingLowTimeSec = 0;

        private NodeTag CmdSrcNode = NodeTag.Empty;

        public PowerlevelSupport()
            : this(new PowerlevelSupportData() { })
        { }

        public PowerlevelSupport(PowerlevelSupportData powerlevelSupportData)
            : base(COMMAND_CLASS_POWERLEVEL.ID)
        {
            PowerlevelSupportData = powerlevelSupportData;

            TimeoutTimer.DoWork += new DoWorkEventHandler(TimeoutTimerDoWork);
            TimeoutTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(TimeoutTimerRunWorkerCompleted);
            TimeoutTimer.WorkerSupportsCancellation = true;
            TimeoutTimer.WorkerReportsProgress = false;
        }

        /*public override void StartEmulation()
        {
            //PowerlevelSupportData.SupportClassObject = this;
        }*/

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
                if (isHandlingAllowed && command != null && command.Length > 1 && PowerlevelSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_POWERLEVEL.POWERLEVEL_GET.ID && !IsSupervised())
                    {
                        rpt = PreparePowerlevelReport();
                    }
                    else if (command[1] == COMMAND_CLASS_POWERLEVEL.POWERLEVEL_TEST_NODE_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareTestNodeReport();
                    }
                    else if (command[1] == COMMAND_CLASS_POWERLEVEL.POWERLEVEL_SET.ID)
                    {
                        ResultForSv = HandlePowerlevelSetCmd(command, sendSrcNode);
                    }
                    else if (command[1] == COMMAND_CLASS_POWERLEVEL.POWERLEVEL_TEST_NODE_SET.ID)
                    {
                        // ResultForSv = HandleTestNodeSetCmd(command);
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

        private byte[] PreparePowerlevelReport()
        {
            if (Version >= 1)
            {
                COMMAND_CLASS_POWERLEVEL.POWERLEVEL_REPORT rpt = new COMMAND_CLASS_POWERLEVEL.POWERLEVEL_REPORT();
                rpt.powerLevel = PowerlevelSupportData.CurrentPowerlevel;
                rpt.timeout = RemainingLowTimeSec;
                return rpt;
            }
            return null;
        }

        private byte[] PrepareTestNodeReport()
        {
            if (Version >= 1)
            {
                COMMAND_CLASS_POWERLEVEL.POWERLEVEL_TEST_NODE_REPORT rpt = new COMMAND_CLASS_POWERLEVEL.POWERLEVEL_TEST_NODE_REPORT();
                rpt.testNodeid = PowerlevelSupportData.TestNodeId;
                rpt.statusOfOperation = (byte) PowerlevelSupportData.SimulatedTestResult;
                rpt.testFrameCount[0] = (byte) (PowerlevelSupportData.TestFramesAcked / 256);
                rpt.testFrameCount[1] = (byte) (PowerlevelSupportData.TestFramesAcked % 256);
                return rpt;
            }
            return null;
        }

        public SvResultNames HandlePowerlevelSet(byte powerlevel, byte timeout, bool updateUI, NodeTag srcNode)
        {
            CmdSrcNode = srcNode;
            if (Version >= 1 && powerlevel <= 0x09)
            {
                TimeoutTimerName = string.Format("{0} CC @ {1}: Timeout Simulation Timer", CommandClassShortName, EndPointName);
                TimeoutTimerCancel();
                if (powerlevel == 0x00)  // NormalPower
                {
                    bool changeFlag = PowerlevelSupportData.CurrentPowerlevel != 0x00 ? true : false;
                    PowerlevelSupportData.CurrentPowerlevel = 0x00;
                    PowerlevelSupportData.Timeout = 0x00;
                    RemainingLowTimeSec = 0;
                    if (changeFlag && ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    {
                        SendUnsolicitedReport(PreparePowerlevelReport(), srcNode);
                    }
                    if (changeFlag && updateUI)
                    {
                        SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about the current state
                    }
                    return SvResultNames.Successful;
                }
                if (powerlevel > 0x00 && powerlevel <= 0x09 && timeout > 0) // minusXdBm
                {
                    PowerlevelSupportData.CurrentPowerlevel = powerlevel;
                    PowerlevelSupportData.Timeout = timeout;
                    TimeoutTimerStart();
                    return SvResultNames.Successful;
                }
            }
            return SvResultNames.Failed;
        }
        public SvResultNames HandlePowerlevelSetCmd(byte[] command, NodeTag srcNode)
        {
            if (Version >= 1)
            {
                COMMAND_CLASS_POWERLEVEL.POWERLEVEL_SET setCommand = command;
                return HandlePowerlevelSet(setCommand.powerLevel, setCommand.timeout, true, srcNode);
            }
            return SvResultNames.Failed;
        }

        public SvResultNames HandleTestNodeSet(byte nodeId, byte powerlevel, ushort framesToSend, ushort framesAcked, PowerlevelTestNodeStatusNames result, bool updateUI, NodeTag srcNode)
        {
            if (Version >= 1 && powerlevel <= 0x09 && (byte) result <= 0x02)
            {
                PowerlevelSupportData.TestNodeId = nodeId;
                PowerlevelSupportData.TestPowerlevel = powerlevel;
                PowerlevelSupportData.TestFramesToSend = framesToSend;
                PowerlevelSupportData.TestFramesAcked = framesAcked;
                PowerlevelSupportData.SimulatedTestResult = result;
                if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                {
                    SendUnsolicitedReport(PrepareTestNodeReport(), srcNode);
                }
                if (updateUI)
                {
                    SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about the current state
                }
                return SvResultNames.Successful;
            }
            return SvResultNames.Failed;
        }
        public SvResultNames HandleTestNodeSetCmd(byte[] command, NodeTag srcNode)
        {
            if (Version >= 1)
            {
                COMMAND_CLASS_POWERLEVEL.POWERLEVEL_TEST_NODE_SET setCommand = command;
                return HandleTestNodeSet(setCommand.testNodeid, setCommand.powerLevel, 
                    (ushort) (setCommand.testFrameCount[0] * 256 + setCommand.testFrameCount[1]),
                    (ushort) (setCommand.testFrameCount[0] * 256 + setCommand.testFrameCount[1]),
                    PowerlevelTestNodeStatusNames.Success, true, srcNode);
            }
            return SvResultNames.Failed;
        }

        #region Powerlevel Timeout Timer
        private void TimeoutTimerStart()
        {
            PowerlevelSupportData.CancelPendingPowerlevelSet += TimeoutTimerReset;
            TimeoutTimer.RunWorkerAsync();
        }
        private void TimeoutTimerDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            byte durationTime = PowerlevelSupportData.Timeout;

            if (durationTime > 0)
            {
                CommandClassSupportUtils.ReportTimerState(PowerlevelSupportData.LogLevel,
                            TimeoutTimerName, ReportTimerStateNames.Started, durationTime);
                ReportOperationState();

                RemainingLowTimeSec = durationTime;
                if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                {
                    //SupportDelegates?.DeviceRestart(this);
                    SendUnsolicitedReport(PreparePowerlevelReport(), CmdSrcNode);
                    //SupportDelegates?.DeviceSleep(this, true);
                }
                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state

                for (; RemainingLowTimeSec > 0; RemainingLowTimeSec--)
                {
                    Thread.Sleep(1000);
                    if (Result.State == ActionStates.Cancelled)
                    {
                        TimeoutTimerCancel();
                    }
                    if (bw.CancellationPending)
                    {
                        CommandClassSupportUtils.ReportTimerState(PowerlevelSupportData.LogLevel,
                                    TimeoutTimerName, ReportTimerStateNames.CancelledRemainSec, RemainingLowTimeSec);
                        e.Cancel = true;
                        break;
                    }
                }
                if (!e.Cancel)
                {
                    CommandClassSupportUtils.ReportTimerState(PowerlevelSupportData.LogLevel,
                                TimeoutTimerName, ReportTimerStateNames.ExpiredSec, durationTime);
                }
                RemainingLowTimeSec = 0;
                PowerlevelSupportData.CurrentPowerlevel = 0x00;
                PowerlevelSupportData.Timeout = 0x00;
                ReportOperationState();

                if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                {
                    DeviceRestart(CmdSrcNode);
                    SendUnsolicitedReport(PreparePowerlevelReport(), CmdSrcNode);
                    SupportDelegates?.DeviceSleep(this, ReportWakeupTimeout);
                }
                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
            }
        }
        private void TimeoutTimerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _resetEvent.Set();
            }
        }
        private void TimeoutTimerCancel()
        {
            PowerlevelSupportData.CancelPendingPowerlevelSet?.Invoke();
            PowerlevelSupportData.CancelPendingPowerlevelSet = null;
        }

        // This method MUST only be called via <CommandClass>SupportData.CancelPending<CommandName>.Invoke();
        private void TimeoutTimerReset()
        {
            if (TimeoutTimer.IsBusy)
            {
                CommandClassSupportUtils.ReportTimerState(PowerlevelSupportData.LogLevel,
                            TimeoutTimerName, ReportTimerStateNames.Reset, 0);
                TimeoutTimer.CancelAsync();
                _resetEvent.WaitOne();
            }
        }
        #endregion // Duration Timer

        private void ReportOperationState()
        {
            CommandClassSupportUtils.ReportAddLog(PowerlevelSupportData.LogLevel,
                        "{0} CC @ {1}: Current power level: 0x{2:X2} - Remaining time: 0x{3:X2}",
                        CommandClassShortName, EndPointName,
                        PowerlevelSupportData.CurrentPowerlevel,
                        PowerlevelSupportData.Timeout);
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
                case COMMAND_CLASS_POWERLEVEL.POWERLEVEL_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    }
                    break;
                case COMMAND_CLASS_POWERLEVEL.POWERLEVEL_TEST_NODE_SET.ID:
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
            if (data is PowerlevelSupportData newData)
            {
                if (PowerlevelSupportData.CurrentPowerlevel!= newData.CurrentPowerlevel
                 || PowerlevelSupportData.Timeout != newData.Timeout)
                {
                    HandlePowerlevelSet(newData.CurrentPowerlevel, newData.Timeout, false, NodeTag.Empty);    // report and inform (EDE) -> HandlePowerlevelSet()
                }

                if (PowerlevelSupportData.TestPowerlevel!= newData.TestPowerlevel
                 || PowerlevelSupportData.TestNodeId != newData.TestNodeId
                 || PowerlevelSupportData.TestFramesToSend != newData.TestFramesToSend
                 || PowerlevelSupportData.TestFramesAcked != newData.TestFramesAcked
                 || PowerlevelSupportData.SimulatedTestResult != newData.SimulatedTestResult)
                {
                    HandleTestNodeSet(newData.TestNodeId, newData.CurrentPowerlevel, newData.TestFramesToSend,
                                      newData.TestFramesAcked, newData.SimulatedTestResult, false, NodeTag.Empty);    // report and inform (EDE) -> HandleTestNodeSet()
                }

                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class PowerlevelSupportData : CommandClassSupportData
    {
        // Internals accessible from TestCases or EDE
        [JsonIgnore]
        public CancelPendingOperationDelegate CancelPendingPowerlevelSet = null;

        // V1+ Powerlevel Report
        [ParameterSeparator(1, "Powerlevel Report")]
        [CustomDataParameter(1, "Powerlevel (0..9)", UiControl.TextBox, InputType.Int_0_9, EditType.Config | EditType.Edit)]
        public byte CurrentPowerlevel { get; set; } = 0;
        [CustomDataParameter(1, "Timeout (sec)", UiControl.TextBox, InputType.Int_0_255, EditType.Config | EditType.Edit)]
        public byte Timeout { get; set; } = 0;

        // V1+ Powerlevel Test Node Report
        [ParameterSeparator(1, "Test Node Report")]
        [CustomDataParameter(1, "Test Powerlevel (0..9)", UiControl.TextBox, InputType.Int_0_9, EditType.Config | EditType.Edit)]
        public byte TestPowerlevel { get; set; } = 0;
        [CustomDataParameter(1, "Test Node Id", UiControl.TextBox, InputType.Int_1_255, EditType.Config | EditType.Edit)]
        public byte TestNodeId { get; set; } = 0;
        [CustomDataParameter(1, "Frames To Send", UiControl.TextBox, InputType.UShort, EditType.Config | EditType.Edit)]
        public ushort TestFramesToSend { get; set; } = 0;
        [CustomDataParameter(1, "Frames Acked", UiControl.TextBox, InputType.UShort, EditType.Config | EditType.Edit)]
        public ushort TestFramesAcked { get; set; } = 0;
        [CustomDataParameter(1, "Test Result", CommandClassesImpl.UiControl.ListComboBox, CommandClassesImpl.InputType.Enum, EditType.Config /*| EditType.View*/ | EditType.Edit, typeof(PowerlevelTestNodeStatusNames))]
        public PowerlevelTestNodeStatusNames SimulatedTestResult { get; set; } = PowerlevelTestNodeStatusNames.Failed;
    }
    public enum PowerlevelTestNodeStatusNames
    {
        [Description("0x00 Failed")]
        Failed = 0x00,
        [Description("0x01 Success")]
        Success = 0x01,
        [Description("0x02 In progress")]
        InProgress = 0x02,
    }
}
