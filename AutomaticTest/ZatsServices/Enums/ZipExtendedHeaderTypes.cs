// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
namespace Zats.Services
{
    public enum ZipExtendedHeaderTypes
    {
        ExpectedDelay = ZWave.Enums.ZipExtendedHeaderTypes.ExpectedDelay,
        InstallationAndMaintenanceGet = ZWave.Enums.ZipExtendedHeaderTypes.InstallationAndMaintenanceGet,
        InstallationAndMaintenanceReport = ZWave.Enums.ZipExtendedHeaderTypes.InstallationAndMaintenanceReport,
        EncapsulationFormatInformation = ZWave.Enums.ZipExtendedHeaderTypes.EncapsulationFormatInformation,
        ReceivedViaMulticast = ZWave.Enums.ZipExtendedHeaderTypes.ReceivedViaMulticast,
        ExtendedZipPacketHeaderLength = ZWave.Enums.ZipExtendedHeaderTypes.ExtendedZipPacketHeaderLength,
        MulticastDestinationAddresses = ZWave.Enums.ZipExtendedHeaderTypes.MulticastDestinationAddresses,
        MulticastAcknowledgement = ZWave.Enums.ZipExtendedHeaderTypes.MulticastAcknowledgement,
    }
}
