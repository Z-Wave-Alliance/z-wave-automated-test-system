// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.Devices;
using ZWave.Enums;
using Newtonsoft.Json;
using Utils;
using ZWave.BasicApplication.CommandClasses;
using ZatsServices_netcore.CommandClassesImpl.Definitions.Interfaces;
using static Zats.Services.ZipResult;
using System.Timers;
using ZWave.Xml.Application;
using Renci.SshNet.Common;
using static Zats.Services.AssociationGroupEx;


// ToDo: Proper Supervision return codes
// ToDo: Activate code for Battery Low check V6+ if BatterySupportData object is available

namespace Zats.Services
{
    [CommandClassName("Firmware Update Command Class")]
    [CommandClassId(COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.ID)]
    [MinVersion(3)]
    [MaxVersion(7)]

    public class FirmwareUpdateMdSupport : CommandClassSupport
    {
        private FirmwareUpdateMdSupportData FirmwareUpdateMdSupportData { get; set; }

        public override CommandClassSupportData CommandClassSupportData
        {
            get { return FirmwareUpdateMdSupportData; }
            set { FirmwareUpdateMdSupportData = (FirmwareUpdateMdSupportData)value; }
        }

        private readonly BackgroundWorker LocalTimer = new BackgroundWorker();
        private string LocalTimerName = "";
        private readonly AutoResetEvent _resetEvent = new AutoResetEvent(false);

        public ushort TimeoutLengthSec = 0;
        public ushort RemainingTimeSec = 0;

        private NodeTag CmdSrcNode = NodeTag.Empty;

        public FirmwareUpdateMdSupport()
            : this(new FirmwareUpdateMdSupportData())
        { }

        public FirmwareUpdateMdSupport(FirmwareUpdateMdSupportData firmwareUpdateMdSupportData)
            : base(COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.ID)
        {
            FirmwareUpdateMdSupportData = firmwareUpdateMdSupportData;

            LocalTimer.DoWork += new DoWorkEventHandler(LocalTimerDoWork);
            LocalTimer.RunWorkerCompleted += new RunWorkerCompletedEventHandler(LocalTimerRunWorkerCompleted);
            LocalTimer.WorkerSupportsCancellation = true;
            LocalTimer.WorkerReportsProgress = false;
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

                if (isHandlingAllowed && command != null && command.Length > 1
                    && FirmwareUpdateMdSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_FIRMWARE_UPDATE_MD.FIRMWARE_MD_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareFirmwareMdReport();
                    }
                    else if (command[1] == COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_REQUEST_GET.ID && !IsSupervised())
                    {
                        CmdSrcNode = sendSrcNode;
                        List<byte[]> reportList = HandleFwuMdRequestGet(command);
                        if (reportList != null && IsAnsweringAllowed())
                        {   // if image transfer is initiated, the Md Request Report and one Md Get Command will be sent
                            // if not, only the Md Request Report will be sent
                            IActionItem[] actionItems = new IActionItem[reportList.Count];
                            for (int i = 0; i < reportList.Count; i++)
                            {
                                actionItems[i] = new SendDataExOperation(_network, ReceivedAchData.DstNode, ReceivedAchData.SrcNode, reportList[i], 
                                    TxOptions, TxSecOptions, (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme, TxOptions2);
                            }
                            ou.SetNextActionItems(actionItems);
                        }
                    }
                    else if (command[1] == COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_REPORT.ID)
                    {
                        CmdSrcNode = sendSrcNode;
                        rpt = HandleFwuMdReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_FIRMWARE_UPDATE_MD_V4.FIRMWARE_UPDATE_ACTIVATION_SET.ID)
                    {
                        rpt = PrepareActivationStatusReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_PREPARE_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareFwuMdDownloadPrepareReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_GET.ID && !IsSupervised())
                    {
                        List<byte[]> reportList = PrepareFwuMdDownloadReport(command);
                        if (reportList != null && IsAnsweringAllowed())
                        {
                            IActionItem[] actionItems = new IActionItem[reportList.Count];
                            for (int i = 0; i < reportList.Count; i++)
                            {
                                actionItems[i] = new SendDataExOperation(_network, ReceivedAchData.DstNode, ReceivedAchData.SrcNode, reportList[i], 
                                    TxOptions, TxSecOptions, (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme, TxOptions2);
                            }
                            ou.SetNextActionItems(actionItems);
                        }
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

        private List<byte[]> PrepareFwuMdDownloadReport(byte[] command)
        {
            if (Version >= 5)
            {
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_GET getCmd = (COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_GET)command;
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REPORT rpt = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REPORT();
                byte numberOfReports = getCmd.numberOfReports;
                ushort firstReportNumber = (ushort)((getCmd.properties1.reportNumber1 << 8) + getCmd.reportNumber2);
                List<byte[]> reportList = new List<byte[]>();
                ushort fragmentSize = FirmwareUpdateMdSupportData.DownloadState.RequestedFragmentSize;
                byte targetNumber = FirmwareUpdateMdSupportData.DownloadState.RequestedTargetNumber;

                byte[] image = FirmwareUpdateMdSupportData.Firmware0Image;
                if (targetNumber > 0)
                {
                    FirmwareTarget firmwareTarget = FirmwareUpdateMdSupportData.FirmwareTargets.Find(x => x.TargetNumber == targetNumber);
                    if (firmwareTarget == null)
                    {
                        return null;
                    }
                    image = firmwareTarget.FirmwareImage;
                }

                for (ushort reportNumber = firstReportNumber; reportNumber < firstReportNumber + numberOfReports; reportNumber++)
                {
                    byte isLast = 0;
                    int startOffset = fragmentSize * (reportNumber - 1);
                    if (startOffset > image.Length)
                    {
                        // If the requested firstReportNumber exceeds the image length, the support implementation 
                        // does not return a response frame (null). This case is not mentioned in spec. 
                        // A status report to signal this controller error is not defined.
                        return reportList.Count > 0 ? reportList : null;
                    }
                    if (startOffset + fragmentSize > image.Length)
                    {
                        fragmentSize = (ushort) (image.Length - startOffset);
                    }
                    if (startOffset + fragmentSize >= image.Length)
                    {
                        isLast = 1;
                    }
                    if (fragmentSize > 0)
                    {
                        byte[] data = new byte[fragmentSize];
                        System.Buffer.BlockCopy(image, startOffset, data, 0, fragmentSize);
                        rpt.data = data;
                    }
                    else
                    {
                        rpt.data = new byte[] { };
                    }
                    rpt.properties1.last = isLast;
                    rpt.properties1.reportNumber1 = (byte)(reportNumber / 256);
                    rpt.reportNumber2 = (byte)(reportNumber % 256);
                    rpt.checksum = Tools.CalculateCrc16Array(((byte[])rpt).Take(4 + fragmentSize));
                    reportList.Add(rpt);
                    if (isLast == 1)
                    {
                        FirmwareUpdateMdSupportData.DownloadState.IsDownloading = false;
                        break;
                    }
                }
                return reportList;
            }
            return null;
        }

        private byte[] PrepareFwuMdDownloadPrepareReport(byte[] command)
        {
            if (Version >= 5)
            {
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_PREPARE_GET getCmd = (COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_PREPARE_GET)command;
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_PREPARE_REPORT rpt = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_PREPARE_REPORT();
                ushort fragmentSize = (ushort)CommandClassSupportUtils.ByteFieldToUInt64(getCmd.fragmentSize, 2);
                byte targetNumber = getCmd.firmwareTarget;
                FirmwareMdPrepareStatusNames status = FirmwareMdPrepareStatusNames.InitiatingTransfer;
                rpt.firmwareChecksum = new byte[] { 0x00, 0x00 };

                rpt.status = (byte)FirmwareMdPrepareStatusNames.InitiatingTransfer;
                if (!getCmd.manufacturerId.SequenceEqual(FirmwareUpdateMdSupportData.ManufacturerId))
                {
                    status = FirmwareMdPrepareStatusNames.InvalidIds;
                }
                else if (targetNumber > FirmwareUpdateMdSupportData.FirmwareTargets.Count)
                {
                    status = FirmwareMdPrepareStatusNames.IsNotDownloadable;
                }
                else if (fragmentSize > FirmwareUpdateMdSupportData.MaxFragmentSize || fragmentSize < 1)
                {
                    status = FirmwareMdPrepareStatusNames.InvalidFragmentSize;
                }
                else if (!FirmwareUpdateMdSupportData.DownloadState.IsAuthenticated)
                {
                    status = FirmwareMdPrepareStatusNames.NoAuth;
                }
                else if (getCmd.hardwareVersion != FirmwareUpdateMdSupportData.HardwareVersion)
                {
                    status = FirmwareMdPrepareStatusNames.InvalidHardwareVersion;
                }

                if (status == FirmwareMdPrepareStatusNames.InitiatingTransfer)
                {
                    if (targetNumber > 0)
                    {
                        FirmwareTarget firmwareTarget = FirmwareUpdateMdSupportData.FirmwareTargets.Find(x => x.TargetNumber == getCmd.firmwareTarget);
                        if (firmwareTarget == null)
                        {
                            status = FirmwareMdPrepareStatusNames.InvalidIds;
                        }
                        else if (!getCmd.firmwareId.SequenceEqual(firmwareTarget.FirmwareId))
                        {
                            status = FirmwareMdPrepareStatusNames.InvalidIds;
                        }
                        else
                        {
                            rpt.firmwareChecksum = firmwareTarget.FirmwareChecksum;
                        }
                    }
                    else
                    {
                        if (!getCmd.firmwareId.SequenceEqual(FirmwareUpdateMdSupportData.Firmware0Id))
                        {
                            status = FirmwareMdPrepareStatusNames.InvalidIds;
                        }
                        else
                        {
                            rpt.firmwareChecksum = FirmwareUpdateMdSupportData.Firmware0Checksum;
                        }
                    }
                }
                if (status == FirmwareMdPrepareStatusNames.InitiatingTransfer)
                {
                    FirmwareUpdateMdSupportData.DownloadState = new FirmwareDownloadState();
                    FirmwareUpdateMdSupportData.DownloadState.IsDownloading = true;
                    FirmwareUpdateMdSupportData.DownloadState.RequestedFragmentSize = fragmentSize;
                    FirmwareUpdateMdSupportData.DownloadState.RequestedTargetNumber = targetNumber;
                }
                rpt.status = (byte)status;
                return rpt;
            }
            return null;
        }

        private byte[] PrepareActivationStatusReport(byte[] command)
        {
            if (Version >= 4)
            {
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V4.FIRMWARE_UPDATE_ACTIVATION_SET setCmd = (COMMAND_CLASS_FIRMWARE_UPDATE_MD_V4.FIRMWARE_UPDATE_ACTIVATION_SET)command;
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V4.FIRMWARE_UPDATE_ACTIVATION_STATUS_REPORT statusRpt = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V4.FIRMWARE_UPDATE_ACTIVATION_STATUS_REPORT();
                statusRpt.manufacturerId = setCmd.manufacturerId;
                statusRpt.firmwareId = setCmd.firmwareId;
                statusRpt.checksum = setCmd.checksum;
                statusRpt.firmwareTarget = setCmd.firmwareTarget;
                statusRpt.firmwareUpdateStatus = (byte)FirmwareMdActivationStatusNames.Success;

                if (!setCmd.manufacturerId.SequenceEqual(FirmwareUpdateMdSupportData.ManufacturerId))
                {
                    statusRpt.firmwareUpdateStatus = (byte)FirmwareMdActivationStatusNames.VersionMismatch;
                }
                if (FirmwareUpdateMdSupportData.UploadState.RequestedTargetNumber > 0)
                {
                    FirmwareTarget firmwareTarget = FirmwareUpdateMdSupportData.FirmwareTargets.Find(x => x.TargetNumber == FirmwareUpdateMdSupportData.UploadState.RequestedTargetNumber);
                    if (firmwareTarget == null)
                    {
                        statusRpt.firmwareUpdateStatus = (byte)FirmwareMdActivationStatusNames.VersionMismatch;
                    }
                }
                if (!setCmd.checksum.SequenceEqual(FirmwareUpdateMdSupportData.UploadState.ProvidedImageChecksum))
                {
                    statusRpt.firmwareUpdateStatus = (byte)FirmwareMdActivationStatusNames.VersionMismatch;
                }
                if (statusRpt.firmwareUpdateStatus == (byte)FirmwareMdActivationStatusNames.Success)
                {
                    ActivateUploadedFirmware();
                }
                return statusRpt;
            }
            return null;
        }

        private byte[] PrepareFirmwareMdReport()
        {
            if (Version == 3 || Version == 4)
            {
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_MD_REPORT rpt = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_MD_REPORT();
                rpt.manufacturerId = FirmwareUpdateMdSupportData.ManufacturerId;
                rpt.firmware0Id = FirmwareUpdateMdSupportData.Firmware0Id;
                rpt.firmware0Checksum = FirmwareUpdateMdSupportData.Firmware0Checksum;
                rpt.firmwareUpgradable = (byte)(FirmwareUpdateMdSupportData.Firmware0IsUpgradable ? 0xFF : 0x00);
                rpt.maxFragmentSize = CommandClassSupportUtils.UInt64ToByteField(FirmwareUpdateMdSupportData.MaxFragmentSize, 2);
                rpt.numberOfFirmwareTargets = (byte)FirmwareUpdateMdSupportData.FirmwareTargets.Count;
                rpt.vg1 = new List<COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_MD_REPORT.TVG1>();
                foreach (FirmwareTarget firmwareTarget in FirmwareUpdateMdSupportData.FirmwareTargets)
                {
                    rpt.vg1.Add(new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_MD_REPORT.TVG1() { firmwareId = firmwareTarget.FirmwareId });
                }
                return rpt;
            }
            if (Version == 5)
            {
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_REPORT rpt = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_REPORT();
                rpt.manufacturerId = FirmwareUpdateMdSupportData.ManufacturerId;
                rpt.firmware0Id = FirmwareUpdateMdSupportData.Firmware0Id;
                rpt.firmware0Checksum = FirmwareUpdateMdSupportData.Firmware0Checksum;
                rpt.firmwareUpgradable = (byte)(FirmwareUpdateMdSupportData.Firmware0IsUpgradable ? 0xFF : 0x00);
                rpt.maxFragmentSize = CommandClassSupportUtils.UInt64ToByteField(FirmwareUpdateMdSupportData.MaxFragmentSize, 2);
                rpt.numberOfFirmwareTargets = (byte)FirmwareUpdateMdSupportData.FirmwareTargets.Count;
                rpt.vg1 = new List<COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_REPORT.TVG1>();
                foreach (FirmwareTarget firmwareTarget in FirmwareUpdateMdSupportData.FirmwareTargets)
                {
                    rpt.vg1.Add(new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_REPORT.TVG1() { firmwareId = firmwareTarget.FirmwareId });
                }
                rpt.hardwareVersion = FirmwareUpdateMdSupportData.HardwareVersion;
                return rpt;
            }
            if (Version == 6)
            {
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V6.FIRMWARE_MD_REPORT rpt = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V6.FIRMWARE_MD_REPORT();
                rpt.manufacturerId = FirmwareUpdateMdSupportData.ManufacturerId;
                rpt.firmware0Id = FirmwareUpdateMdSupportData.Firmware0Id;
                rpt.firmware0Checksum = FirmwareUpdateMdSupportData.Firmware0Checksum;
                rpt.firmwareUpgradable = (byte)(FirmwareUpdateMdSupportData.Firmware0IsUpgradable ? 0xFF : 0x00);
                rpt.maxFragmentSize = CommandClassSupportUtils.UInt64ToByteField(FirmwareUpdateMdSupportData.MaxFragmentSize, 2);
                rpt.numberOfFirmwareTargets = (byte)FirmwareUpdateMdSupportData.FirmwareTargets.Count;
                rpt.vg1 = new List<COMMAND_CLASS_FIRMWARE_UPDATE_MD_V6.FIRMWARE_MD_REPORT.TVG1>();
                foreach (FirmwareTarget firmwareTarget in FirmwareUpdateMdSupportData.FirmwareTargets)
                {
                    rpt.vg1.Add(new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V6.FIRMWARE_MD_REPORT.TVG1() { firmwareId = firmwareTarget.FirmwareId });
                }
                rpt.hardwareVersion = FirmwareUpdateMdSupportData.HardwareVersion;
                rpt.properties1.cc = (byte)(FirmwareUpdateMdSupportData.IsWorkingDuringUpload ? 0x01 : 0x00);
                return rpt;
            }
            if (Version == 7)
            {
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V7.FIRMWARE_MD_REPORT rpt = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V7.FIRMWARE_MD_REPORT();
                rpt.manufacturerId = FirmwareUpdateMdSupportData.ManufacturerId;
                rpt.firmware0Id = FirmwareUpdateMdSupportData.Firmware0Id;
                rpt.firmware0Checksum = FirmwareUpdateMdSupportData.Firmware0Checksum;
                rpt.firmwareUpgradable = (byte)(FirmwareUpdateMdSupportData.Firmware0IsUpgradable ? 0xFF : 0x00);
                rpt.maxFragmentSize = CommandClassSupportUtils.UInt64ToByteField(FirmwareUpdateMdSupportData.MaxFragmentSize, 2);
                rpt.numberOfFirmwareTargets = (byte)FirmwareUpdateMdSupportData.FirmwareTargets.Count;
                rpt.vg1 = new List<COMMAND_CLASS_FIRMWARE_UPDATE_MD_V7.FIRMWARE_MD_REPORT.TVG1>();
                foreach (FirmwareTarget firmwareTarget in FirmwareUpdateMdSupportData.FirmwareTargets)
                {
                    rpt.vg1.Add(new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V7.FIRMWARE_MD_REPORT.TVG1() { firmwareId = firmwareTarget.FirmwareId });
                }
                rpt.hardwareVersion = FirmwareUpdateMdSupportData.HardwareVersion;
                rpt.properties1.cc = (byte)(FirmwareUpdateMdSupportData.IsWorkingDuringUpload ? 0x01 : 0x00);
                rpt.properties1.cc = (byte)(FirmwareUpdateMdSupportData.IsSupportingSubsequentActivation ? 0x01 : 0x00);
                return rpt;
            }
            return null;
        }

        private bool ActivateUploadedFirmware()
        {
            if (FirmwareUpdateMdSupportData.UploadState.RequestedTargetNumber == 0)
            {
                FirmwareUpdateMdSupportData.Firmware0Image = FirmwareUpdateMdSupportData.UploadState.UploadedImage;
                FirmwareUpdateMdSupportData.Firmware0Checksum = FirmwareUpdateMdSupportData.UploadState.ProvidedImageChecksum;
            }
            if (FirmwareUpdateMdSupportData.UploadState.RequestedTargetNumber > 0)
            {
                FirmwareTarget firmwareTarget = FirmwareUpdateMdSupportData.FirmwareTargets.Find(x => x.TargetNumber == FirmwareUpdateMdSupportData.UploadState.RequestedTargetNumber);
                if (firmwareTarget == null)
                {
                    return false;
                }
                firmwareTarget.FirmwareImage = FirmwareUpdateMdSupportData.UploadState.UploadedImage;
                firmwareTarget.FirmwareChecksum = FirmwareUpdateMdSupportData.UploadState.ProvidedImageChecksum;
            }
            return true;
        }

        private byte[] HandleFwuMdReport(byte[] command)
        {
            COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_REPORT fwuMdRpt = (COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_REPORT)command;
            COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_STATUS_REPORT statusRpt = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_STATUS_REPORT();
            bool isLastReport = fwuMdRpt.properties1.last == 0x00 ? false : true;
            ushort reportNumber = (ushort)((fwuMdRpt.properties1.reportNumber1 << 8) + fwuMdRpt.reportNumber2);
            byte[] crc16Cmd = Tools.CalculateCrc16Array(command.Take(command.Length - 2));
            statusRpt.waittime = CommandClassSupportUtils.UInt64ToByteField(FirmwareUpdateMdSupportData.WaitTime, 2);

            LocalTimerCancel();

            if (!FirmwareUpdateMdSupportData.UploadState.IsUploading)
            {
                return null;
            }
            if (!crc16Cmd.SequenceEqual(fwuMdRpt.checksum)
             && !FirmwareUpdateMdSupportData.IgnoreChecksumErrors)
            {
                // Cancel transfer with state 0x00
                statusRpt.status = (byte)FirmwareMdStatusReportNames.ChecksumError;
                return statusRpt;
            }
            if (reportNumber != FirmwareUpdateMdSupportData.UploadState.RequestedReportNumber)
            {
                // Cancel transfer with state 0x01
                statusRpt.status = (byte)FirmwareMdStatusReportNames.SequenceError;
                return statusRpt;
            }
            FirmwareUpdateMdSupportData.UploadState.UploadedSegments.Add(fwuMdRpt.data.ToArray());
            FirmwareUpdateMdSupportData.UploadState.UploadedLength += fwuMdRpt.data.Count;
            if (isLastReport)
            {
                // End transfer with state = result of checks ManufacturerId, FirmwareId, FirmwareTarget, Checksum
                // ManufacturerId, FirmwareId and FirmwareTarget are already checked in HandleFwuMdRequestGet()
                FirmwareUpdateMdSupportData.UploadState.UploadedImage = new byte[FirmwareUpdateMdSupportData.UploadState.UploadedLength];
                int ImageLength = 0;
                foreach (byte[] s in FirmwareUpdateMdSupportData.UploadState.UploadedSegments)
                {
                    System.Buffer.BlockCopy(s, 0, FirmwareUpdateMdSupportData.UploadState.UploadedImage, ImageLength, s.Length);
                    ImageLength += s.Length;
                }
                FirmwareUpdateMdSupportData.UploadState.UploadedSegments.Clear();
                FirmwareUpdateMdSupportData.UploadState.IsUploading = false;
                FirmwareUpdateMdSupportData.UploadState.ReceivedSegmentsPerGet = 0;

                byte[] crc16Image = Tools.CalculateCrc16Array(FirmwareUpdateMdSupportData.UploadState.UploadedImage);
                if (!crc16Image.SequenceEqual(FirmwareUpdateMdSupportData.UploadState.ProvidedImageChecksum)
                 && !FirmwareUpdateMdSupportData.IgnoreChecksumErrors)
                {
                    statusRpt.status = (byte)FirmwareMdStatusReportNames.ChecksumError;
                    return statusRpt;
                }
                if (FirmwareUpdateMdSupportData.UploadState.RequestedTargetNumber > 0)
                {
                    FirmwareTarget firmwareTarget = FirmwareUpdateMdSupportData.FirmwareTargets.Find(x => x.TargetNumber == FirmwareUpdateMdSupportData.UploadState.RequestedTargetNumber);
                    if (firmwareTarget == null)
                    {
                        statusRpt.status = (byte)FirmwareMdStatusReportNames.SequenceError;
                        return statusRpt;
                    }
                }

                if (FirmwareUpdateMdSupportData.UploadState.MustDelayActivation)
                {
                    statusRpt.status = (byte)FirmwareMdStatusReportNames.WaitingForActivation;
                }
                else
                {
                    ActivateUploadedFirmware();
                    statusRpt.status = (byte)FirmwareMdStatusReportNames.Success;
                }
                return statusRpt;
            }
            FirmwareUpdateMdSupportData.UploadState.RequestedReportNumber++;
            if (++FirmwareUpdateMdSupportData.UploadState.ReceivedSegmentsPerGet == FirmwareUpdateMdSupportData.ReportsPerGet)
            {
                FirmwareUpdateMdSupportData.UploadState.ReceivedSegmentsPerGet = 0;
                LocalTimerStart(FirmwareUpdateMdSupportData.TimeoutPerGet);
                return PrepareFwuMdGet();
            }
            return null;
        }

        private byte[] PrepareFwuMdGet()
        {
            COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_GET get = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_GET();
            get.numberOfReports = FirmwareUpdateMdSupportData.ReportsPerGet;
            get.properties1.reportNumber1 = (byte)(FirmwareUpdateMdSupportData.UploadState.RequestedReportNumber / 256);
            get.reportNumber2 = (byte)(FirmwareUpdateMdSupportData.UploadState.RequestedReportNumber % 256);
            return get;
        }

        private byte[] InitiateUpload(byte targetNumber, bool mayDelayActivation, bool mustDelayActivation, byte[] providedChecksum)
        {
            FirmwareUpdateMdSupportData.UploadState.RequestedTargetNumber = targetNumber;
            FirmwareUpdateMdSupportData.UploadState.IsUploading = true;
            FirmwareUpdateMdSupportData.UploadState.MayDelayActivation = mayDelayActivation;
            FirmwareUpdateMdSupportData.UploadState.ProvidedImageChecksum = providedChecksum;
            FirmwareUpdateMdSupportData.UploadState.RequestedReportNumber = 1;
            FirmwareUpdateMdSupportData.UploadState.ReceivedSegmentsPerGet = 0;
            FirmwareUpdateMdSupportData.UploadState.UploadedSegments.Clear();
            if (Version >= 7)
            {
                FirmwareUpdateMdSupportData.UploadState.MustDelayActivation = mustDelayActivation;
            }
            LocalTimerStart(FirmwareUpdateMdSupportData.TimeoutPerGet);
            return PrepareFwuMdGet();
        }

        private FirmwareMdRequestStatusNames CheckMdRequestCommand(byte[] manufacturerId, byte[] firmwareId, ushort fragmentSize, byte targetNumber, byte hardwareVersion)
        {   // Priority (High --> Low): IsNotUpgradable / InvalidIds / InvalidFragmentSize / NoAuth / InvalidHardwareVersion (V5+) / Busy (V6+) / Battery (V6+)
            if (Version >= 3)
            {
                if (targetNumber > FirmwareUpdateMdSupportData.FirmwareTargets.Count)
                {
                    return FirmwareMdRequestStatusNames.IsNotUpgradable;
                }

                if (targetNumber == 0)   // Firmware 0 = Z-Wave Chip
                {
                    if (!FirmwareUpdateMdSupportData.Firmware0IsUpgradable)
                    {
                        return FirmwareMdRequestStatusNames.IsNotUpgradable;
                    }
                    if (!firmwareId.SequenceEqual(FirmwareUpdateMdSupportData.Firmware0Id))
                    {
                        return FirmwareMdRequestStatusNames.InvalidIds;
                    }
                }
                if (targetNumber > 0)
                {
                    FirmwareTarget firmwareTarget = FirmwareUpdateMdSupportData.FirmwareTargets.Find(x => x.TargetNumber == targetNumber);
                    if (firmwareTarget == null)
                    {
                        return FirmwareMdRequestStatusNames.IsNotUpgradable;
                    }
                    if (!firmwareTarget.IsUpgradable)
                    {
                        return FirmwareMdRequestStatusNames.IsNotUpgradable;
                    }
                    if (!firmwareId.SequenceEqual(firmwareTarget.FirmwareId))
                    {
                        return FirmwareMdRequestStatusNames.InvalidIds;
                    }
                }

                if (!manufacturerId.SequenceEqual(FirmwareUpdateMdSupportData.ManufacturerId))
                {
                    return FirmwareMdRequestStatusNames.InvalidIds;
                }
                if (fragmentSize > FirmwareUpdateMdSupportData.MaxFragmentSize || fragmentSize < 1)
                {
                    return FirmwareMdRequestStatusNames.InvalidFragmentSize;
                }
                if (!FirmwareUpdateMdSupportData.UploadState.IsAuthenticated)
                {
                    return FirmwareMdRequestStatusNames.NoAuth;
                }
            }
            if (Version >= 5)
            {
                if (hardwareVersion != FirmwareUpdateMdSupportData.HardwareVersion)
                {
                    return FirmwareMdRequestStatusNames.InvalidHardwareVersion;
                }

            }
            if (Version >= 6)
            {
                if (FirmwareUpdateMdSupportData.UploadState.IsUploading || FirmwareUpdateMdSupportData.DownloadState.IsDownloading)
                {
                    return FirmwareMdRequestStatusNames.Busy;
                }
                /*if (BatterySupportData != null)
                {
                    if (BatterySupportData.BatteryLevel < BatterySupportData.WarningBatteryLevel)
                    {
                        return FirmwareMdRequestStatusNames.BatteryLow;
                    }
                }*/
            }

            return FirmwareMdRequestStatusNames.InitiatingTransfer;
        }

        private List<byte[]> HandleFwuMdRequestGet(byte[] command)
        {
            List<byte[]> reportList = new List<byte[]>();
            FirmwareUpdateMdSupportData.UploadState = new FirmwareUploadState();
            if (Version == 3)
            {
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_REQUEST_GET getCmd = (COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_REQUEST_GET)command;
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_REQUEST_REPORT rpt = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_REQUEST_REPORT();
                ushort fragmentSize = (ushort)CommandClassSupportUtils.ByteFieldToUInt64(getCmd.fragmentSize, 2);
                byte targetNumber = getCmd.firmwareTarget;
                bool mayDelayActivation = false;
                bool mustDelayActivation = false;

                FirmwareMdRequestStatusNames reportedStatus = CheckMdRequestCommand(getCmd.manufacturerId, getCmd.firmwareId, fragmentSize, targetNumber, 0);
                rpt.status = (byte)reportedStatus;
                reportList.Add(rpt);
                if (reportedStatus == FirmwareMdRequestStatusNames.InitiatingTransfer)
                {
                    reportList.Add(InitiateUpload(targetNumber, mayDelayActivation, mustDelayActivation, getCmd.checksum));
                }
                return reportList;
            }
            if (Version == 4)
            {
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V4.FIRMWARE_UPDATE_MD_REQUEST_GET getCmd = (COMMAND_CLASS_FIRMWARE_UPDATE_MD_V4.FIRMWARE_UPDATE_MD_REQUEST_GET)command;
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V4.FIRMWARE_UPDATE_MD_REQUEST_REPORT rpt = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V4.FIRMWARE_UPDATE_MD_REQUEST_REPORT();
                ushort fragmentSize = (ushort)CommandClassSupportUtils.ByteFieldToUInt64(getCmd.fragmentSize, 2);
                byte targetNumber = getCmd.firmwareTarget;
                bool mayDelayActivation = getCmd.properties1.activation == 0x00 ? false : true;
                bool mustDelayActivation = false;

                FirmwareMdRequestStatusNames reportedStatus = CheckMdRequestCommand(getCmd.manufacturerId, getCmd.firmwareId, fragmentSize, targetNumber, 0);
                rpt.status = (byte)reportedStatus;
                reportList.Add(rpt);
                if (reportedStatus == FirmwareMdRequestStatusNames.InitiatingTransfer)
                {
                    reportList.Add(InitiateUpload(targetNumber, mayDelayActivation, mustDelayActivation, getCmd.checksum));
                }
                return reportList;
            }
            if (Version == 5 || Version == 6)
            {
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_GET getCmd = (COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_GET)command;
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_REPORT rpt = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_REPORT();
                ushort fragmentSize = (ushort)CommandClassSupportUtils.ByteFieldToUInt64(getCmd.fragmentSize, 2);
                byte targetNumber = getCmd.firmwareTarget;
                bool mayDelayActivation = getCmd.properties1.activation == 0x00 ? false : true;
                bool mustDelayActivation = false;

                FirmwareMdRequestStatusNames reportedStatus = CheckMdRequestCommand(getCmd.manufacturerId, getCmd.firmwareId, fragmentSize, targetNumber, getCmd.hardwareVersion);
                rpt.status = (byte)reportedStatus;
                reportList.Add(rpt);
                if (reportedStatus == FirmwareMdRequestStatusNames.InitiatingTransfer)
                {
                    reportList.Add(InitiateUpload(targetNumber, mayDelayActivation, mustDelayActivation, getCmd.checksum));
                }
                return reportList;
            }
            if (Version == 7)
            {
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V7.FIRMWARE_UPDATE_MD_REQUEST_GET getCmd = (COMMAND_CLASS_FIRMWARE_UPDATE_MD_V7.FIRMWARE_UPDATE_MD_REQUEST_GET)command;
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V7.FIRMWARE_UPDATE_MD_REQUEST_REPORT rpt = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V7.FIRMWARE_UPDATE_MD_REQUEST_REPORT();
                ushort fragmentSize = (ushort)CommandClassSupportUtils.ByteFieldToUInt64(getCmd.fragmentSize, 2);
                byte targetNumber = getCmd.firmwareTarget;
                bool mayDelayActivation = false;
                bool mustDelayActivation = false;
                if (FirmwareUpdateMdSupportData.IsSupportingSubsequentActivation && getCmd.properties1.activation == 0x01)
                {
                    mustDelayActivation = true;
                }

                FirmwareMdRequestStatusNames reportedStatus = CheckMdRequestCommand(getCmd.manufacturerId, getCmd.firmwareId, fragmentSize, targetNumber, getCmd.hardwareVersion);
                rpt.status = (byte)reportedStatus;
                reportList.Add(rpt);
                if (reportedStatus == FirmwareMdRequestStatusNames.InitiatingTransfer)
                {
                    reportList.Add(InitiateUpload(targetNumber, mayDelayActivation, mustDelayActivation, getCmd.checksum));
                }
                return reportList;
            }
            return null;
        }

        #region Local Timer
        private void LocalTimerStart(ushort time)
        {
            LocalTimerName = string.Format("{0} CC @ {1}: Firmware Update Timer", CommandClassShortName, EndPointName);
            FirmwareUpdateMdSupportData.CancelPendingLocalTimer = LocalTimerReset;
            TimeoutLengthSec = time;
            LocalTimer.RunWorkerAsync();
        }
        private void LocalTimerDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            ReportOperationProgress(LocalTimerName, ReportTimerStateNames.Started, TimeoutLengthSec);
            for (RemainingTimeSec = TimeoutLengthSec; RemainingTimeSec > 0; RemainingTimeSec--)
            {
                Thread.Sleep(1000);

                if (RemainingTimeSec < TimeoutLengthSec && (TimeoutLengthSec - RemainingTimeSec) % 10 == 0)
                {
                    DeviceRestart(NodeTag.Empty);
                    SendUnsolicitedReport(PrepareFwuMdGet(), new NodeIdEx((byte)CmdSrcNode.Id, CmdSrcNode.EndPointId, CmdSrcNode.EndPointId != 0), default(NodeTag));
                    SupportDelegates?.DeviceSleep(this, ReportWakeupTimeout);
                }

                if (Result.State == ActionStates.Cancelled)
                {
                    LocalTimerCancel();
                }
                if (bw.CancellationPending)
                {
                    ReportOperationProgress(LocalTimerName, ReportTimerStateNames.CancelledRemainSec, RemainingTimeSec);
                    e.Cancel = true;
                    break;
                }
            }
            if (!e.Cancel)
            {
                COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_STATUS_REPORT statusRpt = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_STATUS_REPORT();
                ReportOperationProgress(LocalTimerName, ReportTimerStateNames.ExpiredSec, TimeoutLengthSec);
                statusRpt.status = (byte)FirmwareMdStatusReportNames.SequenceError;
                SendUnsolicitedReport(statusRpt, new NodeIdEx((byte)CmdSrcNode.Id, CmdSrcNode.EndPointId, CmdSrcNode.EndPointId != 0), default(NodeTag));
            }
            RemainingTimeSec = 0;
            TimeoutLengthSec = 0;

            SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
        }
        private void LocalTimerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _resetEvent.Set();
            }
        }
        private void LocalTimerCancel()
        {
            FirmwareUpdateMdSupportData.CancelPendingLocalTimer?.Invoke();
            FirmwareUpdateMdSupportData.CancelPendingLocalTimer = null;
        }

        // This method MUST only be called via <CommandClass>SupportData.CancelPending<CommandName>.Invoke();
        private void LocalTimerReset()
        {
            if (LocalTimer.IsBusy)
            {
                ReportOperationProgress(LocalTimerName, ReportTimerStateNames.Reset, 0);
                LocalTimer.CancelAsync();
                _resetEvent.WaitOne();
            }
        }
        #endregion // Local Timer

        private void ReportOperationProgress(string timerName, ReportTimerStateNames timerState, int seconds)
        {
            switch (timerState)
            {
                case ReportTimerStateNames.Started: CommandClassSupportUtils.ReportAddLog(FirmwareUpdateMdSupportData.LogLevel, "{0} started: {1} seconds.", timerName, seconds); break;
                case ReportTimerStateNames.ExpiredSec: CommandClassSupportUtils.ReportTimerState(FirmwareUpdateMdSupportData.LogLevel, timerName, timerState, seconds); break;
                case ReportTimerStateNames.CancelledRemainSec: CommandClassSupportUtils.ReportTimerState(FirmwareUpdateMdSupportData.LogLevel, timerName, timerState, seconds); break;
                case ReportTimerStateNames.Reset: CommandClassSupportUtils.ReportTimerState(FirmwareUpdateMdSupportData.LogLevel, timerName, timerState, seconds); break;
            }
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            if (supervisionGet.encapsulatedCommand[0] != CommandClassId)
            {
                supervisionReport.status = 0x00;                    // NO_SUPPORT
                return;
            }

            // TODO: Implement proper Supervision return codes (see BinarySwitchSupport.cs for an example)
            supervisionReport.status = 0xFF;                        // SUCCESS
        }


        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is FirmwareUpdateMdSupportData newData)
            {
                bool isTargetListChanged = false;
                List<FirmwareTarget> targetsRemoved = FirmwareUpdateMdSupportData.FirmwareTargets.FindAll(x => newData.FirmwareTargets.Find(y => y.TargetNumber == x.TargetNumber) == null);
              //List<FirmwareTarget> targetsAdded   = newData.FirmwareTargets.FindAll(x => FirmwareUpdateMdSupportData.FirmwareTargets.Find(y => y.TargetNumber == x.TargetNumber) == null);
                if (targetsRemoved.Count > 0 /*|| targetAdded.Count > 0*/)
                {
                    isTargetListChanged = true;
                    targetsRemoved.ForEach(x => FirmwareUpdateMdSupportData.FirmwareTargets.Remove(x));
                }
                foreach (FirmwareTarget newFirmwareTarget in newData.FirmwareTargets)
                {
                    if (FirmwareUpdateMdSupportData.FirmwareTargets.Find(x => x.TargetNumber== newFirmwareTarget.TargetNumber) is FirmwareTarget curTarget)
                    {
                        if (curTarget.IsUpgradable != newFirmwareTarget.IsUpgradable
                         || !curTarget.FirmwareId.SequenceEqual(newFirmwareTarget.FirmwareId)
                         || !curTarget.FirmwareChecksum.SequenceEqual(newFirmwareTarget.FirmwareChecksum))
                        {
                            isTargetListChanged = true;
                            curTarget.IsUpgradable = newFirmwareTarget.IsUpgradable;
                            curTarget.FirmwareId = newFirmwareTarget.FirmwareId;
                            curTarget.FirmwareChecksum = newFirmwareTarget.FirmwareChecksum;
                        }
                    }
                    else
                    {
                        isTargetListChanged = true;
                        FirmwareUpdateMdSupportData.FirmwareTargets.Add(newFirmwareTarget);
                    }
                }
                bool isTarget0Changed = false;
                if (FirmwareUpdateMdSupportData.MaxFragmentSize != newData.MaxFragmentSize
                 || FirmwareUpdateMdSupportData.IgnoreChecksumErrors != newData.IgnoreChecksumErrors
                 || !FirmwareUpdateMdSupportData.Firmware0Id.SequenceEqual(newData.Firmware0Id)
                 || !FirmwareUpdateMdSupportData.Firmware0Checksum.SequenceEqual(newData.Firmware0Checksum)
                 || FirmwareUpdateMdSupportData.Firmware0IsUpgradable != newData.Firmware0IsUpgradable
                 || FirmwareUpdateMdSupportData.WaitTime!= newData.WaitTime 
                 || FirmwareUpdateMdSupportData.ReportsPerGet != newData.ReportsPerGet
                 || FirmwareUpdateMdSupportData.IsWorkingDuringUpload != newData.IsWorkingDuringUpload
                 || FirmwareUpdateMdSupportData.ManufacturerId != newData.ManufacturerId
                 || FirmwareUpdateMdSupportData.HardwareVersion != newData.HardwareVersion)
                {
                    isTarget0Changed = true;
                    FirmwareUpdateMdSupportData.MaxFragmentSize = newData.MaxFragmentSize;
                    FirmwareUpdateMdSupportData.IgnoreChecksumErrors = newData.IgnoreChecksumErrors;
                    FirmwareUpdateMdSupportData.Firmware0Id = newData.Firmware0Id;
                    FirmwareUpdateMdSupportData.Firmware0Checksum = newData.Firmware0Checksum;
                    FirmwareUpdateMdSupportData.Firmware0IsUpgradable = newData.Firmware0IsUpgradable;
                    FirmwareUpdateMdSupportData.WaitTime = newData.WaitTime;
                    FirmwareUpdateMdSupportData.ReportsPerGet = newData.ReportsPerGet;
                    FirmwareUpdateMdSupportData.IsWorkingDuringUpload = newData.IsWorkingDuringUpload;
                    FirmwareUpdateMdSupportData.ManufacturerId = newData.ManufacturerId;
                    FirmwareUpdateMdSupportData.HardwareVersion = newData.HardwareVersion;
                }

                if (isTargetListChanged || isTarget0Changed)
                {
                    if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        SendUnsolicitedReport(PrepareFirmwareMdReport(), NodeTag.Empty);
                    }
                }

                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class FirmwareDownloadState // Download means image transfer from an End Node to the Controller
    {
        internal bool IsDownloading { get; set; } = false;
        public bool IsAuthenticated { get; set; } = true;           // IsAuthenticated is always true
        public ushort RequestedFragmentSize { get; set; } = 40;
        internal byte RequestedTargetNumber { get; set; } = 0;
    }
    public class FirmwareUploadState // Upload means image transfer from the Controller to an End Node
    {
        internal bool IsUploading { get; set; } = false;
        internal bool MayDelayActivation { get; set; } = false;     // V4+, implementation will not delay activation
        internal bool MustDelayActivation { get; set; } = false;    // V7+
        public bool IsAuthenticated { get; set; } = true;           // IsAuthenticated is always true
        internal byte[] ProvidedImageChecksum { get; set; } = new byte[] { 0x00, 0x00 };
        internal byte RequestedTargetNumber { get; set; } = 0;
        internal ushort RequestedReportNumber { get; set; } = 0;
        internal ushort ReceivedSegmentsPerGet { get; set; } = 0;
        internal List<byte[]> UploadedSegments { get; set; } = new List<byte[]>();
        internal byte[] UploadedImage { get; set; } = new byte[] { };
        internal long UploadedLength { get; set; } = 0;
    }

    public class FirmwareTarget : INestedItem
    {
        //[CustomDataParameter(3, "TargetNumber", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Int_1_255, EditType.Config)]
        public byte TargetNumber { get; set; } = 0;
        [CustomDataParameter(3, "Is Upgradable", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config)]
        public bool IsUpgradable { get; set; } = false;
        [CustomDataParameter(3, "Firmware Id", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.HexByteArray, EditType.Config)]
        public byte[] FirmwareId { get; set; } = new byte[] { 0x00, 0x00 };
        [CustomDataParameter(3, "Checksum", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.HexByteArray, EditType.Config)]
        public byte[] FirmwareChecksum { get; set; } = new byte[] { 0x00, 0x00 };  // 0x0000 = No FW Checksum
        [JsonIgnore]
        public byte[] FirmwareImage { get; set; } = new byte[] { };

        // only used for EDE UI. see interface for usage notes.
        [JsonIgnore]
        public int NestedId
        {
            get => TargetNumber;
            set { }
        }

        public FirmwareTarget()
        {
        }
        public FirmwareTarget(byte targetNumber, bool isUpgradable, byte[] currentFirmwareId, byte[] currentChecksum)
        {
            TargetNumber = targetNumber;
            IsUpgradable = isUpgradable;
            FirmwareId = currentFirmwareId;
            FirmwareChecksum = currentChecksum;
        }
    }

    public class FirmwareUpdateMdSupportData : CommandClassSupportData, INodeItem
    {
        // Internals not accessible from TestCases or EDE
        [JsonIgnore]
        internal FirmwareUploadState UploadState { get; set; } = new FirmwareUploadState();
        [JsonIgnore]
        internal FirmwareDownloadState DownloadState { get; set; } = new FirmwareDownloadState();
        [JsonIgnore]
        internal byte TimeoutPerGet { get; set; } = 120;  // do not change, value from spec

        // Internals accessible from TestCases or EDE
        [CustomDataParameter(3, "TEST: Ignore CRC Errors", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config | EditType.Edit)]
        public bool IgnoreChecksumErrors { get; set; } = false;
        [JsonIgnore]
        public CancelPendingOperationDelegate CancelPendingLocalTimer = null;


        [ParameterSeparator(3, "Firmware 0")]
        // Md Report V3+
        [CustomDataParameter(3, "Firmware0 Id", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.HexByteArray, EditType.Config)]
        public byte[] Firmware0Id { get; set; } = new byte[] { 0x00, 0x00 };        // 0x0000 = No Firmware ID
        [CustomDataParameter(3, "Firmware0 Checksum", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.HexByteArray, EditType.Config)]
        public byte[] Firmware0Checksum { get; set; } = new byte[] { 0x00, 0x00 };  // 0x0000 = No Firmware Checksum
        [CustomDataParameter(3, "Firmware0 Is Upgradable", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config)]
        public bool Firmware0IsUpgradable { get; set; } = false;                    // 0x00 = Z-Wave Chip is not upgradable; 0xFF = is upgradable
        [JsonIgnore]
        public byte[] Firmware0Image { get; set; } = new byte[] { };


        [JsonIgnore]
        public IEnumerable<INestedItem> ChildItems => FirmwareTargets;
        [NestedControl(3, "FirmwareTargets")]
        public List<FirmwareTarget> FirmwareTargets { get; set; } = new List<FirmwareTarget>();


        [ParameterSeparator(3, "Transfer Features")]
        // Md Report V3+
        [CustomDataParameter(3, "Max Fragment Size", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.UShort, EditType.Config | EditType.Edit)]
        public ushort MaxFragmentSize { get; set; } = 40;
        // Md Status Report V3+
        [CustomDataParameter(3, "Wait Time after Upload", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Int_0_255, EditType.Config | EditType.Edit)]
        public byte WaitTime { get; set; } = 3;
        // Upload V3+
        [CustomDataParameter(3, "Reports per Get Command", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.UShort, EditType.Config | EditType.Edit)]
        public byte ReportsPerGet { get; set; } = 1;    // values > 1 are also supported
        // Md Report V6+
        [CustomDataParameter(6, "Works During Upload", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config | EditType.Edit)]
        public bool IsWorkingDuringUpload { get; set; } = false;    // Spec Field name: CC
        // Md Report V7+
        [CustomDataParameter(7, "Supports Subsequent Activation", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config | EditType.Edit)]
        public bool IsSupportingSubsequentActivation { get; set; } = false;

        // Manufacturer Specific CC
        [ParameterSeparator(3, "Global Device Data")]
        [CustomDataParameter(3, "Manufacturer Id", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.HexByteArray, EditType.Config | EditType.Edit)]
        public byte[] ManufacturerId { get; set; } = new byte[] { 0xFF, 0xFF }; // 0xFFFF = MFG_ID_NOT_DEFINED

        // Version CC
        [CustomDataParameter(3, "Hardware Version", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Int_0_255, EditType.Config | EditType.Edit)]
        public byte HardwareVersion { get; set; } = 0x00;
    }

    public enum FirmwareMdRequestStatusNames : byte
    {
        [Description("0x00 Invalid IDs")]
        InvalidIds = 0x00,              // V1+
        [Description("0x01 No authentication")]
        NoAuth = 0x01,                  // V1+
        [Description("0x02 Invalid fragment size")]
        InvalidFragmentSize = 0x02,     // V3+
        [Description("0x03 Is not upgradable")]
        IsNotUpgradable = 0x03,         // V3+
        [Description("0x04 Invalid hardware version")]
        InvalidHardwareVersion = 0x04,  // V5+
        [Description("0x05 Busy")]
        Busy = 0x05,                    // V6+
        [Description("0x06 Battery low")]
        BatteryLow = 0x06,              // V6+
        [Description("0xFF Initiating transfer")]
        InitiatingTransfer = 0xFF,      // V1+
    }
    public enum FirmwareMdStatusReportNames : byte
    {
        [Description("0x00 Checksum error")]
        ChecksumError = 0x00,           // V1+
        [Description("0x01 Sequence error")]
        SequenceError = 0x01,           // V1+
        [Description("0x02 Invalid manufacturer ID")]
        InvalidManufacturerId = 0x02,   // V4+
        [Description("0x03 Invalid firmware ID")]
        InvalidFirmwareId = 0x03,       // V4+
        [Description("0x04 Invalid target")]
        InvalidTarget = 0x04,           // V4+
        [Description("0x05 Invalid file header info")]
        InvalidFileHeaderInfo = 0x05,   // V4+
        [Description("0x06 Invalid file header format")]
        InvalidFileHeaderFormat = 0x06, // V4+
        [Description("0x07 Insufficient memory")]
        InsufficientMemory = 0x07,      // V4+
        [Description("0x08 Invalid hardware version")]
        InvalidHardwareVersion = 0x08,  // V5+
        [Description("0x09 Invalid signature")]
        InvalidSignature = 0x09,          // V8+
        [Description("0x0A Decryption failed")]
        DecryptionFailed = 0x0A,          // V8+
        [Description("0x0B Decompression failed")]
        DecompressionFailed = 0x0B,       // V8+
        [Description("0x0C Downgrade unsupported")]
        DowngradeUnsupported = 0x0C,      // V8+
        [Description("0xFD Waiting for activation")]
        WaitingForActivation = 0xFD,    // V4+
        [Description("0xFE Waiting for restart")]
        WaitingForRestart = 0xFE,       // V3+
        [Description("0xFF Success")]
        Success = 0xFF,                 // V1+
    }
    public enum FirmwareMdActivationStatusNames : byte
    {
        [Description("0x00 Version mismatch")]
        VersionMismatch = 0x00,         // V4+
        [Description("0x01 Activation error")]
        ActivationError = 0x01,         // V4+
        [Description("0xFF success")]
        Success = 0xFF,                 // V4+
    }
    public enum FirmwareMdPrepareStatusNames : byte
    {
        [Description("0x00 Invalid IDs")]
        InvalidIds = 0x00,              // V5+
        [Description("0x01 No authentication")]
        NoAuth = 0x01,                  // V5+
        [Description("0x02 Invalid fragment size")]
        InvalidFragmentSize = 0x02,     // V5+
        [Description("0x03 Is not downloadable")]
        IsNotDownloadable = 0x03,       // V5+
        [Description("0x04 Invalid hardware version")]
        InvalidHardwareVersion = 0x04,  // V5+
        [Description("0xFF Initiating transfer")]
        InitiatingTransfer = 0xFF,      // V1+
    }
}