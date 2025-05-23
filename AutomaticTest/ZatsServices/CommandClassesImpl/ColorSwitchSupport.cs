using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using UicApplication.Clusters;
using Utils;
using ZatsServices_netcore.CommandClassesImpl.Definitions.Interfaces;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;
using Newtonsoft.Json;
using Zats.Services.CommandClassesImpl;

// ToDo: Supervision Report Status 'Working'

namespace Zats.Services
{
    [CommandClassName("Color Switch Command Class")]
    [CommandClassId(COMMAND_CLASS_SWITCH_COLOR.ID)]
    [MinVersion(1)]
    [MaxVersion(3)]
    public class ColorSwitchSupport : CommandClassSupport
    {
        private ColorSwitchSupportData ColorSwitchSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return ColorSwitchSupportData; }
            set { ColorSwitchSupportData = (ColorSwitchSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private readonly BackgroundWorker TransitionTimer = new BackgroundWorker();
        private string TransitionTimerName = "";
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        private uint TicksElapsed = 0;
        private const uint TickerPerSec = 10;
        private bool IsTimerStarted = false;     // This value is maintained by ColorSwitchSupport(). Do not change!

        private readonly byte OnValue  = 0xFF;   // Value of 100% = ON
        private readonly byte OffValue = 0x00;   // Value of   0% = OFF

        public ColorSwitchSupport()
            : this(new ColorSwitchSupportData() { })
        { }

        public ColorSwitchSupport(ColorSwitchSupportData colorSwitchSupportData)
            : base(COMMAND_CLASS_SWITCH_COLOR.ID)
        {
            ColorSwitchSupportData = colorSwitchSupportData;

            if (!IsTimerStarted)
            {
                TransitionTimer.DoWork += new DoWorkEventHandler(TransitionTimerDoWork);
                TransitionTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(TransitionTimerRunWorkerCompleted);
                TransitionTimer.WorkerSupportsCancellation = true;
                TransitionTimer.WorkerReportsProgress = false;
                TransitionTimerStart(); // the Transition Timer runs infinitely or until Result.State == ActionStates.Cancelled
                IsTimerStarted = true;
            }
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
                if (isHandlingAllowed && command != null && command.Length > 1 && ColorSwitchSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_SET.ID)
                    {
                        ResultForSv = HandleColorSwitchSetCmd(command, sendSrcNode);
                    }
                    else if (command[1] == COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareColorSwitchReportCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_START_LEVEL_CHANGE.ID)
                    {
                        ResultForSv = HandleStartLevelChangeCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_STOP_LEVEL_CHANGE.ID)
                    {
                        ResultForSv = HandleStopLevelChangeCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_SUPPORTED_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareSupportedReport();
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
            COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_SUPPORTED_REPORT rpt = new COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_SUPPORTED_REPORT() { };

            List<ColorComponentIds> supportedColorComponentIds = new List<ColorComponentIds>();
            foreach (ColorComponent colorComponent in ColorSwitchSupportData.SupportedColorComponents)
            {
                supportedColorComponentIds.Add(colorComponent.ColorId);
            }
            List<byte> bitmask = CommandClassSupportUtils.CreateBitmaskFromList(supportedColorComponentIds, 0);
            rpt.colorComponentMask[0] = (bitmask.Count == 0) ? (byte) 0x00 : bitmask[0];
            rpt.colorComponentMask[1] = 0x00;
            return rpt;
        }

        private byte[] PrepareColorSwitchReport(byte reqColorComponentId)
        {
            byte[] rpt;

            byte targetValueB = 0;
            byte currentValueB = 0;
            byte durationB = 0;

            if (ColorSwitchSupportData.SupportedColorComponents.Find(x => x.ColorId == (ColorComponentIds)reqColorComponentId) is ColorComponent reqColor)
            {
                currentValueB = reqColor.CurrentValueB;
                targetValueB = reqColor.TargetValueB;
                durationB = CommandClassSupportUtils.SecondsToDuration((uint)Math.Abs(targetValueB - currentValueB) * reqColor.DurationOffOnSec / OnValue);
            }

            if (Version <= 2)
            {
                rpt = new COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_REPORT()
                {
                    colorComponentId = reqColorComponentId,
                    value = currentValueB,
                };
            }
            else
            {
                rpt = new COMMAND_CLASS_SWITCH_COLOR_V3.SWITCH_COLOR_REPORT()
                {
                    colorComponentId = reqColorComponentId,
                    currentValue = currentValueB,
                    targetValue = targetValueB,
                    duration = durationB,
                };
                CommandClassSupportUtils.ReportAddLog(ColorSwitchSupportData.LogLevel,
                            "{0} CC @ {1}: [Report] Component: {2} - CurrentValue: 0x{3:X2} ({3,3:##0}) - TargetValue: 0x{4:X2} ({4,3:##0}) - Duration: {5}",
                            CommandClassShortName, EndPointName, reqColorComponentId,
                            currentValueB, targetValueB, CommandClassSupportUtils.DurationToSeconds(durationB));
            }
            //ReportOperationState(-1, "ColorReport");
            ReportOperationState(reqColorComponentId, "ColorRep");
            return rpt;
        }

        private byte[] PrepareColorSwitchReportCmd(byte[] command)
        {
            COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_GET colorSwitchGetCmd = command;
            byte reqColorComponentId = colorSwitchGetCmd.colorComponentId;
            return PrepareColorSwitchReport(reqColorComponentId);
        }

        private SvResultNames HandleSingleColorSet(byte colorComponentId, byte targetValueB, uint durationTransitionSec, bool updateUI, NodeTag srcNode)
        {
            SvResultNames success = SvResultNames.Successful;

            if (ColorSwitchSupportData.SupportedColorComponents.Find(x => x.ColorId == (ColorComponentIds)colorComponentId) is ColorComponent currentColor)
            {
                byte currentValueB = currentColor.CurrentValueB;

                if (Version <= 2)
                {   // Patch the requested transition for v1/v2 because the TargetValueB member is no longer
                    // supported from EDE UI and now CurrentValueB and targetValueB hold the target value to set.
                    // TargetValueB at this point still holds the previous CurrentValueB.
                    // Handling with local variables is necessary to avoid visibility of manipulations in the EDE UI.
                    currentValueB = currentColor.TargetValueB;
                }

                // Cancel a possibly running transition
                currentColor.TargetValueB = currentValueB;
                currentColor.StepWidthF = 0;
                currentColor.DurationTransitionSec = 0;
                currentColor.DurationOffOnSec = 0;
                currentColor.CmdSrcNode = NodeTag.Empty;

                if (targetValueB != currentValueB)
                {
                    if (durationTransitionSec == 0)
                    {   // Instantly
                        currentColor.CurrentValueB = targetValueB;
                        currentColor.TargetValueB = targetValueB;
                        if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                        {
                            SendUnsolicitedReport(PrepareColorSwitchReport((byte)currentColor.ColorId), srcNode);
                        }
                        if (updateUI)
                        {
                            SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about the current state
                        }
                    }
                    else
                    {   // Create a new transition
                        currentColor.StepWidthF = (float)(targetValueB - currentValueB) / (durationTransitionSec * TickerPerSec);
                        currentColor.CurrentValueF = currentValueB;
                        currentColor.DurationTransitionSec = durationTransitionSec;
                        currentColor.DurationOffOnSec = OnValue * durationTransitionSec / (uint)Math.Abs(targetValueB - currentValueB);
                        currentColor.CmdSrcNode = srcNode;
                    }
                }
                currentColor.TargetValueB = targetValueB;
                ReportOperationState(colorComponentId, "ColorSet");
            }
            else
            {
                CommandClassSupportUtils.ReportAddLog(ColorSwitchSupportData.LogLevel,
                            "{0} CC @ {1}: Invalid Color Component ID {2} in Set command.",
                            CommandClassShortName, EndPointName, colorComponentId);
                success = SvResultNames.Failed;
            }
            return success;
        }

        private SvResultNames HandleColorSwitchSetCmd(byte[] command, NodeTag srcNode)
        {
            SvResultNames success = SvResultNames.Successful;

            COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_SET colorSwitchSetCmd = command;

            byte requestedDuration = ColorSwitchSupportData.FactoryDefaultSetDuration;
            if (Version >= 2)
            {
                COMMAND_CLASS_SWITCH_COLOR_V2.SWITCH_COLOR_SET colorSwitchSetV2Cmd = command;
                requestedDuration = colorSwitchSetV2Cmd.duration;
                if (requestedDuration == 0xFF)
                {
                    requestedDuration = ColorSwitchSupportData.FactoryDefaultSetDuration;
                }
            }
            uint durationTransitionSec = CommandClassSupportUtils.DurationToSeconds(requestedDuration);

            for (int idx = 0; idx < colorSwitchSetCmd.properties1.colorComponentCount; idx++)
            {
                success = HandleSingleColorSet(colorSwitchSetCmd.vg1[idx].colorComponentId, 
                                               colorSwitchSetCmd.vg1[idx].value, 
                                               durationTransitionSec, true, srcNode);
                if (success != SvResultNames.Successful)
                {
                    break;
                }
            }
            return success;
        }

        #region LevelChangeCmd
        private SvResultNames HandleStartLevelChangeCmd(byte[] command)
        {
            SvResultNames success = SvResultNames.Successful;

            byte requestedDuration = ColorSwitchSupportData.FactoryDefaultLevelDuration;
            if (Version >= 3)
            {
                COMMAND_CLASS_SWITCH_COLOR_V3.SWITCH_COLOR_START_LEVEL_CHANGE colorSwitchV3Cmd = command;
                requestedDuration = colorSwitchV3Cmd.duration;
                if (requestedDuration == 0xFF)
                {
                    requestedDuration = ColorSwitchSupportData.FactoryDefaultLevelDuration;
                }
            }
            uint durationOffOnSec = CommandClassSupportUtils.DurationToSeconds(requestedDuration);

            COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_START_LEVEL_CHANGE colorSwitchStartLevelCmd = command;
            byte colorComponentId = colorSwitchStartLevelCmd.colorComponentId;

            if (ColorSwitchSupportData.SupportedColorComponents.Find(x => x.ColorId == (ColorComponentIds)colorComponentId) is ColorComponent currentColor)
            {
                // Cancel a possibly running transition
                currentColor.TargetValueB = currentColor.CurrentValueB;
                currentColor.StepWidthF = 0;
                currentColor.DurationTransitionSec = 0;
                currentColor.DurationOffOnSec = 0;

                byte upDown = colorSwitchStartLevelCmd.properties1.upDown;
                byte startLevel = colorSwitchStartLevelCmd.startLevel;
                byte ignoreStartLevel = colorSwitchStartLevelCmd.properties1.ignoreStartState;

                byte targetValueB = (upDown == 0x01) ? OffValue : OnValue;
                byte startValueB = (ignoreStartLevel == 0x00) ? startLevel : currentColor.CurrentValueB;
                uint durationTransitionSec = (uint) Math.Abs(startValueB - targetValueB) / OnValue * durationOffOnSec;

                if (targetValueB != startValueB)
                {
                    if (durationOffOnSec == 0)  // Instantly
                    {
                        currentColor.CurrentValueB = targetValueB;
                    }
                    else                        // Create a new transition
                    {
                        currentColor.StepWidthF = (float)((upDown == 0x01) ? -0xFF : +0xFF) / (durationOffOnSec * TickerPerSec);
                        currentColor.CurrentValueF = startValueB;
                        currentColor.DurationTransitionSec = durationTransitionSec;
                        currentColor.DurationOffOnSec = durationOffOnSec;
                    }
                }
                currentColor.TargetValueB = targetValueB;
                ReportOperationState(colorComponentId, "StartLev");
            }
            else
            {
                CommandClassSupportUtils.ReportAddLog(ColorSwitchSupportData.LogLevel, 
                            "{0} CC @ {1}: Invalid Color Component ID {2} in StartLevel command.", 
                            CommandClassShortName, EndPointName, colorComponentId);
                success = SvResultNames.Failed;
            }
            return success;
        }

        private SvResultNames HandleStopLevelChangeCmd(byte[] command)
        {
            COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_STOP_LEVEL_CHANGE colorSwitchCmd = command;

            byte colorComponentId = colorSwitchCmd.colorComponentId;

            if (ColorSwitchSupportData.SupportedColorComponents.Find(x => x.ColorId == (ColorComponentIds)colorComponentId) is ColorComponent currentColor)
            {
                currentColor.TargetValueB = currentColor.CurrentValueB;
                currentColor.StepWidthF = 0;
                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE)
                ReportOperationState(colorComponentId, "StopLev");
                return SvResultNames.Successful;
            }
            CommandClassSupportUtils.ReportAddLog(ColorSwitchSupportData.LogLevel, 
                        "{0} CC @ {1}: Invalid Color Component ID {2} in StopLevel command.", 
                        CommandClassShortName, EndPointName, colorComponentId);
            return SvResultNames.Failed;
        }
        #endregion // LevelChangeCmd

        #region Transition Timer
        private void TransitionTimerStart()
        {
            // ColorSwitchSupportData.CancelPendingTransitionTimer = TransitionTimerReset;
            TransitionTimer.RunWorkerAsync();   // the Transition Timer runs infinitely or until Result.State == ActionStates.Cancelled
        }
        private void TransitionTimerDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            CommandClassSupportUtils.ReportTimerState(ColorSwitchSupportData.LogLevel, 
                        TransitionTimerName, ReportTimerStateNames.Started, TicksElapsed / TickerPerSec);
            while (true)
            {
                TicksElapsed++;

                foreach (ColorComponent colorComponent in ColorSwitchSupportData.SupportedColorComponents)
                {
                    if (colorComponent.StepWidthF != 0)
                    {
                        colorComponent.CurrentValueF += colorComponent.StepWidthF;
                        if ((colorComponent.StepWidthF > 0 && colorComponent.CurrentValueF >= colorComponent.TargetValueB)
                         || (colorComponent.StepWidthF < 0 && colorComponent.CurrentValueF <= colorComponent.TargetValueB) )
                        {
                            colorComponent.CurrentValueB = colorComponent.TargetValueB;
                            colorComponent.StepWidthF = 0;
                            colorComponent.DurationTransitionSec = 0;
                            ReportOperationState(ColorSwitchSupportData.SupportedColorComponents.IndexOf(colorComponent), "Elapsed");
                            if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                            {
                                DeviceRestart(colorComponent.CmdSrcNode);
                                SendUnsolicitedReport(PrepareColorSwitchReport((byte)colorComponent.ColorId), colorComponent.CmdSrcNode);
                                SupportDelegates?.DeviceSleep(this, ReportWakeupTimeout);
                            }
                            SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE)
                        }
                        else
                        {
                            colorComponent.CurrentValueB = Convert.ToByte(colorComponent.CurrentValueF);
                            ReportOperationState(ColorSwitchSupportData.SupportedColorComponents.IndexOf(colorComponent), "Tick");
                        }
                    }
                }

                if (Result.State == ActionStates.Cancelled)
                {
                    TransitionTimerReset();
                }
                Thread.Sleep((int) (1000 / TickerPerSec) );
                if (bw.CancellationPending)
                {
                    CommandClassSupportUtils.ReportTimerState(ColorSwitchSupportData.LogLevel, 
                                TransitionTimerName, ReportTimerStateNames.CancelledElapsedSec, TicksElapsed / TickerPerSec);
                    e.Cancel = true;
                    break;
                }
            }
            if (!e.Cancel)
            {
                CommandClassSupportUtils.ReportTimerState(ColorSwitchSupportData.LogLevel, 
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
                CommandClassSupportUtils.ReportTimerState(ColorSwitchSupportData.LogLevel,
                            TransitionTimerName, ReportTimerStateNames.Reset, 0);
                TransitionTimer.CancelAsync();
                _resetEvent.WaitOne();
            }
        }
        #endregion // Transition Timer

        private void ReportOperationState(int colorId, string info)
        {
            if (ColorSwitchSupportData.LogLevel > 0)
            {
                foreach (ColorComponent colorComponent in ColorSwitchSupportData.SupportedColorComponents)
                {
                    if (colorId == (int) colorComponent.ColorId || colorId == -1)
                    {
                        CommandClassSupportUtils.ReportAddLog(ColorSwitchSupportData.LogLevel, 
                        "{0} CC @ {1}: [{2,-8}] Component: {3,-9} ({4}) - Current: 0x{5:X2} ({5,3:##0}) - Target: 0x{6:X2} ({6,3:##0}) - StepWidth: {7}",
                                    CommandClassShortName, EndPointName, info, colorComponent.ColorId, (byte) colorComponent.ColorId,
                                    colorComponent.CurrentValueB,
                                    colorComponent.TargetValueB,
                                    colorComponent.StepWidthF);
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
                    case COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_SET.ID:
                        if (ResultForSv == SvResultNames.Successful)
                        {
                            supervisionReport.status = (byte)SvStatusNames.Success;
                        }
                        break;
                    case COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_START_LEVEL_CHANGE.ID:
                        if (ResultForSv == SvResultNames.Successful)
                        {
                            supervisionReport.status = (byte)SvStatusNames.Success;
                        }
                        break;
                    case COMMAND_CLASS_SWITCH_COLOR.SWITCH_COLOR_STOP_LEVEL_CHANGE.ID:
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
            if (data is ColorSwitchSupportData newData)
            {
                TransitionTimerName = string.Format("{0} CC @ {1}: Transition Timer", CommandClassShortName, EndPointName);

                bool identicalCapabilities = true;
                if (ColorSwitchSupportData.FactoryDefaultSetDuration != newData.FactoryDefaultSetDuration
                 || ColorSwitchSupportData.FactoryDefaultLevelDuration != newData.FactoryDefaultLevelDuration)
                {
                    identicalCapabilities = false;
                }

                bool identicalSupportedColors = ColorSwitchSupportData.SupportedColorComponents.Count == newData.SupportedColorComponents.Count;
                if (identicalSupportedColors)
                {
                    foreach (ColorComponent newColor in newData.SupportedColorComponents)
                    {
                        if (ColorSwitchSupportData.SupportedColorComponents.FindIndex(x => x.ColorId == newColor.ColorId) == -1)
                        {
                            identicalSupportedColors = false;
                            break;
                        }
                    }
                }

                if (!identicalCapabilities)
                {
                    ColorSwitchSupportData.FactoryDefaultSetDuration = newData.FactoryDefaultSetDuration;
                    ColorSwitchSupportData.FactoryDefaultLevelDuration = newData.FactoryDefaultLevelDuration;
                    //if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    //    SendUnsolicitedReport(PrepareSupportedReport());
                }
                ColorSwitchSupportData.SupportedColorComponents = newData.SupportedColorComponents;
                if (!identicalSupportedColors)
                {
                    if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        SendUnsolicitedReport(PrepareSupportedReport(), NodeTag.Empty);
                    }
                }
                foreach (ColorComponent newColor in ColorSwitchSupportData.SupportedColorComponents)
                {
                    if (newColor.CurrentValueB != newColor.TargetValueB)
                    {
                        if (Version == 3)
                        {
                            HandleSingleColorSet((byte)newColor.ColorId, newColor.TargetValueB, newColor.DurationTransitionSec, false, NodeTag.Empty);
                        }
                        if (Version <= 2)
                        {   // transition for v1/v2 is based on CurrentValue, because TargetValueB member is no longer supported from UI
                            HandleSingleColorSet((byte)newColor.ColorId, newColor.CurrentValueB, newColor.DurationTransitionSec, false, NodeTag.Empty);
                        }
                    }
                }
                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class ColorComponent : INestedItem
    {
        //[CustomDataParameter(1, "Color Id", CommandClassesImpl.UiControl.ListComboBox, CommandClassesImpl.InputType.Enum, EditType.Config /*| EditType.View*/ | EditType.Edit, typeof(ColorComponentIds))]
        public ColorComponentIds ColorId { get; set; }
        [CustomDataParameter(1, 2, "Value", UiControl.TextBox, InputType.Int_0_255, EditType.Config /*| EditType.View*/ | EditType.Edit)]
        public byte ValueB
        {
            get { return CurrentValueB; }
            set { CurrentValueB = value; }
        }        
        internal float CurrentValueF { get; set; } = 0;
        [CustomDataParameter(3, "Current Value", UiControl.TextBox, InputType.Int_0_255, EditType.Config /*| EditType.View*/ /*| EditType.Edit*/)]
        public byte CurrentValueB { get; set; } = 0;
        [CustomDataParameter(3, "Target Value", UiControl.TextBox, InputType.Int_0_255, EditType.Config /*| EditType.View*/ | EditType.Edit)]
        public byte TargetValueB { get; set; } = 0;
        internal float StepWidthF { get; set; } = 0;
        [CustomDataParameter(2, "Duration (sec)", UiControl.TextBox, InputType.DurationSec, EditType.Config /*| EditType.View*/ | EditType.Edit)]
        public uint DurationTransitionSec { get; set; } = 0;
        internal uint DurationOffOnSec { get; set; } = 0;
        internal NodeTag CmdSrcNode { get; set; } = NodeTag.Empty;

        // only used for EDE UI. see interface for useage notes.
        [JsonIgnore]
        public int NestedId
        {
            get => (byte) ColorId;
            set { }
        }

        /*public ColorComponent()
        {
        }*/

        public ColorComponent(ColorComponentIds colorComponentId)
        {
            ColorId = colorComponentId;
        }
        public static List<ColorComponentIds> GetAllSupportedColorComponentIds()
        {
            List<ColorComponentIds> allSupportedColorComponentIds = new List<ColorComponentIds>();
            for (byte id = 0x00; id <= 0x07; id++)
            {
                allSupportedColorComponentIds.Add((ColorComponentIds) id);
            }
            return allSupportedColorComponentIds;
        }
    };

    public class ColorSwitchSupportData : CommandClassSupportData, INodeItem
    {
        // For the encoding of duration values see SDS13781 (0=instantly, 1-127=seconds, 128-254=minutes-127, 255=Factory Default Duration)
        public byte FactoryDefaultSetDuration { get; set; } = 0;    // Default Duration for a Set command transition, used for all Color Component IDs.
        public byte FactoryDefaultLevelDuration { get; set; } = 32; // Default Duration for a transition from 0% to 100% (255), used for all Color Component IDs.

        // V1+
        // Color Components: Configuration and Runtime Data
        // Supported Color Component IDs 0 to 7 are allowed to support, 8 to 15 are reserved and MUST NOT be used. Default: none
        // Usage: .ColorComponents = new List<ColorComponent> { new ColorComponent() { ColorId = ColorComponentIds.Purple, }, ... };
        // Possible values: WarmWhite, ColdWhite, Red, Green, Blue, Amber, Cyan, Purple
        // Public Runtime Data are initially set to 0
        [JsonIgnore]
        public IEnumerable<INestedItem> ChildItems => SupportedColorComponents;
        [NestedControl(1, "SupportedColorComponents")]
        public List<ColorComponent> SupportedColorComponents { get; set; } = new List<ColorComponent>();

        //public bool IsTimerStarted { get; set; } = false;   // EDE: MOVED TO CommandClassSupport! This value is maintained by ColorSwitchSupport(). Do not change!
    }

    public enum ColorComponentIds
    {
        [Description("0x00 Warm White")]
        WarmWhite = 0x00,
        [Description("0x01 Cold White")]
        ColdWhite = 0x01,
        [Description("0x02 Red")]
        Red = 0x02,
        [Description("0x03 Green")]
        Green = 0x03,
        [Description("0x04 Blue")]
        Blue = 0x04,
        [Description("0x05 Amber")]
        Amber = 0x05,
        [Description("0x06 Cyan")]
        Cyan = 0x06,
        [Description("0x07 Purple")]
        Purple = 0x07
    }
}
