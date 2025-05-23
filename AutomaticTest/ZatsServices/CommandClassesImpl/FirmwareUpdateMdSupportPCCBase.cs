using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utils;
using ZWave;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;

namespace Zats.Services
{
    [CommandClassName("Firmware Update Command Class")]
    [CommandClassId(COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.ID)]
    [MinVersion(3)]
    [MaxVersion(5)]
    public abstract class FirmwareUpdateMdSupportPCCBase : CommandClassSupport
    {
        private const ushort START_REPORT_NUMBER = 0x01;
        private readonly byte[] FIRMWARE0_ID = { 0x05, 0x01 }; //Hardcoded devkit 6.80
        private readonly byte[] FIRMWARE1_ID = { 0x01, 0x03 };
        private readonly byte[] MANUFACTURER_ID = { 0x00, 0x00 }; //(Former Zensys) 0x0000 by default (SDS13425) or device.ManufacturerId,
        private readonly byte HARDWAREVERSION = 0x00; // SiLabs original: 0x02
        private readonly int MAX_FRAGMENT_SIZE = 40;
        public const byte RESULT_FAILED_STATUS = 0x00;
        public const byte RESULT_FAILED_KEYS_STATUS = 0x04;
        public const byte RESULT_SUCCUESSFULLY_COMPLETED_STATUS = 0xFF;

        private ushort _lastUsedReportNumber;
        private byte[] _myOtaFirmwareCrc;
        private byte _pStatus = 0x00;
        private NodeTag _curNodeId = NodeTag.Empty;
        private ZWave.Enums.SecuritySchemes _curSecuritySchemes;

        protected byte ReportStatusResult = 0x00;
        protected ActionBase _firmwareUpdateLoaded;
        protected readonly Action _setNewImageCompletedCallback;

        public FirmwareUpdateMdSupportPCCBase()
            : base(COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.ID)
        {
        }

        /// <summary>
        /// Over The Air support task.
        /// Firmware Update Meta Data version 5
        /// </summary>
        public FirmwareUpdateMdSupportPCCBase(Action SetNewImageCompletedCallback)
            : base(COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.ID)
        {
            MinCCVersion = 3;
            MaxCCVersion = 5;
            _network.DelayResponseMs = 2000;
            _setNewImageCompletedCallback = SetNewImageCompletedCallback;
        }

        protected override void CreateWorkflow()
        {
            base.CreateWorkflow();
            ActionUnits.Add(new ActionCompletedUnit(_firmwareUpdateLoaded, OnSetNewImageCompleted));
        }

        protected override void CreateInstance()
        {
            base.CreateInstance();
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

                if (isHandlingAllowed && command != null && command.Length > 1)
                {
                    switch (command[1])
                    {
                        case (COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_GET.ID):
                            {
                                if (IsSupervised())
                                {
                                    break;
                                }
                                ou.SetNextActionItems(SendFwuMdReport(sendSrcNode, scheme));
                            }
                            break;
                        case (COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_GET.ID):
                            {
                                if (IsSupervised())
                                {
                                    break;
                                }
                                ou.SetNextActionItems(HandleCmdClassFirmwareUpdateMdReqGet(sendSrcNode, command, scheme));
                                if (_pStatus == 0xFF)
                                {
                                    ou.AddNextActionItems(SendFwuMdGet(sendSrcNode, START_REPORT_NUMBER, scheme));
                                    FirmareUpdateStart(ou);
                                    _curNodeId = sendSrcNode;
                                    _curSecuritySchemes = scheme;
                                    ou.AddNextActionItems(SendFwuMdStatusReport(0x01));
                                }
                            }
                            break;
                        case (COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REPORT.ID):
                            {
                                HandleCmdClassFirmwareUpdateMdReport(ou, sendSrcNode, command, scheme);
                            }
                            break;
                        default:
                            break;
                    }
                    CommandCallback(command, sendDstNode.EndPointId);
                }
            }
        }

        /// <summary>handleCmdClassFirmwareUpdateMdReport</summary>
        private void HandleCmdClassFirmwareUpdateMdReport(DataReceivedUnit ou, NodeTag node, byte[] command, ZWave.Enums.SecuritySchemes scheme)
        {
            var fwuMdReport = (COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REPORT)command;
            var lastReportNumber = Tools.GetBytes(_lastUsedReportNumber);
            if (fwuMdReport.properties1.reportNumber1 == lastReportNumber[0] &&
                fwuMdReport.reportNumber2 == lastReportNumber[1] /*&& nodeId == _curNodeId*/)
            {
                var crc16Res = Tools.CalculateCrc16Array(command.Take(command.Length - 2));
                if (crc16Res.SequenceEqual(fwuMdReport.checksum))
                {
                    var data = fwuMdReport.data;
                    FirmwareUpdateAddPacket(ou, data);

                    if (fwuMdReport.properties1.last == 0x00)
                    {
                        ushort nextReportNumber = (ushort)(_lastUsedReportNumber + 1);
                        ou.AddNextActionItems(SendFwuMdGet(node, nextReportNumber, scheme));
                    }
                    else
                    {
                        FirmwareUpdateLoad(ou);
                    }
                }
                else //FW_EV_UPDATE_STATUS_UNABLE_TO_RECEIVE
                {
                    "invalid MD report"._DLOG();
                    //retransmit request on the previous frame
                    ou.SetNextActionItems(SendFwuMdGet(node, _lastUsedReportNumber, scheme));
                    ou.AddNextActionItems(SendFwuMdStatusReport(0x01));
                }
            }
            else
            {
                "invalid MD report"._DLOG();
                ou.SetNextActionItems(SendFwuMdGet(node, _lastUsedReportNumber, scheme));
                ou.AddNextActionItems(SendFwuMdStatusReport(0x01));
            }
        }

        /// <summary>handleCmdClassFirmwareUpdateMdReqGet</summary>
        private ActionBase HandleCmdClassFirmwareUpdateMdReqGet(NodeTag node, byte[] command, ZWave.Enums.SecuritySchemes scheme)
        {
            var fwuMdRequestGet = (COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_GET)command;
            _myOtaFirmwareCrc = fwuMdRequestGet.checksum;
            var fragmentSize = Tools.GetInt32(fwuMdRequestGet.fragmentSize);
            if (fwuMdRequestGet.firmwareId.SequenceEqual(FIRMWARE0_ID) &&
                fwuMdRequestGet.manufacturerId.SequenceEqual(MANUFACTURER_ID) &&
                fragmentSize <= MAX_FRAGMENT_SIZE
                )
            {
                _pStatus = 0xFF;
            }
            else
            {
                _pStatus = 0x00;
            }

            if (fwuMdRequestGet.firmwareTarget > 0x01)
            {
                if(Version > 0x03)
                {
                    _pStatus = 0x03;
                }
                else
                {
                    _pStatus = 0x00;
                }
            }

            setStatus(fwuMdRequestGet);

            return SendFwuMdRequestReport(node, _pStatus, scheme);
        }

        /// <summary>ZCB_CmdClassFwUpdateMdReqReport</summary>
        private ActionBase SendFwuMdRequestReport(NodeTag node, byte reportStatus, ZWave.Enums.SecuritySchemes scheme)
        {
            var fwuMdRequestReport = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_REPORT() { status = reportStatus };
            var ret = new SendDataExOperation(_network, node, fwuMdRequestReport, TxOptions, scheme);
            return ret;
        }

        /// <summary>ZCB_UpdateStatusSuccess</summary>
        protected SendDataExOperation SendFwuMdStatusReport(/*byte nodeId, byte reportStatus, SecuritySchemes scheme*/)
        {
            var fwuMdRequestReport = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_STATUS_REPORT() { status = ReportStatusResult };
            var ret = new SendDataExOperation(_network, _curNodeId, fwuMdRequestReport, TxOptions, _curSecuritySchemes);
            return ret;
        }

        protected SendDataExOperation SendFwuMdStatusReport(byte reportStatus)
        {
            var fwuMdRequestReport = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_STATUS_REPORT() { status = reportStatus };
            var ret = new SendDataExOperation(_network, _curNodeId, fwuMdRequestReport, TxOptions, _curSecuritySchemes);
            return ret;
        }

        /// <summary>CmdClassFirmwareUpdateMdGet</summary>
        private ActionBase SendFwuMdGet(NodeTag node, ushort fwuReportNumber, ZWave.Enums.SecuritySchemes scheme)
        {
            _lastUsedReportNumber = fwuReportNumber;
            var reportNumber = Tools.GetBytes(fwuReportNumber);
            var fwuMdGet = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_GET()
            {
                numberOfReports = 0x01,
                reportNumber2 = reportNumber[1],
                properties1 = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_GET.Tproperties1()
                {
                    reportNumber1 = reportNumber[0]
                }
            };
            var ret = new SendDataExOperation(_network, node, fwuMdGet, TxOptions, scheme);
            return ret;
        }

        /// <summary>handleCommandClassFWUpdate</summary>
        private ActionBase SendFwuMdReport(NodeTag node, ZWave.Enums.SecuritySchemes scheme)
        {
            var fwuMdReport = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_REPORT()
            {
                manufacturerId = MANUFACTURER_ID,
                firmware0Id = FIRMWARE0_ID,
                firmware0Checksum = new byte[] { 0x00, 0x00},
                firmwareUpgradable = 0xFF,
                numberOfFirmwareTargets = 0x01,
                maxFragmentSize = Tools.GetBytes(MAX_FRAGMENT_SIZE).Skip(2).ToArray(),
                hardwareVersion = HARDWAREVERSION,
                vg1 = new List<COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_REPORT.TVG1> { new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_REPORT.TVG1() { firmwareId = FIRMWARE1_ID } }
            };
            var ret = new SendDataExOperation(_network, node, fwuMdReport, TxOptions, scheme);
            return ret;
        }

        private void setStatus(COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_GET requestGetCmd)
        {
            var fragmentSize = Tools.GetInt32(requestGetCmd.fragmentSize);

            _pStatus = 0xFF;

            if (Version < 0x04)
            {
                if (!requestGetCmd.firmwareId.SequenceEqual(FIRMWARE0_ID) ||
                !requestGetCmd.manufacturerId.SequenceEqual(MANUFACTURER_ID) ||
                fragmentSize > MAX_FRAGMENT_SIZE
                )
                {
                    _pStatus = 0x00;
                }
            }
            else
            {
                if(!requestGetCmd.manufacturerId.SequenceEqual(MANUFACTURER_ID))
                {
                    _pStatus = 0x02;
                }

                if (!requestGetCmd.firmwareId.SequenceEqual(FIRMWARE0_ID))
                {
                    _pStatus = 0x03;
                }
                
                if (requestGetCmd.firmwareTarget > 0x01)
                {
                    _pStatus = 0x04;
                }

                if (fragmentSize > MAX_FRAGMENT_SIZE)
                {
                    _pStatus = 0x00;
                }

            }

            if (Version > 0x05)
            {
                if(requestGetCmd.hardwareVersion != HARDWAREVERSION)
                {
                    _pStatus = 0x08;
                }
            }
        }

        protected abstract void FirmareUpdateStart(DataReceivedUnit ou);
        protected abstract void FirmwareUpdateAddPacket(DataReceivedUnit ou, IList<byte> data);
        protected abstract void FirmwareUpdateLoad(DataReceivedUnit ou);
        protected abstract void OnSetNewImageCompleted(ActionCompletedUnit ou);

    }
}
