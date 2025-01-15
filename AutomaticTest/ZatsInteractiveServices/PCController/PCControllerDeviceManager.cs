// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using Utils;
using ZWave.Xml.Application;

namespace Zats.Services
{
    public class PCControllerDeviceManager : DeviceManagerBase, IDisposable
    {
        private readonly object _locker = new object();
        private SortedList<Alias, UIInstance> _pcControllerInstances = new SortedList<Alias, UIInstance>();
        public SortedList<Alias, DeviceHostClient> _devices = new SortedList<Alias, DeviceHostClient>();
        public ZWaveDefinition ZWaveDefinition { get; private set; }
        public Dictionary<string, object> CustomProperties { get; set; }
        public Parameters Parameters { get; set; }
        private DeviceManager DeviceManager { get; set; }

        public IIncludeSettings IncludeSettings
        {
            get
            {
                return DeviceManager.IncludeSettings;
            }

            set
            {
                DeviceManager.IncludeSettings = value;
            }
        }

        public PCControllerDeviceManager(ZWaveDefinition zWaveDefinition, DeviceManager deviceManager)
        {
            ZWaveDefinition = zWaveDefinition;
            CustomProperties = new Dictionary<string, object>();
            Parameters = new Parameters();
            DeviceManager = deviceManager;
        }
        public Alias SisController;

        public void InitDevices(IDictionary<Alias, DeviceHostClient> devices)
        {
            foreach (var item in devices)
            {
                _devices.Add(item.Key, item.Value);
                _pcControllerInstances.Add(item.Key, new UIInstance(item.Value.Host.SName)
                {
                    Library = (LibTypes)item.Value.Library,
                    CurrentProfile = CurrentProfile,
                    IsZipDataSource = item.Value.Host.SType == InterfaceTypes.UDP 
                    || item.Value.Host.SType == InterfaceTypes.DTLS1
                });
            }
        }

        internal bool HasDevice(Alias deviceNumber)
        {
            return GetDeviceHostClient(deviceNumber) != null;
        }

        public DeviceHostClient GetDeviceHostClient(Alias deviceAlias)
        {
            lock (_locker)
            {
                if (_devices.ContainsKey(deviceAlias))
                    return _devices[deviceAlias];
                else
                    return DeviceManager.GetDeviceHostClient(deviceAlias);
            }
        }

        public UIInstance GetUIInstance(Alias deviceAlias)
        {
            lock (_locker)
            {
                if (_pcControllerInstances.ContainsKey(deviceAlias))
                    return _pcControllerInstances[deviceAlias];
                else
                    return null;
            }
        }

        internal IEnumerable<Alias> GetDeviceKeys()
        {
            return _devices.Select(x => x.Key);
        }

        public byte[] GetDSK(Alias deviceNumber, int numberOfBytes)
        {
            var device = GetDeviceHostClient(deviceNumber);
            int length = numberOfBytes < 0 ? 0 : numberOfBytes > 32 ? 32 : numberOfBytes;
            byte[] dsk = new byte[length];
            if (device.Host.Dsk != null)
            {
                Array.Copy(device.Host.Dsk, 0, dsk, 0, dsk.Length);
            }
            return dsk;
        }

        public bool ConnectDevices()
        {
            int instanceCount = 0;
            foreach (var item in _devices)
            {
                if (!_pcControllerInstances[item.Key].RunInstanceAndConnect(++instanceCount))
                {
                    return false;
                }
            }
            return true;
        }

        public bool CloseDevice(Alias device)
        {
            return false;
        }

        public void InitDevices(IEnumerable<Pair<string, string>> basicDeviceHosts, List<UnknownDevice> appendList)
        {
            throw new NotImplementedException();
        }

        public void FinalizeDevice(int number)
        {
            throw new NotImplementedException();
        }

        public void FinalizeDevice(Alias alias)
        {
            throw new NotImplementedException();
        }

        #region IDisposable Members

        public void Dispose()
        {
            foreach (var item in _pcControllerInstances)
            {
                item.Value.Dispose();
            }
            _pcControllerInstances.Clear();
            _devices.Clear();
            GC.Collect(); // to check performance in a pack of TCs
        }

        #endregion

        internal void SetCustomProperty(string key, object value)
        {
            if (!CustomProperties.ContainsKey(key))
            {
                CustomProperties.Add(key, value);
            }
            else
            {
                CustomProperties[key] = value;
            }
        }

        internal object GetCustomProperty(string key)
        {
            object ret = null;
            if (CustomProperties.ContainsKey(key))
            {
                ret = CustomProperties[key];
            }
            return ret;
        }

    }
}
