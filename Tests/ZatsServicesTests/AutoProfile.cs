// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Moq;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Utils;
using Zats.Services;
using ZatsTestConsole;
using ZWave;
using ZWave.BasicApplication.Operations;
using ZWave.Enums;
using ZWave.Layers;
using ZWave.Operations;

namespace ZatsServicesTests
{
    [TestFixture]
    public class AutoProfile
    {
        ISessionLayer _sessionLayer;
        ZWave.BasicApplication.BasicApplicationLayer _basicApp;
        ZWave.ZnifferApplication.ZnifferApplicationLayer _znifferApp;
        ZWave.BasicApplication.BasicApplicationLayer _tcpApp;
        Mock<IProgrammingProvider> _gammaProgrammer;
        Mock<IProgrammingProvider> _alphaProgrammer;
        Mock<IFileSystemManager> _fileSystemManager;
        Mock<IPortNamesGetter> _portNamesGetter;
        Mock<IUdpDevicesDiscoveryService<JlinkDeviceInfo>> _udpJLinkDiscoveryService;

        public enum JLink
        {
            JLCOM1, JLCOM2, JLCOM3, JLCOM4, JLCOM5, JLCOM6, JLCOM7, JLCOM8, JLCOM9
        }

        public enum SAlpha
        {
            ACOM1, ACOM2, ACOM3, ACOM4, ACOM5, ACOM6, ACOM7, ACOM8, ACOM9
        }

        public enum PAlpha
        {
            PCOM1, PCOM2, PCOM3, PCOM4, PCOM5, PCOM6, PCOM7, PCOM8, PCOM9
        }

        public enum SNumber
        {
            SN1, SN2, SN3, SN4, SN5, SN6, SN7, SN8, SN9, SN10, SN11
        }

        public class IPs
        {
            public static string IP1 = "192.168.1.111";
            public static string IP2 = "192.168.1.112";
            public static string IP3 = "192.168.1.113";
            public static string IP4 = "192.168.1.114";
        }

        public class DeviceHostTest
        {
            public ChipTypes ChipType { get; set; }
            public JLink? JLink { get; set; }
            public SNumber? SNumber { get; set; }
            public SAlpha? SAlpha { get; set; }
            public PAlpha? PAlpha { get; set; }
            public string IpAddress { get; set; }
            public ushort SerialSessionId { get; private set; }
            public ushort ProgSessionId { get; private set; }
            public byte LedState { get; set; }
            public int HomeId { get; set; }
            public Libraries Library { get; set; }

            public DeviceHostTest(ChipTypes chipType, Libraries library, JLink jLink, SNumber? sNumber, string ipAddress, PAlpha? pAlpha)
            {
                ChipType = chipType;
                Library = library;
                JLink = jLink;
                SNumber = sNumber;
                IpAddress = ipAddress;
                PAlpha = pAlpha;
            }

            public DeviceHostTest(ChipTypes chipType, Libraries library, SAlpha? sAlpha, PAlpha? pAlpha)
            {
                ChipType = chipType;
                Library = library;
                SAlpha = sAlpha;
                PAlpha = pAlpha;
            }

            internal void SetSerialSessionId(ushort sessionId)
            {
                SerialSessionId = sessionId;
            }

            internal void SetProgSessionId(ushort sessionId)
            {
                ProgSessionId = sessionId;
            }
        }

        private const int MAX_SESSIONS = 0x7FFF;
        private BitArray SessionIds = new BitArray(MAX_SESSIONS);

        public byte NextSessionId()
        {
            lock (this)
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

        public void ReleaseSessionId(ushort sessionId)
        {
            lock (this)
            {
                $"SL unregistered {sessionId}"._DLOG();
                SessionIds[sessionId] = false;
            }
        }

        public void ResetSessionIdCounter()
        {
            SessionIds = new BitArray(MAX_SESSIONS);
        }

        private void SetUpEnvironment(IEnumerable<DeviceHostTest> available)
        {
            //ResetSessionIdCounter(); // TODO fail if removed. Check why?
            NUnit.Framework.Assert.IsTrue(available.All(x =>
            {
                bool ret = (x.IpAddress == null || available.Count(y => x.IpAddress == y.IpAddress) == 1)
                && (x.JLink == null || available.Count(y => x.JLink == y.JLink) == 1)
                && (x.PAlpha == null || available.Count(y => x.PAlpha == y.PAlpha) == 1)
                && (x.SAlpha == null || available.Count(y => x.SAlpha == y.SAlpha) == 1)
                && (x.SNumber == null || available.Count(y => x.SNumber == y.SNumber) == 1);
                return ret;
            }));

            var sessionLayer = new Mock<ISessionLayer>();
            sessionLayer.Setup(x => x.NextSessionId()).Returns(() =>
            {
                return NextSessionId();
            });
            sessionLayer.Setup(x => x.ReleaseSessionId(It.IsAny<ushort>())).Callback((ushort x) =>
            {
                ReleaseSessionId(x);
                var tmp = available.Where(y => y.SerialSessionId == x);
                foreach (var t in tmp)
                {
                    t.SetSerialSessionId(0);
                }
                tmp = available.Where(y => y.ProgSessionId == x);
                foreach (var t in tmp)
                {
                    t.SetProgSessionId(0);
                }
            });
            sessionLayer.Setup(x => x.ResetSessionIdCounter()).Callback(() =>
            {
                ResetSessionIdCounter();
                foreach (var t in available)
                {
                    t.SetSerialSessionId(0);
                    t.SetProgSessionId(0);
                }
            });
            sessionLayer.Setup(x => x.CreateClient(It.IsAny<ushort>())).Returns(() =>
            {
                var ret = new Mock<ISessionClient>();
                ret.SetupProperty(y => y.SessionId);
                ret.SetupProperty(y => y.IsHandleFrameEnabled);
                ret.Setup(y => y.ExecuteAsync(It.Is<IActionItem>(f => f is ActionBase)))
                    .Returns((IActionItem a) => ((ActionBase)a).Token)
                    .Callback((IActionItem a) =>
                    {
                        var act = (ActionBase)a;
                        act.SessionId = ret.Object.SessionId;
                        act.SetCompleted();
                        act.Token.SetCompletedSignal();
                        $"{act.SessionId}: {act}"._DLOG();
                    });
                ret.Setup(y => y.ExecuteAsync(It.Is<IActionItem>(f => f is SendOperation)))
                    .Returns((IActionItem a) => ((ActionBase)a).Token)
                    .Callback((IActionItem a) =>
                    {
                        var act = (SendOperation)a;
                        act.SessionId = ret.Object.SessionId;
                        var dht = available.FirstOrDefault(x => x.SerialSessionId == act.SessionId);
                        if (dht != null)
                        {
                            if (act.Data.SequenceEqual("00 E6 01 05 03 00".GetBytes()))
                            {
                                dht.LedState = 0x00;
                            }
                            else if (act.Data.SequenceEqual("00 E6 01 05 03 01".GetBytes()))
                            {
                                dht.LedState = 0xFF;
                            }
                            $"{act.SessionId}: {act} LedState={dht.LedState}"._DLOG();
                        }
                        act.SetCompleted();
                        act.Token.SetCompletedSignal();
                        $"{act.SessionId}: {act} LedState={dht?.LedState}"._DLOG();
                    });
                ret.Setup(y => y.ExecuteAsync(It.Is<IActionItem>(f => f is RequestOperation)))
                    .Returns((IActionItem a) => ((ActionBase)a).Token)
                    .Callback((IActionItem a) =>
                    {
                        var act = (RequestOperation)a;
                        act.SessionId = ret.Object.SessionId;
                        var dht = available.FirstOrDefault(x => x.ProgSessionId == act.SessionId);
                        act.SpecificResult.Payload = new byte[] { 0x00, dht.LedState };
                        act.SetCompleted();
                        act.Token.SetCompletedSignal();
                        $"{act.SessionId}: {act} LedState={dht.LedState}"._DLOG();
                    });
                ret.Setup(y => y.ExecuteAsync(It.Is<IActionItem>(f => f is NVRGetValueOperation)))
                    .Returns((IActionItem a) => ((ActionBase)a).Token)
                    .Callback((IActionItem a) =>
                    {
                        var act = (NVRGetValueOperation)a;
                        act.SessionId = ret.Object.SessionId;
                        var dht = available.FirstOrDefault(x => x.SerialSessionId == act.SessionId);
                        act.SpecificResult.NVRValue = BitConverter.GetBytes(dht.HomeId);
                        act.SetCompleted();
                        act.Token.SetCompletedSignal();
                        $"{act.SessionId}: {act}"._DLOG();
                    });
                ret.Setup(y => y.ExecuteAsync(It.Is<IActionItem>(f => f is MemoryGetIdOperation)))
                   .Returns((IActionItem a) => ((ActionBase)a).Token)
                   .Callback((IActionItem a) =>
                   {
                       var act = (MemoryGetIdOperation)a;
                       act.SessionId = ret.Object.SessionId;
                       var dht = available.FirstOrDefault(x => x.SerialSessionId == act.SessionId);
                       act.SpecificResult.HomeId = BitConverter.GetBytes(dht.HomeId);
                       act.SetCompleted();
                       act.Token.SetCompletedSignal();
                       $"{act.SessionId}: {act}"._DLOG();
                   });
                ret.Setup(y => y.ExecuteAsync(It.Is<IActionItem>(f => f is SerialApiGetInitDataOperation)))
                   .Returns((IActionItem a) => ((ActionBase)a).Token)
                   .Callback((IActionItem a) =>
                   {
                       var act = (SerialApiGetInitDataOperation)a;
                       act.SessionId = ret.Object.SessionId;
                       var dht = available.FirstOrDefault(x => x.SerialSessionId == act.SessionId);
                       if (dht != null && dht.Library != Libraries.NoLib)
                       {
                           act.SpecificResult.ChipType = dht.ChipType;
                           act.SetCompleted();
                       }
                       else
                       {
                           act.SetExpired();
                       }
                       act.Token.SetCompletedSignal();
                       $"{act.SessionId}: {act} SName={dht?.JLink?.ToString()}, ChipType={dht?.ChipType}"._DLOG();
                   });
                ret.Setup(y => y.ExecuteAsync(It.Is<IActionItem>(f => f is TestInterfaceSendDataOperation)))
                    .Returns((IActionItem a) => ((ActionBase)a).Token)
                    .Callback((IActionItem a) =>
                    {
                        var act = (TestInterfaceSendDataOperation)a;
                        act.SessionId = ret.Object.SessionId;
                        //if (act.TestInterfaceCmd.EqualsWithArray(new byte[] { 0x74, 0xFF }))
                        //{
                        //    //act.SpecificResult.ByteArray = new byte[] { ++ledCounter };
                        //}
                        act.SetCompleted();
                        act.Token.SetCompletedSignal();
                        $"{act.SessionId}: {act}"._DLOG();
                    });
                return ret.Object;
            });

            var basicFrameLayer = Mock.Of<IFrameLayer>((x => x.CreateClient(It.IsAny<ushort>()) == Mock.Of<IFrameClient>()));
            var basicTransportLayer = new Mock<ITransportLayer>();
            basicTransportLayer.Setup(x => x.CreateClient(It.IsAny<ushort>())).Returns((ushort s) =>
            {
                var ret = new Mock<ITransportClient>();
                ret.SetupProperty(y => y.SessionId);
                ret.SetupProperty(y => y.DataSource);
                ret.Setup(y => y.Connect()).Returns(() =>
                {
                    var serialCon = available.FirstOrDefault(z =>
                        z.JLink.ToString() == ret.Object.DataSource.SourceName ||
                        z.IpAddress?.ToString() == ret.Object.DataSource.SourceName ||
                        z.SAlpha.ToString() == ret.Object.DataSource.SourceName);
                    if (serialCon != null)
                    {
                        serialCon.SetSerialSessionId(ret.Object.SessionId);
                    }
                    var progCon = available.FirstOrDefault(z => z.PAlpha.ToString() == ret.Object.DataSource.SourceName);
                    if (progCon != null)
                    {
                        progCon.SetProgSessionId(ret.Object.SessionId);
                    }
                    $"{ret.Object.DataSource} connected with {ret.Object.SessionId}"._DLOG();
                    return CommunicationStatuses.Done;
                });
                //ret.Setup(y => y.Connect(It.IsAny<IDataSource>())).Returns((IDataSource ds) =>
                //{
                //    return ZWave.Enums.CommunicationStatuses.Done;
                //});
                return ret.Object;
            });

            var znifferFrameLayer = Mock.Of<IFrameLayer>((x => x.CreateClient(It.IsAny<ushort>()) == Mock.Of<IFrameClient>()));
            var znifferTransportLayer = Mock.Of<ITransportLayer>((x => x.CreateClient(It.IsAny<ushort>()) == Mock.Of<ITransportClient>()));

            _sessionLayer = sessionLayer.Object;
            _basicApp = new ZWave.BasicApplication.BasicApplicationLayer(
                         _sessionLayer,
                         basicFrameLayer,
                         basicTransportLayer.Object);

            _znifferApp = new ZWave.ZnifferApplication.ZnifferApplicationLayer(
                         _sessionLayer,
                         znifferFrameLayer,
                         znifferTransportLayer);

            _tcpApp = new ZWave.BasicApplication.BasicApplicationLayer(
                         _sessionLayer,
                         basicFrameLayer,
                         basicTransportLayer.Object);

            _alphaProgrammer = new Mock<IProgrammingProvider>();
            _alphaProgrammer.Setup(x => x.ProgramSample(It.IsAny<DeviceHostClient>(), It.IsAny<ZWaveRegions?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(
                (DeviceHostClient dhc, ZWaveRegions region, string filePath, bool isAddS2Keys, byte[] prk, byte[] puk) =>
            {
                var dht = available.FirstOrDefault(x => x.PAlpha.ToString() == dhc.Host.PName);
                dht.Library = Libraries.ControllerBridgeLib;
                if (puk != null)
                {
                    dht.HomeId = BitConverter.ToInt32(puk.Take(4).ToArray(), 0);
                }
                return 0;
            });
            _alphaProgrammer.Setup(x => x.HasFirmware(It.Is<DeviceHostClient>(f => available.Any(d => f.Host.PName == d.PAlpha.ToString())))).Returns(true);
            _alphaProgrammer.Setup(x => x.GetChipSeries(It.IsAny<DeviceHostClient>())).Returns((DeviceHostClient dhc) =>
            {
                string ret = null;
                var d = available.FirstOrDefault(dht => dht.JLink == null && dht.PAlpha.ToString() == dhc.Host.PName);
                if (d != null)
                {
                    ret = (d.ChipType == ChipTypes.ZW070x ? ChipSeries.ZW070x : d.ChipType == ChipTypes.ZW050x ? ChipSeries.ZW050x : ChipSeries.ZW030x).ToString();
                }

                dhc.Host.ChipSeries = ret?.ToString();
                return ret;
            });
            _alphaProgrammer.Setup(x => x.SetHomeId(It.IsAny<DeviceHostClient>(), It.IsAny<byte[]>())).Returns((DeviceHostClient dhc, byte[] homeId) =>
            {
                var dht = available.FirstOrDefault(x => x.PAlpha.ToString() == dhc.Host.PName);
                dht.HomeId = BitConverter.ToInt32(homeId.Take(4).ToArray(), 0); homeId.Take(4).ToArray();
                return 0;
            });

            _gammaProgrammer = new Mock<IProgrammingProvider>();
            _gammaProgrammer.Setup(x => x.ProgramSample(It.IsAny<DeviceHostClient>(), It.IsAny<ZWaveRegions?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<byte[]>(), It.IsAny<byte[]>())).Returns(
                (DeviceHostClient dhc, ZWaveRegions region, string filePath, bool isAddS2Keys, byte[] prk, byte[] puk) =>
            {
                var dht = available.FirstOrDefault(x => x.SNumber.ToString() == dhc.Host.JName);
                dht.Library = Libraries.ControllerBridgeLib;
                return 0;
            });
            _gammaProgrammer.Setup(x => x.ProgramSampleWithBootLoaderKeys(It.IsAny<DeviceHostClient>(), It.IsAny<ZWaveRegions?>(), It.IsAny<string>(), It.IsAny<string[]>())).Returns(
                (DeviceHostClient dhc, ZWaveRegions region, string filePath, string[] tokens) =>
                {
                    var dht = available.FirstOrDefault(x => x.SNumber.ToString() == dhc.Host.JName);
                    dht.Library = Libraries.ControllerBridgeLib;
                    return 0;
                });
            _gammaProgrammer.Setup(x => x.GetChipSeries(It.IsAny<DeviceHostClient>())).Returns(ChipSeries.ZGM130S_BRD4207A.ToString());

            _fileSystemManager = new Mock<IFileSystemManager>();

            _portNamesGetter = new Mock<IPortNamesGetter>();
            _portNamesGetter.Setup(x => x.GetPortNames()).Returns(() =>
            {
                var ret = available
                        .Where(d => d.JLink != null)
                        .Select(d => d.JLink.ToString())
                    .Concat(available
                        .Where(d => d.PAlpha != null)
                        .Select(d => d.PAlpha.ToString()))
                    .Concat(available
                        .Where(d => d.SAlpha != null)
                        .Select(d => d.SAlpha.ToString()))
                    .ToArray();
                $"GetPortNames() Count: {ret.Count()}"._DLOG();
                return ret;
            });

            _portNamesGetter.Setup(x => x.GetPortNames(It.IsAny<string>(), It.IsAny<string>())).Returns(() =>
            {
                //var ret = available
                //    .Where(d => d.PAlpha != null)
                //    .Select(d => d.PAlpha.ToString())
                //    .ToArray();
                var ret = new string[0];
                $"GetPortNames(string,string) Count: {ret.Count()}"._DLOG();
                return ret;
            });

            _portNamesGetter.Setup(x => x.GetBoardLinks(It.IsAny<IProgrammingProvider>())).Returns(() =>
            {
                var ret = available
                    .Where(d => d.JLink != null && d.SNumber != null)
                    .Select(d => new Tuple<string, string>(d.JLink.ToString(), d.SNumber.ToString()))
                    .ToArray();
                $"GetBoardLinks(IProgrammingProvider) Count: {ret.Count()}"._DLOG();
                return ret;
            });

            _udpJLinkDiscoveryService = new Mock<IUdpDevicesDiscoveryService<JlinkDeviceInfo>>();
            _udpJLinkDiscoveryService.Setup(x => x.DiscoverTcpDevices()).Returns(() =>
            {
                var ret = available
                    .Where(d => d.JLink != null && d.SNumber != null && d.IpAddress != null)
                    .Select(d => new JlinkDeviceInfo(d.SNumber.ToString(), null, IPAddress.Parse(d.IpAddress), null, true))
                    .ToArray();
                $"DiscoverTcpDevices() Count: {ret.Count()}"._DLOG();
                return ret;
            });
        }

        private static readonly Dictionary<string, IEnumerable<DeviceHostTest>> variations = new Dictionary<string, IEnumerable<DeviceHostTest>>();

        [SetUp]
        public void SetUp()
        {
            ResetSessionIdCounter();
            variations.Clear();
            variations.Add("all_var1", new DeviceHostTest[] {
                new DeviceHostTest(ChipTypes.ZW070x, Libraries.NoLib, JLink.JLCOM1, SNumber.SN1, IPs.IP1, null),
                new DeviceHostTest(ChipTypes.ZW070x, Libraries.NoLib, JLink.JLCOM2, SNumber.SN2, IPs.IP2, null),
                new DeviceHostTest(ChipTypes.ZW070x, Libraries.NoLib, JLink.JLCOM3, SNumber.SN3, null, PAlpha.PCOM2),
                new DeviceHostTest(ChipTypes.ZW070x, Libraries.NoLib, JLink.JLCOM4, SNumber.SN4, null, null),
                new DeviceHostTest(ChipTypes.ZW070x, Libraries.ControllerBridgeLib, JLink.JLCOM5, null, null,null),
                new DeviceHostTest(ChipTypes.ZW050x, Libraries.NoLib, SAlpha.ACOM1, PAlpha.PCOM3),
                new DeviceHostTest(ChipTypes.ZW050x, Libraries.ControllerBridgeLib, SAlpha.ACOM2, null),
                new DeviceHostTest(ChipTypes.ZW050x, Libraries.NoLib, null, PAlpha.PCOM4),
                new DeviceHostTest(ChipTypes.ZW030x, Libraries.NoLib, SAlpha.ACOM4, PAlpha.PCOM5),
                new DeviceHostTest(ChipTypes.ZW030x, Libraries.ControllerBridgeLib, SAlpha.ACOM5, null),
                new DeviceHostTest(ChipTypes.ZW030x, Libraries.NoLib, null, PAlpha.PCOM6)});

            variations.Add("only700_var1", new DeviceHostTest[] {
                new DeviceHostTest(ChipTypes.ZW070x, Libraries.NoLib, JLink.JLCOM1, SNumber.SN1, IPs.IP1, null),
                new DeviceHostTest(ChipTypes.ZW070x, Libraries.NoLib, JLink.JLCOM2, SNumber.SN2, IPs.IP2, null),
                new DeviceHostTest(ChipTypes.ZW070x, Libraries.NoLib, JLink.JLCOM3, SNumber.SN3, null, PAlpha.PCOM2),
                new DeviceHostTest(ChipTypes.ZW070x, Libraries.NoLib, JLink.JLCOM4, SNumber.SN4, null, null),
                new DeviceHostTest(ChipTypes.ZW070x, Libraries.ControllerBridgeLib, JLink.JLCOM5, null, null,null)});

            variations.Add("only700_var2", new DeviceHostTest[] {
                new DeviceHostTest(ChipTypes.ZW070x, Libraries.NoLib, JLink.JLCOM3, SNumber.SN3, null, PAlpha.PCOM2),
                new DeviceHostTest(ChipTypes.ZW070x, Libraries.NoLib, JLink.JLCOM4, SNumber.SN4, null, null)});

            variations.Add("only700_var3", new DeviceHostTest[] {
                new DeviceHostTest(ChipTypes.ZW070x, Libraries.NoLib, JLink.JLCOM1, SNumber.SN1, IPs.IP1, null),
                new DeviceHostTest(ChipTypes.ZW070x, Libraries.NoLib, JLink.JLCOM2, SNumber.SN2, IPs.IP2, null)});

            variations.Add("single700_var1", new DeviceHostTest[] {
                new DeviceHostTest(ChipTypes.ZW070x, Libraries.NoLib, JLink.JLCOM1, SNumber.SN1, IPs.IP1, null)});

            variations.Add("single700_var2", new DeviceHostTest[] {
                new DeviceHostTest(ChipTypes.ZW070x, Libraries.NoLib, JLink.JLCOM2, SNumber.SN2, IPs.IP2, null)});

            variations.Add("single700_var3", new DeviceHostTest[] {
                new DeviceHostTest(ChipTypes.ZW070x, Libraries.NoLib, JLink.JLCOM3, SNumber.SN3, null, PAlpha.PCOM2)});

            variations.Add("single700_var4", new DeviceHostTest[] {
                new DeviceHostTest(ChipTypes.ZW070x, Libraries.NoLib, JLink.JLCOM4, SNumber.SN4, null, null)});

            variations.Add("single700_var5", new DeviceHostTest[] {
                new DeviceHostTest(ChipTypes.ZW070x, Libraries.ControllerBridgeLib, JLink.JLCOM5, null, null,null)});

            variations.Add("only500_var1", new DeviceHostTest[] {
                new DeviceHostTest(ChipTypes.ZW050x, Libraries.NoLib, SAlpha.ACOM1, PAlpha.PCOM3),
                new DeviceHostTest(ChipTypes.ZW050x, Libraries.ControllerBridgeLib, SAlpha.ACOM2, null),
                new DeviceHostTest(ChipTypes.ZW050x, Libraries.NoLib, null, PAlpha.PCOM4)});

            variations.Add("single500_var1", new DeviceHostTest[] {
                new DeviceHostTest(ChipTypes.ZW050x, Libraries.NoLib, SAlpha.ACOM1, PAlpha.PCOM3)});

            variations.Add("single500_var2", new DeviceHostTest[] {
                new DeviceHostTest(ChipTypes.ZW050x, Libraries.ControllerBridgeLib, SAlpha.ACOM2, null)});

            variations.Add("single500_var3", new DeviceHostTest[] {
                new DeviceHostTest(ChipTypes.ZW050x, Libraries.NoLib, null, PAlpha.PCOM4)});

            variations.Add("only300_var1", new DeviceHostTest[] {
                new DeviceHostTest(ChipTypes.ZW030x, Libraries.NoLib, SAlpha.ACOM4, PAlpha.PCOM5),
                new DeviceHostTest(ChipTypes.ZW030x, Libraries.ControllerBridgeLib, SAlpha.ACOM5, null),
                new DeviceHostTest(ChipTypes.ZW030x, Libraries.NoLib, null, PAlpha.PCOM6)});

            variations.Add("single300_var1", new DeviceHostTest[] {
                new DeviceHostTest(ChipTypes.ZW030x, Libraries.NoLib, SAlpha.ACOM4, PAlpha.PCOM5)});

            variations.Add("single300_var2", new DeviceHostTest[] {
                new DeviceHostTest(ChipTypes.ZW030x, Libraries.ControllerBridgeLib, SAlpha.ACOM5, null)});

            variations.Add("single300_var3", new DeviceHostTest[] {
                new DeviceHostTest(ChipTypes.ZW030x, Libraries.NoLib, null, PAlpha.PCOM6)});
        }

        //[TestCase("all_var1")]
        //[TestCase("only700_var1")]
        [TestCase("only700_var2")]
        //[TestCase("only700_var3")]
        //[TestCase("single700_var1")]
        //[TestCase("single700_var2")]
        [TestCase("single700_var3")]
        [TestCase("single700_var4")]
        [TestCase("single700_var5")]
        [TestCase("only500_var1")]
        [TestCase("single500_var1")]
        [TestCase("single500_var2")]
        [TestCase("single500_var3")]
        [TestCase("only300_var1")]
        [TestCase("single300_var1")]
        [TestCase("single300_var2")]
        [TestCase("single300_var3")]
        public void DetectConnectedDevices_Default_ProfileContainsAll(string testVariationKey)
        {
            //Arr
            var available = variations[testVariationKey].ToArray();
            SetUpEnvironment(available);

            var profile = new Profile() { ZName = "ZCOM1" };
            var deviceHostsDetector = new DeviceHostsDetector(
                new CmdLineSettings(),
                _basicApp, _znifferApp, _tcpApp,
                _portNamesGetter.Object, _alphaProgrammer.Object, _gammaProgrammer.Object, _fileSystemManager.Object,
                _udpJLinkDiscoveryService.Object);

            //Act
            deviceHostsDetector.DetectConnectedDevices(profile, null, null);

            //Assert
            _portNamesGetter.Verify(x => x.GetBoardLinks(It.IsAny<IProgrammingProvider>()), Times.AtLeastOnce());
            NUnit.Framework.Assert.AreEqual(available.Count(), profile.DeviceHosts.Count);
            NUnit.Framework.Assert.IsTrue(available.All(x => profile.DeviceHosts.Any(y => y.JName == null || y.JName == x.SNumber.ToString())));
            NUnit.Framework.Assert.IsTrue(available.All(x => profile.DeviceHosts.Any(y => y.SName == null || y.SName == x.IpAddress || y.SName == x.JLink.ToString() || y.SName == x.SAlpha.ToString())));
            NUnit.Framework.Assert.IsTrue(available.All(x => profile.DeviceHosts.Any(y => y.PName == null || y.PName == x.PAlpha.ToString())));
        }

        //[TestCase("all_var1")]
        //[TestCase("only700_var1")]
        [TestCase("only700_var2")]
        [TestCase("only700_var3")]
        [TestCase("single700_var1")]
        [TestCase("single700_var2")]
        [TestCase("single700_var3")]
        [TestCase("single700_var4")]
        [TestCase("single700_var5")]
        [TestCase("only500_var1")]
        [TestCase("single500_var1")]
        [TestCase("single500_var2")]
        [TestCase("single500_var3")]
        [TestCase("only300_var1")]
        [TestCase("single300_var1")]
        [TestCase("single300_var2")]
        [TestCase("single300_var3")]
        public void DetectConnectedDevices_FilteredBySNumber_ContainsOnlyFilteredSNumber(string testVariationKey)
        {
            //Arr.
            var available = variations[testVariationKey].ToArray();
            SetUpEnvironment(available);

            var included = available.Where(d => d.SNumber != null).Take(2).Select(d => d.SNumber.ToString()).ToArray();
            var excluded = available.Where(d => d.SNumber != null).Skip(2).Select(d => d.SNumber.ToString()).ToArray();

            var profile = new Profile() { ZName = "ZCOM1" };
            var deviceHostsDetector = new DeviceHostsDetector(
                new CmdLineSettings(),
                _basicApp, _znifferApp, _tcpApp,
               _portNamesGetter.Object, _alphaProgrammer.Object, _gammaProgrammer.Object, _fileSystemManager.Object,
               _udpJLinkDiscoveryService.Object);

            //Act
            deviceHostsDetector.DetectConnectedDevices(profile, null, included);

            //Assert
            _portNamesGetter.Verify(x => x.GetBoardLinks(It.IsAny<IProgrammingProvider>()), Times.AtLeastOnce());
            NUnit.Framework.Assert.IsTrue(included.All(x => profile.DeviceHosts.Any(y => y.JName == x)));
            NUnit.Framework.Assert.IsFalse(excluded.Any(x => profile.DeviceHosts.Any(y => y.JName == x)));
        }

        //[Test] not completed
        public void DetectConnectedDevices_RouterInfo_MeshRouterTopologyBuilder(string testVariationKey)
        {
            var available = variations[testVariationKey];
            SetUpEnvironment(available);

            var exp = "01708180008";
            var networkRack = new MeshRouterDeviceInfo { SerialNo = exp, IPAddress = IPAddress.Parse("192.168.1.111"), Port = 4950 };
            var jLinkFilterList = available.Take(2).Select(d => d.SNumber.ToString()).ToArray();

            var profile = new Profile();
            profile.DeviceHosts = new System.Collections.ObjectModel.ObservableCollection<DeviceHost>();
            var deviceHostsDetector = new DeviceHostsDetector(
                new CmdLineSettings(),
               _basicApp, _znifferApp, _tcpApp,
               _portNamesGetter.Object, _alphaProgrammer.Object, _gammaProgrammer.Object, _fileSystemManager.Object,
               _udpJLinkDiscoveryService.Object);

            //mock meshRouterController.Request..
            //mock meshRouterTopologyBuilder and check RunDetect()

            //Act
            var res = deviceHostsDetector.DetectConnectedDevices(profile, null, jLinkFilterList, networkRack); //or DetectMeshRoutersor 

            //Assert
            NUnit.Framework.Assert.NotNull(res);
            //verify:
            //_alphaProgrammer.Verify(x => x.HasFirmware(It.IsAny<DeviceHostClient>()), Times.AtLeastOnce());
            //_portNamesGetter.Verify(x => x.GetBoardLinks(It.IsAny<IProgrammingProvider>()), Times.AtLeastOnce());
            //NUnit.Framework.Assert.IsTrue(ipGammaPorts.All(x => profile.DeviceHosts.Any(y => y.JName == x.SerialNo && y.SName == x.IPEndPoint.Address.ToString())));
            //NUnit.Framework.Assert.IsFalse(profile.DeviceHosts.Any(y => y.JName == notExpected));
        }

        [TestCase("all_var1")]
        [TestCase("only700_var1")]
        [TestCase("only700_var2")]
        [TestCase("only700_var3")]
        [TestCase("single700_var1")]
        [TestCase("single700_var2")]
        [TestCase("single700_var3")]
        [TestCase("single700_var4")]
        [TestCase("single700_var5")]
        [TestCase("only500_var1")]
        [TestCase("single500_var1")]
        [TestCase("single500_var2")]
        [TestCase("single500_var3")]
        [TestCase("only300_var1")]
        [TestCase("single300_var1")]
        [TestCase("single300_var2")]
        [TestCase("single300_var3")]
        public void DetectPassiveConnectedDevices_Default_ProfileContainsOnlyExisting(string testVariationKey)
        {
            //Arr
            var available = variations[testVariationKey].Where(x => true).ToArray();
            SetUpEnvironment(available);

            var profile = new Profile() { ZName = "ZCOM1" };
            var deviceHostsDetector = new DeviceHostsDetector(
                new CmdLineSettings(),
                _basicApp, _znifferApp, _tcpApp,
                _portNamesGetter.Object, _alphaProgrammer.Object, _gammaProgrammer.Object, _fileSystemManager.Object,
                _udpJLinkDiscoveryService.Object);

            //Act
            deviceHostsDetector.RunPassiveDetect(profile);

            //Assert
            _portNamesGetter.Verify(x => x.GetBoardLinks(It.IsAny<IProgrammingProvider>()), Times.AtLeastOnce());
            NUnit.Framework.Assert.AreEqual(available.Count(x => x.Library != Libraries.NoLib), profile.DeviceHosts.Count);
            NUnit.Framework.Assert.IsTrue(available.All(x => x.Library == Libraries.NoLib || profile.DeviceHosts.Any(y => y.JName == null || y.JName == x.SNumber.ToString())));
            NUnit.Framework.Assert.IsTrue(available.All(x => x.Library == Libraries.NoLib || profile.DeviceHosts.Any(y => y.SName == null || y.SName == x.JLink.ToString() || y.SName == x.SAlpha.ToString())));
            NUnit.Framework.Assert.IsTrue(available.All(x => x.Library == Libraries.NoLib || profile.DeviceHosts.Any(y => y.PName == null || y.PName == x.PAlpha.ToString())));
        }
    }
}
