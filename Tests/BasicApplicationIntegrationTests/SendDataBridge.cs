// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.Layers;
using ZWave.Layers.Session;
using ZWave.Enums;
using ZWave;
using ZWave.BasicApplication.Enums;
using ZWave.BasicApplication.Operations;
using ZWave.Layers.Transport;
using System.Threading;

namespace BasicApplicationIntegrationTests
{
    [TestFixture]
    public class SendDataBridge : BasicTestsBase
    {
        byte[] cmd_get = new byte[] { 0x20, 0x02 };
        byte[] cmd_report = new byte[] { 0x20, 0x03, 0x055 };
        int POW = 10;

        [SetUp]
        public void SetUp()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (BridgeController slave = al.CreateBridgeController())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    slave.Connect(new SerialPortDataSource(COM_CTRL_BRIDGE_1, BaudRates.Rate_115200));
                    VersionResult res = ctrl.GetVersion();
                    Assert.IsTrue(res.State == ActionStates.Completed);
                    Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || res.Library == Libraries.InstallerLib || res.Library == Libraries.ControllerPortableLib || res.Library == Libraries.ControllerBridgeLib);
                    res = slave.GetVersion();
                    Assert.IsTrue(res.State == ActionStates.Completed);
                    Assert.IsTrue(res.Library == Libraries.ControllerBridgeLib);
                    ActionResult ares = ctrl.SetDefault();
                    Assert.IsTrue(ares.State == ActionStates.Completed);
                    ares = slave.SetDefault();
                    Assert.IsTrue(ares.State == ActionStates.Completed);

                    ActionToken addToken = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                    ActionToken learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);
                    Assert.IsTrue(addToken.WaitCompletedSignal().IsStateCompleted);
                    Assert.IsTrue(learnToken.WaitCompletedSignal().IsStateCompleted);

                    addToken = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                    learnToken = slave.SetSlaveLearnMode(0, SlaveLearnModes.VirtualSlaveLearnModeEnable, INCLUSION_TIMEOUT, null);
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
                using (BridgeController slave = al.CreateBridgeController())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    slave.Connect(new SerialPortDataSource(COM_CTRL_BRIDGE_1, BaudRates.Rate_115200));
                    ActionToken sendToken = null;
                    for (int i = 0; i < POW; i++)
                    {
                        sendToken = slave.SendDataBridge(0x03, 0x01, cmd_get, TXO, null);
                        ActionResult res = sendToken.WaitCompletedSignal();
                        Assert.AreEqual(ActionStates.Completed, res.State);
                    }
                }
            }
        }

        [Test]
        public void TestFailedSync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (BridgeController slave = al.CreateBridgeController())
            {
                slave.Connect(new SerialPortDataSource(COM_CTRL_BRIDGE_1, BaudRates.Rate_115200));
                for (int i = 0; i < POW; i++)
                {
                    SendDataResult res = slave.SendDataBridge(0x03, 0x04, cmd_get, TXO);
                    Assert.AreEqual(TransmitStatuses.CompleteNoAcknowledge, res.TransmitStatus);
                }
            }
        }

        [Test]
        public void TestConcurrentSync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (BridgeController slave = al.CreateBridgeController())
                {
                    ActionResult res;
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    slave.Connect(new SerialPortDataSource(COM_CTRL_BRIDGE_1, BaudRates.Rate_115200));
                    int errors = 0;
                    Thread thread1 = new Thread(() =>
                    {
                        for (int i = 0; i < POW; i++)
                        {
                            res = slave.SendDataBridge(0x03, 0x01, cmd_get, TXO);
                            if (res.State != ActionStates.Completed)
                                Interlocked.Increment(ref errors);
                        }
                    });
                    Thread thread2 = new Thread(() =>
                    {
                        for (int i = 0; i < POW; i++)
                        {
                            res = slave.SendDataBridge(0x03, 0x01, cmd_report, TXO);
                            if (res.State != ActionStates.Completed)
                                Interlocked.Increment(ref errors);
                        }
                    });
                    thread1.Start();
                    thread2.Start();
                    thread1.Join();
                    thread2.Join();
                    if (errors > 0)
                        Assert.Inconclusive();
                }
            }
        }
    }
}
