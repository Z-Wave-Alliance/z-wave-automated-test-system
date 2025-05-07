// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"
    -SetDefault
    -Add controller. 
    -Add slave
    -SendData
    -Stress test 2 Networks with 2 tasks each")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    [DeviceMapping(4, LibTypes.ControllerStaticLib)]
    [DeviceMapping(5, LibTypes.EndDeviceLib)]
    [DeviceMapping(6, LibTypes.EndDeviceLib)]
    public class stressbasic_SendData2NetSecure : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias PrimaryA = new Alias(1, "Network-A Controller");
            Alias Node1A = new Alias(2, "Network-A Slave-1");
            Alias Node2A = new Alias(3, "Network-A Slave-2");
            Alias PrimaryB = new Alias(4, "Network-B Controller");
            Alias Node1B = new Alias(5, "Network-B Slave-1");
            Alias Node2B = new Alias(6, "Network-B Slave-2");

            Alias[] ALL = new Alias[] { PrimaryA, Node1A, Node2A, PrimaryB, Node1B, Node2B };

            ReportService.LogText("Start");

            Service.ApplicationNodeInformation(PrimaryA, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER,
                new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_SECURITY.ID });
            Service.ApplicationNodeInformation(Node1A, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER,
                new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_SECURITY.ID });
            Service.ApplicationNodeInformation(Node2A, true, (byte)GenericTypes.SWITCH_MULTILEVEL, (byte)SpecificTypes.NOT_USED,
                new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_SECURITY.ID });
            Service.ApplicationNodeInformation(PrimaryB, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER,
                new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_SECURITY.ID });
            Service.ApplicationNodeInformation(Node1B, true, (byte)GenericTypes.SWITCH_MULTILEVEL, (byte)SpecificTypes.NOT_USED,
                new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_SECURITY.ID });
            Service.ApplicationNodeInformation(Node2B, true, (byte)GenericTypes.SWITCH_MULTILEVEL, (byte)SpecificTypes.NOT_USED,
                new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_SECURITY.ID });

            ParallelService.SetDefault(ALL);

            ApplicationService.AddSecurity(PrimaryA, SecurityVersions.S0);
            ApplicationService.AddSecurity(Node1A, SecurityVersions.S0);
            ApplicationService.AddSecurity(Node2A, SecurityVersions.S0);
            ApplicationService.AddSecurity(PrimaryB, SecurityVersions.S0);
            ApplicationService.AddSecurity(Node1B, SecurityVersions.S0);
            ApplicationService.AddSecurity(Node2B, SecurityVersions.S0);

            AssertIsTrue(Service.AddNode(PrimaryA, Node1A).Node.Id > 0);
            AssertIsTrue(Service.AddNode(PrimaryA, Node2A).Node.Id > 0);
            AssertIsTrue(Service.AddNode(PrimaryB, Node1B).Node.Id > 0);
            AssertIsTrue(Service.AddNode(PrimaryB, Node2B).Node.Id > 0);

            ApplicationService.StartTrafficToNodeIdTest(Node1A, 0x01, new COMMAND_CLASS_BASIC.BASIC_GET(), 100);
            ApplicationService.StartTrafficToNodeIdTest(Node2A, 0x01, new COMMAND_CLASS_BASIC.BASIC_GET(), 100);
            ApplicationService.StartTrafficToNodeIdTest(Node1B, 0x01, new COMMAND_CLASS_BASIC.BASIC_GET(), 100);
            ApplicationService.StartTrafficToNodeIdTest(Node2B, 0x01, new COMMAND_CLASS_BASIC.BASIC_GET(), 100);
            for (int i = 0; i < 10; i++)
            {
                Delay(6000);
                BasicResult.Test res1A = ApplicationService.GetTrafficTestResult(Node1A, 0x01);
                ReportService.LogTitle(string.Format("{0} after 6 sec: {1}({2}), {3}%", Node1A, res1A.Total, res1A.Fails, res1A.SuccessPercentRate));
                BasicResult.Test res2A = ApplicationService.GetTrafficTestResult(Node2A, 0x01);
                ReportService.LogTitle(string.Format("{0} after 6 sec: {1}({2}), {3}%", Node2A, res2A.Total, res2A.Fails, res2A.SuccessPercentRate));
                BasicResult.Test res1B = ApplicationService.GetTrafficTestResult(Node1B, 0x01);
                ReportService.LogTitle(string.Format("{0} after 6 sec: {1}({2}), {3}%", Node1B, res1B.Total, res1B.Fails, res1B.SuccessPercentRate));
                BasicResult.Test res2B = ApplicationService.GetTrafficTestResult(Node2B, 0x01);
                ReportService.LogTitle(string.Format("{0} after 6 sec: {1}({2}), {3}%", Node2B, res2B.Total, res2B.Fails, res2B.SuccessPercentRate));
            }
            BasicResult.Test resTotal1A = ApplicationService.StopTrafficTest(Node1A, 0x01);
            ReportService.LogTitle(string.Format("{0} Total: {1}({2}), {3}%", Node1A, resTotal1A.Total, resTotal1A.Fails, resTotal1A.SuccessPercentRate));
            BasicResult.Test resTotal2A = ApplicationService.StopTrafficTest(Node2A, 0x01);
            ReportService.LogTitle(string.Format("{0} Total: {1}({2}), {3}%", Node2A, resTotal2A.Total, resTotal2A.Fails, resTotal2A.SuccessPercentRate));
            BasicResult.Test resTotal1B = ApplicationService.StopTrafficTest(Node1B, 0x01);
            ReportService.LogTitle(string.Format("{0} Total: {1}({2}), {3}%", Node1B, resTotal1B.Total, resTotal1B.Fails, resTotal1B.SuccessPercentRate));
            BasicResult.Test resTotal2B = ApplicationService.StopTrafficTest(Node2B, 0x01);
            ReportService.LogTitle(string.Format("{0} Total: {1}({2}), {3}%", Node2B, resTotal2B.Total, resTotal2B.Fails, resTotal2B.SuccessPercentRate));
        }
    }
}