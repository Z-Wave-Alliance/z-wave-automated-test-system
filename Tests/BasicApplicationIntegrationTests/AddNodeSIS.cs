// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BasicApplicationIntegrationTests;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using NUnit.Framework;
using ZWave.Layers.Session;
using ZWave.Layers;
using ZWave.BasicApplication.Operations;
using ZWave;
using ZWave.BasicApplication.Enums;
using ZWave.Enums;
using ZWave.Layers.Transport;
using System.Threading;
using ZWave.Devices;

namespace BasicApplicationIntegrationTests
{
    [TestFixture]
    public class AddNodeSIS : BasicTestsBase
    {
        int POW = 3;
        [SetUp]
        public void SetUp()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl1 = al.CreateController())
            {
                using (Controller ctrl2 = al.CreateController())
                {
                    using (EndDevice slave1 = al.CreateEndDevice())
                    {
                        using (EndDevice slave2 = al.CreateEndDevice())
                        {
                            using (EndDevice slave3 = al.CreateEndDevice())
                            {
                                ctrl1.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                                ctrl2.Connect(new SerialPortDataSource(COM_CTRL_STATIC_2, BaudRates.Rate_115200));
                                slave1.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));
                                slave2.Connect(new SerialPortDataSource(COM_SLAVE_2, BaudRates.Rate_115200));
                                slave3.Connect(new SerialPortDataSource(COM_SLAVE_3, BaudRates.Rate_115200));
                                VersionResult verRes = ctrl1.GetVersion();
                                Assert.AreEqual(ActionStates.Completed, verRes.State);
                                Assert.AreEqual(Libraries.ControllerStaticLib, verRes.Library);
                                verRes = ctrl2.GetVersion();
                                Assert.AreEqual(ActionStates.Completed, verRes.State);
                                Assert.AreEqual(Libraries.ControllerStaticLib, verRes.Library);
                                verRes = slave1.GetVersion();
                                Assert.AreEqual(ActionStates.Completed, verRes.State);
                                Assert.AreEqual(Libraries.EndDeviceLegacyLib, verRes.Library);
                                verRes = slave2.GetVersion();
                                Assert.AreEqual(ActionStates.Completed, verRes.State);
                                Assert.AreEqual(Libraries.EndDeviceLegacyLib, verRes.Library);
                                verRes = slave3.GetVersion();
                                Assert.AreEqual(ActionStates.Completed, verRes.State);
                                Assert.AreEqual(Libraries.EndDeviceLegacyLib, verRes.Library);

                                ActionResult ares = ctrl1.SetDefault();
                                Assert.AreEqual(ActionStates.Completed, ares.State);
                                ctrl1.SoftReset();
                                ares = ctrl2.SetDefault();
                                Assert.AreEqual(ActionStates.Completed, ares.State);
                                ctrl2.SoftReset();
                                ares = slave1.SetDefault();
                                Assert.AreEqual(ActionStates.Completed, ares.State);
                                slave1.SoftReset();
                                ares = slave2.SetDefault();
                                Assert.AreEqual(ActionStates.Completed, ares.State);
                                slave2.SoftReset();
                                ares = slave3.SetDefault();
                                Assert.AreEqual(ActionStates.Completed, ares.State);
                                slave3.SoftReset();
                                Thread.Sleep(2000); // After soft reset.
                            }
                        }
                    }
                }
            }
        }

        [Test]
        public void TestAddNodeSIS()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl1 = al.CreateController())
            {
                using (Controller ctrl2 = al.CreateController())
                {
                    using (EndDevice slave1 = al.CreateEndDevice())
                    {
                        using (EndDevice slave2 = al.CreateEndDevice())
                        {
                            using (EndDevice slave3 = al.CreateEndDevice())
                            {
                                ctrl1.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                                ctrl2.Connect(new SerialPortDataSource(COM_CTRL_STATIC_2, BaudRates.Rate_115200));
                                slave1.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));
                                slave2.Connect(new SerialPortDataSource(COM_SLAVE_2, BaudRates.Rate_115200));
                                slave3.Connect(new SerialPortDataSource(COM_SLAVE_3, BaudRates.Rate_115200));

                                var addToken = ctrl1.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                                var learnToken = ctrl2.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);
                                var addResult = addToken.WaitCompletedSignal();
                                var learnResult = learnToken.WaitCompletedSignal();
                                Assert.IsTrue(addResult);
                                Assert.IsTrue(learnResult);
                                Assert.AreEqual(2, ((AddRemoveNodeResult)addResult).Id);
                                Assert.AreEqual(2, ((SetLearnModeResult)learnResult).Node.Id);

                                ctrl1.SetSucNodeID(new NodeTag(ctrl2.Id), true, false, 0x01);

                                for (int i = 0; i < POW; i++)
                                {
                                    addToken = ctrl2.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                                    learnToken = slave1.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);
                                    addResult = addToken.WaitCompletedSignal();
                                    learnResult = learnToken.WaitCompletedSignal();
                                    Assert.IsTrue(addResult);
                                    Assert.IsTrue(learnResult);
                                    Assert.AreEqual(3 * i + 3, ((AddRemoveNodeResult)addResult).Id);
                                    Assert.AreEqual(3 * i + 3, ((SetLearnModeResult)learnResult).Node.Id);

                                    ctrl1.SetRFReceiveMode(0);

                                    addToken = ctrl2.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                                    learnToken = slave2.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);
                                    addResult = addToken.WaitCompletedSignal();
                                    learnResult = learnToken.WaitCompletedSignal();
                                    Assert.IsTrue(addResult);
                                    Assert.IsTrue(learnResult);
                                    Assert.AreEqual(3 * i + 4, ((AddRemoveNodeResult)addResult).Id);
                                    Assert.AreEqual(3 * i + 4, ((SetLearnModeResult)learnResult).Node.Id);

                                    addToken = ctrl2.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                                    learnToken = slave3.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);
                                    addResult = addToken.WaitCompletedSignal();
                                    learnResult = learnToken.WaitCompletedSignal();
                                    Assert.IsTrue(addResult);
                                    Assert.IsTrue(learnResult);
                                    Assert.AreEqual(3 * i + 5, ((AddRemoveNodeResult)addResult).Id);
                                    Assert.AreEqual(3 * i + 5, ((SetLearnModeResult)learnResult).Node.Id);

                                    ctrl1.SetRFReceiveMode(1);


                                    addToken = ctrl2.RemoveNodeFromNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                                    learnToken = slave1.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);
                                    addResult = addToken.WaitCompletedSignal();
                                    learnResult = learnToken.WaitCompletedSignal();
                                    Assert.IsTrue(addResult);
                                    Assert.IsTrue(learnResult);
                                    Assert.AreEqual(3 * i + 3, ((AddRemoveNodeResult)addResult).Id);
                                    Assert.AreEqual(0, ((SetLearnModeResult)learnResult).Node.Id);

                                    addToken = ctrl2.RemoveNodeFromNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                                    learnToken = slave2.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);
                                    addResult = addToken.WaitCompletedSignal();
                                    learnResult = learnToken.WaitCompletedSignal();
                                    Assert.IsTrue(addResult);
                                    Assert.IsTrue(learnResult);
                                    Assert.AreEqual(3 * i + 4, ((AddRemoveNodeResult)addResult).Id);
                                    Assert.AreEqual(0, ((SetLearnModeResult)learnResult).Node.Id);

                                    addToken = ctrl2.RemoveNodeFromNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                                    learnToken = slave3.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);
                                    addResult = addToken.WaitCompletedSignal();
                                    learnResult = learnToken.WaitCompletedSignal();
                                    Assert.IsTrue(addResult);
                                    Assert.IsTrue(learnResult);
                                    Assert.AreEqual(3 * i + 5, ((AddRemoveNodeResult)addResult).Id);
                                    Assert.AreEqual(0, ((SetLearnModeResult)learnResult).Node.Id);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
