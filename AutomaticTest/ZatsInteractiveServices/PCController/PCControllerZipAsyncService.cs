// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils.UI.Enums;

namespace Zats.Services.PCController
{
    public class PCControllerZipAsyncService : ServiceBase, IZipAsyncService
    {
        protected override void Log(string text, LogLevels logLevel)
        {
            throw new NotImplementedException();
        }

        public ZipToken.AddNode AddNode(Alias zipControllerNumber, Modes mode, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public ZipToken.AddNode AddNode(Alias zipControllerNumber, Modes mode, SecuritySchemes[] GrantSecurities, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public ZipToken.AddNode AddNode(Alias zipControllerNumber, Modes mode, int timeoutMs,
            TransmitOptions Txo)
        {
            throw new NotImplementedException();
        }

        public ZipToken.AddNode AddNodeWithDSK(Alias zipControllerNumber, Modes mode, int dsk, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public ZipToken.AddNode AddNodeWithDSK(Alias zipControllerNumber, Modes mode, byte[] dsk, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public ZipToken.AddNode AddNodeWithDSK(Alias zipControllerNumber, Modes mode, byte[] dsk, SecuritySchemes[] grantedSchemes, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public ZipToken.AddNode AddNodeWithDSK(Alias zipControllerNumber,
            Modes mode, byte[] dsk, SecuritySchemes[] GrantSecurities, int timeoutMs, TransmitOptions Txo)
        {
            throw new NotImplementedException();
        }
        public ZipToken.ExpectData ExpectData(Alias zipSenderNumber, byte[] expectedPacket, int timeoutMs, int hitCount)
        {
            throw new NotImplementedException();
        }

        public ZipToken.ExpectData ExpectData(Alias zipSenderNumber, byte[] expectedPacket, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public ZipToken.RemoveNode RemoveNode(Alias zipControllerNumber, Modes mode, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public void ResetAllPing()
        {
            throw new NotImplementedException();
        }

        public ZipToken.SendData SendData(Alias zipReceiverNumber, byte[] packet)
        {
            throw new NotImplementedException();
        }

        public ZipToken.SendData SendData(Alias zipReceiverNumber, byte[] packet, int timeout)
        {
            throw new NotImplementedException();
        }

        public ZipToken.LearnMode SetLearnMode(Alias zipControllerNumber, LearnModes mode, int timeoutMs)
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

        public void StartPing(Alias zipDeviceNumber, int timeoutMs, int intervalMs)
        {
            throw new NotImplementedException();
        }

        public void StartPing(System.Net.IPAddress ipAddress, int timeoutMs, int intervalMs)
        {
            throw new NotImplementedException();
        }

        public ZipResult.Ping StopPing(System.Net.IPAddress ipAddress)
        {
            throw new NotImplementedException();
        }

        public ZipResult.Ping StopPing(Alias zipDeviceNumber)
        {
            throw new NotImplementedException();
        }

        public bool WaitCompletedSignal(_ActionToken token)
        {
            throw new NotImplementedException();
        }

        public bool WaitForPingStart(System.Net.IPAddress ipAddress, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public bool WaitForPingStart(Alias zipDeviceNumber, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public ZipToken.AddNode AddNode(Alias zipControllerNumber, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public void CancelToken(Alias zipReceiverNumber, _ActionToken token)
        {
            throw new NotImplementedException();
        }

        public void CancelTokenArray(Alias ZipUnsol, ZipToken.Void[] tokenArray)
        {
            throw new NotImplementedException();
        }

        public ZipToken.SendData SendData(Alias zipReceiverNumber, byte[] packet, int timeout, bool isNoAck)
        {
            throw new NotImplementedException();
        }

        public DateTime SendDataTimeStamp(Alias zipReceiverNumber, byte[] packet, int timeout)
        {
            throw new NotImplementedException();
        }
        public byte FirmwareUpdateV5(Alias deviceNumber, Alias enddevice, Node node, string filePath, COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_REPORT fmRep)
        {
            throw new NotImplementedException();
        }
    }
}
