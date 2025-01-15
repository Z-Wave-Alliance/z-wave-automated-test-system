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

namespace BasicApplicationIntegrationTests
{
    [TestFixture]
    public class NoiseData : BasicTestsBase
    {
        private byte[] cmd_get = new byte[] { 0x20, 0x02 };
        private byte[] cmd_report = new byte[] { 0x20, 0x03, 0x55 };

        private const int SEND_INTERVAL = 200;
        private const int NOISE_DURATION = 3000;
        private const int EXPECTED_NOISE = NOISE_DURATION / SEND_INTERVAL - 5;

        private void SetupNetwork(Controller ctrl, Slave slave, bool secure)
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
            Assert.IsTrue(res.Library == Libraries.SlaveEnhancedLib ||
                res.Library == Libraries.SlaveLib ||
                res.Library == Libraries.SlaveRoutingLib);
            ActionResult ares = ctrl.SetDefault();
            Assert.IsTrue(ares.State == ActionStates.Completed);
            ares = slave.SetDefault();
            Assert.IsTrue(ares.State == ActionStates.Completed);

            if (secure)
            {
                SetupSecurity(ctrl, true);
            }
            ActionToken addToken = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
            ActionToken learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);

            Assert.IsTrue(addToken.WaitCompletedSignal());
            Assert.IsTrue(learnToken.WaitCompletedSignal());
        }

        [Test]
        public void TestNoReportAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    SetupNetwork(ctrl, slave, false);
                    ActionToken noiseToken = ctrl.NoiseData(0x02, cmd_get, TXO, null, SEND_INTERVAL, 0);
                    Thread.Sleep(NOISE_DURATION);
                    ctrl.Cancel(noiseToken);
                    NoiseDataResult noiseRes = (NoiseDataResult)noiseToken.WaitCompletedSignal();
                    Assert.GreaterOrEqual(noiseRes.TotalCount, EXPECTED_NOISE);
                }
            }
        }

        [Test]
        public void TestWithReportAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    SetupNetwork(ctrl, slave, false);
                    ActionToken responseToken = slave.ResponseData(cmd_report, TXO, cmd_get);
                    ActionToken noiseToken = ctrl.NoiseData(0x02, cmd_get, TXO, cmd_report, SEND_INTERVAL, 2000);
                    Thread.Sleep(NOISE_DURATION);
                    ctrl.Cancel(noiseToken);
                    NoiseDataResult noiseRes = (NoiseDataResult)noiseToken.WaitCompletedSignal();
                    slave.Cancel(responseToken);
                    ResponseDataResult respRes = (ResponseDataResult)responseToken.WaitCompletedSignal();
                    Assert.IsTrue(Math.Abs(respRes.TotalCount - noiseRes.TotalCount) <= 1);
                }
            }
        }

        [Test]
        public void NoiseDataExTestNoReportAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    SetupNetwork(ctrl, slave, true);
                    ActionToken noiseToken = ctrl.NoiseDataEx(0x02, cmd_get, TXO, null, SEND_INTERVAL, 2000, 
                        SecuritySchemes.S2_AUTHENTICATED, TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY, TransmitOptions2.NONE);
                    Thread.Sleep(NOISE_DURATION);
                    ctrl.Cancel(noiseToken);
                    NoiseDataResult noiseRes = (NoiseDataResult)noiseToken.WaitCompletedSignal();
                    Assert.GreaterOrEqual(noiseRes.TotalCount, EXPECTED_NOISE);
                }
            }
        }

        [Test]
        public void NoiseDataExTestWithReportAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    SetupNetwork(ctrl, slave, true);
                    ActionToken responseToken = slave.ResponseDataEx(cmd_report, TXO, cmd_get);
                    ActionToken noiseToken = ctrl.NoiseDataEx(0x02, cmd_get, TXO, cmd_report, SEND_INTERVAL, 2000,
                        SecuritySchemes.S2_AUTHENTICATED, TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY, TransmitOptions2.NONE);
                    Thread.Sleep(NOISE_DURATION);
                    ctrl.Cancel(noiseToken);
                    NoiseDataResult noiseRes = (NoiseDataResult)noiseToken.WaitCompletedSignal();
                    slave.Cancel(responseToken);
                    ResponseDataResult respRes = (ResponseDataResult)responseToken.WaitCompletedSignal();
                    Assert.IsTrue(Math.Abs(respRes.TotalCount - noiseRes.TotalCount) <= 1);
                }
            }
        }
    }
}
