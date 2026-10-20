// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Utils;
using Zats.Services;
using Zats.Services.Constants;
using Zats.Services.MeshNetworkRacks;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.Enums;
using ZWave.Exceptions;
using ZWave.Layers;
using ZWave.ZnifferApplication;
#if NETCOREAPP
using System.Text.RegularExpressions;
#endif

namespace ZatsTestConsole
{
    public class DeviceHostsDetector : IDisposable
    {
        private const int ADMIN_CONSOLE_PORT = 4902;

        private BasicApplicationLayer _basicApp;
        private ZnifferApplicationLayer _znifferApp;
        private BasicApplicationLayer _tcpApp;
        private List<DeviceHostClient> _deviceHostClients = new List<DeviceHostClient>();
        /// <summary>
        /// Dictionary of Alpha boards by DSK to keep track of programmed boards.
        /// Used as a workaround to match pairs SName/PName by DSK because no other API available.
        /// </summary>
        private Dictionary<ushort, string> _alphaBoardDetectorByDSK = new Dictionary<ushort, string>();
        private IProgrammingProvider _alphaProgrammer;
        private IProgrammingProvider _gammaProgrammer;
        private IFileSystemManager _fileSystemManager;
        private IPortNamesGetter _portNamesGetter;
        private IUdpDevicesDiscoveryService<JlinkDeviceInfo> _udpJLinkDiscoveryService;
        private CmdLineSettings _cmdLineSettings;
        public DeviceHostsDetector(
            CmdLineSettings cmdLineSettings,
            BasicApplicationLayer basicApp,
            ZnifferApplicationLayer znifferApp,
            BasicApplicationLayer tcpApp,
            IPortNamesGetter portNamesGetter,
            IProgrammingProvider alphaProgrammer,
            IProgrammingProvider gammaProgrammer,
            IFileSystemManager fileSystemManager,
            IUdpDevicesDiscoveryService<JlinkDeviceInfo> udpJLinkDiscoveryService)
        {
            _cmdLineSettings = cmdLineSettings;
            _basicApp = basicApp;
            _znifferApp = znifferApp;
            _tcpApp = tcpApp;
            _portNamesGetter = portNamesGetter;
            _alphaProgrammer = alphaProgrammer;
            _gammaProgrammer = gammaProgrammer;
            _fileSystemManager = fileSystemManager;
            _udpJLinkDiscoveryService = udpJLinkDiscoveryService;
        }

        public void RunPassiveDetect(Profile profile)
        {
            _deviceHostClients.Clear();

            DetectDevicesWithSerialApi(false);
            DetectDevicesWithSerialApi(true);

            var gammaBoards = _portNamesGetter.GetBoardLinks(_gammaProgrammer);
            _deviceHostClients.ForEach(x => x.Host.JName = gammaBoards?.FirstOrDefault(y => y.Item1 == x.Host.SName)?.Item2);

            var ports = _portNamesGetter.GetPortNames().Where(x => !_deviceHostClients.Any(y => y.Host.SName == x) && !_deviceHostClients.Any(y => y.Host.PName == x));
            if (!_cmdLineSettings.IsNoZnfDevice)
            {
                var znifferDhc = DetectZnifferApi(ports);
                if (znifferDhc != null)
                {
                    profile.ZName = znifferDhc.Host.SName;
                }
            }

            if (profile.DeviceHosts == null)
            {
                profile.DeviceHosts = new System.Collections.ObjectModel.ObservableCollection<DeviceHost>();
            }

            foreach (var dhc in _deviceHostClients)
            {
                dhc.SClient?.Dispose();
                dhc.SClient = null;
                dhc.PClient?.Dispose();
                dhc.PClient = null;
                profile.DeviceHosts.Add(dhc.Host);
            }
        }

        public MeshRouterTopologyBuilder DetectConnectedDevices(Profile profile, IEnumerable<string> testBeds, IEnumerable<string> jlinkBoardsSerials, MeshRouterDeviceInfo routerInfo = null)
        {
            MeshRouterTopologyBuilder meshRouterTopologyBuilder = null;
            _deviceHostClients.Clear();
            var portsJL = _portNamesGetter.GetBoardLinks(_gammaProgrammer);
            InitGammaBoards(portsJL, testBeds, jlinkBoardsSerials, profile.ZName);
            IEnumerable<string> ports = _portNamesGetter.GetPortNames();
            if (ports != null && !string.IsNullOrWhiteSpace(profile.ZName))
            {
                ports = ports
                    .Except(portsJL
                        .Where(x => x.Item1 != null)
                        .Select(x => x.Item1))
                    .Except(new[] { profile.ZName });
            }
            if (_alphaProgrammer != null)
            {
                InitAlphaBoards(ports);
            }
            ProgramGammaSamples();
            ProgramAlphaSamples(false);
            DetectDevicesWithSerialApi(false);
            ProgramAlphaSamples(true);
            DetectDevicesWithSerialApi(true);

            if (routerInfo != null)
            {
                meshRouterTopologyBuilder = DetectMeshRouters(profile, routerInfo);
            }

            EraseGammaSamples();
            if (_alphaProgrammer != null)
            {
                EraseAlphaSamples();
            }

            if (!_cmdLineSettings.IsNoZnfDevice)
            {
                InitProfileZniffer(profile, ports);
            }

            if (profile.DeviceHosts == null)
            {
                profile.DeviceHosts = new System.Collections.ObjectModel.ObservableCollection<DeviceHost>();
            }
            var devicesToRemove = new List<DeviceHostClient>();
            foreach (var dhc in _deviceHostClients)
            {
                dhc.SClient?.Dispose();
                dhc.SClient = null;
                dhc.PClient?.Dispose();
                dhc.PClient = null;
                // Let's check for duplicate J-Link devices here (connected via Ethernet + usb).
                if (dhc.Host.SType == InterfaceTypes.TCP && _deviceHostClients.Count(x => x.Host.JName == dhc.Host.JName) > 1)
                {
                    devicesToRemove.Add(dhc);
                    continue;
                }
                profile.DeviceHosts.Add(dhc.Host);
            }
            _deviceHostClients.RemoveAll(x => devicesToRemove.Contains(x));

            return meshRouterTopologyBuilder;
        }

        private void InitProfileZniffer(Profile profile, IEnumerable<string> ports)
        {
            if (string.IsNullOrEmpty(profile.ZName))
            {
                ports = _portNamesGetter.GetPortNames().Where(x => !_deviceHostClients.Any(y => y.Host.SName == x) && !_deviceHostClients.Any(y => y.Host.PName == x));
                var znifferDhc = DetectZnifferApi(ports);
                if (znifferDhc == null)
                {
                    znifferDhc = _deviceHostClients.FirstOrDefault(x => x.Host.ChipSeries == ChipSeries.ZW050x.ToString() && !x.HasJName && x.HasSName && x.HasPName);
                    if (znifferDhc != null)
                    {
                        var location = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources");
                        var hexName = znifferDhc.Host.IsUZB ? "s5u.hex" : "s5.hex";
                        var znifferSamplePath = Path.Combine(location, hexName);
                        _alphaProgrammer.ProgramSample(znifferDhc, ZWaveRegions.REGION_DEFAULT, znifferSamplePath, false, null, null);
                    }
                    else
                    {
                        var comparer = new OrderComparer<string>(new[]
                        {
                            ChipSeries.ZGM130S_BRD4207A,
                            ChipSeries.ZGM130S_BRD4202A,
                            ChipSeries.ZGM230S_BRD4205B,
                            ChipSeries.ZG23_BRD4204D,
                            ChipSeries.ZG23_BRD4204C,
                            ChipSeries.ZG23_BRD4204A,
                            ChipSeries.ZG23_BRD4210A,
                            ChipSeries.ZGM230S_BRD4205A,
                            }.Select(x => x.ToString()));
                        znifferDhc = _deviceHostClients.Where(x => x.Host.SType == InterfaceTypes.TCP && x.Host.ChipSeries != null).OrderBy(x => x.HasPName).OrderBy(x => x.Host.ChipSeries, comparer).FirstOrDefault();
                        if (znifferDhc != null)
                        {

                        }
                    }
                }
                if (znifferDhc != null)
                {
                    znifferDhc.SClient?.Dispose();
                    znifferDhc.PClient?.Dispose();
                    _deviceHostClients.Remove(znifferDhc);
                    profile.ZName = znifferDhc.Host.SName;
                }
            }
        }

        public MeshRouterTopologyBuilder DetectMeshRouters(Profile activeProfile, MeshRouterDeviceInfo routerInfo)
        {
            DeviceHost remoteDevice = null;
            do
            {
                remoteDevice = activeProfile.DeviceHosts.FirstOrDefault(x => x.SType == InterfaceTypes.HTTP && (x.RApiType == RemoteApiTypes.WebAppliance || x.RApiType == RemoteApiTypes.MeshRouter));
                if (remoteDevice != null)
                    activeProfile.DeviceHosts.Remove(remoteDevice);
            }
            while (remoteDevice != null && activeProfile.DeviceHosts.Count > 0);

            var meshRouterHost = new DeviceHost
            {
                RApiType = RemoteApiTypes.MeshRouter,
                SType = InterfaceTypes.HTTP,
                JName = routerInfo.SerialNo,
                SName = routerInfo.IPAddress.ToString()
            };

            var fitDHClients = _deviceHostClients.
                Where(dch => dch.SClient != null &&
                    (dch.Host.SType == InterfaceTypes.COM || dch.Host.SType == InterfaceTypes.TCP)).
                ToArray();

            var meshRouterTopologyBuilder = new MeshRouterTopologyBuilder(routerInfo);
            if (meshRouterTopologyBuilder.RunDetect(fitDHClients, _basicApp, _tcpApp))
            {
                activeProfile.DeviceHosts.Add(meshRouterHost);
            }

            return meshRouterTopologyBuilder;
        }

        private void InitGammaBoards(Tuple<string, string>[] portsJL, IEnumerable<string> testBeds, IEnumerable<string> jlinkIdsToAdd, string znifferName)
        {
            // Private IPv4 ranges (RFC 1918): 10.0.0.0/8, 172.16.0.0/12 and 192.168.0.0/16.
            string[] ipFilter = new[] { "10.", "192.168." }
                .Concat(Enumerable.Range(16, 16).Select(x => $"172.{x}."))
                .ToArray();
            var discoveredItems = new JlinkDeviceInfo[1]; // Placeholder Array, will be replaced below.
            if (!string.IsNullOrWhiteSpace(_cmdLineSettings.IPFilter))
            {
                ipFilter = _cmdLineSettings.IPFilter.Split(',', ';');
                // This loop only works if the ip and serialno given as arguments match in the same order 
                var jlink_devices = new List<JlinkDeviceInfo>();
                if (jlinkIdsToAdd == null)
                {
                    throw new InvalidOperationException("-ipf/--ip-filter list was provided but no matching -jlf/--jlink-filter list was found");
                }
                if (jlinkIdsToAdd.ToArray().Length != ipFilter.Length)
                {
                    throw new InvalidOperationException("-ipf/--ip-filter provided list does not match the size of -jlf/--jlink-filter provided list");
                }
                for (int i = 0; i < ipFilter.Length; i++)
                {
                    jlink_devices.Add(new JlinkDeviceInfo(jlinkIdsToAdd.ToList()[i], null, IPAddress.Parse(ipFilter[i]), string.Empty, true));
                }
                discoveredItems = jlink_devices.ToArray();
            }
            else
            {
                discoveredItems = _udpJLinkDiscoveryService.DiscoverTcpDevices();
            }

            discoveredItems = discoveredItems.Where(x => ipFilter.Any(y => x.IPAddress.ToString().StartsWith(y))).ToArray();
            if (jlinkIdsToAdd != null)
            {
                discoveredItems = discoveredItems.Where(x => jlinkIdsToAdd.Any(y => x.SerialNo.EndsWith(y))).ToArray();
            }
            if (portsJL != null)
            {
                discoveredItems = discoveredItems
                    .Concat(portsJL.Where(x => !discoveredItems.Any(y => y.SerialNo == x.Item2))
                        .Select(x => new JlinkDeviceInfo(x.Item2, x.Item1, null, null, true)))
                    .ToArray();
            }
            if (!string.IsNullOrWhiteSpace(znifferName))
            {
                discoveredItems = discoveredItems.Where(x => x.SName != znifferName).ToArray();
            }
            var tbName = discoveredItems.GroupBy(x => x.TestBedName).OrderByDescending(x => x.Count()).FirstOrDefault()?.Key;
            if (string.IsNullOrWhiteSpace(tbName))
            {
                tbName = Dns.GetHostName();
            }
            if (tbName.Length > 10)
            {
                tbName = tbName.Substring(0, 5) + tbName.Substring(tbName.Length - 5, 5);
            }
            if (testBeds == null || testBeds.Count() == 1 && testBeds.First().ToUpper() == "ALL")
            {
                var selectedItems = discoveredItems.Where(x => string.IsNullOrWhiteSpace(x.Nickname) ||
                !x.Nickname.StartsWith("Z1"));
                ProgramManipulatorAndDetectPairs(tbName, selectedItems, discoveredItems);
                foreach (var item in GetTestBedItem(discoveredItems))
                {
                    _deviceHostClients.Add(item);
                }
            }
            else if (testBeds.Count() == 1 && testBeds.First().ToUpper() == "ALL-NEW")
            {
                ProgramManipulatorAndDetectPairs(tbName, discoveredItems, discoveredItems);
                foreach (var item in GetTestBedItem(discoveredItems))
                {
                    _deviceHostClients.Add(item);
                }
            }
            else
            {
                tbName = testBeds.First();
                var selectedItems = discoveredItems.Where(x => string.IsNullOrWhiteSpace(x.Nickname) && x.IPAddress == null ||
                    !string.IsNullOrWhiteSpace(x.Nickname)
                    && x.Nickname.StartsWith("Z1")
                    && testBeds.Any(y => x.Nickname.Contains($".{y.ToUpper()}")))
                    .OrderBy(x => x.TestBedName, new OrderComparer<string>(testBeds));

                ProgramManipulatorAndDetectPairs(tbName, selectedItems, selectedItems);
                foreach (var item in GetTestBedItem(selectedItems))
                {
                    _deviceHostClients.Add(item);
                }
            }
        }

        public BasicApplicationLayer GetAppLayer(JlinkDeviceInfo wstkItem)
        {
            return (wstkItem.IPAddress != null ? _tcpApp : _basicApp);
        }

        private void ProgramManipulatorAndDetectPairs(string testBedName, IEnumerable<JlinkDeviceInfo> selectedItems, IEnumerable<JlinkDeviceInfo> discoveredItems)
        {
            Parallel.ForEach(discoveredItems, (x) =>
            {
                var dhc = new DeviceHostClient(new DeviceHost { SName = x.SName, JName = x.SerialNo });
                try
                {
                    var chipRes = _gammaProgrammer.GetChipSeries(dhc);
                    x.ChipSeries = chipRes.ToString();
                    if (selectedItems.Contains(x) && !new[]
                    {
                        ChipSeries.ZG14_BRD4201A, ChipSeries.ZG14_BRD4206A, ChipSeries.ZG14_BRD4208A
                    }.Select(y => y.ToString()).Contains(x.ChipSeries))
                    {
                        var firmware = _fileSystemManager.GetLatestFirmwareFilePath(Libraries.Manipulator, chipRes, null, null, ".hex");
                        _gammaProgrammer.ProgramSample(dhc, null, firmware, false, null, null);
                    }
                }
                catch (Exception ex)
                {
                    $"{ex.Message}"._DLOG();
                }
            });

            foreach (var wstk in selectedItems)
            {
                wstk.LinkItem = null;
                wstk.TestRole = TestRoles.Unknown;
            }

            int testDeviceIndex = 0;
            var selectedItemsArray = selectedItems.ToArray();
            foreach (var wstkA in selectedItems)
            {
                if (wstkA.TestRole == TestRoles.BoundDevice)
                {
                    continue;
                }
                if (wstkA.Index <= 0)
                {
                    testDeviceIndex++;
                    while (discoveredItems.Where(x => x.TestBedName == testBedName).Any(x => x.Index == testDeviceIndex))
                    {
                        testDeviceIndex++;
                    }
                    wstkA.Index = testDeviceIndex;
                }
                if (Enum.TryParse(wstkA.ChipSeries, out ChipTypes chSeriesA))
                {
                    //_mainViewModel.ResetAppLayers();
                    using (var deviceA = GetAppLayer(wstkA).CreateTestInterfaceDevice((ChipTypes)chSeriesA))
                    {
                        if (deviceA.Connect(wstkA.DataSource) == CommunicationStatuses.Done)
                        {
                            try
                            {
                                PortMaskTypes maskType = PortMaskTypes.ButtonsAndLeds;
                                wstkA.TestRole = TestRoles.SingleDevice;
                                wstkA.Nickname = JlinkDeviceInfo.GetNickname(testBedName, wstkA);
                                deviceA.InitPinsInput(chSeriesA, PortMaskTypes.Manipulator, true);
                                if (!deviceA.ComparePins(chSeriesA, PortMaskTypes.Manipulator, false))
                                {
                                    if (wstkA.IPAddress != null)
                                    {
                                        deviceA.InitPinsInput(chSeriesA, PortMaskTypes.Manipulator, false);
                                        if (!deviceA.ComparePins(chSeriesA, PortMaskTypes.Manipulator, true))
                                        {
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                                wstkA.TestRole = TestRoles.Manipulator;
                                wstkA.Nickname = JlinkDeviceInfo.GetNickname(testBedName, wstkA);
                                if (maskType == PortMaskTypes.ButtonsAndLeds)
                                {
                                    //	Output_A_ANY and (Input_B_ANY or Output_B_TRUE)
                                    deviceA.InitPinsOutput(chSeriesA, maskType, false);
                                }
                                foreach (var wstkB in selectedItemsArray.Where(x => x != wstkA))
                                {
                                    if (wstkB.TestRole == TestRoles.Manipulator || wstkB.TestRole == TestRoles.BoundDevice)
                                    {
                                        continue;
                                    }
                                    if (Enum.TryParse(wstkB.ChipSeries, out ChipTypes chSeriesB))
                                    {
                                        using (var deviceB = GetAppLayer(wstkB).CreateTestInterfaceDevice((ChipTypes)chSeriesB))
                                        {
                                            if (deviceB.Connect(wstkB.DataSource) == CommunicationStatuses.Done)
                                            {
                                                try
                                                {
                                                    if (maskType == PortMaskTypes.ButtonsAndLeds)
                                                    {
                                                        //	Output_A_ANY and (Input_B_ANY or Output_B_TRUE)
                                                        deviceB.InitPinsInput(chSeriesB, maskType, false);
                                                    }
                                                    deviceA.SetPins(chSeriesA, maskType, true);
                                                    if (deviceB.ComparePins(chSeriesB, maskType, true))
                                                    {
                                                        deviceA.SetPins(chSeriesA, maskType, false);
                                                        if (deviceB.ComparePins(chSeriesB, maskType, false))
                                                        {
                                                            deviceA.SetPins(chSeriesA, maskType, true);
                                                            if (deviceB.ComparePins(chSeriesB, maskType, true))
                                                            {
                                                                wstkA.LinkItem = wstkB;
                                                                wstkA.Nickname = JlinkDeviceInfo.GetNickname(testBedName, wstkA);
                                                                wstkB.Index = wstkA.Index;
                                                                wstkB.TestRole = TestRoles.BoundDevice;
                                                                wstkB.Nickname = JlinkDeviceInfo.GetNickname(testBedName, wstkB);
                                                                //_mainViewModel.LogActionCallback(true, $"[LINK] A({wstkA.SName}), B({wstkB.SName})");
                                                            }
                                                        }
                                                    }
                                                }
                                                catch (OperationException)
                                                {
                                                    //_mainViewModel.LogActionCallback(false, $"TestInterface {wstkB.SName} failed");
                                                }
                                            }
                                        }
                                    }
                                    if (wstkB.TestRole == TestRoles.BoundDevice)
                                    {
                                        break;
                                    }
                                }
                            }
                            catch (OperationException)
                            {
                                //_mainViewModel.LogActionCallback(false, $"TestInterface {wstkA.SName} failed");
                            }
                        }
                    }
                }
            }
        }

        private IEnumerable<DeviceHostClient> GetTestBedItem(IEnumerable<JlinkDeviceInfo> testBedBoards)
        {
            foreach (var item1 in testBedBoards)
            {
                if (string.IsNullOrWhiteSpace(item1.Nickname) || item1.Nickname.StartsWith("Z1A"))
                {
                    var deviceHostClient = new DeviceHostClient(
                         new DeviceHost
                         {
                             SName = item1.SName,
                             JName = item1.SerialNo,
                             ChipSeries = item1.ChipSeries,
                         }, GetAppLayer(item1), null);
                    deviceHostClient.WstkNickname = item1.Nickname;
                    yield return deviceHostClient;
                }
                else if (item1.Nickname.StartsWith("Z1M"))
                {
                }
                else if (item1.Nickname.StartsWith("Z1B"))
                {
                    var str = item1.Nickname.Split('.').FirstOrDefault();
                    if (str != null && str.Length > 4 && int.TryParse(str.Substring(3, str.Length - 3), out int order))
                    {
                        var orderStr = str.Substring(3, str.Length - 3);
                        var testBedName = item1.Nickname.Split('.').Skip(1).FirstOrDefault();
                        var item2 = testBedBoards.FirstOrDefault(x => x.Nickname == $"Z1M{orderStr}.{testBedName}");
                        if (item2 != null)
                        {
                            var deviceHostClient = new DeviceHostClient(
                                new DeviceHost
                                {
                                    SName = item1.SName,
                                    JName = item1.SerialNo,
                                    PName = item2.SName,
                                    ChipSeries = item1.ChipSeries,
                                }, GetAppLayer(item1), GetAppLayer(item2));
                            deviceHostClient.WstkNickname = item1.Nickname;
                            yield return deviceHostClient;
                        }
                    }
                }
            }
        }

        private void InitAlphaBoards(IEnumerable<string> ports)
        {
            Parallel.ForEach(ports, (port) =>
            {
                bool anyClient = false;
                lock (_deviceHostClients)
                {
                    anyClient = !_deviceHostClients.Any(x => x.Host.SName == port)
                    && !_deviceHostClients.Any(x => x.Host.PName == port);
                }
                if (anyClient)
                {
                    var deviceHostClient = new DeviceHostClient(new DeviceHost { PName = port }, _basicApp, null);
                    if (deviceHostClient.PClient.Connect() == CommunicationStatuses.Done)
                    {
                        Thread.Sleep(200);
                        bool res = false;
                        lock (_deviceHostClients)
                        {
                            _deviceHostClients.Add(deviceHostClient);
                            res = _alphaProgrammer.HasFirmware(deviceHostClient);
                            if (res)
                            {
                                _alphaProgrammer.GetChipSeries(deviceHostClient);
                            }
                            else
                            {
                                deviceHostClient.PClient.Dispose();
                                _deviceHostClients.Remove(deviceHostClient);
                            }
                        }
                    }
                }
            });
        }

        private DeviceHostClient DetectZnifferApi(IEnumerable<string> ports)
        {
            DeviceHostClient ret = null;
            foreach (var port in ports)
            {
                var dhc = new DeviceHostClient(new DeviceHost { SName = port }, _znifferApp, null);
                using (var zdevice = _znifferApp.CreateZniffer(dhc.SClient))
                {
                    if (dhc.SClient.Connect() == ZWave.Enums.CommunicationStatuses.Done)
                    {
                        var res = zdevice.GetVersion();
                        if (res)
                        {
                            ret = dhc;
                        }
                        else
                        {
                            var dataSource = dhc.SClient.DataSource as SerialPortDataSource;
                            dataSource.BaudRate = ZWave.Enums.BaudRates.Rate_230400;
                            if (dhc.SClient.Connect() == ZWave.Enums.CommunicationStatuses.Done)
                            {
                                res = zdevice.GetVersion();
                                if (res)
                                {
                                    ret = dhc;
                                }
                            }
                        }
                    }
                }
                if (ret != null)
                {
                    break;
                }
            }
            return ret;
        }

        private void EraseAlphaSamples()
        {
            Parallel.ForEach(_deviceHostClients.Where(x => x.HasPName && !x.HasJName), dhc =>
            {
                _alphaProgrammer.EraseSample(dhc);
            });
        }

        private void EraseGammaSamples()
        {
            if (_gammaProgrammer != null)
            {
                foreach (var dhc in _deviceHostClients.Where(x => x.HasJName && string.IsNullOrWhiteSpace(x.WstkNickname) && (x.Host.SType == InterfaceTypes.COM || x.Host.SType == InterfaceTypes.TCP)))
                {
                    _gammaProgrammer.EraseSample(dhc);
                }
            }
        }

        private void ProgramAlphaSamples(bool isUzb)
        {
            _alphaBoardDetectorByDSK.Clear();
            var location = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources");
            int dskCounter = 0;
            Parallel.ForEach(_deviceHostClients.Where(x => x.HasPName && !x.HasSName && !x.HasJName), dhc =>
            {
                //Write unique DSK to each board and keep track in dictionary, will be used to match pairs with read DSK later.
                ushort dsk;
                lock (_alphaBoardDetectorByDSK)
                {
                    dskCounter++;
                    dsk = (ushort)dskCounter;
                    _alphaBoardDetectorByDSK.Add(dsk, dhc.Host.PName);
                }
                var prk = new byte[32];
                var puk = Tools.GetBytes(dsk).Concat(new byte[30]).ToArray();
                var chipSeries = dhc.Host.ChipSeries;
                if (chipSeries == ChipSeries.ZW050x.ToString())
                {
                    string fileName = isUzb ? "sacs5uus.hex" : "sacss5us.hex";
                    var retCode = _alphaProgrammer.ProgramSample(dhc, ZWaveRegions.REGION_DEFAULT, Path.Combine(location, fileName), true, prk, puk);
                }
                else if (!isUzb && chipSeries == ChipSeries.ZW030x.ToString())
                {
                    var retCode = _alphaProgrammer.ProgramSample(dhc, ZWaveRegions.REGION_DEFAULT, Path.Combine(location, "sacs3us.hex"), false, null, null);
                    if (retCode == 0)
                    {
                        _alphaProgrammer.SetHomeId(dhc, Tools.GetBytes(dsk).Concat(new byte[2]).ToArray());
                    }
                }
                else
                {

                }
            });
        }

        /// <summary>
        /// Find connected devices with Serial API and add them to the list if not already added.
        /// Mostly used for Autodetection of connected devices with AlphaBoards.
        /// Gamma Boards can retrieve information using this method or gammaProgrammer.GetChipSeries.
        /// </summary>
        /// <param name="isUzb">Connection type: Serial/USB</param>
        private void DetectDevicesWithSerialApi(bool isUzb)
        {
            var ports = _portNamesGetter.GetPortNames()
                .Where(x =>
                    !_deviceHostClients.Any(y => y.BackupSNameTemp == x) &&
                    !_deviceHostClients.Any(y => y.Host.SName == x) &&
                    !_deviceHostClients.Any(y => y.Host.PName == x));
            var uzbports = _portNamesGetter.GetPortNames("VID_0658", "PID_0200");
            if (isUzb)
            {
                ports = uzbports;
            }
            else
            {
                ports = ports.Except(uzbports);
            }

            foreach (var port in ports)
            {
                FillDeviceHostMissingInfo(port, isUzb);
            }
        }

        /// <summary>
        /// Flash gamma samples with default predefined controller and set pairs with Manipulator via LED signaling if existed.
        /// </summary>
        /// <remarks>
        /// Used for Autodetection of Gamma boards and pairing with Alpha boards.
        /// </remarks>
        private void ProgramGammaSamples()
        {
            if (_gammaProgrammer == null)
            {
                "Gamma programmer Simplicity Commander is provided"._DLOG();
                return;
            }
            var gammaList = _deviceHostClients.Where(x => x.Host != null && x.Host.SType == InterfaceTypes.COM && x.HasJName).ToArray();
            Parallel.ForEach(gammaList, dhc =>
            {
                var cs = _gammaProgrammer.GetChipSeries(dhc);
                if (Enum.TryParse(cs, out ChipSeries chipSeries))
                {
                    string sampleFilePath = _fileSystemManager.GetPredefinedControllerPathFromResources(chipSeries);

                    var retCode = string.IsNullOrEmpty(sampleFilePath) ?
                        0 : // do nothing if no sample defined
                        _gammaProgrammer.ProgramSample(dhc, ZWaveRegions.REGION_US, sampleFilePath, true, null, null);

                    if (retCode != 0)
                    {
                        dhc.SClient.Dispose();
                        lock (_deviceHostClients)
                        {
                            _deviceHostClients.Remove(dhc);
                        }
                    }
                }
            });

            var alphaList = _deviceHostClients.Where(x => x.HasPName && !x.HasJName && (string.IsNullOrWhiteSpace(x.Host.ChipSeries) || x.Host.ChipSeries == ChipSeries.Default.ToString())).ToArray();
            foreach (var dhcA in alphaList)
            {
                var deviceA = dhcA.PClient;
                foreach (var dhcG in gammaList)
                {
                    if (_deviceHostClients.Contains(dhcG)) // check if it was not removed as already detected
                    {
                        var deviceG = dhcG.SClient;
                        if (!deviceG.TransportClient.IsOpen)
                        {
                            deviceG.Connect();
                        }
                        if (DetectLeds(deviceA, deviceG, dhcG.Host.ChipSeries))
                        {
                            dhcA.BackupSNameTemp = dhcG.BackupSNameTemp;
                            dhcA.Host.SName = dhcG.Host.SName;
                            dhcA.Host.JName = dhcG.Host.JName;
                            dhcA.Host.ChipSeries = dhcG.Host.ChipSeries;
                            dhcA.Host.SType = dhcG.Host.SType;
                            dhcA.SClient = deviceG;
                            _deviceHostClients.Remove(dhcG);
                            break;
                        }
                    }
                }
            }
        }

        private static bool DetectLeds(IApplicationClient deviceA, IApplicationClient deviceG, string chipSeriesName)
        {
            byte leds = 0; // all available OFF
            bool ret = false;
            ChipSeries chipSeries = ChipSeries.Default;
            Enum.TryParse(chipSeriesName, out chipSeries);
            ResetLeds(deviceG, 0x02, chipSeries);

            bool readyA0 = false;
            bool readyA1 = false;
            for (int i = 0; i < 3; i++)
            {
                var res = deviceA.Request(new byte[] { 0xEF, 0x00, (byte)ProgrammerCommandTypes.GetLedState, 0xFF }, new ByteIndex[] { ByteIndex.AnyValue, 0x01, (byte)ProgrammerCommandTypes.GetLedState }, 200);
                if (res && res.Payload != null && res.Payload.Length > 1)
                {
                    leds = (byte)(res.Payload[1] & 0x0F & 0x02); // check 2nd bit only 
                    if (leds == 0)
                    {
                        readyA0 = true;
                    }
                    break;
                }
            }

            leds = 0x0F; // all available ON

            SetLeds(deviceG, true, 0x02, chipSeries);

            for (int i = 0; i < 3; i++)
            {
                var res = deviceA.Request(new byte[] { 0xEF, 0x00, (byte)ProgrammerCommandTypes.GetLedState, 0xFF }, new ByteIndex[] { ByteIndex.AnyValue, 0x01, (byte)ProgrammerCommandTypes.GetLedState }, 200);
                if (res && res.Payload != null && res.Payload.Length > 1)
                {
                    leds = (byte)(res.Payload[1] & 0x0F & 0x02); // check 2nd bit only 
                    if (leds > 0)
                    {
                        readyA1 = true;
                    }
                    break;
                }
            }
            ret = readyA0 && readyA1;
            return ret;
        }

        private static void SetLeds(IApplicationClient deviceG, bool state, byte ledMask, ChipSeries chipSeries)
        {
            var stateStr = state ? "01" : "00";
            switch (chipSeries)
            {
                case ChipSeries.ZW080x:
                case ChipSeries.ZGM230S_BRD4205A:
                case ChipSeries.ZGM230S_BRD4205B:
                case ChipSeries.ZGM230_BRD2603A:
                case ChipSeries.ZGM230_BRD2603C:
                case ChipSeries.ZG23_BRD4204A:
                case ChipSeries.ZG23_BRD4204C:
                case ChipSeries.ZG23_BRD4204D:
                case ChipSeries.ZG23_BRD4210A:
                    if ((ledMask & 0x01) > 0)
                    {
                        deviceG.Send($"00 E6 01 00 06 {stateStr}".GetBytes()); Thread.Sleep(100);
                    }
                    if (((ledMask >> 1) & 0x01) > 0)
                    {
                        deviceG.Send($"00 E6 01 00 07 {stateStr}".GetBytes()); Thread.Sleep(100);
                    }
                    if (((ledMask >> 2) & 0x01) > 0)
                    {
                        deviceG.Send($"00 E6 01 00 0A {stateStr}".GetBytes()); Thread.Sleep(100);
                    }
                    if (((ledMask >> 3) & 0x01) > 0)
                    {
                        deviceG.Send($"00 E6 01 00 00 {stateStr}".GetBytes()); Thread.Sleep(100);
                    }
                    break;
                default:
                    if ((ledMask & 0x01) > 0)
                    {
                        deviceG.Send($"00 E6 01 05 04 {stateStr}".GetBytes()); Thread.Sleep(100);
                    }
                    if (((ledMask >> 1) & 0x01) > 0)
                    {
                        deviceG.Send($"00 E6 01 05 03 {stateStr}".GetBytes()); Thread.Sleep(100);
                    }
                    if (((ledMask >> 2) & 0x01) > 0)
                    {
                        deviceG.Send($"00 E6 01 00 02 {stateStr}".GetBytes()); Thread.Sleep(100);
                    }
                    if (((ledMask >> 3) & 0x01) > 0)
                    {
                        deviceG.Send($"00 E6 01 00 03 {stateStr}".GetBytes()); Thread.Sleep(100);
                    }
                    break;

            }
        }

        private static void ResetLeds(IApplicationClient deviceG, byte ledMask, ChipSeries chipSeries)
        {
            deviceG.Send("00 E5 01".GetBytes()); Thread.Sleep(100);

            switch (chipSeries)
            {
                case ChipSeries.ZW080x:
                case ChipSeries.ZGM230S_BRD4205A:
                case ChipSeries.ZGM230S_BRD4205B:
                case ChipSeries.ZGM230_BRD2603A:
                case ChipSeries.ZGM230_BRD2603C:
                case ChipSeries.ZG23_BRD4204A:
                case ChipSeries.ZG23_BRD4204C:
                case ChipSeries.ZG23_BRD4204D:
                case ChipSeries.ZG23_BRD4210A:
                    if ((ledMask & 0x01) > 0)
                    {
                        deviceG.Send("00 E6 05 00 06".GetBytes()); Thread.Sleep(100);
                        deviceG.Send("00 E6 01 00 06 00".GetBytes()); Thread.Sleep(100);
                    }
                    if (((ledMask >> 1) & 0x01) > 0)
                    {
                        deviceG.Send("00 E6 05 00 07".GetBytes()); Thread.Sleep(100);
                        deviceG.Send("00 E6 01 00 07 00".GetBytes()); Thread.Sleep(100);
                    }
                    if (((ledMask >> 2) & 0x01) > 0)
                    {
                        // do not use 3rd bit
                        deviceG.Send("00 E6 05 00 0A".GetBytes()); Thread.Sleep(100);
                        deviceG.Send("00 E6 01 00 0A 00".GetBytes()); Thread.Sleep(100);
                    }
                    if (((ledMask >> 3) & 0x01) > 0)
                    {
                        deviceG.Send("00 E6 05 00 00".GetBytes()); Thread.Sleep(100);
                        deviceG.Send("00 E6 01 00 00 00".GetBytes()); Thread.Sleep(100);
                    }
                    break;
                default:
                    if ((ledMask & 0x01) > 0)
                    {
                        deviceG.Send("00 E6 05 05 04".GetBytes()); Thread.Sleep(100);
                        deviceG.Send("00 E6 01 05 04 00".GetBytes()); Thread.Sleep(100);
                    }
                    if (((ledMask >> 1) & 0x01) > 0)
                    {
                        deviceG.Send("00 E6 05 05 03".GetBytes()); Thread.Sleep(100);
                        deviceG.Send("00 E6 01 05 03 00".GetBytes()); Thread.Sleep(100);
                    }
                    if (((ledMask >> 2) & 0x01) > 0)
                    {
                        // do not use 3rd bit
                        deviceG.Send("00 E6 05 00 02".GetBytes()); Thread.Sleep(100);
                        deviceG.Send("00 E6 01 00 02 00".GetBytes()); Thread.Sleep(100);
                    }
                    if (((ledMask >> 3) & 0x01) > 0)
                    {
                        deviceG.Send("00 E6 05 00 03".GetBytes()); Thread.Sleep(100);
                        deviceG.Send("00 E6 01 00 03 00".GetBytes()); Thread.Sleep(100);
                    }
                    break;
            }
        }

        /// <summary>
        /// Fill missing device host info by connecting to the device and reading Serial API info.
        /// </summary>
        /// <param name="portName">Device port name</param>
        /// <param name="isUzb">Connection type Serial/USB.</param>
        private void FillDeviceHostMissingInfo(string portName, bool isUzb)
        {
            if (_deviceHostClients.Any(x => x.HasJName && x.Host.SName == portName && !string.IsNullOrEmpty(x.Host.ChipSeries)))
                return; //skip if detected with JLink 
            var dhc = new DeviceHostClient(new DeviceHost { SName = portName }, _basicApp, null);
            dhc.SClient.Connect();
            var device = _basicApp.CreateController(dhc.SClient);
            var hasSerialApi = device.SerialApiGetInitData(true);
            if (!hasSerialApi)
            {
                hasSerialApi = device.SerialApiGetInitData(true);
            }
            if (hasSerialApi)
            {
                // For alpha device: FindInDictionary - detect pairs by DSK to fill deviceHostClient with device pair
                ushort dsk = 0;
                if (device.ChipType == ZWave.Enums.ChipTypes.ZW050x)
                {
                    var dskRes = device.NVRGetValue(0x43 - 32, 2);
                    if (dskRes && dskRes.NVRValue != null && dskRes.NVRValue.Length > 1)
                    {
                        dsk = (ushort)((dskRes.NVRValue[0] << 8) + dskRes.NVRValue[1]);
                    }
                }
                else if (device.ChipType == ZWave.Enums.ChipTypes.ZW030x)
                {
                    var homeIdRes = device.MemoryGetId();
                    if (homeIdRes != null && homeIdRes.HomeId != null && homeIdRes.HomeId.Length == 4)
                    {
                        dsk = (ushort)((homeIdRes.HomeId[0] << 8) + homeIdRes.HomeId[1]);
                    }
                }
                // Complete pair with existing alpha board or add new.
                if (_alphaBoardDetectorByDSK.ContainsKey(dsk))
                {
                    var pdhc = _deviceHostClients.FirstOrDefault(x => x.Host.PName == _alphaBoardDetectorByDSK[dsk]);
                    _alphaBoardDetectorByDSK.Remove(dsk);
                    if (pdhc != null)
                    {
                        pdhc.Host.SName = dhc.Host.SName;
                        pdhc.Host.IsUZB = isUzb;
                        pdhc.SClient = dhc.SClient;
                    }
                }
                else
                {
                    dhc.Host.ChipSeries = device.ChipType.ToString();
                    lock (_deviceHostClients)
                    {
                        _deviceHostClients.Add(dhc);
                    }
                }
            }
            else
            {
                device.Dispose();
            }
        }

        public void Dispose()
        {
        }
    }

    public interface IPortNamesGetter
    {
        string[] GetPortNames();
        string[] GetPortNames(string vid, string pid);
        Tuple<string, string>[] GetBoardLinks(IProgrammingProvider gammaProvider);
    }

    public class PortNamesGetter : IPortNamesGetter
    {
        public string[] GetPortNames()
        {
            return GetPortNames(null, null);
        }

        public Tuple<string, string>[] GetBoardLinks(IProgrammingProvider gammaProvider)
        {
#if NETCOREAPP
            return GetGammaSourceAndSerial(gammaProvider);
#else
            return ComputerSystemHardwareHelper.GetGammaSourceAndSerial().ToArray();
#endif
        }

#if NETCOREAPP
        private Tuple<string, string>[] GetGammaSourceAndSerial(IProgrammingProvider gammaProgrammer)
        {
            var ret = new List<Tuple<string, string>>();
            var versionOutput = gammaProgrammer.GetVersion();
            var rx = new Regex(@"\bSN=\d{9}\b");
            var serialNums = rx.Matches(versionOutput)?.Select(x => Regex.Replace(x.ToString(), @"[^\d]", ""));
            foreach (var serial in serialNums)
            {
                var dh = new DeviceHost() { JName = serial };
                var dhc = new DeviceHostClient(dh);
                var res = gammaProgrammer.GetChipSeries(dhc);
                if (!string.IsNullOrEmpty(res))
                {
                    ret.Add(new Tuple<string, string>(dhc.Host.SName, serial));
                }
            }
            return ret.ToArray();
        }
#endif

        public string[] GetPortNames(string vid, string pid)
        {
#if NETCOREAPP
            return ZWave.Layers.Transport.SerialPortTransportClient.GetPortNames(vid, pid);
#else
            string condition = vid != null && pid != null ? $"DeviceId like '%{vid}%{pid}%'" : null;
            return ComputerSystemHardwareHelper.GetWin32PnPEntityClassSerialPortDevices(condition).Select(x => x.SerialPortName).ToArray();
#endif
        }
    }
}
