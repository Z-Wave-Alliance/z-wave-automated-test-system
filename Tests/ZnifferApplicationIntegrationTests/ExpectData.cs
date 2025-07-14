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

namespace ZnifferApplicationIntegrationTests
{
    [TestFixture]
    public class ExpectData : ZnifferTestsBase
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

                        ActionToken expectToken = znf.ExpectData(memRes.HomeId, memRes.NodeId, null, TIMEOUT, null);
                        ActionToken sendToken = slave.SendData(0x01, data, TXO, null, null);
                        ActionResult res = expectToken.WaitCompletedSignal();
                        sendToken.WaitCompletedSignal();
                        Assert.AreEqual(ActionStates.Completed, res.State);

                        expectToken = znf.ExpectData(null, memRes.NodeId, null, TIMEOUT, null);
                        sendToken = slave.SendData(0x01, data, TXO, null, null);
                        res = expectToken.WaitCompletedSignal();
                        sendToken.WaitCompletedSignal();
                        Assert.AreEqual(ActionStates.Completed, res.State);

                        expectToken = znf.ExpectData(memRes.HomeId, null, null, TIMEOUT, null);
                        sendToken = slave.SendData(0x01, data, TXO, null, null);
                        res = expectToken.WaitCompletedSignal();
                        sendToken.WaitCompletedSignal();
                        Assert.AreEqual(ActionStates.Completed, res.State);

                        expectToken = znf.ExpectData(null, null, null, TIMEOUT, null);
                        sendToken = slave.SendData(0x01, data, TXO, null, null);
                        res = expectToken.WaitCompletedSignal();
                        sendToken.WaitCompletedSignal();
                        Assert.AreEqual(ActionStates.Completed, res.State);
                    }
                }
            }
        }

        private MemoryGetIdResult _memoryGetIdResult;
        [Test]
        public void TestDataItemPredicate()
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

                        _memoryGetIdResult = slave.MemoryGetId();
                        COMMAND_CLASS_BASIC.BASIC_REPORT report = new COMMAND_CLASS_BASIC.BASIC_REPORT();
                        byte[] data = report;
                        bool hasVersion = znf.GetVersion().IsStateCompleted;
                        Assert.IsTrue(hasVersion);
                        znf.Start();
                        znf.SetFrequency(0x00);

                        ActionToken expectToken = znf.ExpectData(_memoryGetIdResult.HomeId, _memoryGetIdResult.NodeId, DataItemPredicateMethod, TIMEOUT, null);
                        ActionToken sendToken = slave.SendData(0x01, data, TXO, null, null);
                        ActionResult res = expectToken.WaitCompletedSignal();
                        sendToken.WaitCompletedSignal();
                        Assert.AreEqual(ActionStates.Completed, res.State);

                        expectToken = znf.ExpectData(null, null, DataItemPredicateMethodWithHomeId, TIMEOUT, null);
                        sendToken = slave.SendData(0x01, data, TXO, null, null);
                        res = expectToken.WaitCompletedSignal();
                        sendToken.WaitCompletedSignal();
                        Assert.AreEqual(ActionStates.Completed, res.State);
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

        private bool DataItemPredicateMethodWithHomeId(ActionToken actionToken, DataItem dataIem)
        {
            return dataIem.HomeId != null &&
                dataIem.HomeId.Length == 4 &&
                dataIem.HomeId[0] == _memoryGetIdResult.HomeId[0] &&
                dataIem.HomeId[1] == _memoryGetIdResult.HomeId[1] &&
                dataIem.HomeId[2] == _memoryGetIdResult.HomeId[2] &&
                dataIem.HomeId[3] == _memoryGetIdResult.HomeId[3] &&
                dataIem.Source == _memoryGetIdResult.NodeId;
        }
    }
}