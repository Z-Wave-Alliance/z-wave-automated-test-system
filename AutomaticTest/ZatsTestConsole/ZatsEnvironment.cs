using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Utils.UI.Enums;
using Zats.Services;
using Zats.Services.Enums;

namespace ZatsTestConsole
{
    public class ZatsEnvironment
    {
        private IManagersContainer _managers;
        private TestCaseDefinition _testCaseDefinition;

        public ZatsEnvironment(IManagersContainer managers)
        {
            _managers = managers;
        }

        public bool SetUp(
            Profile activeProfile,
            TestCaseDefinition testCaseDefinition,
            CmdLineSettings settings,
            ZatsSettings zatsSettings,
            ConsoleCustomOutput consoleOutPut,
            string rfRegion,
            LongRangeChannels lrChannel,
            int runCounter,
            int tcCounter)
        {
            _testCaseDefinition = testCaseDefinition;
            _managers.DeviceManager.EnableOptions = _testCaseDefinition.EnableOptions;
            _managers.DeviceManager.DisableOptions = _testCaseDefinition.DisableOptions;
            _managers.DeviceManager.BasicApplicationLayer.SessionLayer.ResetSessionIdCounter();
            _managers.DeviceManager.TextApplicationLayer.SessionLayer.ResetSessionIdCounter();
            _managers.DeviceManager.TcpApplicationLayer.SessionLayer.ResetSessionIdCounter();
            _managers.ZnifferManager.ZnifferApplicationLayer.SessionLayer.ResetSessionIdCounter();
            _managers.ZipManager.ApplicationLayer.SessionLayer.ResetSessionIdCounter();
            _managers.ReportManager.ApplicationLayer.SessionLayer.ResetSessionIdCounter();
            _managers.EmulatedDeviceManager.ResetConfiguredState();

            var instrumentRequiredList = new SortedList<Alias, string>();
            var deviceRequiredList = new List<UnknownDevice>();
            var zipDeviceRequiredList = new SortedList<Alias, string>();
            var uicDeviceRequiredList = new SortedList<Alias, string>();
            var zigDeviceRequiredList = new SortedList<Alias, string>();
            var debugDeviceRequiredList = new SortedList<Alias, string>();
            var emulatedDevicesRequiredList = new List<UnknownDevice>();
            var meshRoutersRequiredList = new SortedList<Alias, string>();
            ConfigurationModes configurationMode = settings.RunMode == RunModes.Readonly && zatsSettings.SaveInclusionState ?
                (zatsSettings.IsInclusionStateReadOnly ? ConfigurationModes.Read : ConfigurationModes.ReadWrite) :
                ConfigurationModes.None;
            var isMixedHardwareMode = settings.RunMode == RunModes.Readonly && zatsSettings.MixedHardwareMode;
            var available = activeProfile.DeviceHosts.Where(x => !string.IsNullOrWhiteSpace(x.ChipSeries) && x.ChipSeries != "Default").ToList();

            _managers.ReportManager.InitDevice();
            consoleOutPut.ReportToConsole("Start", "", LogLevels.Diagnostic);
            if (activeProfile.DeviceHosts == null)
            {
                activeProfile.DeviceHosts = new ObservableCollection<DeviceHost>();
            }

            foreach (var item in testCaseDefinition.Instruments)
            {
                if (!_managers.InstrumentsManager.InitDevice(item))
                    return false;
            }

            foreach (var item in testCaseDefinition.ZipDevices)
            {
                if (!_managers.ZipManager.InitDevice(available, item))
                    return false;
            }

            foreach (var item in testCaseDefinition.UicDevices)
            {
                if (!_managers.UicManager.InitDevice(available, item))
                    return false;
            }

            foreach (var item in testCaseDefinition.ZigDevices)
            {
                if (!_managers.UicManager.InitEndDevice(available, item))
                    return false;
            }

            foreach (var item in testCaseDefinition.SshDevices)
            {
                if (!_managers.UicManager.InitSshDevice(available, item))
                    return false;
            }

            foreach (var item in testCaseDefinition.MeshRouterDevices)
            {
                if (!_managers.MeshRouterManager.InitDevice(activeProfile.DeviceHosts.ToList(), item))
                    return false;
            }

            foreach (var item in testCaseDefinition.DebugDevices)
            {
                debugDeviceRequiredList.Add(item.Key, item.Value.Alias);
            }

            if (isMixedHardwareMode)
            {
                foreach (var device in testCaseDefinition.Devices)
                {
                    if (device.Value.ChipSeries == ChipSeries.Default)
                    {
                        device.Value.ChipSeries = ChipSeries.Any;
                    }
                }
            }

            foreach (var item in testCaseDefinition.Devices
                .OrderBy(i => i.Value.ChipSeries == ChipSeries.Any)
                .ThenBy(i => i.Value.ChipSeries == ChipSeries.Default))
            {
                if (item.Value.ChipSeries == ChipSeries.Default)
                {
                    var is700Available = available.Where(x => !string.IsNullOrWhiteSpace(x.ChipSeries))
                        .Any(x => new[] { "ZW07", "ZGM130", "ZG13", "RZ13", "ZG14" }.Any(y => x.ChipSeries.Contains(y)));
                    var is800Available = available.Where(x => !string.IsNullOrWhiteSpace(x.ChipSeries))
                        .Any(x => new[] { "ZW08", "ZGM230", "ZG23", "xG28" }.Any(y => x.ChipSeries.Contains(y)));

                    if (!string.IsNullOrEmpty(settings.ChipSeries))
                    {
                        item.Value.ChipSeries = settings.ChipSeries.Contains("8") ? ChipSeries.ZW080x
                            : settings.ChipSeries.Contains("7") ? ChipSeries.ZW070x
                            : settings.ChipSeries.Contains("5") ? ChipSeries.ZW050x
                            : ChipSeries.Default;
                    }
                    else
                    {
                        item.Value.ChipSeries = is800Available ? ChipSeries.ZW080x : is700Available ? ChipSeries.ZW070x : ChipSeries.ZW050x;
                    }
                }

                if (item.Value.IsEmulated && item.Value.Libraries != null)
                {
                    emulatedDevicesRequiredList.Add(
                        new UnknownDevice(item.Key,
                        item.Value.Alias,
                        ZatsUtils.GetLibraries(item.Value.Libraries),
                        item.Value.ChipSeries,
                        item.Value.InterfaceType,
                        item.Value.RfRegion != RfRegions.UNKNOWN ? item.Value.RfRegion.ToFrequency() : _managers.Context.Frequency,
                        item.Value.DevKit,
                        null,
                        null)
                        {
                            IsEmulated = true
                        });
                }
                else
                {
                    deviceRequiredList.Add(
                        new UnknownDevice(item.Key,
                        item.Value.Alias,
                        ZatsUtils.GetLibraries(item.Value.Libraries),
                        item.Value.ChipSeries,
                        item.Value.InterfaceType,
                        item.Value.RfRegion != RfRegions.UNKNOWN ? item.Value.RfRegion.ToFrequency() : _managers.Context.Frequency,
                        item.Value.DevKit,
                        item.Value.FileNameFilter,
                        item.Value.Hop)
                        {
                            IsTestInterface = item.Value.IsTestInterface

                        });
                }
            }

            foreach (var item in testCaseDefinition.DeviceGroups)
            {
                if (item.Value.ChipSeries == ChipSeries.Default)
                {
                    var is700Available = available.Where(x => !string.IsNullOrWhiteSpace(x.ChipSeries))
                        .Any(x => new[] { "ZW07", "ZGM130", "ZG13", "RZ13", "ZG14" }.Any(y => x.ChipSeries.Contains(y)));
                    var is800Available = available.Where(x => !string.IsNullOrWhiteSpace(x.ChipSeries))
                        .Any(x => new[] { "ZW08", "ZGM230", "ZG23", "xG28" }.Any(y => x.ChipSeries.Contains(y)));

                    if (!string.IsNullOrEmpty(settings.ChipSeries))
                    {
                        item.Value.ChipSeries = settings.ChipSeries.Contains("8") ? ChipSeries.ZW080x
                            : settings.ChipSeries.Contains("7") ? ChipSeries.ZW070x
                            : settings.ChipSeries.Contains("5") ? ChipSeries.ZW050x
                            : ChipSeries.Default;
                    }
                    else
                    {
                        item.Value.ChipSeries = is800Available ? ChipSeries.ZW080x : is700Available ? ChipSeries.ZW070x : ChipSeries.ZW050x;
                    }
                }
            }

            foreach (var deviceGroupByChipSeries in testCaseDefinition.DeviceGroups.GroupBy(x => x.Value.ChipSeries)
                .OrderBy(x => x.Key))
            {
                var reqNonGrouped = deviceRequiredList.Count(x => DeviceManager.IsCompatibleChipSeries(x.ChipSeries.ToString(), deviceGroupByChipSeries.Key, null));
                var availableByChipSeries = available.Count(x => DeviceManager.IsCompatibleChipSeries(x.ChipSeries, deviceGroupByChipSeries.Key, null));
                var enumerator = deviceGroupByChipSeries.GetEnumerator();
                for (int i = 0; i < availableByChipSeries - reqNonGrouped; i++)
                {
                    if (!enumerator.MoveNext())
                    {
                        enumerator = deviceGroupByChipSeries.GetEnumerator();
                        if (!enumerator.MoveNext())
                        {
                            break;
                        }
                    }
                    var item = enumerator.Current;
                    deviceRequiredList.Add(
                        new UnknownDevice(new Alias((byte)item.Key.Value, GetNextRequiredKey(deviceRequiredList)),
                        null,
                        ZatsUtils.GetLibraries(item.Value.Libraries),
                        item.Value.ChipSeries,
                        item.Value.InterfaceType,
                        item.Value.RfRegion != RfRegions.UNKNOWN ? item.Value.RfRegion.ToFrequency() : _managers.Context.Frequency,
                        item.Value.DevKit,
                        item.Value.FileNameFilter,
                        item.Value.Hop)
                        {
                            IsTestInterface = item.Value.IsTestInterface

                        });
                }
            }

            _managers.DeviceManager.InitDevices(available, deviceRequiredList, settings.IsAppendMode, configurationMode, settings.ConfigPath);
            if (deviceRequiredList.Count > 0)
                return false;

            _managers.EmulatedDeviceManager.InitDevices(emulatedDevicesRequiredList);

            if (debugDeviceRequiredList.Count == 1)
            {
                var doorLocks = _managers.DeviceManager.Devices.Where(x => x.Value.PClient != null).Where(x => x.Value.PClient.Library == ZWave.Enums.Libraries.DoorLockKeyPad);
                if (doorLocks.Count() == 1)
                {
                    var doorLock = doorLocks.First();
                    doorLock.Value.SClient?.Disconnect();
                    var replaceAvailable = new List<DeviceHost>(new[]
                    {
                            new DeviceHost
                            {
                                SName = doorLock.Value.Host.SName,
                                ChipSeries = doorLock.Value.Host.ChipSeries
                            }
                        });
                    _managers.DebugDeviceManager.InitDevices(replaceAvailable, debugDeviceRequiredList);
                }
            }
            _managers.DebugDeviceManager.InitDevices(available, debugDeviceRequiredList);

            var profileZName = GetEnvironmentZnifferPort(activeProfile);
            if (!string.IsNullOrEmpty(profileZName) && !string.IsNullOrEmpty(rfRegion))
            {
                if (System.Net.IPAddress.TryParse(profileZName, out System.Net.IPAddress ptiSource) && settings.IsZnifferVirtual == false)
                {
                    var host = new DeviceHost() { SName = profileZName, SPort = 4905 /*non-virtual Zniffer PTI via JLink*/, JName = profileZName, SType = InterfaceTypes.TCP };
                    var chipType = _managers.DeviceManager.GetChipType(new DeviceHostClient(host, null, null));
                    if (!string.IsNullOrWhiteSpace(chipType) && Enum.TryParse(chipType, out ChipSeries chipSeries))
                    {
                        var hosts = new List<DeviceHost>() { host };
                        var reqZnif = new UnknownDevice(new Alias(0xDEAD), null,
                            new ZWave.Enums.Libraries[] { ZWave.Enums.Libraries.ZnifferPTI },
                            chipSeries, InterfaceTypes.TCP, rfRegion, DevKits.Default, null, null);
                        var reqs = new List<UnknownDevice>() { reqZnif };
                        _managers.DeviceManager.InitDevices(hosts, reqs, settings.IsAppendMode, configurationMode, settings.ConfigPath);
                    }
                }
                var isInited = _managers.ZnifferManager.InitDevice(profileZName, settings.IsZnifferVirtual, rfRegion, lrChannel);
                if (!isInited && testCaseDefinition.IsZnifferRequired)
                    return false;
            }

            try
            {
                byte? lrChannelConfigCode = null;
                string lrChannelConfigDescription = null;
                if (RfRegionsExtensions.IsLongRangeRfRegion(rfRegion))
                {
                    ZnifferChannelConfig lrChannelConfig = ZnifferManager.GetDefaultZnifferLRChannelConfig(lrChannel);
                    lrChannelConfigCode = (byte)lrChannelConfig;
                    lrChannelConfigDescription = ZnifferManager.GetZnifferChannelConfigDescription(lrChannelConfig);
                }

                consoleOutPut.ReportDevicesToConsole(testCaseDefinition, _managers, rfRegion, lrChannelConfigCode, lrChannelConfigDescription);
            }
            catch (Exception ex)
            {
                consoleOutPut.ReportDiagnosticToConsole(false, ex.Message);
                return false;
            }
            return true;
        }

        private ushort GetNextRequiredKey(List<UnknownDevice> deviceRequiredList)
        {
            ushort ret = 0;
            for (int i = 0; i < deviceRequiredList.Count + 1; i++)
            {
                ret++;
                if (!deviceRequiredList.Any(x => x.DeviceAlias.Value == ret))
                {
                    break;
                }
            }
            return ret;
        }

        public bool Connect(CmdLineSettings settings)
        {
            _managers.InstrumentsManager.ConnectDevices();
            _managers.DeviceManager.ConnectDevices();
            _managers.DebugDeviceManager.ConnectDevices(_testCaseDefinition.DebugDevices.Keys.ToArray());
            bool ret = _managers.EmulatedDeviceManager.ConnectDevices(_testCaseDefinition.Devices.Keys.ToArray());

            if (ret && settings.RunServiceType == RunServiceType.PCControllerUI)
            {
                //move UI Controlled Devices from DeviceManager to PCControllerDeviceManager: 
                var uiControlledDevices = _managers.DeviceManager.CutDevices(
                    ZWave.Enums.Libraries.ControllerStaticLib,
                    ZWave.Enums.Libraries.ControllerBridgeLib,
                    ZWave.Enums.Libraries.ControllerPortableLib,
                    ZWave.Enums.Libraries.EndDeviceLib,
                    ZWave.Enums.Libraries.EndDeviceLegacyLib,
                    ZWave.Enums.Libraries.EndDeviceLegacyRoutingLib,
                    ZWave.Enums.Libraries.TestController);
                _managers.PCControllerDeviceManager.InitDevices(uiControlledDevices);
                _managers.PCControllerDeviceManager.InitDevices(_managers.ZipManager.CutDevices());

                ret = _managers.PCControllerDeviceManager.ConnectDevices();
            }
            else
            {
                ret = _managers.ZipManager.ConnectDevices(_testCaseDefinition.ZipDevices.Keys.ToArray());
            }
            return ret;
        }

        private bool CheckLibrariesSpecified(TestCaseDefinition testCaseDefinition, ConsoleCustomOutput consoleOutPut)
        {
            bool ret = false;
            if (testCaseDefinition.Devices.Any(x => x.Value.Libraries == null || x.Value.Libraries.Length == 0))
            {
                var device = testCaseDefinition.Devices.First(x => x.Value.Libraries == null || x.Value.Libraries.Length == 0);
                if (device.Value != null)
                {
                    consoleOutPut.ConsoleWriteLine($"Device Mapping {device.Value.Number} doesn't specify Library.", LogLevels.Error);
                }
            }
            else
            {
                ret = true;
            }
            return ret;
        }

        private string GetEnvironmentZnifferPort(Profile profileName)
        {
            string ret = null;
            ret = profileName.ZName;
            return ret;
        }
    }
}
