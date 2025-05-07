using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.Devices;

namespace Zats.Services
{
    [CommandClassName("Manufacturer Specific Command Class")]
    [CommandClassId(COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID)]
    [MinVersion(1)]
    [MaxVersion(2)]
    public class ManufacturerSpecificSupport : CommandClassSupport
    {
        private ManufacturerSpecificSupportData ManufacturerSpecificSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return ManufacturerSpecificSupportData; }
            set { ManufacturerSpecificSupportData = (ManufacturerSpecificSupportData)value; }
        }

        public ManufacturerSpecificSupport()
            : this(new ManufacturerSpecificSupportData() { })
        { }

        public ManufacturerSpecificSupport(ManufacturerSpecificSupportData manufacturerSpecificSupportData)
            : base(COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID)
        {
            ManufacturerSpecificSupportData = manufacturerSpecificSupportData;
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
                bool nonSecureManufacturerSpecificGet = false;

                ZWave.Enums.SecuritySchemes[] schemes = Network.GetSecuritySchemes();

                if (schemes != null &&
                    schemes.Contains(ZWave.Enums.SecuritySchemes.S0) &&
                    !schemes.Contains(ZWave.Enums.SecuritySchemes.S2_UNAUTHENTICATED) &&
                    !schemes.Contains(ZWave.Enums.SecuritySchemes.S2_AUTHENTICATED) &&
                    !schemes.Contains(ZWave.Enums.SecuritySchemes.S2_ACCESS))
                {
                    // backwards compatible rule for S0 Inclusions: Manufacturer Specific Get must be answered non-securely
                    nonSecureManufacturerSpecificGet = true;
                }

                if (isHandlingAllowed || nonSecureManufacturerSpecificGet && command != null && command.Length > 1 && ManufacturerSpecificSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_MANUFACTURER_SPECIFIC.MANUFACTURER_SPECIFIC_GET.ID)
                    {
                        rpt = PrepareManufacturerSpecificReport();
                    }
                    else if (command[1] == COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.DEVICE_SPECIFIC_GET.ID)
                    {
                        if (!isHandlingAllowed)
                        {
                            return;
                        }
                        rpt = PrepareDeviceSpecificReport(command);
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
        private byte[] PrepareDeviceSpecificReport(byte deviceIdType)
        {
            if (Version >= 2)
            {
                if (deviceIdType == 0)
                {
                    deviceIdType = (byte)ManufacturerSpecificSupportData.DefaultDeviceIdType;
                }

                COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.DEVICE_SPECIFIC_REPORT rpt = new COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.DEVICE_SPECIFIC_REPORT();
                switch (deviceIdType)
                {
                    case 0x01:
                        rpt.properties1.deviceIdType = 0x01;
                        rpt.properties2.deviceIdDataFormat = (byte)ManufacturerSpecificSupportData.DeviceIdDataFormat1;
                        rpt.properties2.deviceIdDataLengthIndicator = (byte)ManufacturerSpecificSupportData.SerialNumber.Length;
                        rpt.deviceIdData = ManufacturerSpecificSupportData.SerialNumber;
                        break;
                    case 0x02:
                        rpt.properties1.deviceIdType = 0x02;
                        rpt.properties2.deviceIdDataFormat = (byte)ManufacturerSpecificSupportData.DeviceIdDataFormat2;
                        rpt.properties2.deviceIdDataLengthIndicator = (byte)ManufacturerSpecificSupportData.PseudoRandom.Length;
                        rpt.deviceIdData = ManufacturerSpecificSupportData.PseudoRandom;
                        break;
                }
                return rpt;
            }
            return null;
        }
        private byte[] PrepareDeviceSpecificReport(byte[] command)
        {
            if (Version >= 2)
            {
                return PrepareDeviceSpecificReport(((COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.DEVICE_SPECIFIC_GET)command).properties1.deviceIdType);
            }
            return null;
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet,
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            supervisionReport.status = (byte)SvStatusNames.NoSupport;
            return;
        }

        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is ManufacturerSpecificSupportData newData)
            {
                if (ManufacturerSpecificSupportData.ManufacturerId != newData.ManufacturerId
                 || ManufacturerSpecificSupportData.ProductTypeId != newData.ProductTypeId
                 || ManufacturerSpecificSupportData.ProductId != newData.ProductId)
                {
                    ManufacturerSpecificSupportData.ManufacturerId = newData.ManufacturerId;
                    ManufacturerSpecificSupportData.ProductTypeId = newData.ProductTypeId;
                    ManufacturerSpecificSupportData.ProductId = newData.ProductId;
                    if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        SendUnsolicitedReport(PrepareManufacturerSpecificReport(), NodeTag.Empty);
                    }
                }
                if (ManufacturerSpecificSupportData.DefaultDeviceIdType != newData.DefaultDeviceIdType)
                {
                    ManufacturerSpecificSupportData.DefaultDeviceIdType = newData.DefaultDeviceIdType;
                }
                if (ManufacturerSpecificSupportData.DeviceIdDataFormat1 != newData.DeviceIdDataFormat1
                 || !ManufacturerSpecificSupportData.SerialNumber.SequenceEqual(newData.SerialNumber) )
                {
                    ManufacturerSpecificSupportData.DeviceIdDataFormat1 = newData.DeviceIdDataFormat1;
                    ManufacturerSpecificSupportData.SerialNumber = newData.SerialNumber;
                    if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        SendUnsolicitedReport(PrepareDeviceSpecificReport((byte)DeviceIdTypeNames.SerialNumber), NodeTag.Empty);
                    }
                }
                if (ManufacturerSpecificSupportData.DeviceIdDataFormat2 != newData.DeviceIdDataFormat2
                 || !ManufacturerSpecificSupportData.PseudoRandom.SequenceEqual(newData.PseudoRandom))
                {
                    ManufacturerSpecificSupportData.DeviceIdDataFormat2 = newData.DeviceIdDataFormat2;
                    ManufacturerSpecificSupportData.PseudoRandom = newData.PseudoRandom;
                    if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        SendUnsolicitedReport(PrepareDeviceSpecificReport((byte)DeviceIdTypeNames.PseudoRandom), NodeTag.Empty);
                    }
                }
                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class ManufacturerSpecificSupportData : CommandClassSupportData
    {
        // Manufacturer Specific Report
        [ParameterSeparator(1, "Manufacturer Specific Report")]
        [XmlDataParameter(1, 0x72, 0x05, new byte[] { 0x00 }, EditType.Config)]
        public byte[] ManufacturerId { get; set; } = { 0xFF, 0xFF }; // MFG_ID_NOT_DEFINED

        [XmlDataParameter(1, 0x72, 0x05, new byte[] { 0x01 }, EditType.Config)]
        public byte[] ProductTypeId { get; set; } = { 0x00, 0xAA };

        [XmlDataParameter(1, 0x72, 0x05, new byte[] { 0x02 }, EditType.Config)]
        public byte[] ProductId { get; set; } = { 0x00, 0x01 };

        [ParameterSeparator(2, "Device Specific Report")]
        [CustomDataParameter(2, "Default Device ID Type", CommandClassesImpl.UiControl.ListComboBox, CommandClassesImpl.InputType.Enum, EditType.Config, typeof(DeviceIdTypeNames))]
        public DeviceIdTypeNames DefaultDeviceIdType { get; set; } = DeviceIdTypeNames.SerialNumber;

        [CustomDataParameter(2, "Serial Number Format", CommandClassesImpl.UiControl.ListComboBox, CommandClassesImpl.InputType.Enum, EditType.Config, typeof(DeviceIdDataFormatNames))]
        public DeviceIdDataFormatNames DeviceIdDataFormat1 { get; set; } = DeviceIdDataFormatNames.UTF8;

        [CustomDataParameter(2, "Serial Number Data", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.HexByteArray, EditType.Config)]
        public byte[] SerialNumber { get; set; } = { };

        [CustomDataParameter(2, "Pseudo Random Format", CommandClassesImpl.UiControl.ListComboBox, CommandClassesImpl.InputType.Enum, EditType.Config, typeof(DeviceIdDataFormatNames))]
        public DeviceIdDataFormatNames DeviceIdDataFormat2 { get; set; } = DeviceIdDataFormatNames.UTF8;

        [CustomDataParameter(2, "Pseudo Random Data", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.HexByteArray, EditType.Config)]
        public byte[] PseudoRandom { get; set; } = { };
    }

    public enum DeviceIdDataFormatNames : byte
    {
        [Description("0x00 UTF8")]
        UTF8 = 0x00,
        [Description("0x01 Binary")]
        Binary = 0x01
    }
    public enum DeviceIdTypeNames : byte
    {
        [Description("0x01 Serial Number")]
        SerialNumber = 0x01,
        [Description("0x02 Pseudo Random")]
        PseudoRandom = 0x02
    }
}
