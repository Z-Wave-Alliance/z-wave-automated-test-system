// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections;
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
    [TestFixture]
    class ZatsEnvironmentTests
    {
        static ISessionLayer _sessionLayer;
        /*
         * requestDevices - Setup requested device - main test goal
         * Test are cover only DeviceManager any other - no
         * (DB) - different behavior?
         */

        const string Five = "500s";
        const string Seven = "Z-Wave 7";
        const string OldVer = "OldVersion";

        private static bool RunSetupReadMode(Profile profile, SortedList<Alias, DeviceMappingAttribute> requestDevices, ZatsEnvironment zatsEnvironment, bool isZnifferRequired = false)
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
            testCaseDefinition.IsZnifferRequired = isZnifferRequired;
            testCaseDefinition.DeviceGroups = new SortedList<Alias, MultiDeviceMappingAttribute>();

            var consoleOutPut = new ConsoleCustomOutput(false, false, false);

            var actualSetupResult = zatsEnvironment.SetUp(profile, testCaseDefinition, cmdLineSettings, zatsSettings, consoleOutPut, frequency, LongRangeChannels.Undefined, 1, 1);
            return actualSetupResult;
        }

        private const int MAX_SESSIONS = 0x7FFF;
        private static BitArray SessionIds = new BitArray(MAX_SESSIONS);

        public static byte NextSessionId()
        {
            //lock (ZatsEnvironmentTests.)
            {
                byte sessionId = 1;
                for (int i = sessionId; i < SessionIds.Length; i++)
                {
                    if (SessionIds[i] == false)
                    {
                        SessionIds[i] = true;
                        break;
                    }
                    sessionId++;
                }
                $"SL registered {sessionId}"._DLOG();
                return sessionId;
            }
        }

        public static void ReleaseSessionId(ushort sessionId)
        {
            //lock (this)
            {
                $"SL unregistered {sessionId}"._DLOG();
                SessionIds[sessionId] = false;
            }
        }

        public static void ResetSessionIdCounter()
        {
            SessionIds = new BitArray(MAX_SESSIONS);
        }

        private static ZatsEnvironment CreateZatsEnvironment(Dictionary<string, Tuple<Libraries, string>> actualVersions, SortedList<Alias, DeviceMappingAttribute> requestDevices, out DeviceManager dm)
        {
            List<string> usedSrcs = new List<string>();
            var alphaProgrammerMock = new Mock<IProgrammingProvider>();
            var gammaProgrammerMock = new Mock<IProgrammingProvider>();
            var fileSystemManagerMock = new Mock<IFileSystemManager>();
            var sessionLayer = new Mock<ISessionLayer>();
            sessionLayer.Setup(x => x.NextSessionId()).Returns(() =>
            {
                return NextSessionId();
            });
            sessionLayer.Setup(x => x.ReleaseSessionId(It.IsAny<ushort>())).Callback((ushort x) =>
            {
                ReleaseSessionId(x);
                //var tmp = available.Where(y => y.SerialSessionId == x);
                //foreach (var t in tmp)
                //{
                //    t.SetSerialSessionId(0);
                //}
                //tmp = available.Where(y => y.ProgSessionId == x);
                //foreach (var t in tmp)
                //{
                //    t.SetProgSessionId(0);
                //}
            });
            sessionLayer.Setup(x => x.ResetSessionIdCounter()).Callback(() =>
            {
                ResetSessionIdCounter();
                //foreach (var t in available)
                //{
                //    t.SetSerialSessionId(0);
                //    t.SetProgSessionId(0);
                //}
            });
            sessionLayer.Setup(x => x.CreateClient(It.IsAny<ushort>()))
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
                        if (!act.Item1.IsSampleApplication())
                        {
                            ((SerialApiGetInitDataOperation)a).SpecificResult.ChipType = act.Item2 == Seven ? ChipTypes.ZW070x : ChipTypes.ZW050x;
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
                        if (!act.Item1.IsSampleApplication())
                        {
                            ((VersionOperation)a).SpecificResult.Library = act.Item1;
                            ((VersionOperation)a).SpecificResult.Version = act.Item2;
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
                            if (!act.Item1.IsSampleApplication())
                            {
                                ((RequestOperation)a).SpecificResult.Payload = GetVersion(act.Item2)
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

            var frameLayer = Mock.Of<IFrameLayer>((x => x.CreateClient(It.IsAny<ushort>()) == Mock.Of<IFrameClient>()));

            var transportLayerMock = new Mock<ITransportLayer>();
            transportLayerMock.Setup(x => x.CreateClient(It.IsAny<ushort>()))
                .Returns(() =>
                {
                    var dsMock = new Mock<IDataSource>();
                    var ret = new Mock<ITransportClient>();
                    dsMock.SetupAllProperties();
                    ret.SetupProperty(y => y.DataSource, dsMock.Object);
                    ret.Setup(y => y.Connect(It.Is<IDataSource>(ab=>!ab.SourceName.Contains(_BusyPortName)))).Returns(CommunicationStatuses.Done).
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

            _sessionLayer = sessionLayer.Object;
            var basicApp = new ZWave.BasicApplication.BasicApplicationLayer(
                       _sessionLayer,
                       frameLayer,
                       transportLayerMock.Object);

            Profile profile = new Profile() { IsAddS2Keys = false };
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();

            dm = new DeviceManager(false, alphaProgrammerMock.Object, gammaProgrammerMock.Object, fileSystemManagerMock.Object, null);
            dm.IsSkipCheckPortExists = true;
            dm.BasicApplicationLayer = basicApp;
            dm.ZatsCache = new ZatsCache();
            dm.ZatsCache.ProgrammingResults = new Collection<ProgrammingResult>();
            dm.CurrentProfile = profile;

            var context = new ProfileContext(dm);

            var zm = new ZnifferManager(new ZWave.Xml.FrameHeader.FrameDefinition());
            zm.ZnifferApplicationLayer = new ZWave.ZnifferApplication.ZnifferApplicationLayer(_sessionLayer, frameLayer, transportLayerMock.Object);

            var managersContainerMock = new Mock<IManagersContainer>();
            managersContainerMock.Setup(x => x.Context).Returns(context);
            managersContainerMock.Setup(x => x.DeviceManager).Returns(dm);
            managersContainerMock.Setup(x => x.ZnifferManager).Returns(zm);
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
        //[Ignore("Fails. Should take any device by lib if no matched alias")]
        public void Mixed_2xAlias3xLib_2x5s1xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM1", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM1", PName = "COM11", ChipSeries = "ZW0713", JName = "440123001" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", PName = "COM121", ChipSeries = "ZW050x", Alias = "A" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", PName = "COM122", ChipSeries = "ZW050x", Alias = "B" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", PName = "COM123", ChipSeries = "ZW050x", Alias = "C" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.Controller) { Alias = "A" });
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, ChipSeries.ZW050x, LibTypes.Controller) { Alias = "B" });
            requestDevices.Add(new Alias(3), new DeviceMappingAttribute(3, LibTypes.EndDeviceAny));
            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult1 = dm.GetDevice(new Alias(1));
            var actualDeviceResult2 = dm.GetDevice(new Alias(2));
            var actualDeviceResult3 = dm.GetDevice(new Alias(3));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult1);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult2);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult3);
            NUnit.Framework.Assert.AreEqual(Five, actualDeviceResult1.Version);
            NUnit.Framework.Assert.AreEqual(Five, actualDeviceResult2.Version);
            NUnit.Framework.Assert.AreEqual(Seven, actualDeviceResult3.Version);
        }

        [Test]
        public void ZW050xs_Lib_1x5s()
        {
            var expected = "COM101";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(expected, actualDeviceResult.DataSource.SourceName);
        }

        [Test]
        public void Geckos_Lib_1xGk()
        {
            var expected = "COM101";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW0713", JName = "123456700" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW0713", JName = "123456701" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(expected, actualDeviceResult.DataSource.SourceName);
        }

        [Test]
        public void Mixed_Lib_1xGk()
        {
            var expected = "COM103";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW0713", JName = "123456789" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(expected, actualDeviceResult.DataSource.SourceName);
        }

        [Test] // if no one by lib
        public void ZW050xs_1AliasLib_1x5s()
        {
            var expected = "COM102";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, "500s"));
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, "500s"));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, "500s"));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW050x", Alias = "expected" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x", Alias = "expected" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, "expected", LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(expected, actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
        }

        [Test] // if no one by lib
        public void Geckos_AliasLib_1xGk()
        {
            var expected = "COM102";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM100", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM100", ChipSeries = "ZW0713", JName = "12345678901" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "12345678902", Alias = "other" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW0713", JName = "12345678903", Alias = "expected" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW0713", JName = "12345678904", Alias = "expected" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, "expected", LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(expected, actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
        }

        [Test]
        public void Mixed_AliasLib_1x5s()
        {
            var expected = "COM102";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "123456789" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW050x", Alias = "expected" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.Controller) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(expected, actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
        }

        [Test]
        public void Mixed_AliasLib_1xGk()
        {
            var expected = "COM102";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW0713", JName = "123456789", Alias = "expected" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM104", ChipSeries = "ZW0713", JName = "123456780" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, "expected", LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(expected, actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
        }

        [Test]
        public void ZW050xs_Alias_1x5s()
        {
            var expected = "COM103";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, "500s"));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, "500s"));
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.EndDeviceLib, "500s"));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW050x", Alias = "expected" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, "expected", LibTypes.EndDeviceLib));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult.DataSource);
            NUnit.Framework.Assert.AreEqual(expected, actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
        }

        [Test]
        //[Ignore("Fails. If not requeired LibType DeviceManger init performs all libs in a requiered list - review requirement")]
        public void Geckos_Alias_1xGk()
        {
            var expected = "COM103";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "00110011001" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW0713", JName = "00110011002" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW0713", JName = "00110011003", Alias = "expected" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, "expected", LibTypes.EndDeviceLib));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult.DataSource);
            NUnit.Framework.Assert.AreEqual(expected, actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
        }

        [Test]
        public void Mixed_Alias_1x5s()
        {
            var expected = "COM103";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "123456789" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW050x", Alias = "expected" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.EndDeviceLib) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult.DataSource);
            NUnit.Framework.Assert.AreEqual(expected, actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
        }

        [Test]
        //[Ignore("Fails. If not requeired LibType DeviceManger init performs all libs in a requiered list - review requirement")]
        public void Mixed_Alias_1xGk()
        {
            var expected = "COM103";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM22", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "123456789" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW0713", JName = "123456780", Alias = "expected" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM22", ChipSeries = "ZW050x" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, "expected", LibTypes.EndDeviceLib));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult.DataSource);
            NUnit.Framework.Assert.AreEqual(expected, actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
        }

        [Test]
        public void ZW050xs_AliasLibChip5_1x5s()
        {
            var expected = "COM102";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW050x", Alias = "expected" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.Controller) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(expected, actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
        }

        [Test]
        public void Geckos_AliasLibChip7_1xGk()
        {
            var expected = "COM102";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "1234567890" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW0713", JName = "1234567891", Alias = "expected" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW0713", JName = "1234567892" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW070x, LibTypes.Controller) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(expected, actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
        }

        [Test]
        public void ZW050xs_AliasLibChip7_NEFbyLib()
        {
            var expected = "COM102";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW050x", Alias = "expected" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW070x, LibTypes.EndDeviceAny) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test]
        public void Geckos_AliasLibChip5_1xGkByAlias()
        {
            var expected = "COM102";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "1234567890" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW0713", JName = "1234567891", Alias = "expected" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW0713", JName = "1234567892" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.Controller) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(expected, dm.GetDevice(new Alias(1)).DataSource.SourceName);
        }

        [Test]
        public void Mixed_AliasLibChip5_1x5s()
        {
            var expected = "COM102";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "132456789" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW050x", Alias = "expected" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.Controller) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(expected, actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
        }

        [Test]
        public void Mixed_AliasLibChip7_1xGk()
        {
            var expected = "COM102";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "132456789" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW0713", JName = "132456780", Alias = "expected" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM104", ChipSeries = "ZW050x" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW070x, LibTypes.Controller) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(expected, actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
        }

        //TODO: differnt oreder requested devices and profile

        [Test(Description = "Skip Chip - searched by alias")]
        public void MixedAliasOrderLast_AliasLibChip7__1x5s()
        {
            var expected = "COM104";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM105", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "132456789" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW0713", JName = "132456780" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x", Alias = "not_expected", IsUZB = true });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW050x", Alias = "expected" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM105", ChipSeries = "ZW050x" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.Controller) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(expected, actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("expected", actualDeviceResult.DataSource.Alias);
        }

        [Test(Description = "Skip Chip - searched by alias")]
        //[Ignore("For Build")]
        public void MixedAliasOrderFirst_AliasLibChip7_1x5s()
        {
            var expected = "COM103";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM105", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "132456789" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW0713", JName = "132456780" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW050x", Alias = "not_expected", IsUZB = true });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM104", ChipSeries = "ZW050x", Alias = "expected" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM105", ChipSeries = "ZW050x" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.Controller) { Alias = "not_expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(expected, actualDeviceResult.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual("not_expected", actualDeviceResult.DataSource.Alias);
        }
        [Test]
        public void ZW050xs_LibChip5_1x5s()
        {
            var expected = "COM102";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(expected, actualDeviceResult.DataSource.SourceName);
            //NUnit.Framework.Assert.AreEqual(ZWave.Enums.ChipTypes.ZW050x, actualDeviceResult.ChipType);
            NUnit.Framework.Assert.IsNull(actualDeviceResult.DataSource.Alias);
        }

        [Test]
        public void Geckos_LibChip7_1xGk()
        {
            var expected = "COM102";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW0713", JName = "1234567890" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "1234567891" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW0713", JName = "1234567892" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW070x, LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(expected, actualDeviceResult.DataSource.SourceName);
            //NUnit.Framework.Assert.AreEqual(ZWave.Enums.ChipTypes.ZW050x, actualDeviceResult.ChipType);
            NUnit.Framework.Assert.IsNull(actualDeviceResult.DataSource.Alias);
        }

        [Test]
        public void Mixed_LibChip5_1x5s()
        {
            var expected = "COM102";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "132456789" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(expected, actualDeviceResult.DataSource.SourceName);
        }

        [Test]
        public void Mixed_LibChip5_1x7s()
        {
            var expected = "COM102";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "132456789" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.EndDeviceAny));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(actualDeviceResult);
            NUnit.Framework.Assert.AreEqual(expected, actualDeviceResult.DataSource.SourceName);
        }

        [Test(Description = "Fails because skips chip version")]
        public void Mixed_LibChip7_NEF1()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM104", ChipSeries = "ZW0713", JName = "132456789" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW070x, LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        // Tests with Init fail expected, one device requested
        //TODO: "Not enough devices" Request more than exists in profile

        [Test]
        public void ZW050xs_Lib_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.SwitchOnOff, "500s"));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, "500s"));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test]
        public void Geckos_Lib_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.SwitchOnOff, Seven));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "123456700" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW0713", JName = "123456701" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test]
        public void Mixed_Lib_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW0713", JName = "123456789" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.EndDeviceAny));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test]
        public void ZW050xs_AliasLib_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, "500s"));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, "500s"));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, "500s"));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x", Alias = "expected" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, "expected", LibTypes.EndDeviceAny));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test]
        public void Geckos_AliasLib_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM100", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM100", ChipSeries = "ZW0713", JName = "3214567890" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "1324567890", Alias = "other" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW0713", JName = "1324567001", Alias = "expected" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW0713", JName = "3214567891" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, "expected", LibTypes.EndDeviceAny));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test]
        //[Ignore("Fails. Should take any device by lib if no matched alias")]
        public void Geckos_AnyAliasLib_1xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM100", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM100", ChipSeries = "ZW0713", JName = "3214567890" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "1324567890", Alias = "other" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW0713", JName = "1324567001", Alias = "expected" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW0713", JName = "3214567891" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM104", ChipSeries = "ZW0713", JName = "3214567891" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, "any", LibTypes.EndDeviceAny));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
        }

        [Test]
        public void Mixed_AliasLib_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "123456789" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x", Alias = "expected" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, "expected", LibTypes.EndDeviceAny));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test(Description = "Check on real setup (DB), maybe would be a good idea to disable map device only by Alias")]
        public void ZW050xs_AliasAny_1x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, "500s"));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, "500s"));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, "500s"));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x", Alias = "expected_NOT" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, "expected", LibTypes.ControllerStaticLib));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerStaticLib, dm.GetDevice(new Alias(1)).Library);
        }

        [Test(Description = "not good require basic device by alias without lib")]
        public void Geckos_AliasAny_1xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "00110011001" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW0713", JName = "00110011002" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW0713", JName = "00110011003", Alias = "expected_not" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, "expected", LibTypes.ControllerStaticLib));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.ControllerStaticLib, dm.GetDevice(new Alias(1)).Library);
        }

        [Test(Description = "not good require basic device by alias without lib")]
        public void Mixed_AliasAny_1x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "123456789" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x", Alias = "expected" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, "any", LibTypes.EndDeviceLib));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.AreEqual(Libraries.EndDeviceLib, dm.GetDevice(new Alias(1)).Library);
        }

        [Test(Description = "Test can run on any compactable devices")]
        public void Mixed_RunOnAnyChips()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, OldVer));
            actualVersions.Add("COM105", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "123456789" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x", Alias = "expected" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM104", ChipSeries = "ZW030x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM105", ChipSeries = "ZW0713" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.Any, LibTypes.ControllerBridgeLib));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, ChipSeries.Any, LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(3), new DeviceMappingAttribute(3, ChipSeries.ZW030x, LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(4), new DeviceMappingAttribute(4, ChipSeries.ZW070x, LibTypes.EndDeviceLib));
            requestDevices.Add(new Alias(5), new DeviceMappingAttribute(5, ChipSeries.ZW050x, LibTypes.EndDeviceLib));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(requestDevices.Count, dm.Devices.Count());
        }

        [Test(Description = "Check on real setup (DB), maybe would be a good idea to disable map device only by Alias")]
        public void ZW050xs_AliasLibChip5_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x", Alias = "expected" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.EndDeviceAny) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test(Description = "Check on real setup (DB), maybe would be a good idea to disable map device only by Alias")]

        public void Geckos_AliasLibChip5_NEFbyLib()
        {
            var expected = "COM102";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "1234567890" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW0713", JName = "1234567891", Alias = "expected" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW0713", JName = "1234567892" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.EndDeviceAny) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test(Description = "Check on real setup (DB), maybe would be a good idea to disable map device only by Alias")]

        public void Mixed_AliasLibChip5_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "132456789" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x", Alias = "expected" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.EndDeviceAny) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test(Description = "Check on real setup (DB), maybe would be a good idea to disable map device only by Alias")]

        public void Mixed_AliasLibChip7_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "132456789" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x", Alias = "expected" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW070x, LibTypes.EndDeviceAny) { Alias = "expected" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test]
        public void ZW050xs_LibChip5_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.ControllerPortableLib));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test]
        public void Geckos_LibChip7_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW0713", JName = "1234567890" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "1234567891" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW0713", JName = "1234567892" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW070x, LibTypes.DoorLockKeyPad));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test(Description = "skips chip ")]
        public void Mixed_LibChip5_NEF()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "132456789" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW0713", JName = "132456788" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.EndDeviceAny));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var found = dm.GetDevice(new Alias(1));
            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test]
        public void Mixed_LibChip7_NEF2()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "132456789" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW0713", JName = "132456788" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.EndDeviceAny));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        // Tests with Init Pass, several device requested
        [Test]
        public void ZW050xs_Lib_Nx5s()
        {
            var expectedRequestCount = 2;

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, "500s"));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, "500s"));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.Controller));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, LibTypes.EndDeviceAny));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(expectedRequestCount, dm.Devices.Count());
        }

        [Test]
        public void Geckos_Lib_NxGk()
        {
            var expectedRequestCount = 2;

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "123456700" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW0713", JName = "123456701" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.Controller));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, LibTypes.EndDeviceAny));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(expectedRequestCount, dm.Devices.Count());
        }

        [Test]
        public void Mixed_Lib_1x5s1xGk()
        {
            var expectedRequestCount = 2;

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW0713", JName = "123456789" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.Controller));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(expectedRequestCount, dm.Devices.Count());
        }

        [Test]
        public void ZW050xs_1xAlias2xLib_Nx5s()
        {
            var expectedRequestCount = 2;
            var expectedSrc = "COM102";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, "500s"));
            actualVersions.Add(expectedSrc, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, "500s"));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, "500s"));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.EndDeviceLib, "500s"));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expectedSrc, ChipSeries = "ZW050x", Alias = "expected1" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM104", ChipSeries = "ZW050x", Alias = "expected2" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, "expected1", LibTypes.Controller));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, LibTypes.EndDeviceAny));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult1 = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(expectedRequestCount, dm.Devices.Count());
            NUnit.Framework.Assert.AreEqual(expectedSrc, actualDeviceResult1.DataSource.SourceName);
        }

        [Test]
        public void Geckos_1xAlias2xLib_2x7s()
        {
            var expectedRequestCount = 2;
            var expectedSrc1 = "COM102";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add(expectedSrc1, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZGM130S_BRD4207A", JName = "12345678901" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expectedSrc1, ChipSeries = "ZGM130S_BRD4207A", JName = "12345678902", Alias = "expected1" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZGM130S_BRD4207A", JName = "12345678903" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM104", ChipSeries = "ZGM130S_BRD4207A", JName = "12345678904", Alias = "expected2" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, "expected1", LibTypes.Controller));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, LibTypes.EndDeviceAny));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult1 = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(expectedRequestCount, dm.Devices.Count());
            NUnit.Framework.Assert.AreEqual(expectedSrc1, actualDeviceResult1.DataSource.SourceName);
        }

        [TestCase(ChipSeries.ZW030x)]
        [TestCase(ChipSeries.ZW050x)]
        [TestCase(ChipSeries.ZG14_BRD4201A)]
        [TestCase(ChipSeries.RZ13_BRD4201B)]
        [TestCase(ChipSeries.ZG13L_BRD4201C)]
        [TestCase(ChipSeries.ZG13S_BRD4201D)]
        [TestCase(ChipSeries.ZGM130S_BRD4202A)]
        [TestCase(ChipSeries.RZ13_BRD4203A)]
        [TestCase(ChipSeries.ZG23_BRD4204A)]
        [TestCase(ChipSeries.ZG23_BRD4204C)]
        [TestCase(ChipSeries.ZG23_BRD4204D)]
        [TestCase(ChipSeries.ZG23_BRD4210A)]
        [TestCase(ChipSeries.ZGM230S_BRD4205A)]
        [TestCase(ChipSeries.ZGM230S_BRD4205B)]
        [TestCase(ChipSeries.ZGM230_BRD2603A)]
        [TestCase(ChipSeries.ZG14_BRD4206A)]
        [TestCase(ChipSeries.ZGM130S_BRD4207A)]
        [TestCase(ChipSeries.ZG14_BRD4208A)]
        [TestCase(ChipSeries.RZ13_BRD4209A)]
        [TestCase(ChipSeries.ZG23_BRD4210A)]
        [TestCase(ChipSeries.xG28_BRD4400A)]
        [TestCase(ChipSeries.xG28_BRD4401A)]
        [TestCase(ChipSeries.xG28_BRD4400B)]
        [TestCase(ChipSeries.xG28_BRD4401B)]
        [TestCase(ChipSeries.xG28_BRD4400C)]
        [TestCase(ChipSeries.xG28_BRD4401C)]
        public void SingleBoardControllerExplicitly_WithParameter(ChipSeries chipSeries)
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = chipSeries.ToString(), JName = "12345678901" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, chipSeries, LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult1 = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(1, dm.Devices.Count());
        }

        [TestCase(ChipSeries.ZG14_BRD4201A)]
        [TestCase(ChipSeries.ZGM130S_BRD4202A)]
        [TestCase(ChipSeries.ZG14_BRD4206A)]
        [TestCase(ChipSeries.ZGM130S_BRD4207A)]
        [TestCase(ChipSeries.ZG14_BRD4208A)]
        public void SingleBoardControllerZW070x_WithParameter(ChipSeries chipSeries)
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = chipSeries.ToString(), JName = "12345678901" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW070x, LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult1 = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(1, dm.Devices.Count());
        }

        [TestCase(ChipSeries.ZG23_BRD4204A)]
        [TestCase(ChipSeries.ZG23_BRD4204C)]
        [TestCase(ChipSeries.ZG23_BRD4204D)]
        [TestCase(ChipSeries.ZGM230S_BRD4205A)]
        [TestCase(ChipSeries.ZGM230S_BRD4205B)]
        [TestCase(ChipSeries.ZGM230_BRD2603A)]
        [TestCase(ChipSeries.ZG23_BRD4210A)]
        public void SingleBoardControllerZW080x_WithParameter(ChipSeries chipSeries)
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = chipSeries.ToString(), JName = "12345678901" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW080x, LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult1 = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(1, dm.Devices.Count());
        }

        [Test]
        public void Mixed_1xAlias2xLib_NxGk()
        {
            var expectedRequestCount = 2;
            var expectedSrc1 = "COM102";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add(expectedSrc1, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expectedSrc1, ChipSeries = "ZW0713", JName = "12345678902", Alias = "expected1" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW0713", JName = "12345678903" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM104", ChipSeries = "ZW050x", Alias = "expected2" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, "expected1", LibTypes.Controller));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, LibTypes.EndDeviceAny));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult1 = dm.GetDevice(new Alias(1));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(expectedRequestCount, dm.Devices.Count());
            NUnit.Framework.Assert.AreEqual(expectedSrc1, actualDeviceResult1.DataSource.SourceName);
        }

        [Test]
        //[Ignore("test fails due to chip series equal to 700s by default")]
        public void Mixed_8xLibAll_4xGk4x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Five));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Five));
            actualVersions.Add("COM105", new Tuple<Libraries, string>(Libraries.PowerStrip, Five));
            actualVersions.Add("COM106", new Tuple<Libraries, string>(Libraries.SensorPIR, Five));
            actualVersions.Add("COM107", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add("COM108", new Tuple<Libraries, string>(Libraries.SwitchOnOff, Five));
            actualVersions.Add("COM109", new Tuple<Libraries, string>(Libraries.WallController, Five));
            actualVersions.Add("COM51", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Seven));
            actualVersions.Add("COM52", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Seven));
            actualVersions.Add("COM53", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM54", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Seven));
            actualVersions.Add("COM55", new Tuple<Libraries, string>(Libraries.PowerStrip, Seven));
            actualVersions.Add("COM56", new Tuple<Libraries, string>(Libraries.SensorPIR, Seven));
            actualVersions.Add("COM57", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM58", new Tuple<Libraries, string>(Libraries.SwitchOnOff, Seven));
            actualVersions.Add("COM59", new Tuple<Libraries, string>(Libraries.WallController, Seven));

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
            requestDevices.Add(new Alias(4), new DeviceMappingAttribute(4, LibTypes.EndDeviceLib));
            requestDevices.Add(new Alias(5), new DeviceMappingAttribute(5, ChipSeries.ZW050x, LibTypes.ControllerBridgeLib));
            requestDevices.Add(new Alias(6), new DeviceMappingAttribute(6, ChipSeries.ZW050x, LibTypes.ControllerPortableLib));
            requestDevices.Add(new Alias(7), new DeviceMappingAttribute(7, ChipSeries.ZW050x, LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(8), new DeviceMappingAttribute(8, ChipSeries.ZW050x, LibTypes.EndDeviceLib));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(2)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(3)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(4)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(5)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(6)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(7)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(8)).Version);
        }

        [Test]
        public void Mixed_8xLibAll4xChip7And4xChip5_4xGk4x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Five));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Five));
            actualVersions.Add("COM105", new Tuple<Libraries, string>(Libraries.PowerStrip, Five));
            actualVersions.Add("COM106", new Tuple<Libraries, string>(Libraries.SensorPIR, Five));
            actualVersions.Add("COM107", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add("COM108", new Tuple<Libraries, string>(Libraries.SwitchOnOff, Five));
            actualVersions.Add("COM109", new Tuple<Libraries, string>(Libraries.WallController, Five));
            actualVersions.Add("COM51", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Seven));
            actualVersions.Add("COM52", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Seven));
            actualVersions.Add("COM53", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM54", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Seven));
            actualVersions.Add("COM55", new Tuple<Libraries, string>(Libraries.PowerStrip, Seven));
            actualVersions.Add("COM56", new Tuple<Libraries, string>(Libraries.SensorPIR, Seven));
            actualVersions.Add("COM57", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM58", new Tuple<Libraries, string>(Libraries.SwitchOnOff, Seven));
            actualVersions.Add("COM59", new Tuple<Libraries, string>(Libraries.WallController, Seven));

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
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW070x, LibTypes.ControllerBridgeLib));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, ChipSeries.ZW070x, LibTypes.ControllerPortableLib));
            requestDevices.Add(new Alias(3), new DeviceMappingAttribute(3, ChipSeries.ZW070x, LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(4), new DeviceMappingAttribute(4, ChipSeries.ZW070x, LibTypes.EndDeviceLib));
            requestDevices.Add(new Alias(5), new DeviceMappingAttribute(5, ChipSeries.ZW050x, LibTypes.ControllerBridgeLib));
            requestDevices.Add(new Alias(6), new DeviceMappingAttribute(6, ChipSeries.ZW050x, LibTypes.ControllerPortableLib));
            requestDevices.Add(new Alias(7), new DeviceMappingAttribute(7, ChipSeries.ZW050x, LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(8), new DeviceMappingAttribute(8, ChipSeries.ZW050x, LibTypes.EndDeviceLib));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(2)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(3)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(4)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(5)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(6)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(7)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(8)).Version);
        }

        [Test]
        public void Mixed_8xLibsAll4xChip5_4xGk4x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Five));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Five));
            actualVersions.Add("COM105", new Tuple<Libraries, string>(Libraries.PowerStrip, Five));
            actualVersions.Add("COM106", new Tuple<Libraries, string>(Libraries.SensorPIR, Five));
            actualVersions.Add("COM107", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add("COM108", new Tuple<Libraries, string>(Libraries.SwitchOnOff, Five));
            actualVersions.Add("COM109", new Tuple<Libraries, string>(Libraries.WallController, Five));
            actualVersions.Add("COM51", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Seven));
            actualVersions.Add("COM52", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Seven));
            actualVersions.Add("COM53", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM54", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Seven));
            actualVersions.Add("COM55", new Tuple<Libraries, string>(Libraries.PowerStrip, Seven));
            actualVersions.Add("COM56", new Tuple<Libraries, string>(Libraries.SensorPIR, Seven));
            actualVersions.Add("COM57", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM58", new Tuple<Libraries, string>(Libraries.SwitchOnOff, Seven));
            actualVersions.Add("COM59", new Tuple<Libraries, string>(Libraries.WallController, Seven));

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
            requestDevices.Add(new Alias(4), new DeviceMappingAttribute(4, LibTypes.EndDeviceLib));
            requestDevices.Add(new Alias(5), new DeviceMappingAttribute(5, ChipSeries.ZW050x, LibTypes.ControllerBridgeLib));
            requestDevices.Add(new Alias(6), new DeviceMappingAttribute(6, ChipSeries.ZW050x, LibTypes.ControllerPortableLib));
            requestDevices.Add(new Alias(7), new DeviceMappingAttribute(7, ChipSeries.ZW050x, LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(8), new DeviceMappingAttribute(8, ChipSeries.ZW050x, LibTypes.EndDeviceLib));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(2)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(3)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(4)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(5)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(6)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(7)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(8)).Version);
        }

        [Test]
        //[Ignore("test fails due to chip series equal to 700s by default")]
        public void Mixed_8xLibsAll4xChip7_4xGk4x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Five));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Five));
            actualVersions.Add("COM105", new Tuple<Libraries, string>(Libraries.PowerStrip, Five));
            actualVersions.Add("COM106", new Tuple<Libraries, string>(Libraries.SensorPIR, Five));
            actualVersions.Add("COM107", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add("COM108", new Tuple<Libraries, string>(Libraries.SwitchOnOff, Five));
            actualVersions.Add("COM109", new Tuple<Libraries, string>(Libraries.WallController, Five));
            actualVersions.Add("COM51", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Seven));
            actualVersions.Add("COM52", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Seven));
            actualVersions.Add("COM53", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM54", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Seven));
            actualVersions.Add("COM55", new Tuple<Libraries, string>(Libraries.PowerStrip, Seven));
            actualVersions.Add("COM56", new Tuple<Libraries, string>(Libraries.SensorPIR, Seven));
            actualVersions.Add("COM57", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM58", new Tuple<Libraries, string>(Libraries.SwitchOnOff, Seven));
            actualVersions.Add("COM59", new Tuple<Libraries, string>(Libraries.WallController, Seven));

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
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW070x, LibTypes.ControllerBridgeLib));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, ChipSeries.ZW070x, LibTypes.ControllerPortableLib));
            requestDevices.Add(new Alias(3), new DeviceMappingAttribute(3, ChipSeries.ZW070x, LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(4), new DeviceMappingAttribute(4, ChipSeries.ZW070x, LibTypes.EndDeviceLib));
            requestDevices.Add(new Alias(5), new DeviceMappingAttribute(5, ChipSeries.ZW050x, LibTypes.ControllerBridgeLib));
            requestDevices.Add(new Alias(6), new DeviceMappingAttribute(6, ChipSeries.ZW050x, LibTypes.ControllerPortableLib));
            requestDevices.Add(new Alias(7), new DeviceMappingAttribute(7, ChipSeries.ZW050x, LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(8), new DeviceMappingAttribute(8, ChipSeries.ZW050x, LibTypes.EndDeviceLib));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(2)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(3)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(4)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(5)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(6)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(7)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(8)).Version);
        }

        [Test]
        public void Mixed_8xLibAll4xAlias5_4xGk4x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Five));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Five));
            actualVersions.Add("COM105", new Tuple<Libraries, string>(Libraries.PowerStrip, Five));
            actualVersions.Add("COM106", new Tuple<Libraries, string>(Libraries.SensorPIR, Five));
            actualVersions.Add("COM107", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add("COM108", new Tuple<Libraries, string>(Libraries.SwitchOnOff, Five));
            actualVersions.Add("COM109", new Tuple<Libraries, string>(Libraries.WallController, Five));
            actualVersions.Add("COM51", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Seven));
            actualVersions.Add("COM52", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Seven));
            actualVersions.Add("COM53", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM54", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Seven));
            actualVersions.Add("COM55", new Tuple<Libraries, string>(Libraries.PowerStrip, Seven));
            actualVersions.Add("COM56", new Tuple<Libraries, string>(Libraries.SensorPIR, Seven));
            actualVersions.Add("COM57", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM58", new Tuple<Libraries, string>(Libraries.SwitchOnOff, Seven));
            actualVersions.Add("COM59", new Tuple<Libraries, string>(Libraries.WallController, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x", Alias = "A" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x", Alias = "B" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x", Alias = "C" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM104", ChipSeries = "ZW050x", Alias = "D" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM105", ChipSeries = "ZW050x", Alias = "E" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM106", ChipSeries = "ZW050x", Alias = "F" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM107", ChipSeries = "ZW050x", Alias = "G" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM108", ChipSeries = "ZW050x", Alias = "K" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM109", ChipSeries = "ZW050x", Alias = "L" });
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
            requestDevices.Add(new Alias(4), new DeviceMappingAttribute(4, LibTypes.EndDeviceLib));
            requestDevices.Add(new Alias(5), new DeviceMappingAttribute(5, ChipSeries.ZW050x, LibTypes.ControllerBridgeLib) { Alias = "A" });
            requestDevices.Add(new Alias(6), new DeviceMappingAttribute(6, ChipSeries.ZW050x, LibTypes.ControllerPortableLib) { Alias = "B" });
            requestDevices.Add(new Alias(7), new DeviceMappingAttribute(7, ChipSeries.ZW050x, LibTypes.ControllerStaticLib) { Alias = "C" });
            requestDevices.Add(new Alias(8), new DeviceMappingAttribute(8, ChipSeries.ZW050x, LibTypes.EndDeviceLib) { Alias = "G" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(2)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(3)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(4)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(5)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(6)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(7)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(8)).Version);
        }

        [Test]
        //[Ignore("test fails due to chip series equal to 700s by default")]
        public void Mixed_8xLibAll4xAlias7_4xGk4x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Five));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Five));
            actualVersions.Add("COM105", new Tuple<Libraries, string>(Libraries.PowerStrip, Five));
            actualVersions.Add("COM106", new Tuple<Libraries, string>(Libraries.SensorPIR, Five));
            actualVersions.Add("COM107", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add("COM108", new Tuple<Libraries, string>(Libraries.SwitchOnOff, Five));
            actualVersions.Add("COM109", new Tuple<Libraries, string>(Libraries.WallController, Five));
            actualVersions.Add("COM51", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Seven));
            actualVersions.Add("COM52", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Seven));
            actualVersions.Add("COM53", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM54", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Seven));
            actualVersions.Add("COM55", new Tuple<Libraries, string>(Libraries.PowerStrip, Seven));
            actualVersions.Add("COM56", new Tuple<Libraries, string>(Libraries.SensorPIR, Seven));
            actualVersions.Add("COM57", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM58", new Tuple<Libraries, string>(Libraries.SwitchOnOff, Seven));
            actualVersions.Add("COM59", new Tuple<Libraries, string>(Libraries.WallController, Seven));

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
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM51", ChipSeries = "ZW0713", JName = "1324567801", Alias = "A" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM52", ChipSeries = "ZW0713", JName = "1324567802", Alias = "B" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM53", ChipSeries = "ZW0713", JName = "1324567803", Alias = "C" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM54", ChipSeries = "ZW0713", JName = "1324567804", Alias = "D" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM55", ChipSeries = "ZW0713", JName = "1324567805", Alias = "E" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM56", ChipSeries = "ZW0713", JName = "1324567806", Alias = "F" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM57", ChipSeries = "ZW0713", JName = "1324567807", Alias = "G" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM58", ChipSeries = "ZW0713", JName = "1324567808", Alias = "K" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM59", ChipSeries = "ZW0713", JName = "1324567809", Alias = "L" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, "A", LibTypes.ControllerBridgeLib));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, "B", LibTypes.ControllerPortableLib));
            requestDevices.Add(new Alias(3), new DeviceMappingAttribute(3, "C", LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(4), new DeviceMappingAttribute(4, "G", LibTypes.EndDeviceLib));
            requestDevices.Add(new Alias(5), new DeviceMappingAttribute(5, ChipSeries.ZW050x, LibTypes.ControllerBridgeLib));
            requestDevices.Add(new Alias(6), new DeviceMappingAttribute(6, ChipSeries.ZW050x, LibTypes.ControllerPortableLib));
            requestDevices.Add(new Alias(7), new DeviceMappingAttribute(7, ChipSeries.ZW050x, LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(8), new DeviceMappingAttribute(8, ChipSeries.ZW050x, LibTypes.EndDeviceLib));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(2)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(3)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(4)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(5)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(6)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(7)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(8)).Version);
        }

        [Test]
        public void Mixed_6xLib3xChip5And3xChip7_3xGk3x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Five));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Five));
            actualVersions.Add("COM105", new Tuple<Libraries, string>(Libraries.PowerStrip, Five));
            actualVersions.Add("COM106", new Tuple<Libraries, string>(Libraries.SensorPIR, Five));
            actualVersions.Add("COM107", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add("COM108", new Tuple<Libraries, string>(Libraries.SwitchOnOff, Five));
            actualVersions.Add("COM109", new Tuple<Libraries, string>(Libraries.WallController, Five));
            actualVersions.Add("COM51", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Seven));
            actualVersions.Add("COM52", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Seven));
            actualVersions.Add("COM53", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM54", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Seven));
            actualVersions.Add("COM55", new Tuple<Libraries, string>(Libraries.PowerStrip, Seven));
            actualVersions.Add("COM56", new Tuple<Libraries, string>(Libraries.SensorPIR, Seven));
            actualVersions.Add("COM57", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM58", new Tuple<Libraries, string>(Libraries.SwitchOnOff, Seven));
            actualVersions.Add("COM59", new Tuple<Libraries, string>(Libraries.WallController, Seven));

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
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW070x, LibTypes.Controller));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, ChipSeries.ZW070x, LibTypes.Controller));
            requestDevices.Add(new Alias(3), new DeviceMappingAttribute(3, ChipSeries.ZW070x, LibTypes.Controller));
            requestDevices.Add(new Alias(4), new DeviceMappingAttribute(4, ChipSeries.ZW050x, LibTypes.Controller));
            requestDevices.Add(new Alias(5), new DeviceMappingAttribute(5, ChipSeries.ZW050x, LibTypes.Controller));
            requestDevices.Add(new Alias(6), new DeviceMappingAttribute(6, ChipSeries.ZW050x, LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(2)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(3)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(4)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(5)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(6)).Version);
        }

        [Test]
        public void Mixed_6xLib3xChip5_3xGk3x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Five));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Five));
            actualVersions.Add("COM105", new Tuple<Libraries, string>(Libraries.PowerStrip, Five));
            actualVersions.Add("COM106", new Tuple<Libraries, string>(Libraries.SensorPIR, Five));
            actualVersions.Add("COM107", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add("COM108", new Tuple<Libraries, string>(Libraries.SwitchOnOff, Five));
            actualVersions.Add("COM109", new Tuple<Libraries, string>(Libraries.WallController, Five));
            actualVersions.Add("COM51", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Seven));
            actualVersions.Add("COM52", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Seven));
            actualVersions.Add("COM53", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM54", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Seven));
            actualVersions.Add("COM55", new Tuple<Libraries, string>(Libraries.PowerStrip, Seven));
            actualVersions.Add("COM56", new Tuple<Libraries, string>(Libraries.SensorPIR, Seven));
            actualVersions.Add("COM57", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM58", new Tuple<Libraries, string>(Libraries.SwitchOnOff, Seven));
            actualVersions.Add("COM59", new Tuple<Libraries, string>(Libraries.WallController, Seven));

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
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.Controller));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, LibTypes.Controller));
            requestDevices.Add(new Alias(3), new DeviceMappingAttribute(3, LibTypes.Controller));
            requestDevices.Add(new Alias(4), new DeviceMappingAttribute(4, ChipSeries.ZW050x, LibTypes.Controller));
            requestDevices.Add(new Alias(5), new DeviceMappingAttribute(5, ChipSeries.ZW050x, LibTypes.Controller));
            requestDevices.Add(new Alias(6), new DeviceMappingAttribute(6, ChipSeries.ZW050x, LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(2)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(3)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(4)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(5)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(6)).Version);
        }

        [Test]
        //[Ignore("test fails due to chip series equal to 700s by default")]
        public void Mixed_6xLib3xChip7_3x5s3xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Five));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Five));
            actualVersions.Add("COM105", new Tuple<Libraries, string>(Libraries.PowerStrip, Five));
            actualVersions.Add("COM106", new Tuple<Libraries, string>(Libraries.SensorPIR, Five));
            actualVersions.Add("COM107", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add("COM108", new Tuple<Libraries, string>(Libraries.SwitchOnOff, Five));
            actualVersions.Add("COM109", new Tuple<Libraries, string>(Libraries.WallController, Five));
            actualVersions.Add("COM51", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Seven));
            actualVersions.Add("COM52", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Seven));
            actualVersions.Add("COM53", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM54", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Seven));
            actualVersions.Add("COM55", new Tuple<Libraries, string>(Libraries.PowerStrip, Seven));
            actualVersions.Add("COM56", new Tuple<Libraries, string>(Libraries.SensorPIR, Seven));
            actualVersions.Add("COM57", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM58", new Tuple<Libraries, string>(Libraries.SwitchOnOff, Seven));
            actualVersions.Add("COM59", new Tuple<Libraries, string>(Libraries.WallController, Seven));

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
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.Controller));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, ChipSeries.ZW050x, LibTypes.Controller));
            requestDevices.Add(new Alias(3), new DeviceMappingAttribute(3, ChipSeries.ZW050x, LibTypes.Controller));
            requestDevices.Add(new Alias(4), new DeviceMappingAttribute(4, LibTypes.Controller));
            requestDevices.Add(new Alias(5), new DeviceMappingAttribute(5, LibTypes.Controller));
            requestDevices.Add(new Alias(6), new DeviceMappingAttribute(6, LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);

            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(2)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(3)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(4)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(5)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(6)).Version);
        }

        [Test]
        //[Ignore("test fails due to chip series equal to 700s by default")]
        public void Mixed_6xLib_3xGk3x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Five));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Five));
            actualVersions.Add("COM105", new Tuple<Libraries, string>(Libraries.PowerStrip, Five));
            actualVersions.Add("COM106", new Tuple<Libraries, string>(Libraries.SensorPIR, Five));
            actualVersions.Add("COM107", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add("COM108", new Tuple<Libraries, string>(Libraries.SwitchOnOff, Five));
            actualVersions.Add("COM109", new Tuple<Libraries, string>(Libraries.WallController, Five));
            actualVersions.Add("COM51", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Seven));
            actualVersions.Add("COM52", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Seven));
            actualVersions.Add("COM53", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM54", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Seven));
            actualVersions.Add("COM55", new Tuple<Libraries, string>(Libraries.PowerStrip, Seven));
            actualVersions.Add("COM56", new Tuple<Libraries, string>(Libraries.SensorPIR, Seven));
            actualVersions.Add("COM57", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM58", new Tuple<Libraries, string>(Libraries.SwitchOnOff, Seven));
            actualVersions.Add("COM59", new Tuple<Libraries, string>(Libraries.WallController, Seven));

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
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.Controller));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, LibTypes.Controller));
            requestDevices.Add(new Alias(3), new DeviceMappingAttribute(3, LibTypes.Controller));
            requestDevices.Add(new Alias(4), new DeviceMappingAttribute(4, ChipSeries.ZW050x, LibTypes.Controller));
            requestDevices.Add(new Alias(5), new DeviceMappingAttribute(5, ChipSeries.ZW050x, LibTypes.Controller));
            requestDevices.Add(new Alias(6), new DeviceMappingAttribute(6, ChipSeries.ZW050x, LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(2)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(3)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(4)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(5)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(6)).Version);
        }

        [Test]
        public void Mixed_4xLib2xChip5And2xChip7_2xGk2x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Five));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Five));
            actualVersions.Add("COM105", new Tuple<Libraries, string>(Libraries.EndDeviceLegacyLib, Five));
            actualVersions.Add("COM106", new Tuple<Libraries, string>(Libraries.EndDeviceLegacyRoutingLib, Five));
            actualVersions.Add("COM107", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add("COM108", new Tuple<Libraries, string>(Libraries.SwitchOnOff, Five));
            actualVersions.Add("COM109", new Tuple<Libraries, string>(Libraries.WallController, Five));
            actualVersions.Add("COM51", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Seven));
            actualVersions.Add("COM52", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Seven));
            actualVersions.Add("COM53", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM54", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Seven));
            actualVersions.Add("COM55", new Tuple<Libraries, string>(Libraries.PowerStrip, Seven));
            actualVersions.Add("COM56", new Tuple<Libraries, string>(Libraries.SensorPIR, Seven));
            actualVersions.Add("COM57", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM58", new Tuple<Libraries, string>(Libraries.EndDeviceLegacyLib, Seven));
            actualVersions.Add("COM59", new Tuple<Libraries, string>(Libraries.EndDeviceLegacyRoutingLib, Seven));

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
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW070x, LibTypes.EndDeviceAny));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, ChipSeries.ZW070x, LibTypes.EndDeviceAny));
            requestDevices.Add(new Alias(3), new DeviceMappingAttribute(3, ChipSeries.ZW050x, LibTypes.EndDeviceAny));
            requestDevices.Add(new Alias(4), new DeviceMappingAttribute(4, ChipSeries.ZW050x, LibTypes.EndDeviceAny));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(2)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(3)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(4)).Version);
        }

        [Test]
        public void Mixed_4xLib2xChip5_2xGk2x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Five));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Five));
            actualVersions.Add("COM105", new Tuple<Libraries, string>(Libraries.PowerStrip, Five));
            actualVersions.Add("COM106", new Tuple<Libraries, string>(Libraries.SensorPIR, Five));
            actualVersions.Add("COM107", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add("COM108", new Tuple<Libraries, string>(Libraries.EndDeviceLegacyRoutingLib, Five));
            actualVersions.Add("COM109", new Tuple<Libraries, string>(Libraries.EndDeviceLegacyLib, Five));
            actualVersions.Add("COM51", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Seven));
            actualVersions.Add("COM52", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Seven));
            actualVersions.Add("COM53", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM54", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Seven));
            actualVersions.Add("COM55", new Tuple<Libraries, string>(Libraries.PowerStrip, Seven));
            actualVersions.Add("COM56", new Tuple<Libraries, string>(Libraries.SensorPIR, Seven));
            actualVersions.Add("COM57", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM58", new Tuple<Libraries, string>(Libraries.EndDeviceLegacyRoutingLib, Seven));
            actualVersions.Add("COM59", new Tuple<Libraries, string>(Libraries.EndDeviceLegacyLib, Seven));

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
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.EndDeviceAny));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, LibTypes.EndDeviceAny));
            requestDevices.Add(new Alias(3), new DeviceMappingAttribute(3, ChipSeries.ZW050x, LibTypes.EndDeviceAny));
            requestDevices.Add(new Alias(4), new DeviceMappingAttribute(4, ChipSeries.ZW050x, LibTypes.EndDeviceAny));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(2)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(3)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(4)).Version);
        }

        [Test]
        //[Ignore("test fails due to chip series equal to 700s by default")]
        public void Mixed_4xLib2xChip7_2x5s2xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Five));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Five));
            actualVersions.Add("COM105", new Tuple<Libraries, string>(Libraries.PowerStrip, Five));
            actualVersions.Add("COM106", new Tuple<Libraries, string>(Libraries.SensorPIR, Five));
            actualVersions.Add("COM107", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add("COM109", new Tuple<Libraries, string>(Libraries.EndDeviceLegacyRoutingLib, Five));
            actualVersions.Add("COM51", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Seven));
            actualVersions.Add("COM52", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Seven));
            actualVersions.Add("COM53", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM54", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Seven));
            actualVersions.Add("COM55", new Tuple<Libraries, string>(Libraries.PowerStrip, Seven));
            actualVersions.Add("COM56", new Tuple<Libraries, string>(Libraries.SensorPIR, Seven));
            actualVersions.Add("COM57", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM59", new Tuple<Libraries, string>(Libraries.EndDeviceLegacyRoutingLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM104", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM105", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM106", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM107", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM109", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM51", ChipSeries = "ZW0713", JName = "1324567801" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM52", ChipSeries = "ZW0713", JName = "1324567802" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM53", ChipSeries = "ZW0713", JName = "1324567803" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM54", ChipSeries = "ZW0713", JName = "1324567804" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM55", ChipSeries = "ZW0713", JName = "1324567805" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM56", ChipSeries = "ZW0713", JName = "1324567806" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM57", ChipSeries = "ZW0713", JName = "1324567807" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM59", ChipSeries = "ZW0713", JName = "1324567809" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.EndDeviceAny));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, ChipSeries.ZW050x, LibTypes.EndDeviceAny));
            requestDevices.Add(new Alias(3), new DeviceMappingAttribute(3, LibTypes.EndDeviceAny));
            requestDevices.Add(new Alias(4), new DeviceMappingAttribute(4, LibTypes.EndDeviceAny));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(2)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(3)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(4)).Version);
        }

        [Test]
        public void Mixed_4xLib_3xGk3x1s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Five));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Five));
            actualVersions.Add("COM105", new Tuple<Libraries, string>(Libraries.PowerStrip, Five));
            actualVersions.Add("COM106", new Tuple<Libraries, string>(Libraries.SensorPIR, Five));
            actualVersions.Add("COM107", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add("COM108", new Tuple<Libraries, string>(Libraries.EndDeviceLegacyRoutingLib, Five));
            actualVersions.Add("COM109", new Tuple<Libraries, string>(Libraries.EndDeviceLegacyLib, Five));
            actualVersions.Add("COM51", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Seven));
            actualVersions.Add("COM52", new Tuple<Libraries, string>(Libraries.ControllerPortableLib, Seven));
            actualVersions.Add("COM53", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));
            actualVersions.Add("COM54", new Tuple<Libraries, string>(Libraries.DoorLockKeyPad, Seven));
            actualVersions.Add("COM55", new Tuple<Libraries, string>(Libraries.PowerStrip, Seven));
            actualVersions.Add("COM56", new Tuple<Libraries, string>(Libraries.SensorPIR, Seven));
            actualVersions.Add("COM57", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM58", new Tuple<Libraries, string>(Libraries.EndDeviceLegacyRoutingLib, Seven));
            actualVersions.Add("COM59", new Tuple<Libraries, string>(Libraries.EndDeviceLegacyLib, Seven));

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
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.EndDeviceAny));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, LibTypes.EndDeviceAny));
            requestDevices.Add(new Alias(3), new DeviceMappingAttribute(3, LibTypes.EndDeviceAny));
            requestDevices.Add(new Alias(4), new DeviceMappingAttribute(4, ChipSeries.ZW050x, LibTypes.EndDeviceAny));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(2)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(3)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(4)).Version);
        }

        [Test]
        public void Mixed_2xAlias3xLib_1xGk2x5s()
        {
            var expSrc2 = "COM104";
            var expSrc3 = "COM103";
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Seven));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Seven));
            actualVersions.Add(expSrc3, new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add(expSrc2, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "12345678901" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW0713", JName = "12345678902" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expSrc3, ChipSeries = "ZW050x", Alias = "Board" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = expSrc2, ChipSeries = "ZW050x", IsUZB = true, Alias = "Stick" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.ControllerStaticLib));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, ChipSeries.ZW050x, LibTypes.Controller) { Alias = "Stick" });
            requestDevices.Add(new Alias(3), new DeviceMappingAttribute(3, ChipSeries.ZW050x, LibTypes.EndDeviceAny) { Alias = "Board" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult1 = dm.GetDevice(new Alias(1));
            var actualDeviceResult2 = dm.GetDevice(new Alias(2));
            var actualDeviceResult3 = dm.GetDevice(new Alias(3));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, actualDeviceResult1.Version);
            NUnit.Framework.Assert.AreEqual(Five, actualDeviceResult2.Version);
            NUnit.Framework.Assert.AreEqual(Five, actualDeviceResult3.Version);
            NUnit.Framework.Assert.AreEqual(expSrc2, actualDeviceResult2.DataSource.SourceName);
            NUnit.Framework.Assert.AreEqual(expSrc3, actualDeviceResult3.DataSource.SourceName);
        }

        [Test]
        //[Ignore("Fails. Should take any device by lib if no matched alias")]
        public void Mixed_2xAliasAny3xLib_1x5s2xGk()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Seven));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM105", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW0713", JName = "12345678901" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW0713", JName = "12345678902" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW050x", Alias = "Board" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM104", ChipSeries = "ZW050x", IsUZB = true, Alias = "Stick" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM105", ChipSeries = "ZW050x" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.ControllerStaticLib) { Alias = "Stick" });
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, "Any", LibTypes.Controller));
            requestDevices.Add(new Alias(3), new DeviceMappingAttribute(3, LibTypes.EndDeviceAny));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);
            var actualDeviceResult1 = dm.GetDevice(new Alias(1));
            var actualDeviceResult2 = dm.GetDevice(new Alias(2));
            var actualDeviceResult3 = dm.GetDevice(new Alias(3));

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Five, actualDeviceResult1.Version);
            NUnit.Framework.Assert.AreEqual(Seven, actualDeviceResult2.Version);
            NUnit.Framework.Assert.AreEqual(Seven, actualDeviceResult3.Version);
        }

        [Test]
        public void Empty_Any_InitFailed()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            //actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Five));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.EndDeviceAny));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
            NUnit.Framework.Assert.IsEmpty(dm.Devices);
        }

        [Test]
        public void Mixed_EMPTY_InitTrue()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Five));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW0713", JName = "13245678901" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsEmpty(dm.Devices);
        }

        [Test]
        //[Ignore("test fails due to doesn't init device by alias if not exist alias in the profile")]
        public void Mixed_2xAlias4xLib2xAll_4xGk4x5s()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Five));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM104", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add("COM105", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add("COM106", new Tuple<Libraries, string>(Libraries.SensorPIR, Five));
            actualVersions.Add("COM107", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add("COM108", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Five));
            actualVersions.Add("COM109", new Tuple<Libraries, string>(Libraries.SensorPIR, Five));
            //geckos
            actualVersions.Add("COM51", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Seven));
            actualVersions.Add("COM52", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Seven));
            actualVersions.Add("COM53", new Tuple<Libraries, string>(Libraries.ControllerBridgeLib, Seven));
            actualVersions.Add("COM54", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM55", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM56", new Tuple<Libraries, string>(Libraries.SensorPIR, Seven));
            actualVersions.Add("COM57", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM58", new Tuple<Libraries, string>(Libraries.EndDeviceLib, Seven));
            actualVersions.Add("COM59", new Tuple<Libraries, string>(Libraries.SensorPIR, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { PName = "COM201", SName = "COM101", ChipSeries = "ZW050x", Alias = "Ctrl" });
            profile.DeviceHosts.Add(new DeviceHost() { PName = "COM202", SName = "COM102", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { PName = "COM203", SName = "COM103", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { PName = "COM204", SName = "COM104", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { PName = "COM205", SName = "COM105", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { PName = "COM206", SName = "COM106", ChipSeries = "ZW050x" });
            profile.DeviceHosts.Add(new DeviceHost() { PName = "COM207", SName = "COM107", ChipSeries = "ZW050x", Alias = "z5s1" });
            profile.DeviceHosts.Add(new DeviceHost() { PName = "COM208", SName = "COM108", ChipSeries = "ZW050x", Alias = "z5s2" });
            profile.DeviceHosts.Add(new DeviceHost() { PName = "COM209", SName = "COM109", ChipSeries = "ZW050x", Alias = "z5s3" });
            profile.DeviceHosts.Add(new DeviceHost() { PName = "COM210", SName = "COM51", ChipSeries = "ZW0713", JName = "1324567801", Alias = "Ctrl" });
            profile.DeviceHosts.Add(new DeviceHost() { PName = "COM212", SName = "COM52", ChipSeries = "ZW0713", JName = "1324567802" });
            profile.DeviceHosts.Add(new DeviceHost() { PName = "COM213", SName = "COM53", ChipSeries = "ZW0713", JName = "1324567803" });
            profile.DeviceHosts.Add(new DeviceHost() { PName = "COM214", SName = "COM54", ChipSeries = "ZW0713", JName = "1324567804" });
            profile.DeviceHosts.Add(new DeviceHost() { PName = "COM215", SName = "COM55", ChipSeries = "ZW0713", JName = "1324567805" });
            profile.DeviceHosts.Add(new DeviceHost() { PName = "COM216", SName = "COM56", ChipSeries = "ZW0713", JName = "1324567806" });
            profile.DeviceHosts.Add(new DeviceHost() { PName = "COM217", SName = "COM57", ChipSeries = "ZW0713", JName = "1324567807", Alias = "Gk1" });
            profile.DeviceHosts.Add(new DeviceHost() { PName = "COM218", SName = "COM58", ChipSeries = "ZW0713", JName = "1324567808", Alias = "Gk2" });
            profile.DeviceHosts.Add(new DeviceHost() { PName = "COM219", SName = "COM59", ChipSeries = "ZW0713", JName = "1324567809", Alias = "Gk3" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.Controller));
            requestDevices.Add(new Alias(2), new DeviceMappingAttribute(2, ChipSeries.ZW050x, LibTypes.Controller));
            requestDevices.Add(new Alias(3), new DeviceMappingAttribute(3, LibTypes.EndDeviceAny));
            requestDevices.Add(new Alias(4), new DeviceMappingAttribute(4, ChipSeries.ZW050x, LibTypes.EndDeviceAny));
            requestDevices.Add(new Alias(5), new DeviceMappingAttribute(5, "Gk1", LibTypes.EndDeviceLib));
            requestDevices.Add(new Alias(6), new DeviceMappingAttribute(6, ChipSeries.ZW050x, LibTypes.EndDeviceLib) { Alias = "z5s2" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(1)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(2)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(3)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(4)).Version);
            NUnit.Framework.Assert.AreEqual(Seven, dm.GetDevice(new Alias(5)).Version);
            NUnit.Framework.Assert.AreEqual(Five, dm.GetDevice(new Alias(6)).Version);
        }

        private static readonly string _BusyPortName = "ZnifBusy";
        [Test]
        public void ZnifferRequired_Busy_InitFalse()
        {
            var expected = "COM101";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));

            var profile = new Profile();
            profile.ZName = _BusyPortName; //Important in this test!

            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW050x" });
            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);

            //Act
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment, true);

            //Assert
            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test]
        [Ignore("Zniffer is not started by that logic?!")]
        public void ZnifferRequired_NotBusy_InitTrue()
        {
            var expected = "COM101";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));

            var profile = new Profile();
            profile.ZName = "zniffer1"; //Important in this test!

            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW050x" });
            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);

            //Act
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment, true);

            //Assert
            NUnit.Framework.Assert.IsTrue(actualSetupResult);
        }

        [Test]
        public void ZnifferNOTRequired_Busy_InitFalse()
        {
            var expected = "COM101";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));

            var profile = new Profile();
            profile.ZName = _BusyPortName; //Important in this test!

            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW050x" });
            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);

            //Act
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment, false); // not req

            //Assert
            NUnit.Framework.Assert.IsTrue(actualSetupResult);
        }

        [Test]
        public void ZnifferNOTRequired_NotBusy_InitTrue()
        {
            var expected = "COM101";

            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add(expected, new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));

            var profile = new Profile();
            profile.ZName = "zniffer1"; //Important in this test!

            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = expected, ChipSeries = "ZW050x" });
            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, LibTypes.Controller));

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);

            //Act
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment, false); ; // not req

            //Assert
            NUnit.Framework.Assert.IsTrue(actualSetupResult);
        }

        [Test]
        public void HopNotMatch_Any_NotDetected()
        {
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM101", ChipSeries = "ZW050x", Hop="NO" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW070x", Hop="EXP" });

            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.ControllerStaticLib) { Hop = "EXP" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsFalse(actualSetupResult);
        }

        [Test]
        public void HopMatch_Any_Detected()
        {
            var expectedSrc = "COM101";
            var actualVersions = new Dictionary<string, Tuple<Libraries, string>>();
            actualVersions.Add("COM101", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM102", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Five));
            actualVersions.Add("COM103", new Tuple<Libraries, string>(Libraries.ControllerStaticLib, Seven));

            var profile = new Profile();
            profile.DeviceHosts = new ObservableCollection<DeviceHost>();
            profile.DeviceHosts.Add(new DeviceHost() { SName = expectedSrc, ChipSeries = "ZW050x", Hop = "EXP" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM102", ChipSeries = "ZW050x", Hop = "no" });
            profile.DeviceHosts.Add(new DeviceHost() { SName = "COM103", ChipSeries = "ZW070x", Hop = "EXP" });


            var requestDevices = new SortedList<Alias, DeviceMappingAttribute>();
            requestDevices.Add(new Alias(1), new DeviceMappingAttribute(1, ChipSeries.ZW050x, LibTypes.ControllerStaticLib) { Hop = "EXP" });

            ZatsEnvironment zatsEnvironment = CreateZatsEnvironment(actualVersions, requestDevices, out DeviceManager dm);
            bool actualSetupResult = RunSetupReadMode(profile, requestDevices, zatsEnvironment);

            NUnit.Framework.Assert.IsTrue(actualSetupResult);
            var actualDeviceResult1 = dm.GetDevice(new Alias(1));
            NUnit.Framework.Assert.IsNotNull(dm.Devices);
            NUnit.Framework.Assert.AreEqual(1, dm.Devices.Count());
            NUnit.Framework.Assert.AreEqual("COM101", actualDeviceResult1.DataSource.SourceName);
        }
    }
}
