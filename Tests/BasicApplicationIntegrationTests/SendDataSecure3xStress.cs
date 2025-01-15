// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
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

namespace BasicApplicationIntegrationTests
{
    [TestFixture]
    public class SendDataSecure3xStress : BasicTestsBase
    {
        int POW = 100;
        int TIMEOUT = 30000;
        [SetUp]
        public void SetUp()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl1 = al.CreateController())
            {
                using (Slave slave1 = al.CreateSlave())
                {
                    using (Controller ctrl2 = al.CreateController())
                    {
                        using (Slave slave2 = al.CreateSlave())
                        {
                            using (Controller ctrl3 = al.CreateController())
                            {
                                using (Slave slave3 = al.CreateSlave())
                                {
                                    ctrl1.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                                    slave1.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));
                                    ctrl2.Connect(new SerialPortDataSource(COM_CTRL_STATIC_2, BaudRates.Rate_115200));
                                    slave2.Connect(new SerialPortDataSource(COM_SLAVE_2, BaudRates.Rate_115200));
                                    ctrl3.Connect(new SerialPortDataSource(COM_CTRL_STATIC_3, BaudRates.Rate_115200));
                                    slave3.Connect(new SerialPortDataSource(COM_SLAVE_3, BaudRates.Rate_115200));

                                    VersionResult res = ctrl1.GetVersion();
                                    Assert.IsTrue(res.State == ActionStates.Completed);
                                    Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || res.Library == Libraries.InstallerLib || res.Library == Libraries.ControllerPortableLib || res.Library == Libraries.ControllerBridgeLib);

                                    res = slave1.GetVersion();
                                    Assert.IsTrue(res.State == ActionStates.Completed);
                                    Assert.IsTrue(res.Library == Libraries.SlaveEnhancedLib || res.Library == Libraries.SlaveLib || res.Library == Libraries.SlaveRoutingLib);

                                    res = ctrl2.GetVersion();
                                    Assert.IsTrue(res.State == ActionStates.Completed);
                                    Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || res.Library == Libraries.InstallerLib || res.Library == Libraries.ControllerPortableLib || res.Library == Libraries.ControllerBridgeLib);

                                    res = slave2.GetVersion();
                                    Assert.IsTrue(res.State == ActionStates.Completed);
                                    Assert.IsTrue(res.Library == Libraries.SlaveEnhancedLib || res.Library == Libraries.SlaveLib || res.Library == Libraries.SlaveRoutingLib);

                                    res = ctrl3.GetVersion();
                                    Assert.IsTrue(res.State == ActionStates.Completed);
                                    Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || res.Library == Libraries.InstallerLib || res.Library == Libraries.ControllerPortableLib || res.Library == Libraries.ControllerBridgeLib);

                                    res = slave3.GetVersion();
                                    Assert.IsTrue(res.State == ActionStates.Completed);
                                    Assert.IsTrue(res.Library == Libraries.SlaveEnhancedLib || res.Library == Libraries.SlaveLib || res.Library == Libraries.SlaveRoutingLib);

                                    ActionResult ares = ctrl1.SetDefault();
                                    Assert.IsTrue(ares.State == ActionStates.Completed);

                                    ares = slave1.SetDefault();
                                    Assert.IsTrue(ares.State == ActionStates.Completed);

                                    ares = ctrl2.SetDefault();
                                    Assert.IsTrue(ares.State == ActionStates.Completed);

                                    ares = slave2.SetDefault();
                                    Assert.IsTrue(ares.State == ActionStates.Completed);

                                    ares = ctrl3.SetDefault();
                                    Assert.IsTrue(ares.State == ActionStates.Completed);

                                    ares = slave3.SetDefault();
                                    Assert.IsTrue(ares.State == ActionStates.Completed);
                                }
                            }
                        }
                    }
                }
            }
        }

        [Test]
        public void TestRequestsThreeNetworks()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl1 = al.CreateController())
            {
                using (Slave slave1 = al.CreateSlave())
                {
                    using (Controller ctrl2 = al.CreateController())
                    {
                        using (Slave slave2 = al.CreateSlave())
                        {
                            using (Controller ctrl3 = al.CreateController())
                            {
                                using (Slave slave3 = al.CreateSlave())
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
                                    ctrl3.Connect(new SerialPortDataSource(COM_CTRL_STATIC_3, BaudRates.Rate_115200));
                                    SetupSecurity(ctrl3, true);
                                    slave3.Connect(new SerialPortDataSource(COM_SLAVE_3, BaudRates.Rate_115200));
                                    SetupSecurity(slave3, true);

                                    ActionToken addToken = ctrl1.AddNodeToNetwork(Modes.NodeAny, TIMEOUT, null);
                                    ctrl1.WaitNodeStatusSignal(NodeStatuses.LearnReady, TIMEOUT);
                                    ActionToken learnToken = slave1.SetLearnMode(LearnModes.LearnModeClassic, TIMEOUT, null);

                                    if (!addToken.WaitCompletedSignal())
                                    {
                                        errors++;
                                    }
                                    if (!learnToken.WaitCompletedSignal())
                                    {
                                        errors++;
                                    }

                                    MemoryGetIdResult memres = ctrl1.MemoryGetId();
                                    Assert.AreEqual(1, memres.NodeId);
                                    memres = slave1.MemoryGetId();
                                    Assert.AreEqual(2, memres.NodeId);

                                    Assert.AreEqual(0, errors);

                                    addToken = ctrl2.AddNodeToNetwork(Modes.NodeAny, TIMEOUT, null);
                                    ctrl2.WaitNodeStatusSignal(NodeStatuses.LearnReady, TIMEOUT);
                                    learnToken = slave2.SetLearnMode(LearnModes.LearnModeClassic, TIMEOUT, null);

                                    if (!addToken.WaitCompletedSignal())
                                    {
                                        errors++;
                                    }
                                    if (!learnToken.WaitCompletedSignal())
                                    {
                                        errors++;
                                    }

                                    memres = ctrl2.MemoryGetId();
                                    Assert.AreEqual(1, memres.NodeId);
                                    memres = slave2.MemoryGetId();
                                    Assert.AreEqual(2, memres.NodeId);

                                    Assert.AreEqual(0, errors);

                                    addToken = ctrl3.AddNodeToNetwork(Modes.NodeAny, TIMEOUT, null);
                                    ctrl3.WaitNodeStatusSignal(NodeStatuses.LearnReady, TIMEOUT);
                                    learnToken = slave3.SetLearnMode(LearnModes.LearnModeClassic, TIMEOUT, null);

                                    if (!addToken.WaitCompletedSignal())
                                    {
                                        errors++;
                                    }
                                    if (!learnToken.WaitCompletedSignal())
                                    {
                                        errors++;
                                    }

                                    memres = ctrl3.MemoryGetId();
                                    Assert.AreEqual(1, memres.NodeId);
                                    memres = slave3.MemoryGetId();
                                    Assert.AreEqual(2, memres.NodeId);

                                    Assert.AreEqual(0, errors);

                                    int ee1 = 0;
                                    Thread t1 = new Thread(() =>
                                    {
                                        slave1.ResponseData(new COMMAND_CLASS_BASIC.BASIC_REPORT(), TXO, new COMMAND_CLASS_BASIC.BASIC_GET());
                                        for (int i = 0; i < POW && errors == 0; i++)
                                        {
                                            DateTime before = DateTime.Now;
                                            ActionResult res = ctrl1.RequestData(0x02, new COMMAND_CLASS_BASIC.BASIC_GET(), TXO, new COMMAND_CLASS_BASIC.BASIC_REPORT(), TIMEOUT);
                                            DateTime after = DateTime.Now;
                                            if (res.State != ActionStates.Completed)
                                            {
                                                Interlocked.Increment(ref ee1);
                                                Console.Out.WriteLine("{0:HH:mm:ss}: t1 state:{1}, time:{2:HH:mm:ss}-{3:HH:mm:ss}", DateTime.Now, res.State, before, after);
                                                Console.Out.WriteLine(res.TraceLogString);
                                            }
                                            if ((i + 1) % 1000 == 0)
                                                Console.Out.WriteLine("{0:HH:mm:ss}: t1 total: {1}, errors:{2}", DateTime.Now, i, ee1);
                                        }
                                    });
                                    int ee2 = 0;
                                    Thread t2 = new Thread(() =>
                                    {
                                        slave2.ResponseData(new COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_REPORT(), TXO, new COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_GET());
                                        for (int i = 0; i < POW && errors == 0; i++)
                                        {
                                            DateTime before = DateTime.Now;
                                            ActionResult res = ctrl2.RequestData(0x02, new COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_GET(), TXO, new COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_REPORT(), TIMEOUT);
                                            DateTime after = DateTime.Now;
                                            if (res.State != ActionStates.Completed)
                                            {
                                                Interlocked.Increment(ref ee2);
                                                Console.Out.WriteLine("{0:HH:mm:ss}: t2 state:{1}, time:{2:HH:mm:ss}-{3:HH:mm:ss}", DateTime.Now, res.State, before, after);
                                                Console.Out.WriteLine(res.TraceLogString);

                                            }
                                            if ((i + 1) % 1000 == 0)
                                                Console.Out.WriteLine("{0:HH:mm:ss}: t2 total: {1}, errors:{2}", DateTime.Now, i, ee2);
                                        }
                                    });
                                    int ee3 = 0;
                                    Thread t3 = new Thread(() =>
                                    {
                                        slave3.ResponseData(new COMMAND_CLASS_VERSION.VERSION_REPORT(), TXO, new COMMAND_CLASS_VERSION.VERSION_GET());
                                        for (int i = 0; i < POW && errors == 0; i++)
                                        {
                                            DateTime before = DateTime.Now;
                                            ActionResult res = ctrl3.RequestData(0x02, new COMMAND_CLASS_VERSION.VERSION_GET(), TXO, new COMMAND_CLASS_VERSION.VERSION_REPORT(), TIMEOUT);
                                            DateTime after = DateTime.Now;
                                            if (res.State != ActionStates.Completed)
                                            {
                                                Interlocked.Increment(ref ee3);
                                                Console.Out.WriteLine("{0:HH:mm:ss}: t3 state:{1}, time:{2:HH:mm:ss}-{3:HH:mm:ss}", DateTime.Now, res.State, before, after);
                                                Console.Out.WriteLine(res.TraceLogString);
                                            }
                                            if ((i + 1) % 1000 == 0)
                                                Console.Out.WriteLine("{0:HH:mm:ss}: t3 total: {1}, errors:{2}", DateTime.Now, i, ee3);
                                        }
                                    });

                                    t1.IsBackground = true;
                                    t2.IsBackground = true;
                                    t3.IsBackground = true;

                                    t1.Start();
                                    t2.Start();
                                    t3.Start();

                                    t1.Join();
                                    t2.Join();
                                    t3.Join();
                                    Assert.AreEqual(0, errors);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
