// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using NUnit.Framework;
using ZWave;
using ZWave.Enums;
using ZWave.Layers;
using ZWave.Layers.Session;
using ZWave.Layers.Transport;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Enums;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Operations;

namespace BasicApplicationIntegrationTests
{
    [TestFixture]
    public class AddNodeBridgeSlaveSecure : BasicTestsBase
    {
        int POW = 10;

        [SetUp]
        public void SetUp()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (BridgeController slave = al.CreateBridgeController())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    ctrl.MemoryGetId();
                    SetupSecurity(ctrl, true);
                    slave.Connect(new SerialPortDataSource(COM_CTRL_BRIDGE_1, BaudRates.Rate_115200));
                    slave.MemoryGetId();
                    SetupSecurity(slave, true);

                    VersionResult res = ctrl.GetVersion();
                    Assert.IsTrue(res.IsStateCompleted);
                    Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || res.Library == Libraries.InstallerLib || res.Library == Libraries.ControllerPortableLib || res.Library == Libraries.ControllerBridgeLib);
                    res = slave.GetVersion();
                    Assert.IsTrue(res.IsStateCompleted);
                    Assert.IsTrue(res.Library == Libraries.ControllerBridgeLib);
                    Assert.IsTrue(ctrl.SetDefault().IsStateCompleted);
                    Assert.IsTrue(slave.SetDefault().IsStateCompleted);

                    ActionToken addToken = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                    ActionToken learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);
                    Assert.IsTrue(addToken.WaitCompletedSignal().IsStateCompleted);
                    Assert.IsTrue(learnToken.WaitCompletedSignal().IsStateCompleted);
                }
            }
        }

        [Test]
        public void TestAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (BridgeController slave = al.CreateBridgeController())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    ctrl.MemoryGetId();
                    SetupSecurity(ctrl, true);
                    slave.Connect(new SerialPortDataSource(COM_CTRL_BRIDGE_1, BaudRates.Rate_115200));
                    slave.MemoryGetId();
                    SetupSecurity(slave, true);

                    ActionToken addToken = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                    ActionToken learnToken = slave.SetSlaveLearnMode(0, SlaveLearnModes.VirtualSlaveLearnModeEnable, INCLUSION_TIMEOUT, null);
                    Assert.IsTrue(addToken.WaitCompletedSignal().IsStateCompleted);
                    Assert.IsTrue(learnToken.WaitCompletedSignal().IsStateCompleted);
                }
            }
        }

        [Test]
        public void TestManyAsync()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (BridgeController slave = al.CreateBridgeController())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    ctrl.MemoryGetId();
                    SetupSecurity(ctrl, true);
                    slave.Connect(new SerialPortDataSource(COM_CTRL_BRIDGE_1, BaudRates.Rate_115200));
                    slave.MemoryGetId();
                    SetupSecurity(slave, true);

                    ActionToken addRemoveToken;
                    ActionToken learnToken;
                    for (int i = 0; i < POW; i++)
                    {
                        addRemoveToken = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                        learnToken = slave.SetSlaveLearnMode(0, SlaveLearnModes.VirtualSlaveLearnModeEnable, INCLUSION_TIMEOUT, null);
                        Assert.IsTrue(addRemoveToken.WaitCompletedSignal().IsStateCompleted);
                        SetLearnModeResult res = (SetLearnModeResult)learnToken.WaitCompletedSignal();
                        Assert.IsTrue(res.IsStateCompleted);

                        addRemoveToken = ctrl.RemoveNodeFromNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                        learnToken = slave.SetSlaveLearnMode(res.NodeId, SlaveLearnModes.VirtualSlaveLearnModeEnable, INCLUSION_TIMEOUT, null);
                        Assert.IsTrue(addRemoveToken.WaitCompletedSignal().IsStateCompleted);
                        Assert.IsTrue(learnToken.WaitCompletedSignal().IsStateCompleted);
                    }
                }
            }
        }
    }
}
