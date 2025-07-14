// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using ZWave.BasicApplication.TransportService;

namespace Zats.Services
{
    public class TransportServiceTestSettings : TestSettingsBase, ITransportServiceTestSettings
    {
        public TransportServiceTestSettings(DeviceManager deviceManager)
            : base(deviceManager)
        {
        }

        public void SetSubsequentFragmentCRC16(Alias deviceNumber, byte[] crc16, int offset, int timesUsed)
        {
            SetupTestValue(deviceNumber, tsmi =>
                {
                    tsmi.SetTestSubsequentFragmentCRC16(crc16, offset, timesUsed);
                });
        }

        public void SetFirstFragmentCRC16(Alias deviceNumber, byte[] crc16, int timesUsed)
        {
            SetupTestValue(deviceNumber, tsmi =>
                {
                    tsmi.SetTestFirstFragmentCRC16(crc16, timesUsed);
                });
        }

        public void SetSegmentCompleteCmdSessionId(Alias deviceNumber, byte sessionId, int timesUsed)
        {
            SetupTestValue(deviceNumber, tsmi =>
            {
                tsmi.SetTestSegmentCompleteCmdSessionId(sessionId, timesUsed);
            });
        }

        public void SetIgnoreFirstSegment(Alias deviceNumber, int timesUsed)
        {
            SetupTestValue(deviceNumber, tsmi =>
            {
                tsmi.SetTestIgnoreFirstSegment(timesUsed);
            });
        }

        public void SetIgnoreSubsequentSegment(Alias deviceNumber, int offset, int timesUsed)
        {
            SetupTestValue(deviceNumber, tsmi =>
            {
                tsmi.SetTestIgnoreSubsequentSegment(offset, timesUsed);
            });
        }

        private void SetupTestValue(Alias deviceNumber, Action<TransportServiceManagerInfo> setupAction)
        {
            var device = DeviceManager.GetDevice(deviceNumber);
            var tsm = device.SessionClient.GetSubstituteManager(typeof(TransportServiceManager));
            if (tsm != null)
            {
                var transportManager = (TransportServiceManager)tsm;
                setupAction(transportManager.TransportServiceManagerInfo);
            }
        }
    }
}
