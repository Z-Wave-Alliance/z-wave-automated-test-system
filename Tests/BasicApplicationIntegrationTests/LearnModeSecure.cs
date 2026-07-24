// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ZWave.Layers.Transport;
using ZWave.Layers;
using ZWave.Enums;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Operations;
using ZWave.Layers.Session;
using ZWave;
using ZWave.BasicApplication.Enums;
using System.Threading;

namespace BasicApplicationIntegrationTests
{
    [TestFixture]
    public class LearnModeSecure : BasicTestsBase
    {
        [SetUp]
        public void SetUp()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl1 = al.CreateController())
            {
                using (EndDevice slave1 = al.CreateEndDevice())
                {
                    using (Controller ctrl2 = al.CreateController())
                    {
                        using (EndDevice slave2 = al.CreateEndDevice())
                        {
                            ctrl1.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                            slave1.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));
                            ctrl2.Connect(new SerialPortDataSource(COM_CTRL_STATIC_2, BaudRates.Rate_115200));
                            slave2.Connect(new SerialPortDataSource(COM_SLAVE_2, BaudRates.Rate_115200));
                            VersionResult res = ctrl1.GetVersion();
                            Assert.IsTrue(res.State == ActionStates.Completed);
                            Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || 
                                res.Library == Libraries.InstallerLib || 
                                res.Library == Libraries.ControllerPortableLib || 
                                res.Library == Libraries.ControllerBridgeLib);
                            res = slave1.GetVersion();
                            Assert.IsTrue(res.State == ActionStates.Completed);
                            Assert.IsTrue(res.Library == Libraries.EndDeviceLegacyLib || 
                                res.Library == Libraries.EndDeviceLib || 
                                res.Library == Libraries.EndDeviceLegacyRoutingLib);
                            
                            res = ctrl2.GetVersion();
                            Assert.IsTrue(res.State == ActionStates.Completed);
                            Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || 
                                res.Library == Libraries.InstallerLib || 
                                res.Library == Libraries.ControllerPortableLib || 
                                res.Library == Libraries.ControllerBridgeLib);
                            res = slave2.GetVersion();
                            Assert.IsTrue(res.State == ActionStates.Completed);
                            Assert.IsTrue(res.Library == Libraries.EndDeviceLegacyLib || 
                                res.Library == Libraries.EndDeviceLib || 
                                res.Library == Libraries.EndDeviceLegacyRoutingLib);

                            ActionResult ares = ctrl1.SetDefault();
                            Assert.IsTrue(ares.State == ActionStates.Completed);
                            ares = slave1.SetDefault();
                            Assert.IsTrue(ares.State == ActionStates.Completed);
                            ares = ctrl2.SetDefault();
                            Assert.IsTrue(ares.State == ActionStates.Completed);
                            ares = slave2.SetDefault();
                            Assert.IsTrue(ares.State == ActionStates.Completed);
                        }
                    }
                }
            }
        }

        [Test]
        public void TestSlaveExpiredAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (EndDevice slave = al.CreateEndDevice())
                {
                    int errors = 0;
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    slave.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));
                    //SetupSecurity(slave, true);

                    ActionToken learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);

                    Thread.Sleep(INCLUSION_TIMEOUT + 2000);
                    MemoryGetIdResult memRes = slave.MemoryGetId();
                    slave.SetLearnMode(LearnModes.LearnModeDisable, 2000);

                    if (!learnToken.WaitCompletedSignal())
                    {
                        errors++;
                    }

                    MemoryGetIdResult memres = ctrl.MemoryGetId();
                    Assert.AreEqual(1, memres.Node.Id);
                    memres = slave.MemoryGetId();
                    Assert.AreEqual(0, memres.Node.Id);
                    Assert.AreEqual(0, errors);
                }
            }
        }

       
    }
}
