// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Linq;
using System.Net;
using System.Threading;
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
using ZWave.Devices;

namespace ZipApplicationIntegrationTests
{
    [TestFixture]
    public class SendDataSecureStress : ZipTestsBase
    {
        int POW = 1000;
        int TIMEOUT = 10000;
        byte[] cmd_get = new byte[] { 0x20, 0x02 };
        byte[] cmd_report = new byte[] { 0x20, 0x03, 0x055 };

        [SetUp]
        public void SetUp()
        {
            BasicApplicationLayer bal = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            ZipApplicationLayer zal = new ZipApplicationLayer(new SessionLayer(), new ZipFrameLayer(), new UdpClientTransportLayer());
            using (Controller ctrl = bal.CreateController())
            {
                using (EndDevice slave = bal.CreateEndDevice())
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
                        Assert.IsTrue(res.Library == Libraries.EndDeviceLegacyLib || res.Library == Libraries.EndDeviceLib || res.Library == Libraries.EndDeviceLegacyRoutingLib);
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

                        SecurityManager sm1 = CreateSecurityManager(ctrl);
                        ctrl.SessionClient.AddSubstituteManager(sm1);
                        ctrl.SessionClient.ExecuteAsync(sm1.CreateSecurityReportTask());

                        SecurityManager sm2 = CreateSecurityManager(slave);
                        slave.SessionClient.AddSubstituteManager(sm2);
                        slave.SessionClient.ExecuteAsync(sm2.CreateSecurityReportTask());

                        ActionToken addToken = zip.AddNodeToNetwork(Modes.NodeAny, TXO, 10000, null);
                        ActionToken learnToken = ctrl.SetLearnMode(LearnModes.LearnModeClassic, 10000, null);

                        Assert.IsTrue(addToken.WaitCompletedSignal());
                        Assert.IsTrue(learnToken.WaitCompletedSignal());

                        networkKeys[SecurityManagerInfo.GetNetworkKeyIndex(SecuritySchemes.S0, false)] = sm2.SecurityManagerInfo.GetActualNetworkKey(ZWave.Enums.SecuritySchemes.S0, false);

                        addToken = zip.AddNodeToNetwork(Modes.NodeAny, TXO, 10000, null);
                        learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, 10000, null);

                        Assert.IsTrue(addToken.WaitCompletedSignal());
                        Assert.IsTrue(learnToken.WaitCompletedSignal());

                        networkKeys[SecurityManagerInfo.GetNetworkKeyIndex(SecuritySchemes.S0, false)] = sm2.SecurityManagerInfo.GetActualNetworkKey(ZWave.Enums.SecuritySchemes.S0, false);
                    }
                }
            }
        }

        private byte[][] networkKeys = new byte[8][];

        [Test]
        public void TestRequestOneNetwork()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            ZipApplicationLayer zal = new ZipApplicationLayer(new SessionLayer(), new ZipFrameLayer(), new UdpClientTransportLayer());
            using (ZipController zip = zal.CreateZipController())
            {
                using (Controller ctrl = al.CreateController())
                {
                    using (EndDevice slave = al.CreateEndDevice())
                    {
                        zip.Connect(new SocketDataSource(ZIP_ADDR, ZIP_UDP_PORT));
                        ctrl.Connect(new SerialPortDataSource(COM1, BaudRates.Rate_115200));
                        slave.Connect(new SerialPortDataSource(COM2, BaudRates.Rate_115200));
                        
                        SecurityManager sm1 = CreateSecurityManager(ctrl);
                        ctrl.SessionClient.AddSubstituteManager(sm1);
                        ctrl.SessionClient.ExecuteAsync(sm1.CreateSecurityReportTask());

                        SecurityManager sm2 = CreateSecurityManager(slave);
                        slave.SessionClient.AddSubstituteManager(sm2);
                        slave.SessionClient.ExecuteAsync(sm2.CreateSecurityReportTask());

                        GetIpAddressResult ipctrl = zip.GetIpAddress(new NodeTag(0x02));
                        GetIpAddressResult ipslave = zip.GetIpAddress(new NodeTag(0x03));
                        using (ZipDevice zctrl = zal.CreateZipDevice())
                        {
                            using (ZipDevice zslave = zal.CreateZipDevice())
                            {
                                zctrl.Connect(new SocketDataSource(ipctrl.IpAddress.ToString(), ZIP_UDP_PORT));
                                zslave.Connect(new SocketDataSource(ipslave.IpAddress.ToString(), ZIP_UDP_PORT));

                                slave.ResponseData(new COMMAND_CLASS_BASIC.BASIC_REPORT(), TXO, new COMMAND_CLASS_BASIC.BASIC_GET());
                                for (int i = 0; i < POW; i++)
                                {
                                    ActionResult res = zslave.RequestData(null, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT(), TIMEOUT);
                                    Assert.AreEqual(ActionStates.Completed, res.State);
                                }
                            }
                        }
                    }
                }
            }
        }

        [Test]
        public void TestRequestOneNetwork2()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            ZipApplicationLayer zal = new ZipApplicationLayer(new SessionLayer(), new ZipFrameLayer(), new UdpClientTransportLayer());
            using (ZipController zip = zal.CreateZipController())
            {
                using (Controller ctrl = al.CreateController())
                {
                    using (EndDevice slave = al.CreateEndDevice())
                    {
                        int errors = 0;
                        zip.Connect(new SocketDataSource(ZIP_ADDR, ZIP_UDP_PORT));
                        ctrl.Connect(new SerialPortDataSource(COM1, BaudRates.Rate_115200));
                        slave.Connect(new SerialPortDataSource(COM2, BaudRates.Rate_115200));

                        SecurityManager sm1 = CreateSecurityManager(ctrl);
                        ctrl.SessionClient.AddSubstituteManager(sm1);
                        ctrl.SessionClient.ExecuteAsync(sm1.CreateSecurityReportTask());

                        SecurityManager sm2 = CreateSecurityManager(slave);
                        slave.SessionClient.AddSubstituteManager(sm2);
                        slave.SessionClient.ExecuteAsync(sm2.CreateSecurityReportTask());

                        GetIpAddressResult ipctrl = zip.GetIpAddress(new NodeTag(0x02));
                        GetIpAddressResult ipslave = zip.GetIpAddress(new NodeTag(0x03));
                        using (ZipDevice zctrl = zal.CreateZipDevice())
                        {
                            using (ZipDevice zslave = zal.CreateZipDevice())
                            {
                                zctrl.Connect(new SocketDataSource(ipctrl.IpAddress.ToString(), ZIP_UDP_PORT));
                                zslave.Connect(new SocketDataSource(ipslave.IpAddress.ToString(), ZIP_UDP_PORT));

                                slave.ResponseData(new COMMAND_CLASS_BASIC.BASIC_REPORT(), TXO, new COMMAND_CLASS_BASIC.BASIC_GET());
                                ctrl.ResponseData(new COMMAND_CLASS_BASIC.BASIC_REPORT(), TXO, new COMMAND_CLASS_BASIC.BASIC_GET());

                                Thread t1 = new Thread(() =>
                                {
                                    for (int i = 0; i < POW && errors == 0; i++)
                                    {
                                        ActionResult res = zctrl.RequestData(null, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT(), TIMEOUT);
                                        if (res.State == ActionStates.Failed)
                                        {
                                            Interlocked.Increment(ref errors);
                                        }
                                    }
                                });
                                Thread t2 = new Thread(() =>
                                {
                                    for (int i = 0; i < POW && errors == 0; i++)
                                    {
                                        ActionResult res = zslave.RequestData(null, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT(), TIMEOUT);
                                        if (res.State == ActionStates.Failed)
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
}
