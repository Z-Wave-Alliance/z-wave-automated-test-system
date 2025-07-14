// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Threading;
using ZWave.BasicApplication.Devices;
using Utils.UI.Enums;
using Utils.Extensions;
using System.Collections.Generic;

namespace Zats.Services
{
    /// <summary>
    /// Provides methods that executed simultaneously on several device modules
    /// </summary>
    public class ParallelService : ServiceBase, IParallelService
    {
        private DeviceManager DeviceManager { get; set; }

        public ParallelService(DeviceManager deviceManager)
        {
            DeviceManager = deviceManager;
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            DeviceManager.Log(text, logLevel);
        }

        public bool result = true;


        /// <summary>
        /// Consequentially calls SetDefault(), SoftReset(), MemoryGetId()
        /// </summary>
        /// <param name="deviceNumbers"></param>
        public void Reset(params Alias[] deviceNumbers)
        {
            deviceNumbers?.DoForEach(deviceNumber =>
            {
                var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
                DeviceManager.Reset(deviceHostClient, deviceNumber);
            });
        }

        public void EnableSmartStart(params Alias[] deviceNumbers)
        {
            for (int i = 0; i < deviceNumbers.Length; i++)
            {
                Alias deviceNumber = deviceNumbers[i];
                DeviceManager.EnableSmartStart(deviceNumber);
                DeviceManager.RunSmartStartSupport(deviceNumber);
            }
        }

        public void DisableSmartStart(params Alias[] deviceNumbers)
        {
            for (int i = 0; i < deviceNumbers.Length; i++)
            {
                Alias deviceNumber = deviceNumbers[i];
                DeviceManager.DisableSmartStart(deviceNumber);
                DeviceManager.CancelSmartStartSupport(deviceNumber);
            }
        }

        public void SetDefault(IEnumerable<Alias> deviceNumbers)
        {
            deviceNumbers?.DoForEach(deviceNumber =>
            {
                var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
                var device = deviceHostClient.SClient as Device;
                device.SetDefault();
                device.MemoryGetId();
            });
        }

        public void SetDefault(params Alias[] deviceNumbers)
        {
            SetDefault((IEnumerable<Alias>)deviceNumbers);
        }

        public void Reconnect(Alias[] deviceNumbers)
        {
            deviceNumbers?.DoForEach(deviceNumber =>
            {
                var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
                DeviceManager.Reconnect(deviceHostClient);
            });
            Thread.Sleep(200);
        }

        public void SoftReset(params Alias[] deviceNumbers)
        {
            deviceNumbers?.DoForEach(deviceNumber =>
            {
                var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
                DeviceManager.SoftReset(deviceHostClient, deviceNumber);
            });
        }

        public void MemoryGetId(params Alias[] deviceNumbers)
        {
            deviceNumbers?.DoForEach(deviceNumber =>
            {
                var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
                var device = deviceHostClient.SClient as Device;
                device.MemoryGetId();
            });
        }

        public void GetVersion(params Alias[] deviceNumbers)
        {
            deviceNumbers?.DoForEach(deviceNumber =>
            {
                var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
                var device = deviceHostClient.SClient as Device;
                device.GetVersion();
            });
        }

        public void SerialApiGetCapabilities(params Alias[] deviceNumbers)
        {
            deviceNumbers?.DoForEach(deviceNumber =>
            {
                var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
                var device = deviceHostClient.SClient as Device;
                device.SerialApiGetCapabilities();
            });
        }

        public void SerialApiGetInitData(Alias[] deviceNumbers)
        {
            deviceNumbers?.DoForEach(deviceNumber =>
            {
                var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
                var device = deviceHostClient.SClient as Device;
                device.SerialApiGetInitData();
            });
        }

        public void SetRFReceiveMode(bool mode, params Alias[] deviceNumbers)
        {
            deviceNumbers?.DoForEach(deviceNumber =>
            {
                var deviceHostClient = DeviceManager.GetDeviceHostClient(deviceNumber);
                var device = deviceHostClient.SClient as Device;
                device.SetRFReceiveMode((byte)((mode == true) ? 0x01 : 0x00));
            });
        }
    }
}
