// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using ZWave;
using ZWave.BasicApplication.Devices;

namespace Zats.Services
{
    public class FirmwareUpdateMD : IFirmwareUpdateMD
    {
        public ZWave.Enums.TransmitOptions TX_OPTIONS = ZWave.Enums.TransmitOptions.TransmitOptionAcknowledge | ZWave.Enums.TransmitOptions.TransmitOptionAutoRoute | ZWave.Enums.TransmitOptions.TransmitOptionExplore;
        private DeviceManager DeviceManager { get; set; }
        public FirmwareUpdateMD(DeviceManager deviceManager)
        {
            DeviceManager = deviceManager;
        }
        public BasicResult.Void DisableSupport(Alias controllerNumber, ActionToken token)
        {
            Device device = DeviceManager.GetDevice(controllerNumber);
            device.Cancel(token);
            return new BasicResult.Void();
        }

        public BasicResult.Void EnableSupport(params Alias[] controlles)
        {
            DeviceManager.EnableFirmwareUpdateMD(controlles);
            return new BasicResult.Void();
        }

        public BasicResult.Void EnableSupport(byte version, params Alias[] controlles)
        {
            DeviceManager.EnableFirmwareUpdateSupport(controlles, version);
            return new BasicResult.Void();
        }
    }
}