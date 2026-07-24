// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZWave.BasicApplication.Enums;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using NUnit.Framework;
using ZWave.Layers;
using ZWave;
using ZWave.Enums;
using ZWave.Layers.Session;
using System.Threading;
using ZWave.BasicApplication.Operations;
using ZWave.Layers.Transport;
using ZWave.Devices;

namespace BasicApplicationIntegrationTests
{
    [TestFixture]
    public class ResponseData : BasicTestsBase
    {
        byte[] cmd_get = new byte[] { 0x20, 0x02 };
        byte[] cmd_report = new byte[] { 0x20, 0x03, 0x55 };
        int POW = 10;
        int REQUEST_DATA_TIMEOUT = 3000;

        [SetUp]
        public void SetUp()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (EndDevice slave = al.CreateEndDevice())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    slave.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));
                    VersionResult res = ctrl.GetVersion();
                    Assert.IsTrue(res.State == ActionStates.Completed);
                    Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || 
                        res.Library == Libraries.InstallerLib || 
                        res.Library == Libraries.ControllerPortableLib || 
                        res.Library == Libraries.ControllerBridgeLib);
                    res = slave.GetVersion();
                    Assert.IsTrue(res.State == ActionStates.Completed);
                    Assert.IsTrue(res.Library == Libraries.EndDeviceLegacyLib ||
                        res.Library == Libraries.EndDeviceLib || 
                        res.Library == Libraries.EndDeviceLegacyRoutingLib);
                    ActionResult ares = ctrl.SetDefault();
                    Assert.IsTrue(ares.State == ActionStates.Completed);
                    ares = slave.SetDefault();
                    Assert.IsTrue(ares.State == ActionStates.Completed);

                    ActionToken addToken = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                    ActionToken learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);

                    Assert.IsTrue(addToken.WaitCompletedSignal());
                    Assert.IsTrue(learnToken.WaitCompletedSignal());
                }
            }
        }

        [Test]
        public void TestSync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (EndDevice slave = al.CreateEndDevice())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    slave.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));
                    slave.ResponseData(cmd_report, TXO, cmd_get);
                    ActionResult res = ctrl.RequestData(new NodeTag(0x02), cmd_get, TXO, cmd_report, REQUEST_DATA_TIMEOUT);
                    Assert.AreEqual(ActionStates.Completed, res.State);
                }
            }
        }

        [Test]
        public void TestCallback()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (EndDevice slave = al.CreateEndDevice())
                {
                    byte counter = 0;
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    slave.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));

                    slave.ResponseData((x, a, b, c) =>
                    {
                        cmd_report[2] = counter;
                        return cmd_report;
                    }, TXO, cmd_get);

                    for (int i = 0; i < POW; i++)
                    {
                        RequestDataResult res = ctrl.RequestData(new NodeTag(0x02), cmd_get, TXO, cmd_report, REQUEST_DATA_TIMEOUT);
                        Assert.AreEqual(ActionStates.Completed, res.State);
                        Assert.AreEqual(counter, res.Command[2]);
                        counter++;
                    }
                }
            }
        }
    }
}
