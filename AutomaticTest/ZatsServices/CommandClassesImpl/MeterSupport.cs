// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Utils;
using Utils.UI.Enums;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;

// ToDo: Supervision Report Status 'Working' for Meter Reset (optional)

namespace Zats.Services
{
    public class MeterSupport : CommandClassSupport
    {
        private MeterSupportData MeterSupportData { get; set; }

        private readonly string CommandClassName = "Meter CC";
        private string EndPointName = "";
        private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private readonly BackgroundWorker RuntimeTimer = new BackgroundWorker();
        private string RuntimeTimerName = "";
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        private readonly double SimulationConsumingStep = 1.25;
        private readonly double SimulationProducingStep = 0.75;
        private uint RuntimeElapsed = 0;
        private uint LastMeterReportTime = 0;
        private bool MeterResetOccured = false;

        public MeterSupport()
            : this(new MeterSupportData() { })
        { }

        public MeterSupport(MeterSupportData meterSupportData)
            : base(COMMAND_CLASS_METER.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 6;
            MeterSupportData = meterSupportData;
            LogLevel = MeterSupportData.LogLevel;

            if (!MeterSupportData.IsTimerStarted)
            {
                RuntimeTimer.DoWork += new DoWorkEventHandler(RuntimeTimer_DoWork);
                RuntimeTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(RuntimeTimer_RunWorkerCompleted);
                RuntimeTimer.WorkerSupportsCancellation = true;
                RuntimeTimer.WorkerReportsProgress = false;
                RuntimeTimer.RunWorkerAsync();
                MeterSupportData.IsTimerStarted = true;
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
                RuntimeTimerName = string.Format("{0}{1}: Runtime Timer", CommandClassName, EndPointName);
                ResultForSv = SvResultNames.Undefined;
                if (isResponseAllowed && command != null && command.Length > 1 && MeterSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_METER.METER_GET.ID)
                    {
                        byte requestedScaleIndex = MeterSupportData.DefaultScale;

                        if (Version == 1)
                        {
                            rpt = PrepareReportV1();
                            requestedScaleIndex = MeterSupportData.DefaultScale;
                        }
                        else if (Version == 2)
                        {
                            rpt = PrepareReportV2(command, ref requestedScaleIndex);
                        }
                        else if (Version == 3)
                        {
                            rpt = PrepareReportV3(command, ref requestedScaleIndex);
                        }
                        else if (Version == 4 || Version == 5 || Version == 6)
                        {
                            rpt = PrepareReportV4(command, ref requestedScaleIndex);
                        }
                        if (rpt != null)
                        {
                            MeterSupportData.MeterValues[requestedScaleIndex].PreviousValueConsuming = MeterSupportData.MeterValues[requestedScaleIndex].ValueConsuming;
                            MeterSupportData.MeterValues[requestedScaleIndex].PreviousValueProducing = MeterSupportData.MeterValues[requestedScaleIndex].ValueProducing;
                            LastMeterReportTime = RuntimeElapsed;
                        }
                    }
                    else if (command[1] == COMMAND_CLASS_METER_V2.METER_SUPPORTED_GET.ID)
                    {
                        rpt = PrepareSupportedReport();
                    }
                    else if (command[1] == COMMAND_CLASS_METER_V2.METER_RESET.ID)
                    {
                        ResultForSv = HandleMeterReset(command);
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

        private byte[] PrepareReportV1()
        {
            COMMAND_CLASS_METER.METER_REPORT rpt = new COMMAND_CLASS_METER.METER_REPORT() { };
            byte reportedScaleIndex = MeterSupportData.DefaultScale;
            byte reportedSize = MeterSupportData.MeterValues[reportedScaleIndex].Size;
            byte reportedPrecision = MeterSupportData.MeterValues[reportedScaleIndex].Precision;
            double reportedValue = MeterSupportData.MeterValues[reportedScaleIndex].ValueConsuming;

            rpt.meterType = (byte) MeterSupportData.MeterType;
            rpt.properties1.scale = reportedScaleIndex;
            rpt.properties1.size = reportedSize;
            rpt.properties1.precision = reportedPrecision;
            rpt.meterValue = CommandClassSupportUtils.ValueToByteField(reportedValue, reportedSize, reportedPrecision);

            return rpt;
        }
        private byte[] PrepareReportV2(byte[] command, ref byte requestedScaleIndex)
        {
            COMMAND_CLASS_METER_V2.METER_GET meterGetCmd = command;
            requestedScaleIndex = ValidateRequestedScale(meterGetCmd.properties1.scale, 0x00);

            if (requestedScaleIndex != 0xFF) // invalid scale request -> ignore command
            {
                COMMAND_CLASS_METER_V2.METER_REPORT rpt = new COMMAND_CLASS_METER_V2.METER_REPORT() { };
                uint deltaTime;
                byte reportedRateType = MeterSupportData.MeterValues[requestedScaleIndex].DefaultRateType;
                byte reportedSize = MeterSupportData.MeterValues[requestedScaleIndex].Size;
                byte reportedPrecision = MeterSupportData.MeterValues[requestedScaleIndex].Precision;
                double reportedValue = 0.0;
                double reportedPreviousValue = 0.0;
                switch (reportedRateType)
                {
                    case 0x01:
                        reportedValue = MeterSupportData.MeterValues[requestedScaleIndex].ValueConsuming;
                        reportedPreviousValue = MeterSupportData.MeterValues[requestedScaleIndex].PreviousValueConsuming;
                        break;
                    case 0x02:
                        reportedValue = MeterSupportData.MeterValues[requestedScaleIndex].ValueProducing;
                        reportedPreviousValue = MeterSupportData.MeterValues[requestedScaleIndex].PreviousValueProducing;
                        break;
                }

                rpt.properties1.meterType = (byte) MeterSupportData.MeterType;
                rpt.properties1.rateType = reportedRateType;
                rpt.properties2.size = reportedSize;
                rpt.properties2.precision = reportedPrecision;
                rpt.properties2.scale = requestedScaleIndex;

                rpt.meterValue = CommandClassSupportUtils.ValueToByteField(reportedValue, reportedSize, reportedPrecision);

                deltaTime = MeterSupportData.MeterValues[requestedScaleIndex].DeltaTime;
                if (MeterSupportData.SimulateTime == 1)
                {
                    if (LastMeterReportTime != 0)
                    {
                        deltaTime = RuntimeElapsed - LastMeterReportTime;
                        if (deltaTime > 0)
                        {
                            rpt.previousMeterValue = CommandClassSupportUtils.ValueToByteField(
                                                        reportedPreviousValue, reportedSize, reportedPrecision);
                        }
                    }
                }
                rpt.deltaTime = CommandClassSupportUtils.UInt64ToByteField(deltaTime, 2);

                return rpt;
            }
            return null;
        }
        private byte[] PrepareReportV3(byte[] command, ref byte requestedScaleIndex)
        {
            COMMAND_CLASS_METER_V3.METER_GET meterGetCmd = command;
            requestedScaleIndex = ValidateRequestedScale(meterGetCmd.properties1.scale, 0x00);

            if (requestedScaleIndex != 0xFF) // invalid scale request -> ignore command
            {
                COMMAND_CLASS_METER_V3.METER_REPORT rpt = new COMMAND_CLASS_METER_V3.METER_REPORT() { };
                uint deltaTime;
                byte reportedRateType = MeterSupportData.MeterValues[requestedScaleIndex].DefaultRateType;
                byte reportedSize = MeterSupportData.MeterValues[requestedScaleIndex].Size;
                byte reportedPrecision = MeterSupportData.MeterValues[requestedScaleIndex].Precision;
                double reportedValue = 0.0;
                double reportedPreviousValue = 0.0;
                switch (reportedRateType)
                {
                    case 0x01:
                        reportedValue = MeterSupportData.MeterValues[requestedScaleIndex].ValueConsuming;
                        reportedPreviousValue = MeterSupportData.MeterValues[requestedScaleIndex].PreviousValueConsuming;
                        break;
                    case 0x02:
                        reportedValue = MeterSupportData.MeterValues[requestedScaleIndex].ValueProducing;
                        reportedPreviousValue = MeterSupportData.MeterValues[requestedScaleIndex].PreviousValueProducing;
                        break;
                }

                rpt.properties1.meterType = (byte) MeterSupportData.MeterType;
                rpt.properties1.rateType = reportedRateType;
                rpt.properties2.size = reportedSize;
                rpt.properties2.precision = reportedPrecision;
                rpt.properties1.scaleBit2 = (byte) ((requestedScaleIndex & 0x04) >> 2);
                rpt.properties2.scaleBits10 = (byte) (requestedScaleIndex & 0x03);

                rpt.meterValue = CommandClassSupportUtils.ValueToByteField(reportedValue, reportedSize, reportedPrecision);

                deltaTime = MeterSupportData.MeterValues[requestedScaleIndex].DeltaTime;
                if (MeterSupportData.SimulateTime == 1)
                {
                    if (LastMeterReportTime != 0)
                    {
                        deltaTime = RuntimeElapsed - LastMeterReportTime;
                        if (deltaTime > 0)
                        {
                            rpt.previousMeterValue = CommandClassSupportUtils.ValueToByteField(
                                                        reportedPreviousValue, reportedSize, reportedPrecision);
                        }
                    }
                }
                rpt.deltaTime = CommandClassSupportUtils.UInt64ToByteField(deltaTime, 2);

                return rpt;
            }
            return null;
        }
        private byte[] PrepareReportV4(byte[] command, ref byte requestedScaleIndex)
        {
            COMMAND_CLASS_METER_V4.METER_GET meterGetCmd = command;
            requestedScaleIndex = ValidateRequestedScale(meterGetCmd.properties1.scale, meterGetCmd.scale2);
            byte requestedRateType = meterGetCmd.properties1.rateType;

            //CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: MeterGet V4+ received. rateType: {2}, scale: {3}",
            //            CommandClassName, EndPointName, requestedRateType, requestedScaleIndex);

            if ((requestedScaleIndex != 0xFF)   // invalid scale request > ignore command
             && (requestedRateType != 0x03)     // invalid rate type request -> ignore command
             && ((requestedRateType & MeterSupportData.SupportedRateTypes) != 0 || requestedRateType == 0x00))
            {
                COMMAND_CLASS_METER_V4.METER_REPORT rpt = new COMMAND_CLASS_METER_V4.METER_REPORT() { };
                uint deltaTime;
                byte reportedRateType = (requestedRateType == 0x00) ? MeterSupportData.MeterValues[requestedScaleIndex].DefaultRateType : requestedRateType;
                byte reportedSize = MeterSupportData.MeterValues[requestedScaleIndex].Size;
                byte reportedPrecision = MeterSupportData.MeterValues[requestedScaleIndex].Precision;
                double reportedValue = 0.0;
                double reportedPreviousValue = 0.0;

                switch (reportedRateType)
                {
                    case 0x01:
                        reportedValue = MeterSupportData.MeterValues[requestedScaleIndex].ValueConsuming;
                        reportedPreviousValue = MeterSupportData.MeterValues[requestedScaleIndex].PreviousValueConsuming;
                        break;
                    case 0x02:
                        reportedValue = MeterSupportData.MeterValues[requestedScaleIndex].ValueProducing;
                        reportedPreviousValue = MeterSupportData.MeterValues[requestedScaleIndex].PreviousValueProducing;
                        break;
                }

                rpt.properties1.meterType = (byte) MeterSupportData.MeterType;
                rpt.properties1.rateType = reportedRateType;
                rpt.properties2.size = reportedSize;
                rpt.properties2.precision = reportedPrecision;
                if (requestedScaleIndex >= 8)   // M.S.T. == 0x07
                {
                    rpt.properties1.scaleBit2 = 1;
                    rpt.properties2.scaleBits10 = 3;
                    rpt.scale2 = (byte) (requestedScaleIndex - 8);
                }
                else
                {
                    rpt.properties1.scaleBit2 = (byte) ((requestedScaleIndex & 0x04) >> 2);
                    rpt.properties2.scaleBits10 = (byte) (requestedScaleIndex & 0x03);
                    rpt.scale2 = ByteValue.Empty;   // reset data.scale2.HasValue back to false -> omit scale2 field
                }

                rpt.meterValue = CommandClassSupportUtils.ValueToByteField(reportedValue, reportedSize, reportedPrecision);

                deltaTime = MeterSupportData.MeterValues[requestedScaleIndex].DeltaTime;
                if (MeterSupportData.SimulateTime == 1)
                {
                    if (LastMeterReportTime != 0)
                    {
                        deltaTime = RuntimeElapsed - LastMeterReportTime;
                        if (deltaTime > 0)
                        {
                            rpt.previousMeterValue = CommandClassSupportUtils.ValueToByteField(
                                                        reportedPreviousValue, reportedSize, reportedPrecision);
                        }
                    }
                }
                rpt.deltaTime = CommandClassSupportUtils.UInt64ToByteField(deltaTime, 2);
                //CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: MeterReport V4+. rateType: {2}, scale: {3}, value: {4}",
                //            CommandClassName, EndPointName, reportedRateType, requestedScaleIndex, reportedValue);
                return rpt;
            }
            return null;
        }

        private byte[] PrepareSupportedReport()
        {
            if (Version == 2)
            {
                COMMAND_CLASS_METER_V2.METER_SUPPORTED_REPORT rpt = new COMMAND_CLASS_METER_V2.METER_SUPPORTED_REPORT() { };
                rpt.properties1.meterType = (byte) MeterSupportData.MeterType;
                rpt.properties1.meterReset = (byte) (MeterSupportData.MeterResetSupported ? 0x01 : 0x00);
                rpt.properties2.scaleSupported = 0;
                switch (MeterSupportData.MeterType)
                {
                    case MeterTypeNames.Electric: rpt.properties2.scaleSupported = (byte) (MeterSupportData.ScalesSupportedBitmask[0] & 0x0F); break;
                    case MeterTypeNames.Gas:      rpt.properties2.scaleSupported = (byte) (MeterSupportData.ScalesSupportedBitmask[0] & 0x0F); break;
                    case MeterTypeNames.Water:    rpt.properties2.scaleSupported = (byte) (MeterSupportData.ScalesSupportedBitmask[0] & 0x0F); break;
                }
                return rpt;
            }
            if (Version == 3)
            {
                COMMAND_CLASS_METER_V3.METER_SUPPORTED_REPORT rpt = new COMMAND_CLASS_METER_V3.METER_SUPPORTED_REPORT() { };
                rpt.properties1.meterType = (byte) MeterSupportData.MeterType;
                rpt.properties1.meterReset = (byte) (MeterSupportData.MeterResetSupported ? 0x01 : 0x00);
                rpt.scaleSupported = 0;
                switch (MeterSupportData.MeterType)
                {
                    case MeterTypeNames.Electric: rpt.scaleSupported = (byte) (MeterSupportData.ScalesSupportedBitmask[0] & 0x7F); break;
                    case MeterTypeNames.Gas:      rpt.scaleSupported = (byte) (MeterSupportData.ScalesSupportedBitmask[0] & 0x0F); break;
                    case MeterTypeNames.Water:    rpt.scaleSupported = (byte) (MeterSupportData.ScalesSupportedBitmask[0] & 0x0F); break;
                }
                return rpt;
            }
            if (Version == 4 || Version == 5 || Version == 6)
            {
                COMMAND_CLASS_METER_V4.METER_SUPPORTED_REPORT rpt = new COMMAND_CLASS_METER_V4.METER_SUPPORTED_REPORT() { };
                rpt.properties1.meterType = (byte) MeterSupportData.MeterType;
                rpt.properties1.rateType = MeterSupportData.SupportedRateTypes;
                rpt.properties1.meterReset = (byte) (MeterSupportData.MeterResetSupported ? 0x01 : 0x00);
                rpt.properties2.mST = 0;
                rpt.numberOfScaleSupportedBytesToFollow = ByteValue.Empty;   // reset data.XX.HasValue back to false -> omit the field;
                switch (MeterSupportData.MeterType)
                {
                    case MeterTypeNames.Electric:
                        rpt.properties2.scaleSupported0 = (byte) (MeterSupportData.ScalesSupportedBitmask[0] & 0x7F);
                        if (MeterSupportData.ScalesSupportedBitmask[1] != 0x00)
                        {
                            rpt.properties2.mST = 1;
                            rpt.numberOfScaleSupportedBytesToFollow = 1;
                            rpt.scaleSupported.Add((byte)(MeterSupportData.ScalesSupportedBitmask[1] & 0x03));
                        }
                        break;
                    case MeterTypeNames.Gas:
                        rpt.properties2.scaleSupported0 = (byte) (MeterSupportData.ScalesSupportedBitmask[0] & 0x0F);
                        break;
                    case MeterTypeNames.Water:
                        rpt.properties2.scaleSupported0 = (byte) (MeterSupportData.ScalesSupportedBitmask[0] & 0x0F);
                        break;
                    case MeterTypeNames.Heating:
                        if (Version >= 5)
                            rpt.properties2.scaleSupported0 = (byte) (MeterSupportData.ScalesSupportedBitmask[0] & 0x01);
                        break;
                    case MeterTypeNames.Cooling:
                        if (Version >= 5)
                            rpt.properties2.scaleSupported0 = (byte) (MeterSupportData.ScalesSupportedBitmask[0] & 0x01);
                        break;
                }
                return rpt;
            }
            return null;
        }

        // ValidateRequestedScale() returns 0xFF if the requested scale is not supported.
        // scale1: 3 bit (0...7); scale2: 8 bit -> mapped to index 0..6 and 8..n
        private byte ValidateRequestedScale(byte scale1, byte scale2)
        {
            if (scale1 == 0x00)
                scale1 = MeterSupportData.DefaultScale; // is in range 0...3
            if (scale1 == 0x00)
                if ((byte) (MeterSupportData.ScalesSupportedBitmask[0] & 0x01) != 0)
                    return scale1;
            if (scale1 == 0x01)
                if ((byte) (MeterSupportData.ScalesSupportedBitmask[0] & 0x02) != 0)
                    return scale1;
            if (scale1 == 0x02)
                if ((byte) (MeterSupportData.ScalesSupportedBitmask[0] & 0x04) != 0)
                    return scale1;
            if (scale1 == 0x03)
                if ((byte) (MeterSupportData.ScalesSupportedBitmask[0] & 0x08) != 0)
                    return scale1;
            if (scale1 == 0x04)
                if ((byte) (MeterSupportData.ScalesSupportedBitmask[0] & 0x10) != 0)
                    return scale1;
            if (scale1 == 0x05)
                if ((byte) (MeterSupportData.ScalesSupportedBitmask[0] & 0x20) != 0)
                    return scale1;
            if (scale1 == 0x06)
                if ((byte) (MeterSupportData.ScalesSupportedBitmask[0] & 0x40) != 0)
                    return scale1;
            if (scale1 == 0x07 && Version >= 4)
            {
                if (scale2 == 0x00)
                    if ((byte) (MeterSupportData.ScalesSupportedBitmask[1] & 0x01) != 0)
                        return (byte) (scale2 + 8);
                if (scale2 == 0x01)
                    if ((byte) (MeterSupportData.ScalesSupportedBitmask[1] & 0x02) != 0)
                        return (byte) (scale2 + 8);
            }
            return 0xFF;
        }

        private void RuntimeTimer_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            CommandClassSupportUtils.ReportTimerState(LogLevel, RuntimeTimerName, ReportTimerStateNames.Started, RuntimeElapsed);
            while (true)
            {
                // Block the RuntimeTimer thread for 3 seconds to keep the reset value for an immediately following Meter.Get/Meter.Report
                if (MeterResetOccured)
                {
                    Thread.Sleep(3000);
                }
                RuntimeElapsed += MeterSupportData.SimulationTickLengthSec;
                if (MeterSupportData.SimulateConsuming == 1)
                {
                    for (byte scale = 0; scale < MeterSupportData.MeterValues.Length; scale++)
                    {
                        MeterSupportData.MeterValues[scale].ValueConsuming += SimulationConsumingStep;
                    }
                    if ((RuntimeElapsed % MeterSupportData.SimulationTickLogSec) == 0 || MeterResetOccured)
                    {
                        for (byte scale = 0; scale < MeterSupportData.MeterValues.Length; scale++)
                        {
                            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: MeterValue[{2}] (Consuming) after {3} seconds: {4}",
                                    CommandClassName, EndPointName, scale, RuntimeElapsed, MeterSupportData.MeterValues[scale].ValueConsuming);
                        }
                    }
                }
                if (MeterSupportData.SimulateProducing == 1)
                {
                    for (byte scale = 0; scale < MeterSupportData.MeterValues.Length; scale++)
                    {
                        MeterSupportData.MeterValues[scale].ValueProducing += SimulationProducingStep;
                    }
                    if ((RuntimeElapsed % MeterSupportData.SimulationTickLogSec) == 0 || MeterResetOccured)
                    {
                        for (byte scale = 0; scale < MeterSupportData.MeterValues.Length; scale++)
                        {
                            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: MeterValue[{2}] (Producing) after {3} seconds: {4}",
                                    CommandClassName, EndPointName, scale, RuntimeElapsed, MeterSupportData.MeterValues[scale].ValueProducing);
                        }
                    }
                }
                MeterResetOccured = false;
                Thread.Sleep((int) MeterSupportData.SimulationTickLengthSec * 1000);
                if (bw.CancellationPending)
                {
                    CommandClassSupportUtils.ReportTimerState(LogLevel, RuntimeTimerName, ReportTimerStateNames.CancelledElapsedSec, RuntimeElapsed);
                    e.Cancel = true;
                    break;
                }
            }
            if (!e.Cancel)
            {
                CommandClassSupportUtils.ReportTimerState(LogLevel, RuntimeTimerName, ReportTimerStateNames.ExpiredSec, RuntimeElapsed);
            }
        }

        private void RuntimeTimer_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _resetEvent.Set();
            }
        }
        private void RuntimeTimer_Reset()
        {
            if (RuntimeTimer.IsBusy)
            {
                RuntimeTimer.CancelAsync();
                _resetEvent.WaitOne();
                CommandClassSupportUtils.ReportTimerState(LogLevel, RuntimeTimerName, ReportTimerStateNames.Reset, 0);
            }
        }

        private SvResultNames HandleMeterReset(byte[] command)
        {
            if (MeterSupportData.MeterResetSupported)
            { 
                if (Version >= 2 && Version <= 5)
                {
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: MeterReset accepted.",
                                CommandClassName, EndPointName);
                    for (byte scale = 0; scale < MeterSupportData.MeterValues.Length; scale++)
                    {
                        MeterSupportData.MeterValues[scale].ValueConsuming = 0.0;
                        MeterSupportData.MeterValues[scale].PreviousValueConsuming = 0.0;
                        MeterSupportData.MeterValues[scale].ValueProducing = 0.0;
                        MeterSupportData.MeterValues[scale].PreviousValueProducing = 0.0;
                    }
                    MeterResetOccured = true;
                    return SvResultNames.Successful;
                }
                if (Version == 6)
                {
                    COMMAND_CLASS_METER_V6.METER_RESET meterResetCmd = command;
                    MeterTypeNames requestedMeterType = (MeterTypeNames) meterResetCmd.properties1.meterType;
                    byte requestedRateType = meterResetCmd.properties1.rateType;
                    byte requestedScaleIndex = ValidateRequestedScale(
                                                (byte) ((meterResetCmd.properties1.scaleBit2 << 2) | meterResetCmd.properties2.scaleBits10), 
                                                meterResetCmd.scale2);
                    double resetValue = CommandClassSupportUtils.ByteListToValue(meterResetCmd.meterValue, 
                                                                                 meterResetCmd.properties2.size, 
                                                                                 meterResetCmd.properties2.precision);
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: MeterReset V6 received. Type: {2} (0x{3:X2}), rateType: {4}, scale: {5}, Value: {6}",
                                CommandClassName, EndPointName, requestedMeterType, (byte) requestedMeterType, requestedRateType, requestedScaleIndex, resetValue);
                    if ((requestedScaleIndex != 0xFF)                       // invalid scale request > ignore command
                     && requestedMeterType == MeterSupportData.MeterType    // invalid meter type request -> ignore command
                     && (requestedRateType != 0x03)                         // invalid rate type request -> ignore command
                     && ((requestedRateType & MeterSupportData.SupportedRateTypes) != 0 || requestedRateType == 0x00))
                    {
                        MeterResetOccured = true;   // Blocks the RuntimeTimer thread for 3 seconds to keep the reset value for an immediately following Meter.Get/Meter.Report
                        CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: MeterReset V6 accepted. Type: {2} (0x{3:X2}), rateType: {4}, scale: {5}, Value: {6}",
                                    CommandClassName, EndPointName, requestedMeterType, (byte)requestedMeterType, requestedRateType, requestedScaleIndex, resetValue);
                        switch (requestedRateType)
                        {
                            case 0x01:
                                MeterSupportData.MeterValues[requestedScaleIndex].ValueConsuming = resetValue;
                                MeterSupportData.MeterValues[requestedScaleIndex].PreviousValueConsuming = resetValue;
                                break;
                            case 0x02:
                                MeterSupportData.MeterValues[requestedScaleIndex].ValueProducing = resetValue;
                                MeterSupportData.MeterValues[requestedScaleIndex].PreviousValueProducing = resetValue;
                                break;
                        }
                        return SvResultNames.Successful;
                    }
                }
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: MeterReset ignored.",
                        CommandClassName, EndPointName);
            return SvResultNames.Failed;
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
                case COMMAND_CLASS_METER_V2.METER_RESET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }

    }

    public class MeterValue
    {
        // V1+ Meter Report
        public byte Size { get; set; } = 1;                 // 0 = unknown; 1, 2, 4 = size
        public byte Precision { get; set; } = 0;            // 0..7
        public double ValueConsuming { get; set; } = 0.0;
        public double ValueProducing { get; set; } = 0.0;
        // V2+ Meter Report
        public byte DefaultRateType { get; set; } = 0x01;   // 0x01 = Import (consumed); 0x02 = Export (produced); MUST NOT be 0x00 or 0x03
        public double PreviousValueConsuming { get; set; } = 0.0;
        public double PreviousValueProducing { get; set; } = 0.0;
        public ushort DeltaTime { get; set; } = 0;
    };

    public class MeterSupportData
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
        public int SimulateTime { get; set; } = 0;          // 0 = off (uses MeterSupportData time values), 1 = on
        public int SimulateConsuming { get; set; } = 0;     // 0 = off; 1 = runs with consuming (same value for all scales)
        public int SimulateProducing { get; set; } = 0;     // 0 = off; 1 = runs with producing (same value for all scales)
        public uint SimulationTickLengthSec { get; set; } = 1;  // Length of a consuming/producing 'tick' in seconds
        public uint SimulationTickLogSec { get; set; } = 10;    // Logging each SimulationTickLogSec seconds
        public MeterTypeNames MeterType { get; set; } = MeterTypeNames.Invalid; // Invalid Meter Type 0x00 (valid value must be set by caller)
        public byte DefaultScale { get; set; } = 0x00;      // 0x00 ... 0x03 (Spec Requirement) according to the supported Meter Type

        // V2+ Supported Report
        public List<byte> ScalesSupportedBitmask { get; set; } = new List<byte> { 0x01, 0x00 }; // according to SDS13781, table Meter Supported Report::Scale Supported Bitmask encoding
        public bool MeterResetSupported { get; set; } = false;
        // V4+ Supported Report
        public byte SupportedRateTypes { get; set; } = 0x01;// 0x00 = Reserved; 0x01 = Import (consumed); 0x02 = Export (produced); 0x03 = Import and Export supported (V4+)

        // V1+ Meter Report
        // Meter Values are available for each scale, for consuming and producing.
        // The MeterValues member has 10 elements, the number of scales of Electric Meter (9) plus one reserved element for index 7 (M.S.T.).
        // MeterValues[0] to [6] are addressed by the two Scale bit fields Scale(2) and Scale(1_0).
        // MeterValues[8] to [9] (kVar and kVarh) are addressed by the Scale2 field, implemented in ValidateRequestedScale: scale2 + 8
        public MeterValue[] MeterValues { get; set; } = new MeterValue[10] { new MeterValue(), new MeterValue(), new MeterValue(), new MeterValue(), new MeterValue(), new MeterValue(), new MeterValue(), new MeterValue(), new MeterValue(), new MeterValue() };

        public bool IsTimerStarted { get; set; } = false;   // This value is maintained by MeterSupport(). Do not change!
    }

    public enum MeterTypeNames : byte
    {
        Invalid  = 0x00,
        Electric = 0x01,
        Gas      = 0x02,
        Water    = 0x03,
        Heating  = 0x04,
        Cooling  = 0x05,
    }
}
