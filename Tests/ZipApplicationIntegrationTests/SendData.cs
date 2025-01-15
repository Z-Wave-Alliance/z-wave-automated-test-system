// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.Layers.Session;
using ZWave.Layers.Transport;
using ZWave.Enums;
using ZWave;
using ZWave.BasicApplication.Operations;
using ZWave.Layers;
using System.Threading;
using ZWave.ZipApplication;
using ZWave.ZipApplication.Devices;
using ZWave.ZipApplication.Operations;
using ZWave.CommandClasses;
using System.Net;
using ZWave.BasicApplication.Enums;

namespace ZipApplicationIntegrationTests
{
    [TestFixture]
    public class SendData : ZipTestsBase
    {
        int POW = 10;

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

                        slave.ApplicationNodeInformation(true, 0x11, 00, new byte[] { COMMAND_CLASS_BASIC.ID });
                        ctrl.ApplicationNodeInformation(true, 0x02, 0x01, new byte[] { COMMAND_CLASS_BASIC.ID });

                        ActionToken addToken = zip.AddNodeToNetwork(Modes.NodeAny, TXO, 10000, null);
                        ActionToken learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, 10000, null);

                        Assert.IsTrue(addToken.WaitCompletedSignal());
                        Assert.IsTrue(learnToken.WaitCompletedSignal());
                    }
                }
            }
        }

        [Test]
        public void TestSync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            ZipApplicationLayer zal = new ZipApplicationLayer(new SessionLayer(), new ZipFrameLayer(), new UdpClientTransportLayer());
            using (ZipController zip = zal.CreateZipController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    zip.Connect(new SocketDataSource(ZIP_ADDR, ZIP_UDP_PORT));
                    slave.Connect(new SerialPortDataSource(COM2, BaudRates.Rate_115200));

                    GetIpAddressResult ipres = zip.GetIpAddress(0x02);
                    using (ZipDevice zslave = zal.CreateZipDevice())
                    {
                        Thread.Sleep(2000);
                        zslave.Connect(new SocketDataSource(ipres.IpAddress.ToString(), ZIP_UDP_PORT));
                        for (int i = 0; i < POW; i++)
                        {
                            var res = zslave.SendData(null, new COMMAND_CLASS_BASIC.BASIC_GET());
                            Assert.IsTrue(res);
                            Assert.IsTrue(res.IsAckReceived);
                        }
                    }
                }
            }
        }

        [Test]
        public void TestFailedSync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            ZipApplicationLayer zal = new ZipApplicationLayer(new SessionLayer(), new ZipFrameLayer(), new UdpClientTransportLayer());
            using (ZipController zip = zal.CreateZipController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    zip.Connect(new SocketDataSource(ZIP_ADDR, ZIP_UDP_PORT));
                    slave.Connect(new SerialPortDataSource(COM2, BaudRates.Rate_115200));

                    GetIpAddressResult ipres = zip.GetIpAddress(0x02);
                    using (ZipDevice zslave = zal.CreateZipDevice())
                    {
                        Thread.Sleep(2000);
                        zslave.Connect(new SocketDataSource(ipres.IpAddress.ToString(), ZIP_UDP_PORT));
                        slave.SetRFReceiveMode(0x00);
                        var res = zslave.SendData(null, new COMMAND_CLASS_BASIC.BASIC_GET());
                        Assert.IsTrue(res);
                        Assert.IsTrue(res.IsNAckReceived);
                        slave.SetRFReceiveMode(0x01);
                    }
                }
            }
        }
    }
}
