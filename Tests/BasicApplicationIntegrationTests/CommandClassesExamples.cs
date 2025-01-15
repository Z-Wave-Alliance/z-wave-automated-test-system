// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;
using System.Linq;
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

namespace BasicApplicationIntegrationTests
{
    [TestFixture]
    public class CommandClassesExamples : BasicTestsBase
    {
        [SetUp]
        public void SetUp()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    slave.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));
                    VersionResult res = ctrl.GetVersion();
                    Assert.IsTrue(res.State == ActionStates.Completed);
                    Assert.IsTrue(res.Library == Libraries.ControllerStaticLib || 
                        res.Library == Libraries.InstallerLib || 
                        res.Library == Libraries.ControllerPortableLib || 
                        res.Library == Libraries.ControllerBridgeLib);
                    res = slave.GetVersion();
                    Assert.IsTrue(res.State == ActionStates.Completed);
                    Assert.IsTrue(res.Library == Libraries.SlaveEnhancedLib || 
                        res.Library == Libraries.SlaveLib ||
                        res.Library == Libraries.SlaveRoutingLib);
                    ActionResult ares = ctrl.SetDefault();
                    Assert.IsTrue(ares.State == ActionStates.Completed);
                    ares = slave.SetDefault();
                    Assert.IsTrue(ares.State == ActionStates.Completed);

                    ActionToken addToken = ctrl.AddNodeToNetwork(Modes.NodeAny, INCLUSION_TIMEOUT, null);
                    ActionToken learnToken = slave.SetLearnMode(LearnModes.LearnModeClassic, INCLUSION_TIMEOUT, null);

                    Assert.IsTrue(addToken.WaitCompletedSignal());
                    Assert.IsTrue(learnToken.WaitCompletedSignal());
                }
            }
        }

        [Test]
        public void SENSOR_MULTILEVEL_SUPPORTED_SENSOR_REPORT()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    slave.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));

                    COMMAND_CLASS_SENSOR_MULTILEVEL_V6.SENSOR_MULTILEVEL_SUPPORTED_SENSOR_REPORT report = new COMMAND_CLASS_SENSOR_MULTILEVEL_V6.SENSOR_MULTILEVEL_SUPPORTED_SENSOR_REPORT();
                    report.bitMask = new List<byte>(new byte[] { 0x05, 0x03 });

                    slave.SendData(0x01, report, TXO);
                }
            }
        }

        [Test]
        public void SIMPLE_AV_CONTROL_SET()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    slave.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));

                    COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SET cmd = new COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SET();
                    cmd.sequenceNumber = 0x01;
                    cmd.properties1 = 0x33;
                    cmd.reserved2 = new byte[] { 0x02, 0x03 };
                    cmd.vg = new List<COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SET.TVG>();
                    cmd.vg.Add(new COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SET.TVG() { command = new byte[] { 0x03, 0x04 } });
                    cmd.vg.Add(new COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SET.TVG() { command = new byte[] { 0x05, 0x06 } });
                    cmd.vg.Add(new COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SET.TVG() { command = new byte[] { 0x07, 0x08 } });
                    slave.SendData(0x01, cmd, TXO);
                }
            }
        }

        [Test]
        public void MULTI_CMD_ENCAP()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    slave.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));

                    byte[] cmd = new byte[] { 0x8F, 0x01, 0x05, 0x03, 0x80, 0x03, 0x64, 0x02, 0x46, 0x04, 0x02, 0x46, 0x07, 0x02, 0x81, 0x05, 0x02, 0x84, 0x07 };
                    slave.SendData(0x01, cmd, TXO);

                    COMMAND_CLASS_MULTI_CMD.MULTI_CMD_ENCAP cmd2 = new COMMAND_CLASS_MULTI_CMD.MULTI_CMD_ENCAP();
                    cmd2.numberOfCommands = 2;
                    cmd2.encapsulatedCommand = new List<COMMAND_CLASS_MULTI_CMD.MULTI_CMD_ENCAP.TENCAPSULATEDCOMMAND>();
                    cmd2.encapsulatedCommand.Add(new COMMAND_CLASS_MULTI_CMD.MULTI_CMD_ENCAP.TENCAPSULATEDCOMMAND());
                    cmd2.encapsulatedCommand.Add(new COMMAND_CLASS_MULTI_CMD.MULTI_CMD_ENCAP.TENCAPSULATEDCOMMAND());
                    cmd2.encapsulatedCommand.Add(new COMMAND_CLASS_MULTI_CMD.MULTI_CMD_ENCAP.TENCAPSULATEDCOMMAND());

                    COMMAND_CLASS_FIRMWARE_UPDATE_MD.FIRMWARE_MD_REPORT c0 = new COMMAND_CLASS_FIRMWARE_UPDATE_MD.FIRMWARE_MD_REPORT();
                    c0.firmwareId = new byte[] { 1, 2 };
                    c0.manufacturerId = new byte[] { 3, 4 };
                    c0.checksum = new byte[] { 5, 6 };
                    byte[] cb0 = c0;
                    cmd2.encapsulatedCommand[0].commandLength = (byte)cb0.Length;
                    cmd2.encapsulatedCommand[0].commandClass = cb0[0];
                    cmd2.encapsulatedCommand[0].command = cb0[1];
                    cmd2.encapsulatedCommand[0].data = new List<byte>(cb0.Skip(2));

                    COMMAND_CLASS_DOOR_LOCK_V2.DOOR_LOCK_CONFIGURATION_REPORT c1 = new COMMAND_CLASS_DOOR_LOCK_V2.DOOR_LOCK_CONFIGURATION_REPORT();
                    c1.lockTimeoutMinutes = 3;
                    c1.lockTimeoutSeconds = 2;
                    c1.operationType = 1;
                    c1.properties1 = 4;
                    byte[] cb1 = c1;
                    cmd2.encapsulatedCommand[1].commandLength = (byte)cb1.Length;
                    cmd2.encapsulatedCommand[1].commandClass = cb1[0];
                    cmd2.encapsulatedCommand[1].command = cb1[1];
                    cmd2.encapsulatedCommand[1].data = new List<byte>(cb1.Skip(2));


                    slave.SendData(0x01, cmd2, TXO);
                }
            }
        }

        [Test]
        public void CRC_16_ENCAP()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    slave.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));

                    COMMAND_CLASS_CRC_16_ENCAP.CRC_16_ENCAP cmd = new COMMAND_CLASS_CRC_16_ENCAP.CRC_16_ENCAP();
                    COMMAND_CLASS_DOOR_LOCK_V2.DOOR_LOCK_CONFIGURATION_REPORT c1 = new COMMAND_CLASS_DOOR_LOCK_V2.DOOR_LOCK_CONFIGURATION_REPORT();
                    c1.lockTimeoutMinutes = 3;
                    c1.lockTimeoutSeconds = 2;
                    c1.operationType = 1;
                    c1.properties1 = 4;
                    byte[] cb1 = c1;
                    
                    cmd.commandClass = cb1[0];
                    cmd.command = cb1[1];
                    cmd.data = new List<byte>(cb1.Skip(2));
                    cmd.checksum = new byte[] { 33, 33 };
                    slave.SendData(0x01, cmd, TXO);
                }
            }
        }


        [Test]
        public void MULTI_CHANNEL_CMD_ENCAP()
        {
            BasicApplicationLayer al = new BasicApplicationLayer(new SessionLayer(), new BasicFrameLayer(), new SerialPortTransportLayer());
            using (Controller ctrl = al.CreateController())
            {
                using (Slave slave = al.CreateSlave())
                {
                    ctrl.Connect(new SerialPortDataSource(COM_CTRL_STATIC_1, BaudRates.Rate_115200));
                    slave.Connect(new SerialPortDataSource(COM_SLAVE_1, BaudRates.Rate_115200));

                    COMMAND_CLASS_MULTI_CHANNEL_V3.MULTI_CHANNEL_CMD_ENCAP cmd = new COMMAND_CLASS_MULTI_CHANNEL_V3.MULTI_CHANNEL_CMD_ENCAP();
                    COMMAND_CLASS_DOOR_LOCK_V2.DOOR_LOCK_CONFIGURATION_REPORT c1 = new COMMAND_CLASS_DOOR_LOCK_V2.DOOR_LOCK_CONFIGURATION_REPORT();
                    c1.lockTimeoutMinutes = 3;
                    c1.lockTimeoutSeconds = 2;
                    c1.operationType = 1;
                    c1.properties1 = 4;
                    byte[] cb1 = c1;

                    cmd.commandClass = cb1[0];
                    cmd.command = cb1[1];
                    cmd.parameter = new List<byte>(cb1.Skip(2));
                    cmd.properties1 = 33;
                    cmd.properties2 = 44;
                    slave.SendData(0x01, cmd, TXO);
                }
            }
        }
    }
}
