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
using Newtonsoft.Json;
using Zats.Services.CommandClassesImpl;

// ToDo: LearnedCodeReadback Get/Report (LCR)
// ToDo: RepeatLearntCode (RLC)
// ToDo: Repeat (IRR)
// ToDo: Supervision status 'Working'

namespace Zats.Services
{
    [CommandClassName("IR Repeater Command Class")]
    [CommandClassId(COMMAND_CLASS_IR_REPEATER.ID)]
    [MinVersion(1)]
    [MaxVersion(1)]
    public class IrRepeaterSupport : CommandClassSupport
    {
        private IrRepeaterSupportData IrRepeaterSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return IrRepeaterSupportData; }
            set { IrRepeaterSupportData = (IrRepeaterSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private readonly BackgroundWorker LearningTimer = new BackgroundWorker();
        private string LearningTimerName = "";
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        private byte CurrentLearningCode = 0;
        private byte CurrentLearningTimeout = 0;
        private byte RemainingLearningTime = 0;
        private IrRepeaterLearningStatusNames CurrentLearningStatus;

        private NodeTag CmdSrcNode = NodeTag.Empty;

        public IrRepeaterSupport()
            : this(new IrRepeaterSupportData() { })
        { }

        public IrRepeaterSupport(IrRepeaterSupportData irRepeaterSupportData)
            : base(COMMAND_CLASS_IR_REPEATER.ID)
        {
            IrRepeaterSupportData = irRepeaterSupportData;

            LearningTimer.DoWork += new DoWorkEventHandler(LearningTimerDoWork);
            LearningTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(LearningTimerRunWorkerCompleted);
            LearningTimer.WorkerSupportsCancellation = true;
            LearningTimer.WorkerReportsProgress = false;

            IrRepeaterSupportData.IrRepeatCommandSupported = false;     // force IRR not yet supported
            IrRepeaterSupportData.LearntCodeReadbackSupported = false;  // force LCR not yet supported
            IrRepeaterSupportData.RepeatLearntCodeSupported  = false;   // force RLC not yet supported

            if (IrRepeaterSupportData.LearntCodeReadbackSupported)      // LCR
            {
                IrRepeaterSupportData.NumberOfIrCodeIdentifiersForLearning = Math.Max((byte) 1, IrRepeaterSupportData.NumberOfIrCodeIdentifiersForLearning);
                IrRepeaterSupportData.IrRepeatCommandSupported = true;
            }

            if (IrRepeaterSupportData.RepeatLearntCodeSupported)        // RLC
            {
                IrRepeaterSupportData.NumberOfIrCodeIdentifiersForLearning = Math.Max((byte)1, IrRepeaterSupportData.NumberOfIrCodeIdentifiersForLearning);
                IrRepeaterSupportData.IrRepeatCommandSupported = true;
            }
            if (!IrRepeaterSupportData.PulseEncodingSupported)
            {
                IrRepeaterSupportData.MinimumPulseTimeUnit = 0;
                IrRepeaterSupportData.MaximumPulseTimeUnit = 0;
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

                LearningTimerName = string.Format("{0}{1}: Learning Timeout", CommandClassName, EndPointName);
                ResultForSv = SvResultNames.Undefined;
                if (isHandlingAllowed && command != null && command.Length > 1 && IrRepeaterSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_IR_REPEATER.IR_REPEATER_CAPABILITIES_GET.ID)
                    {
                        rpt = PrepareCapabilitiesReport();
                    }
                    else if (command[1] == COMMAND_CLASS_IR_REPEATER.IR_REPEATER_IR_CODE_LEARNING_START.ID)
                    {
                        ResultForSv = HandleLearningStartCmd(command, sendSrcNode);
                    }
                    else if (command[1] == COMMAND_CLASS_IR_REPEATER.IR_REPEATER_IR_CODE_LEARNING_STOP.ID)
                    {
                        ResultForSv = HandleLearningStopCmd();
                        if (IrRepeaterSupportData.SendLearningStatusReport)
                        {
                            rpt = PrepareLearningStatusReport();
                        }
                        CurrentLearningCode = 0;
                    }
                    else if (command[1] == COMMAND_CLASS_IR_REPEATER.IR_REPEATER_LEARNT_IR_CODE_REMOVE.ID)
                    {
                        ResultForSv = HandleCodeRemoveCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_IR_REPEATER.IR_REPEATER_LEARNT_IR_CODE_GET.ID)
                    {
                        rpt = PrepareLearntCodesReport();
                    }
                    else if (command[1] == COMMAND_CLASS_IR_REPEATER.IR_REPEATER_LEARNT_IR_CODE_READBACK_GET.ID)
                    {
                        // not yet supported
                    }
                    else if (command[1] == COMMAND_CLASS_IR_REPEATER.IR_REPEATER_CONFIGURATION_SET.ID)
                    {
                        ResultForSv = HandleConfigurationSetCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_IR_REPEATER.IR_REPEATER_CONFIGURATION_GET.ID)
                    {
                        rpt = PrepareConfigurationReport();
                    }
                    else if (command[1] == COMMAND_CLASS_IR_REPEATER.IR_REPEATER_REPEAT_LEARNT_CODE.ID)
                    {
                        // not yet supported
                    }
                    else if (command[1] == COMMAND_CLASS_IR_REPEATER.IR_REPEATER_REPEAT.ID)
                    {
                        // not yet supported
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

        private byte[] PrepareCapabilitiesReport()
        {
            COMMAND_CLASS_IR_REPEATER.IR_REPEATER_CAPABILITIES_REPORT rpt = new COMMAND_CLASS_IR_REPEATER.IR_REPEATER_CAPABILITIES_REPORT();
            rpt.numberOfIrCodeIdentifiersForLearning = IrRepeaterSupportData.NumberOfIrCodeIdentifiersForLearning;
            rpt.properties1.irr = (byte) (IrRepeaterSupportData.IrRepeatCommandSupported ? 0x01 : 0x00);
            rpt.properties1.lcr = (byte) (IrRepeaterSupportData.LearntCodeReadbackSupported ? 0x01 : 0x00);
            rpt.properties1.rlc = (byte) (IrRepeaterSupportData.RepeatLearntCodeSupported ? 0x01 : 0x00);
            rpt.properties1.pes = (byte) (IrRepeaterSupportData.PulseEncodingSupported ? 0x01 : 0x00);
            rpt.properties1.bes = (byte) (IrRepeaterSupportData.BitEncodingSupported ? 0x01 : 0x00);
            rpt.properties1.dutyCycleBitmask = IrRepeaterSupportData.DutyCycleBitmask;
            rpt.carrier = IrRepeaterSupportData.Carrier;
            rpt.minimumSubCarrier = IrRepeaterSupportData.MinimumSubCarrier;
            rpt.maxiumSubCarrier = IrRepeaterSupportData.MaximumSubCarrier;
            rpt.minimumPulseTimeUnitMsb = (byte) (IrRepeaterSupportData.MinimumPulseTimeUnit >> 4);
            rpt.properties2.minimumPulseTimeUnitLsb = (byte) (IrRepeaterSupportData.MinimumPulseTimeUnit & 0x0F);
            rpt.properties2.maximumPulseTimeUnitMsb = (byte) (IrRepeaterSupportData.MaximumPulseTimeUnit >> 8);
            rpt.maximumPulseTimeUnitLsb = (byte) (IrRepeaterSupportData.MaximumPulseTimeUnit & 0xFF);
            return rpt;
        }
        private byte[] PrepareLearningStatusReport()
        {
            COMMAND_CLASS_IR_REPEATER.IR_REPEATER_IR_CODE_LEARNING_STATUS rpt = new COMMAND_CLASS_IR_REPEATER.IR_REPEATER_IR_CODE_LEARNING_STATUS();
            rpt.irCodeIdentifier = CurrentLearningCode;
            rpt.status = (byte) CurrentLearningStatus;
            return rpt;
        }

        private byte[] PrepareLearntCodesReport()
        {
            if (IrRepeaterSupportData.NumberOfIrCodeIdentifiersForLearning > 0)
            {
                COMMAND_CLASS_IR_REPEATER.IR_REPEATER_LEARNT_IR_CODE_REPORT rpt = new COMMAND_CLASS_IR_REPEATER.IR_REPEATER_LEARNT_IR_CODE_REPORT();
                rpt.irCodeIdentifierStatusBitmask = CommandClassSupportUtils.CreateBitmaskFromList(IrRepeaterSupportData.LearntIrCodesList, 1);   // Bit 0 represents code #1
                rpt.properties1.irCodeIdenfierStatusLength = (byte)rpt.irCodeIdentifierStatusBitmask.Count;
                return rpt;
            }
            return null;
        }

        private SvResultNames HandleLearningStartCmd(byte[] command, NodeTag srcNode)
        {
            COMMAND_CLASS_IR_REPEATER.IR_REPEATER_IR_CODE_LEARNING_START startCmd = command;
            byte irCodeId = startCmd.irCodeIdentifier;

            if (irCodeId > 0 && irCodeId <= IrRepeaterSupportData.NumberOfIrCodeIdentifiersForLearning)
            {
                //ushort pulseTimeUnit = (ushort)((setCmd.properties1.pulseTimeUnitMsb << 8) + setCmd.pulseTimeUnitLsb);
                //pulseTimeUnit = Math.Max(pulseTimeUnit, IrRepeaterSupportData.MinimumPulseTimeUnit);    // Lower or higher values must be ignored
                //pulseTimeUnit = Math.Min(pulseTimeUnit, IrRepeaterSupportData.MaximumPulseTimeUnit);
                CurrentLearningCode = irCodeId;
                CurrentLearningTimeout = startCmd.timeout;
                CommandClassSupportUtils.ReportAddLog(IrRepeaterSupportData.LogLevel, 
                            "{0} CC @ {1}: Learning Start: Code {2} and Timeout {3} accepted",
                            CommandClassShortName, EndPointName, CurrentLearningCode, CurrentLearningTimeout);
                CmdSrcNode = srcNode;
                LearningTimerName = string.Format("{0} CC @ {1}: Learning Timer", CommandClassShortName, EndPointName);
                LearningTimerStart();
                return SvResultNames.Successful;
            }
            CommandClassSupportUtils.ReportAddLog(IrRepeaterSupportData.LogLevel, 
                        "{0} CC @ {1}: Learning Start: Code {2}, Timeout {3}, command ignored",
                        CommandClassShortName, EndPointName, CurrentLearningCode, CurrentLearningTimeout);
            return SvResultNames.Failed;
        }

        private SvResultNames HandleLearningStopCmd()
        {
            if (IrRepeaterSupportData.NumberOfIrCodeIdentifiersForLearning > 0)
            {
                CommandClassSupportUtils.ReportAddLog(IrRepeaterSupportData.LogLevel, 
                            "{0} CC @ {1}: Learning Stop accepted", CommandClassShortName, EndPointName);
                LearningTimerCancel();
                CurrentLearningStatus = IrRepeaterLearningStatusNames.FailStopped;
                return SvResultNames.Successful;
            }
            CommandClassSupportUtils.ReportAddLog(IrRepeaterSupportData.LogLevel, 
                        "{0} CC @ {1}: Learning Stop ignored", CommandClassShortName, EndPointName);
            return SvResultNames.Failed;
        }

        private SvResultNames HandleCodeRemoveCmd(byte[] command)
        {
            COMMAND_CLASS_IR_REPEATER.IR_REPEATER_LEARNT_IR_CODE_REMOVE removeCmd = command;
            byte requestedCode = removeCmd.irCodeIdentifier;
            if (IrRepeaterSupportData.NumberOfIrCodeIdentifiersForLearning > 0)
            {
                if (requestedCode == 0x00)
                {
                    IrRepeaterSupportData.LearntIrCodesList = new List<byte>();
                    CommandClassSupportUtils.ReportAddLog(IrRepeaterSupportData.LogLevel, 
                                "{0} CC @ {1}: Code Remove: All codes removed", CommandClassShortName, EndPointName);
                    return SvResultNames.Successful;
                }
                if (requestedCode <= IrRepeaterSupportData.NumberOfIrCodeIdentifiersForLearning && IrRepeaterSupportData.LearntIrCodesList.Contains(requestedCode))
                {
                    IrRepeaterSupportData.LearntIrCodesList.Remove(requestedCode);
                    CommandClassSupportUtils.ReportAddLog(IrRepeaterSupportData.LogLevel, 
                                "{0} CC @ {1}: Code Remove: Code {2} removed", CommandClassShortName, EndPointName, requestedCode);
                    return SvResultNames.Successful;
                }
            }
            CommandClassSupportUtils.ReportAddLog(IrRepeaterSupportData.LogLevel, 
                        "{0} CC @ {1}: Code Remove command for Code {2} ignored", CommandClassShortName, EndPointName, requestedCode);
            return SvResultNames.Failed;
        }

        private SvResultNames HandleConfigurationSetCmd(byte[] command)
        {
            COMMAND_CLASS_IR_REPEATER.IR_REPEATER_CONFIGURATION_SET setCmd = command;
            if (IrRepeaterSupportData.BitEncodingSupported)
            {
                if (setCmd.vg1.Count >= 2)
                {
                    IrRepeaterSupportData.Bit0EncodingLength = CommandClassSupportUtils.ByteFieldToInt64(setCmd.vg1[0].dataBitEncodingLength, 2);
                    IrRepeaterSupportData.Bit0Encoding = setCmd.vg1[0].dataBitEncoding.ToArray();
                    IrRepeaterSupportData.Bit1EncodingLength = CommandClassSupportUtils.ByteFieldToInt64(setCmd.vg1[1].dataBitEncodingLength, 2);
                    IrRepeaterSupportData.Bit1Encoding = setCmd.vg1[1].dataBitEncoding.ToArray();
                }
                else
                {
                    // Workaround for wrong decoding of {vg1}
                    byte[] tmp;
                    int idx;
                    tmp = new byte[] { command[4], command[5] };
                    IrRepeaterSupportData.Bit0EncodingLength = CommandClassSupportUtils.ByteFieldToInt64(tmp.ToArray(), 2);
                    tmp = new byte[] { };
                    idx = 6;
                    for (int i = 0; i < ((IrRepeaterSupportData.Bit0EncodingLength - 1) / 8) + 1; i++)
                    {
                        tmp[i] = command[idx++];
                    }
                    IrRepeaterSupportData.Bit0Encoding = tmp;
                    tmp = new byte[] { command[idx], command[idx + 1] };
                    IrRepeaterSupportData.Bit1EncodingLength = CommandClassSupportUtils.ByteFieldToInt64(tmp.ToArray(), 2);
                    tmp = new byte[] { };
                    idx += 2;
                    for (int i = 0; i < ((IrRepeaterSupportData.Bit1EncodingLength - 1) / 8) + 1; i++)
                    {
                        tmp[i] = command[idx++];
                    }
                    IrRepeaterSupportData.Bit1Encoding = tmp;
                }
                IrRepeaterSupportData.Period = (setCmd.periodMsb << 4) + setCmd.properties1.periodLsb;
                if (IrRepeaterSupportData.Period >= 1 && IrRepeaterSupportData.Period <= 4096
                    && IrRepeaterSupportData.Bit0Encoding.Length * 8 >= IrRepeaterSupportData.Bit0EncodingLength
                    && IrRepeaterSupportData.Bit1Encoding.Length * 8 >= IrRepeaterSupportData.Bit1EncodingLength)
                {
                    return SvResultNames.Successful;
                }
            }
            return SvResultNames.Failed;
        }

        private byte[] PrepareConfigurationReport()
        {
            COMMAND_CLASS_IR_REPEATER.IR_REPEATER_CONFIGURATION_REPORT rpt = new COMMAND_CLASS_IR_REPEATER.IR_REPEATER_CONFIGURATION_REPORT();
            if (IrRepeaterSupportData.BitEncodingSupported)
            {
                rpt.periodMsb = (byte) (IrRepeaterSupportData.Period >> 4);
                rpt.properties1.periodLsb = (byte) (IrRepeaterSupportData.Period & 0x0F);
                rpt.vg1.Add(new COMMAND_CLASS_IR_REPEATER.IR_REPEATER_CONFIGURATION_REPORT.TVG1());
                rpt.vg1.Add(new COMMAND_CLASS_IR_REPEATER.IR_REPEATER_CONFIGURATION_REPORT.TVG1());
                rpt.vg1[0].dataBitEncodingLength = CommandClassSupportUtils.Int64ToByteField(IrRepeaterSupportData.Bit0EncodingLength, 2);
                rpt.vg1[0].dataBitEncoding = IrRepeaterSupportData.Bit0Encoding;
                rpt.vg1[1].dataBitEncodingLength = CommandClassSupportUtils.Int64ToByteField(IrRepeaterSupportData.Bit1EncodingLength, 2);
                rpt.vg1[1].dataBitEncoding = IrRepeaterSupportData.Bit1Encoding;
                return rpt;
            }
            return null;
        }

        #region Learning Timer
        private void LearningTimerStart()
        {
            IrRepeaterSupportData.CancelPendingLearningTimer = LearningTimerReset;
            LearningTimer.RunWorkerAsync();
        }
        private void LearningTimerDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            byte learningTime = CurrentLearningTimeout;

            if (learningTime > 0)
            {
                ReportOperationProgress(LearningTimerName, ReportTimerStateNames.Started, learningTime);
                if (!IrRepeaterSupportData.FailLearning && learningTime >= 2)
                {
                    learningTime /= 2;  // Simulate Success before Timeout occurs
                }
                for (RemainingLearningTime = learningTime; RemainingLearningTime > 0; RemainingLearningTime--)
                {
                    Thread.Sleep(1000);
                    if (bw.CancellationPending)
                    {
                        ReportOperationProgress(LearningTimerName, ReportTimerStateNames.CancelledRemainSec, RemainingLearningTime);
                        e.Cancel = true;
                        break;
                    }
                }
                if (!e.Cancel)
                {
                    ReportOperationProgress(LearningTimerName, ReportTimerStateNames.ExpiredSec, learningTime);
                    if (IrRepeaterSupportData.FailLearning)
                    {
                        CurrentLearningStatus = IrRepeaterLearningStatusNames.FailTimeout;
                    }
                    else
                    {
                        CurrentLearningStatus = IrRepeaterLearningStatusNames.Success;
                        IrRepeaterSupportData.LearntIrCodesList.Add(CurrentLearningCode);
                    }
                    if (IrRepeaterSupportData.SendLearningStatusReport)
                    {
                        byte[] rpt = PrepareLearningStatusReport();
                        // CommandClassSupportUtils.AddUnsolicitedFrame(rpt, new byte[] { (byte)ReceivedAchData.SrcNode.Id }, TxOptions, (byte)ReceivedAchData.SecurityScheme);
                        DeviceRestart(CmdSrcNode);
                        SendUnsolicitedReport(rpt, CmdSrcNode);
                        SupportDelegates?.DeviceSleep(this, ReportWakeupTimeout);
                        CommandClassSupportUtils.ReportAddLog(IrRepeaterSupportData.LogLevel, 
                                    "{0} CC @ {1}: Learning Status Report '{2}' added to queue",
                                    CommandClassShortName, EndPointName, CurrentLearningStatus);
                    }
                }
                else
                {
                    CurrentLearningStatus = IrRepeaterLearningStatusNames.FailStopped; // already set in HandleLearningStop() for Status Report
                }
                CommandClassSupportUtils.ReportAddLog(IrRepeaterSupportData.LogLevel, 
                            "{0} CC @ {1}: Code {2} Learning Status: '{3}' = 0x{4:X2}", 
                            CommandClassShortName, EndPointName, CurrentLearningCode, CurrentLearningStatus, (byte) CurrentLearningStatus);
                RemainingLearningTime = 0;
                CurrentLearningTimeout = 0;
                CurrentLearningCode = 0;

                if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                {
                    DeviceRestart(CmdSrcNode);
                    SendUnsolicitedReport(PrepareLearningStatusReport(), CmdSrcNode);
                    SupportDelegates?.DeviceSleep(this, ReportWakeupTimeout);
                }
                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
            }
        }
        private void LearningTimerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _resetEvent.Set();
            }
        }
        private void LearningTimerCancel()
        {
            IrRepeaterSupportData.CancelPendingLearningTimer?.Invoke();
            IrRepeaterSupportData.CancelPendingLearningTimer = null;
        }
        // This method MUST only be called via <CommandClass>SupportData.CancelPending<CommandName>.Invoke();
        private void LearningTimerReset()
        {
            if (LearningTimer.IsBusy)
            {
                ReportOperationProgress(LearningTimerName, ReportTimerStateNames.Reset, 0);
                LearningTimer.CancelAsync();
                _resetEvent.WaitOne();
            }
        }
        #endregion // Learning Timer

        private void ReportOperationProgress(string timerName, ReportTimerStateNames timerState, int timerValue)
        {
            CommandClassSupportUtils.ReportTimerState(IrRepeaterSupportData.LogLevel, timerName, timerState, timerValue);
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
                case COMMAND_CLASS_IR_REPEATER.IR_REPEATER_CONFIGURATION_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    }
                    break;
                case COMMAND_CLASS_IR_REPEATER.IR_REPEATER_IR_CODE_LEARNING_START.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    }
                    break;
                case COMMAND_CLASS_IR_REPEATER.IR_REPEATER_IR_CODE_LEARNING_STOP.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    }
                    break;
                case COMMAND_CLASS_IR_REPEATER.IR_REPEATER_LEARNT_IR_CODE_REMOVE.ID:
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
            if (data is IrRepeaterSupportData newData)
            {
                if (IrRepeaterSupportData.NumberOfIrCodeIdentifiersForLearning != newData.NumberOfIrCodeIdentifiersForLearning
                 || IrRepeaterSupportData.IrRepeatCommandSupported != newData.IrRepeatCommandSupported
                 || IrRepeaterSupportData.LearntCodeReadbackSupported != newData.LearntCodeReadbackSupported
                 || IrRepeaterSupportData.RepeatLearntCodeSupported != newData.RepeatLearntCodeSupported
                 || IrRepeaterSupportData.BitEncodingSupported != newData.BitEncodingSupported
                 || IrRepeaterSupportData.DutyCycleBitmask != newData.DutyCycleBitmask
                 || IrRepeaterSupportData.Carrier != newData.Carrier
                 || IrRepeaterSupportData.MinimumSubCarrier != newData.MinimumSubCarrier
                 || IrRepeaterSupportData.MaximumSubCarrier != newData.MaximumSubCarrier
                 || IrRepeaterSupportData.MinimumPulseTimeUnit != newData.MinimumPulseTimeUnit
                 || IrRepeaterSupportData.MaximumPulseTimeUnit != newData.MaximumPulseTimeUnit)
                {
                    IrRepeaterSupportData.NumberOfIrCodeIdentifiersForLearning = newData.NumberOfIrCodeIdentifiersForLearning;
                    IrRepeaterSupportData.IrRepeatCommandSupported = newData.IrRepeatCommandSupported;
                    IrRepeaterSupportData.LearntCodeReadbackSupported = newData.LearntCodeReadbackSupported;
                    IrRepeaterSupportData.RepeatLearntCodeSupported = newData.RepeatLearntCodeSupported;
                    IrRepeaterSupportData.BitEncodingSupported = newData.BitEncodingSupported;
                    IrRepeaterSupportData.DutyCycleBitmask = newData.DutyCycleBitmask;
                    IrRepeaterSupportData.Carrier = newData.Carrier;
                    IrRepeaterSupportData.MinimumSubCarrier = newData.MinimumSubCarrier;
                    IrRepeaterSupportData.MaximumSubCarrier = newData.MaximumSubCarrier;
                    IrRepeaterSupportData.MinimumPulseTimeUnit = newData.MinimumPulseTimeUnit;
                    IrRepeaterSupportData.MaximumPulseTimeUnit = newData.MaximumPulseTimeUnit;
                    if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        SendUnsolicitedReport(PrepareCapabilitiesReport(), NodeTag.Empty);
                    }
                }

                // The Learning Status Report doesn't use any data class members.
                // rpt.irCodeIdentifier = CurrentLearningCode;
                // rpt.status = (byte) CurrentLearningStatus;
                // if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                //   SendUnsolicitedReport(PrepareLearningStatusReport());

                if (!CommandClassSupportUtils.ListElementsEqual(IrRepeaterSupportData.LearntIrCodesList, newData.LearntIrCodesList))
                {
                    IrRepeaterSupportData.LearntIrCodesList = newData.LearntIrCodesList;
                    if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                    {
                        SendUnsolicitedReport(PrepareLearntCodesReport(), NodeTag.Empty);
                    }
                }

                if (IrRepeaterSupportData.Period != newData.Period
                    || IrRepeaterSupportData.Bit0EncodingLength != newData.Bit0EncodingLength
                    || IrRepeaterSupportData.Bit1EncodingLength != newData.Bit1EncodingLength
                    || IrRepeaterSupportData.Bit0Encoding != null && !IrRepeaterSupportData.Bit0Encoding.SequenceEqual(newData.Bit0Encoding)
                    || IrRepeaterSupportData.Bit1Encoding != null && !IrRepeaterSupportData.Bit1Encoding.SequenceEqual(newData.Bit1Encoding))
                {
                    IrRepeaterSupportData.Period = newData.Period;
                    IrRepeaterSupportData.Bit0EncodingLength = newData.Bit0EncodingLength;
                    IrRepeaterSupportData.Bit1EncodingLength = newData.Bit1EncodingLength;
                    IrRepeaterSupportData.Bit0Encoding = newData.Bit0Encoding;
                    IrRepeaterSupportData.Bit1Encoding = newData.Bit1Encoding;
                    if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        SendUnsolicitedReport(PrepareConfigurationReport(), NodeTag.Empty);
                    }
                }

                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class IrRepeaterSupportData : CommandClassSupportData
    {
        [JsonIgnore]
        public CancelPendingOperationDelegate CancelPendingLearningTimer = null;

        // Internals accessible from TestCases or EDE

        // True:  each started learning process ends with status FailTimeout = 0x02
        // False: each started learning process ends after the half of the timeout with status Success = 0xFF
        public bool FailLearning { get; set; } = false;                 // EDE: not used (set it to 'true')
        public bool SendLearningStatusReport { get; set; } = false;     // EDE: not used (set it to 'true')

        // Capabilities Report
        [ParameterSeparator(1, "IR Repeater Capabilities Report")]
        [XmlDataParameter(1, 0xA0, 0x02, new byte[] { 0x00 }, EditType.Config)]
        public byte NumberOfIrCodeIdentifiersForLearning { get; set; } = 20;

        [XmlDataParameter(1, 0xA0, 0x02, new byte[] { 0x01, 0x00 }, EditType.Config)]
        public byte DutyCycleBitmask { get; set; } = 0x07;

        [XmlDataParameter(1, 0xA0, 0x02, new byte[] { 0x01, 0x01 }, EditType.Config)]
        public bool BitEncodingSupported { get; set; } = true;          // BES (PES and BES MUST NOT be identical!)

        internal bool PulseEncodingSupported { get { return !BitEncodingSupported; } }

        public bool RepeatLearntCodeSupported { get; set; } = false;    // RLC (not supported, never set it to 'true')
        public bool LearntCodeReadbackSupported { get; set; } = false;  // LCR (not supported, never set it to 'true')
        public bool IrRepeatCommandSupported { get; set; } = false;     // IRR (not supported, never set it to 'true')

        [XmlDataParameter(1, 0xA0, 0x02, new byte[] { 0x02 }, EditType.Config)]
        public byte Carrier { get; set; } = 0x00;                       // 0x00 = 820 nm ... 0xFF = 1075 nm

        [XmlDataParameter(1, 0xA0, 0x02, new byte[] { 0x03 }, EditType.Config)]
        public byte MinimumSubCarrier { get; set; } = 1;                // kHz

        [XmlDataParameter(1, 0xA0, 0x02, new byte[] { 0x04 }, EditType.Config)]
        public byte MaximumSubCarrier { get; set; } = 199;              // kHz


        // (Pulse Encoding Support only)
        [CustomDataParameter(1, "Minimum Pulse time unit", UiControl.TextBox, InputType.Int_0_4095, EditType.Config)]
        public ushort MinimumPulseTimeUnit { get; set; } = 11;

        [CustomDataParameter(1, "Maximum Pulse time unit", UiControl.TextBox, InputType.Int_0_4095, EditType.Config)]
        public ushort MaximumPulseTimeUnit { get; set; } = 222;


        // Configuration Set/Report (Bit Encoding Support only)
        [ParameterSeparator(1, "IR Repeater Configuration Report")]
        [CustomDataParameter(1, "Period", UiControl.TextBox, InputType.Int_0_4095, EditType.Config | EditType.Edit)]
        public int Period { get; set; } = 1000;     // in Microseconds

        [CustomDataParameter(1, "Data bit 0 encoding length", UiControl.TextBox, InputType.UShort, EditType.Config | EditType.Edit )]
        public long Bit0EncodingLength { get; set; } = 0;           // Number of used bits in Bit0Encoding (not used in this BES inplementation)

        [CustomDataParameter(1, "Data bit 0 encoding", UiControl.TextBox, InputType.HexByteArray, EditType.Config | EditType.Edit )]
        public byte[] Bit0Encoding { get; set; } = new byte[] { };  // Bits encoding Bit0 (not used in this BES inplementation); Example: new byte[] { 0b01100110, 0b01100110 };

        [CustomDataParameter(1, "Data bit 1 encoding length", UiControl.TextBox, InputType.UShort, EditType.Config | EditType.Edit )]
        public long Bit1EncodingLength { get; set; } = 0;           // Number of used bits in Bit1Encoding (not used in this BES inplementation)

        [CustomDataParameter(1, "Data bit 1 encoding", UiControl.TextBox, InputType.HexByteArray, EditType.Config | EditType.Edit )]
        public byte[] Bit1Encoding { get; set; } = new byte[] { };  // Bits encoding Bit1 (not used in this BES inplementation); Example: new byte[] { 0b01100110, 0b01100110 };

        // Learnt Codes Report
        [CustomDataParameter(1, "Learnt IR codes", UiControl.TextBox, InputType.HexByteArray, EditType.Config | EditType.Edit)]
        public byte[] LearntIrCodes { get; set; } = new byte[] { };

        internal List<byte> LearntIrCodesList 
        { 
            get 
            { 
                return LearntIrCodes.ToList(); 
            } 
            set 
            { 
                LearntIrCodes = value.ToArray(); 
            } 
        }
    }

    public enum IrRepeaterLearningStatusNames : byte
    {
        [Description("0x00 Fail buffer overflow")]
        FailBufferOverflow = 0x00,
        [Description("0x01 Fail stopped")]
        FailStopped = 0x01,
        [Description("0x02 Fail timeout")]
        FailTimeout = 0x02,
        [Description("0xFF Success")]
        Success = 0xFF,
    };
}
