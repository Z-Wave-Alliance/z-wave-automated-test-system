// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
// SPDX-FileCopyrightText: 2025 Z-Wave Alliance https://z-wavealliance.org
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using Utils;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using ZWave.Xml.Application;
using Renci.SshNet.Common;
using ZWave.Xml.FrameHeader;
using static UicApplication.Clusters.SmartStart.List;
using ZatsServices_netcore.CommandClassesImpl.Definitions.Interfaces;
using Zats.Services.CommandClassesImpl;

// ToDo: Supervision Report Status 'Working'

namespace Zats.Services
{
    [CommandClassName("Window Covering Command Class")]
    [CommandClassId(COMMAND_CLASS_WINDOW_COVERING.ID)]
    [MinVersion(1)]
    [MaxVersion(1)]
    public class WindowCoveringSupport : CommandClassSupport
    {
        private WindowCoveringSupportData WindowCoveringSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return WindowCoveringSupportData; }
            set { WindowCoveringSupportData = (WindowCoveringSupportData)value; }
        }

        public MultilevelSwitchSupportData MultilevelSwitchSupportData
        {
            get { return WindowCoveringSupportData.MultilevelSwitchSupportData; }
            set { WindowCoveringSupportData.MultilevelSwitchSupportData = value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private uint TicksElapsed = 0;
        private const uint TickerPerSec = 10;

        public readonly byte OnValue  = (byte) WindowCoveringPositionNames.Open;    // Value of 100% = ON  = 0x63
        public readonly byte OffValue = (byte) WindowCoveringPositionNames.Closed;  // Value of   0% = OFF = 0x00

        private readonly BackgroundWorker TransitionTimer = new BackgroundWorker();
        private string TransitionTimerName = "";
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        public WindowCoveringSupport()
            : this(new WindowCoveringSupportData() { }, new MultilevelSwitchSupportData() { })
        { }

        public WindowCoveringSupport(WindowCoveringSupportData windowCoveringSupportData, MultilevelSwitchSupportData multilevelSwitchSupportData)
            : base(COMMAND_CLASS_WINDOW_COVERING.ID)
        {
            WindowCoveringSupportData = windowCoveringSupportData;
            WindowCoveringSupportData.SupportClassObject = this;
            WindowCoveringSupportData.MultilevelSwitchSupportData = multilevelSwitchSupportData;

            TransitionTimer.DoWork += new DoWorkEventHandler(TransitionTimerDoWork);
            TransitionTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(TransitionTimerRunWorkerCompleted);
            TransitionTimer.WorkerSupportsCancellation = true;
            TransitionTimer.WorkerReportsProgress = false;
            TransitionTimerStart(); // the Transition Timer runs infinitely or until Result.State == ActionStates.Cancelled
        }

        public override void StartEmulation()
        {
            WindowCoveringSupportData.SupportClassObject = this;
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

                TransitionTimerName = string.Format("{0} CC @ {1}: Transition Timer", CommandClassShortName, EndPointName);
                ResultForSv = SvResultNames.Undefined;
                if (isHandlingAllowed && command != null && command.Length > 1 && WindowCoveringSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_SUPPORTED_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareSupportedReport();
                    }
                    else if (command[1] == COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_SET.ID)
                    {
                        ResultForSv = HandleWindowCoveringSetCmd(command, sendSrcNode);
                    }
                    else if (command[1] == COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareWindowCoveringReportCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_START_LEVEL_CHANGE.ID)
                    {
                        ResultForSv = HandleStartLevelChangeCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_STOP_LEVEL_CHANGE.ID)
                    {
                        ResultForSv = HandleStopLevelChangeCmd(command);
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

        private byte[] PrepareSupportedReport()
        {
            COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_SUPPORTED_REPORT rpt = new COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_SUPPORTED_REPORT() { };
            List<WindowCoveringParameterNames> supportedParameters = new List<WindowCoveringParameterNames> { };
            foreach (WindowCoveringParameter param in WindowCoveringSupportData.Parameters)
            {
                supportedParameters.Add(param.ParamId);
            }
            rpt.parameterMask = CommandClassSupportUtils.CreateBitmaskFromList(supportedParameters, 0);
            rpt.properties1.numberOfParameterMaskBytes = (byte) rpt.parameterMask.Count;
            return rpt;
        }

        private byte[] PrepareWindowCoveringReport(byte requestedParamId)
        {
            WindowCoveringParameter requestedParam = WindowCoveringSupportData.Parameters.Find(param => (byte)param.ParamId == requestedParamId);
            if (requestedParam != null)
            {
                COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_REPORT rpt = new COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_REPORT() { };
                rpt.parameterId = requestedParamId;
                //CommandClassSupportUtils.ReportAddLog(WindowCoveringSupportData.LogLevel,
                //"{0} CC @ {1}: Report: StepWidth is {2}.", CommandClassName, EndPointName, requestedParam.StepWidthF);
                if ((requestedParamId % 2) == 0 && requestedParam.StepWidthF == 0)
                {
                    rpt.currentValue = 0x00;
                    rpt.targetValue = 0x00;
                    rpt.duration = 0xFE;
                }
                else
                {
                    rpt.currentValue = requestedParam.CurrentValueB;
                    rpt.targetValue = requestedParam.TargetValueB;
                    rpt.duration = CommandClassSupportUtils.SecondsToDuration((uint)Math.Abs(rpt.targetValue - rpt.currentValue) * requestedParam.DurationOffOnSec / OnValue);
                }
                return rpt;
            }
            return null;
        }
        private byte[] PrepareWindowCoveringReportCmd(byte[] command)
        {
            byte requestedParamId = ((COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_GET)command).parameterId;
            return PrepareWindowCoveringReport(requestedParamId);
        }

        public SvResultNames HandleSingleWindowCoveringParam(WindowCoveringParameterNames paramId, byte newValue, byte durationTransitionHex, bool updateUI, NodeTag srcNode)
        {
            SvResultNames success = SvResultNames.Successful;

            WindowCoveringParameter requestedParam = WindowCoveringSupportData.Parameters.Find(param => param.ParamId == paramId);
            if (requestedParam != null && ((byte) paramId % 2) != 0)     // Ignore even Movement parameters
            {
                byte targetValueB = newValue;
                byte currentValueB = requestedParam.CurrentValueB;
                uint durationTransitionSec = CommandClassSupportUtils.DurationToSeconds(durationTransitionHex);

                if (targetValueB > OnValue)
                {
                    CommandClassSupportUtils.ReportAddLog(WindowCoveringSupportData.LogLevel,
                                "{0} CC @ {1}: Invalid value for Parameter ID {2} in Set command.",
                                CommandClassShortName, EndPointName, targetValueB, paramId);
                    success = SvResultNames.Failed;
                }
                else
                {
                    // Cancel a possibly running transition
                    requestedParam.TargetValueB = requestedParam.CurrentValueB;
                    requestedParam.StepWidthF = 0;
                    requestedParam.DurationTransitionHex = 0;
                    requestedParam.DurationOffOnSec = 0;
                    requestedParam.CmdSrcNode = NodeTag.Empty;

                    if (targetValueB != currentValueB)
                    {
                        if (durationTransitionSec == 0)  // Instantly
                        {
                            requestedParam.CurrentValueB = targetValueB;
                            requestedParam.TargetValueB = targetValueB;
                            if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                            {
                                SendUnsolicitedReport(PrepareWindowCoveringReport((byte)requestedParam.ParamId), srcNode);
                            }
                            if (updateUI)
                            {
                                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about the current state
                            }
                        }
                        else                // Create a new transition
                        {
                            requestedParam.StepWidthF = (float)(targetValueB - currentValueB) / (durationTransitionSec * TickerPerSec);
                            requestedParam.CurrentValueF = currentValueB;
                            requestedParam.DurationTransitionHex = durationTransitionHex;
                            requestedParam.DurationOffOnSec = OnValue * durationTransitionSec / (uint)Math.Abs(targetValueB - currentValueB);
                            requestedParam.CmdSrcNode = srcNode;
                        }
                    }
                    requestedParam.TargetValueB = targetValueB;
                    ReportOperationState(requestedParam.ParamId, "Set");
                }
            }
            else
            {
                CommandClassSupportUtils.ReportAddLog(WindowCoveringSupportData.LogLevel,
                            "{0} CC @ {1}: Invalid (even or unsupported) Parameter ID {2} in Set command.",
                            CommandClassShortName, EndPointName, paramId);
                success = SvResultNames.Failed;
            }
            return success;
        }
        public SvResultNames HandleWindowCoveringSetCmd(byte[] command, NodeTag srcNode)
        {
            SvResultNames success = SvResultNames.Successful;

            COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_SET setCmd = command;
            byte paramCount = setCmd.properties1.parameterCount;

            byte requestedDuration = setCmd.duration;
            if (requestedDuration == 0xFF)
            {
                requestedDuration = WindowCoveringSupportData.FactoryDefaultSetDuration;
            }

            List<COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_SET.TVG1> values = setCmd.vg1;
            foreach (COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_SET.TVG1 paramData in values)
            {
                success = HandleSingleWindowCoveringParam(
                                (WindowCoveringParameterNames) (byte) paramData.parameterId, 
                                paramData.value, requestedDuration, true, srcNode);
                if (success != SvResultNames.Successful)
                {
                    break;
                }
            }
            if (paramCount != values.Count)
            {
                CommandClassSupportUtils.ReportAddLog(WindowCoveringSupportData.LogLevel, 
                            "{0} CC @ {1}: ParameterCount field {2} does not match number of vg fields {3} in Set command.", 
                            CommandClassShortName, EndPointName, paramCount, values.Count);
                success = SvResultNames.Failed;
            }
            return success;
        }

        #region LevelChange
        public SvResultNames HandleStartLevelChangeCmd(byte[] command)
        {
            SvResultNames success = SvResultNames.Successful;
            COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_START_LEVEL_CHANGE startLevelCmd = command;

            byte requestedDuration = startLevelCmd.duration;
            if (requestedDuration == 0xFF)
            {
                requestedDuration = WindowCoveringSupportData.FactoryDefaultLevelDuration;
            }

            uint durationOffOnSec = CommandClassSupportUtils.DurationToSeconds(requestedDuration);

            WindowCoveringParameter requestedParam = WindowCoveringSupportData.Parameters.Find(param => (byte)param.ParamId == startLevelCmd.parameterId);
            if (requestedParam != null)
            {
                // Cancel a possibly running transition
                requestedParam.TargetValueB = requestedParam.CurrentValueB;
                requestedParam.StepWidthF = 0;
                requestedParam.DurationTransitionHex = 0;
                requestedParam.DurationOffOnSec = 0;

                byte upDown = startLevelCmd.properties1.upDown;
                byte startLevel = 0;
                byte ignoreStartLevel = 0x01;

                byte targetValueB = (upDown == 0x01) ? OffValue : OnValue;
                byte startValueB = (ignoreStartLevel == 0x00) ? startLevel : requestedParam.CurrentValueB;
                uint durationTransitionSec = (uint) Math.Abs(startValueB - targetValueB) / OnValue * durationOffOnSec;

                if (targetValueB != startValueB)
                {
                    if (durationOffOnSec == 0)  // Instantly
                    {
                        requestedParam.CurrentValueB = targetValueB;
                    }
                    else                        // Create a new transition
                    {
                        requestedParam.StepWidthF = (float) ((upDown == 0x01) ? -0xFF : +0xFF) / (durationOffOnSec * TickerPerSec);
                        requestedParam.CurrentValueF = startValueB;
                        requestedParam.DurationTransitionHex = CommandClassSupportUtils.SecondsToDuration(durationTransitionSec);
                        requestedParam.DurationOffOnSec = durationOffOnSec;
                    }
                }
                requestedParam.TargetValueB = targetValueB;
                requestedParam.LastLevelChangeDirection = upDown;
                ReportOperationState(requestedParam.ParamId, "StartLevel");
            }
            else
            {
                CommandClassSupportUtils.ReportAddLog(WindowCoveringSupportData.LogLevel, 
                            "{0} CC @ {1}: Invalid Parameter ID {2} in StartLevel command.", 
                            CommandClassShortName, EndPointName, startLevelCmd.parameterId);
                success = SvResultNames.Failed;
            }
            return success;
        }

        public SvResultNames HandleStopLevelChangeCmd(byte[] command)
        {
            SvResultNames success = SvResultNames.Successful;
            COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_STOP_LEVEL_CHANGE stopLevelCmd = command;
            WindowCoveringParameter requestedParam = WindowCoveringSupportData.Parameters.Find(param => (byte) param.ParamId == stopLevelCmd.parameterId);
            if (requestedParam != null)
            {
                // requestedParam.TargetValueB = requestedParam.CurrentValueB;
                requestedParam.StepWidthF = 0;
                ReportOperationState(requestedParam.ParamId, "StopLevel");
            }
            else
            {
                CommandClassSupportUtils.ReportAddLog(WindowCoveringSupportData.LogLevel, 
                            "{0} CC @ {1}: Invalid Parameter ID {2} in StopLevel command.", 
                            CommandClassShortName, EndPointName, requestedParam.ParamId);
                success = SvResultNames.Failed;
            }
            return success;
        }
        #endregion // LevelChange

        #region Transition Timer
        private void TransitionTimerStart()
        {
            // WindowCoveringSupportData.CancelPendingTransitionTimer = TransitionTimerReset;
            TransitionTimer.RunWorkerAsync();   // the Transition Timer runs infinitely or until Result.State == ActionStates.Cancelled
        }
        private void TransitionTimerDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            CommandClassSupportUtils.ReportTimerState(WindowCoveringSupportData.LogLevel, 
                        TransitionTimerName, ReportTimerStateNames.Started, TicksElapsed / TickerPerSec);
            while (true)
            {
                TicksElapsed++;

                foreach (WindowCoveringParameter param in WindowCoveringSupportData.Parameters)
                {
                    if (param.StepWidthF != 0)
                    {
                        param.CurrentValueF += param.StepWidthF;
                        if ((param.StepWidthF > 0
                              && param.CurrentValueF >= param.TargetValueB)
                             ||
                             (param.StepWidthF < 0
                              && param.CurrentValueF <= param.TargetValueB)
                           )
                        {
                            param.CurrentValueB = param.TargetValueB;
                            param.DurationTransitionHex = 0;
                            param.StepWidthF = 0;
                            ReportOperationState(param.ParamId, "Elapsed");
                            if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                            {
                                DeviceRestart(param.CmdSrcNode);
                                SendUnsolicitedReport(PrepareWindowCoveringReport((byte)param.ParamId), param.CmdSrcNode);
                                SupportDelegates?.DeviceSleep(this, ReportWakeupTimeout);
                            }
                            SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
                        }
                        else
                        {
                            param.CurrentValueB = Convert.ToByte(param.CurrentValueF);
                            ReportOperationState(param.ParamId, "Tick");
                        }
                    }
                }

                if (Result.State == ActionStates.Cancelled)
                {
                    TransitionTimerReset();
                }
                Thread.Sleep((int)(1000 / TickerPerSec));
                if (bw.CancellationPending)
                {
                    CommandClassSupportUtils.ReportTimerState(WindowCoveringSupportData.LogLevel, 
                                TransitionTimerName, ReportTimerStateNames.CancelledElapsedSec, TicksElapsed / TickerPerSec);
                    e.Cancel = true;
                    break;
                }
            }
            if (!e.Cancel)
            {
                CommandClassSupportUtils.ReportTimerState(WindowCoveringSupportData.LogLevel, 
                            TransitionTimerName, ReportTimerStateNames.ExpiredSec, TicksElapsed / TickerPerSec);
            }
        }

        private void TransitionTimerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _resetEvent.Set();
            }
        }

        private void TransitionTimerReset() // the Transition Timer runs infinitely or until Result.State == ActionStates.Cancelled
        {
            if (TransitionTimer.IsBusy)
            {
                CommandClassSupportUtils.ReportTimerState(WindowCoveringSupportData.LogLevel,
                            TransitionTimerName, ReportTimerStateNames.Reset, 0);
                TransitionTimer.CancelAsync();
                _resetEvent.WaitOne();
            }
        }
        #endregion // Transition Timer

        private void ReportOperationState(WindowCoveringParameterNames paramId, string info)
        {
            if (WindowCoveringSupportData.LogLevel > 0)
            {
                foreach (WindowCoveringParameter param in WindowCoveringSupportData.Parameters)
                {
                    if (param.ParamId == paramId || (byte) paramId == 0xFF)
                    {
                        CommandClassSupportUtils.ReportAddLog(WindowCoveringSupportData.LogLevel, 
                                    "{0} CC @ {1}: [{2}] ID: {3} (0x{4:X2}) - CurrentValue: 0x{5:X2} ({5,3:##0}) - TargetValue: 0x{6:X2} ({6,3:##0}) - StepWidth: {7}",
                                    CommandClassShortName, EndPointName, info, paramId, (byte) paramId,
                                    param.CurrentValueB,
                                    param.TargetValueB,
                                    param.StepWidthF);
                    }
                }
            }
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
                    case COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_SET.ID:
                        if (ResultForSv == SvResultNames.Successful)
                        {
                            supervisionReport.status = (byte)SvStatusNames.Success;
                        }
                        break;
                    case COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_START_LEVEL_CHANGE.ID:
                        if (ResultForSv == SvResultNames.Successful)
                        {
                            supervisionReport.status = (byte)SvStatusNames.Success;
                        }
                        break;
                    case COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_STOP_LEVEL_CHANGE.ID:
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
            if (data is WindowCoveringSupportData newData)
            {
                TransitionTimerName = string.Format("{0} CC @ {1}: Transition Timer", CommandClassShortName, EndPointName);

                if (WindowCoveringSupportData.FactoryDefaultSetDuration != newData.FactoryDefaultSetDuration
                 || WindowCoveringSupportData.FactoryDefaultLevelDuration != newData.FactoryDefaultLevelDuration
                 || WindowCoveringSupportData.SpecificDeviceClass != newData.SpecificDeviceClass)
                {
                    WindowCoveringSupportData.FactoryDefaultSetDuration = newData.FactoryDefaultSetDuration;
                    WindowCoveringSupportData.FactoryDefaultLevelDuration = newData.FactoryDefaultLevelDuration;
                    WindowCoveringSupportData.SpecificDeviceClass = newData.SpecificDeviceClass;
                    //if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    //    SendUnsolicitedReport(PrepareSupportedReport());
                }

                bool identicalParameters = WindowCoveringSupportData.Parameters.Count == newData.Parameters.Count;
                bool transitionRequired = false;
                foreach (WindowCoveringParameter newParam in newData.Parameters)
                {
                    if (WindowCoveringSupportData.Parameters.FindIndex(x => x.ParamId == newParam.ParamId) == -1)
                    {
                        identicalParameters = false;
                    }
                    if (newParam.CurrentValueB != newParam.TargetValueB)
                    {
                        transitionRequired = true;
                    }
                }

                if (!identicalParameters || transitionRequired)
                {
                    WindowCoveringSupportData.Parameters = newData.Parameters;
                    foreach (WindowCoveringParameter param in WindowCoveringSupportData.Parameters)
                    {
                        if (param.CurrentValueB != param.TargetValueB)
                        {
                            HandleSingleWindowCoveringParam(param.ParamId, param.TargetValueB, param.DurationTransitionHex, false, NodeTag.Empty);
                        }
                    }
                }

                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class WindowCoveringParameter : INestedItem
    {
        public WindowCoveringParameterNames ParamId { get; set; }

        #region CurrentValue
        [CustomDataParameter(1, "Current Value", UiControl.TextBox, InputType.Int_0_255, EditType.Config /*| EditType.View*/ /*| EditType.Edit*/)]
        public byte CurrentValueB { get; set; } = 0;
        [JsonIgnore]
        internal float CurrentValueF { get; set; } = 0;
        #endregion

        #region TargetValue
        [CustomDataParameter(1, "Target Value", UiControl.TextBox, InputType.Int_0_255, EditType.Config /*| EditType.View*/ | EditType.Edit)]
        public byte TargetValueB { get; set; } = 0;
        #endregion

        internal float StepWidthF { get; set; } = 0;
        [CustomDataParameter(1, "Duration", UiControl.TextBox, InputType.Hex8, EditType.Config /*| EditType.View*/ | EditType.Edit)]
        public byte DurationTransitionHex { get; set; } = 0;
        internal uint DurationOffOnSec { get; set; } = 0;
        public byte LastLevelChangeDirection { get; set; } = 0;
        internal NodeTag CmdSrcNode { get; set; } = NodeTag.Empty;

        [JsonIgnore]
        public int NestedId { get => (byte) ParamId; set { } }

        public WindowCoveringParameter()
        {
        }

        public WindowCoveringParameter(WindowCoveringParameterNames parmId, byte currentValue, byte durationOffOnSec)
        {
            ParamId = parmId;
            CurrentValueB = currentValue;
            CurrentValueF = currentValue;
            TargetValueB = currentValue;
            StepWidthF = 0;
            DurationTransitionHex = 0;
            DurationOffOnSec = durationOffOnSec;
        }
    }

    public class WindowCoveringSupportData : CommandClassSupportData, IBasicMapping, INodeItem
    {
        // Internals accessible from TestCases or EDE
        // For the encoding of duration values see SDS13781 (0=instantly, 1-127=seconds, 128-254=minutes-127, 255=Factory Default Duration)
        public byte FactoryDefaultSetDuration { get; set; } = 0;    // Default Duration for a Set command transition.
        public byte FactoryDefaultLevelDuration { get; set; } = 16; // Default Duration for a transition from 0% to 100% (0x63).

        [NestedControl(1, "Parameters")]
        public List<WindowCoveringParameter> Parameters { get; set; } = new List<WindowCoveringParameter>();
        public bool AddParameter(WindowCoveringParameterNames parmId, byte currentValue)
        {
            if (Parameters.Find(param => param.ParamId == parmId) == null)
            {
                byte durationOffOnSec = (byte)(((byte)parmId % 2) == 0 ? 0xFE : 0x00);
                WindowCoveringParameter obj = new WindowCoveringParameter(parmId, currentValue, durationOffOnSec);
                Parameters.Add(obj);
                return true;
            }
            return false;
        }

        // Basic CC Mapping
        [JsonIgnore]
        public WindowCoveringSupport SupportClassObject;
        [JsonIgnore]
        public MultilevelSwitchSupportData MultilevelSwitchSupportData;
        public byte BasicZWavePlusVersion { get; set; } = 2;    // Allowed: 1 and 2. It controls the Basic Mapping
        
        [CustomDataParameter(1, "Specific Device Class", CommandClassesImpl.UiControl.ListComboBox, CommandClassesImpl.InputType.Enum, EditType.Config, ItemsSource = typeof(WindowCoveringSpecificDeviceClass))]
        public WindowCoveringSpecificDeviceClass SpecificDeviceClass { get; set; } = WindowCoveringSpecificDeviceClass.PositionAndEndpointAware;

        [JsonIgnore]
        public IEnumerable<INestedItem> ChildItems => Parameters;


#if NO_MAPPING_TO_MULTILEVEL_SWITCH
        private byte BasicUsedParameterId = 0;
#endif // NO_MAPPING_TO_MULTILEVEL_SWITCH

        bool IBasicMapping.BasicSet(byte value, NodeTag srcNode)
        {
            if (SupportClassObject.Version <= 1 && Parameters.Count > 0)
            {
                if ((SpecificDeviceClass == WindowCoveringSpecificDeviceClass.PositionAndEndpointAware && (value <= 0x63 || value == 0xFF))
                 || (SpecificDeviceClass == WindowCoveringSpecificDeviceClass.EndpointAware && (value == 0x00 || value == 0xFF))
                 || (SpecificDeviceClass == WindowCoveringSpecificDeviceClass.NoPositionEndpoint && (value == 0x00 || value == 0xFF) && BasicZWavePlusVersion == 1))
                {
                    WindowCoveringParameter usedParameter = Parameters.Find(param => (((byte) param.ParamId) & 0x01) != 0);
                    if (usedParameter != null)
                    {
                        COMMAND_CLASS_SWITCH_MULTILEVEL_V2.SWITCH_MULTILEVEL_SET setCmd = new COMMAND_CLASS_SWITCH_MULTILEVEL_V2.SWITCH_MULTILEVEL_SET() { };
                        setCmd.value = value;
                        setCmd.duration = 0xFF;
                        CommandClassSupportUtils.ReportAddLog(LogLevel, 
                                    "{0} CC @ {1}: Basic Set with value 0x{2:X2} accepted ({3} / Set).", 
                                    SupportClassObject.CommandClassShortName, SupportClassObject.EndPointName, value, SpecificDeviceClass);
                        return MultilevelSwitchSupportData.SupportClassObject.HandleMultilevelSwitchSetCmd(setCmd, srcNode) == SvResultNames.Successful;
                    }
                }
                else if (SpecificDeviceClass == WindowCoveringSpecificDeviceClass.NoPositionEndpoint && value == 0x00 && BasicZWavePlusVersion == 2)
                {
                    WindowCoveringParameter usedParameter = Parameters.Find(param => (((byte) param.ParamId) & 0x01) != 0);
                    if (usedParameter != null)
                    {
                        COMMAND_CLASS_SWITCH_MULTILEVEL_V2.SWITCH_MULTILEVEL_STOP_LEVEL_CHANGE stopLevelCmd = new COMMAND_CLASS_SWITCH_MULTILEVEL_V2.SWITCH_MULTILEVEL_STOP_LEVEL_CHANGE() { };
                        CommandClassSupportUtils.ReportAddLog(LogLevel, 
                                    "{0} CC @ {1}: Basic Set with value 0x{2:X2} accepted ({3} / StopLevel).", 
                                    SupportClassObject.CommandClassShortName, SupportClassObject.EndPointName, value, SpecificDeviceClass);
                        return MultilevelSwitchSupportData.SupportClassObject.HandleStopLevelChangeCmd(stopLevelCmd) == SvResultNames.Successful;
                    }
                }
                else if ((SpecificDeviceClass == WindowCoveringSpecificDeviceClass.EndpointAware && value <= 0x63)
                      || (SpecificDeviceClass == WindowCoveringSpecificDeviceClass.NoPositionEndpoint && (value <= 0x63 || value == 0xFF)) && BasicZWavePlusVersion == 2)
                {
                    WindowCoveringParameter usedParameter = Parameters.Find(param => (((byte) param.ParamId) & 0x01) != 0);
                    if (usedParameter != null)
                    {
                        COMMAND_CLASS_SWITCH_MULTILEVEL_V3.SWITCH_MULTILEVEL_START_LEVEL_CHANGE startLevelCmd = new COMMAND_CLASS_SWITCH_MULTILEVEL_V3.SWITCH_MULTILEVEL_START_LEVEL_CHANGE() { };
                        startLevelCmd.properties1.ignoreStartLevel = 0x01;
                        startLevelCmd.properties1.incDec = 0x03;
                        switch (MultilevelSwitchSupportData.CurrentValuePrimary)
                        {
                            case 0x00: startLevelCmd.properties1.upDown = 0; break;
                            case 0x63: startLevelCmd.properties1.upDown = 1; value = 0x63;  break;
                            default: startLevelCmd.properties1.upDown = 0; break;
                        }
                        startLevelCmd.dimmingDuration = 0xFF;
                        CommandClassSupportUtils.ReportAddLog(LogLevel, 
                                    "{0} CC @ {1}: Basic Set with value 0x{2:X2} accepted ({3} / StartLevel).", 
                                    SupportClassObject.CommandClassShortName, SupportClassObject.EndPointName, value, SpecificDeviceClass);
                        return MultilevelSwitchSupportData.SupportClassObject.HandleStartLevelChangeCmd(startLevelCmd) == SvResultNames.Successful;
                    }
                }
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0} CC @ {1}: Basic Set with value 0x{2:X2} ignored.", 
                        SupportClassObject.CommandClassShortName, SupportClassObject.EndPointName, value);
            return false;
        }
        COMMAND_CLASS_BASIC_V2.BASIC_REPORT IBasicMapping.BasicV2Get()
        {
            COMMAND_CLASS_BASIC_V2.BASIC_REPORT rpt = new COMMAND_CLASS_BASIC_V2.BASIC_REPORT() { };
            rpt.currentValue = 0xFE;
            rpt.targetValue = 0xFE;
            if (SpecificDeviceClass == WindowCoveringSpecificDeviceClass.PositionAndEndpointAware)
            {
                rpt.currentValue = MultilevelSwitchSupportData.CurrentValuePrimary;
                rpt.targetValue = MultilevelSwitchSupportData.TargetValuePrimary;
            }
            rpt.duration = CommandClassSupportUtils.SecondsToDuration(MultilevelSwitchSupportData.SupportClassObject.RemainingTransition1DurationTimeSec);
            return rpt;
        }
    }

    #region WindowCovering enums
    public enum WindowCoveringParameterNames : byte
    {
        // Outbound horizontal control
        [Description("0 Out left movement")]
        OutLeftMovement  = 0,
        [Description("1 Out left position")]
        OutLeftPosition  = 1,
        [Description("2 Out right movement")]
        OutRightMovement = 2,
        [Description("3 Out right position")]
        OutRightPosition = 3,
        // Inbound horizontal control
        [Description("4 In left movement")]
        InLeftMovement   = 4,
        [Description("5 In left position")]
        InLeftPosition   = 5,
        [Description("6 In right movement")]
        InRightMovement  = 6,
        [Description("7 In right position")]
        InRightPosition  = 7,
        [Description("8 In right left movement")]
        InRightLeftMovement = 8,
        [Description("9 In right left position")]
        InRightLeftPosition = 9,
        // Angle control of vertical slats
        [Description("10 Vertical slats angle movement")]
        VerticalSlatsAnlgeMovement = 10,
        [Description("11 Vertical slats angle position")]
        VerticalSlatsAnlgePosition = 11,
        // Outbound vertical control
        [Description("12 Out bottom movement")]
        OutBottomMovement = 12,
        [Description("13 Out bottom position")]
        OutBottomPosition = 13,
        [Description("14 Out top movement")]
        OutTopMovement = 14,
        [Description("15 Out top position")]
        OutTopPosition = 15,
        // Inbound vertical control
        [Description("16 In bottom movement")]
        InBottomMovement = 16,
        [Description("17 In bottom position")]
        InBottomPosition = 17,
        [Description("18 In top movement")]
        InTopMovement = 18,
        [Description("19 In top position")]
        InTopPosition = 19,
        [Description("20 In top bottom movement")]
        InTopBottomMovement = 20,
        [Description("21 In top bottom position")]
        InTopBottomPosition = 21,
        // Angle control of horizontal slats
        [Description("22 Horizontal slats angle movement")]
        HorizontalSlatsAnlgeMovement = 22,
        [Description("23 Horizontal slats angle position")]
        HorizontalSlatsAnlgePosition = 23,
    }
    public enum WindowCoveringMovementNames : byte
    {
        [Description("0x00 Closing")]
        Closing = 0x00,
        [Description("0x03 Opening")]
        Opening = 0x63,
    }
    public enum WindowCoveringPositionNames : byte
    {
        [Description("0x00 Closed")]
        Closed = 0x00,
        [Description("0x63 Open")]
        Open = 0x63,
        [Description("0xFF Unknown")]
        Unknown = 0xFF,
    }
    public enum WindowCoveringSlatsPositionNames : byte
    {
        [Description("0x00 Closed up")]
        ClosedUp = 0x00,
        [Description("0x32 Open")]
        Open = 0x32,
        [Description("0x63 Closed down")]
        ClosedDown = 0x63,
    }
    public enum WindowCoveringSpecificDeviceClass : byte
    {
        [Description("0x05 No position endpoint")]
        NoPositionEndpoint = 0x05,          // = SPECIFIC_TYPE_CLASS_A_MOTOR_CONTROL, see 'Window Covering DT'
        [Description("0x06 Endpoint aware")]
        EndpointAware = 0x06,               // = SPECIFIC_TYPE_CLASS_B_MOTOR_CONTROL
        [Description("0x07 Position and endpoint aware")]
        PositionAndEndpointAware = 0x07,    // = SPECIFIC_TYPE_CLASS_C_MOTOR_CONTROL
    }
    #endregion // WindowCovering enums
}
