// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services
{
    public class Tlv
    {
        public string Type { get; set; }
        public int Length { get; set; }
    }

    public class Tlv0 : Tlv
    {
        public string Tlv0ValueDeviceType { get; set; }
        public string Tlv0ValueInstallerIconType { get; set; }
    }

    public class Tlv2 : Tlv
    {
        public string Tlv2ValueManufacturerId { get; set; }
        public string Tlv2ValueProductType { get; set; }
        public string Tlv2ValueProductId { get; set; }
        public string Tlv2ValueApplicationVersion { get; set; }
    }

    public class Tlv4 : Tlv
    {
        public string Tlv4Value { get; set; }
    }

    public class Tlv6 : Tlv
    {
        public string Tlv6UUIDPressFormat { get; set; }
        public string Tlv6UUIDData { get; set; }
    }
}
