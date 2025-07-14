// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Linq;
using Utils.UI.Enums;
using UicApplication.Devices;
using UicApplication.Operations;
using ZWave.BasicApplication.Devices;
using ZWave;
using System.Threading;
using ZWave.UicApplication;
using System.Collections.Generic;
using Newtonsoft.Json;
using ZWave.UicApplication.PayloadWrappers;
using ZWave.UicApplication.PayloadWrappers.Enums;
using UicApplication.Clusters;
using System.IO;

namespace Zats.Services
{
    public class UicDeviceAsyncService : ServiceBase, IUicDeviceAsyncService
    {
        private UicManager _uicManager;
        private DeviceManager _deviceManager;

        private const int STANDARD_OPERATION_TIMEOUT_MS = 60000;

        public UicDeviceAsyncService(UicManager uicManager)
        {
            _uicManager = uicManager;
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            _deviceManager.Log(text, logLevel);
        }

        public void SetDefault(Alias uicEndDeviceNumber, int timeoutMs)
        {
            var device = _uicManager.GetDevice(uicEndDeviceNumber);
            device.Connect();
            Thread.Sleep(3000);
            var res1 = device.RequestLine("", 3000);
            var res2 = device.RequestLine("network leave", 3000);
            var res3 = device.Send("\r\n");
            Thread.Sleep(3000);
            //var res4 = device.RequestLine("network leave", 3000);
            device.Disconnect();
            //device.SetDefault(null);
        }
        public void SetLearnMode(Alias uicEndDeviceNumber, int timeoutMs)
        {
            var device = _uicManager.GetDevice(uicEndDeviceNumber);
            device.Connect();
            Thread.Sleep(3000);
            var res1 = device.RequestLine("", 3000);
            var res2 = device.RequestLine("plugin network-steering start 0", 10000);
            var res3 = device.RequestLine("reset", 10000);
            var res4 = device.Send("\r\n");
            Thread.Sleep(3000);
            device.Disconnect();
            //device.SetLearnMode(null);
        }

        public void TestFunction(Alias uicEndDeviceNumber, int timeoutMs)
        {
            var device = _uicManager.GetDevice(uicEndDeviceNumber);
            device.Connect();
            device.RequestLine("\r\n");
            device.RequestLine("");
            Thread.Sleep(10000);
            device.Disconnect();
            //device.SetLearnMode(null);
        }
        public string GetDsk(Alias uicEndDeviceNumber, int timeoutMs)
        {
            var device = _uicManager.GetDevice(uicEndDeviceNumber);
            string dsk_str = device.DataSource.Args.Split(' ')[1];
            return dsk_str;
        }
    }
}
