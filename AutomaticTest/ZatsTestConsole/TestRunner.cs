// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
// SPDX-FileCopyrightText: Z-Wave Alliance https://z-wavealliance.org
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Utils;
using Utils.UI.Enums;
using Zats.Services;
using Zats.Services.MeshNetworkRacks;
using Zats.Services.Telemetry;
using ZWave;
using ZWave.Layers;
using ZWave.Layers.Transport;
using ZWave.Xml.Application;
using ZWave.Xml.FrameHeader;
using ZWave.ZnifferApplication;

namespace ZatsTestConsole
{
    public class TestRunner
    {
#if NETCOREAPP
        private const string ProgrammerAppName = "ZWaveProgrammer";
#else
        private const string ProgrammerAppName = "ZWaveProgrammer.exe";
#endif

        private Stopwatch _stopWatch = new Stopwatch();
        private readonly AutoResetEvent _waitHandle = new AutoResetEvent(false);

        public ZatsSettings ZatsSettings { get; set; }
        public ZatsInstruments ZatsInstruments { get; set; }
        public ZatsDefinition ZatsDefinition { get; set; }
        public ZWaveDefinition ZWaveDefinition { get; set; }
        public FrameDefinition FrameDefinition { get; set; }
        public ZatsProfile ZatsProfile { get; set; }

        public static bool _isQuit = false;
        CmdLineSettings _cmdLineSettings;

        private Profile ActiveProfile { get; set; }
        private StorageHeader StorageHeader { get; set; }

        public int RunCodeTest()
        {
            //ConsoleCtrlHandler.EnableCtrlEvents();

            int ret = 0;
            Tools.StartDebugOutputWorker();
            StorageHeader = new StorageHeader();
            _cmdLineSettings = ZatsUtils.ParseArguments(Environment.GetCommandLineArgs());
            var consoleOutPut = new ConsoleCustomOutput(_cmdLineSettings.IsLessOutput, _cmdLineSettings.IsOutputColorFormated, _cmdLineSettings.UseStartStopTags);

            if (_cmdLineSettings.HasHelp)
            {
                consoleOutPut.ConsoleWriteLine(_cmdLineSettings.Help);
                //consoleOutPut.IsLessOutput = false;
                consoleOutPut.ConsoleWriteLineTotal(_cmdLineSettings.Help);
                _isQuit = true;
                return ret;
            }
            if (_cmdLineSettings.HasParseError)
            {
                consoleOutPut.ConsoleWriteLine(_cmdLineSettings.ParseErrorMessage, LogLevels.Error);
                //consoleOutPut.IsLessOutput = false;
                consoleOutPut.ConsoleWriteLineTotal(_cmdLineSettings.ParseErrorMessage);
                _isQuit = true;
                ret = 1;
                return ret;
            }

            ZWaveDefinition = ZatsUtils.LoadZWaveDefinition();
            FrameDefinition = ZatsUtils.LoadFrameDefinition();
            var zatsXmlRoot = ZatsUtils.GetZatsXmlFilesDirectory(_cmdLineSettings.ConfigPath);

            if (!string.IsNullOrEmpty(zatsXmlRoot))
            {
                if (Directory.Exists(zatsXmlRoot))
                {
                    try
                    {
                        ZatsDefinition = ZatsUtils.LoadZatsDefinition(zatsXmlRoot);
                    }
                    catch { }
                }
                else
                {
                    try
                    {
                        Directory.CreateDirectory(zatsXmlRoot);
                    }
                    catch
                    {
                        Console.Out.WriteLine($"Can't create folder '{zatsXmlRoot}'");
                    }
                }
            }
            if (ZatsDefinition == null)
            {
                ZatsDefinition = new ZatsDefinition();
                ZatsDefinition.Profiles = new ObservableCollection<Profile>();
            }

            if (!string.IsNullOrWhiteSpace(_cmdLineSettings.ProfileName))
            {
                ZatsProfile = new ZatsProfile() { Name = _cmdLineSettings.ProfileName };
            }
            else
            {
                ZatsProfile = ZatsUtils.LoadZatsProfile(zatsXmlRoot);
                if (ZatsProfile == null)
                {
                    ZatsProfile = new ZatsProfile();
                    ZatsProfile.Name = Environment.MachineName;
                    if (string.IsNullOrWhiteSpace(ZatsProfile.Name))
                    {
                        try
                        {
                            ZatsProfile.Name = System.Net.Dns.GetHostName();
                        }
                        catch { }
                        if (string.IsNullOrEmpty(ZatsProfile.Name))
                        {
                            ZatsProfile.Name = "zatsserver";
                        }
                    }
                    ZatsUtils.SaveZatsProfile(ZatsProfile, zatsXmlRoot);
                }
            }

            ActiveProfile = ZatsDefinition.Profiles.FirstOrDefault((x) => x.Name == ZatsProfile.Name);
            if (ActiveProfile == null)
            {
                ZatsDefinition.Profiles.Add(new Profile { Name = ZatsProfile.Name });
                ActiveProfile = ZatsDefinition.Profiles.FirstOrDefault((x) => x.Name == ZatsProfile.Name);
            }
            ZatsSettings = JsonUtils.Load<ZatsSettings>(zatsXmlRoot);
            if (ZatsSettings == null)
            {
                ZatsSettings = new ZatsSettings();
                if (zatsXmlRoot != null)
                {
                    JsonUtils.Save(zatsXmlRoot, ZatsSettings);
                }
            }
            ZatsInstruments = JsonUtils.Load<ZatsInstruments>(zatsXmlRoot);
            if (ZatsInstruments == null)
            {
                ZatsInstruments = new ZatsInstruments();
                ZatsInstruments.Instruments = new[]
                {
                    new ZatsInstrument(){ Type = InstrumentTypes.PsuE364x, SName="192.168.0.198", SType="RawTCP" }
                };
                if (zatsXmlRoot != null)
                {
                    JsonUtils.Save(zatsXmlRoot, ZatsInstruments);
                }
            }
            if (!string.IsNullOrEmpty(_cmdLineSettings.ReportsPath))
            {
                ActiveProfile.Reports = _cmdLineSettings.ReportsPath;
            }
            if (string.IsNullOrEmpty(ActiveProfile.Reports))
            {
                ActiveProfile.Reports = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs");
            }
            if (!Directory.Exists(ActiveProfile.Reports))
            {
                Directory.CreateDirectory(ActiveProfile.Reports);
            }
            Directory.SetCurrentDirectory(ActiveProfile.Reports);
            if (ActiveProfile.DeviceHosts == null)
            {
                ActiveProfile.DeviceHosts = new ObservableCollection<DeviceHost>();
            }
            if (string.IsNullOrEmpty(ActiveProfile.PCControllerAppPath) && _cmdLineSettings.RunServiceType == RunServiceType.PCControllerUI)
            {
                consoleOutPut.ConsoleWriteLine("Couldn't detect ZWaveControllerUI.exe for interactive mode", LogLevels.Error);
            }
            //if (string.IsNullOrEmpty(ActiveProfile.ProgrammerAppPath))
            //{
            //    ActiveProfile.ProgrammerAppPath = ZatsUtils.GetApplicationPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "ZWaveProgrammer.exe");
            //    if (string.IsNullOrEmpty(ActiveProfile.ProgrammerAppPath))
            //    {
            //        consoleOutPut.ConsoleWriteLine("Couldn't detect ZWaveProgrammer.exe for append or override mode and autodetect ZW devices", LogLevels.Error, true);
            //    }
            //}
            if (!string.IsNullOrEmpty(_cmdLineSettings.CommanderAppPath))
            {
                ActiveProfile.CommanderAppPath = _cmdLineSettings.CommanderAppPath;
            }
            if (string.IsNullOrEmpty(ActiveProfile.CommanderAppPath))
            {
                ActiveProfile.CommanderAppPath = ZatsUtils.GetApplicationPath(null, "commander.exe");
                if (string.IsNullOrEmpty(ActiveProfile.CommanderAppPath))
                {
                    consoleOutPut.ConsoleWriteLine("Couldn't detect commander.exe for append or override mode and autodetect Gecko devices", LogLevels.Error);
                }
            }
            IProgrammingProvider _alphaProgrammer = null;
            {
                string filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Programmer");
                filePath = Path.Combine(filePath, ProgrammerAppName);
                if (File.Exists(filePath))
                {
                    _alphaProgrammer = new ProgrammingAlphaProvider(filePath, consoleOutPut.ReportDiagnosticToConsole);
                }
            }
            IProgrammingProvider _gammaProgrammer = null;
            if (!string.IsNullOrEmpty(ActiveProfile.CommanderAppPath))
            {
                _gammaProgrammer = new ProgrammingGammaProvider(ActiveProfile.CommanderAppPath, consoleOutPut.ReportDiagnosticToConsole);
            }

            var testCaseAssemblyPath = Path.GetDirectoryName(_cmdLineSettings.AssemblyFile);
            IFileSystemManager _fileSystemManager = new FileSystemManager(consoleOutPut.ReportDiagnosticToConsole,
                testCaseAssemblyPath, _cmdLineSettings.BasePath, _cmdLineSettings.ReportsPath);

            // Check if mesh network router is present.
            var meshRoutersFilterList = GetSerialNumbersToFilter(_cmdLineSettings.MeshRoutersDevicesFilter);
            bool hasMeshRouter = meshRoutersFilterList?.Count > 0;
            MeshRouterDeviceInfo routerInfo = null;
            MeshRouterTopologyBuilder meshRouterTopologyBuilder = null;
            if (hasMeshRouter)
            {
                var udpMeshNetworkRacksDiscoveryService = new UdpMeshRouterDiscoveryService();
                var filteredRoutersInfo = udpMeshNetworkRacksDiscoveryService.DiscoverTcpDevices().Where(info =>
                    meshRoutersFilterList.Any(serial => info.SerialNo.EndsWith(serial))).ToArray();
                if (filteredRoutersInfo.Length == 1)
                    routerInfo = filteredRoutersInfo[0];
                else
                {
                    consoleOutPut.ReportToConsole("Mesh network router not found!", "*", LogLevels.Fail);
                    return ret;
                }
                bool invalidateProfile = true;
                if (IsProfileContainsMeshRouter(ActiveProfile))
                {
                    var routerHost = ActiveProfile.DeviceHosts.FirstOrDefault(x => x.SType == InterfaceTypes.HTTP &&
                        x.RApiType == RemoteApiTypes.MeshRouter &&
                        x.JName.Equals(routerInfo.SerialNo));
                    if (routerHost != null)
                    {
                        routerHost.SName = routerInfo.IPAddress.ToString(); // In case DHCP server changed router address.
                        invalidateProfile = false;
                    }
                }
                if (invalidateProfile)
                {
                    DeviceHost device = null;
                    do
                    {
                        device = ActiveProfile.DeviceHosts.FirstOrDefault(x => x.SType == InterfaceTypes.COM ||
                            x.SType == InterfaceTypes.TCP);
                        if (device != null)
                            ActiveProfile.DeviceHosts.Remove(device);
                    }
                    while (device != null && ActiveProfile.DeviceHosts.Count > 0);
                }
            }

            if (ActiveProfile.DeviceHosts.Count == 0 || IsProfileContainsRemoteOrZipDevicesOnly(ActiveProfile))
            {
                var sessionLayer = new ZWave.Layers.Session.SessionLayer();
                using (var deviceHostsDetector = new DeviceHostsDetector(
                    _cmdLineSettings,
                    new ZWave.BasicApplication.BasicApplicationLayer(
                        sessionLayer,
                        new ZWave.BasicApplication.BasicFrameLayer(),
                        new SerialPortTransportLayer()),
                    new ZnifferApplicationLayer(
                        sessionLayer,
                        new ZnifferFrameLayer(null),
                        new SerialPortTransportLayer()),
                    new ZWave.BasicApplication.BasicApplicationLayer(
                        sessionLayer,
                        new ZWave.BasicApplication.BasicFrameLayer(), new TcpClientTransportLayer()),
                    new PortNamesGetter(),
                    _alphaProgrammer,
                    _gammaProgrammer,
                    _fileSystemManager,
                    new UdpJLinkDiscoveryService()))
                {
                    var wasLess = consoleOutPut.IsLessOutput;
                    consoleOutPut.IsLessOutput = false;
                    if (ZatsSettings.IsPassiveDetectMode)
                    {
                        deviceHostsDetector.RunPassiveDetect(ActiveProfile);
                    }
                    else
                    {
                        var jlinkDevicesToFilterList = GetSerialNumbersToFilter(_cmdLineSettings.JLinkDevicesFilter);
                        var testBedsFilterList = ConvertFilterToList(_cmdLineSettings.TestBedsFilter);
                        meshRouterTopologyBuilder = deviceHostsDetector.DetectConnectedDevices(ActiveProfile, testBedsFilterList, jlinkDevicesToFilterList, routerInfo);
                        ZatsUtils.DeleteZatsCache(Directory.GetCurrentDirectory());
                    }
                    consoleOutPut.IsLessOutput = wasLess;
                }
                ZatsUtils.SaveZatsDefinition(ZatsDefinition, zatsXmlRoot);
            }
            else if (routerInfo != null)
            {
                meshRouterTopologyBuilder = new MeshRouterTopologyBuilder(routerInfo);
                meshRouterTopologyBuilder.LoadFromProfile(ActiveProfile);
                ZatsUtils.SaveZatsDefinition(ZatsDefinition, zatsXmlRoot);
            }

            HeaderStore.Headers = new Dictionary<byte, Header>();
            foreach (var item in FrameDefinition.Header)
            {
                HeaderStore.Headers.Add(item.Key, item);
            }
            if (ActiveProfile != null && ZWaveDefinition != null && FrameDefinition != null)
            {
                if (_cmdLineSettings.IgnoreMode == IgnoreModes.Fail)
                {
                    _isQuit = true;
                }

                List<Assembly> asmCollection = new List<Assembly>();
                string asmName = "";
                if (File.Exists(_cmdLineSettings.AssemblyFile))
                {
                    var asm = Assembly.LoadFrom(_cmdLineSettings.AssemblyFile);
                    asmName = asm.ManifestModule.Name.Split(new string[] { ".dll" }, StringSplitOptions.RemoveEmptyEntries)[0];
                    asmCollection.Add(asm);
                }
                else if (Directory.Exists(_cmdLineSettings.AssemblyFile))
                {
                    asmName = "Custom";
                    var files = Directory.GetFiles(_cmdLineSettings.AssemblyFile, "*.dll", SearchOption.AllDirectories);
                    int total = files?.Count() ?? 0;
                    int delta = 10;
                    int step = delta;
                    int idx = 0;
                    consoleOutPut.ConsoleWriteLine($"processing {files?.Count()} files in folder {_cmdLineSettings.AssemblyFile}");

                    foreach (var item in files)
                    {
                        if (!item.Contains("Utils") && !item.Contains("Zats") && !item.Contains("ZWave"))
                        {
                            try
                            {
                                var asm = Assembly.LoadFrom(item);
                                asmCollection.Add(asm);
                            }
                            catch (Exception e)
                            {
                                consoleOutPut.ConsoleWriteLine("Exception catched:", LogLevels.Error);
                                consoleOutPut.ConsoleWriteLine(e.ToString(), LogLevels.Information);
                            }
                        }
                        idx++;
                        while (((idx * 100) / total) > (step))
                        {
                            consoleOutPut.ConsoleWrite($"...{step}%");
                            step += delta;
                        }
                    }
                    consoleOutPut.ConsoleWriteLine($"...100%");
                }

                if (asmCollection.Count == 0)
                {
                    consoleOutPut.ConsoleWriteLine($"Assembly not found by specified path: {_cmdLineSettings.AssemblyFile} for the INPUT argument.", LogLevels.Error);
                    return 1;
                }

                Dictionary<string, List<string>> inputFilter = new Dictionary<string, List<string>>();
                if (!string.IsNullOrEmpty(_cmdLineSettings.InputFilter))
                {
                    string[] tokens = _cmdLineSettings.InputFilter.Split(';', ',', ' ');
                    if (tokens != null)
                    {
                        foreach (var item in tokens)
                        {
                            if (File.Exists(item))
                            {
                                var content = File.ReadAllLines(item);
                                if (content != null)
                                {
                                    foreach (var line in content)
                                    {
                                        if (!string.IsNullOrEmpty(line) && !line.StartsWith("#") && !line.StartsWith("//") && !line.StartsWith("--"))
                                        {
                                            ParseInputFilter(inputFilter, line, null, _cmdLineSettings.Frequencies);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                ParseInputFilter(inputFilter, item, null, _cmdLineSettings.Frequencies);
                            }
                        }
                    }
                }
                bool __isTestCaceSpecified = true;
                var testTypes = new List<Tuple<Type, string, string>>();
                foreach (var asm in asmCollection)
                {
                    // workaround for TestCases.dll to skip interactive TestCases untill the specified in the include filter
                    if (asm.GetName().Name == "TestCases")
                    {
                        __isTestCaceSpecified = false;
                    }
                    ZatsMain zm = null;
                    Type[] tests = null;
                    foreach (var item in asm.GetTypes())
                    {
                        if (item.IsSubclassOf(typeof(ZatsMain)))
                        {
                            zm = (ZatsMain)Activator.CreateInstance(item);
                            break;
                        }
                    }

                    if (zm != null && zm.Tests.Count > 0)
                    {
                        tests = zm.Tests.ToArray();
                        __isTestCaceSpecified = true;
                    }
                    else
                    {
                        tests = asm.GetTypes();
                        if (tests.Length > 0)
                        {
                            tests = tests.Where(x => x.IsSubclassOf(typeof(ZatsCodeTestCase))).ToArray();
                        }
                    }

                    if (inputFilter.Count > 0)
                    {
                        __isTestCaceSpecified = true;
                        for (int ii = inputFilter.Count - 1; ii >= 0; ii--)
                        {
                            var item = inputFilter.ElementAt(ii);
                            var groupingKey = item.Key;
                            for (int jj = item.Value.Count - 1; jj >= 0; jj--)
                            {
                                var tcd = item.Value[jj];
                                var test = tests.FirstOrDefault(x => x.Name == tcd);
                                if (test != null)
                                {
                                    testTypes.Add(new Tuple<Type, string, string>(test, Path.GetDirectoryName(asm.Location), groupingKey));
                                    item.Value.RemoveAt(jj);
                                    //if (item.value.count == 0)
                                    //{
                                    //    inputfilter.remove(item.key);
                                    //}
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var t in tests)
                        {
                            testTypes.Add(new Tuple<Type, string, string>(t, Path.GetDirectoryName(asm.Location), asmName));
                        }
                    }
                }

                var missingTCs = new List<string>();
                if (inputFilter.Count > 0)
                {
                    foreach (var item in inputFilter.Values)
                    {
                        foreach (var item2 in item)
                        {
                            missingTCs.Add(item2);
                        }
                    }
                }
                if (missingTCs.Count > 0)
                {
                    int dcount = 20;
                    var names = string.Join(",", missingTCs.Take(dcount));
                    var ending = (missingTCs.Count > dcount) ? $"...(and {missingTCs.Skip(dcount).Count()} more)" : ".";
                    consoleOutPut.ConsoleWriteLine("Missing Test Cases: " + names + ending);
                    consoleOutPut.ConsoleWriteLineTotal("Missing Test Cases: " + names + ending);
                }

                if (testTypes.Count > 0)
                {
                    if (!__isTestCaceSpecified)
                    {
                        testTypes = testTypes.Where(x => !(bool)x.Item1.GetInterfaces()?.Contains(typeof(IUserInteractive))).ToList();
                    }

                    DateTime startTestRun = DateTime.Now;
                    consoleOutPut.ReportToConsole("Test Run Started", "", LogLevels.Diagnostic);
                    consoleOutPut.ConsoleWriteLine();
                    var managersContainer = new ManagersContainer(
                       StorageHeader,
                       ActiveProfile,
                       ZWaveDefinition,
                       FrameDefinition,
                       ZatsSettings,
                       ZatsInstruments,
                       _cmdLineSettings,
                       _alphaProgrammer,
                       _gammaProgrammer,
                       _fileSystemManager,
                       meshRouterTopologyBuilder);
                    managersContainer.SetCurrentTestRunFolder(asmName);
                    consoleOutPut.SetTotalDumpFile(managersContainer.ReportManager.TestRunFolder);
                    consoleOutPut.ReportProfileInformationToConsole(ActiveProfile);

                    var zatsEnvironment = new ZatsEnvironment(managersContainer);
                    var servicesContainer = new ServicesContainer(ActiveProfile, managersContainer, consoleOutPut, _cmdLineSettings);

                    var testsuites = new testsuites();
                    testsuites.testsuite = new Collection<testsuitesTestsuite>();
                    ZatsUtils.SaveZatsTestSuites(testsuites, managersContainer.ReportManager.TestRunFolder);
                    foreach (string freq in _cmdLineSettings.Frequencies)
                    {
                        string rfRegion = freq?.Trim();
                        if (string.IsNullOrEmpty(rfRegion))
                        {
                            continue;
                        }

                        LongRangeChannels[] lrChannels = new LongRangeChannels[0];
                        if (_cmdLineSettings.LongRangeChannels?.Contains(LongRangeChannels.ChannelA) ?? false)
                        {
                            lrChannels = lrChannels.Append(LongRangeChannels.ChannelA).ToArray();
                        }
                        if (_cmdLineSettings.LongRangeChannels?.Contains(LongRangeChannels.ChannelB) ?? false)
                        {
                            lrChannels = lrChannels.Append(LongRangeChannels.ChannelB).ToArray();
                        }
                        // Fallback
                        if (lrChannels.Length == 0)
                        {
                            lrChannels = new LongRangeChannels[] { LongRangeChannels.Undefined };
                        }

                        foreach (LongRangeChannels lrChannel in lrChannels)
                        {
                            foreach (string dutName in _cmdLineSettings.DutNames ?? new[] { "" })
                            {
                                servicesContainer.SetupFrequency(rfRegion);
                                servicesContainer.SetupDutName(dutName);
                                List<TestCaseDefinition> testCasesDefinitions = PrepareTestCaseMetadata(testTypes, consoleOutPut, dutName, rfRegion);

                                var zatsCache = ZatsUtils.LoadZatsCache(Directory.GetCurrentDirectory());
                                if (zatsCache == null)
                                {
                                    zatsCache = new ZatsCache();
                                    zatsCache.ProgrammingResults = new Collection<ProgrammingResult>();
                                    ZatsUtils.SaveZatsCache(zatsCache, Directory.GetCurrentDirectory());
                                }
                                managersContainer.DeviceManager.ZatsCache = zatsCache;
                                List<TestCaseDefinition> extraTCs = new List<TestCaseDefinition>();
                                bool isFirstTCRunIndex = false;
                                for (int runCounter = 1; runCounter <= _cmdLineSettings.NumberOfRuns; runCounter++)
                                {
                                    for (int extraRunCounter = 1; extraRunCounter <= _cmdLineSettings.NumberOfExtraRuns; extraRunCounter++)
                                    {
                                        isFirstTCRunIndex = true;
                                        if ((_cmdLineSettings.NumberOfExtraRuns > 1 && extraRunCounter > 1)
                                            || (_cmdLineSettings.NumberOfRuns > 1 && runCounter > 1))
                                        {
                                            testCasesDefinitions = extraTCs ?? testCasesDefinitions;
                                            extraTCs = new List<TestCaseDefinition>();
                                        }
                                        int tcCounter = 0;
                                        bool connectSucceeded = false;
                                        bool setupSucceeded = false;
                                        TestCaseDefinition tcDefinitionHigh = null;

                                        while (testCasesDefinitions.Count > 0)
                                        {
                                            var tcDefinition = testCasesDefinitions.First();
                                            if (tcDefinition.TestType is IUserInteractive)
                                            {
                                            }
                                            if (!ZatsSettings.SaveInclusionState
                                                && (_cmdLineSettings.ChipSeries == "7" || _cmdLineSettings.ChipSeries == "8"))
                                            {
                                                tcDefinition = null;
                                                if (tcDefinitionHigh != null && connectSucceeded && setupSucceeded) // prev TC
                                                {
                                                    tcDefinition = GetNextTcDefinition(testCasesDefinitions, tcDefinitionHigh);
                                                }
                                                if (tcDefinition == null)
                                                {
                                                    tcDefinitionHigh = testCasesDefinitions.OrderByDescending(t => t.Devices.Count).FirstOrDefault();
                                                    tcDefinition = tcDefinitionHigh;
                                                }
                                            }
                                            testCasesDefinitions.Remove(tcDefinition);

                                            connectSucceeded = false;
                                            setupSucceeded = false;

                                            testsuitesTestsuite testsuite = testsuites.testsuite.FirstOrDefault(x => x.name == tcDefinition.GroupingKey);
                                            if (testsuite == null)
                                            {
                                                testsuite = new testsuitesTestsuite()
                                                {
                                                    package = "ZATS",
                                                    timestamp = DateTime.Now,
                                                    errors = 0,
                                                    failures = 0,
                                                    hostname = Environment.MachineName,
                                                    name = tcDefinition.GroupingKey,
                                                    time = 0,
                                                    testcase = new Collection<testsuiteTestcase>()
                                                };
                                                testsuites.testsuite.Add(testsuite);
                                                ZatsUtils.SaveZatsTestSuites(testsuites, managersContainer.ReportManager.TestRunFolder);
                                            }

                                            tcCounter++;
                                            testsuite.tests++;
                                            var testcase = new testsuiteTestcase()
                                            {
                                                classname = $"{tcDefinition.Name}",
                                                name = $"{tcDefinition.Description}"
                                            };
                                            var extTC = testsuite.testcase.FirstOrDefault(x => x.classname == testcase.classname && x.Item != null);
                                            if (extTC != null)
                                            {
                                                testsuite.testcase.Remove(extTC);
                                                testsuite.tests--;
                                                testsuite.failures--;
                                            }
                                            testsuite.testcase.Add(testcase);

                                            if (ret == 0 || _cmdLineSettings.IgnoreMode == IgnoreModes.Fail)
                                            {
                                                TestCaseResult res = TestCaseResult.Fail;
                                                managersContainer.SetReportFoldersAndFiles(tcDefinition.Name, rfRegion);
                                                consoleOutPut.SetArchiveDumpFile(managersContainer.ReportManager.TestCaseFolder, managersContainer.ReportManager.TestCaseLogName);
                                                if (_cmdLineSettings.RunServiceType == RunServiceType.PCControllerUI) // - save output for all.
                                                {
                                                    var curDir = Directory.GetCurrentDirectory();
                                                    var debugOutPath = Path.Combine(curDir,
                                                        managersContainer.ReportManager.TestRunFolder,
                                                        //managersContainer.ReportManager.TestCaseLogName,
                                                        "debug_output.txt");
                                                    Tools.CreateDebugLogFile(debugOutPath, false);
                                                    if (!tcDefinition.DisableOptions.Any(x => x.Key == ZWaveSet.NodeIdBase2))
                                                    {
                                                        tcDefinition.DisableOptions.Add(ZWaveSet.NodeIdBase2, null);
                                                    }
                                                }
                                                consoleOutPut.ReportTestCaseInformationToConsole(tcDefinition);
                                                var tc = (ZatsCodeTestCase)Activator.CreateInstance(tcDefinition.TestType);
                                                bool isSkipTestCase = false;
                                                if (tc is IUserInteractive tcinteractive)
                                                {
                                                    tcinteractive.MessageBox = servicesContainer.GetMessageBoxProvider();
                                                    isSkipTestCase = !__isTestCaceSpecified;
                                                    tc.AddFailure("Interactive Test Case was not specified.");
                                                }
                                                bool tcInitializeSucceeded = false;
                                                if (!isSkipTestCase)
                                                {
                                                    try
                                                    {
                                                        servicesContainer.SetupTestCaseWithReportService(ref tc);
                                                        tc.Initialize();
                                                        tcInitializeSucceeded = true;
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        tc.AddFailure(ex.Message);
                                                    }
                                                }
                                                if (!tcInitializeSucceeded)
                                                {
                                                    res = TestCaseResult.Fail;
                                                    consoleOutPut.ReportToConsole(tc.GetLastFailure(), "", LogLevels.Fail);
                                                    consoleOutPut.ReportResultToConsole(tcDefinition, res, _stopWatch, runCounter, tcCounter, tc.OutputMessage);
                                                    testcase.Item = new testsuiteTestcaseFailure()
                                                    {
                                                        type = "skipped",
                                                        Value = tc.GetLastFailure()
                                                    };
                                                    testsuite.failures++;
                                                }
                                                else
                                                {
                                                    try
                                                    {
                                                        if (_cmdLineSettings.RunMode != RunModes.Readonly || !ZatsSettings.SaveInclusionState)
                                                        {
                                                            if (_cmdLineSettings.RunMode == RunModes.Clean ||
                                                                _cmdLineSettings.RunMode == RunModes.Overwrite && isFirstTCRunIndex)
                                                            {
                                                                isFirstTCRunIndex = false;
                                                                managersContainer.DeviceManager.EraseAllBoards(ActiveProfile);
                                                                ZatsUtils.SaveZatsCache(zatsCache, Directory.GetCurrentDirectory());
                                                            }
                                                            else
                                                            {
                                                                managersContainer.DeviceManager.ResetAllBoards(ActiveProfile);
                                                            }
                                                            consoleOutPut.ConsoleWriteLine();
                                                        }
                                                        managersContainer.SetSubscribtionForServicesLogs(consoleOutPut.ReportToConsole);
                                                        managersContainer.ResetFilterServicesLogs(new[] { LogLevels.Diagnostic });
                                                        managersContainer.OpenStorageWriters();

                                                        setupSucceeded = zatsEnvironment.SetUp(ActiveProfile, tcDefinition,
                                                            _cmdLineSettings, ZatsSettings, consoleOutPut, rfRegion, lrChannel, runCounter, tcCounter);

                                                        servicesContainer.SetupTestCaseWithServices(ref tc);
                                                        servicesContainer.ResetServicesSettings(ref tc);
                                                        ZatsUtils.SaveZatsCache(zatsCache, Directory.GetCurrentDirectory());
                                                        if (setupSucceeded)
                                                        {
                                                            StorageHeader.Comment = ActiveProfile.Name;
                                                            connectSucceeded = zatsEnvironment.Connect(_cmdLineSettings);
                                                            consoleOutPut.ReportConnectionStatusToConsole(managersContainer, tcDefinition);
                                                        }
                                                        _stopWatch.Reset();
                                                        _stopWatch.Start();
                                                        if (setupSucceeded && connectSucceeded)
                                                        {
                                                            managersContainer.ResetFilterServicesLogs(null);
                                                            managersContainer.SetSubscriptionOnKeyLog();
                                                            try
                                                            {
                                                                if (_cmdLineSettings.TimeoutSeconds > 0)
                                                                {
                                                                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_cmdLineSettings.TimeoutSeconds));
                                                                    var testCaseRunTask = Task.Run(() =>
                                                                    {
                                                                        try
                                                                        {
                                                                            using (cts.Token.Register(Thread.CurrentThread.Interrupt))
                                                                            {
                                                                                res = tc.Execute();
                                                                            }
                                                                        }
                                                                        catch (ThreadInterruptedException)
                                                                        {
                                                                            //consoleOutPut.ConsoleWriteLine("Test Case terminated by timeout", LogLevels.Error, true);
                                                                        }
                                                                        finally
                                                                        {
                                                                            managersContainer.PCControllerDeviceManager.Dispose();
                                                                            cts.Cancel();
                                                                            cts.Dispose();
                                                                        }
                                                                    }, cts.Token);
                                                                    testCaseRunTask.Wait();
                                                                }
                                                                else
                                                                {
                                                                    using (var otel = new TelemetryProvider())
                                                                    {
                                                                        otel.BuildProvider(tcDefinition.Name, new[] { "DeviceManager", "ZnifferManager" });
                                                                        managersContainer.DeviceManager.TelemetryClient =
                                                                            otel.CreateTelemetryClient("DeviceManager");
                                                                        managersContainer.ZnifferManager.TelemetryClient =
                                                                            otel.CreateTelemetryClient("ZnifferManager");
                                                                        Task.Run(() =>
                                                                        {
                                                                            res = tc.Execute();
                                                                        }).Wait();
                                                                    }
                                                                }
                                                            }
                                                            catch (AggregateException taskEx)
                                                            {
                                                                if (taskEx.InnerException != null && !(taskEx.InnerException is ThreadAbortException))
                                                                {
                                                                    throw taskEx.InnerException;
                                                                }
                                                                res = TestCaseResult.Skip;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            res = TestCaseResult.Skip;
                                                            consoleOutPut.ReportToConsole("Not enough connected devices", "*", LogLevels.Fail);
                                                            consoleOutPut.ConsoleWriteLine("Detected devices:");
                                                            consoleOutPut.ReportDevicesToConsole(managersContainer.DeviceManager.HostsWithKnownVersion);
                                                            tc.AddFailure("Not enough connected devices");
                                                        }
                                                        _stopWatch.Stop();
                                                        consoleOutPut.ReportResultToConsole(tcDefinition, res, _stopWatch, runCounter, tcCounter, tc.OutputMessage);
                                                        testcase.time = _stopWatch.ElapsedMilliseconds / 1000;
                                                        testsuite.time += testcase.time;
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        res = TestCaseResult.Skip;
                                                        consoleOutPut.ReportToConsole("Test Case initialization failed", "*", LogLevels.Error);
                                                        consoleOutPut.ReportResultToConsole(tcDefinition, res, _stopWatch, runCounter, tcCounter, tc.OutputMessage);
                                                        consoleOutPut.ConsoleWriteLine(e.ToString(), LogLevels.Information);
                                                        consoleOutPut.ConsoleWriteLine("Test threw an internal exception and was skipped", LogLevels.Error);
                                                    }
                                                    finally
                                                    {
                                                        managersContainer.PCControllerDeviceManager.Dispose();
                                                        managersContainer.RemoveSubscribtionFromServicesLogs();
                                                        managersContainer.RemoveSubscriptionOnKeyLog();
                                                        managersContainer.DeviceManager.FinalizeDevices();
                                                        managersContainer.EmulatedDeviceManager.FinalizeDevices();
                                                        managersContainer.DebugDeviceManager.FinalizeDevices();
                                                        managersContainer.ZipManager.FinalizeDevices();
                                                        managersContainer.ZipManager.InterfaceName = null;
                                                        managersContainer.ZnifferManager.FinalizeDevice();
                                                        managersContainer.ReportManager.FinalizeDevice();
                                                        managersContainer.MeshRouterManager.FinalizeDevices();

                                                        managersContainer.CloseStorageWriters();
                                                        Thread.Sleep(300);
                                                        if (_cmdLineSettings.RunServiceType == RunServiceType.PCControllerUI)
                                                        {
                                                            Tools.CloseDebugLogFile();
                                                        }
                                                    }
                                                }

                                                if (_cmdLineSettings.NumberOfRuns > 1 || res == TestCaseResult.Fail)
                                                {
                                                    extraTCs.Add(tcDefinition);
                                                }

                                                if (res == TestCaseResult.Pass)
                                                {
                                                }
                                                else if (res == TestCaseResult.Fail)
                                                {
                                                    testcase.Item = new testsuiteTestcaseFailure()
                                                    {
                                                        type = "fail",
                                                        message = tc.GetLastFailure()
                                                    };
                                                    testsuite.failures++;
                                                    ret = 1;
                                                }
                                                else if (res == TestCaseResult.Skip)
                                                {
                                                    testcase.Item = new testsuiteTestcaseSkipped()
                                                    {
                                                        type = "skipped",
                                                        Value = tc.GetLastFailure()
                                                    };
                                                    testsuite.skipped++;
                                                    testsuite.skippedSpecified = true;
                                                    ret = 1;
                                                }
                                            }
                                            else
                                            {
                                                testcase.Item = new testsuiteTestcaseSkipped()
                                                {
                                                    type = "skipped",
                                                    Value = "Previous Test Case failed."
                                                };
                                                testsuite.skipped++;
                                                testsuite.skippedSpecified = true;
                                            }
                                            ZatsUtils.SaveZatsTestSuites(testsuites, managersContainer.ReportManager.TestRunFolder);
                                        }
                                    }

                                }

                            }
                        }
                    }
                    ZatsUtils.SaveZatsTestSuites(testsuites, managersContainer.ReportManager.TestRunFolder);
                    //File.Copy("TestResult.xml", Path.Combine(managersContainer.ReportManager.TestRunFolder, "TestResult.xml"), true);
                    managersContainer.Dispose();
                    consoleOutPut.ReportToConsole($"Test Run Completed in {(DateTime.Now - startTestRun).TotalSeconds:N4} sec", "", LogLevels.Diagnostic);
                }
                else
                {
                    consoleOutPut.ReportToConsole($"Test Run Completed without any test", "", LogLevels.Diagnostic);
                }
            }
            else
            {
                consoleOutPut.ConsoleWriteLine("ZATS XML Settings of Profile not found!");
            }
            return ret;
        }

        private TestCaseDefinition GetNextTcDefinition(List<TestCaseDefinition> testCases, TestCaseDefinition tcDefinitionHigh)
        {
            TestCaseDefinition tcDefinition;
            var listA = tcDefinitionHigh.Devices
                             .Select(x => $"{x.Value.GetLibrary(int.Parse(_cmdLineSettings.ChipSeries)).ToString()}_{x.Value.InterfaceType.ToString()}_{x.Value.RfRegion.ToString()}_{x.Value.DevKit.ToString()}")
                             .GroupBy(x => x);
            tcDefinition = testCases.FirstOrDefault(t =>
            {
                var listB = t.Devices
                .Select(x => $"{x.Value.GetLibrary(int.Parse(_cmdLineSettings.ChipSeries)).ToString()}_{x.Value.InterfaceType.ToString()}_{x.Value.RfRegion.ToString()}_{x.Value.DevKit.ToString()}")
                .GroupBy(x => x);
                return listB.All(b => listA.FirstOrDefault(a => a.Key == b.Key && b.Count() <= a.Count()) != null);
            });
            return tcDefinition;
        }

        private bool IsProfileContainsRemoteOrZipDevicesOnly(Profile activeProfile)
        {
            return activeProfile.DeviceHosts.All(x => x.SType == InterfaceTypes.DTLS1 ||
                x.SType == InterfaceTypes.HTTP ||
                x.SType == InterfaceTypes.UDP);
        }

        private bool IsProfileContainsMeshRouter(Profile activeProfile)
        {
            return activeProfile.DeviceHosts.Any(x => x.SType == InterfaceTypes.HTTP &&
                x.RApiType == RemoteApiTypes.MeshRouter);
        }

        private List<string> GetSerialNumbersToFilter(string serialNumbersFilter)
        {
            return ConvertFilterToList(serialNumbersFilter, (x) => int.TryParse(x, out var tmp));
        }

        private List<string> ConvertFilterToList(string filter)
        {
            return ConvertFilterToList(filter, (x) => true);
        }

        private List<string> ConvertFilterToList(string filter, Func<string, bool> validationCallback)
        {
            List<string> ret = null;
            if (!string.IsNullOrEmpty(filter))
            {
                ret = new List<string>();
                string[] tokens = filter.Split(';', ',', ' ');
                if (tokens != null)
                {
                    foreach (var item in tokens)
                    {
                        if (File.Exists(item))
                        {
                            var content = File.ReadAllLines(item);
                            if (content != null)
                            {
                                foreach (var line in content.Where(x => !string.IsNullOrEmpty(x)))
                                {
                                    ParseLineInputFilter(ret, line, validationCallback);
                                }
                            }
                        }
                        else
                        {
                            ParseLineInputFilter(ret, item, validationCallback);
                        }
                    }
                }
                ret = ret.Distinct().ToList();
            }
            return ret;
        }

        private void ParseLineInputFilter(List<string> ret, string line, Func<string, bool> validationCallback)
        {
            string[] entries = line.Split(new char[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var entry in entries)
            {
                if (validationCallback(entry))
                {
                    ret.Add(entry);
                }
            }
        }

        private List<TestCaseDefinition> PrepareTestCaseMetadata(List<Tuple<Type, string, string>> testTypes, ConsoleCustomOutput consoleOutPut, string dutName, string frequency)
        {
            List<TestCaseDefinition> ret = new List<TestCaseDefinition>();
            foreach (var testType in testTypes)
            {
                var tcDefinition = new TestCaseDefinition(testType.Item1, testType.Item2, testType.Item3);
                if (!string.IsNullOrWhiteSpace(dutName))
                {
                    tcDefinition.GroupingKey += $"-{dutName}";
                }
                if (!string.IsNullOrWhiteSpace(frequency))
                {
                    tcDefinition.GroupingKey += $"-{frequency}";
                }
                ret.Add(tcDefinition);
                tcDefinition.Name = testType.Item1.Name;
                foreach (ZatsTestCaseAttribute item in testType.Item1.GetCustomAttributes(typeof(ZatsTestCaseAttribute), false))
                {
                    tcDefinition.Description = item.Description;
                }
                var deviceAttributesManager = new DeviceAttributesService(testType.Item1);
                tcDefinition.Instruments = new SortedList<Alias, InstrumentMappingAttribute>();
                tcDefinition.Devices = new SortedList<Alias, DeviceMappingAttribute>();
                tcDefinition.DeviceGroups = new SortedList<Alias, MultiDeviceMappingAttribute>();
                tcDefinition.DebugDevices = new SortedList<Alias, DebugDeviceMappingAttribute>();
                tcDefinition.ZipDevices = new SortedList<Alias, ZipDeviceMappingAttribute>();
                tcDefinition.MeshRouterDevices = new SortedList<Alias, MeshNetworkRouterMappingAttribute>();
                tcDefinition.UicDevices = new SortedList<Alias, UicDeviceMappingAttribute>();
                tcDefinition.ZigDevices = new SortedList<Alias, ZigDeviceMappingAttribute>();
                tcDefinition.SshDevices = new SortedList<Alias, SshDeviceMappingAttribute>();
                tcDefinition.DisableOptions = new SortedList<ZWaveSet, Alias[]>();
                tcDefinition.EnableOptions = new SortedList<ZWaveSet, Alias[]>();
                tcDefinition.IsZnifferRequired = testType.Item1.GetCustomAttributes(typeof(ZatsTestCaseZnifferRequiredAttribute), false).Any();

                foreach (var tcOption in deviceAttributesManager.TestCaseOptions)
                {
                    if (tcOption.IsEnabled)
                    {
                        tcDefinition.EnableOptions.Add(tcOption.Parameter, tcOption.Numbers);
                    }
                    else
                    {
                        tcDefinition.DisableOptions.Add(tcOption.Parameter, tcOption.Numbers);
                    }
                }
                FillRequiredDevicesFromAttributes(deviceAttributesManager.Instruments, tcDefinition.Instruments, true, consoleOutPut);
                FillRequiredDevicesFromAttributes(deviceAttributesManager.Devices, tcDefinition.Devices, true, consoleOutPut);
                FillRequiredDevicesFromAttributes(deviceAttributesManager.DeviceGroups, tcDefinition.DeviceGroups, true, consoleOutPut);
                FillRequiredDevicesFromAttributes(deviceAttributesManager.TestInterfaceDevices, tcDefinition.Devices);
                FillRequiredDevicesFromAttributes(deviceAttributesManager.DebugDevices, tcDefinition.DebugDevices);
                FillRequiredDevicesFromAttributes(deviceAttributesManager.ZipDevices, tcDefinition.ZipDevices, true, consoleOutPut);
                FillRequiredDevicesFromAttributes(deviceAttributesManager.MeshRouters, tcDefinition.MeshRouterDevices, true, consoleOutPut);
                FillRequiredDevicesFromAttributes(deviceAttributesManager.UicDevices, tcDefinition.UicDevices, true, consoleOutPut);
                FillRequiredDevicesFromAttributes(deviceAttributesManager.ZigDevices, tcDefinition.ZigDevices, true, consoleOutPut);
                FillRequiredDevicesFromAttributes(deviceAttributesManager.SshDevices, tcDefinition.SshDevices, true, consoleOutPut);
            }
            return ret;
        }

        private void FillRequiredDevicesFromAttributes<TAttr>(TAttr[] source, SortedList<Alias, TAttr> dest, bool warnAboutDuplicates = true, ConsoleCustomOutput output = null) where TAttr : WithNumberBaseAttribute
        {
            foreach (var item in source)
            {
                if (!dest.ContainsKey(new Alias(item.Number)))
                    dest.Add(new Alias(item.Number), item);
                else
                {
                    if (warnAboutDuplicates)
                        output?.ConsoleWriteLine("Check TC for duplicate mappings!", LogLevels.Error);
                }
            }
        }

        private void FillRequiredMultiDevicesFromAttributes<TAttr>(TAttr[] source, SortedList<int, TAttr> dest, bool warnAboutDuplicates = true, ConsoleCustomOutput output = null) where TAttr : WithPercentBaseAttribute
        {
            foreach (var item in source)
            {
                //if (!dest.ContainsKey(item.Percent))
                //    dest.Add(item.Percent, item);
                //else
                {
                    if (warnAboutDuplicates)
                        output?.ConsoleWriteLine("Check TC for duplicate mappings!", LogLevels.Error);
                }
            }
        }

        private static string GetStringMetaData(TestCaseDefinition definition, int generation)
        {
            var serialLibs = definition.Devices
                .DefaultIfEmpty()
                .Select(x => $"{x.Value.GetLibrary(generation).ToString()}_{x.Value.InterfaceType.ToString()}_{x.Value.RfRegion.ToString()}_{x.Value.DevKit.ToString()}")
                .OrderBy(x => x)
                .GroupBy(x => x)
                .Select(x => $"[{x.Count()}]{x.Key}")
                .Aggregate((x, y) => x + ", " + y);
            return definition.Location + ":" + serialLibs;
        }

        private static void ParseInputFilter(Dictionary<string, List<string>> inputFilter, string line, string dutName, string[] frequencies)
        {
            var infs = line.Split(';', ',', ' ');
            foreach (var inf in infs)
            {
                var f = inf.Trim();

                if (!string.IsNullOrEmpty(f) && !f.StartsWith("#") && !f.StartsWith("//") && !f.StartsWith("-"))
                {
                    if (f.StartsWith(":"))
                    {
                        var groupingKey = string.Concat(line.Replace(":", "").Take(64));
                        if (!inputFilter.ContainsKey(groupingKey))
                        {
                            inputFilter.Add(groupingKey, new List<string>());
                        }
                    }
                    else
                    {
                        if (inputFilter.Count == 0)
                        {
                            var groupingKey = "Custom";
                            inputFilter.Add(groupingKey, new List<string>());
                        }
                        inputFilter.Last().Value.Add(f);
                    }
                }
            }
        }
    }
}

