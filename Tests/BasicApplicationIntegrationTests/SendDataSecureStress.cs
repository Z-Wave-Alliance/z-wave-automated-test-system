// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System.Linq;
using System.Threading;
using NUnit.Framework;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Enums;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Enums;
using ZWave.Layers;
using ZWave.Layers.Session;
using ZWave.Layers.Transport;
using ZWave.Devices;

namespace BasicApplicationIntegrationTests
{
    [TestFixture]
    public class SendDataSecureStress : BasicTestsBase
    {
        int POW = 100;
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
                            Assert.IsTrue(res.State == ActionStates.Completed);
                            Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || res.Library == Libraries.InstallerLib || res.Library == Libraries.ControllerPortableLib || res.Library == Libraries.ControllerBridgeLib);

                            res = slave1.GetVersion();
                            Assert.IsTrue(res.State == ActionStates.Completed);
                            Assert.IsTrue(res.Library == Libraries.EndDeviceLegacyLib || res.Library == Libraries.EndDeviceLib || res.Library == Libraries.EndDeviceLegacyRoutingLib);

                            res = ctrl2.GetVersion();
                            Assert.IsTrue(res.State == ActionStates.Completed);
                            Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || res.Library == Libraries.InstallerLib || res.Library == Libraries.ControllerPortableLib || res.Library == Libraries.ControllerBridgeLib);

                            res = slave2.GetVersion();
                            Assert.IsTrue(res.State == ActionStates.Completed);
                            Assert.IsTrue(res.Library == Libraries.EndDeviceLegacyLib || res.Library == Libraries.EndDeviceLib || res.Library == Libraries.EndDeviceLegacyRoutingLib);

                            ActionResult ares = ctrl1.SetDefault();
                            Assert.IsTrue(ares.State == ActionStates.Completed);

                            ares = slave1.SetDefault();
                            Assert.IsTrue(ares.State == ActionStates.Completed);

                            ares = ctrl2.SetDefault();
                            Assert.IsTrue(ares.State == ActionStates.Completed);

                            ares = slave2.SetDefault();
                            Assert.IsTrue(ares.State == ActionStates.Completed);
                        }
                    }
                }
            }
        }

        [Test]
        public void TestRequestOneNetwork()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (EndDevice slave = al.CreateEndDevice())
                {
                    int errors = 0;
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    SetupSecurity(ctrl, true);
                    slave.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));
                    SetupSecurity(slave, true);

                    ActionToken addToken = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                    ctrl.WaitNodeStatusSignal(NodeStatuses.LearnReady, INCLUSION_TIMEOUT);
                    ActionToken learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);

                    if (!addToken.WaitCompletedSignal())
                    {
                        errors++;
                    }
                    if(!learnToken.WaitCompletedSignal())
                    {
                        errors++;
                    }

                    MemoryGetIdResult memres = ctrl.MemoryGetId();
                    Assert.AreEqual(1, memres.Node.Id);
                    memres = slave.MemoryGetId();
                    Assert.AreEqual(2, memres.Node.Id);

                    Assert.AreEqual(0, errors);

                    slave.ResponseData(new COMMAND_CLASS_BASIC.BASIC_REPORT(), TXO, new COMMAND_CLASS_BASIC.BASIC_GET());
                    for (int i = 0; i < POW; i++)
                    {
                        ActionResult res = ctrl.RequestData(new NodeTag(0x02), new COMMAND_CLASS_BASIC.BASIC_GET(), TXO, new COMMAND_CLASS_BASIC.BASIC_REPORT(), INCLUSION_TIMEOUT);
                        Assert.AreEqual(ActionStates.Completed, res.State);
                    }
                }
            }
        }


        [Test]
        public void TestSendTwoNetworks()
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
                            int errors = 0;
                            ctrl1.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                            SetupSecurity(ctrl1, true);
                            slave1.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));
                            SetupSecurity(slave1, true);
                            ctrl2.Connect(new SerialPortDataSource(COM_CTRL_STATIC_2, BaudRates.Rate_115200));
                            SetupSecurity(ctrl2, true);
                            slave2.Connect(new SerialPortDataSource(COM_SLAVE_2, BaudRates.Rate_115200));
                            SetupSecurity(slave2, true);

                            ActionToken addToken = ctrl1.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                            ctrl1.WaitNodeStatusSignal(NodeStatuses.LearnReady, INCLUSION_TIMEOUT);
                            ActionToken learnToken = slave1.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);

                            if (!addToken.WaitCompletedSignal())
                            {
                                errors++;
                            }
                            if (!learnToken.WaitCompletedSignal())
                            {
                                errors++;
                            }

                            MemoryGetIdResult memres = ctrl1.MemoryGetId();
                            Assert.AreEqual(1, memres.Node.Id);
                            memres = slave1.MemoryGetId();
                            Assert.AreEqual(2, memres.Node.Id);

                            Assert.AreEqual(0, errors);

                            addToken = ctrl2.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                            ctrl2.WaitNodeStatusSignal(NodeStatuses.LearnReady, INCLUSION_TIMEOUT);
                            learnToken = slave2.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);

                            if (!addToken.WaitCompletedSignal())
                            {
                                errors++;
                            }
                            if (!learnToken.WaitCompletedSignal())
                            {
                                errors++;
                            }

                            memres = ctrl2.MemoryGetId();
                            Assert.AreEqual(1, memres.Node.Id);
                            memres = slave2.MemoryGetId();
                            Assert.AreEqual(2, memres.Node.Id);

                            Assert.AreEqual(0, errors);

                            Thread t1 = new Thread(() =>
                            {
                                for (int i = 0; i < POW && errors == 0; i++)
                                {
                                    ActionResult res = ctrl1.SendData(new NodeTag(0x02), new COMMAND_CLASS_BASIC.BASIC_GET(), TXO);
                                    if (res.State != ActionStates.Completed)
                                    {
                                        Interlocked.Increment(ref errors);
                                    }
                                }
                            });
                            Thread t2 = new Thread(() =>
                            {
                                for (int i = 0; i < POW && errors == 0; i++)
                                {
                                    ActionResult res = ctrl2.SendData(new NodeTag(0x02), new COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_GET(), TXO);
                                    if (res.State != ActionStates.Completed)
                                    {
                                        Interlocked.Increment(ref errors);
                                    }
                                }
                            });
                            t1.Start();
                            t2.Start();
                            t1.Join();
                            t2.Join();
                            Assert.AreEqual(0, errors);
                        }
                    }
                }
            }
        }

        [Test]
        public void TestRequestsTwoNetworks()
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
                            int errors = 0;
                            ctrl1.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                            SetupSecurity(ctrl1, true);
                            slave1.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));
                            SetupSecurity(slave1, true);
                            ctrl2.Connect(new SerialPortDataSource(COM_CTRL_STATIC_2, BaudRates.Rate_115200));
                            SetupSecurity(ctrl2, true);
                            slave2.Connect(new SerialPortDataSource(COM_SLAVE_2, BaudRates.Rate_115200));
                            SetupSecurity(slave2, true);

                            ActionToken addToken = ctrl1.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                            ctrl1.WaitNodeStatusSignal(NodeStatuses.LearnReady, INCLUSION_TIMEOUT);
                            ActionToken learnToken = slave1.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);

                            if (!addToken.WaitCompletedSignal())
                            {
                                errors++;
                            }
                            if (!learnToken.WaitCompletedSignal())
                            {
                                errors++;
                            }

                            MemoryGetIdResult memres = ctrl1.MemoryGetId();
                            Assert.AreEqual(1, memres.Node.Id);
                            memres = slave1.MemoryGetId();
                            Assert.AreEqual(2, memres.Node.Id);

                            Assert.AreEqual(0, errors);

                            addToken = ctrl2.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                            ctrl2.WaitNodeStatusSignal(NodeStatuses.LearnReady, INCLUSION_TIMEOUT);
                            learnToken = slave2.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);

                            if (!addToken.WaitCompletedSignal())
                            {
                                errors++;
                            }
                            if (!learnToken.WaitCompletedSignal())
                            {
                                errors++;
                            }

                            memres = ctrl2.MemoryGetId();
                            Assert.AreEqual(1, memres.Node.Id);
                            memres = slave2.MemoryGetId();
                            Assert.AreEqual(2, memres.Node.Id);

                            Assert.AreEqual(0, errors);

                            Thread t1 = new Thread(() =>
                            {
                                slave1.ResponseData(new COMMAND_CLASS_BASIC.BASIC_REPORT(), TXO, new COMMAND_CLASS_BASIC.BASIC_GET());
                                for (int i = 0; i < POW && errors == 0; i++)
                                {
                                    ActionResult res = ctrl1.RequestData(new NodeTag(0x02), new COMMAND_CLASS_BASIC.BASIC_GET(), TXO, new COMMAND_CLASS_BASIC.BASIC_REPORT(), INCLUSION_TIMEOUT);
                                    if (res.State != ActionStates.Completed)
                                    {
                                        Interlocked.Increment(ref errors);
                                    }
                                }
                            });
                            Thread t2 = new Thread(() =>
                            {
                                slave2.ResponseData(new COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_REPORT(), TXO, new COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_GET());
                                for (int i = 0; i < POW && errors == 0; i++)
                                {
                                    ActionResult res = ctrl2.RequestData(new NodeTag(0x02), new COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_GET(), TXO, new COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_REPORT(), INCLUSION_TIMEOUT);
                                    if (res.State != ActionStates.Completed)
                                    {
                                        Interlocked.Increment(ref errors);
                                    }
                                }
                            });
                            t1.Start();
                            t2.Start();
                            t1.Join();
                            t2.Join();
                            Assert.AreEqual(0, errors);
                        }
                    }
                }
            }
        }
    }
}
