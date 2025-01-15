// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using NUnit.Framework;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Enums;
using ZWave.Layers;
using ZWave.Layers.Session;
using ZWave.Layers.Transport;
using ZWave.ZnifferApplication;
using ZWave.ZnifferApplication.Devices;
using ZWave.ZnifferApplication.Operations;

namespace ZnifferApplicationIntegrationTests
{
    [TestFixture]
    public class CollectData : ZnifferTestsBase
    {
        [SetUp]
        public void SetUp()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    ctrl.Connect(new SerialPortDataSource(COM1, BaudRates.Rate_115200));
                    slave.Connect(new SerialPortDataSource(COM2, BaudRates.Rate_115200));
                    VersionResult res = ctrl.GetVersion();
                    Assert.IsTrue(res.State == ActionStates.Completed);
                    Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || res.Library == Libraries.InstallerLib || res.Library == Libraries.ControllerPortableLib || res.Library == Libraries.ControllerBridgeLib);
                    res = slave.GetVersion();
                    Assert.IsTrue(res.State == ActionStates.Completed);
                    Assert.IsTrue(res.Library == Libraries.SlaveEnhancedLib || res.Library == Libraries.SlaveLib || res.Library == Libraries.SlaveRoutingLib);
                    ActionResult ares = ctrl.SetDefault();
                    Assert.IsTrue(ares.State == ActionStates.Completed);
                    ares = slave.SetDefault();
                    Assert.IsTrue(ares.State == ActionStates.Completed);

                    ActionToken addToken = ctrl.AddNodeToNetwork(Modes.NodeAny, 10000, null);
                    ActionToken learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, 10000, null);

                    Assert.IsTrue(addToken.WaitCompletedSignal());
                    Assert.IsTrue(learnToken.WaitCompletedSignal());
                    Utils.InitFrameDefinition();
                }
            }
        }

        [Test]
        public void TestHomeIdNodeId()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            ZnifferApplicationLayer zl = new ZnifferApplicationLayer(new SessionLayer(), new ZnifferFrameLayer(Utils.FrameDefinition), new SerialPortTransportLayer());

            using (Controller ctrl = al.CreateController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    using (Zniffer znf = zl.CreateZniffer())
                    {
                        ctrl.Connect(new SerialPortDataSource(COM1, BaudRates.Rate_115200));
                        slave.Connect(new SerialPortDataSource(COM2, BaudRates.Rate_115200));
                        znf.Connect(new SerialPortDataSource(COMZ, BaudRates.Rate_115200));

                        MemoryGetIdResult memRes = slave.MemoryGetId();
                        COMMAND_CLASS_BASIC.BASIC_REPORT report = new COMMAND_CLASS_BASIC.BASIC_REPORT();
                        byte[] data = report;
                        bool hasVersion = znf.GetVersion().IsStateCompleted;
                        Assert.IsTrue(hasVersion);
                        znf.Start();
                        znf.SetFrequency(0x00);

                        ActionToken collectToken = znf.CollectData(memRes.HomeId, memRes.NodeId, null, TIMEOUT, null);
                        for (int i = 0; i < POW; i++)
                        {
                            slave.SendData(0x01, data, TXO);
                        }
                        collectToken.SetCompleted();
                        CollectDataResult res = (CollectDataResult)collectToken.WaitCompletedSignal();
                        Assert.AreEqual(ActionStates.Completed, res.State);
                        Assert.AreEqual(POW, res.DataItems.Count);
                    }
                }
            }
        }

        [Test]
        public void TestDataItemPredicate()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            ZnifferApplicationLayer zl = new ZnifferApplicationLayer(new SessionLayer(), new ZnifferFrameLayer(Utils.InitFrameDefinition()), new SerialPortTransportLayer());

            using (Controller ctrl = al.CreateController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    using (Zniffer znf = zl.CreateZniffer())
                    {
                        ctrl.Connect(new SerialPortDataSource(COM1, BaudRates.Rate_115200));
                        slave.Connect(new SerialPortDataSource(COM2, BaudRates.Rate_115200));
                        znf.Connect(new SerialPortDataSource(COMZ, BaudRates.Rate_115200));

                        MemoryGetIdResult memRes = slave.MemoryGetId();
                        COMMAND_CLASS_BASIC.BASIC_REPORT report = new COMMAND_CLASS_BASIC.BASIC_REPORT();
                        byte[] data = report;
                        bool hasVersion = znf.GetVersion().IsStateCompleted;
                        Assert.IsTrue(hasVersion);
                        znf.Start();
                        znf.SetFrequency(0x00);

                        ActionToken collectToken = znf.CollectData(memRes.HomeId, memRes.NodeId, DataItemPredicateMethod, TIMEOUT, null);
                        for (int i = 0; i < POW; i++)
                        {
                            slave.SendData(0x01, data, TXO);
                        }
                        collectToken.SetCompleted();
                        collectToken.SetComletedSignal();
                        CollectDataResult res = (CollectDataResult)collectToken.WaitCompletedSignal();
                        Assert.AreEqual(ActionStates.Completed, res.State);
                        Assert.AreEqual(POW, res.DataItems.Count);
                    }
                }
            }
        }

        private bool DataItemPredicateMethod(ActionToken actionToken, DataItem dataIem)
        {
            return dataIem.Payload != null &&
                dataIem.Payload.Length > 2 &&
                dataIem.Payload[0] == COMMAND_CLASS_BASIC.ID &&
                dataIem.Payload[1] == COMMAND_CLASS_BASIC.BASIC_REPORT.ID;
        }
    }
}
