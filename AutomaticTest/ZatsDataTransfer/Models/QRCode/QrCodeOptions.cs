// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;

namespace Zats.Services
{
    public class QrCodeOptions
    {
        public QrCodeOptions()
        {
            QrHeader = new QrHeader();
            TypesCollection = new List<Tlv>();
        }
        public QrHeader QrHeader { get; set; }
        public List<Tlv> TypesCollection { get; set; }
        public List<StringFamilyTree> ToTreeView(bool IsValidChecksum)
        {
            List<StringFamilyTree> ret = new List<StringFamilyTree>
            {
                new StringFamilyTree { Name = "LeadIn", Value = QrHeader.LeadIn, IsValid = true },
                new StringFamilyTree { Name = "Version", Value = QrHeader.Version, IsValid = true },
                new StringFamilyTree { Name = "Checksum", Value = QrHeader.Checksum, IsValid = IsValidChecksum },
                new StringFamilyTree { Name = "RequestedKeys", Value = QrHeader.RequestedKeys, IsValid = true },
                new StringFamilyTree { Name = "DSK", Value = QrHeader.DSK, IsValid = true }
            };
            foreach (var tlv in TypesCollection)
            {
                List<StringFamilyTree> tlvChildren = new List<StringFamilyTree>
                {
                    new StringFamilyTree { Name = "Type", Value = tlv.Type, IsValid = true },
                    new StringFamilyTree { Name = "Length", Value = tlv.Length.ToString(), IsValid = true }
                };
                if (tlv is Tlv0 tlv0)
                {
                    tlvChildren.Add(new StringFamilyTree { Name = "DeviceType", Value = tlv0.Tlv0ValueDeviceType, IsValid = true });
                    tlvChildren.Add(new StringFamilyTree { Name = "InstallerIconType", Value = tlv0.Tlv0ValueInstallerIconType, IsValid = true });
                }
                else if (tlv is Tlv2 tlv2)
                {
                    tlvChildren.Add(new StringFamilyTree { Name = "ManufacturerId", Value = tlv2.Tlv2ValueManufacturerId, IsValid = true });
                    tlvChildren.Add(new StringFamilyTree { Name = "ProductType", Value = tlv2.Tlv2ValueProductType, IsValid = true });
                    tlvChildren.Add(new StringFamilyTree { Name = "ProductId", Value = tlv2.Tlv2ValueProductId, IsValid = true });
                    tlvChildren.Add(new StringFamilyTree { Name = "ApplicationVersion", Value = tlv2.Tlv2ValueApplicationVersion, IsValid = true });
                }
                else if (tlv is Tlv4 tlv4)
                {
                    tlvChildren.Add(new StringFamilyTree { Name = "Value ", Value = tlv4.Tlv4Value, IsValid = true });
                }
                else if (tlv is Tlv6 tlv6)
                {
                    tlvChildren.Add(new StringFamilyTree { Name = "UUIDPressFormat ", Value = tlv6.Tlv6UUIDPressFormat, IsValid = true });
                    tlvChildren.Add(new StringFamilyTree { Name = "UUIDData ", Value = tlv6.Tlv6UUIDData, IsValid = true });
                }
                ret.Add(new StringFamilyTree { Name = "TLV-" + tlv.Type.ToString(), Children = tlvChildren.ToArray(), IsValid = true });
            }

            return ret;
        }
    }
}
