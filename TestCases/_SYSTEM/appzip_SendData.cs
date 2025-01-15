// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"SendData test case")]
    [ZipDeviceMapping(1, "z1")]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    public class appzip_SendData : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias ZipPrimary = new Alias(1);
            Alias Secondary = new Alias(2);
            Alias Slave = new Alias(3);
            Alias[] ALL = new Alias[] { Secondary, Slave };

            ReportService.LogText("Start");

            Service.ApplicationNodeInformation(Secondary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_BASIC.ID });
            Service.ApplicationNodeInformation(Slave, true, (byte)GenericTypes.SWITCH_MULTILEVEL, (byte)SpecificTypes.NOT_USED, new byte[] { COMMAND_CLASS_BASIC.ID });

            ParallelService.SetDefault(ALL);
            ZipService.SetDefault(ZipPrimary);

            ZipResult.AddNode addRes = ZipService.AddNode(ZipPrimary, Secondary);
            AssertIsTrue(addRes, "Controller added to the network NID:" + addRes.NodeId.ToString(), "Unable to add: Controller");
            addRes = ZipService.AddNode(ZipPrimary, Slave);
            AssertIsTrue(addRes, "Slave added to the network NID:" + addRes.NodeId.ToString(), "Unable to add: Slave");

            ZipResult.SendData sendRes = ZipService.SendData(Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
            sendRes = ZipService.SendData(Slave, new COMMAND_CLASS_BASIC.BASIC_GET());
            Service.SetRFReceiveMode(Slave, false);
            sendRes = ZipService.SendData(Slave, new COMMAND_CLASS_BASIC.BASIC_GET());
        }
    }
}