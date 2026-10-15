// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("Test for Serial API changes in PCStayAwake", Tags.SYS, Tags.Brief, Tags.PCController)]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    public class appbasic_PmStayAwake : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave = new Alias(2);

            ReportService.LogText("Start");

            ParallelService.SetDefault(Primary, Slave);
            Service.ApplicationNodeInformation(Slave, false, (byte)GenericTypes.SWITCH_MULTILEVEL, (byte)SpecificTypes.NOT_USED, new byte[]
            {
                COMMAND_CLASS_ALARM.ID,
            });
            Service.SoftReset(Slave);
            var addRes = Service.AddNode(Primary, Slave);
            AssertIsTrue(addRes, $"Controller added to the network NID: {addRes.NodeId} Unable to add: Controller");

            Service.PowerMgmtCancel(Slave, 1);
            Service.PowerMgmtStayAwake(Slave, 0, 5000, 12000);

            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Delay(1000);
            Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_REPORT());
        }
    }
}