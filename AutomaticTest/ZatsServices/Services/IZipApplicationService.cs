// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zats.Services
{
    public interface IZipApplicationService
    {
        IProvisioningList ProvisioningList { get; }
        ZipResult.Test GetBasicTestResult(Alias virtualDeviceNumber);
        ZipResult.Test GetTrafficTestResult(Alias virtualDeviceNumber);
        void StartBasicTest(Alias virtualDeviceNumber);
        void StartBasicTest(Alias virtualDeviceNumber, int intervalMs);
        void StartTrafficTest(Alias virtualDeviceNumber, byte[] cmd, int intervalMs);
        void StartTrafficTest(Alias virtualDeviceNumber, byte[] cmd, byte[] expectedCmd, int intervalMs, int timeoutMs);
        ZipResult.Test StopBasicTest(Alias virtualDeviceNumber);
        ZipResult.Test StopBasicTestNoResult(Alias virtualDeviceNumber);
        ZipResult.Test StopTrafficTest(Alias virtualDeviceNumber);
        ZipResult.Test StopTrafficTestNoResult(Alias virtualDeviceNumber);
        void AddUnsolResponse(Alias UnsoldeviceNumber, byte[] headerExtension, byte[] cmd, byte[] expectedCmd);
        void AddUnsolResponse(Alias UnsoldeviceNumber, byte[] headerExtension, byte[] cmd, byte[] expectedCmd, int NumOfBytesToCompare);
        ZipToken.Void AddUnsolResponse(Alias UnsoldeviceNumber, byte[] headerExtension, Func<byte[], byte[], byte[]> responseCallBack,
            byte[] expectedCmd);
        ZipToken.Void AddUnsolResponse(Alias UnsoldeviceNumber, bool isNoAck, byte[] headerExtension, Func<byte[], byte[], byte[]> responseCallBack,
            byte[] expectedCmd);
        ZipToken.Void AddUnsolResponse(Alias UnsoldeviceNumber, byte[] cmd, byte[] expectedCmd);
        bool AutoConfigUnsolicitedDestination(Alias ZipDeviceNumber, Alias ZipUnsolDevice);
        void UploadNvmToGwHost(string backUpPath, string startingVersion, string endingVersion, string zipHostAddress, string fileName, string jsonFileName);
        void UploadNvmToGwHostWithSmallBinary(string backUpPath, string startingVersion, string endingVersion, string zipHostAddress, string fileName, string jsonFileName);
        void ImportGW(string target_firmware, string protocol_name, string zipHostAddress, string jsonFileName);
        void GetFileFromGwHost(string fileName, string zipHostAddress, string downloadPath);
        void SetFileFromGwHost(string zipHostAddress, string downloadPath);
        void EepromBackup(string zipHostAddress, string GW_Json_Path);
        void UploadFileToGwHost(string filePath, string zipHostAddress);
        void RestartPiGW(string zipHostAddress);
        ZipToken.Void[] AddUnsolInclusionSupport(Alias ZipUnsol, byte[] dsk);
        void StopGW(string zipHostAddress);
        void StartGW(string zipHostAddress);
        void RemoveGwEEPROM(string zipHostAddress);
        void GetDnsGW(string savePath);
        void ProgramGWmodule(string zipHostAddress, string HexOrGblPath);
        void InstallGW(string Target_File_Name, string zipHostAddress, string Target_Path);
        void BackUpGw(string zipHostAddress);
        void RestoreGw(string zipHostAddress);
        void CheckGwHost(string zipHostAddress);
        void ProgramGWmodule700(string HostAddress_700, string HexPath, string CommanderPath);
        void RemoveFileFromGwHost(string zipHostAddress, string fileName);        

    }
}
