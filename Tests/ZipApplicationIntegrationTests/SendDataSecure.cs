// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Linq;
using System.Net;
using NUnit.Framework;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Operations;
using ZWave.BasicApplication.Security;
using ZWave.CommandClasses;
using ZWave.Enums;
using ZWave.Layers;
using ZWave.Layers.Session;
using ZWave.Layers.Transport;
using ZWave.ZipApplication;
using ZWave.ZipApplication.Devices;
using ZWave.ZipApplication.Operations;

namespace ZipApplicationIntegrationTests
{
    [TestFixture]
    public class SendDataSecure : ZipTestsBase
    {
        int POW = 10;
        byte[] cmd_get = new byte[] { 0x20, 0x02 };
        byte[] cmd_report = new byte[] { 0x20, 0x03, 0x055 };

        [SetUp]
        public void SetUp()
        {
            BasicApplicationLayer bal = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            ZipApplicationLayer zal = new ZipApplicationLayer(new SessionLayer(), new ZipFrameLayer(), new UdpClientTransportLayer());
            using (Controller ctrl = bal.CreateController())
            {
                using (Slave slave = bal.CreateSlave())
                {
                    using (ZipController zip = zal.CreateZipController())
                    {
                        ctrl.Connect(new SerialPortDataSource(COM1, BaudRates.Rate_115200));
                        slave.Connect(new SerialPortDataSource(COM2, BaudRates.Rate_115200));
                        zip.Connect(new SocketDataSource(ZIP_ADDR, ZIP_UDP_PORT));
                        VersionResult res = ctrl.GetVersion();
                        Assert.IsTrue(res.State == ActionStates.Completed);
                        Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || res.Library == Libraries.InstallerLib || res.Library == Libraries.ControllerPortableLib || res.Library == Libraries.ControllerBridgeLib);
                        res = slave.GetVersion();
                        Assert.IsTrue(res.State == ActionStates.Completed);
                        Assert.IsTrue(res.Library == Libraries.SlaveEnhancedLib || res.Library == Libraries.SlaveLib || res.Library == Libraries.SlaveRoutingLib);
                        GetIdResult gres = zip.GetId();
                        Assert.AreEqual(ActionStates.Completed, gres.State);

                        ActionResult ares = ctrl.SetDefault();
                        Assert.IsTrue(ares.State == ActionStates.Completed);
                        ares = slave.SetDefault();
                        Assert.IsTrue(ares.State == ActionStates.Completed);
                        ares = zip.SetDefault(10000);
                        Assert.IsTrue(ares.State == ActionStates.Completed);

                        slave.ApplicationNodeInformation(true, 0x11, 00, new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_SECURITY.ID });
                        ctrl.ApplicationNodeInformation(true, 0x02, 0x01, new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_SECURITY.ID });

                        SecurityManager sm2 = CreateSecurityManager(slave);
                        slave.SessionClient.AddSubstituteManager(sm2);
                        slave.SessionClient.ExecuteAsync(sm2.CreateSecurityReportTask());

                        ActionToken addToken = zip.AddNodeToNetwork(Modes.NodeAny, TXO, 10000, null);
                        ActionToken learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, 10000, null);

                        networkKeys[SecurityManagerInfo.GetNetworkKeyIndex(SecuritySchemes.S0)] = sm2.SecurityManagerInfo.GetActualNetworkKey(ZWave.Enums.SecuritySchemes.S0);

                        Assert.IsTrue(addToken.WaitCompletedSignal());
                        Assert.IsTrue(learnToken.WaitCompletedSignal());
                    }
                }
            }
        }

        private byte[][] networkKeys = new byte[8][];

        [Test]
        public void TestAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            ZipApplicationLayer zal = new ZipApplicationLayer(new SessionLayer(), new ZipFrameLayer(), new UdpClientTransportLayer());
            using (ZipController zip = zal.CreateZipController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    zip.Connect(new SocketDataSource(ZIP_ADDR, ZIP_UDP_PORT));
                    slave.Connect(new SerialPortDataSource(COM2, BaudRates.Rate_115200));
                    SecurityManager sm2 = CreateSecurityManager(slave);
                    slave.SessionClient.AddSubstituteManager(sm2);
                    slave.SessionClient.ExecuteAsync(sm2.CreateSecurityReportTask());

                    GetIpAddressResult ipres = zip.GetIpAddress(0x02);
                    using (ZipDevice zslave = zal.CreateZipDevice())
                    {
                        zslave.Connect(new SocketDataSource(ipres.IpAddress.ToString(), ZIP_UDP_PORT));
                        ActionToken sendToken = null;
                        for (int i = 0; i < POW; i++)
                        {
                            sendToken = zslave.SendData(null, cmd_get, null);
                            ActionResult res = sendToken.WaitCompletedSignal();
                            Assert.AreEqual(ActionStates.Completed, res.State);
                        }
                    }
                }
            }
        }

        [Test]
        public void TestFailedSync()
        {
            ZipApplicationLayer zal = new ZipApplicationLayer(new SessionLayer(), new ZipFrameLayer(), new UdpClientTransportLayer());
            using (ZipController zip = zal.CreateZipController())
            {
                zip.Connect(new SocketDataSource(ZIP_ADDR, ZIP_UDP_PORT));
                GetIpAddressResult ipres = zip.GetIpAddress(0x03);
                using (ZipDevice zslave = zal.CreateZipDevice())
                {
                    zslave.Connect(new SocketDataSource(ipres.IpAddress.ToString(), ZIP_UDP_PORT));
                    ActionToken sendToken = null;
                    sendToken = zslave.SendData(null, cmd_get, null);
                    ActionResult res = sendToken.WaitCompletedSignal();
                    Assert.AreEqual(ActionStates.Failed, res.State);
                }
            }
        }
    }
}
