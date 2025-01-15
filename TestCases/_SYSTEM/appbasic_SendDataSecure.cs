// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"
    -SetDefault
    -Add controller. 
    -Add slave
    -SendData")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    public class appbasic_SendDataSecure : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1, "Primary");
            Alias Secondary = new Alias(2, "Secondary");
            Alias Slave = new Alias(3, "Slave");
            Alias[] ALL = new Alias[] { Primary, Secondary, Slave };

            int POW = 3;

            ReportService.LogText("Start");

            Service.ApplicationNodeInformation(Primary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_SECURITY.ID });
            Service.ApplicationNodeInformation(Secondary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_SECURITY.ID });
            Service.ApplicationNodeInformation(Slave, true, (byte)GenericTypes.SWITCH_MULTILEVEL, (byte)SpecificTypes.NOT_USED, new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_SECURITY.ID });

            ParallelService.SetDefault(ALL);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S0);
            ApplicationService.AddSecurity(Secondary, SecurityVersions.S0);
            ApplicationService.AddSecurity(Slave, SecurityVersions.S0);

            ApplicationService.AddBasic(Secondary);
            ApplicationService.AddBasic(Slave);

            Service.AddNode(Primary, Secondary);
            Service.AddNode(Primary, Slave);

            for (int i = 0; i < POW; i++)
            {
                Service.RequestData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());
                Service.RequestData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());
            }

            ParallelService.SetDefault(ALL);
            ApplicationService.SetSecurityDelay(Primary, SecurityS0Delays.NetworkKeySet, 11000);
            Service.AddNode(Primary, Secondary);
            for (int i = 0; i < POW; i++)
            {
                Service.RequestData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());
            }
        }
    }
}