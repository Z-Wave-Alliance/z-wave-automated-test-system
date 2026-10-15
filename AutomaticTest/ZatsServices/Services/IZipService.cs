// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Zats.Services
{
    public interface IZipService
    {
        ZipResult.AddNode AddNode(Alias zipControllerNumber, Alias deviceNumber, Modes mode, SecuritySchemes[] GrantSchemes, int timeoutMs);
        ZipResult.AddNode AddNode(Alias zipControllerNumber, Alias deviceNumber, Modes mode, int timeoutMs);
        ZipResult.AddNode AddNode(Alias zipControllerNumber, Alias deviceNumber);
        ZipResult.AddNode AddNodeWithCsaDSK(Alias zipControllerNumber, Alias secondaryControllerNumber, Modes mode, byte[] zipDsk, int timeoutMs);
        ZipResult.AddNode AddNodeWithCsaDSK(Alias zipControllerNumber, Alias secondaryControllerNumber, int[] zipDsk);
        ZipResult.AddNode AddNodeWithCsaDSK(Alias zipControllerNumber, Alias secondaryControllerNumber, Modes mode, int[] zipDsk, int timeoutMs);
        ZipResult.AddNode AddNodeWithCsaDSK(Alias zipControllerNumber, Alias secondaryControllerNumber, byte[] zipDsk);
        ZipResult.AddNode AddNodeWithCsaDSK(Alias zipControllerNumber, Alias secondaryControllerNumber, Modes mode, byte[] zipDsk, SecuritySchemes[] GrantSchemes, int timeoutMs);
        ZipResult.AddNode AddNodeWithDSK(Alias zipControllerNumber, Alias deviceNumber);
        ZipResult.AddNode AddNodeWithDSK(Alias zipControllerNumber, Alias deviceNumber, Modes mode, int timeoutMs);
        ZipResult.AddNode AddNodeWithDSK(Alias zipControllerNumber, Alias deviceNumber, Modes mode, byte[] dsk, int timeoutMs);
        ZipResult.AddNode AddNodeWithDSK(Alias zipControllerNumber, Alias deviceNumber, byte[] dsk);
        ZipResult.AddNode AddNodeWithDSK(Alias zipControllerNumber, Alias deviceNumber, int dsk);
        ZipResult.AddNode AddNodeWithDSK(Alias zipControllerNumber, Alias deviceNumber, Modes mode, int dsk, int timeoutMs);
        ZipResult.AddNode AddNodeWithDSK(Alias zipControllerNumber, Alias deviceNumber, Modes mode, byte[] dsk, SecuritySchemes[] GrantSchemes, int timeoutMs);
        ZipResult.AddNode IncludeNode(Alias zipControllerNumber, Alias zipControllerUnsolicitedNumber, Alias deviceNumber, Modes mode, SecuritySchemes[] GrantSchemes, int timeoutMs);
        ZipResult.AddNode IncludeNode(Alias zipControllerNumber, Alias zipControllerUnsolicitedNumber, Alias deviceNumber, Modes mode, int timeoutMs);
        ZipResult.AddNode IncludeNode(Alias zipControllerNumber, Alias zipControllerUnsolicitedNumber, Alias deviceNumber);
        ZipResult.RemoveNode ExcludeNode(Alias zipControllerNumber, Alias deviceNumber, int timeoutMs);
        ZipResult.RemoveNode ExcludeNode(Alias zipControllerNumber, Alias deviceNumber);
        void AddSecondaryUnsolicitedListener(Alias zipControllerNumber, int port);
        void CloseSecondaryUnsolicitedListener();
        string GetConnectionArgs(Alias zipControllerNumber);
        ZipResult.GetId GetId(Alias zipDeviceNumber);
        ZipResult.GetId GetId(Alias zipDeviceNumber, Alias deviceNumber);
        ZipResult.GetIpAddress GetIpAddress(Alias zipControllerNumber, Alias deviceNumber);
        ZipResult.GetIpAddress GetIpAddress(Alias zipControllerNumber, Node node);
        ZipResult.GetNodeList GetNodeList(Alias zipControllerNumber);
        string GetPskValue(Alias zipControllerNumber);
        ZipResult.LearnMode LearnMode(Alias zipControllerNumber, LearnModes mode, int timeoutMs);
        ZipResult.LearnMode LearnMode(Alias zipControllerNumber);
        ZipResult.Ping Ping(IPAddress ipAddress);
        ZipResult.Ping Ping(IPAddress ipAddress, int count, int timeoutMs, int intervalMs);
        ZipResult.Ping Ping(Alias zipDeviceNumber);
        byte[] PutIPv4inIPv6Format(byte[] ipV4addr);
        void Reconnect(Alias deviceNumber);
        void RegisterUnsolicitedZipDevice(Alias deviceNumber, IPEndPoint unsolEndPoint);
        void RegisterUnsolicitedZipDevice(Alias ZipController, Alias UnsoldeviceNumber, Alias deviceNumber);
        void RegisterZipDevice(Alias zipControllerNumber, Alias deviceNumber, Node node);
        void RegisterZipDevice(Alias zipControllerNumber, Alias deviceNumber, IPAddress deviceIpAddress);
        ZipResult.RemoveNode RemoveNode(Alias zipControllerNumber, Alias deviceNumber);
        ZipResult.RemoveNode RemoveNode(Alias zipControllerNumber, Alias deviceNumber, Modes mode, int timeoutMs);
        ZipResult.RemoveNode RemoveNode(Alias zipControllerNumber, Alias deviceNumber, Modes mode, ZWave.Enums.LearnModes learnMode, int timeoutMs);
        ZipResult.RequestData RequestData(Alias zipDeviceNumber, byte[] data, byte[] expectedData, int timeoutMs);
        ZipResult.RequestData RequestData(Alias zipDeviceNumber, byte[] headerExtension, byte[] data, byte[] expectedData, int timeoutMs);
        ZipResult.SendData SendData(Alias zipReceiverNumber, byte[] headerExtension, byte[] packet, bool isValidateAckSeqNo, int timeout);
        ZipResult.SendData SendData(Alias zipReceiverNumber, byte[] headerExtension, byte[] packet, bool isValidateAckSeqNo);
        ZipResult.SendData SendData(Alias zipReceiverNumber, byte[] packet, bool isValidateAckSeqNo);
        ZipResult.SendData SendData(Alias zipReceiverNumber, byte[] packet);
        ZipResult.SendData SendDataIPMulticast(Alias FirstNodeAlias, byte HeaderLengthByte,
            COMMAND_CLASS_ZIP_V5.COMMAND_ZIP_PACKET.PacketOption OptionsDataBytes, byte[] ZWaveCommandBytes);
        ZipResult.SetDefault SetDefault(Alias zipDeviceNumber);
        ZipResult.SetDefault SetDefault(Alias zipDeviceNumber, int timeoutMs);
        void SetTestInclusionValues(byte[] dsk, byte[] csa);
        void SetTestInclusionValues(byte[] dsk);
        void SetTestInclusionValues(byte[] dsk, byte[] csa, bool isKexReportConfirmed);
        ServiceSettings Settings { get; set; }
        void UnregisterZipDevice(Alias deviceNumber);
        ZipResult.FirmwareBackup FirmwareBackupV5(Alias zipControllerNumber, int firmwareTarget, int timeoutMs);
        ZipResult.FirmwareBackup FirmwareBackupV5(Alias zipControllerNumber, byte[] headerExtension, int firmwareTarget, int timeoutMs);
        ZipResult.FirmwareUpdate FirmwareUpdateV5(Alias zipControllerNumber, byte[] data, int firmwareTarget, int timeoutMs);
        ZipResult.FirmwareUpdate FirmwareUpdateV5(Alias zipControllerNumber, byte[] headerExtension, byte[] data, int firmwareTarget, bool hasActivation, int timeoutMs);
        void StopCallbacksListeners(Alias zipControllerNumber);
        bool IsPortaConnection(Alias zipControllerNumber);
        bool IsZipConnected(Alias zipControllerNumber);
        void ReconnectZipController(Alias zipController);
        void ReconnectUnsolicited(Alias unsolicitedNumber);
        void Disconnect(Alias deviceNumber);
        void DeleteProvisioningList(Alias zipControllerNumber);
        ZipResult.AddNode IncludeSmartStartDevice(Alias zipControllerNumber, Alias zipControllerUnsolicitedNumber, Alias endDevice, int timeoutMs, bool isLongRange);
        void SetProvisioningList(Alias zipControllerNumber, List<byte[]> Dsk, bool isLongRange);
    }
}
