// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ZWave.Layers.Transport;
using ZWave.Layers;
using ZWave.Enums;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Operations;
using ZWave.Layers.Session;
using ZWave;
using ZWave.BasicApplication.Enums;
using System.Threading;
using ZWave.CommandClasses;

namespace BasicApplicationIntegrationTests
{
    [TestFixture]
    public class AddNode : BasicTestsBase
    {
        int POW = 10;
        [SetUp]
        public void SetUp()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    slave.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));
                    VersionResult res = ctrl.GetVersion();
                    Assert.IsTrue(res.State == ActionStates.Completed);
                    Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || res.Library == Libraries.InstallerLib || res.Library == Libraries.ControllerPortableLib || res.Library == Libraries.ControllerBridgeLib);
                    res = slave.GetVersion();
                    Assert.IsTrue(res.State == ActionStates.Completed);
                    Assert.IsTrue(res.Library == Libraries.SlaveEnhancedLib || res.Library == Libraries.SlaveLib || res.Library == Libraries.SlaveRoutingLib);
                    ActionResult ares = ctrl.SetDefault();
                    Assert.IsTrue(ares.State == ActionStates.Completed);
                    ares = slave.SetDefault();
                    Assert.IsTrue(ares.State == ActionStates.Completed);
                }
            }
        }

        [Test]
        [Ignore]
        public void TestAsync232()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    using (Controller exclude = al.CreateController())
                    {
                        ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                        slave.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));
                        exclude.Connect(new SerialPortDataSource(COM_CTRL_STATIC_2, BaudRates.Rate_115200));
                        exclude.SetDefault();
                        for (int i = 0; i < 231; i++)
                        {
                            ActionToken addRemoveToken = ctrl.AddNodeToNetwork(Modes.NodeAny, 180000, null);
                            ActionToken learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, 180000, null);
                            AddRemoveNodeResult addRemoveResult = (AddRemoveNodeResult)addRemoveToken.WaitCompletedSignal();
                            SetLearnModeResult learnResult = (SetLearnModeResult)learnToken.WaitCompletedSignal();

                            Assert.AreEqual(ActionStates.Completed, addRemoveResult.State);
                            Assert.AreEqual(i + 2, addRemoveResult.Id);
                            Assert.AreEqual(ActionStates.Completed, learnResult.State);
                            Assert.AreEqual(i + 2, learnResult.NodeId);

                            addRemoveToken = exclude.RemoveNodeFromNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                            learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);
                            addRemoveResult = (AddRemoveNodeResult)addRemoveToken.WaitCompletedSignal();
                            learnResult = (SetLearnModeResult)learnToken.WaitCompletedSignal();

                            Assert.AreEqual(ActionStates.Completed, addRemoveResult.State);
                            Assert.AreEqual(0, addRemoveResult.Id);
                            Assert.AreEqual(ActionStates.Completed, learnResult.State);
                            Assert.AreEqual(0, learnResult.NodeId);

                            MemoryGetIdResult memRes = slave.MemoryGetId();
                            Assert.AreEqual(0, memRes.NodeId);
                        }
                    }
                }
            }
        }

        [Test]
        public void TestAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    slave.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));

                    ActionToken addToken = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                    ActionToken learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);

                    Assert.IsTrue(addToken.WaitCompletedSignal());
                    Thread.Sleep(2000);
                    ctrl.SendData(0x02, new COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_GET(), TXO);
                    Assert.IsTrue(learnToken.WaitCompletedSignal());
                }
            }
        }

        [Test]
        public void TestManyAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    int errors = 0;
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
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
            using (Controller ctrl = al.CreateController())
            {
                ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                ActionResult res = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT);
                Assert.IsTrue(res.State == ActionStates.Expired);
            }
        }

        [Test]
        public void TestStopAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                ActionToken addToken = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                Thread.Sleep(2000);
                ctrl.Cancel(addToken);
                var addResult = addToken.WaitCompletedSignal();
                Assert.AreEqual(ActionStates.Cancelled, addResult.State);
                addToken.WaitCompletedSignal();
            }
        }

        [Test]
        public void TestStopSync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                ActionResult res = ctrl.AddNodeToNetwork(Modes.NodeStop, 0);
                Assert.IsTrue(res == null || res.State == ActionStates.Completed);
            }
        }
    }
}
