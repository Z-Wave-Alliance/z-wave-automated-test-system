// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zats.Services
{
    public interface IZipAsyncService
    {
        ZipToken.AddNode AddNode(Alias zipControllerNumber, int timeoutMs);
        ZipToken.AddNode AddNode(Alias zipControllerNumber, Modes mode, int timeoutMs);
        ZipToken.AddNode AddNode(Alias zipControllerNumber, Modes mode, SecuritySchemes[] GrantSecurities, int timeoutMs);
        ZipToken.AddNode AddNode(Alias zipControllerNumber, Modes mode, int timeoutMs, TransmitOptions Txo);
        ZipToken.AddNode AddNodeWithDSK(Alias zipControllerNumber, Modes mode, int dsk, int timeoutMs);
        ZipToken.AddNode AddNodeWithDSK(Alias zipControllerNumber, Modes mode, byte[] dsk, int timeoutMs);
        ZipToken.AddNode AddNodeWithDSK(Alias zipControllerNumber, Modes mode, byte[] dsk, SecuritySchemes[] grantedSchemes, int timeoutMs);
        ZipToken.AddNode AddNodeWithDSK(Alias zipControllerNumber, Modes mode, byte[] dsk, SecuritySchemes[] GrantSecurities, int timeoutMs, TransmitOptions Txo);
        ZipToken.ExpectData ExpectData(Alias zipSenderNumber, byte[] expectedPacket, int timeoutMs, int hitCount);
        ZipToken.ExpectData ExpectData(Alias zipSenderNumber, byte[] expectedPacket, int timeoutMs);
        ZipToken.RemoveNode RemoveNode(Alias zipControllerNumber, Modes mode, int timeoutMs);
        void ResetAllPing();
        ZipToken.SendData SendData(Alias zipReceiverNumber, byte[] packet);
        ZipToken.SendData SendData(Alias zipReceiverNumber, byte[] packet, int timeout);
        ZipToken.SendData SendData(Alias zipReceiverNumber, byte[] packet, int timeout, bool isNoAck);
        DateTime SendDataTimeStamp(Alias zipReceiverNumber, byte[] packet, int timeout);
        ZipToken.LearnMode SetLearnMode(Alias zipControllerNumber, LearnModes mode, int timeoutMs);
        void SetTestInclusionValues(byte[] dsk, byte[] csa);
        void SetTestInclusionValues(byte[] dsk);
        void SetTestInclusionValues(byte[] dsk, byte[] csa, bool isKexReportConfirmed);
        void StartPing(Alias zipDeviceNumber, int timeoutMs, int intervalMs);
        void StartPing(System.Net.IPAddress ipAddress, int timeoutMs, int intervalMs);
        ZipResult.Ping StopPing(System.Net.IPAddress ipAddress);
        ZipResult.Ping StopPing(Alias zipDeviceNumber);
        bool WaitCompletedSignal(_ActionToken token);
        bool WaitForPingStart(System.Net.IPAddress ipAddress, int timeoutMs);
        bool WaitForPingStart(Alias zipDeviceNumber, int timeoutMs);
        void CancelToken(Alias zipReceiverNumber, _ActionToken token);
        void CancelTokenArray(Alias ZipUnsol, ZipToken.Void[] tokenArray);
        byte FirmwareUpdateV5(Alias deviceNumber, Alias enddevice, Node node, string filePath, COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_REPORT fmRep);
    }
}
