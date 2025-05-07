using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Utils;
using ZWave;
using ZWave.Layers;
using ZWave.Layers.Transport;
using ZWave.Security.S2;
using ZWave.ZipApplication;
using ZWave.ZipApplication.CommandClasses;
using ZWave.ZipApplication.Devices;
using ZipConstants = ZWave.ZipApplication.Constants;

namespace Zats.Services
{
    public class ZipManager : DeviceManagerBase, IUnsolicitedClientsFactory, IDisposable
    {
        public UnsolicitedDestinationHandler<IUnsolicitedClient> _primaryUnsolicitedClients;
        private ActionToken _networkManagementInclusionSupportToken;

        public ushort _unsolicitedPortNo = ZipConstants.DtlsPortNo;
        public ushort _unsolicitedPortSecondNo = ZipConstants.DtlsPortNo + 1;

        private String zipHostAddress;

        private string _args = string.Empty;
        private string _pskKey;
        public string InterfaceName { get; set; }
        public byte[] DskValue { get; set; }
        public byte[] CsaValue { get; set; }
        public bool IsKexReportConfirmed { get; set; }

        private bool isSshConnected;
        public SecuritySchemes[] GrantedSchemes { get; set; }

        private SortedList<Alias, ZipController> _devices = new SortedList<Alias, ZipController>();
        private SortedList<Alias, ZipDevice> _virtualDevices = new SortedList<Alias, ZipDevice>();
        private SortedList<Alias, DeviceHostClient> _devicesHosts = new SortedList<Alias, DeviceHostClient>();

        public MessageBoxImpl MessageBox { get; set; }
        public ProvisioningList ProvisioningList { get; set; }
        public IIncludeSettings IncludeSettings { get; set; }
        public ZipControllerDiscoverService ZipControllerDiscoverService { get; set; }

        private ISocketTransportLayer _transportLayer;
        private ZipApplicationLayer _applicationLayer;
        public ZipApplicationLayer ApplicationLayer
        {
            get => _applicationLayer;
            set
            {
                _applicationLayer = value;
                _transportLayer = (ISocketTransportLayer)_applicationLayer.TransportLayer;
            }
        }

        private IDtlsTransportLayer _dtls1TransportLayer;
        private ZipApplicationLayer _dtls1ApplicationLayer;
        public LibZwaveIpTransportClient _libZwaveIpTransportClient;
        public LibZwaveIpTransportClient _LibZwaveIpListenerTransportClient;
        public ZipApplicationLayer Dtls1ApplicationLayer
        {
            get => _dtls1ApplicationLayer;
            set
            {
                _dtls1ApplicationLayer = value;
                _dtls1TransportLayer = (IDtlsTransportLayer)_dtls1ApplicationLayer.TransportLayer;
            }
        }

        public ZipManager()
            : base()
        {
            ProvisioningList = new ProvisioningList();
            ApplicationLayer = new ZipApplicationLayer(SessionLayer, new ZipFrameLayer(), new UdpClientTransportLayer()
            {
                Listener = new UdpClientTransportListener()
            });
            Dtls1ApplicationLayer = new ZipApplicationLayer(SessionLayer,
                new ZipFrameLayer(),
                new Dtls1ClientTransportLayer
                {
                    Listener = new Dtls1ClientTransportListener()
                });
            ZipControllerDiscoverService = new ZipControllerDiscoverService(ZipConstants.Ipv6DiscoverBroadcastAddress, ZipConstants.Ipv4DiscoverBroadcastAddress, ZipConstants.UdpPortNo);
        }

        public void InitDevice(Alias id, string sourceName, string alias, string args, bool useDtls1)
        {
            _args = args;
            ZipController device = null;
            //string zipControllerIp = zipControllerIp = ZipControllerDiscoverService.
            //        Discover().
            //        FirstOrDefault();
            string zipControllerIp = sourceName;
            if (zipControllerIp != null)
            {
                _libZwaveIpTransportClient = new LibZwaveIpTransportClient();
                _LibZwaveIpListenerTransportClient = new LibZwaveIpTransportClient();
                if (useDtls1)
                {
                    var cmdLineParser = new CommandLineParser(new[] { args });
                    _pskKey = cmdLineParser.GetArgumentString("psk");
                    if (!string.IsNullOrEmpty(_pskKey))
                    {
                        _dtls1TransportLayer.Listener.Listen(
                            new Dtls1StartListenParams
                            {
                                PortNo = ZipConstants.DtlsPortNo,
                                PskKey = _pskKey,
                                IpAddress = zipControllerIp
                            });
                        _primaryUnsolicitedClients =
                            new UnsolicitedDestinationHandler<IUnsolicitedClient>(_dtls1TransportLayer.Listener, this);
                    }
                    device = Dtls1ApplicationLayer.CreateZipController();
                    device.DataSource = new SocketDataSource(zipControllerIp, ZipConstants.DtlsPortNo, args, alias) { CanReconnect = true };
                }
                else
                {
                    //device = ApplicationLayer.CreateZipController();
                    //device.DataSource = new SocketDataSource(zipControllerIp, ZIP_PORT) { CanReconnect = true };

                    //var cmdLineParser = new CommandLineParser(new[] { args });

                    _transportLayer.Listener.Listen(
                        new StartListenParams
                        {
                            PortNo = ZipConstants.UdpPortNo,
                            IpAddress = zipControllerIp,                            
                        });
                    _primaryUnsolicitedClients =
                        new UnsolicitedDestinationHandler<IUnsolicitedClient>(_transportLayer.Listener, this);
                    device = ApplicationLayer.CreateZipController();
                    device.DataSource =
                        new SocketDataSource(zipControllerIp, ZipConstants.UdpPortNo, args, alias) { CanReconnect = true };
                }

                StartCallbacksListeners(device);
                _devices.Add(id, device);
            }
        }

        public SortedList<Alias, DeviceHostClient> CutDevices()
        {
            SortedList<Alias, DeviceHostClient> ret = new SortedList<Alias, DeviceHostClient>();
            var filteredItems = _devicesHosts.ToArray();
            foreach (var item in filteredItems)
            {
                _devices[item.Key].Dispose();
                _devices.Remove(item.Key);
                ret.Add(item.Key, item.Value);
            }
            return ret;
        }

        private void StartCallbacksListeners(ZipController controller)
        {
            InitCallbacks(out Func<byte[], byte[]> dskNeededCallback, out Func<IEnumerable<ZWave.Enums.SecuritySchemes>, bool, KEXSetConfirmResult> kexSetConfirmCallback, out Action csaPinCallback);

            _networkManagementInclusionSupportToken = controller.SessionClient.ExecuteAsync(new NetworkManagementInclusionSupport(
                new byte[]
                {
                    COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V2.NODE_ADD_KEYS_REPORT.ID,
                    COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V2.NODE_ADD_DSK_REPORT.ID
                },
                dskNeededCallback, kexSetConfirmCallback, csaPinCallback, null));
        }

        public IUnsolicitedClient CreatePrimatyClient(string address, ushort portNo) => CreateUnsolicitedClient(address, portNo);

        public IUnsolicitedReceiveClient CreateSecondaryClient(string address, ushort portNo) => CreateUnsolicitedClient(address, portNo);

        public UnsolicitedClient CreateUnsolicitedClient(string address, ushort portNo)
        {
            ZipDevice virtualDevice = null;
            foreach (var device in _virtualDevices)
            {
                if (device.Value.IsUnsolicited && device.Value.IpAddress.ToString().Equals(address) && device.Value.Port == portNo)
                {
                    virtualDevice = device.Value;
                }
            }
            if (virtualDevice != null)
            {
                var unsolicitedClient = new UnsolicitedClient(virtualDevice);
                //InitCallbacks(out Func<byte[], byte[]> dskNeededCallback,
                //    out Func<IEnumerable<ZWave.Enums.SecuritySchemes>, bool, KEXSetConfirmResult> kexSetConfirmCallback,
                //    out Action csaPinCallback);
                //unsolicitedClient.AddSupportedOperation(new NetworkManagementInclusionSupport(
                //        new byte[] {
                //        COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V2.NODE_ADD_KEYS_REPORT.ID,
                //        COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V2.NODE_ADD_DSK_REPORT.ID,
                //        COMMAND_CLASS_NETWORK_MANAGEMENT_INCLUSION_V2.NODE_ADD_STATUS.ID
                //        }, dskNeededCallback, kexSetConfirmCallback, csaPinCallback, null));
                //unsolicitedClient.AddSupportedOperation(new NetworkManagementProxySupport(
                //        new byte[] { COMMAND_CLASS_NETWORK_MANAGEMENT_PROXY_V2.NODE_LIST_REPORT.ID }, null));
                return unsolicitedClient;
            }
            return null;
        }

        private void InitCallbacks(out Func<byte[], byte[]> dskNeededCallback, out Func<IEnumerable<ZWave.Enums.SecuritySchemes>, bool, KEXSetConfirmResult> kexSetConfirmCallback, out Action csaPinCallback)
        {
            //default value
            IsKexReportConfirmed = true;
            dskNeededCallback = (DSKFull) =>
            {
                if (IncludeSettings.IsInteractiveMode)
                {
                    MessageBox.Show("Enter DSK", "USER INSTRUCTION");
                    string dskStr = MessageBox.GetUserInput();
                    if (int.TryParse(dskStr, out int dskValue))
                    {
                        DskValue = new[] { (byte)(dskValue >> 8), (byte)dskValue };
                    }
                }
                return DskValue;

            };
            kexSetConfirmCallback = (requestedSchemes, isClientSideAuthRequested) =>
            {
                KEXSetConfirmResult kexSetConfirmResult = new KEXSetConfirmResult() { IsConfirmed = IsKexReportConfirmed };
                if (kexSetConfirmResult.IsConfirmed)
                {
                    kexSetConfirmResult.IsAllowedCSA = isClientSideAuthRequested;
                    kexSetConfirmResult.GrantedSchemes = new List<ZWave.Enums.SecuritySchemes>();
                    if (GrantedSchemes != null)
                    {
                        if (GrantedSchemes.Contains(SecuritySchemes.S2_ACCESS))
                            kexSetConfirmResult.GrantedSchemes.Add(ZWave.Enums.SecuritySchemes.S2_ACCESS);
                        if (GrantedSchemes.Contains(SecuritySchemes.S2_AUTHENTICATED))
                            kexSetConfirmResult.GrantedSchemes.Add(ZWave.Enums.SecuritySchemes.S2_AUTHENTICATED);
                        if (GrantedSchemes.Contains(SecuritySchemes.S2_UNAUTHENTICATED))
                            kexSetConfirmResult.GrantedSchemes.Add(ZWave.Enums.SecuritySchemes.S2_UNAUTHENTICATED);
                        if (GrantedSchemes.Contains(SecuritySchemes.S0))
                            kexSetConfirmResult.GrantedSchemes.Add(ZWave.Enums.SecuritySchemes.S0);
                    }
                }
                return kexSetConfirmResult;
            };
            csaPinCallback = () => { };
        }

        public void StopCallbacksListeners(ZipController controller)
        {
            if (_networkManagementInclusionSupportToken != null && controller != null)
            {
                controller.Cancel(_networkManagementInclusionSupportToken);
            }
        }

        public bool InitDevice(List<DeviceHost> available, KeyValuePair<Alias, ZipDeviceMappingAttribute> requiredDevice)
        {
            var deviceHost = available.FirstOrDefault(dh => dh.Alias == requiredDevice.Value.Alias && (dh.SType == InterfaceTypes.UDP || dh.SType == InterfaceTypes.DTLS1));
            //var deviceHost = new DeviceHost();
            //deviceHost.Alias = "z2";
            //deviceHost.SName = "fd00:aaaa::3";
            //deviceHost.SType = InterfaceTypes.DTLS1;
            //deviceHost.SArgs = "-psk 123456789012345678901234567890AA";

            if (deviceHost == null)
                return false;

            available.Remove(deviceHost);
            var useDtls = deviceHost.SType == InterfaceTypes.DTLS1;
            InitDevice(requiredDevice.Key, deviceHost.SName, deviceHost.Alias, deviceHost.SArgs, useDtls);
            if (!_devicesHosts.ContainsKey(requiredDevice.Key))
            {
                _devicesHosts.Add(requiredDevice.Key, new DeviceHostClient(deviceHost));
            }
            return true;
        }

        readonly object syncObject = new object();
        public ZipResult.Ping PingWithHandlers(IPAddress ip, int count, int timeoutMs, int intervalMs)
        {
            Guid ipKey = CreateGuid(ip.GetAddressBytes());
            ZipResult.Ping pingResult;
            lock (syncObject)
            {
                if (!pingResults.ContainsKey(ipKey))
                {
                    pingResults.Add(ipKey, new ZipResult.Ping());
                }
                pingResult = pingResults[ipKey];
            }
            pingResult.Reset();

            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            // Use the default Ttl value which is 128, 
            // but change the fragmentation behavior.
            options.DontFragment = true;

            // Create a buffer of 32 bytes of data to be transmitted. 
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);

            for (int i = 0; i < count; i++)
            {
                lock (syncObject)
                {
                    PingReply reply = pingSender.Send(ip, timeoutMs, buffer, options);
                    if (reply.Status == IPStatus.Success)
                    {
                        pingResult.AddReply(true, (int)reply.RoundtripTime);
                    }
                    else
                    {
                        pingResult.AddReply(false, 0);
                    }

                    if (i == 0 && pingStartHandlers.ContainsKey(ipKey))
                    {
                        pingStartHandlers[ipKey].Set();
                    }

                    if (pingCancellFlags.ContainsKey(ipKey))
                    {
                        if (pingStopHandlers.ContainsKey(ipKey))
                            pingStopHandlers[ipKey].Set();
                        break;
                    }
                }
                Thread.Sleep(intervalMs);
            }
            return pingResult;
        }

        public ZipResult.Ping Ping(IPAddress ip, int count, int timeoutMs, int intervalMs)
        {
            ZipResult.Ping pingResult = new ZipResult.Ping();

            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            // Use the default Ttl value which is 128, 
            // but change the fragmentation behavior.
            options.DontFragment = true;

            // Create a buffer of 32 bytes of data to be transmitted. 
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);

            for (int i = 0; i < count; i++)
            {
                PingReply reply = pingSender.Send(ip, timeoutMs, buffer, options);
                if (reply.Status == IPStatus.Success)
                {
                    pingResult.AddReply(true, (int)reply.RoundtripTime);
                }
                else
                {
                    pingResult.AddReply(false, 0);
                }
                Thread.Sleep(intervalMs);
            }
            return pingResult;
        }

        private static Guid CreateGuid(byte[] ipAddr)
        {
            byte[] tmpKey = new byte[16];
            for (int i = 0; i < ipAddr.Length && i < 16; i++)
            {
                tmpKey[tmpKey.Length - 1 - i] = ipAddr[ipAddr.Length - 1 - i];
            }

            Guid ipKey = new Guid(tmpKey);
            return ipKey;
        }

        Dictionary<Guid, ZipResult.Ping> pingResults = new Dictionary<Guid, ZipResult.Ping>();
        Dictionary<Guid, ManualResetEvent> pingStartHandlers = new Dictionary<Guid, ManualResetEvent>();
        Dictionary<Guid, ManualResetEvent> pingStopHandlers = new Dictionary<Guid, ManualResetEvent>();
        Dictionary<Guid, bool> pingCancellFlags = new Dictionary<Guid, bool>();

        public void StartPing(IPAddress ipAddress, int count, int timeoutMs, int intervalMs)
        {
            Guid ipKey = CreateGuid(ipAddress.GetAddressBytes());
            if (pingStartHandlers.ContainsKey(ipKey))
            {
                pingStartHandlers[ipKey].Reset();
            }
            else
            {
                pingStartHandlers.Add(ipKey, new ManualResetEvent(false));
            }

            if (pingStopHandlers.ContainsKey(ipKey))
            {
                pingStopHandlers[ipKey].Reset();
            }
            else
            {
                pingStopHandlers.Add(ipKey, new ManualResetEvent(false));
            }

            if (pingCancellFlags.ContainsKey(ipKey))
                pingCancellFlags.Remove(ipKey);

            ThreadPool.QueueUserWorkItem(new WaitCallback((state) =>
            {
                PingWithHandlers(ipAddress, count, timeoutMs, intervalMs);
            }));
        }

        public ZipResult.Ping StopPing(IPAddress ipAddress)
        {
            ZipResult.Ping ret = new ZipResult.Ping();
            Guid ipKey = CreateGuid(ipAddress.GetAddressBytes());
            if (!pingCancellFlags.ContainsKey(ipKey))
                pingCancellFlags.Add(ipKey, true);
            if (pingStopHandlers.ContainsKey(ipKey))
            {
                pingStopHandlers[ipKey].WaitOne();
            }
            if (pingResults.ContainsKey(ipKey))
            {
                ret = pingResults[ipKey];
            }
            return ret;
        }

        public bool WaitForPingStart(IPAddress ipAddress, int timeoutMs)
        {
            Guid ipKey = CreateGuid(ipAddress.GetAddressBytes());
            if (pingStartHandlers.ContainsKey(ipKey))
                return pingStartHandlers[ipKey].WaitOne(timeoutMs);
            else
                return false;
        }

        public void ResetAllPing()
        {
            foreach (var item in pingStartHandlers)
            {
                if (!pingCancellFlags.ContainsKey(item.Key))
                    pingCancellFlags.Add(item.Key, true);
                item.Value.Set();
                item.Value.Close();
            }
            pingStartHandlers.Clear();

            WaitHandle.WaitAll(pingStopHandlers.Values.ToArray());
            foreach (var item in pingStopHandlers)
            {
                item.Value.Close();
            }
            pingStopHandlers.Clear();
            pingCancellFlags.Clear();
            pingResults.Clear();
        }

        public void FinalizeDevice(ZipDevice device)
        {
            device.Dispose();
        }

        public void FinalizeDevice(Alias deviceAlias)
        {
            FinalizeAllVirtualDevices();
            lock (this)
            {
                if (_devices.ContainsKey(deviceAlias))
                {
                    StopCallbacksListeners(_devices[deviceAlias]);
                    _devices[deviceAlias].Dispose();
                    _devices.Remove(deviceAlias);
                }
            }
        }

        public void FinalizeAllVirtualDevices()
        {
            lock (this)
            {
                foreach (var item in _virtualDevices)
                {
                    item.Value.Dispose();
                }
                _virtualDevices.Clear();
            }
        }

        public void FinalizeVirtualDevice(Alias deviceAlias)
        {
            lock (this)
            {
                if (_virtualDevices.ContainsKey(deviceAlias))
                {
                    _virtualDevices[deviceAlias].Dispose();
                    _virtualDevices.Remove(deviceAlias);
                }
            }
        }

        public ZipDevice GetDevice(Alias deviceAlias)
        {
            lock (this)
            {
                if (_devices.Keys.Contains(deviceAlias))
                    return _devices[deviceAlias];
                else
                    return GetVirtualDevice(deviceAlias);
            }
        }

        public bool ConnectDevices(Alias[] devicesNumbers)
        {
            foreach (var item in devicesNumbers)
            {
                ZipDevice device = GetDevice(item);
                if (device == null || device.Connect() != ZWave.Enums.CommunicationStatuses.Done)
                    return false;
            }
            return true;
        }

        public void NewUnsolicitedDevice(Alias virtualNumber, IPEndPoint ipEndPoint, bool useDtls1)
        {
            NewUnsolicitedDevice(virtualNumber, ipEndPoint.Address, useDtls1);
        }

        public void NewUnsolicitedDevice(Alias virtualNumber, IPAddress ipAddress, bool useDtls1)
        {
            NewVirtualDevice(virtualNumber, ipAddress, useDtls1, true);
        }

        public void FinalizeDevices()
        {
            var keys = _devices.Keys.ToArray();
            foreach (var key in keys)
            {
                FinalizeDevice(key);
            }
        }

        public void ReconnectZipController(Alias zipController)
        {
            ZipDevice zipDevice = GetDevice(zipController);
            ZipController ZipController = _devices[zipController];
            StopCallbacksListeners(ZipController);
            string zipControllerIp = ZipController.IpAddress.ToString();
            _dtls1TransportLayer.Listener.Stop();
            zipDevice.TransportClient.Disconnect();
            zipDevice.Disconnect();
            _primaryUnsolicitedClients._clients.Clear();
            FinalizeDevices();
            FinalizeAllVirtualDevices();
            _devices.Clear();

            if (zipControllerIp != null)
            {
                if (!string.IsNullOrEmpty(_pskKey))
                {
                    _dtls1TransportLayer.Listener.Listen(
                        new Dtls1StartListenParams
                        {
                            PortNo = ZipConstants.DtlsPortNo,
                            PskKey = _pskKey,
                            IpAddress = zipControllerIp
                        });
                    _primaryUnsolicitedClients =
                        new UnsolicitedDestinationHandler<IUnsolicitedClient>(_dtls1TransportLayer.Listener, this);
                }
                ZipController = Dtls1ApplicationLayer.CreateZipController();
                ZipController.DataSource = new SocketDataSource(zipControllerIp, ZipConstants.DtlsPortNo, _pskKey, zipControllerIp)
                { CanReconnect = true };
                ZipController.Connect(ZipController.DataSource);


            }
            else
            {
                var cmdLineParser = new CommandLineParser(new[] { _pskKey });

                _transportLayer.Listener.Listen(
                    new StartListenParams
                    {
                        PortNo = ZipConstants.UdpPortNo,
                        IpAddress = zipControllerIp
                    });
                _primaryUnsolicitedClients = new UnsolicitedDestinationHandler<IUnsolicitedClient>(_transportLayer.Listener, this);
                ZipController = ApplicationLayer.CreateZipController();
                ZipController.DataSource = new SocketDataSource(zipControllerIp, ZipConstants.UdpPortNo, _pskKey, zipControllerIp)
                { CanReconnect = true };
            }

            StartCallbacksListeners(ZipController);
            _devices.Add(zipController, ZipController);
        }

        public void NewVirtualDevice(Alias virtualNumber, IPAddress ipAddress, bool useDtls1, bool isUnsolicited = false)
        {
            ZipDevice device = null;
            SocketDataSource socketDataSource = null;
            if (useDtls1)
            {
                device = Dtls1ApplicationLayer.CreateZipDevice();
                socketDataSource = new SocketDataSource(ipAddress.ToString(), ZipConstants.DtlsPortNo, _args) { CanReconnect = true };
                device.IsUnsolicited = isUnsolicited;
                device.DataSource = socketDataSource;
            }
            else
            {
                device = ApplicationLayer.CreateZipDevice();
                socketDataSource = new SocketDataSource(ipAddress.ToString(), ZipConstants.UdpPortNo) { CanReconnect = true };
                device.IsUnsolicited = isUnsolicited;
                device.DataSource = socketDataSource;
                ((UdpClientTransportLayer)_transportLayer).RegisterListeningClient(device.TransportClient);

            }

            if (!isUnsolicited)
                device.Connect();

            lock (this)
            {
                if (_virtualDevices.ContainsKey(virtualNumber))
                {
                    _virtualDevices[virtualNumber] = device;
                }
                else
                {
                    _virtualDevices.Add(virtualNumber, device);
                }
            }
        }

        public void ReconnectUnsolicited(Alias deviceAlias)
        {
            lock (this)
            {
                if (_virtualDevices.ContainsKey(deviceAlias))
                {
                    _virtualDevices[deviceAlias].Disconnect();
                }
                else if (_devices.ContainsKey(deviceAlias))
                {
                    _devices[deviceAlias].Disconnect();
                    _dtls1TransportLayer.Listener.Stop();
                    //_dtls1TransportLayer.Listener.Listen(
                    //       new Dtls1StartListenParams
                    //       {
                    //           PortNo = _unsolicitedPortNo,
                    //           PskKey = _pskKey
                    //       });
                }
            }
        }

        public void ReconnectVirtualDevice(Alias deviceAlias)
        {
            lock (this)
            {
                if (_virtualDevices.ContainsKey(deviceAlias))
                {
                    _virtualDevices[deviceAlias].Disconnect();
                    _virtualDevices[deviceAlias].Connect();
                }
                else if (_devices.ContainsKey(deviceAlias))
                {
                    _devices[deviceAlias].Disconnect();
                    _dtls1TransportLayer.Listener.Stop();
                    //_dtls1TransportLayer.Listener.Listen(
                    //       new Dtls1StartListenParams
                    //       {
                    //           PortNo = _unsolicitedPortNo,
                    //           PskKey = _pskKey
                    //       });
                    _devices[deviceAlias].Connect();
                }
            }
        }

        public void DisconnectDevice(Alias deviceAlias)
        {
            lock (this)
            {
                if (_virtualDevices.ContainsKey(deviceAlias))
                {
                    _virtualDevices[deviceAlias].Disconnect();
                }
                else if (_devices.ContainsKey(deviceAlias))
                {
                    _devices[deviceAlias].Disconnect();
                    _dtls1TransportLayer.Listener.Stop();
                }
            }
        }

        private ZipDevice GetVirtualDevice(Alias virtualNumber)
        {
            lock (this)
            {
                if (_virtualDevices.Keys.Contains(virtualNumber))
                    return _virtualDevices[virtualNumber];
                else
                    return null;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (Dtls1ApplicationLayer != null)
            {
                var disposable = _dtls1TransportLayer as IDisposable;
                if (disposable != null)
                    disposable.Dispose();
            }
        }

        #endregion

        public void RegisterSecondaryUnsolicitedListener(Alias zipControllerNumber, int port, string pskValue)
        {
            ZipDevice zipDevice = GetDevice(zipControllerNumber);
            if (zipDevice != null && zipDevice.DataSource != null && ZipConstants.DtlsPortNo != port && port != 0)
            {
                var dtlsTransportlayer = Dtls1ApplicationLayer.TransportLayer as Dtls1ClientTransportLayer;
                if (dtlsTransportlayer.ListenerSecond == null)
                {
                    dtlsTransportlayer.ListenerSecond = new Dtls1ClientTransportListener();
                }
                else
                {
                    _dtls1TransportLayer.ListenerSecond.Stop();
                }
                _unsolicitedPortSecondNo = (ushort)port;
                _dtls1TransportLayer.ListenerSecond.Listen(
                    new Dtls1StartListenParams
                    {
                        PortNo = _unsolicitedPortSecondNo,
                        PskKey = pskValue
                    });
            }
        }

        public void CloseSecondaryUnsolicitedListener()
        {
            var dtlsTransportlayer = Dtls1ApplicationLayer.TransportLayer as Dtls1ClientTransportLayer;
            if (dtlsTransportlayer.ListenerSecond != null)
            {
                _dtls1TransportLayer.ListenerSecond.Stop();
            }
        }
    }

    /// <summary>
    /// Helper class to contain functions of use when working with mulitcast interfaces.
    /// </summary>
    [ComVisible(false)]
    public class Utility
    {
        //Pri1: Review for Exception Handling & Event Logging
        #region Private Constants
        private const short AF_INET = 2;
        private const short AF_INET6 = 23;
        private const int SIO_ROUTING_INTERFACE_QUERY = unchecked((int)(0x40000000 | 0x80000000 | 0x08000000 | 20));
        private const string MulticastIPAddress = "233.45.17.171";  // Use the Pipecleaner address
        private const short port = 5004;
        #endregion
        #region members
        private static IPAddress externalInterface = null;
        public static IPAddress multicastIP;
        #endregion
        #region Private Structures
        /// <summary>
        /// DotNet definition of sockaddr_in, the Winsock2 structure that roughly corresponds to an EndPoint structure from System.Net.  Used to interop with Winsock2,
        /// in this case to call IOControl for SIO_ROUTING_INTERFACE_QUERY.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private class sockaddr_in
        {
            public short sin_family = AF_INET;
            public short sin_port = 0;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] sin_addr = new Byte[4];
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] sin_zero = new Byte[8];
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private class sockaddr_in6
        {
            public short sin_family = AF_INET6;
            public short sin_port = 0;
            public uint sin6_flowinfo = 0;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] sin_addr = new Byte[16];
            public uint sin6_scope_id = 0;

        }
        #endregion
        #region Public Static Methods

        /// <summary>
        /// Find the interface we should be binding to to receive a multicast stream by using Socket.IOControl to call
        /// SIO_ROUTING_INTEFACE_QUERY (see WSAIoctl in Winsock2 documentation) passing in a known multicast address.
        /// </summary>
        /// <returns>IPAddress containing the local multicast interface</returns>
        /// <example>
        /// IPAddress ifAddress = MulticastInterface.GetLocalMulticastInterface();
        /// </example>
        public static IPAddress GetLocalMulticastInterface()
        {
            if (externalInterface != null)
            {
                return externalInterface;
            }
            else if (multicastIP != null)
            {
                return GetLocalRoutingInterface(multicastIP);
            }
            else
            {
                return null;
            }
        }

        //Removed, replace this functionality by using bind(IPAddress.Any, ... or IPAddress.IPv6Any);
        public static IPAddress GetLocalRoutingInterface(IPAddress ipAddress)
        {
            Socket sock = null;
            IntPtr ptrInAddr = IntPtr.Zero;
            IntPtr ptrOutAddr = IntPtr.Zero;

            if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
            {
                sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                sockaddr_in inAddr = new sockaddr_in();
                sockaddr_in outAddr = new sockaddr_in();

                try
                {
                    ipAddress.GetAddressBytes().CopyTo(inAddr.sin_addr, 0);

                    // create a sockaddr_in function for our destination IP address
                    inAddr.sin_port = IPAddress.HostToNetworkOrder(port);

                    // create an block of unmanaged memory for use by Marshal.StructureToPtr.  We seem to need to do this, even though
                    // StructureToPtr will go ahead and release/reallocate the memory
                    ptrInAddr = Marshal.AllocCoTaskMem(Marshal.SizeOf(inAddr));

                    // Copy inAddr from managed to unmanaged
                    Marshal.StructureToPtr(inAddr, ptrInAddr, false);

                    // Create a managed byte array to hold the structure, but in byte array form
                    byte[] byteInAddr = new byte[Marshal.SizeOf(inAddr)];

                    // Copy the structure from unmanaged ptr into managed byte array
                    Marshal.Copy(ptrInAddr, byteInAddr, 0, byteInAddr.Length);

                    // Create a second managed byte array to hold the output sockaddr_in structure
                    byte[] byteOutAddr = new byte[Marshal.SizeOf(inAddr)];

                    // Make the call to IOControl, asking for the Interface we should use if we want to route a packet to inAddr
                    sock.IOControl(
                        SIO_ROUTING_INTERFACE_QUERY,
                        byteInAddr,
                        byteOutAddr
                        );

                    // create the memory placeholder for our local interface

                    // Copy the results from the byteOutAddr into an unmanaged pointer
                    ptrOutAddr = Marshal.AllocCoTaskMem(Marshal.SizeOf(outAddr));
                    Marshal.Copy(byteOutAddr, 0, ptrOutAddr, byteOutAddr.Length);

                    // Copy the data from the unmanaged pointer to the ourAddr structure
                    Marshal.PtrToStructure(ptrOutAddr, outAddr);
                }
                catch (SocketException ex)
                {
                    // Perhaps there were no interfaces present, AKA No Network Adapters enabled/installed and connected to media (wired or wireless)
                    ex.Message._DLOG();
                    //EventLog.WriteEntry("RtpSession", se.ToString(), EventLogEntryType.Warning, 99);

                }
                finally
                {
                    // Release the socket
                    sock = null;

                    Marshal.FreeCoTaskMem(ptrInAddr);
                    Marshal.FreeCoTaskMem(ptrOutAddr);
                }

                // Return an IPAddress structure that is initialized with the value of the IP address contained in the outAddr structure
                if (outAddr != null)
                {
                    int len = outAddr.sin_addr.Length;

                    // Have to convert the byte[] to a uint.  It turns out that the IPAddress ctor won't create an IPv4 address from four bytes, it only uses the byte[] ctor for 16 byte IPv6 construction?!?
                    uint ipAsInt = 0;

                    for (int i = len; i > 0; i--)
                    {
                        ipAsInt = ipAsInt * 256 + outAddr.sin_addr[i - 1];
                    }
                    externalInterface = new IPAddress(ipAsInt);
                    return (new IPAddress(ipAsInt));
                }
                else
                {
                    return null;
                }

            }

            if (Socket.OSSupportsIPv6 && ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                sock = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                sockaddr_in6 inAddr = new sockaddr_in6();
                sockaddr_in6 outAddr = new sockaddr_in6();

                try
                {
                    ipAddress.GetAddressBytes().CopyTo(inAddr.sin_addr, 0);

                    // create a sockaddr_in function for our destination IP address
                    inAddr.sin_port = IPAddress.HostToNetworkOrder(port);

                    // create an block of unmanaged memory for use by Marshal.StructureToPtr.  We seem to need to do this, even though
                    // StructureToPtr will go ahead and release/reallocate the memory
                    ptrInAddr = Marshal.AllocCoTaskMem(Marshal.SizeOf(inAddr));

                    // Copy inAddr from managed to unmanaged
                    Marshal.StructureToPtr(inAddr, ptrInAddr, false);

                    // Create a managed byte array to hold the structure, but in byte array form
                    byte[] byteInAddr = new byte[Marshal.SizeOf(inAddr)];

                    // Copy the structure from unmanaged ptr into managed byte array
                    Marshal.Copy(ptrInAddr, byteInAddr, 0, byteInAddr.Length);

                    // Create a second managed byte array to hold the output sockaddr_in structure
                    byte[] byteOutAddr = new byte[Marshal.SizeOf(inAddr)];

                    // Make the call to IOControl, asking for the Interface we should use if we want to route a packet to inAddr

                    sock.IOControl(
                        SIO_ROUTING_INTERFACE_QUERY,
                        byteInAddr,
                        byteOutAddr
                        );

                    // create the memory placeholder for our local interface
                    // Copy the results from the byteOutAddr into an unmanaged pointer
                    ptrOutAddr = Marshal.AllocCoTaskMem(Marshal.SizeOf(outAddr));
                    Marshal.Copy(byteOutAddr, 0, ptrOutAddr, byteOutAddr.Length);

                    // Copy the data from the unmanaged pointer to the ourAddr structure
                    Marshal.PtrToStructure(ptrOutAddr, outAddr);
                }
                catch (SocketException ex)
                {
                    // Perhaps there were no interfaces present, AKA No Network Adapters enabled/installed and connected to media (wired or wireless)
                    ex.Message._DLOG();
                    //EventLog.WriteEntry("RtpSession", se.ToString(), EventLogEntryType.Warning, 99);
                }
                finally
                {
                    // Release the socket
                    sock = null;

                    Marshal.FreeCoTaskMem(ptrInAddr);
                    Marshal.FreeCoTaskMem(ptrOutAddr);
                }

                // Return an IPAddress structure that is initialized with the value of the IP address contained in the outAddr structure
                if (outAddr != null)
                {
                    externalInterface = new IPAddress(outAddr.sin_addr, outAddr.sin6_scope_id);
                    return (new IPAddress(outAddr.sin_addr, outAddr.sin6_scope_id));
                }
                else
                {
                    return null;
                }

            }
            return null;

        }

        /// <summary>
        /// Checks if the IP is a multicast IP
        /// </summary>
        /// <remarks>
        /// Works for both IPv4 and IPv6
        /// </remarks>
        public static bool IsMulticast(IPAddress ipAddress)
        {
            bool isMulticast;

            if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // In IPv6 Multicast addresses first byte is 0xFF
                byte[] ipv6Bytes = ipAddress.GetAddressBytes();
                isMulticast = (ipv6Bytes[0] == 0xff);
            }
            else // IPv4
            {
                Debug.Assert(ipAddress.AddressFamily == AddressFamily.InterNetwork);

                // In IPv4 Multicast addresses first byte is between 224 and 239
                byte[] addressBytes = ipAddress.GetAddressBytes();
                isMulticast = (addressBytes[0] >= 224) && (addressBytes[0] <= 239);
            }

            return isMulticast;
        }

        /// <summary>
        /// Checks if the IP is a multicast IP
        /// </summary>
        /// <remarks>
        /// Works for both IPv4 and IPv6.  Note that this overload is slightly less effecient than the
        /// alternative, as only the IPAddress is needed to determine if an IP is multicast.
        /// </remarks>
        public static bool IsMulticast(IPEndPoint ipEndPoint)
        {
            return IsMulticast(ipEndPoint.Address);
        }

        #endregion
    }
}
