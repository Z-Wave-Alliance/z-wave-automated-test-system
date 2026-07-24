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
    public class RequestData : BasicTestsBase
    {
        byte[] cmd_get = new byte[] { 0x20, 0x02 };
        byte[] cmd_report = new byte[] { 0x20, 0x03, 0x55 };


        byte[] cmd_get1 = new byte[] { 0x20, 0x11, 0x01 };
        byte[] cmd_get2 = new byte[] { 0x20, 0x11, 0x02 };
        byte[] cmd_get3 = new byte[] { 0x20, 0x11, 0x03 };
        byte[] cmd_get4 = new byte[] { 0x20, 0x11, 0x04 };
        byte[] cmd_get5 = new byte[] { 0x20, 0x11, 0x05 };
        byte[] cmd_report1 = new byte[] { 0x20, 0x13, 0x01 };
        byte[] cmd_report2 = new byte[] { 0x20, 0x13, 0x02 };
        byte[] cmd_report3 = new byte[] { 0x20, 0x13, 0x03 };
        byte[] cmd_report4 = new byte[] { 0x20, 0x13, 0x04 };
        byte[] cmd_report5 = new byte[] { 0x20, 0x13, 0x05 };

        int POW = 10;

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
                    Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || res.Library == Libraries.InstallerLib || res.Library == Libraries.ControllerPortableLib || res.Library == Libraries.ControllerBridgeLib);
                    res = slave.GetVersion();
                    Assert.IsTrue(res.State == ActionStates.Completed);
                    Assert.IsTrue(res.Library == Libraries.EndDeviceLegacyLib || res.Library == Libraries.EndDeviceLib || res.Library == Libraries.EndDeviceLegacyRoutingLib);
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
        public void TestExpiredAsync()
        {
            int errors = 0;
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (EndDevice slave = al.CreateEndDevice())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    slave.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));
                    ActionToken requestToken = null;
                    for (int i = 0; i < POW; i++)
                    {
                        requestToken = slave.RequestData(new NodeTag(0x01), cmd_get, TXO, cmd_report, 1000, null);
                        ActionResult res = requestToken.WaitCompletedSignal();
                    }

                }
            }
            Assert.AreEqual(0, errors);
        }
    }
}
