// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Utils;
using Utils.Events;
using Utils.Extensions;
using Utils.UI.Enums;
using Zats.Services.Configuration;
using Zats.Services.Constants;
using Zats.Services.Enums;
using Zats.Services.SampleApplication;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.EmulatedLink;
using ZWave.BasicApplication.Operations;
using ZWave.BasicApplication.Tasks;
using ZWave.Devices;
using ZWave.Enums;
using ZWave.Layers;
using ZWave.Layers.Transport;
using ZWave.TextApplication;
using ZWave.Xml.Application;
using ZWave.Xml.FrameHeader;

namespace Zats.Services
{
    public class DeviceManager : DeviceManagerBase
    {
        private readonly bool _useCLIforEndDevice;
        public event EventHandler<EventArgs<SessionAttachment>> OnConnected;
        public ZWave.Enums.TransmitOptions TX_OPTIONS = ZWave.Enums.TransmitOptions.TransmitOptionAcknowledge | ZWave.Enums.TransmitOptions.TransmitOptionAutoRoute | ZWave.Enums.TransmitOptions.TransmitOptionExplore;
        public bool IsSkipCheckPortExists { get; set; }
        static public readonly string[] SpecificChips = new[] { "ZGM13", "ZG13", "RZ13", "ZG14" };

        private readonly List<ChipSeries> ChipSeriesWithControllerPriority =
            new List<ChipSeries>
            {
                ChipSeries.ZG14_BRD4201A,
                ChipSeries.ZG14_BRD4206A,
                ChipSeries.ZG14_BRD4208A,
                ChipSeries.ZG23_BRD4204C,
                ChipSeries.ZG23_BRD4204D };

        private readonly List<ChipSeries> ChipSeriesWithSampleAppPriority =
             new List<ChipSeries>
             {
                 ChipSeries.ZG13L_BRD4201C,
                 ChipSeries.RZ13_BRD4201B,
                 ChipSeries.ZG13S_BRD4201D,
                 ChipSeries.ZGM130S_BRD4202A,
                 ChipSeries.ZGM130S_BRD4207A,
                 ChipSeries.ZGM230S_BRD4205A,
                 ChipSeries.ZGM230S_BRD4205B,
                 ChipSeries.ZGM230_BRD2603A
             };

        internal int RunProgrammerWithArgs(DeviceHostClient deviceClientHost, string runArgs)
        {
            return _alphaProgrammer.Run(deviceClientHost, runArgs);
        }

        internal int SetHomeId(DeviceHostClient deviceClientHost, byte[] homeId)
        {
            return _alphaProgrammer.SetHomeId(deviceClientHost, homeId);
        }

        public string GetChipType(DeviceHostClient deviceClientHost)
        {
            return _gammaProgrammer.GetChipSeries(deviceClientHost);
        }

        internal string GblCreateCustom(string hexFilePath, string gblFilePath, BootloaderPath blp, bool isSeries2 = false)
        {
            string ret = null;
            if (string.IsNullOrEmpty(gblFilePath))
            {
                gblFilePath = hexFilePath.Replace(".hex", ".gbl"); //put in the same place as original.
            }

            var location = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources");
            var encryptKeyPath = blp?.EncryptKey ?? Path.Combine(location, "sample_encrypt.key");
            var signKeyPath = blp?.SignKey ?? Path.Combine(location, "sample_sign.key");

            if (_gammaProgrammer.GblCreate(hexFilePath, gblFilePath, signKeyPath, encryptKeyPath, isSeries2) == 0)
            {
                ret = gblFilePath;
            }
            return ret;
        }

        private IProgrammingProvider _alphaProgrammer;
        private IProgrammingProvider _gammaProgrammer;
        public FrameDefinition FrameDefinition { get; private set; }
        public MessageBoxImpl MessageBox { get; set; }

        public DeviceManager(
            bool isLinkFramework,
            IProgrammingProvider alphaProgrammer,
            IProgrammingProvider gammaProgrammer,
            IFileSystemManager fileSystemManager,
            FrameDefinition frameDefinition,
            bool useCLIforEndDevice = false)
            : base()
        {
            _useCLIforEndDevice = useCLIforEndDevice;
            _alphaProgrammer = alphaProgrammer;
            _gammaProgrammer = gammaProgrammer;
            FileSystemManager = fileSystemManager;
            FrameDefinition = frameDefinition;
            ProvisioningList = new ProvisioningList();
            if (isLinkFramework)
            {
                BasicApplicationLayer = new BasicApplicationLayer(SessionLayer, new BasicFrameLayer(), new BasicLinkTransportLayer());
            }
            else
            {
                BasicApplicationLayer = new BasicApplicationLayer(SessionLayer, new BasicFrameLayer(), new SerialPortTransportLayer());
            }
            SnifferPtiApplicationLayer = new ZWave.ZnifferApplication.ZnifferApplicationLayer(SessionLayer, new ZWave.ZnifferApplication.SnifferPtiFrameLayer(FrameDefinition), new TcpClientTransportLayer());
            TcpApplicationLayer = new BasicApplicationLayer(SessionLayer, new BasicFrameLayer(), new TcpClientTransportLayer());
            TextApplicationLayer = new TextApplicationLayer(SessionLayer, new TextFrameLayer(), new SerialPortTransportLayer());
            Parameters = new Parameters();
            SecurityTestSettings = new SecurityTestSettings(this);
            SendDataSettings = new SendDataSettings(this);
            Associations = new Associations(this);
            SecurityS2 = new SecurityS2(this);
            FirmwareUpdateMD = new FirmwareUpdateMD(this);
            IncludeSettings = new IncludeSettings();
            SilinkManager = new SilinkManager(this);
        }

        public ConcurrentDictionary<Alias, DeviceHostClient> Devices = new ConcurrentDictionary<Alias, DeviceHostClient>();

        private SortedList<int, ActionToken> _associationsDeviceSupport = new SortedList<int, ActionToken>();
        private SortedList<int, ActionToken> _associationsDevicesGroupInfoSupport = new SortedList<int, ActionToken>();
        private SortedList<int, ActionToken> _firmwareUpdateMdSupport = new SortedList<int, ActionToken>();

        internal BasicResult.Void SoftReset(DeviceHostClient deviceHostClient, Alias deviceAlias)
        {
            var device = deviceHostClient.SClient as Device;
            var startToken = device.Expect(new ByteIndex[] { 0x00, 0x0A }, 4000, null); // UZB needs more time than dev boards
            ActionResult res = device.SoftReset();
            var startRes = startToken.WaitCompletedSignal();
            if (!startRes)
            {
                Log("No Serial API started callback registered", LogLevels.Diagnostic);
                if (deviceHostClient.SClient != null && deviceHostClient.SClient.ChipType <= ChipTypes.ZW050x)
                {
                    Reconnect(deviceHostClient);
                }
            }
            if (!IsOptionDisabled(ZWaveSet.NodeIdBase2, deviceAlias))
            {
                (deviceHostClient.SClient as Device).SerialApiSetNodeIdBaseType(2);
            }
            return new BasicActionResult.Void(res);
        }

        private SortedList<int, SortedList<int, ActionToken>> CCSupportDeviceTokens = new SortedList<int, SortedList<int, ActionToken>>();

        private List<ActionTokenForDeviceContainer> CCSupportContainers = new List<ActionTokenForDeviceContainer>();

        private ConfigurationModes _configurationMode;

        public ProvisioningList ProvisioningList { get; set; }
        public IAssociations Associations { get; set; }
        public ISecurityTestSettings SecurityTestSettings { get; set; }
        public ISecurityS2 SecurityS2 { get; set; }
        public FirmwareUpdateMD FirmwareUpdateMD { get; set; }
        public SendDataSettings SendDataSettings { get; set; }
        public BasicApplicationLayer BasicApplicationLayer { get; set; }
        public ZWave.ZnifferApplication.ZnifferApplicationLayer SnifferPtiApplicationLayer { get; set; }
        public TextApplicationLayer TextApplicationLayer { get; set; }
        public BasicApplicationLayer TcpApplicationLayer { get; set; }
        public Parameters Parameters { get; set; }
        public SortedList<ZWaveSet, Alias[]> EnableOptions { get; set; }
        public SortedList<ZWaveSet, Alias[]> DisableOptions { get; set; }
        public IIncludeSettings IncludeSettings { get; set; }
        public IFileSystemManager FileSystemManager { get; set; }
        public SilinkManager SilinkManager { get; private set; }

        public ZWaveDefinition ZWaveDefinition { get; set; }
        public static string IncludeUsbVcpRegexFilter = "USBVCP";
        public static string ExcludeUsbVcpRegexFilter = @"^(?!.*USBVCP).*$";

        public Dictionary<byte, Dictionary<byte, CommandCallback>> CommandCallbacks = new Dictionary<byte, Dictionary<byte, CommandCallback>>();

        public ConcurrentDictionary<Alias, DeviceHostClient> CutDevices(params Libraries[] libraries)
        {
            var ret = new ConcurrentDictionary<Alias, DeviceHostClient>();
            var filteredItems = Devices.Where(device => libraries.Contains(device.Value.Library)).ToArray();
            Parallel.ForEach(filteredItems, item =>
            {
                LoadDSK(item.Value);
                FinalizeDevice(item.Value);
                if (Devices.TryRemove(item.Key, out DeviceHostClient oldDhc))
                {
                    item.Value.DeviceAlias = item.Key;
                    ret.AddOrUpdate(item.Key, item.Value, (key, val) => item.Value);
                }
            });
            return ret;
        }

        private ChipSeries[] _ZW030xList = new[]
        {
            ChipSeries.ZW030x,
            ChipSeries.ZW020x,
            ChipSeries.ZW010x
        };

        public bool IsZW030x(Alias deviceNumber)
        {
            var device = GetDeviceHostClient(deviceNumber);
            return _ZW030xList.Select(x => x.ToString()).Contains(device.Host?.ChipSeries ?? "");
        }

        private ChipSeries[] _ZW050xList = new[]
        {
            ChipSeries.ZW050x
        };

        public bool IsZW050x(Alias deviceNumber)
        {
            var device = GetDeviceHostClient(deviceNumber);
            return _ZW050xList.Select(x => x.ToString()).Contains(device.Host?.ChipSeries ?? "");
        }

        private ChipSeries[] _ZW070xList = new[]
        {
            ChipSeries.ZW070x,
            ChipSeries.ZGM130S_BRD4200A,
            ChipSeries.ZGM130S_BRD4202A,
            ChipSeries.ZGM130S_BRD4207A,
            ChipSeries.RZ13_BRD4201B,
            ChipSeries.RZ13_BRD4203A,
            ChipSeries.ZG13S_BRD4201D,
            ChipSeries.ZG13L_BRD4201C,
            ChipSeries.ZG14_BRD4201A,
            ChipSeries.ZG14_BRD4206A,
            ChipSeries.ZG14_BRD4208A,
            ChipSeries.RZ13_BRD4209A
        };

        public bool IsZW070x(Alias deviceNumber)
        {
            var device = GetDeviceHostClient(deviceNumber);
            return _ZW070xList.Select(x => x.ToString()).Contains(device.Host?.ChipSeries ?? "") || IsZW080x(deviceNumber);
        }

        private ChipSeries[] _ZW080xList = new[]
        {
            ChipSeries.ZW080x,
            ChipSeries.ZG23_BRD4204C,
            ChipSeries.ZG23_BRD4204D,
            ChipSeries.ZGM230S_BRD4205A,
            ChipSeries.ZGM230S_BRD4205B,
            ChipSeries.ZGM230_BRD2603A,
            ChipSeries.xG28_BRD4400A,
            ChipSeries.xG28_BRD4401A,
            ChipSeries.xG28_BRD4400B,
            ChipSeries.xG28_BRD4401B,
            ChipSeries.xG28_BRD4400C,
            ChipSeries.xG28_BRD4401C,
            ChipSeries.xG28_BRD2705A
        };

        public bool IsZW080x(Alias deviceNumber)
        {
            var device = GetDeviceHostClient(deviceNumber);
            return _ZW080xList.Select(x => x.ToString()).Contains(device.Host?.ChipSeries ?? "");
        }

        public void EnableAssociations(Alias[] deviceNumbers)
        {
            foreach (var item in deviceNumbers)
            {
                var device = GetDevice(item);
                if (!_associationsDevicesGroupInfoSupport.ContainsKey(item.Value))
                {
                    var tokenGroupInfoSupport = device.ExecuteAsync(new ZWave.BasicApplication.CommandClasses.AssociationGroupInfoSupport(device.Network, TX_OPTIONS), null);
                    _associationsDevicesGroupInfoSupport.Add(item.Value, tokenGroupInfoSupport);
                }
                if (!_associationsDeviceSupport.ContainsKey(item.Value))
                {
                    var tokenAssociationSupport = device.ExecuteAsync(new ZWave.BasicApplication.CommandClasses.AssociationSupport(device.Network, TX_OPTIONS), null);
                    _associationsDeviceSupport.Add(item.Value, tokenAssociationSupport);
                }
            }
        }

        public void AddCommandClassSupport(Alias deviceNumber, byte endPoint, CommandClassSupport ccSupport, bool updateVersionCC = true)
        {
            Device device = GetDevice(deviceNumber);
            CancelRunningSupportCommand(ccSupport, device);

            ccSupport.CommandCallbacks = CommandCallbacks;
            ccSupport.TxOptions = TX_OPTIONS;
            ccSupport.SetNetwork(device.Network);
            ccSupport.DstNode = new NodeTag(0, endPoint);

            if (ccSupport is AssociationSupport)
            {
                // Needed to correctly cancel token
                Thread.Sleep(2000);
            }

            var tokenSupport = device.ExecuteAsync(ccSupport, null);

            CCSupportContainers.Add(new ActionTokenForDeviceContainer
            {
                ActionToken = tokenSupport,
                Device = device,
                ActionType = ccSupport.GetType(),
                CommandClassSupport = ccSupport
            });

            if (updateVersionCC)
            {
                AddOrUpdateVersionCommandClassSupportForDevice(deviceNumber, endPoint);
            }
        }

        public void CancelAllRunningSupportCommands(Alias deviceNumber)
        {
            Device device = GetDevice(deviceNumber);
            var itemsToCancel = CCSupportContainers
                            .Where(x => x.Device == device)
                            .Select(y => y).ToList();

            foreach (var itemToCancel in itemsToCancel)
            {
                device.Cancel(itemToCancel.ActionToken);

                CCSupportContainers.Remove(itemToCancel);
            }
        }


        private void CancelRunningSupportCommand(CommandClassSupport ccSupport, Device device)
        {
            var itemsToCancel = CCSupportContainers
                            .Where(x => x.Device == device && x.ActionType == ccSupport.GetType())
                            .Select(y => y).ToList();

            foreach (var itemToCancel in itemsToCancel)
            {
                device.Cancel(itemToCancel.ActionToken);

                CCSupportContainers.Remove(itemToCancel);
            }
        }

        public void AddOrUpdateVersionCommandClassSupportForDevice(Alias deviceNumber, byte endPoint)
        {
            Device device = GetDevice(deviceNumber);

            var versionSupportObject = CCSupportContainers.FirstOrDefault(x => x.Device == device && x.CommandClassSupport is VersionSupport);
            if (versionSupportObject != null)
            {
                device.Cancel(versionSupportObject.ActionToken);

                // Needed to correctly cancel token
                Thread.Sleep(2000);

                CCSupportContainers.Remove(versionSupportObject);
            }

            var commandClassesSupportHandler = new CommandClassesSupportHandler();
            var listOfSupportedByDevice = CCSupportContainers
                .Where(containers => containers.Device == device)
                .Select(container => container.CommandClassSupport)
                .ToList();
            var versionSupport = new VersionSupport(commandClassesSupportHandler.GetDummyVersionSupportData(), listOfSupportedByDevice) { Version = 3 };

            versionSupport.CommandCallbacks = CommandCallbacks;
            versionSupport.TxOptions = TX_OPTIONS;
            versionSupport.SetNetwork(device.Network);
            versionSupport.DstNode = new NodeTag(0, endPoint);
            var versionTokenSupport = device.ExecuteAsync(versionSupport, null);

            CCSupportContainers.Add(new ActionTokenForDeviceContainer
            {
                ActionToken = versionTokenSupport,
                Device = device,
                ActionType = versionSupport.GetType(),
                CommandClassSupport = versionSupport
            });
        }


        public void EnableCommandClassSupport(Alias deviceNumber, byte endPoint, CommandClassSupport ccSupport)
        {
            Device device = GetDevice(deviceNumber);

            //if (!SupportedCCs.ContainsKey(ccSupport.Id))
            //{
            //    SupportedCCs.Add(ccSupport.Id, ccSupport.Version);
            //}

            //if (!CCSupportDeviceTokens.ContainsKey(ccSupport.Id))
            //{
            //    CCSupportDeviceTokens[ccSupport.Id] = new SortedList<int, ActionToken>();
            //}

            //if (!CCSupportDeviceTokens[ccSupport.Id].ContainsKey(deviceNumber.Value))
            //{


            ccSupport.CommandCallbacks = CommandCallbacks;
            ccSupport.TxOptions = TX_OPTIONS;
            ccSupport.SetNetwork(device.Network);
            ccSupport.DstNode = new NodeTag(0, endPoint);


            var tokenSupport = device.ExecuteAsync(ccSupport, null);
        }

        public void EnableFirmwareUpdateSupport(Alias[] deviceNumbers, byte version)
        {
            foreach (var item in deviceNumbers)
            {
                var device = GetDevice(item);

                if (!_firmwareUpdateMdSupport.ContainsKey(item.Value))
                {
                    var fwSupport = new FirmwareUpdateMdSupportBasic(() => { _fwuMDCompleted.Set(); })
                    {
                        CommandCallbacks = CommandCallbacks,
                        TxOptions = TX_OPTIONS,
                    };
                    fwSupport.SetNetwork(device.Network);
                    var tokenFirmwareUpdateMdSupport = device.ExecuteAsync(fwSupport, null);
                    _firmwareUpdateMdSupport.Add(item.Value, tokenFirmwareUpdateMdSupport);
                }
            }
        }

        public void DisableAssociations(Alias deviceNumber)
        {
            Device device = GetDevice(deviceNumber);
            if (_associationsDevicesGroupInfoSupport.ContainsKey(deviceNumber.Value))
            {
                device.Cancel(_associationsDevicesGroupInfoSupport[deviceNumber.Value]);
                _associationsDevicesGroupInfoSupport.Remove(deviceNumber.Value);
            }
            if (_associationsDeviceSupport.ContainsKey(deviceNumber.Value))
            {
                device.Cancel(_associationsDeviceSupport[deviceNumber.Value]);
                _associationsDeviceSupport.Remove(deviceNumber.Value);
            }
        }

        public void AddCommandCallbacks(byte commandClass, Dictionary<byte, CommandCallback> callbacks)
        {
            if (!CommandCallbacks.ContainsKey(commandClass))
            {
                CommandCallbacks.Add(commandClass, callbacks);
            }
            else
            {
                // if a set of callbacks is already present for a Command Class, additional callbacks will be added
                foreach (byte key in callbacks.Keys)
                {
                    CommandCallbacks[commandClass][key] += callbacks[key];
                }
            }
        }

        public void RemoveCommandCallbacks(byte commandClass)
        {
            if (CommandCallbacks != null && CommandCallbacks.ContainsKey(commandClass))
            {
                CommandCallbacks.Remove(commandClass);
            }
        }

        public Device GetDevice(Alias alias)
        {
            if (Devices.TryGetValue(alias.EndPoint(0), out DeviceHostClient dhc))
            {
                return dhc.SClient as Device;
            }
            else { return null; }
        }

        public TestInterfaceDevice GetTIDevice(Alias alias)
        {
            if (Devices.TryGetValue(alias, out DeviceHostClient dhc))
            {
                return dhc.PClient as TestInterfaceDevice;
            }
            else { return null; }
        }

        public DeviceHostClient GetDeviceHostClient(Alias alias)
        {
            if (Devices.TryGetValue(alias.EndPoint(0), out DeviceHostClient ret))
            {
                return ret;
            }
            else
                throw new ServiceException($"can't find device {alias}");
        }

        public IEnumerable<DeviceHostClient> GetDeviceHostClients(Alias groupAlias)
        {
            List<DeviceHostClient> ret = new List<DeviceHostClient>();
            Alias[] aliases = Devices.Keys.Where(x => x.Group == groupAlias.Value).OrderBy(x => x.Value).ToArray();
            foreach (var alias in aliases)
            {
                if (Devices.TryGetValue(alias, out DeviceHostClient dhc))
                {
                    ret.Add(dhc);
                }
                else
                    throw new ServiceException($"can't find device {alias}");
            }
            return ret;
        }

        public void AddOrReplaceDevice(Alias alias, DeviceHostClient newDevice)
        {
            newDevice.DeviceAlias = alias;
            Devices.AddOrUpdate(alias, newDevice, (k, v) => newDevice);
        }


        public void ConnectDevices()
        {
            Parallel.ForEach(Devices, item =>
            {
                LoadDSK(item.Value);
            });
        }

        public static bool IsTerminalApplication(Libraries library)
        {
            return new[]
            {
                Libraries.RailTest
            }.Contains(library);
        }

        public static bool IsSampleApplication(Libraries library)
        {
            return new[]
            {
                Libraries.DoorLockKeyPad,
                Libraries.LEDBulb,
                Libraries.PowerStrip,
                Libraries.SensorPIR,
                Libraries.SwitchOnOff,
                Libraries.WallController,
                Libraries.MultilevelSensor,
            }.Contains(library);
        }

        public static bool IsSerialApiController(Libraries library)
        {
            return new[]
            {
                Libraries.ControllerBridgeLib,
                Libraries.ControllerPortableLib,
                Libraries.ControllerStaticLib,
                Libraries.InstallerLib
            }.Contains(library);
        }

        public static bool IsSerialApi(Libraries library)
        {
            return new[]
            {
                Libraries.ControllerBridgeLib,
                Libraries.ControllerPortableLib,
                Libraries.ControllerStaticLib,
                Libraries.InstallerLib,
                Libraries.EndDeviceSysTestLib,
                Libraries.EndDeviceLib,
                Libraries.EndDeviceLegacyLib,
                Libraries.EndDeviceLegacyRoutingLib,
                Libraries.TestController
            }.Contains(library);
        }

        internal void ReportTestFramesAndExtensionOverrides(Alias controllerNumber)
        {
            Controller controller = (Controller)GetDevice(controllerNumber);
            var sm = (SecurityManager)(controller.SessionClient.GetSubstituteManager(typeof(SecurityManager)));
            if (sm != null)
            {
                if (sm.SecurityManagerInfo.TestFramesS2 != null && sm.SecurityManagerInfo.TestFramesS2.Count > 0)
                {
                    foreach (var item in sm.SecurityManagerInfo.TestFramesS2)
                    {
                        if (item.Value.IsEnabled)
                        {
                            Log(string.Format("TF {0}:{1}{2}{3}{4}{5}{6}{7}",
                                item.Key,
                                item.Value.Command != null ? " cmd:" + item.Value.Command.GetHex() : "",
                                item.Value.DelaySpecified ? " dly:" + item.Value.Delay : "",
                                item.Value.IsBroadcastSpecified ? " bct:" + item.Value.IsBroadcast : "",
                                item.Value.IsEncryptedSpecified ? " enc:" + item.Value.IsEncrypted : "",
                                item.Value.IsTempSpecified ? " tmp:" + item.Value.IsTemp : "",
                                item.Value.IsMulticastSpecified ? " mct:" + item.Value.IsMulticast : "",
                                item.Value.NetworkKey != null ? " key:" + item.Value.NetworkKey.GetHex() : ""
                                ), LogLevels.Information);
                        }
                    }
                }
                if (sm.SecurityManagerInfo.TestExtensionsS2 != null && sm.SecurityManagerInfo.TestExtensionsS2.Count > 0)
                {
                    foreach (var item in sm.SecurityManagerInfo.TestExtensionsS2)
                    {
                        if (item.IsEnabled)
                        {
                            Log(string.Format("EO {0} {1} {2} {3}{4}{5}{6}{7}",
                                item.MessageType,
                                item.ExtensionType,
                                item.Action,
                                item.IsCriticalSpecified ? " crt:" + item.IsCritical : "",
                                item.IsEncryptedSpecified ? " enc:" + item.IsEncrypted : "",
                                item.IsMoreToFollowSpecified ? " mtf:" + item.IsMoreToFollow : "",
                                item.NumOfUsageSpecified ? " num:" + item.NumOfUsage : "",
                                item.ValueSpecified ? " val:" + item.Value : ""
                                ), LogLevels.Information);
                        }
                    }
                }
            }
        }

        private IEnumerable<DeviceHost> RemoveUnavaliblePorts(Profile activeProfile)
        {
            return DeviceManager.RemoveUnavaliblePorts(activeProfile.DeviceHosts.ToList());
        }

        public void EraseAllBoards(Profile activeProfile)
        {
            //Erase Boards
            IEnumerable<DeviceHost> avail = RemoveUnavaliblePorts(activeProfile);
            Parallel.ForEach(avail.Where(x => !string.IsNullOrEmpty(x.PName) || !string.IsNullOrEmpty(x.JName)), host =>
            {
                if (string.IsNullOrEmpty(host.JName))
                {
                    if (_alphaProgrammer != null)
                    {
                        _alphaProgrammer.EraseSample(new DeviceHostClient(host));
                    }
                }
                else
                {
                    if (_gammaProgrammer != null)
                    {
                        _gammaProgrammer.EraseSample(new DeviceHostClient(host, null, null));
                    }
                }
            });

            lock (ZatsCache)
            {
                ZatsCache.ProgrammingResults.Clear();
            }
        }

        public void InitDevices(List<DeviceHost> available, List<UnknownDevice> required, bool isAppendMode, ConfigurationModes configurationMode, string configPath)
        {
            var deviceHostClients = new ConcurrentBag<DeviceHostClient>();
            var avail = available.Where(x => !string.IsNullOrEmpty(x.SName) || !string.IsNullOrEmpty(x.PName));

            // support old chip naming
            foreach (var item in avail)
            {
                if (item.ChipSeries == ChipSeries.ZW0713.ToString())
                {
                    item.ChipSeries = available.Any(x => x.ChipSeries == ChipSeries.ZGM130S_BRD4202A.ToString())
                        ? ChipSeries.ZGM130S_BRD4202A.ToString() : ChipSeries.ZGM130S_BRD4207A.ToString();
                }
                else if (item.ChipSeries == ChipSeries.ZW0714.ToString())
                {
                    item.ChipSeries = available.Any(x => x.ChipSeries == ChipSeries.ZG14_BRD4201A.ToString())
                        ? ChipSeries.ZG14_BRD4201A.ToString() : ChipSeries.ZG14_BRD4206A.ToString();
                }
            }

            if (!IsSkipCheckPortExists)
            {
                avail = RemoveUnavaliblePorts(avail.ToList());
            }

            if (IsAvailable(required, avail)
                && IsCompatibleByChipTypeAndLibrary(required, avail))
            {
                var table = GetCompatibilityByChipSeriesTable(required, avail);
                avail = avail.Where(x => table.Any(y => y.Item2 == x));
                var rr = required.Where(x => !table.Any(y => y.Item1 == x));
                if (avail.Count() >= required.Count && rr.Count() == 0)
                {
                    Parallel.ForEach(avail, host =>
                    {
                        var appLayer = IPAddress.TryParse(host.SName, out IPAddress addr1) ? TcpApplicationLayer : BasicApplicationLayer;
                        var manipulatorLayer = host.PName != null && IPAddress.TryParse(host.PName, out IPAddress addr2) ? TcpApplicationLayer : BasicApplicationLayer;
                        var hostClient = new DeviceHostClient(host, appLayer, manipulatorLayer);
                        if (hostClient.SClient != null)
                        {
                            if (hostClient.SClient.Connect(hostClient.SClient.DataSource) != CommunicationStatuses.Done)
                            {
                                if (!hostClient.Host.IsUZB)
                                {
                                    hostClient.SClient.Dispose();
                                    hostClient.SClient = null;
                                }
                            }
                            else
                            {
                                OnConnected?.Invoke(this, new EventArgs<SessionAttachment>(new SessionAttachment
                                {
                                    ApiType = (byte)ApiTypes.Basic,
                                    SessionId = hostClient.SClient.SessionId,
                                    Name = hostClient.SClient.DataSource.SourceName
                                }));
                            }
                        }
                        if (hostClient.PClient != null)
                        {
                            if (hostClient.PClient.Connect(hostClient.PClient.DataSource) != CommunicationStatuses.Done)
                            {
                                hostClient.PClient.Dispose();
                                hostClient.PClient = null;
                            }
                            else
                            {
                                OnConnected?.Invoke(this, new EventArgs<SessionAttachment>(new SessionAttachment
                                {
                                    ApiType = (byte)ApiTypes.Basic,
                                    SessionId = hostClient.PClient.SessionId,
                                    Name = hostClient.PClient.DataSource.SourceName
                                }));
                            }
                        }
                        if ((hostClient.HasSName && hostClient.SClient == null) || (hostClient.HasPName && hostClient.PClient == null))
                        {
                            FinalizeDevice(hostClient);
                        }
                        else
                            deviceHostClients.Add(hostClient);
                    });

                    var dhcClients = new List<DeviceHostClient>();
                    // reorder according available list
                    foreach (var item in available)
                    {
                        var tmp = deviceHostClients.FirstOrDefault(x => x.Host == item);
                        if (tmp != null)
                        {
                            dhcClients.Add(tmp);
                        }
                    }

                    if (configurationMode != ConfigurationModes.None)
                    {
                        _configurationMode = configurationMode;
                        InitDevicesPassive(dhcClients, required, configPath);
                    }
                    else
                    {
                        InitDevicesNew(dhcClients, required, isAppendMode);
                    }

                    Parallel.ForEach(dhcClients, item =>
                    {
                        FinalizeDevice(item);
                    });
                }
            }
        }

        private bool IsAvailable(List<UnknownDevice> required, IEnumerable<DeviceHost> avail)
        {
            bool ret = false;
            if (required != null && required.Count > 0)
            {
                if (avail != null && avail.Count() >= required.Count)
                {
                    ret = true;
                }
                else
                {
                    Log($"Requires {required.Count} devices, but available only {(avail != null ? avail.Count() : 0)}", LogLevels.Diagnostic);
                }
            }
            else
            {
                Log($"There are no required devices specified", LogLevels.Diagnostic);
            }
            return ret;
        }

        private bool IsCompatibleByChipTypeAndLibrary(IEnumerable<UnknownDevice> required, IEnumerable<DeviceHost> avail)
        {
            bool ret = false;
            foreach (var rcs in required.GroupBy(x => x.ChipSeries))
            {
                string[] list = null;
                if (rcs.Key == ChipSeries.Any)
                {
                    list = typeof(ChipSeries).GetEnumNames();
                }
                else if (rcs.Key == ChipSeries.ZW080x)
                {
                    list = new[]
                    {
                        ChipSeries.ZW080x,
                        ChipSeries.ZG23_BRD4204C,
                        ChipSeries.ZG23_BRD4204D,
                        ChipSeries.ZGM230S_BRD4205A,
                        ChipSeries.ZGM230S_BRD4205B,
                        ChipSeries.ZGM230_BRD2603A,
                        ChipSeries.ZG23_BRD4210A,
                        ChipSeries.xG28_BRD4400A,
                        ChipSeries.xG28_BRD4401A,
                        ChipSeries.xG28_BRD4400B,
                        ChipSeries.xG28_BRD4401B,
                        ChipSeries.xG28_BRD4400C,
                        ChipSeries.xG28_BRD4401C,
                        ChipSeries.xG28_BRD2705A,
                    }.Select(x => x.ToString()).ToArray();
                }
                else if (rcs.Key == ChipSeries.ZW070x)
                {
                    list = new[]
                    {
                        ChipSeries.ZW070x,
                        ChipSeries.ZG14_BRD4201A,
                        ChipSeries.ZG14_BRD4206A,
                        ChipSeries.ZG14_BRD4208A,
                        ChipSeries.ZGM130S_BRD4200A,
                        ChipSeries.ZGM130S_BRD4202A,
                        ChipSeries.ZGM130S_BRD4207A,
                        ChipSeries.ZG13L_BRD4201C,
                        ChipSeries.RZ13_BRD4201B,
                        ChipSeries.RZ13_BRD4203A,
                        ChipSeries.ZG13S_BRD4201D,
                        ChipSeries.RZ13_BRD4209A
                    }.Select(x => x.ToString()).ToArray();
                }
                else if (rcs.Key == ChipSeries.ZW0713)
                {
                    list = new[]
                    {
                        ChipSeries.ZGM130S_BRD4200A,
                        ChipSeries.ZGM130S_BRD4202A,
                        ChipSeries.ZGM130S_BRD4207A,
                        ChipSeries.ZG13L_BRD4201C,
                        ChipSeries.RZ13_BRD4201B,
                        ChipSeries.RZ13_BRD4203A,
                        ChipSeries.ZG13S_BRD4201D,
                        ChipSeries.RZ13_BRD4209A
                    }.Select(x => x.ToString()).ToArray();
                }
                else if (rcs.Key == ChipSeries.ZW0714)
                {
                    list = new[]
                    {
                        ChipSeries.ZG14_BRD4201A,
                        ChipSeries.ZG14_BRD4206A,
                        ChipSeries.ZG14_BRD4208A
                    }.Select(x => x.ToString()).ToArray();
                }
                else
                {
                    list = new[] { rcs.Key }.Select(x => x.ToString()).ToArray();
                }

                ret = true;
                if (avail.Where(a => list.Contains(a.ChipSeries)).Count() < rcs.Count())
                {
                    ret = false;
                    Log($"No enough available devices found - Requires {rcs.Count()} '{rcs.Key}', available {avail.Where(a => list.Contains(a.ChipSeries)).Count()} ", LogLevels.Diagnostic);
                    break;
                }
            }
            return ret;
        }

        internal void Reset(DeviceHostClient deviceHostClient, Alias deviceNumber)
        {
            var device = deviceHostClient.SClient as Device;
            DisableAssociations(deviceNumber);
            SendDataSettings.Clear(deviceNumber);
            SoftReset(deviceHostClient, deviceNumber);
            device.SetDefault();
            device.MemoryGetId();
            device.SerialApiGetInitData();
            if (device.Network.IsSupportedSerialApiCommand((byte)ZWave.BasicApplication.Enums.CommandTypes.CmdSerialApiGetLRNodes))
            {
                device.SerialApiGetLRNodes();
            }
            DisableSmartStart(deviceNumber);
            CancelSmartStartSupport(deviceNumber);
            EnableSmartStart(deviceNumber);
            RunSmartStartSupport(deviceNumber);
            IncludeSettings.Clear(); // clear after starting smart start to be able set custom inclusion settings
        }

        private void InitDevicesPassive(List<DeviceHostClient> availableList, List<UnknownDevice> required, string configPath)
        {
            var tempRemovedAvailableList = new ConcurrentBag<DeviceHostClient>();
            var tempRemovedRequiredList = new ConcurrentBag<UnknownDevice>();
            Parallel.ForEach(required, reqDevice =>
            {
                if (!string.IsNullOrWhiteSpace(reqDevice.Alias))
                {
                    var matched = availableList.FirstOrDefault(x => !string.IsNullOrEmpty(x.Host.Alias) && x.Host.Alias.Equals(reqDevice.Alias, StringComparison.InvariantCulture));
                    if (matched != null)
                    {
                        FillDeviceHostClient(matched);
                        if (matched.Library != Libraries.NoLib)
                        {
                            InitDeviceHostClient(matched);
                            AddOrReplaceDevice(reqDevice.DeviceAlias, matched);
                            tempRemovedRequiredList.Add(reqDevice);
                            tempRemovedAvailableList.Add(matched);
                            LoadConfigurationItem(matched, configPath);
                            InitNodes(matched);
                        }
                    }
                }
            });
            // update input arrays
            availableList.RemoveAll(x => tempRemovedAvailableList.Contains(x));
            required.RemoveAll(x => tempRemovedRequiredList.Contains(x));
        }

        internal bool Disconnect(DeviceHostClient deviceHostClient)
        {
            bool ret = false;
            if (deviceHostClient.SClient != null)
            {
                deviceHostClient.SClient.Disconnect();
            }
            return ret;

        }

        internal bool Connect(DeviceHostClient deviceHostClient)
        {
            bool ret = false;
            if (deviceHostClient.SClient != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (deviceHostClient.SClient.Connect() == CommunicationStatuses.Done)
                    {
                        ret = true;
                        break;
                    }
                    else
                    {
                        ResetSample(deviceHostClient);
                    }
                    Thread.Sleep(500);
                }
            }
            return ret;
        }

        internal bool Reconnect(DeviceHostClient deviceHostClient)
        {
            bool ret = false;
            if (deviceHostClient.SClient != null)
            {
                Disconnect(deviceHostClient);
                ret = Connect(deviceHostClient);
            }
            return ret;
        }

        #region Configuration

        public bool ConfigurationIsEmpty(Alias deviceNumber) => !HasConfiguration(deviceNumber) || Devices[deviceNumber].ConfigurationItem.IsEmpty;

        public ConfigurationItem GetConfiguration(Alias deviceNumber)
        {
            return Devices[deviceNumber].ConfigurationItem;
        }

        public void ConfigurationClear(Alias deviceNumber)
        {
            if (!HasConfiguration(deviceNumber) || ConfigurationReadOnly)
                return;

            var device = GetDevice(deviceNumber);
            var itemFileName = ConfigurationItem.GetItemFileName(device.Network.HomeId, (byte)device.Network.NodeTag.Id);
            File.Delete(itemFileName);
        }

        /// <summary>
        /// Temporary method to update CONFIGURATION if exists!
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="updatesConfigContent"></param>
        public void ConfigurationUpdate(Alias deviceNumber, bool updatesConfigContent = true)
        {
            if (!HasConfiguration(deviceNumber) || ConfigurationReadOnly)
                return;

            var deviceHostClient = GetDeviceHostClient(deviceNumber);
            var ctrlDevice = GetDevice(deviceNumber) as Controller;
            if (ctrlDevice != null)
            {
                var initDataRes = ctrlDevice.SerialApiGetInitData();
                if (ctrlDevice.Network.IsSupportedSerialApiCommand((byte)ZWave.BasicApplication.Enums.CommandTypes.CmdSerialApiGetLRNodes))
                {
                    ctrlDevice.SerialApiGetLRNodes();
                }
                var configItem = Devices[deviceNumber].ConfigurationItem;
                configItem.Node.Clear();

                var sm = (SecurityManager)(ctrlDevice.SessionClient.GetSubstituteManager(typeof(SecurityManager)));
                if (sm != null)
                    configItem.SecuritySettings.NetworkKeys = sm.SecurityManagerInfo.NetworkKeys;

                foreach (var node in ctrlDevice.IncludedNodes)
                {
                    configItem.AddOrUpdateNode(node);
                }

                if (updatesConfigContent && configItem.IsEmpty)
                    configItem.IsEmpty = false;
            }
        }

        private bool ConfigurationReadOnly => _configurationMode != ConfigurationModes.ReadWrite;

        private bool HasConfiguration(Alias deviceNumber) => Devices[deviceNumber].ConfigurationItem != null;

        private void LoadConfigurationItem(DeviceHostClient deviceHostClient, string configPath)
        {
            var device = deviceHostClient.SClient as Device;
            var configurationItem = ConfigurationItem.Load(device.Network, configPath);
            if (!configurationItem.IsEmpty)
            {
                if (configurationItem.SecuritySettings.NetworkKey != null)
                {
                    foreach (var item in configurationItem.SecuritySettings.NetworkKey)
                    {
                        configurationItem.SecuritySettings.NetworkKeys[item.Class] = item;
                    }
                }
                device.Network.IsCsaEnabled = configurationItem.SecuritySettings.IsCsaEnabled;
                device.Network.IsEnabledS0 = configurationItem.SecuritySettings.IsEnabledS0;
                device.Network.IsEnabledS2_UNAUTHENTICATED = configurationItem.SecuritySettings.IsEnabledS2_UNAUTHENTICATED;
                device.Network.IsEnabledS2_AUTHENTICATED = configurationItem.SecuritySettings.IsEnabledS2_AUTHENTICATED;
                device.Network.IsEnabledS2_ACCESS = configurationItem.SecuritySettings.IsEnabledS2_ACCESS;
            }
            deviceHostClient.ConfigurationItem = configurationItem;
        }

        private void ConfigurationAddOrUpdateNode(Alias deviceNumber, NodeTag node) => Devices[deviceNumber].ConfigurationItem?.AddOrUpdateNode(node);

        private void ConfigurationRemoveNode(Alias deviceNumber, NodeTag node) => Devices[deviceNumber].ConfigurationItem?.RemoveNode(node);

        private void InitNodes(DeviceHostClient deviceHostClient)
        {
            if (deviceHostClient.ConfigurationItem == null)
                return;

            var configurationItem = deviceHostClient.ConfigurationItem;
            var device = deviceHostClient.SClient as Device;
            var ctrlDevice = device as Controller;
            var brdgDevice = device as BridgeController;
            if (ctrlDevice != null)
            {
                configurationItem.FillNodesNetwork(ctrlDevice.IncludedNodes);
                var addedNodes = configurationItem.Node.ToArray();
                foreach (var node in addedNodes.Select(x => x.NodeTag))
                {
                    if (device.Network.GetNodeInfo(node).IsEmpty)
                    {
                        var infoRes = ctrlDevice.GetProtocolInfo(node);
                        if (brdgDevice != null)
                        {
                            var virtRes = brdgDevice.IsVirtualNode(node);
                            device.Network.SetVirtual(node, virtRes.RetValue);
                        }
                        device.Network.SetNodeInfo(node, infoRes.NodeInfo);
                    }
                }
            }
        }

        protected override void OnActionChangedUpdateConfigItem(object sender, EventArgs<ActionBase> e)
        {
            base.OnActionChangedUpdateConfigItem(sender, e);

            if (ConfigurationReadOnly)
                return;

            var actionDevice = Devices.FirstOrDefault(d => d.Value.SClient.SessionId == e.Value.SessionId);
            if (actionDevice.Key.Value == 0)
                return;

            if (actionDevice.Value.ConfigurationItem == null)
                return;

            Task.Run(() =>
            {
                if (e.Value.Token.IsStateActive)
                {
                    if (e.Value is SetLearnModeControllerOperation ||
                        e.Value is ControllerChangeOperation)
                    {
                        ConfigurationClear(actionDevice.Key);
                    }
                }
                else if (e.Value.IsStateCompleted)
                {
                    if (e.Value is AddNodeOperation)
                    {
                        var addRemoveResult = (AddRemoveNodeResult)e.Value.Token.Result;
                        if (addRemoveResult)
                        {
                            ConfigurationAddOrUpdateNode(actionDevice.Key, new NodeTag(addRemoveResult.Id));
                        }
                    }
                    else if (e.Value is InclusionTask)
                    {
                        var inclusionResult = (InclusionResult)e.Value.Token.Result;
                        if (inclusionResult)
                        {
                            ConfigurationAddOrUpdateNode(actionDevice.Key, new NodeTag(inclusionResult.AddRemoveNode.Id));
                        }
                    }
                    else if (e.Value is RemoveNodeOperation || e.Value is RemoveNodeIdOperation)
                    {
                        var addRemoveResult = (AddRemoveNodeResult)e.Value.Token.Result;
                        if (addRemoveResult)
                            ConfigurationRemoveNode(actionDevice.Key, addRemoveResult.Node);
                    }
                    else if (e.Value is ExclusionTask)
                    {
                        var exclusionResult = (ExclusionResult)e.Value.Token.Result;
                        if (exclusionResult)
                            ConfigurationRemoveNode(actionDevice.Key, exclusionResult.AddRemoveNode.Node);
                    }
                    else if (e.Value is SetLearnModeControllerOperation)
                    {
                        var setLearnModeResult = (SetLearnModeResult)e.Value.Token.Result;
                        if (setLearnModeResult)
                            ConfigurationUpdate(actionDevice.Key);
                    }
                    else if (e.Value is SetLearnModeEndDeviceOperation)
                    {
                        var setLearnModeResult = (SetLearnModeResult)e.Value.Token.Result;
                        if (setLearnModeResult && setLearnModeResult.LearnModeStatus != LearnModeStatuses.None)
                        {
                            if (setLearnModeResult.LearnModeStatus == LearnModeStatuses.Removed)
                                ConfigurationRemoveNode(actionDevice.Key, setLearnModeResult.Node);
                            else
                                ConfigurationAddOrUpdateNode(actionDevice.Key, setLearnModeResult.Node);
                        }
                    }
                    else if (e.Value is SetSucNodeIdOperation)
                    {
                        if (e.Value.Token.Result)
                        {
                            ConfigurationUpdate(actionDevice.Key);
                            var sucNodeId = (actionDevice.Value.SClient as Device).SucNodeId;
                            var newSucDevice = Devices.FirstOrDefault(d => (d.Value.SClient as Device).Network.NodeTag.Id == sucNodeId);
                            if (newSucDevice.Key.Value > 0 && newSucDevice.Key != actionDevice.Key)
                                ConfigurationUpdate(newSucDevice.Key);
                        }
                    }
                    else if (e.Value is RequestNetworkUpdateOperation)
                    {
                        var updateRes = (RequestNetworkUpdateResult)e.Value.Token.Result;
                        if (updateRes)
                            ConfigurationUpdate(actionDevice.Key);
                    }
                    else if (e.Value is ControllerChangeOperation)
                    {
                        var addRemoveResult = (AddRemoveNodeResult)e.Value.Token.Result;
                        if (addRemoveResult)
                        {
                            ConfigurationUpdate(actionDevice.Key);
                        }
                    }
                    else if (e.Value is IsFailedNodeOperation)
                    {
                        var isFailedNodeResult = (IsFailedNodeResult)e.Value.Token.Result;
                        ConfigurationAddOrUpdateNode(actionDevice.Key, isFailedNodeResult.Node);
                    }
                    else if (e.Value is RequestNodeInfoOperation)
                    {
                        var requestNodeInfoResult = (RequestNodeInfoResult)e.Value.Token.Result;
                        ConfigurationAddOrUpdateNode(actionDevice.Key, requestNodeInfoResult.Node);
                    }
                    else if (e.Value is SetDefaultOperation)
                    {
                        ConfigurationClear(actionDevice.Key);
                        ConfigurationUpdate(actionDevice.Key, false);
                    }
                }
            });
        }

        #endregion

        public static IEnumerable<DeviceHost> RemoveUnavaliblePorts(List<DeviceHost> available)
        {
            var pNames = SerialPortTransportLayer.GetDeviceNames();
            return available.Where(x =>
                    x.SType == InterfaceTypes.TCP ||
                    (string.IsNullOrEmpty(x.SName) || pNames.Contains(x.SName)) &&
                    (string.IsNullOrEmpty(x.PName) || pNames.Contains(x.PName)));
        }

        public void LogNetworkKeys(Alias controllerNumber, NodeTag node)
        {
            Controller controller = (Controller)GetDevice(controllerNumber);
            LogNetworkKeys(controller, node);
        }

        public void LogNetworkKeys(Controller controller, NodeTag node)
        {
            var schemes = controller.Network.GetSecuritySchemes(node);
            if (schemes != null)
            {
                foreach (var scheme in SecuritySchemeSet.ALL)
                {
                    if (schemes.Contains((ZWave.Enums.SecuritySchemes)scheme))
                    {
                        Log($"{scheme}: ".Replace("S2_", "").PadRight(17, ' ') + GetLastTempNetworkKey(controller, scheme, controller.Network.IsLongRange(node))?.ToHexString(), LogLevels.Information);
                    }
                }
            }
            Log($"{SecuritySchemes.S2_TEMP}:         " + GetLastTempNetworkKey(controller, SecuritySchemes.S2_TEMP, false)?.ToHexString(), LogLevels.Information);
        }

        internal int ProgramBootLoader(DeviceHostClient deviceHostClient, BootloaderPath bootLoaderPath)
        {
            return _gammaProgrammer.ProgramBootLoader(deviceHostClient, bootLoaderPath);
        }

        public void ResetAllBoards(Profile activeProfile)
        {
            //Reset Boards
            IEnumerable<DeviceHost> avail = RemoveUnavaliblePorts(activeProfile);
            Parallel.ForEach(avail.Where(x => !string.IsNullOrEmpty(x.PName) || !string.IsNullOrEmpty(x.JName)), host =>
            {
                if (string.IsNullOrEmpty(host.JName))
                {
                    if (_alphaProgrammer != null)
                    {
                        var dhc = new DeviceHostClient(host, BasicApplicationLayer, null);
                        dhc.PClient = BasicApplicationLayer.CreateTestInterfaceDevice(dhc.PClient);
                        _alphaProgrammer.ResetSample(dhc);
                        FinalizeDevice(dhc);
                    }
                }
                else
                {
                    if (_gammaProgrammer != null)
                    {
                        var dhc = new DeviceHostClient(host);
                        _gammaProgrammer.ResetSample(dhc);
                        FinalizeDevice(dhc);
                    }
                }
            });
        }

        public int ResetSamples(IEnumerable<DeviceHostClient> deviceHostClients)
        {
            int ret = 0;
            var dt = DateTime.Now;
            Parallel.ForEach(deviceHostClients, x =>
            {
                if (ResetSample(x) == 0)
                {
                    Interlocked.Increment(ref ret);
                }
            });
            $"ResetSamples completed in {(DateTime.Now - dt).TotalSeconds} sec. "._DLOG();
            return ret;
        }

        public int ResetSample(DeviceHostClient deviceHostClient)
        {
            var exitCode = 1;
            if (deviceHostClient.HasJName)
            {
                if (_gammaProgrammer != null)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        exitCode = _gammaProgrammer.ResetSample(deviceHostClient);
                        if (exitCode == 0)
                        {
                            break;
                        }
                        Thread.Sleep(200);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(deviceHostClient.Host.PName))
            {
                if (_alphaProgrammer != null)
                {
                    exitCode = _alphaProgrammer.ResetSample(deviceHostClient);
                }
            }
            if (IsSerialApi(deviceHostClient.Library) && deviceHostClient.SClient is Device)
            {
                if (!IsOptionDisabled(ZWaveSet.NodeIdBase2, deviceHostClient.DeviceAlias))
                {
                    ((Device)deviceHostClient.SClient).SerialApiSetNodeIdBaseType(2);
                }
            }
            return exitCode;
        }

        public int ProgramSample(DeviceHostClient deviceHostClient, ZWaveRegions region, string samplePath, byte[] prk, byte[] puk)
        {
            var exitCode = 0;
            if (string.IsNullOrEmpty(deviceHostClient.Host.JName))
            {
                if (_alphaProgrammer != null)
                {
                    exitCode = _alphaProgrammer.ProgramSample(deviceHostClient, region, samplePath, true, prk, puk);
                }
            }
            else
            {
                BootloaderPath bootloaderPath = FileSystemManager.GetBootloaderPath(deviceHostClient, null);
                if (_gammaProgrammer != null)
                {
                    exitCode = _gammaProgrammer.ProgramSampleWithBootLoaderImageAndKeys(deviceHostClient, region, samplePath, bootloaderPath);
                }
            }
            if (deviceHostClient.SClient != null && IsSerialApi(deviceHostClient.Library))
            {
                var isVersionDetected = false;
                for (int i = 0; i < 2 && !isVersionDetected; i++)
                {
                    if (i > 0 && _gammaProgrammer != null)
                    {
                        (_gammaProgrammer as ProgrammingGammaProvider).BoardHardReset(deviceHostClient);
                        Reconnect(deviceHostClient);
                    }
                    var ver = new VersionOperation();
                    ver.Token.LogEntryPointSource = deviceHostClient.SClient.DataSource == null ? "" : deviceHostClient.SClient.DataSource.SourceName;
                    var verRes = (VersionResult)deviceHostClient.SClient.SessionClient.ExecuteAsync(ver).WaitCompletedSignal();
                    if (verRes)
                    {
                        deviceHostClient.Library = verRes.Library;
                        isVersionDetected = true;
                    }
                }
                if (!isVersionDetected)
                {
                    $"no firmware at {deviceHostClient.SClient.DataSource}"._DLOG();
                }
            }
            return exitCode;
        }


        public int ProgramSampleWithBootLoaderKeys(DeviceHostClient deviceHostClient, ZWaveRegions region, string samplePath, BootloaderPath bootloaderPath)
        {
            var exitCode = 0;
            if (string.IsNullOrEmpty(deviceHostClient.Host.JName))
            {
                if (_alphaProgrammer != null)
                {
                    exitCode = _alphaProgrammer.ProgramSampleWithBootLoaderImageAndKeys(deviceHostClient, region, samplePath, bootloaderPath);
                }
            }
            else
            {
                if (_gammaProgrammer != null)
                {
                    exitCode = _gammaProgrammer.ProgramSampleWithBootLoaderImageAndKeys(deviceHostClient, region, samplePath, bootloaderPath);
                }
            }
            if (deviceHostClient.SClient != null && IsSerialApi(deviceHostClient.Library))
            {
                var ver = new VersionOperation();
                ver.Token.LogEntryPointSource = deviceHostClient.SClient.DataSource == null ? "" : deviceHostClient.SClient.DataSource.SourceName;
                var verRes = (VersionResult)deviceHostClient.SClient.SessionClient.ExecuteAsync(ver).WaitCompletedSignal();
                if (verRes)
                {
                    deviceHostClient.Library = verRes.Library;
                }
                else
                {
                    $"no firmware at {deviceHostClient.SClient.DataSource}"._DLOG();
                }
            }
            return exitCode;
        }

        public int ProgramSampleWithBootLoaderImageAndKeys(DeviceHostClient deviceHostClient, ZWaveRegions region, string samplePath, BootloaderPath bootloaderPath)
        {
            var exitCode = 0;
            if (string.IsNullOrEmpty(deviceHostClient.Host.JName))
            {
                if (_alphaProgrammer != null)
                {
                    exitCode = _alphaProgrammer.ProgramSampleWithBootLoaderImageAndKeys(deviceHostClient, region, samplePath, bootloaderPath);
                }
            }
            else
            {
                if (_gammaProgrammer != null)
                {
                    exitCode = _gammaProgrammer.ProgramSampleWithBootLoaderImageAndKeys(deviceHostClient, region, samplePath, bootloaderPath);
                }
            }
            if (deviceHostClient.SClient != null && IsSerialApi(deviceHostClient.Library))
            {
                var ver = new VersionOperation();
                ver.Token.LogEntryPointSource = deviceHostClient.SClient.DataSource == null ? "" : deviceHostClient.SClient.DataSource.SourceName;
                var verRes = (VersionResult)deviceHostClient.SClient.SessionClient.ExecuteAsync(ver).WaitCompletedSignal();
                if (verRes)
                {
                    deviceHostClient.Library = verRes.Library;
                }
                else
                {
                    $"no firmware at {deviceHostClient.SClient.DataSource}"._DLOG();
                }
            }
            return exitCode;
        }

        public int RenewDsk(DeviceHostClient deviceHostClient)
        {
            var exitCode = 0;
            if (!string.IsNullOrEmpty(deviceHostClient.Host.JName))
            {
                if (_gammaProgrammer != null)
                {
                    exitCode = _gammaProgrammer.RenewDsk(deviceHostClient);
                }
            }
            return exitCode;
        }
        public int SetHomeID(DeviceHostClient deviceHostClient, byte[] homeId)
        {
            var exitCode = 0;
            if (string.IsNullOrEmpty(deviceHostClient.Host.JName))
            {
                if (_alphaProgrammer != null)
                {
                    exitCode = _alphaProgrammer.SetHomeId(deviceHostClient, homeId);
                }
            }
            return exitCode;
        }

        public int BackUpSample(DeviceHostClient deviceHostClient, string backUpPath)
        {
            var exitCode = 0;

            exitCode = _alphaProgrammer.BackUpSample(deviceHostClient, backUpPath);

            return exitCode;
        }

        //string[] notImplementedPortableControllerVersions = new[] { "_7.11.", "_7.12.", "_7.13.", "_7.14.", "_7.15.", "_7.16.", "_7.17." };
        public List<DeviceHostClient> HostsWithKnownVersion = new List<DeviceHostClient>();

        public void InitDevicesNew(List<DeviceHostClient> available, List<UnknownDevice> required, bool isAppendMode)
        {
            bool isNef = false; // not enough devices
            HostsWithKnownVersion.Clear();

            if (available.Count >= required.Count)
            {
                var devices2Init = new List<Tuple<UnknownDevice, DeviceHostClient, int>>();
                var devices2Append = new List<Tuple<UnknownDevice, DeviceHostClient, int>>();
                var compatibilityTable = GetCompatibilityTable(required, available);
                var availableHops = compatibilityTable
                    .Where(x => !string.IsNullOrWhiteSpace(x.Item2.Host.Hop))
                    .Select(x => x.Item2.Host.Hop)
                    .Distinct().ToList();
                var checkHops = availableHops.Count() > 0;
                while (compatibilityTable.Count > 0)
                {
                    var matchedPairsGroup = compatibilityTable.GroupBy(x => x.Item1).
                        OrderBy(x => x.Key.IsTestInterface).
                        ThenBy(x => x.Count()).
                        ThenByDescending(x => x.Max(y => y.Item3)).
                        ThenBy(x => !string.IsNullOrEmpty(x.Key.Alias)).
                        ThenBy(x => !string.IsNullOrEmpty(x.Key.Hop)).
                        ThenByDescending(x => x.Key.ChipSeries).
                        FirstOrDefault();
                    var matchedPair = matchedPairsGroup.
                        OrderBy(x => x.Item2.HasPName).
                        ThenBy(x => GetChipSeriesPickPriority(x.Item2.Host.ChipSeries, x.Item1.Libraries.FirstOrDefault())).
                        ThenByDescending(x => x.Item3).
                        FirstOrDefault();
                    if (matchedPair != null)
                    {
                        var useHop = checkHops ? (availableHops.Count() > 0 && availableHops.Contains(matchedPair.Item2.Host.Hop)) || availableHops.Count() == 0 : true;
                        if (AdjustSerialApiLibraries(matchedPair) && matchedPair.Item1.DevKit == DevKits.Default && useHop)
                        {
                            devices2Init.Add(matchedPair);
                            compatibilityTable.RemoveAll(x => x.Item2 == matchedPair.Item2);
                            compatibilityTable.RemoveAll(x => x.Item1 == matchedPair.Item1);
                            if (!string.IsNullOrEmpty(matchedPair.Item2.Host.Hop) &&
                                availableHops.Contains(matchedPair.Item2.Host.Hop))
                            {
                                availableHops.Remove(matchedPair.Item2.Host.Hop);
                            }
                        }
                        else if (isAppendMode && (matchedPair.Item2.HasJName || matchedPair.Item2.HasPName) && useHop)
                        {
                            matchedPair.Item2.Library = matchedPair.Item1.Libraries[0];
                            devices2Append.Add(matchedPair);
                            compatibilityTable.RemoveAll(x => x.Item2 == matchedPair.Item2);
                            compatibilityTable.RemoveAll(x => x.Item1 == matchedPair.Item1);
                            if (!string.IsNullOrEmpty(matchedPair.Item2.Host.Hop) &&
                                availableHops.Contains(matchedPair.Item2.Host.Hop))
                            {
                                availableHops.Remove(matchedPair.Item2.Host.Hop);
                            }
                        }
                        else
                        {
                            compatibilityTable.RemoveAll(x => x.Item1 == matchedPair.Item1 && x.Item2 == matchedPair.Item2);
                        }
                    }
                    else
                    {
                        isNef = true;
                        break; // not enough devices
                    }
                }
                if (devices2Init.Count + devices2Append.Count >= required.Count)
                {
                    if (isAppendMode && !isNef)
                    {
                        byte[] prk = null;
                        byte[] puk = null;
                        if (CurrentProfile.IsAddS2Keys)
                        {
                            try
                            {
                                prk = Tools.GetBytes(CurrentProfile.PrivateS2Key);
                                puk = Tools.GetBytes(CurrentProfile.PublicS2Key);
                            }
                            catch { }
                        }

                        Parallel.ForEach(devices2Append, pair =>
                        {
                            var isUzb = pair.Item1.IsTestInterface ? false : pair.Item2.Host.IsUZB;
                            List<string> filters = new List<string>();
                            filters.Add(isUzb ? IncludeUsbVcpRegexFilter : ExcludeUsbVcpRegexFilter);
                            if (!string.IsNullOrWhiteSpace(pair.Item1.FileNameFilter))
                            {
                                filters.Add(pair.Item1.FileNameFilter);
                            }
                            filters.Add(".hex");
                            var chipSeries = (ChipSeries)Enum.Parse(typeof(ChipSeries), pair.Item2.Host.ChipSeries);
                            string samplePath = GetLatestFilePath(pair.Item1.Libraries, chipSeries, pair.Item1.Frequency, null, pair.Item1.DevKit, filters.ToArray());
                            if (!string.IsNullOrEmpty(samplePath))
                            {
                                // init after overwrite - set to default
                                //pair.Item1.DevKit = DevKits.Default;
                                //device.ChipSeries = ChipSeries.Default;
                                var region = GetRegion(pair.Item1.Frequency);
                                int exitCode = ProgramSample(pair.Item2, region, samplePath, prk, puk);
                                if (exitCode == 0)
                                {
                                    var pName = pair.Item2.Host.JName ?? pair.Item2.Host.PName;
                                    lock (ZatsCache)
                                    {
                                        var progRes = ZatsCache.ProgrammingResults.FirstOrDefault(x => x.PName == pName);
                                        if (progRes == null)
                                        {
                                            progRes = new ProgrammingResult();
                                            progRes.PName = pName;
                                            ZatsCache.ProgrammingResults.Add(progRes);
                                        }
                                        progRes.ModifiedAt = DateTime.Now;
                                        progRes.AppName = pair.Item1.Libraries[0].ToString();
                                        progRes.Frequency = pair.Item1.Frequency;
                                        progRes.DevKit = pair.Item1.DevKit.ToString();
                                        progRes.CmdLine = samplePath;
                                    }
                                    FillDeviceHostClient(pair.Item2);
                                    lock (devices2Init)
                                    {
                                        devices2Init.Add(pair);
                                    }
                                }
                                else
                                {
                                    Log($"Failed to program device {pair.Item2.Host.JName ?? pair.Item2.Host.PName} with {samplePath}", LogLevels.Diagnostic);
                                }
                            }
                            else
                            {
                                Log($"{pair.Item1.Libraries[0]} file path not found", LogLevels.Diagnostic);
                            }
                        });
                    }

                    foreach (var pair in devices2Init)
                    {
                        var deviceHostClient = pair.Item2;
                        required.Remove(pair.Item1);
                        available.Remove(deviceHostClient);
                        deviceHostClient.DevKit = pair.Item1.DevKit;
                        deviceHostClient.Frequency = pair.Item1.Frequency;
                        AddOrReplaceDevice(pair.Item1.DeviceAlias, deviceHostClient);
                        if (HostsWithKnownVersion.Contains(deviceHostClient))
                        {
                            HostsWithKnownVersion.Remove(deviceHostClient);
                        }
                    }

                    if (required.Count == 0)
                    {
                        Parallel.ForEach(devices2Init.Select(x => x.Item2), (deviceHostClient) =>
                        {
                            InitDeviceHostClient(deviceHostClient);
                        });
                    }
                }
                else
                {
                    foreach (var item in devices2Init)
                    {
                        Log($"   {item.Item2.SClient?.DataSource}-{item.Item2.SClient?.Library}-{item.Item2.Host.ChipSeries} picked as {item.Item1.Libraries?.DefaultIfEmpty().FirstOrDefault()}-{item.Item1.ChipSeries}  (rank {item.Item3})", LogLevels.Diagnostic);
                    }
                    foreach (var item in devices2Append)
                    {
                        Log($"   {item.Item2.SClient?.DataSource}-{item.Item2.SClient?.Library}-{item.Item2.Host.ChipSeries} picked as {item.Item1.Libraries?.DefaultIfEmpty().FirstOrDefault()}-{item.Item1.ChipSeries}  (rank {item.Item3})", LogLevels.Diagnostic);
                    }
                    foreach (var item in required.Where(x => !devices2Init.Any(y => y.Item1 == x) && !devices2Append.Any(y => y.Item1 == x)))
                    {
                        var hopInfo = item.HasHop ? $" in the Hop={item.Hop}" : string.Empty;
                        Log($"   There is no device found for {item.Libraries?.DefaultIfEmpty().FirstOrDefault()}-{item.ChipSeries}{hopInfo}", LogLevels.Diagnostic);
                    }
                }
            }
        }

        public string GetLatestFilePath(Libraries[] libs, ChipSeries chipSeries, string freq, string ending, DevKits devKit, params string[] regexFilters)
        {
            string samplePath = null;
            if (!new[] { ChipSeries.ZW010x, ChipSeries.ZW020x, ChipSeries.ZW030x, ChipSeries.ZW040x, ChipSeries.ZW040x }.Contains(chipSeries))
            {
                libs = AdjustRequiredLibraries(libs, chipSeries);
                foreach (var frequency in new[] { freq, "EU", "US_LR", "EU_LR" })
                {
                    samplePath = FileSystemManager.GetLatestFilePath(libs[0], chipSeries.ToString(), frequency, ending, devKit, regexFilters);
                    if (samplePath == null && libs[0] == Libraries.ControllerPortableLib)
                    {
                        var controllerBridgePath = FileSystemManager.GetLatestFilePath(Libraries.ControllerBridgeLib, chipSeries.ToString(), frequency, ending, devKit, regexFilters);
                        if (controllerBridgePath != null /*&& notImplementedPortableControllerVersions.Any(x => controllerBridgePath.Contains(x))*/)
                        {
                            libs[0] = Libraries.ControllerBridgeLib;
                            samplePath = controllerBridgePath;
                        }
                    }
                    if (File.Exists(samplePath))
                    {
                        break;
                    }
                }
            }
            else
            {
                samplePath = FileSystemManager.GetLatestFilePath(libs[0], chipSeries.ToString(), freq, ending, devKit, regexFilters);
            }

            return samplePath;
        }

        public ZWaveRegions GetRegion(string frequency)
        {
            ZWaveRegions ret = ZWaveRegions.REGION_DEFAULT;
            if (!string.IsNullOrWhiteSpace(frequency))
            {
                Enum.TryParse($"REGION_{frequency}", out ZWaveRegions region);
                ret = region;
            }
            return ret;
        }

        private int GetChipSeriesPickPriority(string chipSeriesInput, Libraries libraries)
        {
            byte priority = byte.MaxValue;

            if (Enum.TryParse(chipSeriesInput, out ChipSeries chipSeries)
                && IsSerialApiController(libraries)
                ? ChipSeriesWithControllerPriority.Contains(chipSeries)
                : ChipSeriesWithSampleAppPriority.Contains(chipSeries))
            {
                priority = 0;
            }
            return priority;
        }

        private void FillDeviceHostClient(DeviceHostClient deviceHostClient)
        {
            deviceHostClient.Library = Libraries.NoLib;
            var version = string.Empty;
            var chipType = GetChipTypeFromString(deviceHostClient.Host.ChipSeries);
            var pName = deviceHostClient.Host.JName ?? deviceHostClient.Host.PName;
            ProgrammingResult progResult = null;
            lock (ZatsCache)
            {
                progResult = ZatsCache.ProgrammingResults.FirstOrDefault(pr => !string.IsNullOrEmpty(pName) && pr.PName == pName);
            }
            if (progResult != null)
            {
                Libraries lib = Libraries.NoLib;
                Enum.TryParse(progResult.AppName, out lib);
                deviceHostClient.Library = lib;
                deviceHostClient.DevKit = !string.IsNullOrWhiteSpace(progResult.DevKit) ? (DevKits)Enum.Parse(typeof(DevKits), progResult.DevKit) : DevKits.Default;
                deviceHostClient.Frequency = progResult.Frequency;
                deviceHostClient.FileName = progResult.CmdLine;
            }
            if (deviceHostClient.SClient != null && (deviceHostClient.Library == Libraries.NoLib || IsSerialApi(deviceHostClient.Library)))
            {
                var ver = new VersionOperation(true);
                ver.Token.LogEntryPointSource = deviceHostClient.SClient.DataSource == null ? "" : deviceHostClient.SClient.DataSource.SourceName;
                var verRes = (VersionResult)deviceHostClient.SClient.SessionClient.ExecuteAsync(ver).WaitCompletedSignal();
                int retry = 0;
                while (!verRes && retry++ < 3)
                {
                    Thread.Sleep(200);
                    ver = new VersionOperation(true);
                    ver.Token.LogEntryPointSource = deviceHostClient.SClient.DataSource == null ? "" : deviceHostClient.SClient.DataSource.SourceName;
                    verRes = (VersionResult)deviceHostClient.SClient.SessionClient.ExecuteAsync(ver).WaitCompletedSignal();
                    $"retry {retry}: VersionGet at {deviceHostClient.SClient.DataSource}"._DLOG();
                }
                if (verRes)
                {
                    version = verRes.Version;
                    deviceHostClient.Library = verRes.Library;
                }
                else
                {
                    $"no firmware at {deviceHostClient.SClient.DataSource}"._DLOG();
                    lock (ZatsCache)
                    {
                        ZatsCache.ProgrammingResults.Remove(progResult);
                    }
                    deviceHostClient.Library = Libraries.NoLib;
                    deviceHostClient.DevKit = DevKits.Default;
                    deviceHostClient.Frequency = null;
                    deviceHostClient.FileName = null;
                }
            }
            IApplicationClient client = null;
            switch (deviceHostClient.Library)
            {
                case Libraries.RailTest:
                    var textClient = TextApplicationLayer.CreateTextDevice(deviceHostClient.SClient);
                    textClient.IsAllowEmptyResponse = true;
                    client = textClient;
                    (_gammaProgrammer as ProgrammingGammaProvider)?.BoardHardReset(deviceHostClient);
                    Reconnect(deviceHostClient);
                    deviceHostClient.SClient = client;
                    break;
                case Libraries.DoorLockKeyPad:
                case Libraries.LEDBulb:
                case Libraries.PowerStrip:
                case Libraries.SensorPIR:
                case Libraries.SwitchOnOff:
                case Libraries.WallController:
                case Libraries.MultilevelSensor:
                    if (_useCLIforEndDevice && _ZW080xList.Select(x => x.ToString()).Contains(deviceHostClient.Host.ChipSeries))
                    {
                        SilinkManager.Init(deviceHostClient);
                    }
                    else if (deviceHostClient.PClient != null)
                    {
                        client = BasicApplicationLayer.CreateTestInterfaceDevice(deviceHostClient.PClient);
                        deviceHostClient.PClient = client;
                    }
                    break;
                case Libraries.InstallerLib:
                case Libraries.ControllerStaticLib:
                case Libraries.ControllerPortableLib:
                    client = BasicApplicationLayer.CreateController(deviceHostClient.SClient);
                    deviceHostClient.SClient = client;
                    break;
                case Libraries.EndDeviceSysTestLib:
                case Libraries.EndDeviceLib:
                case Libraries.EndDeviceLegacyLib:
                case Libraries.EndDeviceLegacyRoutingLib:
                    client = BasicApplicationLayer.CreateEndDevice(deviceHostClient.SClient);
                    deviceHostClient.SClient = client;
                    break;
                case Libraries.ControllerBridgeLib:
                case Libraries.TestController:
                    client = BasicApplicationLayer.CreateBridgeController(deviceHostClient.SClient);
                    deviceHostClient.SClient = client;
                    break;
                case Libraries.ZnifferPTI:
                    var isOpen = deviceHostClient.SClient?.TransportClient.IsOpen;
                    var ds = deviceHostClient.SClient?.TransportClient.DataSource;
                    deviceHostClient.SClient?.Dispose();
                    client = SnifferPtiApplicationLayer.CreateSnifferPti();
                    deviceHostClient.SClient = client;
                    if (isOpen.HasValue && isOpen.Value)
                    {
                        deviceHostClient.SClient.Connect(new SocketDataSource(ds.SourceName, 4905));
                    }
                    break;
                default:
                    break;
            }
            if (client != null)
            {
                client.Library = deviceHostClient.Library;
                client.Version = version;
                client.ChipType = chipType;
                lock (HostsWithKnownVersion)
                {
                    HostsWithKnownVersion.Add(deviceHostClient);
                }
            }
        }

        private ChipTypes GetChipTypeFromString(string chipSeries)
        {
            ChipTypes ret = ChipTypes.UNKNOWN;
            if (!Enum.TryParse(chipSeries, out ret))
            {
                throw new ArgumentException($"Chip series '{chipSeries}' not defined");
            }
            return ret;
        }

        private void InitDeviceHostClient(DeviceHostClient item)
        {
            var client = item.SClient as Device;
            var cmdClasses = new byte[] { COMMAND_CLASS_BASIC.ID };
            if (client != null)
            {
                client.SessionClient.AddSubstituteManager(new SettingsSubstituteManager(client.Network));
                var deviceOption = ZWave.Enums.DeviceOptions.Listening;
                var genericType = (byte)GenericTypes.STATIC_CONTROLLER;
                var specificType = SpecificTypes.PC_CONTROLLER;

                switch (client.Library)
                {
                    case Libraries.InstallerLib:
                    case Libraries.ControllerStaticLib:
                    case Libraries.ControllerBridgeLib:
                        deviceOption = ZWave.Enums.DeviceOptions.Listening;
                        genericType = (byte)GenericTypes.STATIC_CONTROLLER;
                        specificType = SpecificTypes.PC_CONTROLLER;
                        break;
                    case Libraries.ControllerPortableLib:
                        deviceOption = ZWave.Enums.DeviceOptions.NoneListening;
                        genericType = (byte)GenericTypes.GENERIC_CONTROLLER;
                        specificType = SpecificTypes.PORTABLE_REMOTE_CONTROLLER;
                        break;
                    case Libraries.EndDeviceLib:
                    case Libraries.EndDeviceLegacyLib:
                    case Libraries.EndDeviceLegacyRoutingLib:
                    case Libraries.TestController:
                        deviceOption = ZWave.Enums.DeviceOptions.Listening;
                        genericType = (byte)GenericTypes.SWITCH_MULTILEVEL;
                        specificType = SpecificTypes.NOT_USED;
                        break;
                    default:
                        break;
                }
                PresetSerialAPIDevice(item, client, cmdClasses, deviceOption, genericType, specificType);
            }
        }

        private void PresetSerialAPIDevice(DeviceHostClient item, Device client, byte[] cmdClasses, ZWave.Enums.DeviceOptions deviceOption, byte genericType, byte specificType)
        {
            if (!IsOptionDisabled(ZWaveSet.NodeIdBase2, item.DeviceAlias))
            {
                client.SerialApiSetNodeIdBaseType(2);
            }
            client.MemoryGetId();
            client.SerialApiGetInitData();
            client.SerialApiGetLRNodes();
            client.ApplicationNodeInformation(deviceOption, genericType, specificType, cmdClasses);
            client.Network.SetApplicationNodeInformation(deviceOption, genericType, specificType, cmdClasses);
            client.SerialApiGetCapabilities();
            if (client.Network.IsSupportedSerialApiCommand((byte)ZWave.BasicApplication.Enums.CommandTypes.CmdSerialApiSetup))
            {
                if (Enum.TryParse(item.Frequency, out ZWave.BasicApplication.Enums.RfRegions rfRegion))
                {
                    client.SetRfRegion(rfRegion);
                    client.SoftReset(); // After serialAPI Setup required! impacts on max Payload size
                }
                client.GetMaxPayloadSize();
                client.GetMaxLRPayloadSize();
            }
        }

        private List<Tuple<UnknownDevice, DeviceHost, int>> GetCompatibilityByChipSeriesTable(IEnumerable<UnknownDevice> requiredList, IEnumerable<DeviceHost> availableList)
        {
            DateTime dt = DateTime.Now;
            var сompatibilityTable = new List<Tuple<UnknownDevice, DeviceHost, int>>();
            foreach (var required in requiredList)
            {
                // Filter by chip series
                var candidates = availableList
                    .Where(x => IsCompatibleChipSeries(x.ChipSeries, required.ChipSeries, required.Libraries)).ToList();

                // Filter by interface type
                candidates = availableList
                    .Where(x => required.InterfaceType == InterfaceTypes.COM || required.InterfaceType == x.SType).ToList();

                $"GetCompatibilityTable1 for {required.ToString()}:"._DLOG();
                foreach (var item in candidates.GroupBy(x => x).Select(x => new { val = x.Key, count = x.Count() }).OrderBy(x => x.count))
                {
                    $"  -({item.count}) ChipSeries={item.val.ChipSeries}, SName={item.val.SName}, PName={item.val.PName}, JName={item.val.JName};"._DLOG();
                    сompatibilityTable.Add(new Tuple<UnknownDevice, DeviceHost, int>(required, item.val, item.count));
                }
            }
            $"GetCompatibilityTable1 completed in {(DateTime.Now - dt).TotalSeconds} sec. "._DLOG();
            return сompatibilityTable;
        }

        private List<Tuple<UnknownDevice, DeviceHostClient, int>> GetCompatibilityTable(IEnumerable<UnknownDevice> requiredList, IEnumerable<DeviceHostClient> availableList)
        {
            DateTime dt = DateTime.Now;
            Parallel.ForEach(availableList, (deviceHostClient) =>
            {
                FillDeviceHostClient(deviceHostClient);
            });
            var сompatibilityTable = new List<Tuple<UnknownDevice, DeviceHostClient, int>>();
            foreach (var required in requiredList)
            {
                // Get programmable hosts
                var candidates = availableList.Where(x => x.HasPName || x.HasJName).OrderBy(x => x.HasPName).ToList();

                // Get existing library/frequency/devkit/filenamefilter
                candidates.AddRange(availableList
                    .Where(x => required.Libraries.Contains(x.Library))
                    .Where(x => string.IsNullOrWhiteSpace(x.Frequency) || required.Frequency == x.Frequency)
                    .Where(x => required.DevKit == x.DevKit)
                    .Where(x => !string.IsNullOrWhiteSpace(required.FileNameFilter) ? new Regex(required.FileNameFilter, RegexOptions.IgnoreCase).Match(x.FileName).Success : true));

                if (!_useCLIforEndDevice ||
                    !(_useCLIforEndDevice && _ZW080xList.Contains(required.ChipSeries))) //Skip Manipulator's requirement for 800s with CLI enabled.
                {
                    //Filter for TI required to use Manipulator boards togher with Sample app's board (in case of CLI we don't need it).
                    candidates = candidates
                        .Where(x => (required.IsTestInterface && x.HasPName) || (!required.IsTestInterface && x.HasSName)).ToList();
                }

                // Filter by alias
                if (required.HasAlias && availableList.Where(x => x.Host.Alias == required.Alias).Any())
                {
                    candidates = availableList
                        .Where(x => x.Host.Alias == required.Alias).ToList();
                }

                // Filter by HOP name
                if (required.HasHop && availableList.Where(x => x.Host.Hop == required.Hop).Any())
                {
                    candidates = availableList
                        .Where(x => x.Host.Hop == required.Hop).ToList();
                }

                // Filter by chip series
                candidates = candidates
                    .Where(x => IsCompatibleChipSeries(x.Host.ChipSeries, required.ChipSeries, required.Libraries)).ToList();

                // Filter by interface type
                candidates = candidates
                    .Where(x => required.InterfaceType == InterfaceTypes.COM || required.InterfaceType == x.Host.SType).ToList();

                $"GetCompatibilityTable for {required.ToString()}:"._DLOG();
                foreach (var item in candidates.GroupBy(x => x).Select(x => new { val = x.Key, count = x.Count() }).OrderBy(x => x.count))
                {
                    $"  -({item.count}) ChipSeries={item.val.Host.ChipSeries}, SName={item.val.Host.SName}, PName={item.val.Host.PName}, JName={item.val.Host.JName};"._DLOG();
                    сompatibilityTable.Add(new Tuple<UnknownDevice, DeviceHostClient, int>(required, item.val, item.count));
                }
            }
            $"GetCompatibilityTable completed in {(DateTime.Now - dt).TotalSeconds} sec."._DLOG();
            return сompatibilityTable;
        }

        public static bool IsCompatibleChipSeries(string available, ChipSeries required, params Libraries[] libraries)
        {
            if (required == ChipSeries.Default)
            {
                throw new InvalidOperationException("Not supposed to be Default at this stage");
            }
            if (available == ChipSeries.Default.ToString())
            {
                throw new InvalidOperationException("Invalid device host chip series. Check Profile");
            }
            if (required == ChipSeries.Any)
            {
                return CheckLibraries(available, available.Contains("7") ? ChipSeries.ZW070x : ChipSeries.ZW050x, libraries);
            }
            return CheckLibraries(available, required, libraries);
        }

        public static bool CheckLibraries(string available, ChipSeries required, params Libraries[] libraries)
        {
            if (required == ChipSeries.ZW080x)
            {
                if (available == ChipSeries.ZG23_BRD4204C.ToString()
                    || available == ChipSeries.ZG23_BRD4204D.ToString()
                    || available == ChipSeries.ZGM230S_BRD4205A.ToString()
                    || available == ChipSeries.ZGM230S_BRD4205B.ToString()
                    || available == ChipSeries.ZGM230_BRD2603A.ToString()
                    || available == ChipSeries.ZG23_BRD4210A.ToString()
                    || available == ChipSeries.xG28_BRD4400A.ToString()
                    || available == ChipSeries.xG28_BRD4401A.ToString()
                    || available == ChipSeries.xG28_BRD4400B.ToString()
                    || available == ChipSeries.xG28_BRD4401B.ToString()
                    || available == ChipSeries.xG28_BRD4400C.ToString()
                    || available == ChipSeries.xG28_BRD4401C.ToString()
                    || available == ChipSeries.xG28_BRD2705A.ToString())
                    return true;
            }
            else if (required == ChipSeries.ZW070x)
            {
                if (available == ChipSeries.ZGM130S_BRD4200A.ToString()
                    || available == ChipSeries.ZGM130S_BRD4202A.ToString()
                    || available == ChipSeries.ZGM130S_BRD4207A.ToString()
                    || available == ChipSeries.RZ13_BRD4209A.ToString())
                    return true;

                if (libraries != null)
                {
                    if (libraries.Any(x => IsSerialApiController(x) && x != Libraries.ControllerPortableLib))
                        return available == ChipSeries.ZW070x.ToString()
                            || available == ChipSeries.ZG14_BRD4201A.ToString()
                            || available == ChipSeries.ZG14_BRD4206A.ToString()
                            || available == ChipSeries.ZG14_BRD4208A.ToString();

                    if (libraries.Length == 1)
                        switch (libraries[0])
                        {
                            case Libraries.SensorPIR:
                                return available == ChipSeries.ZG13S_BRD4201D.ToString();
                            case Libraries.LEDBulb:
                                return available == ChipSeries.ZG13L_BRD4201C.ToString();
                        }
                }
            }
            else if (required == ChipSeries.ZW0714)
            {
                if (available == ChipSeries.ZG14_BRD4201A.ToString()
                    || available == ChipSeries.ZG14_BRD4206A.ToString()
                    || available == ChipSeries.ZG14_BRD4208A.ToString())
                    return true;
            }
            else if (required == ChipSeries.ZW0713)
            {
                if (available == ChipSeries.ZGM130S_BRD4200A.ToString()
                    || available == ChipSeries.ZGM130S_BRD4202A.ToString()
                    || available == ChipSeries.ZGM130S_BRD4207A.ToString()
                    || available == ChipSeries.RZ13_BRD4209A.ToString())
                    return true;
            }
            return available == required.ToString();
        }

        private static bool AdjustSerialApiLibraries(Tuple<UnknownDevice, DeviceHostClient, int> matchedPair)
        {
            if (matchedPair.Item2.HasJName)
            {
                if (matchedPair.Item1.Libraries.Contains(Libraries.ControllerStaticLib)
                    && !matchedPair.Item1.Libraries.Contains(Libraries.ControllerBridgeLib))
                {
                    matchedPair.Item1.Libraries = new[] { Libraries.ControllerBridgeLib }.Concat(matchedPair.Item1.Libraries).ToArray();
                }
                if ((matchedPair.Item1.Libraries.Contains(Libraries.EndDeviceLegacyRoutingLib) || matchedPair.Item1.Libraries.Contains(Libraries.EndDeviceLegacyLib))
                    && !matchedPair.Item1.Libraries.Contains(Libraries.EndDeviceLib))
                {
                    matchedPair.Item1.Libraries = new[] { Libraries.EndDeviceLib }.Concat(matchedPair.Item1.Libraries).ToArray();
                }
            }
            return matchedPair.Item1.Libraries.Contains(matchedPair.Item2.Library);
        }

        private Libraries[] AdjustRequiredLibraries(Libraries[] libs, ChipSeries chipSeries)
        {
            Libraries[] ret = libs;
            if (libs.Length == 1)
            {
                if (_ZW080xList.Contains(chipSeries))
                {
                    var ctrlLibs = new Libraries[]
                    {
                        Libraries.ControllerStaticLib,
                        Libraries.ControllerBridgeLib,
                        Libraries.InstallerLib,
                        Libraries.ControllerPortableLib
                    };
                    var endDeviceLibs = new Libraries[]
                    {
                        Libraries.EndDeviceLib,
                        Libraries.EndDeviceLegacyRoutingLib,
                        Libraries.EndDeviceLegacyLib,
                    };
                    if (libs.Any(x => ctrlLibs.Contains(x)))
                    {
                        ret = ctrlLibs;
                    }
                    else if (libs.Any(x => endDeviceLibs.Contains(x)))
                    {
                        ret = endDeviceLibs;
                    }
                }
                else if (_ZW070xList.Contains(chipSeries))
                {
                    var ctrlLibs = new Libraries[]
                    {
                        Libraries.ControllerStaticLib,
                        Libraries.ControllerBridgeLib,
                        Libraries.InstallerLib,
                        Libraries.TestController
                    };
                    var endDeviceLibs = new Libraries[]
                    {
                        Libraries.EndDeviceLib,
                        Libraries.EndDeviceLegacyRoutingLib,
                        Libraries.EndDeviceLegacyLib,
                    };
                    if (libs.Any(x => ctrlLibs.Contains(x)))
                    {
                        ret = ctrlLibs;
                    }
                    else if (libs.Any(x => endDeviceLibs.Contains(x)))
                    {
                        ret = endDeviceLibs;
                    }
                }
            }
            return ret;
        }

        public void FinalizeDevices()
        {
            var numbers = Devices.Keys.ToArray();
            try
            {
                numbers.DoForEach(number =>
                {
                    if (Devices.TryRemove(number, out DeviceHostClient dhc))
                    {
                        FinalizeDevice(dhc);
                    }
                });
            }
            catch (AggregateException agex)
            {
                agex.Message._DLOG();
            }
        }

        private void FinalizeDevice(DeviceHostClient dhc)
        {
            SilinkManager.Disconnect(dhc);
            dhc.SClient?.Dispose();
            dhc.PClient?.Dispose();
        }

        SimpleToken smartStartToken = null;
        internal SimpleToken GetSmartStartToken(int timeoutMs)
        {
            if (smartStartToken != null)
            {
                smartStartToken.SetCompletedSignal();
                smartStartToken.Close();
            }
            smartStartToken = new SimpleToken(timeoutMs);
            var inclusionResult = new InclusionResult();
            inclusionResult.AddRemoveNode = new AddRemoveNodeResult();
            inclusionResult.SetWakeUpInterval = new SendDataResult();
            smartStartToken.Result = inclusionResult;
            return smartStartToken;
        }

        internal NodeProvision? LookupForDsk(List<ProvisioningItem> list, byte updateStateNodeInfo, byte[] homeId)
        {
            NodeProvision? ret = null;
            if (list != null)
            {
                foreach (var item in list)
                {
                    if (item.Dsk != null && item.Dsk.Length >= 12)
                    {
                        if (item.State == PreKittingState.Pending && ((item.Dsk[8] | 0xC0) == homeId[0] &&
                            item.Dsk[9] == homeId[1] &&
                            item.Dsk[10] == homeId[2] &&
                            (item.Dsk[11] & 0xFE) == homeId[3]))
                        {
                            if (updateStateNodeInfo == 0x85 && (item.NodeOptions & (byte)Modes.NodeOptionLongRange) == 0)
                            {
                                ret = new NodeProvision(new NodeTag((byte)item.NodeId), (ZWave.Enums.Modes)item.NodeOptions, item.Dsk, (NetworkKeyS2Flags)item.GrantSchemes);
                                break;
                            }
                            else if (updateStateNodeInfo == 0x87 && (item.NodeOptions & (byte)Modes.NodeOptionLongRange) > 0)
                            {
                                ret = new NodeProvision(new NodeTag((byte)item.NodeId), (ZWave.Enums.Modes)item.NodeOptions, item.Dsk, (NetworkKeyS2Flags)item.GrantSchemes);
                                break;
                            }
                        }
                    }
                }
            }
            return ret;
        }

        internal void BusySmartStartCallback(List<ProvisioningItem> list, bool isBusy, byte[] dsk, ActionResult res)
        {
            if (res as InclusionResult != null)
            {
                var result = res as InclusionResult;
                if (res && result.AddRemoveNode && result.AddRemoveNode.SubstituteStatus == ZWave.Enums.SubstituteStatuses.Done)
                {
                    var item = list.FirstOrDefault(x => x.Dsk != null && x.Dsk.SequenceEqual(dsk));
                    if (item != null)
                    {
                        item.NodeId = result.AddRemoveNode.Id;
                        item.State = PreKittingState.Included;
                    }
                }
                if (smartStartToken != null)
                {
                    smartStartToken.Result = res;
                    smartStartToken.SetCompletedSignal();
                }
            }
        }

        internal void DisableSmartStart(Alias deviceAlias)
        {
            if (!IsOptionDisabled(ZWaveSet.SmartStart, deviceAlias))
            {
                Device device = GetDevice(deviceAlias);
                if (device is Controller && device.SerialApiVersion >= 8)
                {
                    ((Controller)device).SetSmartStartMode(false);
                }
            }
        }

        internal void CancelSmartStartSupport(Alias deviceAlias)
        {
            if (!IsOptionDisabled(ZWaveSet.SmartStart, deviceAlias))
            {
                Device device = GetDevice(deviceAlias);
                if (device is Controller && device.SerialApiVersion >= 8)
                {
                    device.CancelRunningActionsByType(typeof(SmartStartSupport));
                }
            }
        }

        internal void EnableSmartStart(Alias deviceAlias)
        {
            if (!IsOptionDisabled(ZWaveSet.SmartStart, deviceAlias))
            {
                Device device = GetDevice(deviceAlias);
                if (device is Controller && device.SerialApiVersion >= 8)
                {
                    ((Controller)device).SetSmartStartMode(true);
                }
            }
        }

        internal void RunSmartStartSupport(Alias deviceAlias)
        {
            if (!IsOptionDisabled(ZWaveSet.SmartStart, deviceAlias))
            {
                Device device = GetDevice(deviceAlias);
                if (device is Controller && device.SerialApiVersion >= 8)
                {
                    var controller = (Controller)device;
                    var list = ProvisioningList.GetDeviceProvisioningList(deviceAlias);

                    var lifelineSettings = (ZWave.BasicApplication.Enums.SetupNodeLifelineSettings)
                        (IncludeSettings.SetupNodeLifelineSettings == SetupNodeLifelineSettings.DisableAutoSisOnly ?
                        SetupNodeLifelineSettings.Default :
                        IncludeSettings.SetupNodeLifelineSettings);

                    controller.SmartStartSupport(
                        (updateStateNodeInfo, homeId) => LookupForDsk(list, updateStateNodeInfo, homeId),
                        (b, d, r) =>
                        {
                            BusySmartStartCallback(list, b, d, r);
                            if ((r as InclusionResult) != null)
                            {
                                var addRes = (InclusionResult)r;
                                if (addRes && addRes.AddRemoveNode.SubstituteStatus != ZWave.Enums.SubstituteStatuses.None)
                                {
                                    LogNetworkKeys(controller, addRes.AddRemoveNode.Node);
                                }
                            }
                        }, 2000, IncludeSettings.TimeoutMs ?? 60000, lifelineSettings);
                }
            }
        }

        public bool IsOptionDisabled(ZWaveSet parameter, Alias deviceAlias)
        {
            return
                DisableOptions != null &&
                DisableOptions.ContainsKey(parameter) &&
                (DisableOptions[parameter] == null || DisableOptions[parameter].Length == 0 || DisableOptions[parameter].Contains(deviceAlias));
        }

        private AutoResetEvent _fwuMDCompleted = new AutoResetEvent(false);
        public void EnableFirmwareUpdateMD(Alias[] deviceNumbers)
        {
            foreach (var item in deviceNumbers)
            {
                var device = GetDevice(item);
                if (!_firmwareUpdateMdSupport.ContainsKey(item.Value))
                {
                    var tokenFirmwareUpdateMdSupport = device.ExecuteAsync(new ZWave.BasicApplication.CommandClasses.FirmwareUpdateMdSupportBasic(device.Network, TX_OPTIONS, (state) => { _fwuMDCompleted.Set(); }), null);
                    _firmwareUpdateMdSupport.Add(item.Value, tokenFirmwareUpdateMdSupport);
                }
            }
        }

        public void DisableFirmwareUpdateMD(Alias deviceNumber)
        {
            Device device = GetDevice(deviceNumber);

            if (_firmwareUpdateMdSupport.ContainsKey(deviceNumber.Value))
            {
                device.Cancel(_firmwareUpdateMdSupport[deviceNumber.Value]);
                _firmwareUpdateMdSupport.Remove(deviceNumber.Value);
            }
        }

        public byte[] GetLastTempNetworkKey(Controller controller, SecuritySchemes scheme, bool isLongRange)
        {
            byte[] ret = null;
            var sm = (SecurityManager)(controller.SessionClient.GetSubstituteManager(typeof(SecurityManager)));
            if (sm != null)
            {
                if (scheme == SecuritySchemes.S2_TEMP)
                {
                    ret = sm.SecurityManagerInfo.GetActualNetworkKeyS2Temp();
                }
                else
                {
                    ret = sm.SecurityManagerInfo.GetActualNetworkKey((ZWave.Enums.SecuritySchemes)scheme, isLongRange);
                }
            }
            return ret;
        }

        public string GetSecurityStatusInfo(Alias deviceNumber)
        {
            var deviceHostClient = GetDeviceHostClient(deviceNumber);
            string ret = _gammaProgrammer.GetSecurityStatusInfo(deviceHostClient);
            return ret;
        }

        public byte[] GetPRK(Alias deviceNumber)
        {
            var deviceHostClient = GetDeviceHostClient(deviceNumber);
            byte[] prkValue = _gammaProgrammer.GetPrk(deviceHostClient);
            return prkValue;
        }

        public void LoadDSK(Alias deviceNumber)
        {
            var deviceHostClient = GetDeviceHostClient(deviceNumber);
            LoadDSK(deviceHostClient);
        }

        public void LoadDSK(DeviceHostClient deviceHostClient)
        {
            if (IsTerminalApplication(deviceHostClient.Library))
            {

            }
            else if (IsSerialApi(deviceHostClient.Library))
            {
                var device = deviceHostClient.SClient as Device;
                device.ListenDebugData(x => Log(x, LogLevels.Trace));
                deviceHostClient.Host.Dsk = new byte[32];
                if (IsSerialApiController(device.Library) || device.Library == Libraries.EndDeviceSysTestLib)
                {
                    device.GetPRK();
                    ZWave.Security.SecurityS2Utils.CryptoScalarmultCurve25519Base(deviceHostClient.Host.Dsk, device.PRK);
                }
                else
                {
                    if (!deviceHostClient.HasJName)
                    {
                        device.GetPRK();
                        ZWave.Security.SecurityS2Utils.CryptoScalarmultCurve25519Base(deviceHostClient.Host.Dsk, device.PRK);
                    }
                    else
                    {

                        byte[] dskValue = _gammaProgrammer.GetDsk(deviceHostClient);
                        if (dskValue != null && dskValue.Length >= 16)
                        {
                            deviceHostClient.Host.Dsk = dskValue;
                        }
                        _gammaProgrammer.GetZWaveRegion(deviceHostClient);
                    }
                }
            }
            else
            {
                if (deviceHostClient.HasJName)
                {
                    byte[] dskValue = _gammaProgrammer.GetDsk(deviceHostClient);
                    if (dskValue != null && dskValue.Length >= 16)
                    {
                        deviceHostClient.Host.Dsk = dskValue;
                    }
                    _gammaProgrammer.GetZWaveRegion(deviceHostClient);
                }
                else
                {
                    var device = deviceHostClient.PClient as TestInterfaceDevice;
                    deviceHostClient.Host.Dsk = new byte[16];
                    var progEnable = device.TestInterfaceSendData(new byte[] { (byte)ProgrammerCommandTypes.ProgEnable }, 1000);
                    for (int i = 0; i < 16; i++)
                    {
                        deviceHostClient.Host.Dsk[i] = device.TestInterfaceSendData(new byte[] { (byte)ProgrammerCommandTypes.ReadNvr, 0x00, (byte)(0x33 + i) }, 200).ByteValue;
                    }
                    var progRelease = device.TestInterfaceSendData(new byte[] { (byte)ProgrammerCommandTypes.ProgDisable, 0x05 }, 1000);
                }
            }


            Thread.Sleep(5000);
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

        public byte[] GetCSA(Alias controllerNumber, int numberOfBytes)
        {
            int length = numberOfBytes < 0 ? 0 : numberOfBytes > 16 ? 16 : numberOfBytes;
            byte[] csaDsk = new byte[length];
            Controller controller = (Controller)GetDevice(controllerNumber);
            var sm = (SecurityManager)(controller.SessionClient.GetSubstituteManager(typeof(SecurityManager)));
            if (sm != null)
            {
                Array.Copy(sm.SecurityManagerInfo.GetPublicKeyS2(), 0, csaDsk, 0, csaDsk.Length);
            }
            return csaDsk;
        }

        public string GetPowerMeasure(DeviceHostClient deviceHostClient, int windowLenght)
        {
            string exitCode = string.Empty;

            if (string.IsNullOrEmpty(deviceHostClient.Host.JName))
            {
                if (_alphaProgrammer != null)
                {
                    exitCode = _alphaProgrammer.GetPowerMeasure(deviceHostClient, windowLenght);
                }
            }
            else
            {
                if (_gammaProgrammer != null)
                {
                    exitCode = _gammaProgrammer.GetPowerMeasure(deviceHostClient, windowLenght);
                }
            }
            return exitCode;
        }

        public ConcurrentDictionary<Alias, DeviceHostClient> GetDevices()
        {
            return Devices;
        }

        /// <summary>
        /// Read NVM3 to specific file and retrive Application Framework API Version
        /// </summary>
        /// <param name="deviceHostClient">Device host information.</param>
        /// <returns>Z-Wave Application Framework API Version from NVM3: [APP_VERSION, APP_REVISION, APP_PATCH]</returns>
        public byte[] GetZafApiVersion(DeviceHostClient deviceHostClient)
        {
            if (deviceHostClient.HasJName && _gammaProgrammer != null)
            {
                return (_gammaProgrammer as ProgrammingGammaProvider).ReadZafApiVersionFromNVM(deviceHostClient);
            }
            return new byte[0];
        }
    }

    public class UnknownDevice
    {
        public Alias DeviceAlias { get; set; }
        public string Alias { get; set; }
        public Libraries[] Libraries { get; set; }
        public DevKits DevKit { get; set; }
        public ChipSeries ChipSeries { get; set; }
        public InterfaceTypes InterfaceType { get; set; }
        public string Frequency { get; set; }
        public bool HasDoorLockLibrary { get; set; }
        public bool IsEmulated { get; set; }
        public string FileNameFilter { get; set; }
        public bool IsTestInterface { get; set; }
        public bool HasAlias
        {
            get { return !string.IsNullOrEmpty(Alias); }
        }
        public string Hop { get; set; }
        public bool HasHop => !string.IsNullOrEmpty(Hop);

        public UnknownDevice(Alias deviceAlias, string alias, Libraries[] libraries, ChipSeries chipSeries, InterfaceTypes interfaceType, string frequency, DevKits devKit, string fileNameFilter, string hop)
        {
            DeviceAlias = deviceAlias;
            Alias = alias;
            Libraries = libraries;
            DevKit = devKit;
            FileNameFilter = fileNameFilter;
            ChipSeries = chipSeries;
            InterfaceType = interfaceType;
            Frequency = frequency;
            HasDoorLockLibrary = false;
            if (Libraries != null && Libraries.Length > 0)
            {
                HasDoorLockLibrary = Libraries.Contains(ZWave.Enums.Libraries.DoorLockKeyPad);
            }
            Hop = hop;
        }

        public override string ToString()
        {
            return $"<{DeviceAlias}, {Alias}, {ChipSeries}, {Libraries[0]}>";
        }
    }

    public class DeviceHostClient
    {
        public string WstkNickname { get; set; }
        public string BackupSNameTemp { get; set; }
        public Alias DeviceAlias { get; set; }
        public DeviceHost Host { get; set; }
        public IApplicationClient SClient { get; set; }
        public IApplicationClient PClient { get; set; }
        /// <summary>
        /// Additional Client for SerialAPi with PTI support.
        /// Such devices can work in mixed mode: as SerialAPI-device and Zniffer in the one time,
        /// For this case SClient is for SerialAPI and ZClient.
        /// If device supports only PTI (Zniffer) then ZClient is null and should be used SClient.
        /// </summary>
        public IApplicationClient ZClient { get; set; }
        public DevKits DevKit { get; set; }
        public ConfigurationItem ConfigurationItem { get; set; }
        public DeviceHostClient(DeviceHost deviceHost)
            : this(deviceHost, null, null)
        {

        }

        public DeviceHostClient(DeviceHost deviceHost, IApplicationLayer sourceAppLayer, IApplicationLayer programAppLayer)
        {
            Host = deviceHost;
            if (sourceAppLayer != null)
            {
                if (!string.IsNullOrEmpty(Host.SName))
                {
                    if (IPAddress.TryParse(Host.SName, out IPAddress ipAddress))
                    {
                        Host.SType = InterfaceTypes.TCP;
                        SClient = sourceAppLayer.CreateClient();
                        var sds = new SocketDataSource(Host.SName, UdpJLinkDiscoveryService.ADMIN_PORT);
                        SClient.DataSource = sds;
                    }
                    else
                    {
                        SClient = sourceAppLayer.CreateClient();
                        var sds = new SerialPortDataSource(Host.SName, BaudRates.Rate_115200, Host.Alias, Host.SArgs);
                        SClient.DataSource = sds;
                    }
                }
                if (!string.IsNullOrEmpty(Host.PName))
                {
                    if (IPAddress.TryParse(Host.PName, out IPAddress ipAddress))
                    {
                        PClient = (programAppLayer ?? sourceAppLayer).CreateClient();
                        var pds = new SocketDataSource(Host.PName, UdpJLinkDiscoveryService.ADMIN_PORT);
                        PClient.DataSource = pds;
                    }
                    else
                    {
                        PClient = (programAppLayer ?? sourceAppLayer).CreateClient();
                        var pds = new SerialPortDataSource(Host.PName, BaudRates.Rate_115200, Host.Alias, null);
                        PClient.DataSource = pds;
                    }
                }
            }
        }

        public override string ToString()
        {
            return $"s{Host.SName}-p{Host.PName}-j{Host.JName}-a{Host.Alias} {Library} {Frequency} {DevKit.ToString()} s{SClient?.GetType().Name}-p{PClient?.GetType().Name}";
        }

        public bool HasPName
        {
            get { return !string.IsNullOrEmpty(Host.PName); }
        }

        public bool HasJName
        {
            get { return !string.IsNullOrEmpty(Host.JName); }
        }

        public bool HasSName
        {
            get { return !string.IsNullOrEmpty(Host.SName); }
        }

        //public ChipSeries ChipSeries
        //{
        //    get
        //    {
        //        if (PClient as Device != null && (PClient as Device).Library != Libraries.NoLib)
        //        {
        //            return (ChipSeries)(PClient as Device).ChipType;
        //        }
        //        else if (SClient as Device != null)
        //        {
        //            return (ChipSeries)(SClient as Device).ChipType;
        //        }
        //        else
        //        {
        //            var t = (ChipSeries)Enum.Parse(typeof(ChipSeries), Host.ChipSeries);
        //            return t == ChipSeries.Default ? (HasJName ? ChipSeries.ZW070x : ChipSeries.ZW050x) : t;
        //        }
        //    }
        //}

        public string Version
        {
            get
            {
                if (PClient as Device != null && (PClient as Device).Library != Libraries.NoLib)
                {
                    return (PClient as Device).Version;
                }
                else if (SClient as Device != null)
                {
                    return (SClient as Device).Version;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public string Frequency { get; set; }
        public Libraries Library { get; set; }
        public string FileName { get; internal set; }
        public bool IsLongRange
        {
            get
            {
                return Frequency != null && Enum.IsDefined(typeof(RfLongRangeRegions), Frequency);
            }
        }
    }
}