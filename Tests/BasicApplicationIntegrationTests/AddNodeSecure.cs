// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using NUnit.Framework;
using ZWave;
using ZWave.Enums;
using ZWave.Layers;
using ZWave.Layers.Session;
using ZWave.Layers.Transport;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Enums;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Operations;

namespace BasicApplicationIntegrationTests
{
    [TestFixture]
    public class AddNodeSecure : BasicTestsBase
    {
        int POW = 10;

        [SetUp]
        public void SetUp()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl1 = al.CreateController())
            {
                using (EndDevice slave1 = al.CreateEndDevice())
                {
                    using (Controller ctrl2 = al.CreateController())
                    {
                        using (EndDevice slave2 = al.CreateEndDevice())
                        {
                            ctrl1.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                            slave1.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));
                            ctrl2.Connect(new SerialPortDataSource(COM_CTRL_STATIC_2, BaudRates.Rate_115200));
                            slave2.Connect(new SerialPortDataSource(COM_SLAVE_2, BaudRates.Rate_115200));
                            VersionResult res = ctrl1.GetVersion();
                            Assert.IsTrue(res);
                            Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || res.Library == Libraries.InstallerLib || res.Library == Libraries.ControllerPortableLib || res.Library == Libraries.ControllerBridgeLib);
                            res = slave1.GetVersion();
                            Assert.IsTrue(res);
                            Assert.IsTrue(res.Library == Libraries.EndDeviceLegacyLib || res.Library == Libraries.EndDeviceLib || res.Library == Libraries.EndDeviceLegacyRoutingLib);
                            
                            res = ctrl2.GetVersion();
                            Assert.IsTrue(res);
                            Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || res.Library == Libraries.InstallerLib || res.Library == Libraries.ControllerPortableLib || res.Library == Libraries.ControllerBridgeLib);
                            res = slave2.GetVersion();
                            Assert.IsTrue(res);
                            Assert.IsTrue(res.Library == Libraries.EndDeviceLegacyLib || res.Library == Libraries.EndDeviceLib || res.Library == Libraries.EndDeviceLegacyRoutingLib);

                            ActionResult ares = ctrl1.SetDefault();
                            Assert.IsTrue(ares);
                            ares = slave1.SetDefault();
                            Assert.IsTrue(ares);
                            ares = ctrl2.SetDefault();
                            Assert.IsTrue(ares);
                            ares = slave2.SetDefault();
                            Assert.IsTrue(ares);
                        }
                    }
                }
            }
        }

        [Test]
        public void TestSlaveAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (EndDevice slave = al.CreateEndDevice())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    SetupSecurity(ctrl, true);
                    slave.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));
                    //SetupSecurity(slave, true);

                    var addToken = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                    
                    ctrl.WaitNodeStatusSignal(NodeStatuses.LearnReady, INCLUSION_TIMEOUT);
                    var learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);

                    var addRes = (AddRemoveNodeResult)addToken.WaitCompletedSignal();
                    var learnRes = learnToken.WaitCompletedSignal();

                    Assert.IsTrue(addRes);
                    Assert.IsTrue(learnRes);
                    Assert.AreEqual(SubstituteStatuses.Done, addRes.SubstituteStatus);
                }
            }
        }

        [Test]
        public void TestControllerAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (Controller slave = al.CreateController())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    SetupSecurity(ctrl, false);
                    slave.Connect(new SerialPortDataSource(COM_CTRL_STATIC_2, BaudRates.Rate_115200));
                    SetupSecurity(slave, false);

                    var addToken = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                    ctrl.WaitNodeStatusSignal(NodeStatuses.LearnReady, INCLUSION_TIMEOUT);
                    var learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);

                    var addRes = (AddRemoveNodeResult)addToken.WaitCompletedSignal();
                    var learnRes = learnToken.WaitCompletedSignal();

                    Assert.IsTrue(addRes);
                    Assert.IsTrue(learnRes);
                    Assert.AreEqual(SubstituteStatuses.Done, addRes.SubstituteStatus);
                }
            }
        }

        [Test]
        public void TestNonSecureControllerAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (Controller slave = al.CreateController())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    SetupSecurity(ctrl, true);
                    slave.Connect(new SerialPortDataSource(COM_CTRL_STATIC_2, BaudRates.Rate_115200));
                    slave.MemoryGetId();

                    var addToken = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                    ctrl.WaitNodeStatusSignal(NodeStatuses.LearnReady, INCLUSION_TIMEOUT);
                    var learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);

                    var addRes = (AddRemoveNodeResult)addToken.WaitCompletedSignal();
                    var learnRes = learnToken.WaitCompletedSignal();

                    Assert.IsTrue(addRes);
                    Assert.IsTrue(learnRes);
                    Assert.AreEqual(SubstituteStatuses.Failed, addRes.SubstituteStatus);
                }
            }
        }

        [Test]
        public void TestSlaveManyAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (EndDevice slave = al.CreateEndDevice())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    SetupSecurity(ctrl, true);
                    slave.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));
                    //SetupSecurity(slave, true);

                    for (int i = 0; i < POW; i++)
                    {
                        var addToken = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                        ctrl.WaitNodeStatusSignal(NodeStatuses.LearnReady, INCLUSION_TIMEOUT);
                        var learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);

                        var addRes = (AddRemoveNodeResult)addToken.WaitCompletedSignal();
                        var learnRes = learnToken.WaitCompletedSignal();

                        Assert.IsTrue(addRes);
                        Assert.IsTrue(learnRes);
                        Assert.AreEqual(SubstituteStatuses.Done, addRes.SubstituteStatus);

                        var memres = ctrl.MemoryGetId();
                        Assert.IsTrue(memres);
                        Assert.AreEqual(1, memres.Node.Id);

                        memres = slave.MemoryGetId();
                        Assert.IsTrue(memres);
                        int newNodeId = (i + 2);
                        if (newNodeId > 232)
                            newNodeId -= 231;
                        Assert.AreEqual(newNodeId, memres.Node.Id);

                        var removeToken = ctrl.RemoveNodeFromNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                        learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);

                        var removeRes = removeToken.WaitCompletedSignal();
                        learnRes = learnToken.WaitCompletedSignal();

                        Assert.IsTrue(removeRes);
                        Assert.IsTrue(learnRes);

                        memres = ctrl.MemoryGetId();
                        Assert.IsTrue(memres);
                        Assert.AreEqual(1, memres.Node.Id);
                        memres = slave.MemoryGetId();
                        Assert.IsTrue(memres);
                        Assert.AreEqual(0, memres.Node.Id);
                    }
                }
            }
        }

        [Test]
        public void TestControllerManyAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (Controller slave = al.CreateController())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    SetupSecurity(ctrl, false);
                    slave.Connect(new SerialPortDataSource(COM_CTRL_STATIC_2, BaudRates.Rate_115200));
                    SetupSecurity(slave, false);

                    for (int i = 0; i < POW; i++)
                    {
                        var addToken = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                        ctrl.WaitNodeStatusSignal(NodeStatuses.LearnReady, INCLUSION_TIMEOUT);
                        var learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);

                        var addRes = (AddRemoveNodeResult)addToken.WaitCompletedSignal();
                        var learnRes = learnToken.WaitCompletedSignal();

                        Assert.IsTrue(addRes);
                        Assert.IsTrue(learnRes);
                        Assert.AreEqual(SubstituteStatuses.Done, addRes.SubstituteStatus);

                        var memres = ctrl.MemoryGetId();
                        Assert.IsTrue(memres);
                        Assert.AreEqual(1, memres.Node.Id);

                        memres = slave.MemoryGetId();
                        Assert.IsTrue(memres);
                        int newNodeId = (i + 2);
                        if (newNodeId > 232)
                            newNodeId -= 231;
                        Assert.AreEqual(newNodeId, memres.Node.Id);

                        var removeToken = ctrl.RemoveNodeFromNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                        learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);

                        var removeRes = removeToken.WaitCompletedSignal();
                        learnRes = learnToken.WaitCompletedSignal();

                        Assert.IsTrue(removeRes);
                        Assert.IsTrue(learnRes);

                        memres = ctrl.MemoryGetId();
                        Assert.IsTrue(memres);
                        Assert.AreEqual(1, memres.Node.Id);
                        memres = slave.MemoryGetId();
                        Assert.IsTrue(memres);
                        Assert.AreEqual(1, memres.Node.Id);
                    }
                }
            }
        }
    }
}
