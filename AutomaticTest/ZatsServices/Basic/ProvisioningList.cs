// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Linq;

namespace Zats.Services
{
    public class ProvisioningList : IProvisioningList
    {
        private SortedList<Alias, List<ProvisioningItem>> _deviceProvisioningList = new SortedList<Alias, List<ProvisioningItem>>();
        public ProvisioningList()
        {
        }

        public List<ProvisioningItem> GetDeviceProvisioningList(Alias alias)
        {
            lock (this)
            {
                if (!_deviceProvisioningList.Keys.Contains(alias))
                {
                    _deviceProvisioningList.Add(alias, new List<ProvisioningItem>());
                }
            }
            return _deviceProvisioningList[alias];
        }

        #region IProvisioningList Members

        //public void Add(int key, byte[] dsk, SecuritySchemes[] schemes)
        //{
        //    Add(key, dsk, schemes, Modes.NodeOptionNetworkWide);
        //}

        public void Add(Alias key, byte[] dsk, SecuritySchemes[] schemes, Modes nodeOptions)
        {
            byte schemesFlags = 0;
            if (schemes != null && schemes.Length > 0)
            {
                schemesFlags = (byte)ZWave.BasicApplication.Security.SecurityManagerInfo.
                    ConvertToNetworkKeyMask(schemes.Select(x => (ZWave.Enums.SecuritySchemes)x).ToArray());
            }
            else
            {
                schemesFlags = 0x87;
            }
            GetDeviceProvisioningList(key).Add(new ProvisioningItem() { Dsk = dsk, GrantSchemes = schemesFlags, NodeOptions = (byte)nodeOptions });
        }

        public void Clear(Alias key)
        {
            GetDeviceProvisioningList(key).Clear();
        }

        public ProvisioningItem Get(Alias key, byte[] dsk)
        {
            return GetDeviceProvisioningList(key).FirstOrDefault(x => x.Dsk != null && x.Dsk.SequenceEqual(dsk));
        }

        public void Remove(Alias key, byte[] dsk)
        {
            var item = GetDeviceProvisioningList(key).FirstOrDefault(x => x.Dsk != null && x.Dsk.SequenceEqual(dsk));
            if (item != null)
            {
                GetDeviceProvisioningList(key).Remove(item);
            }
        }

        #endregion
    }

    public class ProvisioningService : IProvisioningService
    {
        private DeviceManager DeviceManager { get; set; }

        public ProvisioningService(DeviceManager deviceManager)
        {
            DeviceManager = deviceManager;
        }

        public void Add(Alias controllerNumber, Alias deviceNumber)
        {
            Add(controllerNumber, deviceNumber, SecuritySchemeSet.ALL);
        }

        public void Add(Alias controllerNumber, Alias deviceNumber, SecuritySchemes[] schemes)
        {
            var controller = DeviceManager.GetDeviceHostClient(controllerNumber);
            var device = DeviceManager.GetDeviceHostClient(deviceNumber);
            if (controller.IsLongRange && device.IsLongRange)
            {
                DeviceManager.ProvisioningList.Add(controllerNumber, DeviceManager.GetDSK(deviceNumber, 16), schemes, Modes.NodeOptionLongRange);
            }
            else
            {
                DeviceManager.ProvisioningList.Add(controllerNumber, DeviceManager.GetDSK(deviceNumber, 16), schemes, Modes.NodeOptionNetworkWide);
            }
        }

        public void Add(Alias controllerNumber, Alias deviceNumber, SecuritySchemes[] schemes, Modes nodeOptions)
        {
            DeviceManager.ProvisioningList.Add(controllerNumber, DeviceManager.GetDSK(deviceNumber, 16), schemes, nodeOptions);
        }

        public void Add(Alias controllerNumber, byte[] dsk, SecuritySchemes[] schemes, Modes nodeOptions)
        {
            DeviceManager.ProvisioningList.Add(controllerNumber, dsk, schemes, nodeOptions);
        }

        /// <summary>
        /// Setups predefined DSK to Provisioning List from IncludeSettings.JoinDSK
        /// </summary>
        /// <param name="controllerNumber"></param>
        /// <param name="schemes"></param>
        public void AddDUTPreConfigured(Alias controllerNumber, SecuritySchemes[] schemes)
        {
            if (DeviceManager.IncludeSettings.JoinDSK != null && DeviceManager.IncludeSettings.JoinDSK.Length == 16)
            {
                DeviceManager.ProvisioningList.Add(controllerNumber, DeviceManager.IncludeSettings.JoinDSK, schemes, Modes.NodeOptionNetworkWide);
            }
        }

        public void Add(Alias controllerNumber, Alias[] deviceNumbers, SecuritySchemes[] schemes)
        {
            foreach (var deviceNumber in deviceNumbers)
            {
                Add(controllerNumber, deviceNumber, schemes);
            }
        }

        public void Remove(Alias controllerNumber, Alias deviceNumber)
        {
            DeviceManager.ProvisioningList.Remove(controllerNumber, DeviceManager.GetDSK(deviceNumber, 16));
        }

        public void Clear(Alias controllerNumber)
        {
            DeviceManager.ProvisioningList.Clear(controllerNumber);
        }

        public ProvisioningItem Get(Alias controllerNumber, Alias deviceNumber)
        {
            return DeviceManager.ProvisioningList.Get(controllerNumber, DeviceManager.GetDSK(deviceNumber, 16));
        }
    }
}
