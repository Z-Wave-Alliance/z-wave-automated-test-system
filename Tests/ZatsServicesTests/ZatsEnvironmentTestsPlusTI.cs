// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Moq;
using NUnit.Framework;
using Utils;
using Zats.Services;
using Zats.Services.MeshNetworkRacks;
using ZatsTestConsole;
using ZWave;
using ZWave.BasicApplication.Operations;
using ZWave.Enums;
using ZWave.Layers;
using ZWave.Operations;

namespace ZatsServicesTests
{
    [TestFixture] // no PName => no TI!
    class ZatsEnvironmentTestsPlusTI
    {
        /*
         * requestDevices - Setup requested device - main test goal
         * Test are cover only DeviceManager any other - no
         * (DB) - different behavior?
         */

        const string Five = "500s";
        const string Seven = "Z-Wave 7";

        private static bool RunSetupReadMode(Dictionary<string, Tuple<Libraries, DeviceHost>> actualVersions,
            SortedList<Alias, DeviceMappingAttribute> requestDevices,
            ZatsEnvironment zatsEnvironment)
        {
            var cmdLineSettings = new CmdLineSettings();
            var frequency = "Freq";
            cmdLineSettings.Frequencies = new[] { frequency };
            cmdLineSettings.AssemblyFile = "lib.dll";
            cmdLineSettings.RunMode = RunModes.Readonly;
            var zatsSettings = new ZatsSettings();

            var testCaseDefinition = new TestCaseDefinition(null, "", "");
            testCaseDefinition.Devices = requestDevices;
            testCaseDefinition.Instruments = new SortedList<Alias, InstrumentMappingAttribute>();
            testCaseDefinition.DebugDevices = new SortedList<Alias, DebugDeviceMappingAttribute>();
            testCaseDefinition.ZipDevices = new SortedList<Alias, ZipDeviceMappingAttribute>();
            testCaseDefinition.MeshRouterDevices = new SortedList<Alias, MeshNetworkRouterMappingAttribute>();
            testCaseDefinition.UicDevices = new SortedList<Alias, UicDeviceMappingAttribute>();
            testCaseDefinition.ZigDevices = new SortedList<Alias, ZigDeviceMappingAttribute>();
            testCaseDefinition.SshDevices = new SortedList<Alias, SshDeviceMappingAttribute>();
            testCaseDefinition.DeviceGroups = new SortedList<Alias, MultiDeviceMappingAttribute>();

            var consoleOutPut = new ConsoleCustomOutput(false, false);
            Profile profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            foreach (var item in actualVersions.Select(x => x.Value.Item2))
            {
                profile.DeviceHosts.Add(item);
            }

            var actualSetupResult = zatsEnvironment.SetUp(profile, testCaseDefinition, cmdLineSettings, zatsSettings, consoleOutPut, frequency, 1, 1);
            return actualSetupResult;
        }

        private static ZatsEnvironment CreateZatsEnvironment(Dictionary<string, Tuple<Libraries, DeviceHost>> actualVersions, out DeviceManager dm)
        {
            List<string> usedSrcs = new List<string>();

            var fileSystemManagerMock = new Mock<IFileSystemManager>();
            fileSystemManagerMock.Setup(x => x.GetLatestFilePath(It.IsAny<Libraries>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DevKits>(), It.IsAny<string[]>())).Returns("string FGx");
            fileSystemManagerMock.Setup(x => x.GetLatestFilePath(It.IsAny<Libraries>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DevKits>(), It.IsAny<string[]>())).Returns("string FGx");

            var alphaProgrammerMock = new Mock<IProgrammingProvider>();
            alphaProgrammerMock.Setup(x => x.ProgramSample(It.IsAny<DeviceHostClient>(), It.IsAny<ZWaveRegions?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(0);

            var gammaProgrammerMock = new Mock<IProgrammingProvider>();
            gammaProgrammerMock.Setup(x => x.ProgramSample(It.IsAny<DeviceHostClient>(), It.IsAny<ZWaveRegions?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(0);
            gammaProgrammerMock.Setup(x => x.ProgramSampleWithBootLoaderKeys(It.IsAny<DeviceHostClient>(), It.IsAny<ZWaveRegions?>(), It.IsAny<string>(), It.IsAny<string[]>())).Returns(0);

            var basicSessionLayerMock = new Mock<ISessionLayer>();
            basicSessionLayerMock.Setup(x => x.CreateClient(It.IsAny<ushort>()))
                .Returns(() =>
                {
                    var ret = new Mock<ISessionClient>();
                    ret.SetupProperty(y => y.SessionId);
                    ret.SetupProperty(y => y.IsHandleFrameEnabled);
                    ret.Setup(y => y.ExecuteAsync(It.Is<IActionItem>(f => f is ActionBase)))
                    .Returns((IActionItem a) => ((ActionBase)a).Token)
                    .Callback((IActionItem a) =>
                    {
                        ((ActionBase)a).Token.SetCompleted();
                        ((ActionBase)a).Token.SetCompletedSignal();
                    });

                    ret.Setup(y => y.ExecuteAsync(It.Is<IActionItem>(f => f is SerialApiGetInitDataOperation)))
                    .Returns((IActionItem a) => ((ActionBase)a).Token)
                    .Callback((IActionItem a) =>
                    {
                        var act = actualVersions[((ActionBase)a).Token.LogEntryPointSource];
                        if (!DeviceManager.IsSampleApplication(act.Item1))
                        {
                            ((SerialApiGetInitDataOperation)a).SpecificResult.ChipType = !string.IsNullOrEmpty(act.Item2.JName) ? ChipTypes.ZW070x : ChipTypes.ZW050x;
                            ((ActionBase)a).Token.SetCompleted();
                        }
                        else
                        {
                            ((ActionBase)a).SetExpired();
                        }
                        ((ActionBase)a).Token.SetCompletedSignal();
                    });

                    ret.Setup(y => y.ExecuteAsync(It.Is<IActionItem>(f => f is VersionOperation)))
                    .Returns((IActionItem a) => ((ActionBase)a).Token)
                    .Callback((IActionItem a) =>
                    {
                        var act = actualVersions[((ActionBase)a).Token.LogEntryPointSource];
                        if (!DeviceManager.IsSampleApplication(act.Item1))
                        {
                            ((VersionOperation)a).SpecificResult.Library = act.Item1;
                            ((VersionOperation)a).SpecificResult.Version = !string.IsNullOrEmpty(act.Item2.JName) ? Seven : Five;
                            ((ActionBase)a).Token.SetCompleted();
                        }
                        else
                        {
                            ((ActionBase)a).SetExpired();
                        }
                        ((ActionBase)a).Token.SetCompletedSignal();
                    });

                    ret.Setup(y => y.ExecuteAsync(It.Is<IActionItem>(f => f is RequestOperation)))
                    .Returns((IActionItem a) => ((ActionBase)a).Token)
                    .Callback((IActionItem a) =>
                    {
                        var act = actualVersions[((ActionBase)a).Token.LogEntryPointSource];
                        if (((RequestOperation)a).Data.SequenceEqual("00 15".GetBytes()))
                        {
                            if (!DeviceManager.IsSampleApplication(act.Item1))
                            {
                                ((RequestOperation)a).SpecificResult.Payload = GetVersion(!string.IsNullOrEmpty(act.Item2.JName) ? Seven : Five)
                                .Concat(new[] { (byte)act.Item1 })
                                .ToArray();
                                ((ActionBase)a).Token.SetCompleted();
                            }
                            else
                            {
                                ((ActionBase)a).SetExpired();
                            }
                        }
                        else
                        {
                            ((ActionBase)a).Token.SetCompleted();
                        }
                        ((ActionBase)a).Token.SetCompletedSignal();
                    });

                    return ret.Object;
                });

            var basicFrameLayer = Mock.Of<IFrameLayer>((x => x.CreateClient(It.IsAny<ushort>()) == Mock.Of<IFrameClient>()));
            var basicTransportLayerMock = new Mock<ITransportLayer>();
            basicTransportLayerMock.Setup(x => x.CreateClient(It.IsAny<ushort>()))
                .Returns(() =>
                {
                    var dsMock = new Mock<IDataSource>();
                    var ret = new Mock<ITransportClient>();
                    dsMock.SetupAllProperties();
                    ret.SetupProperty(y => y.DataSource, dsMock.Object);
                    ret.Setup(y => y.Connect(It.IsAny<IDataSource>())).Returns(CommunicationStatuses.Done).
                    Callback((IDataSource ds) =>
                    {
                        lock (usedSrcs)
                        {
                            usedSrcs.Add(ds.SourceName);
                        }
                        lock (ds)
                        {
                            dsMock.Setup(x => x.SourceName).Returns(ds.SourceName);
                            dsMock.Setup(x => x.Args).Returns(ds.Args);
                            dsMock.Setup(x => x.Alias).Returns(ds.Alias);
                            ret.SetupProperty(y => y.DataSource, dsMock.Object);
                        }
                    });
                    return ret.Object;
                });

            var basicApp = new ZWave.BasicApplication.BasicApplicationLayer(
               basicSessionLayerMock.Object,
               basicFrameLayer,
               basicTransportLayerMock.Object);

            Profile profile = new Profile() { IsAddS2Keys = false };
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            var zc = new ZatsCache();
            zc.ProgrammingResults = new Collection<ProgrammingResult>();
            foreach (var item in actualVersions)
            {
                profile.DeviceHosts.Add(item.Value.Item2);
                var pName = item.Value.Item2.JName ?? item.Value.Item2.PName;
                if (!string.IsNullOrEmpty(pName))
                {
                    zc.ProgrammingResults.Add(new ProgrammingResult() { PName = pName, AppName = item.Value.Item1.ToString() });
                }
            }

            dm = new DeviceManager(false, alphaProgrammerMock.Object, gammaProgrammerMock.Object, fileSystemManagerMock.Object, null);
            dm.IsSkipCheckPortExists = true;
            dm.BasicApplicationLayer = basicApp;
            dm.ZatsCache = zc;
            dm.CurrentProfile = profile;

            var context = new ProfileContext(dm);

            var managersContainerMock = new Mock<IManagersContainer>();
            managersContainerMock.Setup(x => x.Context).Returns(context);
            managersContainerMock.Setup(x => x.DeviceManager).Returns(dm);
            managersContainerMock.Setup(x => x.ZnifferManager).Returns(new ZnifferManager(new ZWave.Xml.FrameHeader.FrameDefinition()));
            managersContainerMock.Setup(x => x.ZipManager).Returns(new ZipManager());
            managersContainerMock.Setup(x => x.ReportManager).Returns(new ReportManager());
            managersContainerMock.Setup(x => x.DebugDeviceManager).Returns(new DebugDeviceManager());
            var emulatedDeviceManagerMock = new Mock<IEmulatedDeviceManager>();
            managersContainerMock.Setup(x => x.EmulatedDeviceManager).Returns(emulatedDeviceManagerMock.Object);
            managersContainerMock.Setup(x => x.MeshRouterManager).Returns(new MeshRouterManager(null));

            var zatsEnvironment = new ZatsEnvironment(managersContainerMock.Object);
            return zatsEnvironment;
        }

        private static byte[] GetVersion(string version)
        {
            byte[] ret = new byte[12];
            var tmp = Encoding.UTF7.GetBytes(version);
            Array.Copy(tmp, 0, ret, 0, tmp.Length > 12 ? 12 : tmp.Length);
            return ret;
        }

        [Test]
        public void Mixed_2xAlias3xLib_3x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM101", PName = "COM44", ChipSeries = "ZW0713", JName = "440123001" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM102", PName = "COM55", ChipSeries = "ZW050x", Alias = "A" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM103", PName = "COM66", ChipSeries = "ZW050x", Alias = "B" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.NoLib, new DeviceHost()
            { SName = "COM104", PName = "COM77", ChipSeries = "ZW050x", Alias = "C" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.NoLib, new DeviceHost()
            { SName = "COM105", PName = "COM44", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM106", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM106", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM107", new Tuple<Libraries, DeviceHost>(Libraries.NoLib, new DeviceHost()
            { SName = "COM107", PName = "COM66", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM108", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM108", PName = "COM77", ChipSeries = "ZW0713", JName = "440123004" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.Controller) { Alias = "A" });
            requestDevices.Add(new Alias(2), new TestInterfaceDeviceMappingAttribute(2, ChipSeries.ZW050x, LibTypes.SwitchOnOff) { Alias = "B" });
            requestDevices.Add(new Alias(3), new TestInterfaceDeviceMappingAttribute(3, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult1 = dm.GetDevice(new Alias(1));
            var actualDeviceResult2 = dm.GetTIDevice(new Alias(2));
            var actualDeviceResult3 = dm.GetTIDevice(new Alias(3));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult1);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult2);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult3);
            NUnit.Framework.Assert.AreEqual(Five, actualDeviceResult1.Version);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerStaticLib, actualDeviceResult1.Library);
            NUnit.Framework.Assert.AreEqual("A", actualDeviceResult1.DataSource.Alias);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult2.Library);
            NUnit.Framework.Assert.AreEqual("B", actualDeviceResult2.DataSource.Alias);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult3.Library);
        }

        [Test]
        public void ZW050xs_1xLib_1x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", PName = "COM66", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x", Alias = "expected1" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", PName = "COM77", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM104", PName = "COM88", ChipSeries = "ZW050x", Alias = "expected2" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
            NUnit.Framework.Assert.AreEqual("COM77", actualDeviceResult.DataSource.SourceName);
        }

        [Test]
        public void Geckos_1xLib_1xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "123456700" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", ChipSeries = "ZW0713", JName = "123456701", PName = "COM77" }));
            //Won't take without PName:
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM104", ChipSeries = "ZW0713", JName = "123456701" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
            NUnit.Framework.Assert.AreEqual("COM77", actualDeviceResult.DataSource.SourceName);
        }

        [Test]
        public void Mixed_Lib_1xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM102", PName = "COM67", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", ChipSeries = "ZW0713", JName = "123456789", PName = "COM77" }));
            actualVersions.Add("COM100", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM100", ChipSeries = "ZW0713", JName = "123456700" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
            NUnit.Framework.Assert.AreEqual("COM77", actualDeviceResult.DataSource.SourceName);
        }

        [Test] // if no one by lib
        public void ZW050xs_1xAliasLib_1x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", PName = "COM66", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x", Alias = "expected1" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", PName = "COM77", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM104", PName = "COM88", ChipSeries = "ZW050x", Alias = "expected2" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, "expected2", LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
            NUnit.Framework.Assert.AreEqual("COM88", actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected2", actualDeviceResult.DataSource.Alias);
        }

        [Test] // if no one by lib
        public void Geckos_1xAliasLib_1xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM201", SName = "COM101", ChipSeries = "ZW0713", JName = "12345678901" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM202", SName = "COM102", ChipSeries = "ZW0713", JName = "12345678902", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM203", SName = "COM103", ChipSeries = "ZW0713", JName = "12345678903" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM204", SName = "COM104", ChipSeries = "ZW0713", JName = "12345678904", Alias = "expected" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM205", SName = "COM105", ChipSeries = "ZW0713", JName = "12345678905" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, "expected", LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
            NUnit.Framework.Assert.AreEqual("COM204", actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
        }

        [Test]
        public void Mixed_AliasLib_1x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM101", PName = "55", ChipSeries = "ZW0713", JName = "123456790" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM102", PName = "66", ChipSeries = "ZW0713", JName = "123456789", Alias = "some" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", PName = "COM88", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM104", PName = "COM77", ChipSeries = "ZW050x" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.SwitchOnOff) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
            NUnit.Framework.Assert.AreEqual("COM88", actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
        }

        [Test]
        public void Mixed_AliasLib_1xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM102", ChipSeries = "ZW0713", JName = "123456789", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", PName = "COM55", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM104", PName = "COM66", ChipSeries = "ZW0713", JName = "123456780" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.SwitchOnOff) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
            NUnit.Framework.Assert.AreEqual("COM55", actualDeviceResult.DataSource.SourceName);
        }

        [Test]
        public void ZW050xs_1xAlias_1x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", PName = "COM66", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x", Alias = "expected1" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", PName = "COM77", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM104", PName = "COM88", ChipSeries = "ZW050x", Alias = "expected2" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, "expected2"));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
            NUnit.Framework.Assert.AreEqual("COM88", actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected2", actualDeviceResult.DataSource.Alias);
        }

        [Test]
        public void Geckos_1xAlias_1xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM201", SName = "COM101", ChipSeries = "ZW0713", JName = "12345678901" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM202", SName = "COM102", ChipSeries = "ZW0713", JName = "12345678902", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM203", SName = "COM103", ChipSeries = "ZW0713", JName = "12345678903" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM204", SName = "COM104", ChipSeries = "ZW0713", JName = "12345678904", Alias = "expected" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM205", SName = "COM105", ChipSeries = "ZW0713", JName = "12345678905" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, "expected"));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
            NUnit.Framework.Assert.AreEqual("COM204", actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
        }

        [Test]
        public void Mixed_1xAlias_1x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM55", SName = "COM101", ChipSeries = "ZW0713", JName = "123456780" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM66", SName = "COM102", ChipSeries = "ZW0713", JName = "123456781", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM77", SName = "COM103", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM88", SName = "COM104", ChipSeries = "ZW050x" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, "expected"));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
            NUnit.Framework.Assert.AreEqual("COM66", actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
        }

        [Test]
        public void Mixed_1xAlias_1xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM55", SName = "COM101", ChipSeries = "ZW0713", JName = "123456780" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM66", SName = "COM102", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM77", SName = "COM103", Alias = "expected", ChipSeries = "ZW0713", JName = "123456782" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM88", SName = "COM104", ChipSeries = "ZW050x" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, "expected"));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
            NUnit.Framework.Assert.AreEqual("COM77", actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
        }

        [Test]
        //[Ignore("TestInterfaceDevice Manager Init Devices only with ChipSeries.Default!")]
        public void ZW050xs_1xAliasLibChip5_1x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM55", SName = "COM101", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM66", SName = "COM102", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM77", SName = "COM103", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM88", SName = "COM104", ChipSeries = "ZW050x" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.SwitchOnOff) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
            NUnit.Framework.Assert.AreEqual("COM77", actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
        }

        [Test]
        //[Ignore("TestInterfaceDevice Manager Init Devices only with ChipSeries.Default!")]
        public void Geckos_1xAliasLibChip7_1xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", PName = "COM55", ChipSeries = "ZW0713", JName = "123456780" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM102", PName = "COM66", Alias = "expected", ChipSeries = "ZW0713", JName = "123456781" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", PName = "COM77", Alias = "expected", ChipSeries = "ZW0713", JName = "123456782" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM104", PName = "COM88", ChipSeries = "ZW0713", JName = "123456782" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, ChipSeries.ZW070x, LibTypes.SwitchOnOff) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
            NUnit.Framework.Assert.AreEqual("COM77", actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
        }

        [Test]
        //[Ignore("TestInterfaceDevice Manager Init Devices only with ChipSeries.Default!")]
        public void ZW050xs_1xAliasLibChip7_1x5s1xAlias()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", PName = "COM55", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM102", PName = "COM66", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", PName = "COM77", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM104", PName = "COM88", ChipSeries = "ZW050x" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.SwitchOnOff) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
            NUnit.Framework.Assert.AreEqual("COM77", actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
        }

        [Test]
        //[Ignore("TestInterfaceDevice Manager Init Devices only with ChipSeries.Default!")]
        public void Geckos_1xAliasLibChip5_1xGk1xAlias()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM201", SName = "COM101", ChipSeries = "ZW0713", JName = "12345678901" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM202", SName = "COM102", ChipSeries = "ZW0713", JName = "12345678902", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM203", SName = "COM103", ChipSeries = "ZW0713", JName = "12345678903" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM204", SName = "COM104", ChipSeries = "ZW0713", JName = "12345678904", Alias = "expected" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM205", SName = "COM105", ChipSeries = "ZW0713", JName = "12345678905" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, ChipSeries.ZW070x, LibTypes.SwitchOnOff) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
            NUnit.Framework.Assert.AreEqual("COM204", actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
        }

        [Test]
        //[Ignore("TestInterfaceDevice Manager Init Devices only with ChipSeries.Default!")]
        public void Mixed_AliasLibChip5_1x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM55", SName = "COM101", ChipSeries = "ZW0713", JName = "123456780" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM66", SName = "COM102", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM77", SName = "COM103", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM88", SName = "COM104", ChipSeries = "ZW0713", JName = "123456782" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.SwitchOnOff) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
            NUnit.Framework.Assert.AreEqual("COM77", actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
        }


        [Test]
        //[Ignore("TestInterfaceDevice Manager Init Devices only with ChipSeries.Default!")]
        public void Mixed_1xAliasLibChip7_1xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { PName = "COM55", SName = "COM101", ChipSeries = "ZW0713", JName = "123456780" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { PName = "COM66", SName = "COM102", ChipSeries = "ZW0713", JName = "123456781", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { PName = "COM77", SName = "COM103", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { PName = "COM88", SName = "COM104", ChipSeries = "ZW050x" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.SwitchOnOff) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
            NUnit.Framework.Assert.AreEqual("COM77", actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);

        }

        //TODO: differnt oreder requested devices and profile
        [Test]
        public void MixedAliasOrderLast_AliasLibChip7__1x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", PName = "COM55", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM102", PName = "COM66", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", PName = "COM76", ChipSeries = "ZW050x", Alias = "not_expected" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM104", PName = "COM77", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM105", PName = "COM88", ChipSeries = "ZW050x" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1,/* ChipSeries.ZW070x,*/ LibTypes.SwitchOnOff) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
            NUnit.Framework.Assert.AreEqual("COM77", actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);

        }

        [Test(Description = "Skip Chip - searched by alias")]
        //[Ignore("Fails due to order of device hosts")]
        public void MixedAliasOrderFirst_AliasLibChip7_1x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", PName = "COM55", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM102", PName = "COM66", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", PName = "COM76", ChipSeries = "ZW050x", Alias = "not_expected" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM104", PName = "COM77", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM105", PName = "COM88", ChipSeries = "ZW050x" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, /*ChipSeries.ZW070x,*/ LibTypes.SwitchOnOff) { Alias = "not_expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
            NUnit.Framework.Assert.AreEqual("COM76", actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("not_expected", actualDeviceResult.DataSource.Alias);

        }

        [Test]
        //[Ignore("TestInterfaceDevice Manager Init Devices only with ChipSeries.Default!")]
        public void ZW050xs_1xLibChip5_1x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", PName = "COM66", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x", Alias = "expected1" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", PName = "COM77", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM104", PName = "COM88", ChipSeries = "ZW050x", Alias = "expected2" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
            NUnit.Framework.Assert.AreEqual("COM77", actualDeviceResult.DataSource.SourceName);
        }

        [Test]
        //[Ignore("TestInterfaceDevice Manager Init Devices only with ChipSeries.Default!")]
        public void Geckos_1xLibChip7_1xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "123456780", PName = "COM55" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM102", ChipSeries = "ZW0713", JName = "123456781", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", PName = "COM77", Alias = "expected", ChipSeries = "ZW0713", JName = "123456782" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM104", PName = "COM88", ChipSeries = "ZW0713", JName = "123456783" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, ChipSeries.ZW070x, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
            NUnit.Framework.Assert.AreEqual("COM77", actualDeviceResult.DataSource.SourceName);
        }

        [Test]
        //[Ignore("TestInterfaceDevice Manager Init Devices only with ChipSeries.Default!")]
        public void Mixed_LibChip5_1x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM55", SName = "COM101", ChipSeries = "ZW0713", JName = "123456780" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM66", SName = "COM102", ChipSeries = "ZW0713", JName = "123456781", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM76", SName = "COM103", ChipSeries = "ZW050x", Alias = "not_expected" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM77", SName = "COM104", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM88", SName = "COM105", ChipSeries = "ZW050x" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
        }

        [Test]
        //[Ignore("TestInterfaceDevice Manager Init Devices only with ChipSeries.Default!")]
        public void Mixed_1xLibChip7_1xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM55", SName = "COM101", ChipSeries = "ZW0713", JName = "123456780" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM66", SName = "COM102", ChipSeries = "ZW0713", JName = "123456781", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM77", SName = "COM103", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM88", SName = "COM104", ChipSeries = "ZW050x", Alias = "not_expected" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM99", SName = "COM105", ChipSeries = "ZW050x" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, ChipSeries.ZW070x, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
            NUnit.Framework.Assert.AreEqual("COM55", actualDeviceResult.DataSource.SourceName);
        }

        // Tests with Init fail expected, one device requested
        //TODO: "Not enough devices" Request more than exists in profile

        [Test]
        public void ZW050xs_1xLib_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", PName = "COM66", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x", Alias = "expected1" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost() { SName = "COM103", PName = "COM77", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost() { SName = "COM104", PName = "COM88", ChipSeries = "ZW050x", Alias = "expected2" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test]
        public void Geckos_1xLib_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM55", SName = "COM101", ChipSeries = "ZW0713", JName = "123456780" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM66", SName = "COM102", ChipSeries = "ZW0713", JName = "123456781", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM77", SName = "COM103", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM88", SName = "COM104", ChipSeries = "ZW050x", Alias = "not_expected" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM99", SName = "COM105", ChipSeries = "ZW050x" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, LibTypes.WallController));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test]
        public void Mixed_1xLib_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM55", SName = "COM101", ChipSeries = "ZW0713", JName = "123456780" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM66", SName = "COM102", ChipSeries = "ZW0713", JName = "123456781", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM77", SName = "COM103", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM88", SName = "COM104", ChipSeries = "ZW050x", Alias = "not_expected" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM99", SName = "COM105", ChipSeries = "ZW050x" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, LibTypes.WallController));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test]
        public void ZW050xs_1xAliasLib_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x", Alias = "expected1" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", PName = "COM77", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM104", PName = "COM88", ChipSeries = "ZW050x", Alias = "expected2" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(2, "expected1", LibTypes.WallController));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test]
        public void ZW050xs_2xAliasLib_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x", Alias = "expected1" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", PName = "COM77", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM104", PName = "COM88", ChipSeries = "ZW050x", Alias = "expected2" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, "expected2", LibTypes.Controller));
            requestDevices.Add(new Alias(2), new TestInterfaceDeviceMappingAttribute(2, "expected1", LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test]
        //[Ignore("Doesn't takes in account specified lib and takes only by Alias")]
        public void Geckos_1xAliasLib_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM201", SName = "COM101", ChipSeries = "ZW0713", JName = "12345678901" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM202", SName = "COM102", ChipSeries = "ZW0713", JName = "12345678902", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM203", SName = "COM103", ChipSeries = "ZW0713", JName = "12345678903" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM204", SName = "COM104", ChipSeries = "ZW0713", JName = "12345678904", Alias = "expected" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM205", SName = "COM105", ChipSeries = "ZW0713", JName = "12345678905" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, "expected", LibTypes.WallController));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test]
        public void Geckos_1xAnyAliasLib_1xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "12345678901" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM102", ChipSeries = "ZW0713", JName = "12345678902", Alias = "expected1" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost() { SName = "COM103", PName = "COM203", ChipSeries = "ZW0713", JName = "12345678903" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM104", PName = "COM204", ChipSeries = "ZW0713", JName = "12345678903" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM105", PName = "COM205", ChipSeries = "ZW0713", JName = "12345678904", Alias = "expected2" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, "any", LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(1)).Library);
            NUnit.Framework.Assert.AreEqual("COM205", dm.GetTIDevice(new Alias(1)).DataSource.SourceName);
        }

        [Test]
        public void Mixed_1xAliasAnyLib_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { PName = "COM55", SName = "COM101", ChipSeries = "ZW0713", JName = "123456780" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { PName = "COM66", SName = "COM102", ChipSeries = "ZW0713", JName = "123456781", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { PName = "COM77", SName = "COM103", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { PName = "COM88", SName = "COM104", ChipSeries = "ZW050x", Alias = "not_expected" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { PName = "COM99", SName = "COM105", ChipSeries = "ZW050x" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, "any", LibTypes.WallController));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test]
        //[Ignore("Check correctness")]
        public void ZW050xs_1xAliasAny_1x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", PName = "COM77", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM104", PName = "COM88", ChipSeries = "ZW050x" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, "any"));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(1)).Library);
        }

        [Test]
        public void Geckos_1xAliasAny_1xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM201", SName = "COM101", ChipSeries = "ZW0713", JName = "12345678901" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM202", SName = "COM102", ChipSeries = "ZW0713", JName = "12345678902", Alias = "expected2" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM203", SName = "COM103", ChipSeries = "ZW0713", JName = "12345678903" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM204", SName = "COM104", ChipSeries = "ZW0713", JName = "12345678904", Alias = "expected1" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM205", SName = "COM105", ChipSeries = "ZW0713", JName = "12345678905" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, "Any"));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var deviceRes = dm.GetTIDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, deviceRes.Library);
        }

        [Test]
        //[Ignore("Check corectness")]
        public void Mixed_1xAliasAny_1xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM55", SName = "COM101", ChipSeries = "ZW0713", JName = "123456780" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM66", SName = "COM102", ChipSeries = "ZW0713", JName = "123456781" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM77", SName = "COM103", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM88", SName = "COM104", ChipSeries = "ZW050x" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, "any"));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(1)).Library);
            NUnit.Framework.Assert.AreEqual("COM55", dm.GetTIDevice(new Alias(1)).DataSource.SourceName);
        }

        [Test(Description = "Check on real setup (DB), maybe would be a good idea to disable map device only by Alias")]
        //[Ignore("TestInterfaceDevice Manager Init Devices only with ChipSeries.Default!")]
        public void ZW050xs_1xAliasLibChip5_1x5sByAlias()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", PName = "COM77", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM104", PName = "COM88", ChipSeries = "ZW050x", Alias = "expected" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.SwitchOnOff) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(1)).Library);
        }

        [Test(Description = "Check lib requirement if specified alias")]
        public void Geckos_1xAliasLibChip5_1xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM201", SName = "COM101", ChipSeries = "ZW0713", JName = "12345678901" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM202", SName = "COM102", ChipSeries = "ZW0713", JName = "12345678902", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM203", SName = "COM103", ChipSeries = "ZW0713", JName = "12345678903" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM204", SName = "COM104", ChipSeries = "ZW0713", JName = "12345678904", Alias = "expected" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM205", SName = "COM105", ChipSeries = "ZW0713", JName = "12345678905" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.SwitchOnOff) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test(Description = "Check on real setup (DB), maybe would be a good idea to disable map device only by Alias")]
        ////[Ignore("TestInterfaceDevice Manager Init Devices only with ChipSeries.Default!")]
        public void Mixed_1xAliasLibChip5_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { PName = "COM55", SName = "COM101", ChipSeries = "ZW0713", JName = "123456780" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { PName = "COM66", SName = "COM102", ChipSeries = "ZW0713", JName = "123456781", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { PName = "COM77", SName = "COM103", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { PName = "COM88", SName = "COM104", ChipSeries = "ZW050x", Alias = "not_expected" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { PName = "COM99", SName = "COM105", ChipSeries = "ZW050x" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.WallController) { Alias = "any" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test(Description = "Check on real setup (DB), maybe would be a good idea to disable map device only by Alias")]
        public void Mixed_1xAliasLibChip7_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { PName = "COM55", SName = "COM101", ChipSeries = "ZW0713", JName = "123456780" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM66", SName = "COM102", ChipSeries = "ZW0713", JName = "123456781", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { PName = "COM77", SName = "COM103", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { PName = "COM88", SName = "COM104", ChipSeries = "ZW050x", Alias = "not_expected" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { PName = "COM99", SName = "COM105", ChipSeries = "ZW050x" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, ChipSeries.ZW070x, LibTypes.WallController) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test]
        ////[Ignore("TestInterfaceDevice Manager Init Devices only with ChipSeries.Default!")]
        public void ZW050xs_1xLibChip5_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", PName = "COM66", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x", Alias = "expected1" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", PName = "COM77", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM104", PName = "COM88", ChipSeries = "ZW050x", Alias = "expected2" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.WallController));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test]
        public void Geckos_1xLibChip7_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", PName = "COM55", ChipSeries = "ZW0713", JName = "123456780" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM102", ChipSeries = "ZW0713", JName = "123456781", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", PName = "COM77", Alias = "expected", ChipSeries = "ZW0713", JName = "123456782" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM104", PName = "COM88", ChipSeries = "ZW0713", JName = "123456783" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, ChipSeries.ZW070x, LibTypes.DoorLockKeyPad));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test]
        public void Mixed_1xLibChip5_1x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost() { PName = "COM201", SName = "COM101", ChipSeries = "ZW050x", Alias = "Ctrl" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost() { PName = "COM202", SName = "COM102", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM203", SName = "COM103", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM204", SName = "COM104", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM205", SName = "COM105", ChipSeries = "ZW050x" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(1)).Library);
            NUnit.Framework.Assert.AreEqual("COM204", dm.GetTIDevice(new Alias(1)).DataSource.SourceName);
        }

        [Test]
        //[Ignore("TestInterfaceDevice Manager Init Devices only with ChipSeries.Default!")]
        public void Mixed_LibChip7_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost() { PName = "COM201", SName = "COM101", ChipSeries = "ZW050x", Alias = "Ctrl" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost() { PName = "COM202", SName = "COM102", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM203", SName = "COM103", ChipSeries = "ZW0713", JName = "j1" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM204", SName = "COM104", ChipSeries = "ZW0713", JName = "j2" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM205", SName = "COM105", ChipSeries = "ZW050x" }));


            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW070x, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(1)).Library);
        }

        // Tests with Init Pass, several device requested
        [Test]
        public void ZW050xs_2xLib_2x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", PName = "COM66", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x", Alias = "expected1" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", PName = "COM77", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM104", PName = "COM88", ChipSeries = "ZW050x", Alias = "expected2" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.Controller));
            requestDevices.Add(new Alias(2), new TestInterfaceDeviceMappingAttribute(2, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerStaticLib, dm.GetDevice(new Alias(1)).Library);
            NUnit.Framework.Assert.AreEqual("COM102", dm.GetDevice(new Alias(1)).DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(2)).Library);
            NUnit.Framework.Assert.AreEqual("COM77", dm.GetTIDevice(new Alias(2)).DataSource.SourceName);
        }

        [Test]
        public void Geckos_2xLib_2xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", PName = "COM201", ChipSeries = "ZW0713", JName = "12345678901" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM102", PName = "COM202", ChipSeries = "ZW0713", JName = "12345678902", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", PName = "COM203", ChipSeries = "ZW0713", JName = "12345678903" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM104", PName = "COM204", ChipSeries = "ZW0713", JName = "12345678904", Alias = "expected" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM105", PName = "COM205", ChipSeries = "ZW0713", JName = "12345678905" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.Controller));
            requestDevices.Add(new Alias(2), new TestInterfaceDeviceMappingAttribute(2, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult1 = dm.GetDevice(new Alias(1));
            var actualDeviceResult2 = dm.GetTIDevice(new Alias(2));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerStaticLib, actualDeviceResult1.Library);
            NUnit.Framework.Assert.AreEqual("COM101", actualDeviceResult1.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult2.Library);
            NUnit.Framework.Assert.AreEqual("COM203", actualDeviceResult2.DataSource.SourceName);
        }

        [Test]
        public void Mixed_2xLib_1x5s1xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "12345678901" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x", Alias = "asdasd" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", PName = "COM203", ChipSeries = "ZW0713", JName = "12345678903" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM104", PName = "COM204", ChipSeries = "ZW050x", Alias = "xcva" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM105", PName = "COM205", ChipSeries = "ZW050x" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.Controller));
            requestDevices.Add(new Alias(2), new TestInterfaceDeviceMappingAttribute(2, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult1 = dm.GetDevice(new Alias(1));
            var actualDeviceResult2 = dm.GetTIDevice(new Alias(2));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerStaticLib, actualDeviceResult1.Library);
            NUnit.Framework.Assert.AreEqual("COM102", actualDeviceResult1.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult2.Library);
            NUnit.Framework.Assert.AreEqual("COM203", actualDeviceResult2.DataSource.SourceName);
        }

        [Test]
        public void ZW050xs_1xAlias2xLib_Nx5s()
        {
            var expectedRequestCount = 2;

            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", PName = "COM66", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM102", Alias = "expected1", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", PName = "COM77", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM104", PName = "COM88", ChipSeries = "ZW050x", Alias = "expected2" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, "expected1", LibTypes.Controller));
            requestDevices.Add(new Alias(2), new TestInterfaceDeviceMappingAttribute(2, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult1 = dm.GetDevice(new Alias(1));
            var actualDeviceResult2 = dm.GetTIDevice(new Alias(2));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerStaticLib, actualDeviceResult1.Library);
            NUnit.Framework.Assert.AreEqual("COM102", actualDeviceResult1.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult2.Library);
        }

        [Test]
        public void Geckos_1xAlias2xLib_2x7s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "12345678901" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM102", ChipSeries = "ZW0713", JName = "12345678902", Alias = "expected1" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM103", PName = "COM203", ChipSeries = "ZW0713", JName = "12345678903" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM104", PName = "COM204", ChipSeries = "ZW0713", JName = "12345678904", Alias = "expected2" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.Controller));
            requestDevices.Add(new Alias(2), new TestInterfaceDeviceMappingAttribute(2, "expected2", LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult1 = dm.GetDevice(new Alias(1));
            var actualDeviceResult2 = dm.GetTIDevice(new Alias(2));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual("COM101", actualDeviceResult1.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerStaticLib, actualDeviceResult1.Library);
            NUnit.Framework.Assert.AreEqual("COM204", actualDeviceResult2.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult2.Library);
        }

        [Test]
        //[Ignore("Should take TI by default chip type = Gecko, but takes but alias order")]
        public void Mixed_1xAlias2xLib_2xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(
                Libraries.SwitchOnOff, new DeviceHost()
                { PName = "COM55", SName = "COM101", ChipSeries = "ZW0713", JName = "123456780" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(
                Libraries.ControllerStaticLib, new DeviceHost()
                { PName = "COM66", SName = "COM102", ChipSeries = "ZW0713", JName = "123456781" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(
                Libraries.SwitchOnOff, new DeviceHost()
                { PName = "COM77", SName = "COM103", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(
                Libraries.ControllerStaticLib, new DeviceHost()
                { SName = "COM104", ChipSeries = "ZW0713", JName = "123456782", Alias = "expected" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, "expected", LibTypes.Controller));
            requestDevices.Add(new Alias(2), new TestInterfaceDeviceMappingAttribute(2, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult1 = dm.GetDevice(new Alias(1));
            var actualDeviceResult2 = dm.GetTIDevice(new Alias(2));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerStaticLib, actualDeviceResult1.Library);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult1.DataSource.Alias);
            NUnit.Framework.Assert.AreEqual("COM104", actualDeviceResult1.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult2.Library);
        }

        [Test]
        //[Ignore("Should take TI by default chip type = Gecko, but takes but alias order")]
        public void Mixed_1xAlias2xLib_1x5s1xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(
                Libraries.SwitchOnOff, new DeviceHost()
                { PName = "COM55", SName = "COM101", ChipSeries = "ZW0713", JName = "123456780" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(
                Libraries.ControllerStaticLib, new DeviceHost()
                { PName = "COM66", SName = "COM102", ChipSeries = "ZW050x", Alias = "expected" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(
                Libraries.ControllerStaticLib, new DeviceHost()
                { PName = "COM77", SName = "COM103", ChipSeries = "ZW0713", JName = "123456781", Alias = "expected" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(
                Libraries.ControllerStaticLib, new DeviceHost()
                { PName = "COM88", SName = "COM104", ChipSeries = "ZW0713", JName = "123456782" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, "expected", LibTypes.Controller));
            requestDevices.Add(new Alias(2), new TestInterfaceDeviceMappingAttribute(2, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult1 = dm.GetDevice(new Alias(1));
            var actualDeviceResult2 = dm.GetTIDevice(new Alias(2));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerStaticLib, actualDeviceResult1.Library);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult1.DataSource.Alias);
            NUnit.Framework.Assert.AreEqual("COM103", actualDeviceResult1.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult2.Library);
        }

        [Test]
        //[Ignore("test fails due to chip series equal to 700s by default")]
        public void Mixed_8xLibAll_4xGk4x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM101", PName = "COM111", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM102", PName = "COM112", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM103", PName = "COM113", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM104", PName = "COM114", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM105", PName = "COM115", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM106", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM106", PName = "COM116", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM107", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM107", PName = "COM117", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM109", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM109", PName = "COM119", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM51", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM51", PName = "COM121", ChipSeries = "ZW0713", JName = "1324567801", Alias = "A" }));
            actualVersions.Add("COM52", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM52", PName = "COM122", ChipSeries = "ZW0713", JName = "1324567802", Alias = "B" }));
            actualVersions.Add("COM53", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM53", PName = "COM123", ChipSeries = "ZW0713", JName = "1324567803", Alias = "C" }));
            actualVersions.Add("COM54", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM54", PName = "COM124", ChipSeries = "ZW0713", JName = "1324567804", Alias = "D" }));
            actualVersions.Add("COM55", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM55", PName = "COM125", ChipSeries = "ZW0713", JName = "1324567805", Alias = "E" }));
            actualVersions.Add("COM56", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM56", PName = "COM126", ChipSeries = "ZW0713", JName = "1324567806", Alias = "F" }));
            actualVersions.Add("COM57", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM57", PName = "COM127", ChipSeries = "ZW0713", JName = "1324567807", Alias = "G" }));
            actualVersions.Add("COM59", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM59", PName = "COM129", ChipSeries = "ZW0713", JName = "1324567809", Alias = "L" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.ControllerBridgeLib));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, LibTypes.ControllerPortableLib));
            requestDevices.Add(new Alias(3), new DeviceMappingAttribute(3, LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(4), new TestInterfaceDeviceMappingAttribute(4, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(5), new DeviceMappingAttribute(5, ChipSeries.ZW050x, LibTypes.ControllerBridgeLib));
            requestDevices.Add(new Alias(6), new DeviceMappingAttribute(6, ChipSeries.ZW050x, LibTypes.ControllerPortableLib));
            requestDevices.Add(new Alias(7), new DeviceMappingAttribute(7, ChipSeries.ZW050x, LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(8), new TestInterfaceDeviceMappingAttribute(8, ChipSeries.ZW050x, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);

            NUnit.Framework.Assert.AreEqual(Libraries.ControllerBridgeLib, dm.GetDevice(new Alias(1)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerPortableLib, dm.GetDevice(new Alias(2)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerStaticLib, dm.GetDevice(new Alias(3)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(4)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerBridgeLib, dm.GetDevice(new Alias(5)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerPortableLib, dm.GetDevice(new Alias(6)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerStaticLib, dm.GetDevice(new Alias(7)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(8)).Library);

            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(2)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(3)).Version);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZGM130S_BRD4207A, dm.GetTIDevice(new Alias(4)).ChipType);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(5)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(6)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(7)).Version);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZW050x, dm.GetTIDevice(new Alias(8)).ChipType);
        }

        [Test]
        //[Ignore("TestInterfaceDevice Manager Init Devices only with ChipSeries.Default!")]
        public void Mixed_8xLibAll4xChip7And4xChip5_4xGk4x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM101", PName = "COM111", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM102", PName = "COM112", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM103", PName = "COM113", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM104", PName = "COM114", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM105", PName = "COM115", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM106", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM106", PName = "COM116", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM107", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM107", PName = "COM117", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM108", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM108", PName = "COM118", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM109", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM109", PName = "COM119", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM51", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM51", PName = "COM121", ChipSeries = "ZW0713", JName = "1324567801" }));
            actualVersions.Add("COM52", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM52", PName = "COM122", ChipSeries = "ZW0713", JName = "1324567802" }));
            actualVersions.Add("COM53", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM53", PName = "COM123", ChipSeries = "ZW0713", JName = "1324567803" }));
            actualVersions.Add("COM54", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM54", PName = "COM124", ChipSeries = "ZW0713", JName = "1324567804" }));
            actualVersions.Add("COM55", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM55", PName = "COM125", ChipSeries = "ZW0713", JName = "1324567805" }));
            actualVersions.Add("COM56", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM56", PName = "COM126", ChipSeries = "ZW0713", JName = "1324567806" }));
            actualVersions.Add("COM57", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM57", PName = "COM127", ChipSeries = "ZW0713", JName = "1324567807" }));
            actualVersions.Add("COM58", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM58", PName = "COM128", ChipSeries = "ZW0713", JName = "1324567808" }));
            actualVersions.Add("COM59", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM59", PName = "COM129", ChipSeries = "ZW0713", JName = "1324567809" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW070x, LibTypes.ControllerBridgeLib));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, ChipSeries.ZW070x, LibTypes.ControllerPortableLib));
            requestDevices.Add(new Alias(3), new DeviceMappingAttribute(3, ChipSeries.ZW070x, LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(4), new TestInterfaceDeviceMappingAttribute(4, ChipSeries.ZW070x, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(5), new DeviceMappingAttribute(5, ChipSeries.ZW050x, LibTypes.ControllerBridgeLib));
            requestDevices.Add(new Alias(6), new DeviceMappingAttribute(6, ChipSeries.ZW050x, LibTypes.ControllerPortableLib));
            requestDevices.Add(new Alias(7), new DeviceMappingAttribute(7, ChipSeries.ZW050x, LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(8), new TestInterfaceDeviceMappingAttribute(8, ChipSeries.ZW050x, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(2)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(3)).Version);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZGM130S_BRD4207A, dm.GetTIDevice(new Alias(4)).ChipType);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(5)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(6)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(7)).Version);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZW050x, dm.GetTIDevice(new Alias(8)).ChipType);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(4)).Library);
        }

        [Test]
        //[Ignore("TestInterfaceDevice Manager Init Devices only with ChipSeries.Default!")]
        public void Mixed_8xLibsAll4xChip5_4xGk4x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM101", PName = "COM111", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM102", PName = "COM112", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM103", PName = "COM113", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM104", PName = "COM114", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM105", PName = "COM115", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM106", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM106", PName = "COM116", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM107", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM107", PName = "COM117", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM108", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM108", PName = "COM118", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM109", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM109", PName = "COM119", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM51", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM51", PName = "COM121", ChipSeries = "ZW0713", JName = "1324567801" }));
            actualVersions.Add("COM52", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM52", PName = "COM122", ChipSeries = "ZW0713", JName = "1324567802" }));
            actualVersions.Add("COM53", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM53", PName = "COM123", ChipSeries = "ZW0713", JName = "1324567803" }));
            actualVersions.Add("COM54", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM54", PName = "COM124", ChipSeries = "ZW0713", JName = "1324567804" }));
            actualVersions.Add("COM55", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM55", PName = "COM125", ChipSeries = "ZW0713", JName = "1324567805" }));
            actualVersions.Add("COM56", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM56", PName = "COM126", ChipSeries = "ZW0713", JName = "1324567806" }));
            actualVersions.Add("COM57", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM57", PName = "COM127", ChipSeries = "ZW0713", JName = "1324567807" }));
            actualVersions.Add("COM58", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM58", PName = "COM128", ChipSeries = "ZW0713", JName = "1324567808" }));
            actualVersions.Add("COM59", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM59", PName = "COM129", ChipSeries = "ZW0713", JName = "1324567809" }));


            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM104", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM105", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM106", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM107", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM108", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM109", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM51", ChipSeries = "ZW0713", JName = "1324567801" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM52", ChipSeries = "ZW0713", JName = "1324567802" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM53", ChipSeries = "ZW0713", JName = "1324567803" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM54", ChipSeries = "ZW0713", JName = "1324567804" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM55", ChipSeries = "ZW0713", JName = "1324567805" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM56", ChipSeries = "ZW0713", JName = "1324567806" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM57", ChipSeries = "ZW0713", JName = "1324567807" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM58", ChipSeries = "ZW0713", JName = "1324567808" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM59", ChipSeries = "ZW0713", JName = "1324567809" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.ControllerBridgeLib));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, LibTypes.ControllerPortableLib));
            requestDevices.Add(new Alias(3), new DeviceMappingAttribute(3, LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(4), new TestInterfaceDeviceMappingAttribute(4, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(5), new DeviceMappingAttribute(5, ChipSeries.ZW050x, LibTypes.ControllerBridgeLib));
            requestDevices.Add(new Alias(6), new DeviceMappingAttribute(6, ChipSeries.ZW050x, LibTypes.ControllerPortableLib));
            requestDevices.Add(new Alias(7), new DeviceMappingAttribute(7, ChipSeries.ZW050x, LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(8), new TestInterfaceDeviceMappingAttribute(8, ChipSeries.ZW050x, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(2)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(3)).Version);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZGM130S_BRD4207A, dm.GetTIDevice(new Alias(4)).ChipType);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(5)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(6)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(7)).Version);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZW050x, dm.GetTIDevice(new Alias(8)).ChipType);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(4)).Library);
        }

        [Test]
        //[Ignore("test fails due to chip series equal to 700s by default")]
        ////[Ignore("TestInterfaceDevice Manager Init Devices only with ChipSeries.Default!")]
        public void Mixed_8xLibsAll4xChip7_4xGk4x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM101", PName = "COM111", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM102", PName = "COM112", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM103", PName = "COM113", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM104", PName = "COM114", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM105", PName = "COM115", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM106", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM106", PName = "COM116", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM107", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM107", PName = "COM117", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM108", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM108", PName = "COM118", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM109", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM109", PName = "COM119", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM51", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM51", PName = "COM121", ChipSeries = "ZW0713", JName = "1324567801" }));
            actualVersions.Add("COM52", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM52", PName = "COM122", ChipSeries = "ZW0713", JName = "1324567802" }));
            actualVersions.Add("COM53", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM53", PName = "COM123", ChipSeries = "ZW0713", JName = "1324567803" }));
            actualVersions.Add("COM54", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM54", PName = "COM124", ChipSeries = "ZW0713", JName = "1324567804" }));
            actualVersions.Add("COM55", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM55", PName = "COM125", ChipSeries = "ZW0713", JName = "1324567805" }));
            actualVersions.Add("COM56", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM56", PName = "COM126", ChipSeries = "ZW0713", JName = "1324567806" }));
            actualVersions.Add("COM57", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM57", PName = "COM127", ChipSeries = "ZW0713", JName = "1324567807" }));
            actualVersions.Add("COM58", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM58", PName = "COM128", ChipSeries = "ZW0713", JName = "1324567808" }));
            actualVersions.Add("COM59", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM59", PName = "COM129", ChipSeries = "ZW0713", JName = "1324567809" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW070x, LibTypes.ControllerBridgeLib));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, ChipSeries.ZW070x, LibTypes.ControllerPortableLib));
            requestDevices.Add(new Alias(3), new DeviceMappingAttribute(3, ChipSeries.ZW070x, LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(4), new TestInterfaceDeviceMappingAttribute(4, ChipSeries.ZW070x, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(5), new DeviceMappingAttribute(5, ChipSeries.ZW050x, LibTypes.ControllerBridgeLib));
            requestDevices.Add(new Alias(6), new DeviceMappingAttribute(6, ChipSeries.ZW050x, LibTypes.ControllerPortableLib));
            requestDevices.Add(new Alias(7), new DeviceMappingAttribute(7, ChipSeries.ZW050x, LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(8), new TestInterfaceDeviceMappingAttribute(8, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);

            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(2)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(3)).Version);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZGM130S_BRD4207A, dm.GetTIDevice(new Alias(4)).ChipType);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(5)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(6)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(7)).Version);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZGM130S_BRD4207A, dm.GetTIDevice(new Alias(8)).ChipType);

            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(4)).Library);
        }

        [Test]
        //[Ignore("Failes because doesn't takes 700s by default but order by alias -> takes any first by existing alias")]
        public void Mixed_8xLibAll4xAlias5_4xGk4x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM101", PName = "COM111", ChipSeries = "ZW050x", Alias = "A" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM102", PName = "COM112", ChipSeries = "ZW050x", Alias = "B" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM103", PName = "COM113", ChipSeries = "ZW050x", Alias = "C" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM104", PName = "COM114", ChipSeries = "ZW050x", Alias = "D" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM105", PName = "COM115", ChipSeries = "ZW050x", Alias = "E" }));
            actualVersions.Add("COM106", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM106", PName = "COM116", ChipSeries = "ZW050x", Alias = "F" }));
            actualVersions.Add("COM107", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM107", PName = "COM117", ChipSeries = "ZW050x", Alias = "G" }));
            actualVersions.Add("COM108", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM108", PName = "COM118", ChipSeries = "ZW050x", Alias = "K" }));
            actualVersions.Add("COM109", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM109", PName = "COM119", ChipSeries = "ZW050x", Alias = "L" }));
            actualVersions.Add("COM51", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM51", PName = "COM121", ChipSeries = "ZW0713", JName = "1324567801" }));
            actualVersions.Add("COM52", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM52", PName = "COM122", ChipSeries = "ZW0713", JName = "1324567802" }));
            actualVersions.Add("COM53", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM53", PName = "COM123", ChipSeries = "ZW0713", JName = "1324567803" }));
            actualVersions.Add("COM54", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM54", PName = "COM124", ChipSeries = "ZW0713", JName = "1324567804" }));
            actualVersions.Add("COM55", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM55", PName = "COM125", ChipSeries = "ZW0713", JName = "1324567805" }));
            actualVersions.Add("COM56", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM56", PName = "COM126", ChipSeries = "ZW0713", JName = "1324567806" }));
            actualVersions.Add("COM57", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM57", PName = "COM127", ChipSeries = "ZW0713", JName = "1324567807" }));
            actualVersions.Add("COM58", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM58", PName = "COM128", ChipSeries = "ZW0713", JName = "1324567808" }));
            actualVersions.Add("COM59", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM59", PName = "COM129", ChipSeries = "ZW0713", JName = "1324567809" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.ControllerBridgeLib));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, LibTypes.ControllerPortableLib));
            requestDevices.Add(new Alias(3), new DeviceMappingAttribute(3, LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(4), new TestInterfaceDeviceMappingAttribute(4, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(5), new DeviceMappingAttribute(5, ChipSeries.ZW050x, LibTypes.ControllerBridgeLib) { Alias = "A" });
            requestDevices.Add(new Alias(6), new DeviceMappingAttribute(6, ChipSeries.ZW050x, LibTypes.ControllerPortableLib) { Alias = "B" });
            requestDevices.Add(new Alias(7), new DeviceMappingAttribute(7, ChipSeries.ZW050x, LibTypes.ControllerStaticLib) { Alias = "C" });
            requestDevices.Add(new Alias(8), new TestInterfaceDeviceMappingAttribute(8, ChipSeries.ZW050x, LibTypes.SwitchOnOff) { Alias = "G" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(2)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(3)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(5)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(6)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(7)).Version);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(4)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(8)).Library);
            NUnit.Framework.Assert.AreEqual("G", dm.GetTIDevice(new Alias(8)).DataSource.Alias);
        }

        [Test]
        //[Ignore("test fails due to chip series equal to 700s by default")]
        public void Mixed_8xLibAll4xAlias7_4xGk4x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM101", PName = "COM111", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM102", PName = "COM112", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM103", PName = "COM113", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM104", PName = "COM114", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM105", PName = "COM115", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM106", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM106", PName = "COM116", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM107", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM107", PName = "COM117", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM108", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM108", PName = "COM118", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM109", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM109", PName = "COM119", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM51", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM51", PName = "COM121", ChipSeries = "ZW0713", JName = "1324567801", Alias = "A" }));
            actualVersions.Add("COM52", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM52", PName = "COM122", ChipSeries = "ZW0713", JName = "1324567802", Alias = "B" }));
            actualVersions.Add("COM53", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM53", PName = "COM123", ChipSeries = "ZW0713", JName = "1324567803", Alias = "C" }));
            actualVersions.Add("COM54", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM54", PName = "COM124", ChipSeries = "ZW0713", JName = "1324567804", Alias = "D" }));
            actualVersions.Add("COM55", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM55", PName = "COM125", ChipSeries = "ZW0713", JName = "1324567805", Alias = "E" }));
            actualVersions.Add("COM56", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM56", PName = "COM126", ChipSeries = "ZW0713", JName = "1324567806", Alias = "F" }));
            actualVersions.Add("COM57", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM57", PName = "COM127", ChipSeries = "ZW0713", JName = "1324567807", Alias = "G" }));
            actualVersions.Add("COM58", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM58", PName = "COM128", ChipSeries = "ZW0713", JName = "1324567808", Alias = "K" }));
            actualVersions.Add("COM59", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM59", PName = "COM129", ChipSeries = "ZW0713", JName = "1324567809", Alias = "L" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, "A", LibTypes.ControllerBridgeLib));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, "B", LibTypes.ControllerPortableLib));
            requestDevices.Add(new Alias(3), new DeviceMappingAttribute(3, "C", LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(4), new TestInterfaceDeviceMappingAttribute(4, "G", LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(5), new DeviceMappingAttribute(5, ChipSeries.ZW050x, LibTypes.ControllerBridgeLib));
            requestDevices.Add(new Alias(6), new DeviceMappingAttribute(6, ChipSeries.ZW050x, LibTypes.ControllerPortableLib));
            requestDevices.Add(new Alias(7), new DeviceMappingAttribute(7, ChipSeries.ZW050x, LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(8), new TestInterfaceDeviceMappingAttribute(8, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(2)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(3)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(5)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(6)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(7)).Version);
            var tis = dm.GetDevices();
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(4)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(8)).Library);
            NUnit.Framework.Assert.AreEqual("G", dm.GetTIDevice(new Alias(4)).DataSource.Alias);
        }

        [Test]
        //[Ignore("TestInterfaceDevice Manager Init Devices only with ChipSeries.Default!")]
        public void Mixed_6xLib3xChip5And3xChip7_3xGk3x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM101", PName = "COM111", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM102", PName = "COM112", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM103", PName = "COM113", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM104", PName = "COM114", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM105", PName = "COM115", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM106", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM106", PName = "COM116", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM107", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM107", PName = "COM117", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM108", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM108", PName = "COM118", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM109", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM109", PName = "COM119", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM51", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM51", PName = "COM121", ChipSeries = "ZW0713", JName = "1324567801" }));
            actualVersions.Add("COM52", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM52", PName = "COM122", ChipSeries = "ZW0713", JName = "1324567802" }));
            actualVersions.Add("COM53", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM53", PName = "COM123", ChipSeries = "ZW0713", JName = "1324567803" }));
            actualVersions.Add("COM54", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM54", PName = "COM124", ChipSeries = "ZW0713", JName = "1324567804" }));
            actualVersions.Add("COM55", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM55", PName = "COM125", ChipSeries = "ZW0713", JName = "1324567805" }));
            actualVersions.Add("COM56", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM56", PName = "COM126", ChipSeries = "ZW0713", JName = "1324567806" }));
            actualVersions.Add("COM57", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM57", PName = "COM127", ChipSeries = "ZW0713", JName = "1324567807" }));
            actualVersions.Add("COM58", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM58", PName = "COM128", ChipSeries = "ZW0713", JName = "1324567808" }));
            actualVersions.Add("COM59", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM59", PName = "COM129", ChipSeries = "ZW0713", JName = "1324567809" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.Controller));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, ChipSeries.ZW070x, LibTypes.Controller));
            requestDevices.Add(new Alias(3), new TestInterfaceDeviceMappingAttribute(3, ChipSeries.ZW050x, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(4), new TestInterfaceDeviceMappingAttribute(4, ChipSeries.ZW050x, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(5), new TestInterfaceDeviceMappingAttribute(5, ChipSeries.ZW070x, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(6), new TestInterfaceDeviceMappingAttribute(6, ChipSeries.ZW070x, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);

            NUnit.Framework.Assert.AreEqual(Libraries.ControllerBridgeLib, dm.GetDevice(new Alias(1)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerBridgeLib, dm.GetDevice(new Alias(2)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(3)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(4)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(5)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(6)).Library);

            NUnit.Framework.Assert.AreEqual(ChipTypes.ZW050x, dm.GetDevice(new Alias(1)).ChipType);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZW070x, dm.GetDevice(new Alias(2)).ChipType);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZW050x, dm.GetTIDevice(new Alias(3)).ChipType);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZW050x, dm.GetTIDevice(new Alias(4)).ChipType);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZGM130S_BRD4207A, dm.GetTIDevice(new Alias(5)).ChipType);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZGM130S_BRD4207A, dm.GetTIDevice(new Alias(6)).ChipType);
        }

        [Test]
        //[Ignore("TestInterfaceDevice Manager Init Devices only with ChipSeries.Default!")]
        public void Mixed_6xLib3xChip5_3xGk3x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM101", PName = "COM111", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM102", PName = "COM112", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM103", PName = "COM113", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM104", PName = "COM114", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM105", PName = "COM115", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM106", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM106", PName = "COM116", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM107", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM107", PName = "COM117", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM108", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM108", PName = "COM118", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM109", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM109", PName = "COM119", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM51", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM51", PName = "COM121", ChipSeries = "ZW0713", JName = "1324567801", Alias = "A" }));
            actualVersions.Add("COM52", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM52", PName = "COM122", ChipSeries = "ZW0713", JName = "1324567802", Alias = "B" }));
            actualVersions.Add("COM53", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM53", PName = "COM123", ChipSeries = "ZW0713", JName = "1324567803", Alias = "C" }));
            actualVersions.Add("COM54", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM54", PName = "COM124", ChipSeries = "ZW0713", JName = "1324567804", Alias = "D" }));
            actualVersions.Add("COM55", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM55", PName = "COM125", ChipSeries = "ZW0713", JName = "1324567805", Alias = "E" }));
            actualVersions.Add("COM56", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM56", PName = "COM126", ChipSeries = "ZW0713", JName = "1324567806", Alias = "F" }));
            actualVersions.Add("COM57", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM57", PName = "COM127", ChipSeries = "ZW0713", JName = "1324567807", Alias = "G" }));
            actualVersions.Add("COM58", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM58", PName = "COM128", ChipSeries = "ZW0713", JName = "1324567808", Alias = "K" }));
            actualVersions.Add("COM59", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM59", PName = "COM129", ChipSeries = "ZW0713", JName = "1324567809", Alias = "L" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.Controller));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, ChipSeries.ZW050x, LibTypes.Controller));
            requestDevices.Add(new Alias(3), new TestInterfaceDeviceMappingAttribute(3, ChipSeries.ZW050x, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(6), new TestInterfaceDeviceMappingAttribute(6, ChipSeries.ZW050x, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(4), new TestInterfaceDeviceMappingAttribute(4, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(5), new TestInterfaceDeviceMappingAttribute(5, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(2)).Version);
            var tis = dm.GetDevices();
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(3)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(4)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(5)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(6)).Library);
        }

        [Test]
        //[Ignore("TestInterfaceDevice Manager Init Devices only with ChipSeries.Default!")]
        public void Mixed_6xLib3xChip7_3x5s3xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM101", PName = "COM111", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM102", PName = "COM112", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM104", PName = "COM114", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM105", PName = "COM115", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM106", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM106", PName = "COM116", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM107", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM107", PName = "COM117", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM108", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM108", PName = "COM118", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM109", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM109", PName = "COM119", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM51", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM51", PName = "COM121", ChipSeries = "ZW0713", JName = "1324567801", Alias = "A" }));
            actualVersions.Add("COM53", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM53", PName = "COM123", ChipSeries = "ZW0713", JName = "1324567803", Alias = "C" }));
            actualVersions.Add("COM54", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM54", PName = "COM124", ChipSeries = "ZW0713", JName = "1324567804", Alias = "D" }));
            actualVersions.Add("COM55", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM55", PName = "COM125", ChipSeries = "ZW0713", JName = "1324567805", Alias = "E" }));
            actualVersions.Add("COM56", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM56", PName = "COM126", ChipSeries = "ZW0713", JName = "1324567806", Alias = "F" }));
            actualVersions.Add("COM58", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM58", PName = "COM128", ChipSeries = "ZW0713", JName = "1324567808", Alias = "K" }));
            actualVersions.Add("COM59", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM59", PName = "COM129", ChipSeries = "ZW0713", JName = "1324567809", Alias = "L" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.Controller));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, ChipSeries.ZW050x, LibTypes.Controller));
            requestDevices.Add(new Alias(3), new TestInterfaceDeviceMappingAttribute(3, ChipSeries.ZW050x, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(4), new DeviceMappingAttribute(4, LibTypes.Controller));
            requestDevices.Add(new Alias(5), new DeviceMappingAttribute(5, LibTypes.Controller));
            requestDevices.Add(new Alias(6), new TestInterfaceDeviceMappingAttribute(6, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);

            NUnit.Framework.Assert.AreEqual(Libraries.ControllerBridgeLib, dm.GetDevice(new Alias(1)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerStaticLib, dm.GetDevice(new Alias(2)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(3)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerBridgeLib, dm.GetDevice(new Alias(4)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerStaticLib, dm.GetDevice(new Alias(5)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(6)).Library);

            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(2)).Version);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZW050x, dm.GetTIDevice(new Alias(3)).ChipType);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(4)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(5)).Version);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZGM130S_BRD4207A, dm.GetTIDevice(new Alias(6)).ChipType);
        }

        [Test]
        public void Mixed_6xLib_3xGk3x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM101", PName = "COM111", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM102", PName = "COM112", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM103", PName = "COM113", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM104", PName = "COM114", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM105", PName = "COM115", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM106", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM106", PName = "COM116", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM107", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM107", PName = "COM117", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM108", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM108", PName = "COM118", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM109", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM109", PName = "COM119", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM51", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM51", PName = "COM121", ChipSeries = "ZW0713", JName = "1324567801", Alias = "A" }));
            actualVersions.Add("COM52", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM52", PName = "COM122", ChipSeries = "ZW0713", JName = "1324567802", Alias = "B" }));
            actualVersions.Add("COM53", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM53", PName = "COM123", ChipSeries = "ZW0713", JName = "1324567803", Alias = "C" }));
            actualVersions.Add("COM54", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM54", PName = "COM124", ChipSeries = "ZW0713", JName = "1324567804", Alias = "D" }));
            actualVersions.Add("COM55", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM55", PName = "COM125", ChipSeries = "ZW0713", JName = "1324567805", Alias = "E" }));
            actualVersions.Add("COM56", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM56", PName = "COM126", ChipSeries = "ZW0713", JName = "1324567806", Alias = "F" }));
            actualVersions.Add("COM57", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM57", PName = "COM127", ChipSeries = "ZW0713", JName = "1324567807", Alias = "G" }));
            actualVersions.Add("COM58", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM58", PName = "COM128", ChipSeries = "ZW0713", JName = "1324567808", Alias = "K" }));
            actualVersions.Add("COM59", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM59", PName = "COM129", ChipSeries = "ZW0713", JName = "1324567809", Alias = "L" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.Controller));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, LibTypes.Controller));
            requestDevices.Add(new Alias(3), new TestInterfaceDeviceMappingAttribute(3, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(4), new TestInterfaceDeviceMappingAttribute(4, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(5), new TestInterfaceDeviceMappingAttribute(5, ChipSeries.ZW050x, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(6), new TestInterfaceDeviceMappingAttribute(6, ChipSeries.ZW050x, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(2)).Version);
            var tis = dm.GetDevices();
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(3)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(4)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(5)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(6)).Library);
        }

        [Test]
        //[Ignore("TestInterfaceDevice Manager Init Devices only with ChipSeries.Default!")]
        public void Mixed_4xLib2xChip5And2xChip7_2xGk2x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM101", PName = "COM111", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM102", PName = "COM112", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM103", PName = "COM113", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM104", PName = "COM114", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM105", PName = "COM115", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM106", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM106", PName = "COM116", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM107", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM107", PName = "COM117", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM108", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM108", PName = "COM118", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM109", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM109", PName = "COM119", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM51", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM51", PName = "COM121", ChipSeries = "ZW0713", JName = "1324567801", Alias = "A" }));
            actualVersions.Add("COM52", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM52", PName = "COM122", ChipSeries = "ZW0713", JName = "1324567802", Alias = "B" }));
            actualVersions.Add("COM53", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM53", PName = "COM123", ChipSeries = "ZW0713", JName = "1324567803", Alias = "C" }));
            actualVersions.Add("COM54", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM54", PName = "COM124", ChipSeries = "ZW0713", JName = "1324567804", Alias = "D" }));
            actualVersions.Add("COM55", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM55", PName = "COM125", ChipSeries = "ZW0713", JName = "1324567805", Alias = "E" }));
            actualVersions.Add("COM56", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM56", PName = "COM126", ChipSeries = "ZW0713", JName = "1324567806", Alias = "F" }));
            actualVersions.Add("COM57", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM57", PName = "COM127", ChipSeries = "ZW0713", JName = "1324567807", Alias = "G" }));
            actualVersions.Add("COM58", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM58", PName = "COM128", ChipSeries = "ZW0713", JName = "1324567808", Alias = "K" }));
            actualVersions.Add("COM59", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM59", PName = "COM129", ChipSeries = "ZW0713", JName = "1324567809", Alias = "L" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, ChipSeries.ZW070x, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(2), new TestInterfaceDeviceMappingAttribute(2, ChipSeries.ZW070x, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(3), new TestInterfaceDeviceMappingAttribute(3, ChipSeries.ZW050x, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(4), new TestInterfaceDeviceMappingAttribute(4, ChipSeries.ZW050x, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);

            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(1)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(2)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(3)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(4)).Library);

            NUnit.Framework.Assert.AreEqual(ChipTypes.ZGM130S_BRD4207A, dm.GetTIDevice(new Alias(1)).ChipType);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZGM130S_BRD4207A, dm.GetTIDevice(new Alias(2)).ChipType);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZW050x, dm.GetTIDevice(new Alias(3)).ChipType);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZW050x, dm.GetTIDevice(new Alias(4)).ChipType);

            NUnit.Framework.Assert.AreEqual("COM127", dm.GetTIDevice(new Alias(1)).DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("COM128", dm.GetTIDevice(new Alias(2)).DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("COM117", dm.GetTIDevice(new Alias(3)).DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("COM118", dm.GetTIDevice(new Alias(4)).DataSource.SourceName);

        }

        [Test]
        //[Ignore("TestInterfaceDevice Manager Init Devices only with ChipSeries.Default!")]
        public void Mixed_4xLib2xChip5_2xGk2x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM101", PName = "COM111", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM102", PName = "COM112", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM103", PName = "COM113", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM104", PName = "COM114", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM105", PName = "COM115", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM106", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM106", PName = "COM116", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM107", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM107", PName = "COM117", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM108", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM108", PName = "COM118", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM109", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM109", PName = "COM119", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM51", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM51", PName = "COM121", ChipSeries = "ZW0713", JName = "1324567801", Alias = "A" }));
            actualVersions.Add("COM52", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM52", PName = "COM122", ChipSeries = "ZW0713", JName = "1324567802", Alias = "B" }));
            actualVersions.Add("COM53", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM53", PName = "COM123", ChipSeries = "ZW0713", JName = "1324567803", Alias = "C" }));
            actualVersions.Add("COM54", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM54", PName = "COM124", ChipSeries = "ZW0713", JName = "1324567804", Alias = "D" }));
            actualVersions.Add("COM55", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM55", PName = "COM125", ChipSeries = "ZW0713", JName = "1324567805", Alias = "E" }));
            actualVersions.Add("COM56", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM56", PName = "COM126", ChipSeries = "ZW0713", JName = "1324567806", Alias = "F" }));
            actualVersions.Add("COM57", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM57", PName = "COM127", ChipSeries = "ZW0713", JName = "1324567807", Alias = "G" }));
            actualVersions.Add("COM58", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM58", PName = "COM128", ChipSeries = "ZW0713", JName = "1324567808", Alias = "K" }));
            actualVersions.Add("COM59", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM59", PName = "COM129", ChipSeries = "ZW0713", JName = "1324567809", Alias = "L" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.Controller));
            requestDevices.Add(new Alias(2), new TestInterfaceDeviceMappingAttribute(2, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(3), new TestInterfaceDeviceMappingAttribute(3, ChipSeries.ZW050x, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(4), new TestInterfaceDeviceMappingAttribute(4, ChipSeries.ZW050x, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerBridgeLib, dm.GetDevice(new Alias(1)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(2)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(3)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(4)).Library);

            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZGM130S_BRD4207A, dm.GetTIDevice(new Alias(2)).ChipType);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZW050x, dm.GetTIDevice(new Alias(3)).ChipType);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZW050x, dm.GetTIDevice(new Alias(4)).ChipType);

            NUnit.Framework.Assert.AreEqual("COM117", dm.GetTIDevice(new Alias(3)).DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("COM118", dm.GetTIDevice(new Alias(4)).DataSource.SourceName);
        }

        [Test]
        //[Ignore("test fails due to chip series equal to 700s by default")]
        ////[Ignore("TestInterfaceDevice Manager Init Devices only with ChipSeries.Default!")]
        public void Mixed_4xLib2xChip7_2x5s2xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM101", PName = "COM111", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM102", PName = "COM112", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM103", PName = "COM113", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM104", PName = "COM114", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM105", PName = "COM115", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM106", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM106", PName = "COM116", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM107", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM107", PName = "COM117", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM108", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM108", PName = "COM118", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM109", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM109", PName = "COM119", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM51", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM51", PName = "COM121", ChipSeries = "ZW0713", JName = "1324567801", Alias = "A" }));
            actualVersions.Add("COM52", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM52", PName = "COM122", ChipSeries = "ZW0713", JName = "1324567802", Alias = "B" }));
            actualVersions.Add("COM53", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM53", PName = "COM123", ChipSeries = "ZW0713", JName = "1324567803", Alias = "C" }));
            actualVersions.Add("COM54", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM54", PName = "COM124", ChipSeries = "ZW0713", JName = "1324567804", Alias = "D" }));
            actualVersions.Add("COM55", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM55", PName = "COM125", ChipSeries = "ZW0713", JName = "1324567805", Alias = "E" }));
            actualVersions.Add("COM56", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM56", PName = "COM126", ChipSeries = "ZW0713", JName = "1324567806", Alias = "F" }));
            actualVersions.Add("COM57", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM57", PName = "COM127", ChipSeries = "ZW0713", JName = "1324567807", Alias = "G" }));
            actualVersions.Add("COM58", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM58", PName = "COM128", ChipSeries = "ZW0713", JName = "1324567808", Alias = "K" }));
            actualVersions.Add("COM59", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM59", PName = "COM129", ChipSeries = "ZW0713", JName = "1324567809", Alias = "L" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(2), new TestInterfaceDeviceMappingAttribute(2, ChipSeries.ZW050x, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(3), new TestInterfaceDeviceMappingAttribute(3, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(4), new TestInterfaceDeviceMappingAttribute(4, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);

            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(1)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(2)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(3)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(4)).Library);

            NUnit.Framework.Assert.AreEqual(ChipTypes.ZW050x, dm.GetTIDevice(new Alias(1)).ChipType);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZW050x, dm.GetTIDevice(new Alias(2)).ChipType);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZGM130S_BRD4207A, dm.GetTIDevice(new Alias(3)).ChipType);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZGM130S_BRD4207A, dm.GetTIDevice(new Alias(4)).ChipType);

            NUnit.Framework.Assert.AreEqual("COM117", dm.Devices[new Alias(1)].PClient.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("COM118", dm.Devices[new Alias(2)].PClient.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("COM127", dm.Devices[new Alias(3)].PClient.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("COM128", dm.Devices[new Alias(4)].PClient.DataSource.SourceName);
        }

        [Test]
        public void Mixed_4xLib_3xGk1x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM101", PName = "COM111", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM102", PName = "COM112", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost()
            { SName = "COM103", PName = "COM113", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM104", PName = "COM114", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM105", PName = "COM115", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM106", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM106", PName = "COM116", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM107", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM107", PName = "COM117", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM108", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM108", PName = "COM118", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM109", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM109", PName = "COM119", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM51", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM51", PName = "COM121", ChipSeries = "ZW0713", JName = "1324567801", Alias = "A" }));
            actualVersions.Add("COM52", new Tuple<Libraries, DeviceHost>(Libraries.ControllerPortableLib, new DeviceHost()
            { SName = "COM52", PName = "COM122", ChipSeries = "ZW0713", JName = "1324567802", Alias = "B" }));
            actualVersions.Add("COM53", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost()
            { SName = "COM53", PName = "COM123", ChipSeries = "ZW0713", JName = "1324567803", Alias = "C" }));
            actualVersions.Add("COM54", new Tuple<Libraries, DeviceHost>(Libraries.DoorLockKeyPad, new DeviceHost()
            { SName = "COM54", PName = "COM124", ChipSeries = "ZW0713", JName = "1324567804", Alias = "D" }));
            actualVersions.Add("COM55", new Tuple<Libraries, DeviceHost>(Libraries.PowerStrip, new DeviceHost()
            { SName = "COM55", PName = "COM125", ChipSeries = "ZW0713", JName = "1324567805", Alias = "E" }));
            actualVersions.Add("COM56", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost()
            { SName = "COM56", PName = "COM126", ChipSeries = "ZW0713", JName = "1324567806", Alias = "F" }));
            actualVersions.Add("COM57", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM57", PName = "COM127", ChipSeries = "ZW0713", JName = "Gk1324567807", Alias = "G" }));
            actualVersions.Add("COM58", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost()
            { SName = "COM58", PName = "COM128", ChipSeries = "ZW0713", JName = "Gk1324567808", Alias = "K" }));
            actualVersions.Add("COM59", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost()
            { SName = "COM59", PName = "COM129", ChipSeries = "ZW0713", JName = "1324567809", Alias = "L" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(2), new TestInterfaceDeviceMappingAttribute(2, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(3), new TestInterfaceDeviceMappingAttribute(3, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(4), new TestInterfaceDeviceMappingAttribute(4, ChipSeries.ZW050x, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerBridgeLib, dm.GetDevice(new Alias(1)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(2)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(3)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(4)).Library);

            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZGM130S_BRD4207A, dm.GetTIDevice(new Alias(2)).ChipType);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZGM130S_BRD4207A, dm.GetTIDevice(new Alias(3)).ChipType);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZW050x, dm.GetTIDevice(new Alias(4)).ChipType);
        }

        [Test]
        public void Mixed_2xAlias3xLib_1xGk2x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM55", SName = "COM101", ChipSeries = "ZW0713", JName = "123456780" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM66", SName = "COM102", ChipSeries = "ZW050x", Alias = "Stick" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM77", SName = "COM103", ChipSeries = "ZW050x", Alias = "Board" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM88", SName = "COM104", ChipSeries = "ZW0713", JName = "123456781" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, ChipSeries.ZW050x, LibTypes.Controller) { Alias = "Stick" });
            requestDevices.Add(new Alias(3), new TestInterfaceDeviceMappingAttribute(3, ChipSeries.ZW050x, LibTypes.SwitchOnOff) { Alias = "Board" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);
            var actualDeviceResult1 = dm.GetDevice(new Alias(1));
            var actualDeviceResult2 = dm.GetDevice(new Alias(2));
            var actualDeviceResult3 = dm.GetTIDevice(new Alias(3));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, actualDeviceResult1.Version);
            NUnit.Framework.Assert.AreEqual(Five, actualDeviceResult2.Version);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerStaticLib, actualDeviceResult1.Library);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerStaticLib, actualDeviceResult2.Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult3.Library);
            NUnit.Framework.Assert.AreEqual("Board", actualDeviceResult3.DataSource.Alias);
        }

        [Test]
        public void Mixed_2xAliasAny3xLib_1x5s2xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost() { SName = "COM101", PName = "COM201", ChipSeries = "ZW0713", JName = "98765432", Alias = "Gecko" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost() { SName = "COM102", PName = "COM202", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { SName = "COM103", Alias = "Stick", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM104", PName = "COM204", ChipSeries = "ZW0713", JName = "34534" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM105", PName = "COM205", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM106", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { SName = "COM106", PName = "COM206", ChipSeries = "ZW050x", Alias = "TI2" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, "Gecko", LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(2), new TestInterfaceDeviceMappingAttribute(2, ChipSeries.ZW050x, LibTypes.SwitchOnOff) { Alias = "Any" });
            requestDevices.Add(new Alias(3), new TestInterfaceDeviceMappingAttribute(3, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerBridgeLib, dm.GetDevice(new Alias(1)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(2)).Library);
            NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, dm.GetTIDevice(new Alias(3)).Library);
            NUnit.Framework.Assert.AreEqual("COM101", dm.GetDevice(new Alias(1)).DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("COM205", dm.GetTIDevice(new Alias(2)).DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("COM204", dm.GetTIDevice(new Alias(3)).DataSource.SourceName);

        }

        [Test]
        public void Empty_Any_InitFailed()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new TestInterfaceDeviceMappingAttribute(1, LibTypes.SwitchOnOff));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
            NUnit.Framework.Assert.IsEmpty(dm.Devices);
        }

        [Test]
        public void Mixed_EMPTY_InitTrue()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost() { PName = "COM201", SName = "COM101", ChipSeries = "ZW050x", Alias = "Ctrl" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost() { PName = "COM202", SName = "COM102", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM203", SName = "COM103", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM204", SName = "COM104", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM205", SName = "COM105", ChipSeries = "ZW050x" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsEmpty(dm.Devices);
            //NUnit.Framework.Assert.AreEqual(Libraries.SwitchOnOff, actualDeviceResult.Library);
        }

        [Test]
        ////[Ignore("test fails due to doesn't init device by alias if not exist alias in the profile")]
        public void Mixed_2xAlias4xLib2xAll_4xGk4x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, DeviceHost>>();
            actualVersions.Add("COM101", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost() { PName = "COM201", SName = "COM101", ChipSeries = "ZW050x", Alias = "Ctrl" }));
            actualVersions.Add("COM102", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost() { PName = "COM202", SName = "COM102", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM103", new Tuple<Libraries, DeviceHost>(Libraries.ControllerStaticLib, new DeviceHost() { PName = "COM203", SName = "COM103", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM104", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM204", SName = "COM104", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM105", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM205", SName = "COM105", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM106", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost() { PName = "COM206", SName = "COM106", ChipSeries = "ZW050x" }));
            actualVersions.Add("COM107", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM207", SName = "COM107", ChipSeries = "ZW050x", Alias = "TI3" }));
            actualVersions.Add("COM108", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM208", SName = "COM108", ChipSeries = "ZW050x", Alias = "TI2" }));
            actualVersions.Add("COM109", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost() { PName = "COM209", SName = "COM109", ChipSeries = "ZW050x", Alias = "TI1" }));
            //geckos
            actualVersions.Add("COM51", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost() { PName = "COM210", SName = "COM51", ChipSeries = "ZW0713", JName = "1324567801", Alias = "Ctrl" }));
            actualVersions.Add("COM52", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost() { PName = "COM212", SName = "COM52", ChipSeries = "ZW0713", JName = "1324567802" }));
            actualVersions.Add("COM53", new Tuple<Libraries, DeviceHost>(Libraries.ControllerBridgeLib, new DeviceHost() { PName = "COM213", SName = "COM53", ChipSeries = "ZW0713", JName = "1324567803" }));
            actualVersions.Add("COM54", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM214", SName = "COM54", ChipSeries = "ZW0713", JName = "1324567804" }));
            actualVersions.Add("COM55", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM215", SName = "COM55", ChipSeries = "ZW0713", JName = "1324567805" }));
            actualVersions.Add("COM56", new Tuple<Libraries, DeviceHost>(Libraries.SensorPIR, new DeviceHost() { PName = "COM216", SName = "COM56", ChipSeries = "ZW0713", JName = "1324567806" }));
            actualVersions.Add("COM57", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM217", SName = "COM57", ChipSeries = "ZW0713", JName = "1324567807", Alias = "TI3" }));
            actualVersions.Add("COM58", new Tuple<Libraries, DeviceHost>(Libraries.SwitchOnOff, new DeviceHost() { PName = "COM218", SName = "COM58", ChipSeries = "ZW0713", JName = "1324567808", Alias = "TI2" }));
            actualVersions.Add("COM59", new Tuple<Libraries, DeviceHost>(Libraries.WallController, new DeviceHost() { PName = "COM219", SName = "COM59", ChipSeries = "ZW0713", JName = "1324567809", Alias = "TI1" }));

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.Controller));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, ChipSeries.ZW050x, LibTypes.Controller));
            requestDevices.Add(new Alias(3), new TestInterfaceDeviceMappingAttribute(3, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(4), new TestInterfaceDeviceMappingAttribute(4, ChipSeries.ZW050x, LibTypes.SwitchOnOff));
            requestDevices.Add(new Alias(5), new TestInterfaceDeviceMappingAttribute(5, "TI1"));
            requestDevices.Add(new Alias(6), new TestInterfaceDeviceMappingAttribute(6, "TI2"));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(actualVersions, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(2)).Version);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZGM130S_BRD4207A, dm.GetTIDevice(new Alias(3)).ChipType);
            NUnit.Framework.Assert.AreEqual(ChipTypes.ZW050x, dm.GetTIDevice(new Alias(4)).ChipType);
        }
    }
}
