// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Net;
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
    public class AddNode : ZipTestsBase
    {
        int POW = 10;
        [SetUp]
        public void SetUp()
        {
            BasicApplicationLayer bal = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            ZipApplicationLayer zal = new ZipApplicationLayer(new SessionLayer(), new ZipFrameLayer(), new UdpClientTransportLayer());
            using (Controller ctrl = bal.CreateController())
            {
                using (Slave slave = bal.CreateSlave())
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
                        Assert.IsTrue(res.Library == Libraries.SlaveEnhancedLib || res.Library == Libraries.SlaveLib || res.Library == Libraries.SlaveRoutingLib);
                        GetIdResult gres = zip.GetId();
                        Assert.AreEqual(ActionStates.Completed, gres.State);

                        ActionResult ares = ctrl.SetDefault();
                        Assert.IsTrue(ares.State == ActionStates.Completed);
                        ares = slave.SetDefault();
                        Assert.IsTrue(ares.State == ActionStates.Completed);
                        ares = zip.SetDefault(10000);
                        Assert.IsTrue(ares.State == ActionStates.Completed);

                        slave.ApplicationNodeInformation(true, 0x11, 00, new byte[] { COMMAND_CLASS_BASIC.ID });
                        ctrl.ApplicationNodeInformation(true, 0x02, 0x01, new byte[] { COMMAND_CLASS_BASIC.ID });
                    }
                }
            }
        }

        [Test]
        public void TestAsync()
        {
            BasicApplicationLayer bal = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            ZipApplicationLayer zal = new ZipApplicationLayer(new SessionLayer(), new ZipFrameLayer(), new UdpClientTransportLayer());
            using (Controller ctrl = bal.CreateController())
            {
                using (Slave slave = bal.CreateSlave())
                {
                    using (ZipController zip = zal.CreateZipController())
                    {
                        ctrl.Connect(new SerialPortDataSource(COM1, BaudRates.Rate_115200));
                        slave.Connect(new SerialPortDataSource(COM2, BaudRates.Rate_115200));
                        zip.Connect(new SocketDataSource(ZIP_ADDR, ZIP_UDP_PORT));
                        for (int j = 0; j < 20; j++)
                        {
                            ActionToken addToken = zip.AddNodeToNetwork(Modes.NodeAny, TXO, 10000, null);
                            ActionToken learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, 10000, null);
                            var addRes = (AddNodeResult)addToken.WaitCompletedSignal();
                            var learnRes = (ZWave.BasicApplication.Operations.SetLearnModeResult)learnToken.WaitCompletedSignal();
                            Assert.IsTrue(addRes);
                            Assert.IsTrue(learnRes);
                            Assert.IsTrue(addRes.NodeId == learnRes.NodeId);
                            var ipAddress = zip.GetIpAddress(addRes.NodeId);
                            for (int i = 0; i < 5; i++)
                            {
                                using (ZipDevice zipSlave = zal.CreateZipDevice())
                                {
                                    zipSlave.Connect(new SocketDataSource(ipAddress.IpAddress.ToString(), ZIP_UDP_PORT));
                                    zipSlave.SendData(null, new COMMAND_CLASS_BASIC.BASIC_GET());
                                }
                            }

                            ActionToken removeToken = zip.RemoveNodeFromNetwork(Modes.NodeAny, 10000, null);
                            learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, 10000, null);
                            Assert.IsTrue(addToken.WaitCompletedSignal());
                            Assert.IsTrue(learnToken.WaitCompletedSignal());
                        }
                    }
                }
            }
        }

        [Test]
        public void TestManyAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            ZipApplicationLayer zal = new ZipApplicationLayer(new SessionLayer(), new ZipFrameLayer(), new UdpClientTransportLayer());
            using (ZipController zip = zal.CreateZipController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    int errors = 0;
                    zip.Connect(new SocketDataSource(ZIP_ADDR, ZIP_UDP_PORT));
                    slave.Connect(new SerialPortDataSource(COM2, BaudRates.Rate_115200));

                    ActionToken addRemoveToken;
                    ActionToken learnToken;
                    MemoryGetIdResult memres;
                    for (int i = 0; i < POW; i++)
                    {
                        addRemoveToken = zip.AddNodeToNetwork(Modes.NodeAny, TXO, 10000, null);
                        learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, 10000, null);

                        if (addRemoveToken.WaitCompletedSignal())
                        {
                            errors++;
                        }
                        if (learnToken.WaitCompletedSignal())
                        {
                            errors++;
                        }

                        memres = slave.MemoryGetId();
                        int newNodeId = (i + 2);
                        if (newNodeId > 232)
                            newNodeId -= 231;
                        Assert.AreEqual(newNodeId, memres.NodeId);

                        addRemoveToken = zip.RemoveNodeFromNetwork(Modes.NodeAny, 10000, null);
                        learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, 10000, null);

                        if (addRemoveToken.WaitCompletedSignal())
                        {
                            errors++;
                        }
                        if (learnToken.WaitCompletedSignal())
                        {
                            errors++;
                        }

                        memres = slave.MemoryGetId();
                        Assert.AreEqual(0, memres.NodeId);


                        Assert.AreEqual(0, errors);
                    }
                }
            }
        }

        //[Test]
        public void TestExpiredAsync()
        {
            ZipApplicationLayer zal = new ZipApplicationLayer(new SessionLayer(), new ZipFrameLayer(), new UdpClientTransportLayer());
            using (ZipController zip = zal.CreateZipController())
            {
                zip.Connect(new SocketDataSource(ZIP_ADDR, ZIP_UDP_PORT));
                ActionToken addToken = zip.AddNodeToNetwork(Modes.NodeAny, TXO, 10000, null);
                Assert.IsTrue(addToken.WaitCompletedSignal().State == ActionStates.Expired);
            }
        }

        //[Test]
        public void TestExpiredSync()
        {
            ZipApplicationLayer zal = new ZipApplicationLayer(new SessionLayer(), new ZipFrameLayer(), new UdpClientTransportLayer());
            using (ZipController zip = zal.CreateZipController())
            {
                zip.Connect(new SocketDataSource(ZIP_ADDR, ZIP_UDP_PORT));
                ActionResult res = zip.AddNodeToNetwork(Modes.NodeAny, TXO, 10000);
                Assert.IsTrue(res.State == ActionStates.Expired);
            }
        }

        //[Test]
        public void TestStopAsync()
        {
            ZipApplicationLayer zal = new ZipApplicationLayer(new SessionLayer(), new ZipFrameLayer(), new UdpClientTransportLayer());
            using (ZipController zip = zal.CreateZipController())
            {
                zip.Connect(new SocketDataSource(ZIP_ADDR, ZIP_UDP_PORT));
                ActionToken addToken = zip.AddNodeToNetwork(Modes.NodeAny, TXO, 10000, null);
                Thread.Sleep(2000);
                ActionResult res = zip.AddNodeToNetwork(Modes.NodeStop, TXO, 0);
                Assert.IsTrue(res == null || res.State == ActionStates.Completed);
                Assert.IsTrue(addToken.WaitCompletedSignal().State == ActionStates.Cancelled);
            }
        }

        //[Test]
        public void TestStopSync()
        {
            ZipApplicationLayer zal = new ZipApplicationLayer(new SessionLayer(), new ZipFrameLayer(), new UdpClientTransportLayer());
            using (ZipController zip = zal.CreateZipController())
            {
                zip.Connect(new SocketDataSource(ZIP_ADDR, ZIP_UDP_PORT));
                ActionResult res = zip.AddNodeToNetwork(Modes.NodeStop, TXO, 0);
                Assert.IsTrue(res == null || res.State == ActionStates.Completed);
            }
        }
    }
}
