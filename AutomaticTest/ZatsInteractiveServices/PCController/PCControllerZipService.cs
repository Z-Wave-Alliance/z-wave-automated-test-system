// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Net;
using Utils.Threading;
using Utils.UI.Enums;
using ZWave.Enums;

namespace Zats.Services.PCController
{
    public class PCControllerZipService : PCControllerService, IZipService
    {
        public PCControllerZipService(PCControllerDeviceManager pccDeviceManager, DeviceManager deviceManager) : base(pccDeviceManager, deviceManager)
        {
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            throw new NotImplementedException();
        }

        public ZipResult.AddNode AddNode(Alias zipControllerNumber, Alias deviceNumber, Modes mode, SecuritySchemes[] GrantSchemes, int timeoutMs)
        {
            ZipResult.AddNode ret = null;
            var initialLifelineSettings = DeviceManager.IncludeSettings.SetupNodeLifelineSettings;
            if (initialLifelineSettings == SetupNodeLifelineSettings.DisableAutoSisOnly)
            {
                DeviceManager.IncludeSettings.SetupNodeLifelineSettings = SetupNodeLifelineSettings.Default;
            }

            var basicRes = base.AddNode(zipControllerNumber, deviceNumber, mode, LearnModes.LearnModeClassic, timeoutMs);

            ret = new PCControllerActionResult.AddNodeZip()
            {
                NodeId = basicRes.NodeId,
                Status = basicRes ? (byte)1 : (byte)0
            };
            ret.Init(basicRes, new ConcurrentList<ZWave.TraceLogItem>());

            if (initialLifelineSettings != SetupNodeLifelineSettings.DisableAutoSisOnly)
            {
                DeviceManager.IncludeSettings.SetupNodeLifelineSettings = initialLifelineSettings;
            }

            return ret;
        }

        public new ZipResult.AddNode AddNode(Alias zipControllerNumber, Alias deviceNumber, Modes mode, int timeoutMs)
        {
            return AddNode(zipControllerNumber, deviceNumber, Modes.NodeAny, SecuritySchemeSet.ALL, INCLUSION_TIMEOUT_MS);
        }

        public new ZipResult.AddNode AddNode(Alias zipControllerNumber, Alias deviceNumber)
        {
            return AddNode(zipControllerNumber, deviceNumber, Modes.NodeAny, INCLUSION_TIMEOUT_MS);
        }

        public ZipResult.AddNode AddNodeWithCsaDSK(Alias zipControllerNumber, Alias secondaryControllerNumber, Modes mode, byte[] zipDsk, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public new ZipResult.AddNode AddNodeWithCsaDSK(Alias zipControllerNumber, Alias secondaryControllerNumber, int[] zipDsk)
        {
            throw new NotImplementedException();
        }

        public ZipResult.AddNode AddNodeWithCsaDSK(Alias zipControllerNumber, Alias secondaryControllerNumber, Modes mode, int[] zipDsk, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public new ZipResult.AddNode AddNodeWithCsaDSK(Alias zipControllerNumber, Alias secondaryControllerNumber, byte[] zipDsk)
        {
            throw new NotImplementedException();
        }

        public ZipResult.AddNode IncludeNode(Alias zipControllerNumber, Alias zipControllerUnsolicitedNumber, Alias deviceNumber, Modes mode, SecuritySchemes[] GrantSchemes, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public ZipResult.AddNode IncludeNode(Alias zipControllerNumber, Alias zipControllerUnsolicitedNumber, Alias deviceNumber, Modes mode, int timeoutMs)
        {
            return IncludeNode(zipControllerNumber, zipControllerUnsolicitedNumber, deviceNumber, mode, null, timeoutMs);
        }

        public ZipResult.AddNode IncludeNode(Alias zipControllerNumber, Alias zipControllerUnsolicitedNumber, Alias deviceNumber, int timeoutMs)
        {
            return IncludeNode(zipControllerNumber, zipControllerUnsolicitedNumber, deviceNumber, Modes.NodeAnyS2, timeoutMs);
        }

        public ZipResult.AddNode IncludeNode(Alias zipControllerNumber, Alias zipControllerUnsolicitedNumber, Alias deviceNumber)
        {
            return IncludeNode(zipControllerNumber, zipControllerUnsolicitedNumber, deviceNumber, INCLUSION_TIMEOUT_MS);
        }

        public ZipResult.AddNode AddNodeWithCsaDSK(Alias zipControllerNumber, Alias secondaryControllerNumber, Modes mode, byte[] zipDsk, SecuritySchemes[] GrantSchemes, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public ZipResult.AddNode AddNodeWithDSK(Alias zipControllerNumber, Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public ZipResult.AddNode AddNodeWithDSK(Alias zipControllerNumber, Alias deviceNumber, Modes mode, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public ZipResult.AddNode AddNodeWithDSK(Alias zipControllerNumber, Alias deviceNumber, Modes mode, byte[] dsk, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public ZipResult.AddNode AddNodeWithDSK(Alias zipControllerNumber, Alias deviceNumber, Modes mode, byte[] dsk, SecuritySchemes[] GrantSchemes, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public new ZipResult.AddNode AddNodeWithDSK(Alias zipControllerNumber, Alias deviceNumber, byte[] dsk)
        {
            throw new NotImplementedException();
        }

        public new ZipResult.AddNode AddNodeWithDSK(Alias zipControllerNumber, Alias deviceNumber, int dsk)
        {
            throw new NotImplementedException();
        }

        public ZipResult.AddNode AddNodeWithDSK(Alias zipControllerNumber, Alias deviceNumber, Modes mode, int dsk, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public void AddSecondaryUnsolicitedListener(Alias zipControllerNumber, int port)
        {
            throw new NotImplementedException();
        }

        public void CloseSecondaryUnsolicitedListener()
        {
            throw new NotImplementedException();
        }

        public string GetConnectionArgs(Alias zipControllerNumber)
        {
            throw new NotImplementedException();
        }

        public ZipResult.GetId GetId(Alias zipDeviceNumber)
        {
            throw new NotImplementedException();
        }

        public ZipResult.GetId GetId(Alias zipDeviceNumber, Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public ZipResult.GetIpAddress GetIpAddress(Alias zipControllerNumber, Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public ZipResult.GetIpAddress GetIpAddress(Alias zipControllerNumber, Node node)
        {
            throw new NotImplementedException();
        }

        public ZipResult.GetNodeList GetNodeList(Alias zipControllerNumber)
        {
            throw new NotImplementedException();
        }

        public string GetPskValue(Alias zipControllerNumber)
        {
            throw new NotImplementedException();
        }

        public ZipResult.LearnMode LearnMode(Alias zipControllerNumber, LearnModes mode, int timeoutMs)
        {
            ZipResult.LearnMode ret = null;

            var basicRes = base.SetLearnMode(zipControllerNumber, mode, timeoutMs);
            ret = new PCControllerActionResult.LearnModeZip();
            ret.Init(basicRes, new ConcurrentList<ZWave.TraceLogItem>());
            return ret;
        }

        public ZipResult.LearnMode LearnMode(Alias zipControllerNumber)
        {
            return LearnMode(zipControllerNumber, LearnModes.LearnModeClassic, INCLUSION_TIMEOUT_MS);
        }

        public ZipResult.Ping Ping(System.Net.IPAddress ipAddress)
        {
            throw new NotImplementedException();
        }

        public ZipResult.Ping Ping(System.Net.IPAddress ipAddress, int count, int timeoutMs, int intervalMs)
        {
            throw new NotImplementedException();
        }

        public ZipResult.Ping Ping(Alias zipDeviceNumber)
        {
            throw new NotImplementedException();
        }

        public byte[] PutIPv4inIPv6Format(byte[] ipV4addr)
        {
            throw new NotImplementedException();
        }

        public new void Reconnect(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public void RegisterUnsolicitedZipDevice(Alias deviceNumber, System.Net.IPEndPoint unsolEndPoint)
        {
            throw new NotImplementedException();
        }

        public void RegisterZipDevice(Alias zipControllerNumber, Alias deviceNumber, Node node)
        {
            throw new NotImplementedException();
        }

        public void RegisterZipDevice(Alias zipControllerNumber, Alias deviceNumber, IPAddress deviceIpAddress)
        {
            throw new NotImplementedException();
        }

        public new ZipResult.RemoveNode RemoveNode(Alias zipControllerNumber, Alias deviceNumber)
        {
            return RemoveNode(zipControllerNumber, deviceNumber, Modes.NodeAny, INCLUSION_TIMEOUT_MS);

        }

        public ZipResult.RemoveNode RemoveNode(Alias zipControllerNumber, Alias deviceNumber, Modes mode, int timeoutMs)
        {
            var learnMode = LearnModes.LearnModeClassic;
            ZipResult.RemoveNode ret = null;

            var basicRes = base.RemoveNode(zipControllerNumber, deviceNumber, mode, learnMode, timeoutMs);
            ret = new PCControllerActionResult.RemoveNodeZip();
            ret.Init(basicRes, new ConcurrentList<ZWave.TraceLogItem>());
            return ret;

        }

        public ZipResult.RequestData RequestData(Alias zipDeviceNumber, byte[] data, byte[] expectedData, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public ZipResult.RequestData RequestData(Alias zipDeviceNumber, byte[] headerExtension, byte[] data, byte[] expectedData, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public ZipResult.SendData SendData(Alias zipReceiverNumber, byte[] headerExtension, byte[] packet, bool isValidateAckSeqNo, int timeout)
        {
            throw new NotImplementedException();
        }

        public ZipResult.SendData SendData(Alias zipReceiverNumber, byte[] headerExtension, byte[] packet, bool isValidateAckSeqNo)
        {
            throw new NotImplementedException();
        }

        public ZipResult.SendData SendData(Alias zipReceiverNumber, byte[] packet, bool isValidateAckSeqNo)
        {
            throw new NotImplementedException();
        }

        public ZipResult.SendData SendData(Alias zipReceiverNumber, byte[] packet)
        {
            throw new NotImplementedException();
        }

        public ZipResult.SendData SendDataIPMulticast(Alias FirstNodeAlias, byte HeaderLengthByte,
            COMMAND_CLASS_ZIP_V5.COMMAND_ZIP_PACKET.PacketOption OptionsDataBytes, byte[] ZWaveCommandBytes)
        {
            throw new NotImplementedException();
        }

        public new ZipResult.SetDefault SetDefault(Alias zipDeviceNumber)
        {
            var basicRes = base.SetDefault(zipDeviceNumber);
            var ret = new PCControllerActionResult.SetDefaultZip();
            ret.Init(basicRes, new ConcurrentList<ZWave.TraceLogItem>());
            return ret;
        }

        public ZipResult.SetDefault SetDefault(Alias zipDeviceNumber, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public void SetTestInclusionValues(byte[] dsk, byte[] csa)
        {
            throw new NotImplementedException();
        }

        public void SetTestInclusionValues(byte[] dsk)
        {
            throw new NotImplementedException();
        }

        public void SetTestInclusionValues(byte[] dsk, byte[] csa, bool isKexReportConfirmed)
        {
            throw new NotImplementedException();
        }

        public new ServiceSettings Settings
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void UnregisterZipDevice(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public ZipResult.FirmwareBackup FirmwareBackupV5(Alias zipControllerNumber, int firmwareTarget, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public ZipResult.FirmwareBackup FirmwareBackupV5(Alias zipControllerNumber, byte[] headerExtension, int firmwareTarget, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public ZipResult.FirmwareUpdate FirmwareUpdateV5(Alias zipControllerNumber, byte[] data, int firmwareTarget, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public ZipResult.FirmwareUpdate FirmwareUpdateV5(Alias zipControllerNumber, byte[] headerExtension, byte[] data, int firmwareTarget, bool hasActivation, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public void StopCallbacksListeners(Alias zipControllerNumber)
        {
            throw new NotImplementedException();
        }

        public void TestEnableClientSideAuthS2(Alias device)
        {
            throw new NotImplementedException();
        }

        public bool IsPortaConnection(Alias zipControllerNumber)
        {
            throw new NotImplementedException();
        }

        public bool IsZipConnected(Alias zipControllerNumber)
        {
            throw new NotImplementedException();
        }

        public void ReconnectZipController(Alias zipController)
        {
            throw new NotImplementedException();
        }

        public void ReconnectUnsolicited(Alias unsolicitedNumber)
        {
            throw new NotImplementedException();
        }

        public void RegisterUnsolicitedZipDevice(Alias ZipController, Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public void RegisterUnsolicitedZipDevice(Alias ZipController, Alias UnsoldeviceNumber, Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public void Disconnect(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public void DeleteProvisioningList(Alias zipControllerNumber)
        {
            throw new NotImplementedException();
        }

        public void SetProvisioningList(Alias zipControllerNumber, List<byte[]> Dsk)
        {
            throw new NotImplementedException();
        }

        public COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V3.NODE_ADD_STATUS IncludeSmartStartDevice(Alias zipControllerNumber, Alias zipControllerUnsolicitedNumber, Alias endDevice, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public void SetProvisioningList(Alias zipControllerNumber, List<byte[]> Dsk, bool isLongRange)
        {
            throw new NotImplementedException();
        }

        public ZipResult.AddNode IncludeSmartStartDevice(Alias zipControllerNumber, Alias zipControllerUnsolicitedNumber, Alias endDevice, int timeoutMs, bool isLongRange)
        {
            throw new NotImplementedException();
        }

        ZipResult.RemoveNode IZipService.ExcludeNode(Alias zipControllerNumber, Alias deviceNumber, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        ZipResult.RemoveNode IZipService.ExcludeNode(Alias zipControllerNumber, Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public ZipResult.RemoveNode RemoveNode(Alias zipControllerNumber, Alias deviceNumber, Modes mode, ZWave.Enums.LearnModes learnMode, int timeoutMs)
        {
            throw new NotImplementedException();
        }
    }
}
