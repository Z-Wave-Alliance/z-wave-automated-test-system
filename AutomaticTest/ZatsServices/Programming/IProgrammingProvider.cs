// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using ZWave.Enums;

namespace Zats.Services
{
    public interface IProgrammingProvider
    {
        int ProgramSample(DeviceHostClient deviceHostClient, ZWaveRegions? region, string samplePath, bool isAddS2Keypair, byte[] prk, byte[] puk);
        int BackUpSample(DeviceHostClient deviceHostClient, string backUpPath);
        int ProgramBootLoader(DeviceHostClient deviceHostClient, BootloaderPath bootloaderPath);
        int ProgramSampleWithBootLoaderImageAndKeys(DeviceHostClient deviceHostClient, ZWaveRegions? region, string samplePath, BootloaderPath bootloaderPath);
        bool HasFirmware(DeviceHostClient deviceHostClient);
        string GetChipSeries(DeviceHostClient deviceHostClient);
        int SetHomeId(DeviceHostClient deviceHostClient, byte[] homeId);
        int ResetSample(DeviceHostClient deviceHostClient);
        int EraseSample(DeviceHostClient deviceHostClient);
        string GetPowerMeasure(DeviceHostClient deviceHostClient, int windowLenght);
        byte[] GetDsk(DeviceHostClient deviceHostClient);
        byte[] GetPrk(DeviceHostClient deviceHostClient);
        int Run(DeviceHostClient deviceClientHost, string args, int timeoutMs = 30000);
        int GblCreate(string hexFilePath, string gblFilePath, string signKeyFilePath, string encryptKeyFilePath, bool isSeries2 = false);
        string GetVersion();
        int RenewDsk(DeviceHostClient deviceHostClient);
        string GetSecurityStatusInfo(DeviceHostClient deviceHostClient);
        ZWaveRegions GetZWaveRegion(DeviceHostClient deviceHostClient);
    }
}
