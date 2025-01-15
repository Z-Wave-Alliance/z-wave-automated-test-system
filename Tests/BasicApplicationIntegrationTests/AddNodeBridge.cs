// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using NUnit.Framework;
using ZWave.Layers.Transport;
using ZWave.Layers;
using ZWave.Enums;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Operations;
using ZWave.Layers.Session;
using ZWave;
using System.Threading;

namespace BasicApplicationIntegrationTests
{
    [TestFixture]
    public class AddNodeBridge : BasicTestsBase
    {
        int POW = 10;

        [SetUp]
        public void SetUp()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (BridgeController bridge = al.CreateBridgeController())
                {
                    using (Slave slave = al.CreateSlave())
                    {
                        ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                        bridge.Connect(new SerialPortDataSource(COM_CTRL_BRIDGE_1, BaudRates.Rate_115200));
                        slave.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));
                        VersionResult res = ctrl.GetVersion();
                        Assert.IsTrue(res.State == ActionStates.Completed);
                        Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || res.Library == Libraries.InstallerLib || res.Library == Libraries.ControllerPortableLib || res.Library == Libraries.ControllerBridgeLib);
                        res = bridge.GetVersion();
                        Assert.IsTrue(res.State == ActionStates.Completed);
                        Assert.IsTrue(res.Library == Libraries.ControllerBridgeLib);
                        res = slave.GetVersion();
                        Assert.IsTrue(res.State == ActionStates.Completed);
                        Assert.IsTrue(res.Library == Libraries.SlaveEnhancedLib || res.Library == Libraries.SlaveLib || res.Library == Libraries.SlaveRoutingLib);
                        ActionResult ares = ctrl.SetDefault();
                        Assert.IsTrue(ares.State == ActionStates.Completed);
                        ares = bridge.SetDefault();
                        Assert.IsTrue(ares.State == ActionStates.Completed);
                        ares = slave.SetDefault();
                        Assert.IsTrue(ares.State == ActionStates.Completed);
                    }
                }
            }
        }

        [Test]
        public void TestSlaveAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (BridgeController ctrl = al.CreateBridgeController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_BRIDGE_1, BaudRates.Rate_115200));
                    slave.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));

                    ActionToken addToken = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                    ActionToken learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);

                    Assert.IsTrue(addToken.WaitCompletedSignal());
                    Assert.IsTrue(learnToken.WaitCompletedSignal());
                }
            }
        }

        [Test]
        public void TestManyAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (BridgeController ctrl = al.CreateBridgeController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    int errors = 0;
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_BRIDGE_1, BaudRates.Rate_115200));
                    slave.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));

                    ActionToken addRemoveToken;
                    ActionToken learnToken;
                    MemoryGetIdResult memres;
                    for (int i = 0; i < POW; i++)
                    {
                        addRemoveToken = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                        learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);

                        if (!addRemoveToken.WaitCompletedSignal())
                        {
                            errors++;
                        }
                        if (!learnToken.WaitCompletedSignal())
                        {
                            errors++;
                        }

                        memres = ctrl.MemoryGetId();
                        Assert.AreEqual(1, memres.NodeId);
                        memres = slave.MemoryGetId();
                        int newNodeId = (i + 2);
                        if (newNodeId > 232)
                            newNodeId -= 231;
                        Assert.AreEqual(newNodeId, memres.NodeId);

                        addRemoveToken = ctrl.RemoveNodeFromNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                        learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);

                        if (!addRemoveToken.WaitCompletedSignal())
                        {
                            errors++;
                        }
                        if (!learnToken.WaitCompletedSignal())
                        {
                            errors++;
                        }

                        memres = ctrl.MemoryGetId();
                        Assert.AreEqual(1, memres.NodeId);
                        memres = slave.MemoryGetId();
                        Assert.AreEqual(0, memres.NodeId);

                        Assert.AreEqual(0, errors);
                    }

                }
            }
        }

        [Test]
        public void TestExpiredSync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (BridgeController ctrl = al.CreateBridgeController())
            {
                ctrl.Connect(new SerialPortDataSource(COM_CTRL_BRIDGE_1, BaudRates.Rate_115200));
                ActionResult res = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT);
                Assert.IsTrue(res.State == ActionStates.Expired);
            }
        }

        [Test]
        public void TestStopSync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (BridgeController ctrl = al.CreateBridgeController())
            {
                ctrl.Connect(new SerialPortDataSource(COM_CTRL_BRIDGE_1, BaudRates.Rate_115200));
                ActionResult res = ctrl.AddNodeToNetwork(Modes.NodeStop, 0);
                Assert.IsTrue(res == null || res.State == ActionStates.Completed);
            }
        }
    }
}
