// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.Devices;

namespace Zats.Services
{
    public class ManufacturerSpecificSupport : CommandClassSupport
    {
        private ManufacturerSpecificSupportData ManufacturerSpecificSupportData { get; set; }

        //private readonly static string CommandClassName = "Manufacturer Specific CC";
        //private string EndPointName = "";
        //private readonly int LogLevel;  // shortcut for SupportData.LogLevel

        public ManufacturerSpecificSupport()
            : this(new ManufacturerSpecificSupportData() { })
        { }

        public ManufacturerSpecificSupport(ManufacturerSpecificSupportData manufacturerSpecificSupportData)
            : base(COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 2;
            ManufacturerSpecificSupportData = manufacturerSpecificSupportData;
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

                if (isResponseAllowed && command != null && command.Length > 1 && ManufacturerSpecificSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_MANUFACTURER_SPECIFIC.MANUFACTURER_SPECIFIC_GET.ID)
                    {
                        rpt = PrepareManufacturerSpecificReport();
                    }
                    else if (command[1] == COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.DEVICE_SPECIFIC_GET.ID)
                    {
                        rpt = PrepareDeviceSpecificReport(command);
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

        private byte[] PrepareManufacturerSpecificReport()
        {
            COMMAND_CLASS_MANUFACTURER_SPECIFIC.MANUFACTURER_SPECIFIC_REPORT rpt = new COMMAND_CLASS_MANUFACTURER_SPECIFIC.MANUFACTURER_SPECIFIC_REPORT()
            {
                manufacturerId = ManufacturerSpecificSupportData.ManufacturerId,
                productTypeId = ManufacturerSpecificSupportData.ProductTypeId,
                productId = ManufacturerSpecificSupportData.ProductId
            };
            return rpt;
        }
        private byte[] PrepareDeviceSpecificReport(byte[] command)
        {
            if (Version >= 2)
            {
                COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.DEVICE_SPECIFIC_REPORT rpt = new COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.DEVICE_SPECIFIC_REPORT();
                switch (((COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.DEVICE_SPECIFIC_GET)command).properties1.deviceIdType)
                {
                    case 0x00:
                    case 0x01:
                    default:
                        rpt.properties1.deviceIdType = 0x01;
                        rpt.properties2.deviceIdDataFormat = ManufacturerSpecificSupportData.DeviceIdDataFormat1;
                        rpt.properties2.deviceIdDataLengthIndicator = (byte)ManufacturerSpecificSupportData.SerialNumber.Length;
                        rpt.deviceIdData = ManufacturerSpecificSupportData.SerialNumber;
                        break;
                    case 0x02:
                        rpt.properties1.deviceIdType = 0x02;
                        rpt.properties2.deviceIdDataFormat = ManufacturerSpecificSupportData.DeviceIdDataFormat2;
                        rpt.properties2.deviceIdDataLengthIndicator = (byte)ManufacturerSpecificSupportData.PseudoRandom.Length;
                        rpt.deviceIdData = ManufacturerSpecificSupportData.PseudoRandom;
                        break;
                }
                return rpt;
            }
            return null;
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, 
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            supervisionReport.status = (byte) SvStatusNames.NoSupport;
            return;
        }
    }

    public class ManufacturerSpecificSupportData
    {
        // Manufacturer Specific
        public byte[] ManufacturerId { get; set; } = { 0xFF, 0xFF }; // MFG_ID_NOT_DEFINED
        public byte[] ProductTypeId { get; set; } = { 0x00, 0xAA };
        public byte[] ProductId { get; set; } = { 0x00, 0x01 };
        // Device Specific
        public byte DeviceIdDataFormat0 { get; set; } = 0x00;   // 0x00 = UTF-8; 0x01 = Plain binary for DeviceIdType 0x00
        public byte DeviceIdDataFormat1 { get; set; } = 0x00;   // 0x00 = UTF-8; 0x01 = Plain binary for DeviceIdType 0x01
        public byte DeviceIdDataFormat2 { get; set; } = 0x00;   // 0x00 = UTF-8; 0x01 = Plain binary for DeviceIdType 0x02
        public byte[] DeviceIdType { get; set; } = { 0x44, 0x45, 0x56, 0x49, 0x43, 0x44, 0x31 };    // Get 0x00 = Factory default Device ID Type
        public byte[] SerialNumber { get; set; } = { 0x31, 0x31, 0x31 };        // Get 0x01 = Serial number
        public byte[] PseudoRandom { get; set; } = { 0x39, 0x38, 0x37, 0x36 };  // Get 0x02 = Pseudo Random
    }
}
