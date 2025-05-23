using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Linq;
using Utils;
using Utils.UI.Enums;
using Zats.Services.CommandClassesImpl;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;
using ZatsServices_netcore.CommandClassesImpl.Definitions.Interfaces;
using Newtonsoft.Json;
using System.Reflection;

// ToDo: Supervision Report Status 'Working' for Meter Reset (optional)
// ToDo: Handle DeltaTime* in RuntimeTimer

namespace Zats.Services
{
    [CommandClassName("Meter Command Class")]
    [CommandClassId(COMMAND_CLASS_METER.ID)]
    [MinVersion(1)]
    [MaxVersion(6)]

    public class MeterSupport : CommandClassSupport
    {
        private MeterSupportData MeterSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return MeterSupportData; }
            set { MeterSupportData = (MeterSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private readonly BackgroundWorker RuntimeTimer = new BackgroundWorker();
        private string RuntimeTimerName = "";
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        private readonly double SimulationConsumingStep = 1.25;
        private readonly double SimulationProducingStep = 0.75;
        private uint RuntimeElapsed = 0;
        private bool IsTimerStarted = false;   // This value is maintained by MeterSupport(). Do not change!
        private bool MeterResetOccured = false;

        public MeterSupport()
            : this(new MeterSupportData() { })
        { }

        public MeterSupport(MeterSupportData meterSupportData)
            : base(COMMAND_CLASS_METER.ID)
        {
            MeterSupportData = meterSupportData;

            if (!IsTimerStarted)
            {
                RuntimeTimer.DoWork += new DoWorkEventHandler(RuntimeTimerDoWork);
                RuntimeTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(RuntimeTimerRunWorkerCompleted);
                RuntimeTimer.WorkerSupportsCancellation = true;
                RuntimeTimer.WorkerReportsProgress = false;
                RuntimeTimerStart();
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

                RuntimeTimerName = string.Format("{0} CC @ {1}: Runtime Timer", CommandClassShortName, EndPointName);
                ResultForSv = SvResultNames.Undefined;
                if (isHandlingAllowed && command != null && command.Length > 1 && MeterSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_METER.METER_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareMeterReportCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_METER_V2.METER_SUPPORTED_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareSupportedReport();
                    }
                    else if (command[1] == COMMAND_CLASS_METER_V2.METER_RESET.ID)
                    {
                        ResultForSv = HandleMeterResetCmd(command, sendSrcNode);
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

        private byte[] PrepareMeterReportV1(bool updatePreviousValueAndDeltaTime)
        {
            ScaleNames requestedScaleNumber = MeterSupportData.DefaultScale;
            MeterValue requestedScaleData = MeterSupportData.MeterValues.Find(x => x.ScaleNumber == requestedScaleNumber);
            if (requestedScaleData != null)
            {
                COMMAND_CLASS_METER.METER_REPORT rpt = new COMMAND_CLASS_METER.METER_REPORT();
                byte reportedScaleIndex = (byte) MeterSupportData.DefaultScale;
                byte reportedSize = requestedScaleData.SizeConsuming;
                byte reportedPrecision = requestedScaleData.PrecisionConsuming;
                double reportedValue = requestedScaleData.ValueConsuming;
                rpt.meterType = (byte) MeterSupportData.MeterType;
                rpt.properties1.scale = reportedScaleIndex;
                rpt.properties1.size = reportedSize;
                rpt.properties1.precision = reportedPrecision;
                rpt.meterValue = CommandClassSupportUtils.ValueToByteField(reportedValue, reportedSize, reportedPrecision);
                if (updatePreviousValueAndDeltaTime)
                {
                    requestedScaleData.PreviousValueConsuming = requestedScaleData.ValueConsuming;
                    requestedScaleData.LastMeterReportTimeConsuming = RuntimeElapsed;
                }
                return rpt;
            }
            return null;
        }
        private byte[] PrepareMeterReportV2(ScaleNames requestedScaleNumber, bool updatePreviousValueAndDeltaTime)
        {
            if (requestedScaleNumber == 0)
            {
                requestedScaleNumber = MeterSupportData.DefaultScale;
            }
            MeterValue requestedScaleData = MeterSupportData.MeterValues.Find(x => x.ScaleNumber == requestedScaleNumber);
            if (requestedScaleData != null)
            {
                COMMAND_CLASS_METER_V2.METER_REPORT rpt = new COMMAND_CLASS_METER_V2.METER_REPORT();
                uint reportedDeltaTime, simulatedDeltaTime;
                byte reportedSize, reportedPrecision;
                double reportedValue, reportedPreviousValue;
                RateTypeNames reportedRateType = (MeterSupportData.SupportedRateTypes & RateTypeNames.Import) != 0 ? RateTypeNames.Import : RateTypeNames.Export;
                if (reportedRateType == RateTypeNames.Import)
                {
                    reportedSize = requestedScaleData.SizeConsuming;
                    reportedPrecision = requestedScaleData.PrecisionConsuming;
                    reportedValue = requestedScaleData.ValueConsuming;
                    reportedPreviousValue = requestedScaleData.PreviousValueConsuming;
                    reportedDeltaTime = requestedScaleData.DeltaTimeConsuming;
                    if (updatePreviousValueAndDeltaTime)
                    {
                        requestedScaleData.PreviousValueConsuming = requestedScaleData.ValueConsuming;
                        requestedScaleData.LastMeterReportTimeConsuming = RuntimeElapsed;
                    }
                }
                else // RateTypeNames.Export
                {
                    reportedSize = requestedScaleData.SizeProducing;
                    reportedPrecision = requestedScaleData.PrecisionProducing;
                    reportedValue = requestedScaleData.ValueProducing;
                    reportedPreviousValue = requestedScaleData.PreviousValueProducing;
                    reportedDeltaTime = requestedScaleData.DeltaTimeProducing;
                    if (updatePreviousValueAndDeltaTime)
                    {
                        requestedScaleData.PreviousValueProducing = requestedScaleData.ValueProducing;
                        requestedScaleData.LastMeterReportTimeProducing = RuntimeElapsed;
                    }
                }

                rpt.properties1.meterType = (byte) MeterSupportData.MeterType;
                rpt.properties1.rateType = (byte) reportedRateType;
                rpt.properties2.size = reportedSize;
                rpt.properties2.precision = reportedPrecision;
                rpt.properties2.scale = (byte) requestedScaleNumber;

                rpt.meterValue = CommandClassSupportUtils.ValueToByteField(reportedValue, reportedSize, reportedPrecision);

                simulatedDeltaTime = CalculateSimulatedDeltaTime(requestedScaleData, reportedRateType);
                if (simulatedDeltaTime > 0)
                {
                    reportedDeltaTime = simulatedDeltaTime;
                }
                rpt.previousMeterValue = CommandClassSupportUtils.ValueToByteField(
                                              reportedPreviousValue, reportedSize, reportedPrecision);
                rpt.deltaTime = CommandClassSupportUtils.UInt64ToByteField(reportedDeltaTime, 2);

                return rpt;
            }
            return null;
        }
        private byte[] PrepareMeterReportV3(ScaleNames requestedScaleNumber, bool updatePreviousValueAndDeltaTime)
        {
            if (requestedScaleNumber == 0)
            {
                requestedScaleNumber = MeterSupportData.DefaultScale;
            }
            MeterValue requestedScaleData = MeterSupportData.MeterValues.Find(x => x.ScaleNumber == requestedScaleNumber);
            if (requestedScaleData != null)
            {
                COMMAND_CLASS_METER_V3.METER_REPORT rpt = new COMMAND_CLASS_METER_V3.METER_REPORT();
                uint reportedDeltaTime, simulatedDeltaTime;
                byte reportedSize, reportedPrecision;
                double reportedValue, reportedPreviousValue;
                RateTypeNames reportedRateType = (MeterSupportData.SupportedRateTypes & RateTypeNames.Import) != 0 ? RateTypeNames.Import : RateTypeNames.Export;
                if (reportedRateType == RateTypeNames.Import)
                {
                    reportedSize = requestedScaleData.SizeConsuming;
                    reportedPrecision = requestedScaleData.PrecisionConsuming;
                    reportedValue = requestedScaleData.ValueConsuming;
                    reportedPreviousValue = requestedScaleData.PreviousValueConsuming;
                    reportedDeltaTime = requestedScaleData.DeltaTimeConsuming;
                    if (updatePreviousValueAndDeltaTime)
                    {
                        requestedScaleData.PreviousValueConsuming = requestedScaleData.ValueConsuming;
                        requestedScaleData.LastMeterReportTimeConsuming = RuntimeElapsed;
                    }
                }
                else // RateTypeNames.Export
                {
                    reportedSize = requestedScaleData.SizeProducing;
                    reportedPrecision = requestedScaleData.PrecisionProducing;
                    reportedValue = requestedScaleData.ValueProducing;
                    reportedPreviousValue = requestedScaleData.PreviousValueProducing;
                    reportedDeltaTime = requestedScaleData.DeltaTimeProducing;
                    if (updatePreviousValueAndDeltaTime)
                    {
                        requestedScaleData.PreviousValueProducing = requestedScaleData.ValueProducing;
                        requestedScaleData.LastMeterReportTimeProducing = RuntimeElapsed;
                    }
                }

                rpt.properties1.meterType = (byte)MeterSupportData.MeterType;
                rpt.properties1.rateType = (byte)reportedRateType;
                rpt.properties2.size = reportedSize;
                rpt.properties2.precision = reportedPrecision;
                rpt.properties1.scaleBit2 = (byte)(((byte)requestedScaleNumber & 0x04) >> 2);
                rpt.properties2.scaleBits10 = (byte)((byte)requestedScaleNumber & 0x03);

                rpt.meterValue = CommandClassSupportUtils.ValueToByteField(reportedValue, reportedSize, reportedPrecision);

                simulatedDeltaTime = CalculateSimulatedDeltaTime(requestedScaleData, reportedRateType);
                if (simulatedDeltaTime > 0)
                {
                    reportedDeltaTime = simulatedDeltaTime;
                }
                rpt.previousMeterValue = CommandClassSupportUtils.ValueToByteField(
                                              reportedPreviousValue, reportedSize, reportedPrecision);
                rpt.deltaTime = CommandClassSupportUtils.UInt64ToByteField(reportedDeltaTime, 2);

                return rpt;
            }
            return null;
        }
        private byte[] PrepareMeterReportV4(ScaleNames requestedScaleNumber, byte requestedRateType, bool updatePreviousValueAndDeltaTime)
        {
            if (requestedScaleNumber == 0)
            {
                requestedScaleNumber = MeterSupportData.DefaultScale;
            }
            MeterValue requestedScaleData = MeterSupportData.MeterValues.Find(x => x.ScaleNumber == requestedScaleNumber);

            //CommandClassSupportUtils.ReportAddLog(LogLevel, "{0} CC @ {1}: MeterGet V4+ received. rateType: {2}, scale: {3}",
            //            CommandClassShortName, EndPointName, requestedRateType, requestedScaleNumber);

            if ((requestedScaleData != null)    // invalid scale request -> ignore command
             && (requestedRateType <= 0x02)     // invalid rate type request -> ignore command
             && ((requestedRateType & (byte) MeterSupportData.SupportedRateTypes) != 0 || requestedRateType == 0x00))
            {
                COMMAND_CLASS_METER_V4.METER_REPORT rpt = new COMMAND_CLASS_METER_V4.METER_REPORT();
                uint reportedDeltaTime, simulatedDeltaTime;
                byte reportedSize, reportedPrecision;
                double reportedValue, reportedPreviousValue;
                RateTypeNames reportedRateType = (RateTypeNames) requestedRateType;
                if (requestedRateType == 0x00)
                {
                    reportedRateType = (MeterSupportData.SupportedRateTypes & RateTypeNames.Import) != 0 ? RateTypeNames.Import : RateTypeNames.Export;
                }
                if (reportedRateType == RateTypeNames.Import)
                {
                    reportedSize = requestedScaleData.SizeConsuming;
                    reportedPrecision = requestedScaleData.PrecisionConsuming;
                    reportedValue = requestedScaleData.ValueConsuming;
                    reportedPreviousValue = requestedScaleData.PreviousValueConsuming;
                    reportedDeltaTime = requestedScaleData.DeltaTimeConsuming;
                    if (updatePreviousValueAndDeltaTime)
                    {
                        requestedScaleData.PreviousValueConsuming = requestedScaleData.ValueConsuming;
                        requestedScaleData.LastMeterReportTimeConsuming = RuntimeElapsed;
                    }
                }
                else // RateTypeNames.Export
                {
                    reportedSize = requestedScaleData.SizeProducing;
                    reportedPrecision = requestedScaleData.PrecisionProducing;
                    reportedValue = requestedScaleData.ValueProducing;
                    reportedPreviousValue = requestedScaleData.PreviousValueProducing;
                    reportedDeltaTime = requestedScaleData.DeltaTimeProducing;
                    if (updatePreviousValueAndDeltaTime)
                    {
                        requestedScaleData.PreviousValueProducing = requestedScaleData.ValueProducing;
                        requestedScaleData.LastMeterReportTimeProducing = RuntimeElapsed;
                    }
                }

                rpt.properties1.meterType = (byte) MeterSupportData.MeterType;
                rpt.properties1.rateType = (byte) reportedRateType;
                rpt.properties2.size = reportedSize;
                rpt.properties2.precision = reportedPrecision;

                MeterSupportUtils.ScaleFieldsV4(requestedScaleNumber, out byte scaleBits10, out byte scaleBit2, out ByteValue scale2);
                rpt.properties1.scaleBit2 = scaleBit2;
                rpt.properties2.scaleBits10 = scaleBits10;
                rpt.scale2 = scale2;

                rpt.meterValue = CommandClassSupportUtils.ValueToByteField(reportedValue, reportedSize, reportedPrecision);

                simulatedDeltaTime = CalculateSimulatedDeltaTime(requestedScaleData, reportedRateType);
                if (simulatedDeltaTime > 0)
                {
                    reportedDeltaTime = simulatedDeltaTime;
                }
                rpt.previousMeterValue = CommandClassSupportUtils.ValueToByteField(
                                              reportedPreviousValue, reportedSize, reportedPrecision);

                rpt.deltaTime = CommandClassSupportUtils.UInt64ToByteField(reportedDeltaTime, 2);
                //CommandClassSupportUtils.ReportAddLog(LogLevel, "{0} CC @ {1}: MeterReport V4+. rateType: {2}, scale: {3}, value: {4}",
                //            CommandClassShortName, EndPointName, reportedRateType, requestedScaleNumber, reportedValue);
                return rpt;
            }
            return null;
        }

        private byte[] PrepareMeterReportByScale(ScaleNames requestedScaleNumber, byte requestedRateType, bool updatePreviousValueAndDeltaTime)
        {
            byte[] rpt = null;

            if (Version == 1)
            {
                rpt = PrepareMeterReportV1(updatePreviousValueAndDeltaTime);
            }
            else if (Version == 2)
            {
                rpt = PrepareMeterReportV2(requestedScaleNumber, updatePreviousValueAndDeltaTime);
            }
            else if (Version == 3)
            {
                rpt = PrepareMeterReportV3(requestedScaleNumber, updatePreviousValueAndDeltaTime);
            }
            else if (Version == 4 || Version == 5 || Version == 6)
            {
                rpt = PrepareMeterReportV4(requestedScaleNumber, requestedRateType, updatePreviousValueAndDeltaTime);
            }
            return rpt;
        }

        private byte[] PrepareMeterReportCmd(byte[] command)
        {
            byte[] rpt = null;
            ScaleNames requestedScale;

            if (Version == 1)
            {
                rpt = PrepareMeterReportV1(true);   // uses MeterSupportData.DefaultScale
            }
            else if (Version == 2)
            {
                COMMAND_CLASS_METER_V2.METER_GET meterGetCmd = command;
                requestedScale = (ScaleNames) meterGetCmd.properties1.scale;
                rpt = PrepareMeterReportV2(requestedScale, true);
            }
            else if (Version == 3)
            {
                COMMAND_CLASS_METER_V3.METER_GET meterGetCmd = command;
                requestedScale = (ScaleNames) meterGetCmd.properties1.scale;
                rpt = PrepareMeterReportV3(requestedScale, true);
            }
            else if (Version == 4 || Version == 5 || Version == 6)
            {
                COMMAND_CLASS_METER_V4.METER_GET meterGetCmd = command;

                if (meterGetCmd.properties1.scale != 7)
                {
                    requestedScale = (ScaleNames)(meterGetCmd.properties1.scale);
                }
                else // Scale field is set to M.S.T.
                {
                    requestedScale = (ScaleNames)(8 + meterGetCmd.scale2);
                }
                byte requestedRateType = meterGetCmd.properties1.rateType;
                rpt = PrepareMeterReportV4(requestedScale, requestedRateType, true);
            }
            return rpt;
        }

        private byte[] PrepareSupportedReport()
        {
            List<byte> scalesSupportedList = new List<byte>();
            foreach (MeterValue scale in MeterSupportData.MeterValues)
            {
                scalesSupportedList.Add((byte) scale.ScaleNumber);
            }
            List<byte> scalesSupportedBitmask = CommandClassSupportUtils.CreateBitmaskFromList(scalesSupportedList, 0, true);

            if (Version == 2)
            {
                COMMAND_CLASS_METER_V2.METER_SUPPORTED_REPORT rpt = new COMMAND_CLASS_METER_V2.METER_SUPPORTED_REPORT();
                rpt.properties1.meterType = (byte) MeterSupportData.MeterType;
                rpt.properties1.meterReset = (byte) (MeterSupportData.MeterResetSupported ? 0x01 : 0x00);
                rpt.properties2.scaleSupported = 0x00;
                switch (MeterSupportData.MeterType)
                {
                    case MeterTypeNames.Electric: rpt.properties2.scaleSupported = (byte) (scalesSupportedBitmask[0] & 0x0F); break;
                    case MeterTypeNames.Gas:      rpt.properties2.scaleSupported = (byte) (scalesSupportedBitmask[0] & 0x0B); break;
                    case MeterTypeNames.Water:    rpt.properties2.scaleSupported = (byte) (scalesSupportedBitmask[0] & 0x0F); break;
                }
                return rpt;
            }
            if (Version == 3)
            {
                COMMAND_CLASS_METER_V3.METER_SUPPORTED_REPORT rpt = new COMMAND_CLASS_METER_V3.METER_SUPPORTED_REPORT();
                rpt.properties1.meterType = (byte) MeterSupportData.MeterType;
                rpt.properties1.meterReset = (byte) (MeterSupportData.MeterResetSupported ? 0x01 : 0x00);
                rpt.scaleSupported = 0;
                switch (MeterSupportData.MeterType)
                {
                    case MeterTypeNames.Electric: rpt.scaleSupported = (byte) (scalesSupportedBitmask[0] & 0x7F); break;
                    case MeterTypeNames.Gas:      rpt.scaleSupported = (byte) (scalesSupportedBitmask[0] & 0x0B); break;
                    case MeterTypeNames.Water:    rpt.scaleSupported = (byte) (scalesSupportedBitmask[0] & 0x0F); break;
                }
                return rpt;
            }
            if (Version == 4 || Version == 5 || Version == 6)
            {
                COMMAND_CLASS_METER_V4.METER_SUPPORTED_REPORT rpt = new COMMAND_CLASS_METER_V4.METER_SUPPORTED_REPORT();
                rpt.properties1.meterType = (byte) MeterSupportData.MeterType;
                rpt.properties1.rateType = (byte) MeterSupportData.SupportedRateTypes;
                rpt.properties1.meterReset = (byte) (MeterSupportData.MeterResetSupported ? 0x01 : 0x00);
                rpt.properties2.mST = 0;
                rpt.numberOfScaleSupportedBytesToFollow = ByteValue.Empty;   // reset data.XX.HasValue back to false -> omit the field;
                switch (MeterSupportData.MeterType)
                {
                    case MeterTypeNames.Electric:
                        rpt.properties2.scaleSupported0 = (byte) (scalesSupportedBitmask[0] & 0x7F);
                        if (scalesSupportedBitmask.Count > 1 && scalesSupportedBitmask[1] != 0x00)
                        {
                            rpt.properties2.mST = 1;
                            rpt.numberOfScaleSupportedBytesToFollow = 1;
                            rpt.scaleSupported.Add((byte) (scalesSupportedBitmask[1] & 0x03));
                        }
                        break;
                    case MeterTypeNames.Gas:
                        rpt.properties2.scaleSupported0 = (byte) (scalesSupportedBitmask[0] & 0x0B);
                        break;
                    case MeterTypeNames.Water:
                        rpt.properties2.scaleSupported0 = (byte) (scalesSupportedBitmask[0] & 0x0F);
                        break;
                    case MeterTypeNames.Heating:
                        if (Version >= 5)
                        {
                            rpt.properties2.scaleSupported0 = (byte) (scalesSupportedBitmask[0] & 0x01);
                        }
                        break;
                    case MeterTypeNames.Cooling:
                        if (Version >= 5)
                        {
                            rpt.properties2.scaleSupported0 = (byte) (scalesSupportedBitmask[0] & 0x01);
                        }
                        break;
                }
                return rpt;
            }
            return null;
        }

        private uint CalculateSimulatedDeltaTime(MeterValue requestedScale, RateTypeNames reportedRateType)
        {
            uint simulatedDeltaTime = 0;
            if (MeterSupportData.SimulateTime == 1)
            {
                if (reportedRateType == RateTypeNames.Import && requestedScale.LastMeterReportTimeConsuming != 0)
                {
                    simulatedDeltaTime = RuntimeElapsed - requestedScale.LastMeterReportTimeConsuming;
                }
                if (reportedRateType == RateTypeNames.Export && requestedScale.LastMeterReportTimeProducing != 0)
                {
                    simulatedDeltaTime = RuntimeElapsed - requestedScale.LastMeterReportTimeProducing;
                }
            }
            return simulatedDeltaTime;
        }

        #region Runtime Timer
        private void RuntimeTimerStart()
        {
            // MeterSupportData.CancelPendingRuntimeTimer = RuntimeTimerReset;
            RuntimeTimer.RunWorkerAsync();   // the Runtime Timer runs infinitely or until Result.State == ActionStates.Cancelled
        }
        private void RuntimeTimerDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            CommandClassSupportUtils.ReportTimerState(MeterSupportData.LogLevel, RuntimeTimerName, ReportTimerStateNames.Started, RuntimeElapsed);
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
                    foreach (MeterValue scale in MeterSupportData.MeterValues)
                    {
                        scale.ValueConsuming += SimulationConsumingStep;
                    }
                    if ((RuntimeElapsed % MeterSupportData.SimulationTickLogSec) == 0 || MeterResetOccured)
                    {
                        foreach (MeterValue scale in MeterSupportData.MeterValues)
                        {
                            CommandClassSupportUtils.ReportAddLog(MeterSupportData.LogLevel, 
                                        "{0} CC @ {1}: MeterValue[{2}] (Consuming) after {3} seconds: {4}",
                                        CommandClassShortName, EndPointName, scale.ScaleNumber, RuntimeElapsed, 
                                        scale.ValueConsuming);
                        }
                    }
                }
                if (MeterSupportData.SimulateProducing == 1)
                {
                    foreach (MeterValue scale in MeterSupportData.MeterValues)
                    {
                        scale.ValueProducing += SimulationProducingStep;
                    }
                    if ((RuntimeElapsed % MeterSupportData.SimulationTickLogSec) == 0 || MeterResetOccured)
                    {
                        foreach (MeterValue scale in MeterSupportData.MeterValues)
                        {
                            CommandClassSupportUtils.ReportAddLog(MeterSupportData.LogLevel,
                                        "{0} CC @ {1}: MeterValue[{2}] (Producing) after {3} seconds: {4}",
                                        CommandClassShortName, EndPointName, scale.ScaleNumber, RuntimeElapsed,
                                        scale.ValueProducing);
                        }
                    }
                }
                MeterResetOccured = false;
                if (Result.State == ActionStates.Cancelled)
                {
                    RuntimeTimerReset();
                }
                Thread.Sleep((int) MeterSupportData.SimulationTickLengthSec * 1000);
                if (bw.CancellationPending)
                {
                    CommandClassSupportUtils.ReportTimerState(MeterSupportData.LogLevel, RuntimeTimerName, ReportTimerStateNames.CancelledElapsedSec, RuntimeElapsed);
                    e.Cancel = true;
                    break;
                }
            }
            if (!e.Cancel)
            {
                CommandClassSupportUtils.ReportTimerState(MeterSupportData.LogLevel, RuntimeTimerName, ReportTimerStateNames.ExpiredSec, RuntimeElapsed);
            }
        }

        private void RuntimeTimerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _resetEvent.Set();
            }
        }
        private void RuntimeTimerReset() // the Runtime Timer runs infinitely or until Result.State == ActionStates.Cancelled
        {
            if (RuntimeTimer.IsBusy)
            {
                CommandClassSupportUtils.ReportTimerState(MeterSupportData.LogLevel, RuntimeTimerName, ReportTimerStateNames.Reset, 0);
                RuntimeTimer.CancelAsync();
                _resetEvent.WaitOne();
                IsTimerStarted = false;
            }
        }
        #endregion // Runtime Timer

        private SvResultNames HandleMeterResetCmd(byte[] command, NodeTag srcNode)
        {
            if (MeterSupportData.MeterResetSupported)
            { 
                if (Version >= 2 && Version <= 5)
                {
                    CommandClassSupportUtils.ReportAddLog(MeterSupportData.LogLevel, "{0} CC @ {1}: MeterReset V2-V5 accepted.",
                                CommandClassShortName, EndPointName);
                    foreach (MeterValue scaleData in MeterSupportData.MeterValues)
                    {
                        if (scaleData.ScaleNumber.IsAccumulating() == false)
                        {
                            continue; // ignore non accumulating scales
                        }

                        bool valuesChanged = false;
                        if (scaleData.ValueProducing != 0.0
                         || scaleData.ValueConsuming != 0.0
                         || scaleData.PreviousValueProducing != 0.0
                         || scaleData.PreviousValueConsuming != 0.0)
                        {
                            valuesChanged = true;
                        }
                        scaleData.ValueProducing = 0.0;
                        scaleData.ValueConsuming = 0.0;
                        scaleData.PreviousValueProducing = 0.0;
                        scaleData.PreviousValueConsuming = 0.0;
                        if (valuesChanged && ReportTypes.HasFlag(ReportTypes.OnStateChange))
                        {
                            SendUnsolicitedReport(PrepareMeterReportByScale(scaleData.ScaleNumber, 0, true), srcNode);
                        }
                    }
                    SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
                    MeterResetOccured = true;   // Blocks the RuntimeTimer thread for 3 seconds to keep the reset value for an immediately following Meter.Get/Meter.Report
                    return SvResultNames.Successful;
                }
                if (Version == 6)
                {
                    COMMAND_CLASS_METER_V6.METER_RESET meterResetCmd = command;
                    MeterTypeNames requestedMeterType = (MeterTypeNames) meterResetCmd.properties1.meterType;
                    byte requestedRateType = meterResetCmd.properties1.rateType;
                    ScaleNames requestedScaleNumber = (ScaleNames)((meterResetCmd.properties1.scaleBit2 << 2) | meterResetCmd.properties2.scaleBits10
                                                | (meterResetCmd.scale2 << 8));
                    double resetValue = CommandClassSupportUtils.ByteListToValue(meterResetCmd.meterValue, 
                                                                                 meterResetCmd.properties2.size, 
                                                                                 meterResetCmd.properties2.precision);
                    CommandClassSupportUtils.ReportAddLog(MeterSupportData.LogLevel, 
                                "{0} CC @ {1}: MeterReset V6 received. Type: {2} (0x{3:X2}), rateType: {4}, scale: {5}, Value: {6}",
                                CommandClassShortName, EndPointName, requestedMeterType, (byte) requestedMeterType, 
                                requestedRateType, requestedScaleNumber, resetValue);

                    MeterValue requestedScaleData = MeterSupportData.MeterValues.Find(x => x.ScaleNumber == requestedScaleNumber);
                    if ((requestedScaleData != null)                        // invalid scale request -> ignore command
                     && requestedScaleData.ScaleNumber.IsAccumulating()     // ignore non accumulating scales
                     && requestedMeterType == MeterSupportData.MeterType    // invalid meter type request -> ignore command
                     && (requestedRateType <= 0x02)                         // invalid rate type request -> ignore command
                     && ((requestedRateType & (byte) MeterSupportData.SupportedRateTypes) != 0 || requestedRateType == 0x00))
                    {
                        MeterResetOccured = true;   // Blocks the RuntimeTimer thread for 3 seconds to keep the reset value for an immediately following Meter.Get/Meter.Report
                        CommandClassSupportUtils.ReportAddLog(MeterSupportData.LogLevel,
                                    "{0} CC @ {1}: MeterReset V6 accepted. Type: {2} (0x{3:X2}), rateType: {4}, scale: {5}, Value: {6}",
                                    CommandClassShortName, EndPointName, requestedMeterType, (byte) requestedMeterType,
                                    requestedRateType, requestedScaleNumber, resetValue);
                        bool valuesChanged = false;
                        switch (requestedRateType)
                        {
                            case 0x01:
                                if (requestedScaleData.ValueConsuming != resetValue
                                 || requestedScaleData.PreviousValueConsuming != resetValue)
                                {
                                    valuesChanged = true;
                                }
                                requestedScaleData.ValueConsuming = resetValue;
                                requestedScaleData.PreviousValueConsuming = resetValue;
                                break;
                            case 0x02:
                                if (requestedScaleData.ValueProducing != resetValue
                                 || requestedScaleData.PreviousValueProducing != resetValue)
                                {
                                    valuesChanged = true;
                                }
                                requestedScaleData.ValueProducing = resetValue;
                                requestedScaleData.PreviousValueProducing = resetValue;
                                break;
                        }
                        if (valuesChanged)
                        {
                            if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                            {
                                SendUnsolicitedReport(PrepareMeterReportByScale(requestedScaleNumber, requestedRateType, true), srcNode);
                            }
                            SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
                        }
                        return SvResultNames.Successful;
                    }
                }
            }
            CommandClassSupportUtils.ReportAddLog(MeterSupportData.LogLevel, 
                        "{0} CC @ {1}: MeterReset ignored.", CommandClassShortName, EndPointName);
            return SvResultNames.Failed;
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
                case COMMAND_CLASS_METER_V2.METER_RESET.ID:
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
            if (data is MeterSupportData newData)
            {
                List<byte[]> meterReports = new List<byte[]>();
                bool supportedReportRequired = false;
                RuntimeTimerName = string.Format("{0} CC @ {1}: Runtime Timer", CommandClassShortName, EndPointName);

                if (MeterSupportData.SimulateTime != newData.SimulateTime
                 || MeterSupportData.SimulateConsuming != newData.SimulateConsuming
                 || MeterSupportData.SimulateProducing != newData.SimulateProducing
                 || MeterSupportData.SimulationTickLengthSec != newData.SimulationTickLengthSec
                 || MeterSupportData.SimulationTickLogSec != newData.SimulationTickLogSec)
                {
                    MeterSupportData.SimulateTime = newData.SimulateTime;
                    MeterSupportData.SimulateConsuming = newData.SimulateConsuming;
                    MeterSupportData.SimulateProducing = newData.SimulateProducing;
                    MeterSupportData.SimulationTickLengthSec = newData.SimulationTickLengthSec;
                    MeterSupportData.SimulationTickLogSec = newData.SimulationTickLogSec;
                }

                if (MeterSupportData.DefaultScale != newData.DefaultScale
                 || MeterSupportData.MeterResetSupported != newData.MeterResetSupported
                 || MeterSupportData.SupportedRateTypes != newData.SupportedRateTypes)
                {
                    MeterSupportData.DefaultScale = newData.DefaultScale;
                    MeterSupportData.MeterResetSupported = newData.MeterResetSupported;
                    MeterSupportData.SupportedRateTypes = newData.SupportedRateTypes;
                    supportedReportRequired = true;
                }

                if (MeterSupportData.MeterType != newData.MeterType)
                {
                    MeterSupportData.MeterType = newData.MeterType;
                    supportedReportRequired = true;
                    MeterSupportData.MeterValues.Clear();
                }

                // Check scales and their values, send necessary reports.
                List<MeterValue> scalesRemoved = MeterSupportData.MeterValues.FindAll(x => newData.MeterValues.Find(y => y.ScaleNumber == x.ScaleNumber) == null);
                if (scalesRemoved.Count > 0)
                {
                    supportedReportRequired = true;
                    scalesRemoved.ForEach(x => MeterSupportData.MeterValues.Remove(x));
                }
                foreach (MeterValue newScale in newData.MeterValues)
                {
                    bool valuesChanged = false;
                    if (MeterSupportData.MeterValues.Find(x => x.ScaleNumber == newScale.ScaleNumber) is MeterValue currentScale)
                    {
                        if (currentScale.SizeConsuming != newScale.SizeConsuming
                            || currentScale.SizeProducing != newScale.SizeProducing
                            || currentScale.PrecisionConsuming != newScale.PrecisionConsuming
                            || currentScale.PrecisionProducing != newScale.PrecisionProducing
                            || currentScale.ValueConsuming != newScale.ValueConsuming
                            || currentScale.ValueProducing != newScale.ValueProducing
                            || currentScale.PreviousValueConsuming != newScale.PreviousValueConsuming
                            || currentScale.PreviousValueProducing != newScale.PreviousValueProducing
                            || currentScale.DeltaTimeConsuming != newScale.DeltaTimeConsuming
                            || currentScale.DeltaTimeProducing != newScale.DeltaTimeProducing
                        )
                        {
                            valuesChanged = true;
                            MeterSupportData.MeterValues.Remove(currentScale);
                            MeterSupportData.MeterValues.Add(newScale);
                        }
                    }
                    else
                    {
                        supportedReportRequired = true;
                        valuesChanged = true;
                        MeterSupportData.MeterValues.Add(newScale);
                    }

                    if (valuesChanged)
                    {
                        if (MeterSupportData.SupportedRateTypes == RateTypeNames.Import || MeterSupportData.SupportedRateTypes == RateTypeNames.ImportAndExport)
                        {
                            meterReports.Add(PrepareMeterReportByScale(newScale.ScaleNumber, (byte)RateTypeNames.Import, false));
                        }

                        if (MeterSupportData.SupportedRateTypes == RateTypeNames.Export || MeterSupportData.SupportedRateTypes == RateTypeNames.ImportAndExport)
                        {
                            meterReports.Add(PrepareMeterReportByScale(newScale.ScaleNumber, (byte)RateTypeNames.Export, false));
                        }
                    }
                }

                if (supportedReportRequired && ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                {
                    SendUnsolicitedReport(PrepareSupportedReport(), NodeTag.Empty);
                }
                foreach (byte[] r in meterReports)
                {
                    SendUnsolicitedReport(r, NodeTag.Empty);
                }

                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class MeterValue : INestedItem
    {
        // V1+ Meter Report (Consuming members only!)
        public ScaleNames ScaleNumber { get; set; } = ScaleNames.ScalekWh;
        public byte SizeConsuming { get; set; } = 1;                    // 0 = unknown; 1, 2, 4 = size
        public byte SizeProducing { get; set; } = 1;                    // 0 = unknown; 1, 2, 4 = size
        public byte PrecisionConsuming { get; set; } = 0;               // 0..7
        public byte PrecisionProducing { get; set; } = 0;               // 0..7
        public double ValueConsuming { get; set; } = 0.0;
        public double ValueProducing { get; set; } = 0.0;
        // V2+ Meter Report
        public double PreviousValueConsuming { get; set; } = 0.0;
        public double PreviousValueProducing { get; set; } = 0.0;
        public ushort DeltaTimeConsuming { get; set; } = 0;             // Currently not affected by RuntimeTimer
        public ushort DeltaTimeProducing { get; set; } = 0;             // Currently not affected by RuntimeTimer
        internal uint LastMeterReportTimeConsuming { get; set; } = 0;   // Internal value for runtime simulation, not for UI
        internal uint LastMeterReportTimeProducing { get; set; } = 0;   // Internal value for runtime simulation, not for UI

        // only used for EDE UI. see interface for useage notes.
        [JsonIgnore]
        public int NestedId
        {
            get => (byte)ScaleNumber;
            set { }
        }
    };

    public class MeterSupportData : CommandClassSupportData, INodeItem
    {
        // Internals accessible from TestCases or EDE
        public int SimulateTime { get; set; } = 0;          // 0 = off (uses MeterSupportData time values), 1 = on
        public int SimulateConsuming { get; set; } = 0;     // 0 = off; 1 = runs with consuming (same value for all scales)
        public int SimulateProducing { get; set; } = 0;     // 0 = off; 1 = runs with producing (same value for all scales)
        public uint SimulationTickLengthSec { get; set; } = 1;  // Length of a consuming/producing 'tick' in seconds
        public uint SimulationTickLogSec { get; set; } = 10;    // Logging each SimulationTickLogSec seconds

        public MeterTypeNames MeterType { get; set; } = MeterTypeNames.Electric; // Set default to Electric Meter so EDE shows input fields right away, CTT TCs should set this explicitely
        public ScaleNames DefaultScale { get; set; } = 0x00;      // 0x00 ... 0x03 (Spec Requirement) according to the supported Meter Type

        // V2+ Supported Report
        [CustomDataParameter(2, "Meter Reset Supported", UiControl.CheckBox, InputType.Boolean, EditType.Config)]
        public bool MeterResetSupported { get; set; } = false;
        // V4+ Supported Report
        public RateTypeNames SupportedRateTypes { get; set; } = RateTypeNames.Import;   // 0x00 = Reserved; 0x01 = Import (consumed); 0x02 = Export (produced); 0x03 = Import and Export supported (V4+)

        // V1+ Meter Report
        [NestedControl(1, "Meter Values")]
        public List<MeterValue> MeterValues { get; set; } = new List<MeterValue>();

        [JsonIgnore]
        public IEnumerable<INestedItem> ChildItems => MeterValues;

        public Type GetAvailableScaleNamesEnumType()
        {
            switch (MeterType)
            {
                case MeterTypeNames.Invalid:
                    return typeof(InvalidScaleNames); // dummy
                case MeterTypeNames.Electric:
                    return typeof(ElectricScaleNames);
                case MeterTypeNames.Gas:
                    return typeof(GasScaleNames);
                case MeterTypeNames.Water:
                    return typeof(WaterScaleNames);
                case MeterTypeNames.Heating:
                    return typeof(HeatingScaleNames);
                case MeterTypeNames.Cooling:
                    return typeof(CoolingScaleNames);
                default:
                    throw new Exception($"Unmapped Notification Type: {MeterType}!");
            }
        }
    }

    public static class MeterSupportUtils
    {
        public static void ScaleFieldsV4(ScaleNames scaleName, out byte scaleBits10, out byte scaleBit2, out ByteValue scale2)
        {
            bool isScale2Needed = (byte)scaleName >= 8; // M.S.T. == 0x07

            scaleBits10 = isScale2Needed ? (byte)3 : (byte)((byte)scaleName & 0x03);
            scaleBit2 = isScale2Needed ? (byte)1 : (byte)(((byte)scaleName & 0x04) >> 2);
            scale2 = isScale2Needed ? (ByteValue)((byte)scaleName - 8) : ByteValue.Empty;
        }

        public static bool IsAccumulating(this ScaleNames scaleName)
        {
            var field = typeof(ScaleNames).GetField(scaleName.ToString());
            var attribute = field.GetCustomAttribute<CategoryAttribute>();
            return attribute != null && attribute.Category == MeasurementMode.Accumulating;
        }
    }

    public static class MeasurementMode
    {
        public const string Instantaneous = "instantaneous";
        public const string Accumulating = "accumulating";
    }

    public enum MeterTypeNames : byte
    {
        [Description("0x00 Invalid")]
        Invalid  = 0x00,
        [Description("0x01 Electric")]
        Electric = 0x01,
        [Description("0x02 Gas")]
        Gas      = 0x02,
        [Description("0x03 Water")]
        Water    = 0x03,
        [Description("0x04 Heating")]
        Heating  = 0x04,
        [Description("0x05 Cooling")]
        Cooling  = 0x05,
    }
    public enum RateTypeNames : byte
    {
        [Description("0x00 Reserved")]
        Reserved = 0x00,
        [Description("0x01 Import")]
        Import = 0x01,
        [Description("0x02 Export")]
        Export = 0x02,
        [Description("0x03 Import & Export")]
        ImportAndExport = 0x03,
    }

    public enum ScaleNames : byte
    {
        // The values match the Supported Report Bitmask for each Meter Type
        [Description("0 kWh")]
        [Category(MeasurementMode.Accumulating)]
        ScalekWh = 0,               // E      H C
        [Description("1 kVAh")]
        [Category(MeasurementMode.Accumulating)]
        ScalekVAh = 1,              // E
        [Description("2 W")]
        [Category(MeasurementMode.Instantaneous)]
        ScaleW = 2,                 // E
        [Description("3 Pulse Count")]
        [Category(MeasurementMode.Accumulating)]
        ScalePulseCount = 3,        // E G W
        [Description("4 V")]
        [Category(MeasurementMode.Instantaneous)]
        ScaleV = 4,                 // E
        [Description("5 A")]
        [Category(MeasurementMode.Instantaneous)]
        ScaleA = 5,                 // E
        [Description("6 Power Factor")]
        [Category(MeasurementMode.Instantaneous)]
        ScalePowerFactor = 6,       // E
        [Description("8 kVar")]
        [Category(MeasurementMode.Instantaneous)]
        ScalekVar = 8,              // E
        [Description("9 kVarh")]
        [Category(MeasurementMode.Accumulating)]
        ScalekVarh = 9,             // E
        [Description("0 Cubic Meter")]
        [Category(MeasurementMode.Accumulating)]
        ScaleCubicMeter = 0,        //   G W
        [Description("1 Cubic Feet")]
        [Category(MeasurementMode.Accumulating)]
        ScaleCubicFeet = 1,         //   G W
        [Description("2 US Gallons")]
        [Category(MeasurementMode.Accumulating)]
        ScaleUsGallons = 2,         //     W
    }

    public enum InvalidScaleNames : byte { }

    public enum ElectricScaleNames : byte
    {
        // The values match the Supported Report Bitmask for each Meter Type
        [Description("0 kWh")]
        [Category(MeasurementMode.Accumulating)]
        ScalekWh = 0,               // E      H C
        [Description("1 kVAh")]
        [Category(MeasurementMode.Accumulating)]
        ScalekVAh = 1,              // E
        [Description("2 W")]
        [Category(MeasurementMode.Instantaneous)]
        ScaleW = 2,                 // E
        [Description("3 Pulse Count")]
        [Category(MeasurementMode.Accumulating)]
        ScalePulseCount = 3,        // E G W
        [Description("4 V")]
        [Category(MeasurementMode.Instantaneous)]
        ScaleV = 4,                 // E
        [Description("5 A")]
        [Category(MeasurementMode.Instantaneous)]
        ScaleA = 5,                 // E
        [Description("6 Power Factor")]
        [Category(MeasurementMode.Instantaneous)]
        ScalePowerFactor = 6,       // E
        [Description("8 kVar")]
        [Category(MeasurementMode.Instantaneous)]
        ScalekVar = 8,              // E
        [Description("9 kVarh")]
        [Category(MeasurementMode.Accumulating)]
        ScalekVarh = 9,             // E
    }

    public enum GasScaleNames : byte
    {
        // The values match the Supported Report Bitmask for each Meter Type
        [Description("3 Pulse Count")]
        [Category(MeasurementMode.Accumulating)]
        ScalePulseCount = 3,        // E G W
        [Description("0 Cubic Meter")]
        [Category(MeasurementMode.Accumulating)]
        ScaleCubicMeter = 0,        //   G W
        [Description("1 Cubic Feet")]
        [Category(MeasurementMode.Accumulating)]
        ScaleCubicFeet = 1,         //   G W
    }

    public enum WaterScaleNames : byte
    {
        // The values match the Supported Report Bitmask for each Meter Type
        [Description("3 Pulse Count")]
        [Category(MeasurementMode.Accumulating)]
        ScalePulseCount = 3,        // E G W
        [Description("0 Cubic Meter")]
        [Category(MeasurementMode.Accumulating)]
        ScaleCubicMeter = 0,        //   G W
        [Description("1 Cubic Feet")]
        [Category(MeasurementMode.Accumulating)]
        ScaleCubicFeet = 1,         //   G W
        [Description("2 US Gallons")]
        [Category(MeasurementMode.Accumulating)]
        ScaleUsGallons = 2,         //     W
    }

    public enum HeatingScaleNames : byte
    {
        // The values match the Supported Report Bitmask for each Meter Type
        [Description("0 kWh")]
        [Category(MeasurementMode.Accumulating)]
        ScalekWh = 0,               // E      H C
    }
    public enum CoolingScaleNames : byte
    {
        // The values match the Supported Report Bitmask for each Meter Type
        [Description("0 kWh")]
        [Category(MeasurementMode.Accumulating)]
        ScalekWh = 0,               // E      H C
    }
}
