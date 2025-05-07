using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZWave.TextApplication;
using ZWave.Layers.Session;
using ZWave.Layers.Transport;
using ZWave.Layers;
using ZWave.Enums;
using System.Net;
using Utils;
using ZWave.BasicApplication;

namespace Zats.Services
{
    public class DebugDeviceManager : DeviceManagerBase
    {
        public DebugDeviceManager()
            : base()
        {
            ApplicationLayer = new TextApplicationLayer(SessionLayer, new TextFrameLayer(), new SerialPortTransportLayer());
            TcpApplicationLayer = new TextApplicationLayer(SessionLayer, new TextFrameLayer(), new TcpClientTransportLayer());
        }

        private SortedList<Alias, TextDevice> _devices = new SortedList<Alias, TextDevice>();
        public TextApplicationLayer ApplicationLayer { get; set; }
        public TextApplicationLayer TcpApplicationLayer { get; set; }

        public void FinalizeDevice(Alias deviceAlias)
        {
            lock (this)
            {
                if (_devices.ContainsKey(deviceAlias))
                {
                    _devices[deviceAlias].Dispose();
                    _devices.Remove(deviceAlias);
                }
            }
        }

        public TextDevice GetDevice(Alias deviceAlias)
        {
            lock (this)
            {
                if (_devices.Keys.Contains(deviceAlias))
                    return _devices[deviceAlias];
                else
                    return null;
            }
        }

        public void ConnectDevices(Alias[] devicesNumbers)
        {
            foreach (var item in devicesNumbers)
            {
                TextDevice device = GetDevice(item);
                try
                {
                    if (device != null)
                    {
                        device.Connect();
                    }
                }
                catch (KeyNotFoundException)
                {
                    // Do nothing.
                }
            }
        }

        public void InitDevices(List<DeviceHost> available, InterfaceTypes iftype, SortedList<Alias, string> required)
        {
            if (required.Count > 0)
            {
                var tmpAvailable = available.Where(deviceHost => deviceHost.SType == iftype && !string.IsNullOrEmpty(deviceHost.SName));
                var tmpRequired = required.ToArray();

                foreach (var device in tmpRequired)
                {
                    var deviceHost = tmpAvailable.FirstOrDefault(dh => dh.Alias == device.Value); // Compare by alias.
                    if (deviceHost == null)
                        deviceHost = tmpAvailable.FirstOrDefault(dh => string.IsNullOrEmpty(dh.Alias)); // Compare by empty alias.
                    if (deviceHost != null)
                    {
                        available.Remove(deviceHost);
                        required.Remove(device.Key);

                        var port = deviceHost.SName;
                        TextDevice textDevice = null;
                        if (Uri.TryCreate(string.Format("http://{0}", deviceHost.SName), UriKind.Absolute, out Uri url) &&
                            IPAddress.TryParse(url.Host, out IPAddress ipAddress))
                        {
                            textDevice = TcpApplicationLayer.CreateTextDevice();
                            textDevice.RequestEndOfLine = "\r";
                            textDevice.ResponseEndOfLine = "\r";
                            textDevice.DataSource = new SocketDataSource(ipAddress.ToString(), url.Port, null, deviceHost.Alias);
                        }
                        else
                        {
                            textDevice = ApplicationLayer.CreateTextDevice();
                            textDevice.DataSource = new SerialPortDataSource(port, BaudRates.Rate_115200);
                        }
                        _devices.Add(device.Key, textDevice);
                    }
                }
            }
        }

        public void InitDevices(List<DeviceHost> available, SortedList<Alias, string> required)
        {
            InitDevices(available, InterfaceTypes.TCP, required);
            InitDevices(available, InterfaceTypes.COM, required);
        }

        public void FinalizeDevices()
        {
            var keys = _devices.Keys.ToArray();
            foreach (var key in keys)
            {
                FinalizeDevice(key);
            }
        }
    }
}
