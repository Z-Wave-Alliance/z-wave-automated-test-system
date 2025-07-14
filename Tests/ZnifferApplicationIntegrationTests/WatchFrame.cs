// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
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
using ZWave.ZnifferApplication;
using ZWave.ZnifferApplication.Devices;

namespace ZnifferApplicationIntegrationTests
{
    [TestFixture]
    public class WatchFrame : ZnifferTestsBase
    {
        [SetUp]
        public void SetUp()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    ctrl.Connect(new SerialPortDataSource(COM1, BaudRates.Rate_115200));
                    slave.Connect(new SerialPortDataSource(COM2, BaudRates.Rate_115200));
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

                    ActionToken addToken = ctrl.AddNodeToNetwork(Modes.NodeAny, 10000, null);
                    ActionToken learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, 10000, null);

                    Assert.IsTrue(addToken.WaitCompletedSignal());
                    Assert.IsTrue(learnToken.WaitCompletedSignal());
                    Utils.InitFrameDefinition();
                }
            }
        }

        [Test]
        public void TestListener()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            ZnifferApplicationLayer zl = new ZnifferApplicationLayer(new SessionLayer(), new ZnifferFrameLayer(Utils.InitFrameDefinition()), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    using (Zniffer znf = zl.CreateZniffer())
                    {
                        ctrl.Connect(new SerialPortDataSource(COM1, BaudRates.Rate_115200));
                        slave.Connect(new SerialPortDataSource(COM2, BaudRates.Rate_115200));
                        znf.Connect(new SerialPortDataSource(COMZ, BaudRates.Rate_115200));
                        COMMAND_CLASS_BASIC.BASIC_REPORT report = new COMMAND_CLASS_BASIC.BASIC_REPORT();
                        byte[] data = report;
                        bool isReceived = false;
                        bool hasVersion = znf.GetVersion().IsStateCompleted;
                        Assert.IsTrue(hasVersion);
                        znf.Start();
                        znf.SetFrequency(0x00);
                        znf.FrameClient.ReceiveFrameCallback = (x) =>
                        {
                            ZWave.ZnifferApplication.DataFrame df = (ZWave.ZnifferApplication.DataFrame)x;
                            if (df.Data != null && df.Data.Length > 1 && df.Data[0] == (byte)ZWave.ZnifferApplication.Enums.CommandTypes.DataHandler)
                            {
                                if (df.DataItem.Payload != null && df.DataItem.Payload.Length > 1 && df.DataItem.Payload[0] == data[0] && df.DataItem.Payload[1] == data[1])
                                    isReceived = true;
                            }
                        };
                        slave.SendData(0x01, data, TXO);

                        Thread.Sleep(2000);
                        Assert.IsTrue(isReceived);
                    }
                }
            }
        }

    }
}
