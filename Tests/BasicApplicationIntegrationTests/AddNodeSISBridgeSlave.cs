// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using NUnit.Framework;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Operations;
using ZWave.Enums;
using ZWave.Layers;
using ZWave.Layers.Session;
using ZWave.Layers.Transport;
using ZWave.BasicApplication.Enums;

namespace BasicApplicationIntegrationTests
{
    public class AddNodeSISBridgeSlave : BasicTestsBase
    {
        //AddSISBridgeCtrl
        int POW = 3;

        [SetUp]
        public void SetUp()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl1 = al.CreateController())
            {
                using (Controller ctrl2 = al.CreateController())
                {
                    using (BridgeController bridge = al.CreateBridgeController())
                    {
                        ctrl1.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                        ctrl1.MemoryGetId();
                        ctrl2.Connect(new SerialPortDataSource(COM_CTRL_STATIC_2, BaudRates.Rate_115200));
                        ctrl2.MemoryGetId();
                        bridge.Connect(new SerialPortDataSource(COM_CTRL_BRIDGE_1, BaudRates.Rate_115200));
                        bridge.MemoryGetId();

                        VersionResult res = ctrl1.GetVersion();
                        Assert.IsTrue(res.IsStateCompleted);
                        Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || res.Library == Libraries.InstallerLib || res.Library == Libraries.ControllerPortableLib || res.Library == Libraries.ControllerBridgeLib);
                        res = ctrl2.GetVersion();
                        Assert.IsTrue(res.IsStateCompleted);
                        Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || res.Library == Libraries.InstallerLib || res.Library == Libraries.ControllerPortableLib || res.Library == Libraries.ControllerBridgeLib);
                        res = bridge.GetVersion();
                        Assert.IsTrue(res.IsStateCompleted);
                        Assert.IsTrue(res.Library == Libraries.ControllerBridgeLib);
                        
                        Assert.IsTrue(ctrl1.SetDefault().IsStateCompleted);
                        Assert.IsTrue(ctrl2.SetDefault().IsStateCompleted);
                        Assert.IsTrue(bridge.SetDefault().IsStateCompleted);
                    }
                }
            }
        }

        [TestCase(SecurityUnderTest.None, false)]
        [TestCase(SecurityUnderTest.S0, false)]
        [TestCase(SecurityUnderTest.S2, true)]
        public void TestAddNodeSISBridgeSlave(SecurityUnderTest securityUnderTest, bool isEnableDSK)
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (BridgeController bridge = al.CreateBridgeController())
            {
                using (Controller ctrl = al.CreateController())
                {
                    bridge.Connect(new SerialPortDataSource(COM_CTRL_BRIDGE_1, BaudRates.Rate_115200));
                    bridge.MemoryGetId();
                    SetupSecurity(bridge, securityUnderTest, isEnableDSK);
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    ctrl.MemoryGetId();
                    SetupSecurity(ctrl, securityUnderTest, isEnableDSK);

                    bridge.SetSucNodeID(bridge.Id, true, TransmitOptions.TransmitOptionAcknowledge | TransmitOptions.TransmitOptionAutoRoute | TransmitOptions.TransmitOptionExplore, 0x01);
                    var addToken = bridge.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                    var learnToken = ctrl.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);
                    var addResult = addToken.WaitCompletedSignal();
                    var learnResult = learnToken.WaitCompletedSignal();

                    Assert.IsTrue(addResult);
                    Assert.IsTrue(learnResult);
                    Assert.AreEqual(2, ((AddRemoveNodeResult)addResult).Id);
                    Assert.AreEqual(2, ((SetLearnModeResult)learnResult).NodeId);

                    for (int i = 0; i < POW; i++)
                    {
                        addToken = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                        System.Threading.Thread.Sleep(3000);//check delay for NIF
                        learnToken = bridge.SetSlaveLearnMode(0, SlaveLearnModes.VirtualSlaveLearnModeEnable, INCLUSION_TIMEOUT, null);
                        addResult = addToken.WaitCompletedSignal();
                        learnResult = learnToken.WaitCompletedSignal();

                        Assert.IsTrue(addResult);
                        Assert.IsTrue(learnResult);
                        Assert.AreEqual(i + 3, ((AddRemoveNodeResult)addResult).Id);
                        Assert.AreEqual(i + 3, ((SetLearnModeResult)learnResult).NodeId);

                        System.Threading.Thread.Sleep(1500);
                    }
                }
            }
        }

        [TestCase(SecurityUnderTest.None, false)]
        [TestCase(SecurityUnderTest.S0, false)]
        [TestCase(SecurityUnderTest.S2, true)]
        public void TestAddSISControllerBridgeSlave(SecurityUnderTest securityUnderTest, bool isEnableDSK)
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (BridgeController bridge = al.CreateBridgeController())
            {
                using (Controller ctrl1Inclusion = al.CreateController())
                {
                    using (Controller ctrl2SIS = al.CreateController())
                    {
                        bridge.Connect(new SerialPortDataSource(COM_CTRL_BRIDGE_1, BaudRates.Rate_115200));
                        bridge.MemoryGetId();
                        SetupSecurity(bridge, securityUnderTest, isEnableDSK);
                        ctrl1Inclusion.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                        ctrl1Inclusion.MemoryGetId();
                        SetupSecurity(ctrl1Inclusion, securityUnderTest, isEnableDSK);
                        ctrl2SIS.Connect(new SerialPortDataSource(COM_CTRL_STATIC_2, BaudRates.Rate_115200));
                        ctrl2SIS.MemoryGetId();
                        SetupSecurity(ctrl2SIS, securityUnderTest, isEnableDSK);

                        var addToken = ctrl1Inclusion.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                        var learnToken = ctrl2SIS.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);
                        var addResult = addToken.WaitCompletedSignal();
                        var learnResult = learnToken.WaitCompletedSignal();
                      
                        Assert.IsTrue(addResult);
                        Assert.IsTrue(learnResult);
                        Assert.AreEqual(2, ((AddRemoveNodeResult)addResult).Id);
                        Assert.AreEqual(2, ((SetLearnModeResult)learnResult).NodeId);
                        
                        ctrl1Inclusion.SetSucNodeID(ctrl2SIS.Id, true, TransmitOptions.TransmitOptionAcknowledge | TransmitOptions.TransmitOptionAutoRoute | TransmitOptions.TransmitOptionExplore, 0x01);

                        addToken = ctrl1Inclusion.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                        learnToken = bridge.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);
                        addResult = addToken.WaitCompletedSignal();
                        learnResult = learnToken.WaitCompletedSignal();
                        Assert.IsTrue(addResult);
                        Assert.IsTrue(learnResult);
                        Assert.AreEqual(3, ((AddRemoveNodeResult)addResult).Id);
                        Assert.AreEqual(3, ((SetLearnModeResult)learnResult).NodeId);
                        
                        for (int i = 0; i < POW; i++)
                        {
                            System.Threading.Thread.Sleep(1500); // To avoid inclusion fail on next step
                         
                            addToken = ctrl1Inclusion.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                            System.Threading.Thread.Sleep(3000);//check delay for NIF
                            learnToken = bridge.SetSlaveLearnMode(0, SlaveLearnModes.VirtualSlaveLearnModeEnable, INCLUSION_TIMEOUT, null);
                            addResult = addToken.WaitCompletedSignal();
                            learnResult = learnToken.WaitCompletedSignal();

                            Assert.IsTrue(addResult);
                            Assert.IsTrue(learnResult);
                            Assert.AreEqual(i + 4, ((AddRemoveNodeResult)addResult).Id);
                            Assert.AreEqual(i + 4, ((SetLearnModeResult)learnResult).NodeId);
                        }
                    }
                }
            }
        }

    }
}
