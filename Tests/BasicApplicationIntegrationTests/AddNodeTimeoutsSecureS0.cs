// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using NUnit.Framework;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Operations;
using ZWave.BasicApplication.Security;
using ZWave.Enums;
using ZWave.Layers;
using ZWave.Layers.Session;
using ZWave.Layers.Transport;

namespace BasicApplicationIntegrationTests
{
    [TestFixture]
    public class AddNodeTimeoutsSecureS0 : BasicTestsBase
    {
        const int ADD_TIMEOUT = 60000;
        const int POW = 10;

        [SetUp]
        public void SetUp()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl1 = al.CreateController())
            {
                using (Controller ctrl2 = al.CreateController())
                {
                    ctrl1.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    ctrl2.Connect(new SerialPortDataSource(COM_CTRL_STATIC_2, BaudRates.Rate_115200));
                    VersionResult res = ctrl1.GetVersion();
                    Assert.IsTrue(res.State == ActionStates.Completed);
                    Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || 
                        res.Library == Libraries.InstallerLib || 
                        res.Library == Libraries.ControllerPortableLib || 
                        res.Library == Libraries.ControllerBridgeLib);

                    res = ctrl2.GetVersion();
                    Assert.IsTrue(res.State == ActionStates.Completed);
                    Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || 
                        res.Library == Libraries.InstallerLib || 
                        res.Library == Libraries.ControllerPortableLib || 
                        res.Library == Libraries.ControllerBridgeLib);

                    ActionResult ares = ctrl1.SetDefault();
                    Assert.IsTrue(ares.State == ActionStates.Completed);
                    ares = ctrl2.SetDefault();
                    Assert.IsTrue(ares.State == ActionStates.Completed);
                }
            }
        }

        [TestCase(SecurityS0Delays.SchemeGet, null)]
        [TestCase(null, SecurityS0Delays.SchemeReport)]
        [TestCase(SecurityS0Delays.NetworkKeySet, null)]
        [TestCase(null, SecurityS0Delays.NetworkKeyVerify)]
        [TestCase(SecurityS0Delays.SchemeInherit, null)]
        [TestCase(null, SecurityS0Delays.SchemeReportEnc)]
        [TestCase(SecurityS0Delays.NonceGet, null)]
        public void TestInclusionTimeout(SecurityS0Delays? addDelay, SecurityS0Delays? learnDelay)
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl1 = al.CreateController())
            {
                using (Controller ctrl2 = al.CreateController())
                {
                    ctrl1.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    SetupSecurityS0Only(ctrl1, false);
                    ctrl2.Connect(new SerialPortDataSource(COM_CTRL_STATIC_2, BaudRates.Rate_115200));
                    SetupSecurityS0Only(ctrl2, false);

                    var sm1 = (SecurityManager)ctrl1.SessionClient.GetSubstituteManager(typeof(SecurityManager));
                    var sm2 = (SecurityManager)ctrl2.SessionClient.GetSubstituteManager(typeof(SecurityManager));

                    if (addDelay != null)
                    {
                        sm1.SecurityManagerInfo.SetDelayS0((SecurityS0Delays)addDelay, 0);
                    }
                    if (learnDelay != null)
                    {
                        sm2.SecurityManagerInfo.SetDelayS0((SecurityS0Delays)learnDelay, 0);
                    }
                    ActionToken addToken = ctrl1.AddNodeToNetwork(Modes.NodeAny, ADD_TIMEOUT, null);
                    ActionToken learnToken = ctrl2.SetLearnMode(LearnModes.LearnModeClassic, ADD_TIMEOUT, null);
                    AddRemoveNodeResult addRes = (AddRemoveNodeResult)addToken.WaitCompletedSignal();
                    Assert.IsTrue(addRes.IsStateCompleted);
                    Assert.AreEqual(SubstituteStatuses.Done, addRes.SubstituteStatus);
                    SetLearnModeResult learnRes = (SetLearnModeResult)learnToken.WaitCompletedSignal();
                    Assert.IsTrue(learnRes.IsStateCompleted);
                    Assert.AreEqual(SubstituteStatuses.Done, learnRes.SubstituteStatus);
                    Assert.IsTrue(ctrl1.SetDefault().IsStateCompleted);
                    Assert.IsTrue(ctrl2.SetDefault().IsStateCompleted);

                    if (addDelay != null)
                    {
                        sm1.SecurityManagerInfo.SetDelayS0((SecurityS0Delays)addDelay, 5000);
                    }
                    if (learnDelay != null)
                    {
                        sm2.SecurityManagerInfo.SetDelayS0((SecurityS0Delays)learnDelay, 5000);
                    }
                    addToken = ctrl1.AddNodeToNetwork(Modes.NodeAny, ADD_TIMEOUT, null);
                    learnToken = ctrl2.SetLearnMode(LearnModes.LearnModeClassic, ADD_TIMEOUT, null);
                    addRes = (AddRemoveNodeResult)addToken.WaitCompletedSignal();
                    Assert.IsTrue(addRes.IsStateCompleted);
                    Assert.AreEqual(SubstituteStatuses.Done, addRes.SubstituteStatus);
                    learnRes = (SetLearnModeResult)learnToken.WaitCompletedSignal();
                    Assert.IsTrue(learnRes.IsStateCompleted);
                    Assert.AreEqual(SubstituteStatuses.Done, learnRes.SubstituteStatus);
                    Assert.IsTrue(ctrl1.SetDefault().IsStateCompleted);
                    Assert.IsTrue(ctrl2.SetDefault().IsStateCompleted);

                    if (addDelay != null)
                    {
                        sm1.SecurityManagerInfo.SetDelayS0((SecurityS0Delays)addDelay, 15000);
                    }
                    if (learnDelay != null)
                    {
                        sm2.SecurityManagerInfo.SetDelayS0((SecurityS0Delays)learnDelay, 15000);
                    }
                    addToken = ctrl1.AddNodeToNetwork(Modes.NodeAny, ADD_TIMEOUT, null);
                    learnToken = ctrl2.SetLearnMode(LearnModes.LearnModeClassic, ADD_TIMEOUT, null);
                    addRes = (AddRemoveNodeResult)addToken.WaitCompletedSignal();
                    Assert.IsTrue(addRes.IsStateCompleted);
                    Assert.AreEqual(SubstituteStatuses.Failed, addRes.SubstituteStatus);
                    learnRes = (SetLearnModeResult)learnToken.WaitCompletedSignal();
                    Assert.IsTrue(learnRes.IsStateCompleted);
                    if (learnDelay != null)
                    {
                        var delay = (SecurityS0Delays)learnDelay;
                        if (delay == SecurityS0Delays.SchemeReport ||
                            delay == SecurityS0Delays.NetworkKeyVerify
                            )
                        {
                            Assert.AreEqual(SubstituteStatuses.Failed, learnRes.SubstituteStatus);
                        }
                        else if (delay == SecurityS0Delays.SchemeReportEnc)
                        {
                            Assert.AreEqual(SubstituteStatuses.Done, learnRes.SubstituteStatus);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                    else
                    {
                        var delay = (SecurityS0Delays)addDelay;
                        if (delay == SecurityS0Delays.SchemeInherit ||
                            delay == SecurityS0Delays.NetworkKeySet ||
                            delay == SecurityS0Delays.NonceGet
                            )
                        {
                            Assert.AreEqual(SubstituteStatuses.Failed, learnRes.SubstituteStatus);
                        }
                        else
                        {
                            Assert.AreEqual(SubstituteStatuses.None, learnRes.SubstituteStatus);
                        }
                    }
                    Assert.IsTrue(ctrl1.SetDefault().IsStateCompleted);
                    Assert.IsTrue(ctrl2.SetDefault().IsStateCompleted);

                    if (addDelay != null)
                    {
                        sm1.SecurityManagerInfo.SetDelayS0((SecurityS0Delays)addDelay, 0);
                    }
                    if (learnDelay != null)
                    {
                        sm2.SecurityManagerInfo.SetDelayS0((SecurityS0Delays)learnDelay, 0);
                    }
                    addToken = ctrl1.AddNodeToNetwork(Modes.NodeAny, ADD_TIMEOUT, null);
                    learnToken = ctrl2.SetLearnMode(LearnModes.LearnModeClassic, ADD_TIMEOUT, null);
                    addRes = (AddRemoveNodeResult)addToken.WaitCompletedSignal();
                    Assert.IsTrue(addRes.IsStateCompleted);
                    Assert.AreEqual(SubstituteStatuses.Done, addRes.SubstituteStatus);
                    learnRes = (SetLearnModeResult)learnToken.WaitCompletedSignal();
                    Assert.IsTrue(learnRes.IsStateCompleted);
                    Assert.AreEqual(SubstituteStatuses.Done, learnRes.SubstituteStatus);
                    Assert.IsTrue(ctrl1.SetDefault().IsStateCompleted);
                    Assert.IsTrue(ctrl2.SetDefault().IsStateCompleted);
                }
            }
        }
    }
}
