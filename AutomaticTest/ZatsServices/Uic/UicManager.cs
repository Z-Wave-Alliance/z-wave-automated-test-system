using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UicApplication;
using UicApplication.Devices;
using Utils;
using ZWave.Layers;
using ZWave.Layers.Transport;
using ZWave.TextApplication;
using ZWave.UicApplication;
using ZWave.UicApplication.PayloadWrappers;
using ZWave.UicApplication.SshTransport;
using ZWave.ZipApplication;

namespace Zats.Services
{
    public class UicManager : DeviceManagerBase
    {
        public bool UseMqttConnection = true;

        public UicClient mainUiClient;

        private SortedList<Alias, string> _clients = new SortedList<Alias, string>();
        private SortedList<Alias, DeviceHostClient> _devicesHosts = new SortedList<Alias, DeviceHostClient>();
        private SortedList<Alias, UicDevice> _endDeviceHosts = new SortedList<Alias, UicDevice>();
        private SortedList<Alias, SshTerminalClient> _sshClientHosts = new SortedList<Alias, SshTerminalClient>();
        public UicApplicationLayer ApplicationLayer { get; set; }
        public UicDeviceApplicationLayer DeviceApplicationLayer { get; set; }
        public UicManager()
            : base()
        {
            if (UseMqttConnection)
            {
                ApplicationLayer = new UicApplicationLayer(SessionLayer,
                    new UicFrameLayer(), new UicTransportLayer());
                DeviceApplicationLayer = new UicDeviceApplicationLayer(SessionLayer,
                    new TextFrameLayer(), new TcpClientTransportLayer());
            }
            else
            {
                ApplicationLayer = new UicApplicationLayer(SessionLayer,
                new UicFrameLayer(), new UicSocketTransportLayer());
                DeviceApplicationLayer = new UicDeviceApplicationLayer(SessionLayer,
                    new TextFrameLayer(), new TcpClientTransportLayer());
            }
        }
       
        public string GetClient(Alias alias)
        {
            lock (this)
            {
                if (_clients.Keys.Contains(alias))
                    return _clients[alias];
                else
                    return null;
            }
        }
        
        public UicDevice GetDevice(Alias alias)
        {
            lock (this)
            {
                if (_endDeviceHosts.Keys.Contains(alias))
                    return _endDeviceHosts[alias];
                else
                    return null;
            }
        }

        public SshTerminalClient GetSshClient(Alias alias)
        {
            lock (this)
            {
                if (_sshClientHosts.Keys.Contains(alias))
                    return _sshClientHosts[alias];
                else
                    return null;
            }
        }

        public bool InitDevice(List<DeviceHost> available, KeyValuePair<Alias, UicDeviceMappingAttribute> requiredDevice)
        {
            List<DeviceHost> checkAvailable = new List<DeviceHost>();
            List<DeviceHost> tmpAvailable = available.FindAll(dh => dh.SType == InterfaceTypes.MQTT);
            var deviceHost = available.FirstOrDefault(dh => dh.Alias == requiredDevice.Value.Alias &&
                (dh.SType == InterfaceTypes.MQTT));

            if (deviceHost == null)
            {
                return false;
            }
            available.Remove(deviceHost);
            InitDevice(requiredDevice.Key, deviceHost.SName, deviceHost.Alias);

            if (!_devicesHosts.ContainsKey(requiredDevice.Key))
            {
                _devicesHosts.Add(requiredDevice.Key, new DeviceHostClient(deviceHost));
            }
            return true;
        }
        public void InitDevice(Alias id, string sourceName, string alias)
        {
            if (sourceName != null && !_clients.ContainsKey(id))
            {
                UicClient uicDevice = ApplicationLayer.CreateUicClient();
                uicDevice.DataSource = new DataSourceBase()
                {
                    Alias = alias,
                    SourceName = sourceName
                };

                uicDevice.UnId = GetDeviceUnid(uicDevice);
                uicDevice.Subscribe("ucl/#", null);
                _clients.Add(id, uicDevice.UnId);
                mainUiClient = uicDevice;
            }
        }
        public bool InitEndDevice(List<DeviceHost> available, KeyValuePair<Alias, ZigDeviceMappingAttribute> requiredDevice)
        {
            List<DeviceHost> checkAvailable = new List<DeviceHost>();
            List<DeviceHost> tmpAvailable = available.FindAll(dh => dh.SType == InterfaceTypes.ZIGBEE);
            var deviceHost = available.FirstOrDefault(dh => dh.Alias == requiredDevice.Value.Alias &&
                (dh.SType == InterfaceTypes.ZIGBEE));

            if (deviceHost == null)
            {
                return false;
            }
            available.Remove(deviceHost);
            InitEndDevice(requiredDevice.Key, deviceHost.SName, deviceHost.Alias, deviceHost.SArgs);

            return true;
        }

        public void InitEndDevice(Alias id, string sourceName, string alias, string SArgs)
        {
            if (sourceName != null && !_endDeviceHosts.ContainsKey(id))
            {
                UicDevice uicDevice = DeviceApplicationLayer.CreateUicDevice();
                uicDevice.DataSource = new SocketDataSource()
                {
                    Alias = alias,
                    Args = SArgs,
                    SourceName = sourceName,
                    Port = 4901
                };

                _endDeviceHosts.Add(id, uicDevice);
            }
        }

        public bool InitSshDevice(List<DeviceHost> available, KeyValuePair<Alias, SshDeviceMappingAttribute> requiredDevice)
        {
            List<DeviceHost> checkAvailable = new List<DeviceHost>();
            List<DeviceHost> tmpAvailable = available.FindAll(dh => dh.SType == InterfaceTypes.SSH);
            var deviceHost = available.FirstOrDefault(dh => dh.Alias == requiredDevice.Value.Alias &&
                (dh.SType == InterfaceTypes.SSH));

            if (deviceHost == null)
            {
                return false;
            }
            available.Remove(deviceHost);
            InitSshDevice(requiredDevice.Key, deviceHost.SName, deviceHost.Alias, deviceHost.SArgs);

            return true;
        }

        public void InitSshDevice(Alias id, string sourceName, string alias, string SArgs)
        {
            if (sourceName != null && !_sshClientHosts.ContainsKey(id))
            {
                var sshTerm = new SshTerminalClient();
                sshTerm.DataSource = new SocketDataSource()
                {
                    Alias = alias,
                    Args = SArgs,
                    SourceName = sourceName,
                    Port = 21
                };

                _sshClientHosts.Add(id, sshTerm);
            }
        }

        public void NewVirtualDevice(Alias virtualNumber, string unid)
        {
            lock (this)
            {
                if (_clients.Keys.Contains(virtualNumber))
                {
                    _clients[virtualNumber] = unid;
                }
                else
                {
                    _clients.Add(virtualNumber, unid);
                }
            }
        }
        public void FinalizeVirtualDevice(Alias virtualNumber)
        {
            lock (this)
            {
                if (_clients.Keys.Contains(virtualNumber))
                {
                    _clients.Remove(virtualNumber);
                }
            }
        }

        private string GetDeviceUnid(UicClient uicDevice)
        {
            string ret = null;
            if (UseMqttConnection)
            {
                uicDevice.Subscribe("ucl/by-unid/+/State", null);
                var unidResult = uicDevice.GetUnId();
                ret = unidResult.UnId;
            }
            else
            {
                var getUnidToken = uicDevice.RequestPlainData(null, "UIC_Get_Unid", null, "UIC_Get_Unid_Status", 30000, null);
                var res = (RequestDataResult)getUnidToken.WaitCompletedSignal();
                getUnidToken.Close();
                if (res.mqttReceivedPayload != null)
                {
                    var getUnidPayloadWrapper = JsonConvert.DeserializeObject<GetUnidPayloadWrapper>(res.mqttReceivedPayload);
                    ret = getUnidPayloadWrapper.Unid;
                }
            }
            return ret;
        }
    }
}
