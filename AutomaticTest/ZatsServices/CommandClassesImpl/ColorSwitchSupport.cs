// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Utils;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;

// ToDo: Supervision Report Status 'Working'

namespace Zats.Services
{
    public class ColorSwitchSupport : CommandClassSupport
    {
        private ColorSwitchSupportData ColorSwitchSupportData { get; set; }

        private readonly string CommandClassName = "Color Switch CC";
        private string EndPointName = "";
        private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private readonly BackgroundWorker TransitionTimer = new BackgroundWorker();
        private string TransitionTimerName = "";
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        private uint TicksElapsed = 0;
        private const uint TickerPerSec = 10;

        private readonly byte OnValue  = 0xFF;   // Value of 100% = ON
        private readonly byte OffValue = 0x00;   // Value of   0% = OFF

        public ColorSwitchSupport()
            : this(new ColorSwitchSupportData() { })
        { }

        public ColorSwitchSupport(ColorSwitchSupportData colorSwitchSupportData)
            : base(COMMAND_CLASS_SWITCH_COLOR.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 3;
            ColorSwitchSupportData = colorSwitchSupportData;
            LogLevel = colorSwitchSupportData.LogLevel;
            InitializeColorComponents();

            if (!ColorSwitchSupportData.IsTimerStarted)
            {
                TransitionTimer.DoWork += new DoWorkEventHandler(TransitionTimer_DoWork);
                TransitionTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(TransitionTimer_RunWorkerCompleted);
                TransitionTimer.WorkerSupportsCancellation = true;
                TransitionTimer.WorkerReportsProgress = false;
                TransitionTimer.RunWorkerAsync();
                ColorSwitchSupportData.IsTimerStarted = true;
            }
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
                TransitionTimerName = string.Format("{0}{1}: Transition Timer", CommandClassName, EndPointName);
                ResultForSv = SvResultNames.Undefined;
                if (isResponseAllowed && command != null && command.Length > 1 && ColorSwitchSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_SET.ID)
                    {
                        ResultForSv = HandleColorSwitchSet(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_GET.ID)
                    {
                        rpt = PrepareReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_START_LEVEL_CHANGE.ID)
                    {
                        ResultForSv = HandleStartLevelChange(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_STOP_LEVEL_CHANGE.ID)
                    {
                        ResultForSv = HandleStopLevelChange(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_SUPPORTED_GET.ID)
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

        private void InitializeColorComponents()
        {
            for (int idx = 0; idx < ColorSwitchSupportData.ComponentData.Length; idx++)
            {
                ColorSwitchSupportData.ComponentData[idx] = new ColorComponentData()
                {
                    CurrentValueB = 0,
                    CurrentValueF = 0,
                    TargetValueB = 0,
                    StepWidthF = 0,
                };
            }
        }

        private byte[] PrepareSupportedReport()
        {
            COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_SUPPORTED_REPORT rpt = new COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_SUPPORTED_REPORT() { };
            List<byte> mask = CommandClassSupportUtils.CreateBitmaskFromList(ColorSwitchSupportData.SupportedColorComponentIds, 0);
            rpt.colorComponentMask[0] = (mask.Count == 0) ? (byte) 0x00 : mask[0];
            rpt.colorComponentMask[1] = 0x00;
            return rpt;
        }

        private byte[] PrepareReport(byte[] command)
        {
            COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_GET colorSwitchGetCmd = command;
            byte colorComponentId = colorSwitchGetCmd.colorComponentId;
            byte[] rpt;

            if (Version <= 2)
            {
                rpt = new COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_REPORT()
                {
                    colorComponentId = colorComponentId,
                    value = ColorSwitchSupportData.ComponentData[colorComponentId].CurrentValueB
                };
            }
            else
            {
                byte targetValueB = ColorSwitchSupportData.ComponentData[colorComponentId].TargetValueB;
                byte currentValueB = ColorSwitchSupportData.ComponentData[colorComponentId].CurrentValueB;
                rpt = new COMMAND_CLASS_SWITCH_COLOR_V3.SWITCH_COLOR_REPORT()
                {
                    colorComponentId = colorComponentId,
                    currentValue = currentValueB,
                    targetValue = targetValueB,
                    duration = CommandClassSupportUtils.SecondsToDuration((uint)Math.Abs(targetValueB - currentValueB) * ColorSwitchSupportData.ComponentData[colorComponentId].DurationOffOnSec / OnValue),
                };
                //CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: [Report] Component: {2} - CurrentValue: 0x{3:X2} ({3,3:##0}) - TargetValue: 0x{4:X2} ({4,3:##0}) - Duration: {5}",
                //  CommandClassName, EndPointName, colorComponentId, currentValueB, targetValueB, CommandClassSupportUtils.DurationToSeconds(rpt.duration));
            }
            //ReportOperationState(-1, "ColorReport");
            ReportOperationState(colorComponentId, "ColorRep");
            return rpt;
        }

        private SvResultNames HandleColorSwitchSet(byte[] command)
        {
            SvResultNames success = SvResultNames.Successful;

            COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_SET colorSwitchSetCmd = command;

            byte requestedDuration = ColorSwitchSupportData.FactoryDefaultSetDuration;
            if (Version >= 2)
            {
                COMMAND_CLASS_SWITCH_COLOR_V2.SWITCH_COLOR_SET colorSwitchSetV2Cmd = command;
                requestedDuration = colorSwitchSetV2Cmd.duration;
                if (requestedDuration == 0xFF)
                    requestedDuration = ColorSwitchSupportData.FactoryDefaultSetDuration;
            }
            uint durationTransitionSec = CommandClassSupportUtils.DurationToSeconds(requestedDuration);

            for (int idx = 0; idx < colorSwitchSetCmd.properties1.colorComponentCount; idx++)
            {
                byte colorComponentId = colorSwitchSetCmd.vg1[idx].colorComponentId;
                if (ColorSwitchSupportData.SupportedColorComponentIds.Contains((ColorComponentIds) colorComponentId))
                {
                    byte targetValueB = colorSwitchSetCmd.vg1[idx].value;
                    byte currentValueB = ColorSwitchSupportData.ComponentData[colorComponentId].CurrentValueB;

                    // Cancel a possibly running transition
                    ColorSwitchSupportData.ComponentData[colorComponentId].TargetValueB = currentValueB;
                    ColorSwitchSupportData.ComponentData[colorComponentId].StepWidthF = 0;
                    ColorSwitchSupportData.ComponentData[colorComponentId].DurationTransitionSec = 0;
                    ColorSwitchSupportData.ComponentData[colorComponentId].DurationOffOnSec = 0;

                    if (targetValueB != currentValueB)
                    {
                        if (durationTransitionSec == 0) // Instantly
                        {
                            ColorSwitchSupportData.ComponentData[colorComponentId].CurrentValueB = targetValueB;
                        }
                        else                            // Create a new transition
                        {
                            ColorSwitchSupportData.ComponentData[colorComponentId].StepWidthF = (float)(targetValueB - currentValueB) / (durationTransitionSec * TickerPerSec);
                            ColorSwitchSupportData.ComponentData[colorComponentId].CurrentValueF = currentValueB;
                            ColorSwitchSupportData.ComponentData[colorComponentId].DurationTransitionSec = durationTransitionSec;
                            ColorSwitchSupportData.ComponentData[colorComponentId].DurationOffOnSec = OnValue * durationTransitionSec / (uint)Math.Abs(targetValueB - currentValueB);
                        }
                    }
                    ColorSwitchSupportData.ComponentData[colorComponentId].TargetValueB = targetValueB;
                    ReportOperationState(colorComponentId, "ColorSet");
                }
                else
                {
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Invalid Color Component ID {2} in Set command.", 
                                CommandClassName, EndPointName, colorComponentId);
                    success = SvResultNames.Failed;
                }
            }
            return success;
        }

        private SvResultNames HandleStartLevelChange(byte[] command)
        {
            SvResultNames success = SvResultNames.Successful;

            byte requestedDuration = ColorSwitchSupportData.FactoryDefaultLevelDuration;
            if (Version >= 3)
            {
                COMMAND_CLASS_SWITCH_COLOR_V3.SWITCH_COLOR_START_LEVEL_CHANGE colorSwitchV3Cmd = command;
                requestedDuration = colorSwitchV3Cmd.duration;
                if (requestedDuration == 0xFF)
                    requestedDuration = ColorSwitchSupportData.FactoryDefaultLevelDuration;
            }
            uint durationOffOnSec = CommandClassSupportUtils.DurationToSeconds(requestedDuration);

            COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_START_LEVEL_CHANGE colorSwitchStartLevelCmd = command;
            byte colorComponentId = colorSwitchStartLevelCmd.colorComponentId;

            if (ColorSwitchSupportData.SupportedColorComponentIds.Contains((ColorComponentIds) colorComponentId))
            {
                // Cancel a possibly running transition
                ColorSwitchSupportData.ComponentData[colorComponentId].TargetValueB = ColorSwitchSupportData.ComponentData[colorComponentId].CurrentValueB;
                ColorSwitchSupportData.ComponentData[colorComponentId].StepWidthF = 0;
                ColorSwitchSupportData.ComponentData[colorComponentId].DurationTransitionSec = 0;
                ColorSwitchSupportData.ComponentData[colorComponentId].DurationOffOnSec = 0;

                byte upDown = colorSwitchStartLevelCmd.properties1.upDown;
                byte startLevel = colorSwitchStartLevelCmd.startLevel;
                byte ignoreStartLevel = colorSwitchStartLevelCmd.properties1.ignoreStartState;

                byte targetValueB = (upDown == 0x01) ? OffValue : OnValue;
                byte startValueB = (ignoreStartLevel == 0x00) ? startLevel : ColorSwitchSupportData.ComponentData[colorComponentId].CurrentValueB;
                uint durationTransitionSec = (uint) Math.Abs(startValueB - targetValueB) / OnValue * durationOffOnSec;

                if (targetValueB != startValueB)
                {
                    if (durationOffOnSec == 0)  // Instantly
                    {
                        ColorSwitchSupportData.ComponentData[colorComponentId].CurrentValueB = targetValueB;
                    }
                    else                        // Create a new transition
                    {
                        ColorSwitchSupportData.ComponentData[colorComponentId].StepWidthF = (float)((upDown == 0x01) ? -0xFF : +0xFF) / (durationOffOnSec * TickerPerSec);
                        ColorSwitchSupportData.ComponentData[colorComponentId].CurrentValueF = startValueB;
                        ColorSwitchSupportData.ComponentData[colorComponentId].DurationTransitionSec = durationTransitionSec;
                        ColorSwitchSupportData.ComponentData[colorComponentId].DurationOffOnSec = durationOffOnSec;
                    }
                }
                ColorSwitchSupportData.ComponentData[colorComponentId].TargetValueB = targetValueB;
                ReportOperationState(colorComponentId, "StartLev");
            }
            else
            {
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Invalid Color Component ID {2} in StartLevel command.", 
                            CommandClassName, EndPointName, colorComponentId);
                success = SvResultNames.Failed;
            }
            return success;
        }

        private SvResultNames HandleStopLevelChange(byte[] command)
        {
            COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_STOP_LEVEL_CHANGE colorSwitchCmd = command;

            byte colorComponentId = colorSwitchCmd.colorComponentId;

            if (ColorSwitchSupportData.SupportedColorComponentIds.Contains((ColorComponentIds) colorComponentId))
            {
                ColorSwitchSupportData.ComponentData[colorComponentId].TargetValueB = ColorSwitchSupportData.ComponentData[colorComponentId].CurrentValueB;
                ColorSwitchSupportData.ComponentData[colorComponentId].StepWidthF = 0;
                ReportOperationState(colorComponentId, "StopLev");
                return SvResultNames.Successful;
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Invalid Color Component ID {2} in StopLevel command.", 
                        CommandClassName, EndPointName, colorComponentId);
            return SvResultNames.Failed;
        }

        private void TransitionTimer_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            CommandClassSupportUtils.ReportTimerState(LogLevel, TransitionTimerName, ReportTimerStateNames.Started, TicksElapsed / TickerPerSec);
            while (true)
            {
                TicksElapsed++;

                for (int idx = 0; idx < ColorSwitchSupportData.ComponentData.Length; idx++)
                {
                    if (ColorSwitchSupportData.ComponentData[idx].StepWidthF != 0)
                    {
                        ColorSwitchSupportData.ComponentData[idx].CurrentValueF += ColorSwitchSupportData.ComponentData[idx].StepWidthF;
                        if ((ColorSwitchSupportData.ComponentData[idx].StepWidthF > 0
                              && ColorSwitchSupportData.ComponentData[idx].CurrentValueF >= ColorSwitchSupportData.ComponentData[idx].TargetValueB)
                             ||
                             (ColorSwitchSupportData.ComponentData[idx].StepWidthF < 0
                              && ColorSwitchSupportData.ComponentData[idx].CurrentValueF <= ColorSwitchSupportData.ComponentData[idx].TargetValueB)
                           )
                        {
                            ColorSwitchSupportData.ComponentData[idx].CurrentValueB = ColorSwitchSupportData.ComponentData[idx].TargetValueB;
                            ColorSwitchSupportData.ComponentData[idx].StepWidthF = 0;
                            ReportOperationState(idx, "Elapsed");
                        }
                        else
                        {
                            ColorSwitchSupportData.ComponentData[idx].CurrentValueB = Convert.ToByte(ColorSwitchSupportData.ComponentData[idx].CurrentValueF);
                            ReportOperationState(idx, "Tick");
                        }
                    }
                }

                Thread.Sleep((int) (1000 / TickerPerSec) );
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
        private void ReportOperationState(int componentIndex, string info)
        {
            if (LogLevel > 0)
            {
                for (int idx = 0; idx < ColorSwitchSupportData.ComponentData.Length; idx++)
                {
                    if (idx == componentIndex || componentIndex == -1)
                    {
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: [{2,-8}] Component: {3,-9} ({4}) - Current: 0x{5:X2} ({5,3:##0}) - Target: 0x{6:X2} ({6,3:##0}) - StepWidth: {7}",
                        CommandClassName, EndPointName, info, (ColorComponentIds) idx, idx, 
                        ColorSwitchSupportData.ComponentData[idx].CurrentValueB,
                        ColorSwitchSupportData.ComponentData[idx].TargetValueB,
                        ColorSwitchSupportData.ComponentData[idx].StepWidthF);
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
                    case COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_SET.ID:
                        if (ResultForSv == SvResultNames.Successful)
                            supervisionReport.status = (byte) SvStatusNames.Success;
                        break;
                    case COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_START_LEVEL_CHANGE.ID:
                        if (ResultForSv == SvResultNames.Successful)
                            supervisionReport.status = (byte) SvStatusNames.Success;
                        break;
                    case COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_STOP_LEVEL_CHANGE.ID:
                        if (ResultForSv == SvResultNames.Successful)
                            supervisionReport.status = (byte) SvStatusNames.Success;
                        break;
                }
                ResultForSv = SvResultNames.Undefined;
            }
        }
    }

    public class ColorComponentData
    {
        public byte  CurrentValueB;
        public float CurrentValueF;
        public byte  TargetValueB;
        public float StepWidthF;
        public uint  DurationTransitionSec;
        public uint  DurationOffOnSec;
    };

    public class ColorSwitchSupportData
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
        // For the encoding of duration values see SDS13781 (0=instantly, 1-127=seconds, 128-254=minutes-127, 255=Factory Default Duration)
        public byte FactoryDefaultSetDuration { get; set; } = 0;    // Default Duration for a Set command transition, used for all Color Component IDs.
        public byte FactoryDefaultLevelDuration { get; set; } = 32; // Default Duration for a transition from 0% to 100% (255), used for all Color Component IDs.

        // V1+
        // Supported Color Component IDs 0 to 7 are supported, 8 to 15 are reserved and MUST NOT be used
        public List<ColorComponentIds> SupportedColorComponentIds { get; set; } = new List<ColorComponentIds>
        {
            ColorComponentIds.WarmWhite,
            ColorComponentIds.ColdWhite,
            ColorComponentIds.Red,
            ColorComponentIds.Green,
            ColorComponentIds.Blue,
            ColorComponentIds.Amber,
            ColorComponentIds.Cyan,
            ColorComponentIds.Purple,
        };
        // Color Component Runtime Data
        public ColorComponentData[] ComponentData { get; set; } = new ColorComponentData[8];
        public bool IsTimerStarted { get; set; } = false;   // This value is maintained by ColorSwitchSupport(). Do not change!
    }

    public enum ColorComponentIds
    { 
        WarmWhite = 0x00,
        ColdWhite = 0x01,
        Red = 0x02,
        Green = 0x03,
        Blue = 0x04,
        Amber = 0x05,
        Cyan = 0x06,
        Purple = 0x07
    }
}
