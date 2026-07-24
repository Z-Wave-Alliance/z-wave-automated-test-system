// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Linq;
using System.Threading;
using NUnit.Framework;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Enums;
using ZWave.Layers;
using ZWave.Layers.Session;
using ZWave.Layers.Transport;
using ZWave.ZipApplication;
using ZWave.ZipApplication.Devices;
using ZWave.ZipApplication.Operations;

namespace ZipApplicationIntegrationTests
{
    [TestFixture]
    public class AddNodeSecure : ZipTestsBase
    {
        int POW = 10;
        [SetUp]
        public void SetUp()
        {
            BasicApplicationLayer bal = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            ZipApplicationLayer zal = new ZipApplicationLayer(new SessionLayer(), new ZipFrameLayer(), new UdpClientTransportLayer());
            using (Controller ctrl = bal.CreateController())
            {
                using (EndDevice slave = bal.CreateEndDevice())
                {
                    using (ZipController zip = zal.CreateZipController())
                    {
                        ctrl.Connect(new SerialPortDataSource(COM1, BaudRates.Rate_115200));
                        slave.Connect(new SerialPortDataSource(COM2, BaudRates.Rate_115200));
                        zip.Connect(new SocketDataSource(ZIP_ADDR, ZIP_UDP_PORT));
                        VersionResult res = ctrl.GetVersion();
                        Assert.IsTrue(res.State == ActionStates.Completed);
                        Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || res.Library == Libraries.InstallerLib || res.Library == Libraries.ControllerPortableLib || res.Library == Libraries.ControllerBridgeLib);
                        res = slave.GetVersion();
                        Assert.IsTrue(res.State == ActionStates.Completed);
                        Assert.IsTrue(res.Library == Libraries.EndDeviceLegacyLib || res.Library == Libraries.EndDeviceLib || res.Library == Libraries.EndDeviceLegacyRoutingLib);
                        GetIdResult gres = zip.GetId();
                        Assert.AreEqual(ActionStates.Completed, gres.State);

                        ActionResult ares = ctrl.SetDefault();
                        Assert.IsTrue(ares.State == ActionStates.Completed);
                        ares = slave.SetDefault();
                        Assert.IsTrue(ares.State == ActionStates.Completed);
                        ares = zip.SetDefault(10000);
                        Assert.IsTrue(ares.State == ActionStates.Completed);

                        slave.ApplicationNodeInformation(true, 0x11, 00, new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_SECURITY.ID });
                        ctrl.ApplicationNodeInformation(true, 0x02, 0x01, new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_SECURITY.ID });
                    }
                }
            }
        }

        [Test]
        public void TestSync()
        {
            BasicApplicationLayer bal = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            ZipApplicationLayer zal = new ZipApplicationLayer(new SessionLayer(), new ZipFrameLayer(), new UdpClientTransportLayer());
            using (Controller ctrl = bal.CreateController())
            {
                using (EndDevice slave = bal.CreateEndDevice())
                {
                    using (ZipController zip = zal.CreateZipController())
                    {
                        ctrl.Connect(new SerialPortDataSource(COM1, BaudRates.Rate_115200));
                        slave.Connect(new SerialPortDataSource(COM2, BaudRates.Rate_115200));
                        zip.Connect(new SocketDataSource(ZIP_ADDR, ZIP_UDP_PORT));

                        SecurityManager sm1 = CreateSecurityManager(ctrl);
                        ctrl.SessionClient.AddSubstituteManager(sm1);
                        ctrl.SessionClient.ExecuteAsync(sm1.CreateSecurityReportTask());

                        SecurityManager sm2 = CreateSecurityManager(slave);
                        slave.SessionClient.AddSubstituteManager(sm2);
                        slave.SessionClient.ExecuteAsync(sm2.CreateSecurityReportTask());

                        for (int i = 0; i < 1; i++)
                        {
                            ActionToken addToken = zip.AddNodeToNetwork(Modes.NodeAny, TXO, 10000, null);
                            ActionToken learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, 10000, null);
                            Assert.IsTrue(addToken.WaitCompletedSignal());
                            Assert.IsTrue(learnToken.WaitCompletedSignal());

                            ActionToken removeToken = zip.RemoveNodeFromNetwork(Modes.NodeAny, 10000, null);
                            learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, 10000, null);
                            Assert.IsTrue(addToken.WaitCompletedSignal());
                            Assert.IsTrue(learnToken.WaitCompletedSignal());

                            addToken = zip.AddNodeToNetwork(Modes.NodeAny, TXO, 10000, null);
                            learnToken = ctrl.SetLearnMode(LearnModes.LearnModeClassic, 10000, null);
                            Assert.IsTrue(addToken.WaitCompletedSignal());
                            Assert.IsTrue(learnToken.WaitCompletedSignal());

                            removeToken = zip.RemoveNodeFromNetwork(Modes.NodeAny, 10000, null);
                            learnToken = ctrl.SetLearnMode(LearnModes.LearnModeClassic, 10000, null);
                            Assert.IsTrue(addToken.WaitCompletedSignal());
                            Assert.IsTrue(learnToken.WaitCompletedSignal());

                            Thread.Sleep(20000);
                        }
                    }
                }
            }
        }

        [Test]
        public void TestSlaveAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            ZipApplicationLayer zal = new ZipApplicationLayer(new SessionLayer(), new ZipFrameLayer(), new UdpClientTransportLayer());
            using (ZipController zip = zal.CreateZipController())
            {
                using (EndDevice slave = al.CreateEndDevice())
                {
                    int errors = 0;
                    zip.Connect(new SocketDataSource(ZIP_ADDR, ZIP_UDP_PORT));
                    slave.Connect(new SerialPortDataSource(COM2, BaudRates.Rate_115200));

                    SecurityManager sm2 = CreateSecurityManager(slave);
                    slave.SessionClient.AddSubstituteManager(sm2);
                    slave.SessionClient.ExecuteAsync(sm2.CreateSecurityReportTask());

                    ActionToken addToken = zip.AddNodeToNetwork(Modes.NodeAny, TXO, 10000, null);
                    ActionToken learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, 10000, null);

                    if (!addToken.WaitCompletedSignal())
                    {
                        errors++;
                    }
                    if (!learnToken.WaitCompletedSignal())
                    {
                        errors++;
                    }

                    MemoryGetIdResult memres = slave.MemoryGetId();
                    Assert.AreEqual(2, memres.Node.Id);

                    Assert.AreEqual(0, errors);
                }
            }
        }

        [Test]
        public void TestControllerAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            ZipApplicationLayer zal = new ZipApplicationLayer(new SessionLayer(), new ZipFrameLayer(), new UdpClientTransportLayer());
            using (ZipController zip = zal.CreateZipController())
            {
                using (Controller slave = al.CreateController())
                {
                    int errors = 0;
                    zip.Connect(new SocketDataSource(ZIP_ADDR, ZIP_UDP_PORT));
                    slave.Connect(new SerialPortDataSource(COM1, BaudRates.Rate_115200));

                    SecurityManager sm2 = CreateSecurityManager(slave);
                    slave.SessionClient.AddSubstituteManager(sm2);
                    slave.SessionClient.ExecuteAsync(sm2.CreateSecurityReportTask());

                    ActionToken addToken = zip.AddNodeToNetwork(Modes.NodeAny, TXO, 10000, null);
                    ActionToken learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, 10000, null);

                    if (!addToken.WaitCompletedSignal())
                    {
                        errors++;
                    }
                    if (!learnToken.WaitCompletedSignal())
                    {
                        errors++;
                    }

                    MemoryGetIdResult memres = slave.MemoryGetId();
                    Assert.AreEqual(2, memres.Node.Id);

                    Assert.AreEqual(0, errors);
                }
            }
        }

        [Test]
        public void TestSlaveManyAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            ZipApplicationLayer zal = new ZipApplicationLayer(new SessionLayer(), new ZipFrameLayer(), new UdpClientTransportLayer());
            using (ZipController zip = zal.CreateZipController())
            {
                using (EndDevice slave = al.CreateEndDevice())
                {
                    int errors = 0;
                    zip.Connect(new SocketDataSource(ZIP_ADDR, ZIP_UDP_PORT));
                    slave.Connect(new SerialPortDataSource(COM2, BaudRates.Rate_115200));

                    SecurityManager sm2 = CreateSecurityManager(slave);
                    slave.SessionClient.AddSubstituteManager(sm2);
                    slave.SessionClient.ExecuteAsync(sm2.CreateSecurityReportTask());
                    ActionToken addRemoveToken;
                    ActionToken learnToken;
                    MemoryGetIdResult memres;
                    for (int i = 0; i < POW; i++)
                    {
                        addRemoveToken = zip.AddNodeToNetwork(Modes.NodeAny, TXO, 10000, null);
                        learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, 10000, null);

                        if (!addRemoveToken.WaitCompletedSignal())
                        {
                            errors++;
                        }
                        if (!learnToken.WaitCompletedSignal())
                        {
                            errors++;
                        }

                        memres = slave.MemoryGetId();
                        int newNodeId = (i + 2);
                        if (newNodeId > 232)
                            newNodeId -= 231;
                        Assert.AreEqual(newNodeId, memres.Node.Id);


                        addRemoveToken = zip.RemoveNodeFromNetwork(Modes.NodeAny, 10000, null);

                        learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, 10000, null);

                        if (!addRemoveToken.WaitCompletedSignal())
                        {
                            errors++;
                        }
                        if (!learnToken.WaitCompletedSignal())
                        {
                            errors++;
                        }

                        memres = slave.MemoryGetId();
                        Assert.AreEqual(0, memres.Node.Id);


                        Assert.AreEqual(0, errors);
                    }

                }
            }
        }

        [Test]
        public void TestControllerManyAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            ZipApplicationLayer zal = new ZipApplicationLayer(new SessionLayer(), new ZipFrameLayer(), new UdpClientTransportLayer());
            using (ZipController zip = zal.CreateZipController())
            {
                using (Controller slave = al.CreateController())
                {
                    int errors = 0;
                    zip.Connect(new SocketDataSource(ZIP_ADDR, ZIP_UDP_PORT));
                    slave.Connect(new SerialPortDataSource(COM1, BaudRates.Rate_115200));

                    SecurityManager sm2 = CreateSecurityManager(slave);
                    slave.SessionClient.AddSubstituteManager(sm2);
                    slave.SessionClient.ExecuteAsync(sm2.CreateSecurityReportTask());
                    ActionToken addRemoveToken;
                    ActionToken learnToken;
                    MemoryGetIdResult memres;
                    for (int i = 0; i < POW; i++)
                    {
                        addRemoveToken = zip.AddNodeToNetwork(Modes.NodeAny, TXO, 10000, null);
                        learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, 10000, null);

                        if (!addRemoveToken.WaitCompletedSignal())
                        {
                            errors++;
                        }
                        if (!learnToken.WaitCompletedSignal())
                        {
                            errors++;
                        }

                        memres = slave.MemoryGetId();
                        int newNodeId = i + 2;
                        if (newNodeId > 232)
                            newNodeId -= 231;
                        Assert.AreEqual(newNodeId, memres.Node.Id);


                        addRemoveToken = zip.RemoveNodeFromNetwork(Modes.NodeAny, 10000, null);
                        learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, 10000, null);

                        if (!addRemoveToken.WaitCompletedSignal())
                        {
                            errors++;
                        }
                        if (!learnToken.WaitCompletedSignal())
                        {
                            errors++;
                        }

                        memres = slave.MemoryGetId();
                        Assert.AreEqual(1, memres.Node.Id);


                        Assert.AreEqual(0, errors);
                    }

                }
            }
        }
    }
}
