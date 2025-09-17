// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using Utils;
using ZWave.Enums;
using ZWave.Layers;
using ZWave.Layers.Transport;
using ZWave.TextApplication;

namespace Zats.Services.SampleApplication
{
    public class SilinkManager
    {
        private const int DEFAULT_ADMIN_CONSOLE_PORT = 4902;
        private readonly ITransportLayer _transportLayer = new TcpClientTransportLayer(); //DO NOT USE <new TelnetTransportLayer();> - strnage implementation
        private readonly DeviceManager _deviceManager;

        /// <summary>
        /// List of telent supported Text providers
        /// </summary>
        private readonly ConcurrentDictionary<DeviceHostClient, TextDevice> _silinkAdminDevices = new ConcurrentDictionary<DeviceHostClient, TextDevice>();
        /// <summary>
        /// List of silink admin connection (for USB connected devices only)
        /// </summary>
        private readonly ConcurrentDictionary<DeviceHostClient, SilinkApplicationProvider> _silinkApplications = new ConcurrentDictionary<DeviceHostClient, SilinkApplicationProvider>();
        private int _lastUsedPort = 4900;

        public SilinkManager(DeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
            _transportLayer = _deviceManager.TcpApplicationLayer.TransportLayer; // new TcpClientTransportLayer();
        }

        private void StopSilinkApplication(DeviceHostClient deviceHostClient)
        {
            if (_silinkApplications.ContainsKey(deviceHostClient))
            {
                _silinkApplications.TryRemove(deviceHostClient, out SilinkApplicationProvider silinkApplication);
                silinkApplication.Dispose();
            }
        }

        /// <summary>
        /// Get Silink Admin console.
        /// Start silink in case of USB connected device.
        /// </summary>
        /// <param name="deviceHostClient">Connacted Device host information.</param>
        /// <exception cref="InvalidOperationException"></exception>
        private void StartSilinkApplication(DeviceHostClient deviceHostClient)
        {
            StopSilinkApplication(deviceHostClient);

            var path = Path.GetDirectoryName(_deviceManager.CurrentProfile.CommanderAppPath);
            var silinkAppPath = new DirectoryInfo(path).GetFiles("silink.exe", SearchOption.AllDirectories).FirstOrDefault().FullName;
            SilinkApplicationProvider silinkAppProvider = new SilinkApplicationProvider(silinkAppPath);

            _lastUsedPort += 100;
            var isSilinked = silinkAppProvider.Connect(deviceHostClient.Host.JName, _lastUsedPort);
            if (!isSilinked)
            {
                silinkAppProvider.Dispose();
                _deviceManager.ResetSample(deviceHostClient);
                Thread.Sleep(1000);
                _lastUsedPort += 10;
                isSilinked = silinkAppProvider.Connect(deviceHostClient.Host.JName, _lastUsedPort);
            }
            if (isSilinked)
            {
                _silinkApplications.AddOrUpdate(deviceHostClient, silinkAppProvider, (x, y) => silinkAppProvider);
            }
            else
            {
                silinkAppProvider.Dispose();
                throw new InvalidOperationException($"Failed connect using Silink path {silinkAppPath} to {deviceHostClient.Host.JName}");
            }
        }

        /// <summary>
        /// Init connection to Silink admin and creates TextDevice for HMIActions.
        /// Starts Silink connection in case of USB connection.
        /// </summary>
        /// <param name="deviceHostClient">Device host</param>
        /// <exception cref="InvalidOperationException">Silink connection exception</exception>
        public void Init(DeviceHostClient deviceHostClient)
        {
            string admin_ip = string.Empty;
            int admin_port = 0;
            if (deviceHostClient.Host.SType == InterfaceTypes.COM)
            {
                StartSilinkApplication(deviceHostClient);
                if (_silinkApplications.TryGetValue(deviceHostClient, out SilinkApplicationProvider silinkApp))
                {
                    admin_ip = silinkApp.Address;
                    admin_port = silinkApp.ADM_CONSOLE;
                    //Substitute SerialPort connection with Virtual COM
                    var vComDeviceClient = new TextApplicationLayer(ApiTypes.SampleCLI,
                        _deviceManager.SessionLayer,
                        _deviceManager.TextApplicationLayer.FrameLayer,
                        _transportLayer)
                        .CreateTextDevice();
                    vComDeviceClient.ApiType = ApiTypes.SampleCLI;
                    vComDeviceClient.DataSource = new SocketDataSource(silinkApp.Address, silinkApp.VCOM0);
                    deviceHostClient.SClient.Disconnect();
                    deviceHostClient.SClient = vComDeviceClient;
                    deviceHostClient.SClient.Connect();
                }
            }
            else if (deviceHostClient.Host.SType == InterfaceTypes.TCP)
            {
                admin_ip = deviceHostClient.SClient.DataSource.SourceName;
                admin_port = DEFAULT_ADMIN_CONSOLE_PORT;

                TextDevice textDevice = _deviceManager.TextApplicationLayer.CreateTextDevice(deviceHostClient.SClient);
                textDevice.ApiType = ApiTypes.SampleCLI;
                deviceHostClient.SClient = textDevice;
            }
            else
            {
                throw new InvalidOperationException("Undefined Device type tried to init using Silink");
            }
            var adminDevice = new TextApplicationLayer(ApiTypes.WstkInstrument,
                _deviceManager.SessionLayer,
                _deviceManager.TextApplicationLayer.FrameLayer,
                _transportLayer)
                .CreateTextDevice();
            adminDevice.DataSource = new SocketDataSource(admin_ip, admin_port);
            _silinkAdminDevices.AddOrUpdate(deviceHostClient, adminDevice, (x, y) => adminDevice);

            StartSilinkAdmin(deviceHostClient);
        }

        /// <summary>
        /// Connect and start communication with Silink Admin CLI
        /// </summary>
        /// <param name="deviceHostClient"></param>
        /// <returns>Connection result.</returns>
        public bool StartSilinkAdmin(DeviceHostClient deviceHostClient)
        {
            var device = GetSilinkAdminDevice(deviceHostClient);
            bool isSucceed = false;
            try
            {
                if (device.Connect() == CommunicationStatuses.Done)
                {
                    var newLine = "\r\n";
                    device.RequestEndOfLine = newLine;
                    device.ResponseEndOfLine = "> "; // expected result end, like: WSTK|WPK>
                    SilinkAdminIOHandler silinkAdminIOHandler = new SilinkAdminIOHandler(device);
                    silinkAdminIOHandler.Ping();
                    var res = silinkAdminIOHandler.EnableButtons();
                    isSucceed = !string.IsNullOrEmpty(res) && (res.Contains("WSTK>") || res.Contains("WPK>"));
                }
            }
            catch (Exception ex)
            {
                ex.Message._DLOG();
            }
            return isSucceed;
        }

        public void Disconnect(DeviceHostClient deviceHostClient)
        {
            StopSilinkAdmin(deviceHostClient);
            StopSilinkApplication(deviceHostClient);
        }


        public void StopSilinkAdmin(DeviceHostClient deviceHostClient)
        {
            var device = GetSilinkAdminDevice(deviceHostClient);
            if (device != null)
            {
                //var cmd = "sys reset sys"; //will click reset button and disconnect from the silink admin, as an option.
                var cmd = "quit";
                device.Send(cmd);
                device.Disconnect();
                _silinkAdminDevices.TryRemove(deviceHostClient, out device);
                if (_silinkAdminDevices.IsEmpty) _lastUsedPort = 4900;
                device.Dispose();
            }
        }

        public TextDevice GetSilinkAdminDevice(Alias alias)
        {
            return GetSilinkAdminDevice(_deviceManager.GetDeviceHostClient(alias));
        }

        public TextDevice GetSilinkAdminDevice(DeviceHostClient deviceHostClient)
        {
            if (_silinkAdminDevices.TryGetValue(deviceHostClient, out TextDevice ret))
            {
                return ret;
            }
            return null;
        }

    }
}
