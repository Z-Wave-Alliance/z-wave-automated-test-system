// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
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
    public class ExpectDataBridge : BasicTestsBase
    {
        byte[] cmd_get = new byte[] { 0x20, 0x02 };
        byte[] cmd_report = new byte[] { 0x20, 0x03, 0x055 };
        int POW = 10;
        int TIMEOUT = 2000;

        [SetUp]
        public void SetUp()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (BridgeController bridge = al.CreateBridgeController())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    bridge.Connect(new SerialPortDataSource(COM_CTRL_BRIDGE_1, BaudRates.Rate_115200));
                    VersionResult res = ctrl.GetVersion();
                    Assert.IsTrue(res.State == ActionStates.Completed);
                    Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || res.Library == Libraries.InstallerLib || res.Library == Libraries.ControllerPortableLib || res.Library == Libraries.ControllerBridgeLib);
                    res = bridge.GetVersion();
                    Assert.IsTrue(res.State == ActionStates.Completed);
                    Assert.IsTrue(res.Library == Libraries.ControllerBridgeLib);
                    ActionResult ares = ctrl.SetDefault();
                    Assert.IsTrue(ares.State == ActionStates.Completed);
                    ares = bridge.SetDefault();
                    Assert.IsTrue(ares.State == ActionStates.Completed);

                    ActionToken addToken = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                    ActionToken learnToken = bridge.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);
                    Assert.IsTrue(addToken.WaitCompletedSignal().IsStateCompleted);
                    Assert.IsTrue(learnToken.WaitCompletedSignal().IsStateCompleted);

                    addToken = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                    learnToken = bridge.SetVirtualDeviceLearnMode(new NodeTag(0), VirtualDeviceLearnModes.Enable, INCLUSION_TIMEOUT, null);
                    Assert.IsTrue(addToken.WaitCompletedSignal().IsStateCompleted);
                    Assert.IsTrue(learnToken.WaitCompletedSignal().IsStateCompleted);
                }
            }
        }

        [Test]
        public void TestAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (BridgeController bridge = al.CreateBridgeController())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    bridge.Connect(new SerialPortDataSource(COM_CTRL_BRIDGE_1, BaudRates.Rate_115200));
                    ActionToken expectToken = null;
                    ActionToken sendToken = null;
                    int errors = 0;

                    for (int i = 0; i < POW; i++)
                    {
                        expectToken = bridge.ExpectData(cmd_get, TIMEOUT, null);
                        sendToken = ctrl.SendData(new NodeTag(0x02), cmd_get, TXO, (action) =>
                        {
                        });
                        if (!expectToken.WaitCompletedSignal())
                        {
                            errors++;
                        }
                        if (!sendToken.WaitCompletedSignal())
                        {
                            errors++;
                        }

                        expectToken = bridge.ExpectData(new NodeTag(0x03), cmd_get, TIMEOUT, null);

                        sendToken = ctrl.SendData(new NodeTag(0x03), cmd_get, TXO, (action) =>
                        {
                        });
                        if (!expectToken.WaitCompletedSignal())
                        {
                            errors++;
                        }
                        if (!sendToken.WaitCompletedSignal())
                        {
                            errors++;
                        }

                    }
                    bridge.SendDataBridge(new NodeTag(0x02), new NodeTag(0x01), new COMMAND_CLASS_BASIC.BASIC_REPORT(), TXO);

                    Assert.AreEqual(0, errors);
                }
            }
        }

        [Test]
        public void TestConcurrentSync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (BridgeController bridge = al.CreateBridgeController())
                {
                    ActionResult res;
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    bridge.Connect(new SerialPortDataSource(COM_CTRL_BRIDGE_1, BaudRates.Rate_115200));
                    Thread thread1 = new Thread(() =>
                    {
                        for (int i = 0; i < POW; i++)
                        {
                            res = ctrl.ExpectData(cmd_report, TIMEOUT);
                            res = ctrl.SendData(new NodeTag(0x02), cmd_get, TXO);
                        }
                    }) { Name = "TestConcurrentAsync - Thread1" };
                    Thread thread2 = new Thread(() =>
                    {
                        for (int i = 0; i < POW; i++)
                        {
                            res = bridge.ExpectData(cmd_get, TIMEOUT);
                            res = bridge.SendData(new NodeTag(0x01), cmd_report, TXO);
                        }
                    }) { Name = "TestConcurrentAsync - Thread2" };
                    thread1.Start();
                    thread2.Start();
                    res = ctrl.SendData(new NodeTag(0x02), cmd_get, TXO); //initiate
                    thread1.Join();
                    thread2.Join();
                }
            }
        }
    }
}
