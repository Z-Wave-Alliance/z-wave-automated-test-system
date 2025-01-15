// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
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

// ToDo: Supervision Report Status 'Working'

namespace Zats.Services
{
    public class WindowCoveringSupport : CommandClassSupport
    {
        private WindowCoveringSupportData WindowCoveringSupportData { get; set; }

        public readonly string CommandClassName = "Window Covering CC";
        public string EndPointName = "";
        private readonly int LogLevel = 0;
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
            MinCCVersion = 1;
            MaxCCVersion = 1;
            WindowCoveringSupportData = windowCoveringSupportData;
            WindowCoveringSupportData.SupportClassObject = this;
            WindowCoveringSupportData.MultilevelSwitchSupportData = multilevelSwitchSupportData;
            LogLevel = WindowCoveringSupportData.LogLevel;

            TransitionTimer.DoWork += new DoWorkEventHandler(TransitionTimer_DoWork);
            TransitionTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(TransitionTimer_RunWorkerCompleted);
            TransitionTimer.WorkerSupportsCancellation = true;
            TransitionTimer.WorkerReportsProgress = false;
            TransitionTimer.RunWorkerAsync();
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
                TransitionTimerName = string.Format("{0}{1}: Transition Timer (Global)", CommandClassName, EndPointName);
                ResultForSv = SvResultNames.Undefined;
                if (isResponseAllowed && command != null && command.Length > 1 && WindowCoveringSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_SUPPORTED_GET.ID)
                    {
                        rpt = PrepareSupportedReport();
                    }
                    else if (command[1] == COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_SET.ID)
                    {
                        ResultForSv = HandleWindowCoveringSet(command);
                    }
                    else if (command[1] == COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_GET.ID)
                    {
                        rpt = PrepareReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_START_LEVEL_CHANGE.ID)
                    {
                        ResultForSv = HandleStartLevelChange(command);
                    }
                    else if (command[1] == COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_STOP_LEVEL_CHANGE.ID)
                    {
                        ResultForSv = HandleStopLevelChange(command);
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

        private byte[] PrepareSupportedReport()
        {
            COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_SUPPORTED_REPORT rpt = new COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_SUPPORTED_REPORT() { };
            List<WindowCoveringParameterNames> supportedParameters = new List<WindowCoveringParameterNames> { };
            foreach (WindowCoveringParameter param in WindowCoveringSupportData.Parameters)
            {
                supportedParameters.Add(param.ParmId);
            }
            rpt.parameterMask = CommandClassSupportUtils.CreateBitmaskFromList(supportedParameters, 0);
            rpt.properties1.numberOfParameterMaskBytes = (byte) rpt.parameterMask.Count;
            return rpt;
        }

        private byte[] PrepareReport(byte[] command)
        {
            byte requestedParamId = ((COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_GET)command).parameterId;
            WindowCoveringParameter requestedParam = WindowCoveringSupportData.Parameters.Find(param => (byte)param.ParmId == requestedParamId);
            if (requestedParam != null)
            {
                COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_REPORT rpt = new COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_REPORT() { };
                rpt.parameterId = requestedParamId;
                //CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Report: StepWidth is {2}.", CommandClassName, EndPointName, requestedParam.StepWidthF);
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
                    rpt.duration = CommandClassSupportUtils.SecondsToDuration((uint) Math.Abs(rpt.targetValue - rpt.currentValue) * requestedParam.DurationOffOnSec / OnValue);
                }
                return rpt;
            }
            return null;
        }

        public SvResultNames HandleWindowCoveringSet(byte[] command)
        {
            SvResultNames success = SvResultNames.Successful;

            COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_SET setCmd = command;
            byte paramCount = setCmd.properties1.parameterCount;

            byte requestedDuration = setCmd.duration;
            if (requestedDuration == 0xFF)
                requestedDuration = WindowCoveringSupportData.FactoryDefaultSetDuration;

            uint durationTransitionSec = CommandClassSupportUtils.DurationToSeconds(requestedDuration);
            List<COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_SET.TVG1> values = setCmd.vg1;
            foreach (COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_SET.TVG1 paramData in values)
            {
                WindowCoveringParameter requestedParam = WindowCoveringSupportData.Parameters.Find(param => (byte) param.ParmId == paramData.parameterId);
                if (requestedParam != null && (paramData.parameterId % 2) != 0)     // Ignore even Movement parameters
                {
                    byte targetValueB = paramData.value;
                    byte currentValueB = requestedParam.CurrentValueB;

                    if (targetValueB > OnValue)
                    {
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Invalid value for Parameter ID {2} in Set command.",
                                    CommandClassName, EndPointName, targetValueB, paramData.parameterId);
                        success = SvResultNames.Failed;
                    }
                    else
                    { 
                        // Cancel a possibly running transition
                        requestedParam.TargetValueB = requestedParam.CurrentValueB;
                        requestedParam.StepWidthF = 0;
                        requestedParam.DurationTransitionSec = 0;
                        requestedParam.DurationOffOnSec = 0;

                        if (targetValueB != currentValueB)
                        {
                            if (durationTransitionSec == 0)  // Instantly
                            {
                                requestedParam.CurrentValueB = targetValueB;
                            }
                            else                // Create a new transition
                            {
                                requestedParam.StepWidthF = (float)(targetValueB - currentValueB) / (durationTransitionSec * TickerPerSec);
                                requestedParam.CurrentValueF = currentValueB;
                                requestedParam.DurationTransitionSec = durationTransitionSec;
                                requestedParam.DurationOffOnSec = OnValue * durationTransitionSec / (uint)Math.Abs(targetValueB - currentValueB);
                            }
                        }
                        requestedParam.TargetValueB = targetValueB;
                        ReportOperationState(requestedParam.ParmId, "Set");
                    }
                }
                else
                {
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Invalid (even or unsupported) Parameter ID {2} in Set command.", 
                                CommandClassName, EndPointName, paramData.parameterId);
                    success = SvResultNames.Failed;
                }
            }
            if (paramCount != values.Count)
            {
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: ParameterCount field {2} does not match number of vg fields {3} in Set command.", 
                            CommandClassName, EndPointName, paramCount, values.Count);
                success = SvResultNames.Failed;
            }
            return success;
        }

        public SvResultNames HandleStartLevelChange(byte[] command)
        {
            SvResultNames success = SvResultNames.Successful;
            COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_START_LEVEL_CHANGE startLevelCmd = command;

            byte requestedDuration = startLevelCmd.duration;
            if (requestedDuration == 0xFF)
                requestedDuration = WindowCoveringSupportData.FactoryDefaultLevelDuration;

            uint durationOffOnSec = CommandClassSupportUtils.DurationToSeconds(requestedDuration);

            WindowCoveringParameter requestedParam = WindowCoveringSupportData.Parameters.Find(param => (byte)param.ParmId == startLevelCmd.parameterId);
            if (requestedParam != null)
            {
                // Cancel a possibly running transition
                requestedParam.TargetValueB = requestedParam.CurrentValueB;
                requestedParam.StepWidthF = 0;
                requestedParam.DurationTransitionSec = 0;
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
                        requestedParam.DurationTransitionSec = durationTransitionSec;
                        requestedParam.DurationOffOnSec = durationOffOnSec;
                    }
                }
                requestedParam.TargetValueB = targetValueB;
                requestedParam.LastLevelChangeDirection = upDown;
                ReportOperationState(requestedParam.ParmId, "StartLevel");
            }
            else
            {
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Invalid Parameter ID {2} in StartLevel command.", 
                            CommandClassName, EndPointName, startLevelCmd.parameterId);
                success = SvResultNames.Failed;
            }
            return success;
        }

        public SvResultNames HandleStopLevelChange(byte[] command)
        {
            SvResultNames success = SvResultNames.Successful;
            COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_STOP_LEVEL_CHANGE stopLevelCmd = command;
            WindowCoveringParameter requestedParam = WindowCoveringSupportData.Parameters.Find(param => (byte) param.ParmId == stopLevelCmd.parameterId);
            if (requestedParam != null)
            {
                // requestedParam.TargetValueB = requestedParam.CurrentValueB;
                requestedParam.StepWidthF = 0;
                ReportOperationState(requestedParam.ParmId, "StopLevel");
            }
            else
            {
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Invalid Parameter ID {2} in StopLevel command.", 
                            CommandClassName, EndPointName, requestedParam.ParmId);
                success = SvResultNames.Failed;
            }
            return success;
        }

        private void TransitionTimer_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            CommandClassSupportUtils.ReportTimerState(LogLevel, TransitionTimerName, ReportTimerStateNames.Started, TicksElapsed / TickerPerSec);
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
                            param.StepWidthF = 0;
                            ReportOperationState(param.ParmId, "Elapsed");
                        }
                        else
                        {
                            param.CurrentValueB = Convert.ToByte(param.CurrentValueF);
                            ReportOperationState(param.ParmId, "Tick");
                        }
                    }
                }

                Thread.Sleep((int)(1000 / TickerPerSec));
                if (bw.CancellationPending)
                {
                    CommandClassSupportUtils.ReportTimerState(LogLevel, TransitionTimerName, ReportTimerStateNames.CancelledElapsedSec, TicksElapsed / TickerPerSec);
                    e.Cancel = true;
                    break;
                }
            }
            if (!e.Cancel)
            {
                CommandClassSupportUtils.ReportTimerState(LogLevel, TransitionTimerName, ReportTimerStateNames.ExpiredSec, TicksElapsed / TickerPerSec);
            }
        }

        private void TransitionTimer_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
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
                CommandClassSupportUtils.ReportTimerState(LogLevel, TransitionTimerName, ReportTimerStateNames.Reset, 0);
            }
        }

        private void ReportOperationState(WindowCoveringParameterNames paramId, string info)
        {
            if (LogLevel > 0)
            {
                foreach (WindowCoveringParameter param in WindowCoveringSupportData.Parameters)
                {
                    if (param.ParmId == paramId || (byte) paramId == 0xFF)
                    {
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: [{2}] ID: {3} (0x{4:X2}) - CurrentValue: 0x{5:X2} ({5,3:##0}) - TargetValue: 0x{6:X2} ({6,3:##0}) - StepWidth: {7}",
                        CommandClassName, EndPointName, info, paramId, (byte) paramId,
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
                if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != Id)
                {
                    supervisionReport.status = (byte) SvStatusNames.NoSupport;
                    return;
                }

                supervisionReport.status = (byte) SvStatusNames.Fail;  // FAIL as default
                switch (supervisionGet.encapsulatedCommand[1])
                {
                    case COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_SET.ID:
                        if (ResultForSv == SvResultNames.Successful)
                            supervisionReport.status = (byte) SvStatusNames.Success;
                        break;
                    case COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_START_LEVEL_CHANGE.ID:
                        if (ResultForSv == SvResultNames.Successful)
                            supervisionReport.status = (byte) SvStatusNames.Success;
                        break;
                    case COMMAND_CLASS_WINDOW_COVERING.WINDOW_COVERING_STOP_LEVEL_CHANGE.ID:
                        if (ResultForSv == SvResultNames.Successful)
                            supervisionReport.status = (byte) SvStatusNames.Success;
                        break;
                }
                ResultForSv = SvResultNames.Undefined;
            }
        }
    }

    public class WindowCoveringParameter
    {
        public WindowCoveringParameterNames ParmId;
        public byte  CurrentValueB;
        public float CurrentValueF;
        public byte  TargetValueB;
        public float StepWidthF;
        public uint  DurationTransitionSec;
        public uint  DurationOffOnSec;
        public byte  LastLevelChangeDirection;

        public WindowCoveringParameter(WindowCoveringParameterNames parmId, byte currentValue, byte durationOffOnSec)
        {
            ParmId = parmId;
            CurrentValueB = currentValue;
            CurrentValueF = currentValue;
            TargetValueB = currentValue;
            StepWidthF = 0;
            DurationTransitionSec = 0;
            DurationOffOnSec = durationOffOnSec;
        }
    }

    public class WindowCoveringSupportData : IBasicMapping
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
        // For the encoding of duration values see SDS13781 (0=instantly, 1-127=seconds, 128-254=minutes-127, 255=Factory Default Duration)
        public byte FactoryDefaultSetDuration { get; set; } = 0;    // Default Duration for a Set command transition.
        public byte FactoryDefaultLevelDuration { get; set; } = 16; // Default Duration for a transition from 0% to 100% (0x63).
        public WindowCoveringSpecificDeviceType SpecificDeviceClass { get; set; } = WindowCoveringSpecificDeviceType.PositionAndEndpointAware;

        public List<WindowCoveringParameter> Parameters { get; set; } = new List<WindowCoveringParameter>();

        public bool AddParameter(WindowCoveringParameterNames parmId, byte currentValue)
        {
            if (Parameters.Find(param => param.ParmId == parmId) == null)
            {
                byte duration = (byte)(((byte)parmId % 2) == 0 ? 0xFE : 0x00);
                WindowCoveringParameter obj = new WindowCoveringParameter(parmId, currentValue, duration);
                Parameters.Add(obj);
                return true;
            }
            return false;
        }

        // Basic CC Mapping
        public WindowCoveringSupport SupportClassObject;
        public MultilevelSwitchSupportData MultilevelSwitchSupportData;
        public byte BasicZWavePlusVersion { get; set; } = 2;    // Allowed: 1 and 2. Controls Basic Mapping
#if NO_MAPPING_TO_MULTILEVEL_SWITCH
        private byte BasicUsedParameterId = 0;
#endif // NO_MAPPING_TO_MULTILEVEL_SWITCH

        bool IBasicMapping.BasicSet(byte value)
        {
            if (SupportClassObject.Version <= 1 && Parameters.Count > 0)
            {
                if ((SpecificDeviceClass == WindowCoveringSpecificDeviceType.PositionAndEndpointAware && (value <= 0x63 || value == 0xFF))
                 || (SpecificDeviceClass == WindowCoveringSpecificDeviceType.EndpointAware && (value == 0x00 || value == 0xFF))
                 || (SpecificDeviceClass == WindowCoveringSpecificDeviceType.NoPositionEndpoint && (value == 0x00 || value == 0xFF) && BasicZWavePlusVersion == 1))
                {
                    WindowCoveringParameter usedParameter = Parameters.Find(param => (((byte) param.ParmId) & 0x01) != 0);
                    if (usedParameter != null)
                    {
                        COMMAND_CLASS_SWITCH_MULTILEVEL_V2.SWITCH_MULTILEVEL_SET setCmd = new COMMAND_CLASS_SWITCH_MULTILEVEL_V2.SWITCH_MULTILEVEL_SET() { };
                        setCmd.value = value;
                        setCmd.duration = 0xFF;
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Basic Set with value 0x{2:X2} accepted ({3} / Set).", 
                                    SupportClassObject.CommandClassName, SupportClassObject.EndPointName, value, SpecificDeviceClass);
                        return MultilevelSwitchSupportData.SupportClassObject.HandleMultilevelSwitchSet(setCmd) == SvResultNames.Successful;
                    }
                }
                else if (SpecificDeviceClass == WindowCoveringSpecificDeviceType.NoPositionEndpoint && value == 0x00 && BasicZWavePlusVersion == 2)
                {
                    WindowCoveringParameter usedParameter = Parameters.Find(param => (((byte) param.ParmId) & 0x01) != 0);
                    if (usedParameter != null)
                    {
                        COMMAND_CLASS_SWITCH_MULTILEVEL_V2.SWITCH_MULTILEVEL_STOP_LEVEL_CHANGE stopLevelCmd = new COMMAND_CLASS_SWITCH_MULTILEVEL_V2.SWITCH_MULTILEVEL_STOP_LEVEL_CHANGE() { };
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Basic Set with value 0x{2:X2} accepted ({3} / StopLevel).", 
                                    SupportClassObject.CommandClassName, SupportClassObject.EndPointName, value, SpecificDeviceClass);
                        return MultilevelSwitchSupportData.SupportClassObject.HandleStopLevelChange(stopLevelCmd) == SvResultNames.Successful;
                    }
                }
                else if ((SpecificDeviceClass == WindowCoveringSpecificDeviceType.EndpointAware && value <= 0x63)
                      || (SpecificDeviceClass == WindowCoveringSpecificDeviceType.NoPositionEndpoint && (value <= 0x63 || value == 0xFF)) && BasicZWavePlusVersion == 2)
                {
                    WindowCoveringParameter usedParameter = Parameters.Find(param => (((byte) param.ParmId) & 0x01) != 0);
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
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Basic Set with value 0x{2:X2} accepted ({3} / StartLevel).", 
                                    SupportClassObject.CommandClassName, SupportClassObject.EndPointName, value, SpecificDeviceClass);
                        return MultilevelSwitchSupportData.SupportClassObject.HandleStartLevelChange(startLevelCmd) == SvResultNames.Successful;
                    }
                }
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Basic Set with value 0x{2:X2} ignored.", SupportClassObject.CommandClassName, SupportClassObject.EndPointName, value);
            return false;
        }
        COMMAND_CLASS_BASIC_V2.BASIC_REPORT IBasicMapping.BasicV2Get()
        {
            COMMAND_CLASS_BASIC_V2.BASIC_REPORT rpt = new COMMAND_CLASS_BASIC_V2.BASIC_REPORT() { };
            rpt.currentValue = 0xFE;
            rpt.targetValue = 0xFE;
            if (SpecificDeviceClass == WindowCoveringSpecificDeviceType.PositionAndEndpointAware)
            {
                rpt.currentValue = MultilevelSwitchSupportData.CurrentValuePrimary;
                rpt.targetValue = MultilevelSwitchSupportData.TargetValuePrimary;
            }
            rpt.duration = CommandClassSupportUtils.SecondsToDuration(MultilevelSwitchSupportData.SupportClassObject.RemainingTransition1DurationTimeSec);
            return rpt;
        }
    }

    public enum WindowCoveringParameterNames : byte
    {
        // Outbound horizontal control
        OutLeftMovement  = 0,
        OutLeftPosition  = 1,
        OutRightMovement = 2,
        OutRightPosition = 3,
        // Inbound horizontal control
        InLeftMovement   = 4,
        InLeftPosition   = 5,
        InRightMovement  = 6,
        InRightPosition  = 7,
        InRightLeftMovement = 8,
        InRightLeftPosition = 9,
        // Angle control of vertical slats
        VerticalSlatsAnlgeMovement = 10,
        VerticalSlatsAnlgePosition = 11,
        // Outbound vertical control
        OutBottomMovement = 12,
        OutBottomPosition = 13,
        OutTopMovement = 14,
        OutTopPosition = 15,
        // Inbound vertical control
        InBottomMovement = 16,
        InBottomPosition = 17,
        InTopMovement = 18,
        InTopPosition = 19,
        InTopBottomMovement = 20,
        InTopBottomPosition = 21,
        // Angle control of horizontal slats
        HorizontalSlatsAnlgeMovement = 22,
        HorizontalSlatsAnlgePosition = 23,
    }
    public enum WindowCoveringMovementNames : byte
    {
        Closing = 0x00,
        Opening = 0x63,
    }
    public enum WindowCoveringPositionNames : byte
    {
        Closed = 0x00,
        Open = 0x63,
        Unknown = 0xFF,
    }
    public enum WindowCoveringSlatsPositionNames : byte
    {
        ClosedUp = 0x00,
        Open = 0x32,
        ClosedDown = 0x63,
    }
    public enum WindowCoveringSpecificDeviceType : byte
    {
        NoPositionEndpoint = 0x05,
        EndpointAware = 0x06,
        PositionAndEndpointAware = 0x07,
    }
}
