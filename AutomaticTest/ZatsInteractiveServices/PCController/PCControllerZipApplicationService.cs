// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils.UI.Enums;

namespace Zats.Services.PCController
{
    public class PCControllerZipApplicationService : ServiceBase, IZipApplicationService
    {
        protected override void Log(string text, LogLevels logLevel)
        {
            throw new NotImplementedException();
        }

        public ZipResult.Test GetBasicTestResult(Alias virtualDeviceNumber)
        {
            throw new NotImplementedException();
        }

        public ZipResult.Test GetTrafficTestResult(Alias virtualDeviceNumber)
        {
            throw new NotImplementedException();
        }

        public void StartBasicTest(Alias virtualDeviceNumber)
        {
            throw new NotImplementedException();
        }

        public void StartBasicTest(Alias virtualDeviceNumber, int intervalMs)
        {
            throw new NotImplementedException();
        }

        public void StartTrafficTest(Alias virtualDeviceNumber, byte[] cmd, int intervalMs)
        {
            throw new NotImplementedException();
        }

        public void StartTrafficTest(Alias virtualDeviceNumber, byte[] cmd, byte[] expectedCmd, int intervalMs, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public ZipResult.Test StopBasicTest(Alias virtualDeviceNumber)
        {
            throw new NotImplementedException();
        }

        public ZipResult.Test StopBasicTestNoResult(Alias virtualDeviceNumber)
        {
            throw new NotImplementedException();
        }

        public ZipResult.Test StopTrafficTest(Alias virtualDeviceNumber)
        {
            throw new NotImplementedException();
        }

        public ZipResult.Test StopTrafficTestNoResult(Alias virtualDeviceNumber)
        {
            throw new NotImplementedException();
        }

        public void AddUnsolResponse(Alias UnsoldeviceNumber, byte[] headerExtension, byte[] cmd, byte[] expectedCmd)
        {
            throw new NotImplementedException();
        }

        public void AddUnsolResponse(Alias UnsoldeviceNumber, byte[] headerExtension, byte[] cmd, byte[] expectedCmd, int NumOfBytesToCompare)
        {
            throw new NotImplementedException();
        }

        public IProvisioningList ProvisioningList
        {
            get { throw new NotImplementedException(); }
        }

        public bool AutoConfigUnsolicitedDestination(Alias ZipDeviceNumber, Alias ZipUnsolDevice)
        {
            throw new NotImplementedException();
        }

        public ZipToken.Void AddUnsolResponse(Alias UnsoldeviceNumber, byte[] cmd, byte[] expectedCmd)
        {
            throw new NotImplementedException();
        }

        public ZipToken.Void AddUnsolResponse(Alias UnsoldeviceNumber, byte[] headerExtension, Func<byte[], byte[], byte[]> responseCallBack, byte[] expectedCmd)
        {
            throw new NotImplementedException();
        }

        public void UploadNvmToGwHost(string backUpPath, string startingVersion, string endingVersion, string zipHostAddress, string fileName, string jsonFileName)
        {
            throw new NotImplementedException();
        }
        public void UploadNvmToGwHostWithSmallBinary(string backUpPath, string startingVersion, string endingVersion, string zipHostAddress, string fileName, string jsonFileName)
        {
            throw new NotImplementedException();
        }

        public void ImportGW(string target_firmware, string protocol_name, string zipHostAddress, string jsonFileName)
        {
            throw new NotImplementedException();
        }

        public void GetFileFromGwHost(string fileName, string zipHostAddress, string downloadPath)
        {
            throw new NotImplementedException();
        }

        public void SetFileFromGwHost(string zipHostAddress, string downloadPath)
        {
            throw new NotImplementedException();
        }

        public ZipToken.Void AddUnsolResponse(Alias UnsoldeviceNumber, bool isNoAck, byte[] headerExtension, Func<byte[], byte[], byte[]> responseCallBack, byte[] expectedCmd)
        {
            throw new NotImplementedException();
        }

        public void EepromBackup(string zipHostAddress, string GW_Json_Path)
        {
            throw new NotImplementedException();
        }

        public void RestartPiGW(string zipHostAddress)
        {
            throw new NotImplementedException();
        }

        public ZipToken.Void[] AddUnsolInclusionSupport(Alias ZipUnsol, byte[] dsk)
        {
            throw new NotImplementedException();
        }

        public void StopGW(string zipHostAddress)
        {
            throw new NotImplementedException();
        }

        public void StartGW(string zipHostAddress)
        {
            throw new NotImplementedException();
        }

        public void RemoveGwEEPROM(string zipHostAddress)
        {
            throw new NotImplementedException();
        }

        public string GetDnsGW(string zipHostAddress)
        {
            throw new NotImplementedException();
        }

        public string GetDnsGW()
        {
            throw new NotImplementedException();
        }

        void IZipApplicationService.GetDnsGW(string savePath)
        {
            throw new NotImplementedException();
        }


        public void SendCommandToLibzwaveip(string Command, string zipHostAddress, string GwIpv6Address)
        {
            throw new NotImplementedException();
        }


        public void ProgramGWmodule(string zipHostAddress, string HexOrGblPath)
        {
            throw new NotImplementedException();
        }

        public void InstallGW(string Target_File_Name, string zipHostAddress, string Target_Path)
        {
            throw new NotImplementedException();
        }

        public void BackUpGw(string zipHostAddress)
        {
            throw new NotImplementedException();
        }

        public void RestoreGw(string zipHostAddress)
        {
            throw new NotImplementedException();
        }

        public void CheckGwHost(string zipHostAddress)
        {
            throw new NotImplementedException();
        }

        public void ProgramGWmodule700(string HostAddress_700, string HexPath, string CommanderPath)
        {
            throw new NotImplementedException();
        }

        public void RemoveFileFromGwHost(string zipHostAddress, string fileName)
        {
            throw new NotImplementedException();
        }

        public void UploadFileToGwHost(string filePath, string zipHostAddress)
        {
            throw new NotImplementedException();
        }
    }
}
