// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"
    -SetDefault
    -Add controller. 
    -Add slave
    -SendData
    -Stress test with reports")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    public class stressbasic_RequestDataSecure : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);

            Alias[] ALL = new Alias[] { Primary, Secondary };

            ReportService.LogText("Start");

            Service.ApplicationNodeInformation(Primary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_SECURITY.ID });
            Service.ApplicationNodeInformation(Secondary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_SECURITY.ID });

            ParallelService.SetDefault(ALL);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S0);
            ApplicationService.AddSecurity(Secondary, SecurityVersions.S0);
            
            Service.AddNode(Primary, Secondary);

            ApplicationService.AddResponse(Secondary, new COMMAND_CLASS_BASIC.BASIC_REPORT(), new COMMAND_CLASS_BASIC.BASIC_GET());

            ApplicationService.StartTrafficToNodeIdTest(Primary, 0x02, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT(), 100, 10000);
            for (int i = 0; i < 10; i++)
            {
                Delay(6000);
                BasicResult.Test res = ApplicationService.GetTrafficTestResult(Primary, 0x02);
                ReportService.LogTitle(string.Format("after 6 sec: {0}({1}), {2}%", res.Total, res.Fails, res.SuccessPercentRate));
            }
            BasicResult.Test resTotal = ApplicationService.StopTrafficTest(Primary, 0x02);
            ReportService.LogTitle(string.Format("Total: {0}({1}), {2}%", resTotal.Total, resTotal.Fails, resTotal.SuccessPercentRate));
        }
    }
}