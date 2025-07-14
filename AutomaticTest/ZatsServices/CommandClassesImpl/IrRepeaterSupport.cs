// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
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

// ToDo: LearnedCodeReadback Get/Report (LCR), RepeatLearntCode (RLC), Repeat (IRR)
// ToDo: Supervision status 'Working'

namespace Zats.Services
{
    public class IrRepeaterSupport : CommandClassSupport
    {
        private IrRepeaterSupportData IrRepeaterSupportData { get; set; }

        private readonly string CommandClassName = "IR Repeater CC";
        private string EndPointName = "";
        private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private readonly BackgroundWorker LearningTimer = new BackgroundWorker();
        private string LearningTimerName = "";
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        private byte CurrentLearningCode = 0;
        private byte CurrentLearningTimeout = 0;
        private byte RemainingLearningTime = 0;
        private IrRepeaterLearningStatusNames CurrentLearningStatus;

        public IrRepeaterSupport()
            : this(new IrRepeaterSupportData() { })
        { }

        public IrRepeaterSupport(IrRepeaterSupportData irRepeaterSupportData)
            : base(COMMAND_CLASS_IR_REPEATER.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 1;
            IrRepeaterSupportData = irRepeaterSupportData;
            LogLevel = IrRepeaterSupportData.LogLevel;

            LearningTimer.DoWork += new DoWorkEventHandler(LearningTimer_DoWork);
            LearningTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Timer_RunWorkerCompleted);
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
            IrRepeaterSupportData.PulseEncodingSupported = !IrRepeaterSupportData.BitEncodingSupported; // force (PES or BES)
            if (!IrRepeaterSupportData.PulseEncodingSupported)
            {
                IrRepeaterSupportData.MinimumPulseTimeUnit = 0;
                IrRepeaterSupportData.MaximumPulseTimeUnit = 0;
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
                LearningTimerName = string.Format("{0}{1}: Learning Timeout", CommandClassName, EndPointName);
                ResultForSv = SvResultNames.Undefined;
                if (isResponseAllowed && command != null && command.Length > 1 && IrRepeaterSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_IR_REPEATER.IR_REPEATER_CAPABILITIES_GET.ID)
                    {
                        rpt = PrepareCapabilitiesReport();
                    }
                    else if (command[1] == COMMAND_CLASS_IR_REPEATER.IR_REPEATER_IR_CODE_LEARNING_START.ID)
                    {
                        ResultForSv = HandleLearningStart(command);
                    }
                    else if (command[1] == COMMAND_CLASS_IR_REPEATER.IR_REPEATER_IR_CODE_LEARNING_STOP.ID)
                    {
                        ResultForSv = HandleLearningStop();
                        rpt = PrepareLearningStatusReport();
                        CurrentLearningCode = 0;
                    }
                    else if (command[1] == COMMAND_CLASS_IR_REPEATER.IR_REPEATER_LEARNT_IR_CODE_REMOVE.ID)
                    {
                        ResultForSv = HandleCodeRemove(command);
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
                        ResultForSv = HandleConfigurationSet(command);
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

                    if (rpt != null)
                    {
                        ApiOperation sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command);
                }
            }
        }

        private byte[] PrepareCapabilitiesReport()
        {
            COMMAND_CLASS_IR_REPEATER.IR_REPEATER_CAPABILITIES_REPORT rpt = new COMMAND_CLASS_IR_REPEATER.IR_REPEATER_CAPABILITIES_REPORT() { };
            rpt.numberOfIrCodeIdentifiersForLearning = IrRepeaterSupportData.NumberOfIrCodeIdentifiersForLearning;
            rpt.properties1.irr = (byte)(IrRepeaterSupportData.IrRepeatCommandSupported ? 0x01 : 0x00);
            rpt.properties1.lcr = (byte)(IrRepeaterSupportData.LearntCodeReadbackSupported ? 0x01 : 0x00);
            rpt.properties1.rlc = (byte)(IrRepeaterSupportData.RepeatLearntCodeSupported ? 0x01 : 0x00);
            rpt.properties1.pes = (byte)(IrRepeaterSupportData.PulseEncodingSupported ? 0x01 : 0x00);
            rpt.properties1.bes = (byte)(IrRepeaterSupportData.BitEncodingSupported ? 0x01 : 0x00);
            rpt.properties1.dutyCycleBitmask = IrRepeaterSupportData.DutyCycleBitmask;
            rpt.carrier = IrRepeaterSupportData.Carrier;
            rpt.minimumSubCarrier = IrRepeaterSupportData.MinimumSubCarrier;
            rpt.maxiumSubCarrier = IrRepeaterSupportData.MaximumSubCarrier;
            rpt.minimumPulseTimeUnitMsb = (byte)(IrRepeaterSupportData.MinimumPulseTimeUnit >> 4);
            rpt.properties2.minimumPulseTimeUnitLsb = (byte)(IrRepeaterSupportData.MinimumPulseTimeUnit & 0x0F);
            rpt.properties2.maximumPulseTimeUnitMsb = (byte)(IrRepeaterSupportData.MaximumPulseTimeUnit >> 8);
            rpt.maximumPulseTimeUnitLsb = (byte)(IrRepeaterSupportData.MaximumPulseTimeUnit & 0xFF);
            return rpt;
        }

        private SvResultNames HandleLearningStart(byte[] command)
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
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Learning Start: Code {2} and Timeout {3} accepted",
                            CommandClassName, EndPointName, CurrentLearningCode, CurrentLearningTimeout);
                LearningTimer.RunWorkerAsync();
                return SvResultNames.Successful;
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Learning Start: Code {2}, Timeout {3}, command ignored",
                        CommandClassName, EndPointName, CurrentLearningCode, CurrentLearningTimeout);
            return SvResultNames.Failed;
        }

        private SvResultNames HandleLearningStop()
        {
            if (IrRepeaterSupportData.NumberOfIrCodeIdentifiersForLearning > 0)
            {
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Learning Stop accepted", CommandClassName, EndPointName);
                LearningTimer_Reset();
                CurrentLearningStatus = IrRepeaterLearningStatusNames.FailStopped;
                return SvResultNames.Successful;
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Learning Stop ignored", CommandClassName, EndPointName);
            return SvResultNames.Failed;
        }
        private byte[] PrepareLearningStatusReport()
        {
            if (IrRepeaterSupportData.SendLearningStatusReport)
            {
                COMMAND_CLASS_IR_REPEATER.IR_REPEATER_IR_CODE_LEARNING_STATUS rpt = new COMMAND_CLASS_IR_REPEATER.IR_REPEATER_IR_CODE_LEARNING_STATUS() { };
                rpt.irCodeIdentifier = CurrentLearningCode;
                rpt.status = (byte)CurrentLearningStatus;
                return rpt;
            }
            return null;
        }

        private SvResultNames HandleCodeRemove(byte[] command)
        {
            COMMAND_CLASS_IR_REPEATER.IR_REPEATER_LEARNT_IR_CODE_REMOVE removeCmd = command;
            byte requestedCode = removeCmd.irCodeIdentifier;
            if (IrRepeaterSupportData.NumberOfIrCodeIdentifiersForLearning > 0)
            {
                if (requestedCode == 0x00)
                {
                    IrRepeaterSupportData.LearntIrCodes = new List<byte> { };
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Code Remove: All codes removed", CommandClassName, EndPointName);
                    return SvResultNames.Successful;
                }
                if (requestedCode <= IrRepeaterSupportData.NumberOfIrCodeIdentifiersForLearning && IrRepeaterSupportData.LearntIrCodes.Contains(requestedCode))
                {
                    IrRepeaterSupportData.LearntIrCodes.Remove(requestedCode);
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Code Remove: Code {2} removed", CommandClassName, EndPointName, requestedCode);
                    return SvResultNames.Successful;
                }
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Code Remove command for Code {2} ignored", CommandClassName, EndPointName, requestedCode);
            return SvResultNames.Failed;
        }

        private byte[] PrepareLearntCodesReport()
        {
            if (IrRepeaterSupportData.NumberOfIrCodeIdentifiersForLearning > 0)
            {
                COMMAND_CLASS_IR_REPEATER.IR_REPEATER_LEARNT_IR_CODE_REPORT rpt = new COMMAND_CLASS_IR_REPEATER.IR_REPEATER_LEARNT_IR_CODE_REPORT() { };
                rpt.irCodeIdentifierStatusBitmask = CommandClassSupportUtils.CreateBitmaskFromList(IrRepeaterSupportData.LearntIrCodes, 1);   // Bit 0 represents code #1
                rpt.properties1.irCodeIdenfierStatusLength = (byte)rpt.irCodeIdentifierStatusBitmask.Count;
                return rpt;
            }
            return null;
        }

        private SvResultNames HandleConfigurationSet(byte[] command)
        {
            COMMAND_CLASS_IR_REPEATER.IR_REPEATER_CONFIGURATION_SET setCmd = command;
            if (IrRepeaterSupportData.BitEncodingSupported)
            {
                if (setCmd.vg1.Count >= 2)
                {
                    IrRepeaterSupportData.Bit0EncodingLength = CommandClassSupportUtils.ByteFieldToInt64(setCmd.vg1[0].dataBitEncodingLength, 2);
                    IrRepeaterSupportData.Bit0Encoding = setCmd.vg1[0].dataBitEncoding.ToList();
                    IrRepeaterSupportData.Bit1EncodingLength = CommandClassSupportUtils.ByteFieldToInt64(setCmd.vg1[1].dataBitEncodingLength, 2);
                    IrRepeaterSupportData.Bit1Encoding = setCmd.vg1[1].dataBitEncoding.ToList();
                }
                else
                {
                    // Workaround for wrong decoding of {vg1}
                    List<byte> tmp;
                    int idx;
                    tmp = new List<byte> { command[4], command[5] };
                    IrRepeaterSupportData.Bit0EncodingLength = CommandClassSupportUtils.ByteFieldToInt64(tmp.ToArray(), 2);
                    tmp = new List<byte> { };
                    idx = 6;
                    for (int i = 0; i < ((IrRepeaterSupportData.Bit0EncodingLength - 1) / 8) + 1; i++)
                    {
                        tmp.Add(command[idx++]);
                    }
                    IrRepeaterSupportData.Bit0Encoding = tmp;
                    tmp = new List<byte> { command[idx], command[idx + 1] };
                    IrRepeaterSupportData.Bit1EncodingLength = CommandClassSupportUtils.ByteFieldToInt64(tmp.ToArray(), 2);
                    tmp = new List<byte> { };
                    idx += 2;
                    for (int i = 0; i < ((IrRepeaterSupportData.Bit1EncodingLength - 1) / 8) + 1; i++)
                    {
                        tmp.Add(command[idx++]);
                    }
                    IrRepeaterSupportData.Bit1Encoding = tmp;
                }
                IrRepeaterSupportData.Period = (setCmd.periodMsb << 4) + setCmd.properties1.periodLsb;
                if (IrRepeaterSupportData.Period >= 1 && IrRepeaterSupportData.Period <= 4096
                    && IrRepeaterSupportData.Bit0Encoding.Count * 8 >= IrRepeaterSupportData.Bit0EncodingLength
                    && IrRepeaterSupportData.Bit1Encoding.Count * 8 >= IrRepeaterSupportData.Bit1EncodingLength)
                {
                    return SvResultNames.Successful;
                }
            }
            return SvResultNames.Failed;
        }

        private byte[] PrepareConfigurationReport()
        {
            COMMAND_CLASS_IR_REPEATER.IR_REPEATER_CONFIGURATION_REPORT rpt = new COMMAND_CLASS_IR_REPEATER.IR_REPEATER_CONFIGURATION_REPORT() { };
            if (IrRepeaterSupportData.BitEncodingSupported)
            {
                rpt.periodMsb = (byte)(IrRepeaterSupportData.Period >> 4);
                rpt.properties1.periodLsb = (byte)(IrRepeaterSupportData.Period & 0x0F);
                rpt.vg1.Add(new COMMAND_CLASS_IR_REPEATER.IR_REPEATER_CONFIGURATION_REPORT.TVG1() { });
                rpt.vg1.Add(new COMMAND_CLASS_IR_REPEATER.IR_REPEATER_CONFIGURATION_REPORT.TVG1() { });
                rpt.vg1[0].dataBitEncodingLength = CommandClassSupportUtils.Int64ToByteField(IrRepeaterSupportData.Bit0EncodingLength, 2);
                rpt.vg1[0].dataBitEncoding = IrRepeaterSupportData.Bit0Encoding;
                rpt.vg1[1].dataBitEncodingLength = CommandClassSupportUtils.Int64ToByteField(IrRepeaterSupportData.Bit1EncodingLength, 2);
                rpt.vg1[1].dataBitEncoding = IrRepeaterSupportData.Bit1Encoding;
                return rpt;
            }
            return null;
        }

        private void LearningTimer_DoWork(object sender, DoWorkEventArgs e)
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
                        IrRepeaterSupportData.LearntIrCodes.Add(CurrentLearningCode);
                    }
                    byte[] rpt = PrepareLearningStatusReport();
                    if (rpt != null)
                    {
                        CommandClassSupportUtils.AddUnsolicitedFrame(rpt, new byte[] { (byte)ReceivedAchData.SrcNode.Id }, TxOptions, (byte)ReceivedAchData.SecurityScheme);
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Learning Status Report '{2}' added to queue",
                                    CommandClassName, EndPointName, CurrentLearningStatus);
                    }
                }
                else
                {
                    CurrentLearningStatus = IrRepeaterLearningStatusNames.FailStopped; // already set in HandleLearningStop() for Status Report
                }
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Code {2} Learning Status: '{3}' = 0x{4:X2}", 
                            CommandClassName, EndPointName, CurrentLearningCode, CurrentLearningStatus, (byte) CurrentLearningStatus);
                RemainingLearningTime = 0;
                CurrentLearningTimeout = 0;
                CurrentLearningCode = 0;
            }
        }
        private void Timer_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _resetEvent.Set();
            }
        }
        private void LearningTimer_Reset()
        {
            if (LearningTimer.IsBusy)
            {
                LearningTimer.CancelAsync();
                _resetEvent.WaitOne();
                ReportOperationProgress(LearningTimerName, ReportTimerStateNames.Reset, 0);
            }
        }
        private void ReportOperationProgress(string timerName, ReportTimerStateNames timerState, int timerValue)
        {
            CommandClassSupportUtils.ReportTimerState(LogLevel, timerName, timerState, timerValue);
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
                case COMMAND_CLASS_IR_REPEATER.IR_REPEATER_CONFIGURATION_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
                case COMMAND_CLASS_IR_REPEATER.IR_REPEATER_IR_CODE_LEARNING_START.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
                case COMMAND_CLASS_IR_REPEATER.IR_REPEATER_IR_CODE_LEARNING_STOP.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
                case COMMAND_CLASS_IR_REPEATER.IR_REPEATER_LEARNT_IR_CODE_REMOVE.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }
    }

    public class IrRepeaterSupportData
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
        // True:  each started learning process ends with status FailTimeout = 0x02
        // False: each started learning process ends after the half of the timeout with status Success = 0xFF
        public bool FailLearning { get; set; } = false;
        public bool SendLearningStatusReport { get; set; } = false;

        // Capabilities Report
        public byte NumberOfIrCodeIdentifiersForLearning { get; set; } = 20;
        public bool IrRepeatCommandSupported { get; set; } = false;     // IRR
        public bool LearntCodeReadbackSupported { get; set; } = false;  // LCR
        public bool RepeatLearntCodeSupported { get; set; } = false;    // RLC
        public bool PulseEncodingSupported { get; set; } = false;       // PES
        public bool BitEncodingSupported { get; set; } = true;          // BES
        public byte DutyCycleBitmask { get; set; } = 0x07;
        public byte Carrier { get; set; } = 0x00;                       // 0x00 = 820 nm ... 0xFF = 1075 nm
        public byte MinimumSubCarrier { get; set; } = 1;                // kHz
        public byte MaximumSubCarrier { get; set; } = 199;              // kHz
        // (Pulse Encoding Support only)
        public ushort MinimumPulseTimeUnit { get; set; } = 11;
        public ushort MaximumPulseTimeUnit { get; set; } = 222;

        public List<byte> LearntIrCodes { get; set; } = new List<byte> { };
        // Configuration Set/Report (Bit Encoding Support only)
        public int Period { get; set; } = 1000;     // in Microseconds
        public long Bit0EncodingLength = 12;        // Number of used bits in Bit0Encoding
        public List<byte> Bit0Encoding = new List<byte> { 0b01100110, 0b01100110 };
        public long Bit1EncodingLength = 14;        // Number of used bits in Bit1Encoding
        public List<byte> Bit1Encoding = new List<byte> { 0b11110110, 0b11110110 };
    }
    public enum IrRepeaterLearningStatusNames : byte
    {
        FailBufferOverflow = 0x00,
        FailStopped = 0x01,
        FailTimeout = 0x02,
        Success = 0xFF,
    };
}
