// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Response data with. Endpoint nodes")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class appbasic_CommandClassSupport : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave = new Alias(2);

            ReportService.LogText("Start");
            ParallelService.SetDefault(Primary, Slave);

            var cmdClasses = new byte[] { COMMAND_CLASS_ALARM.ID, COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID };
            Service.ApplicationNodeInformationCmdClasses(Slave, cmdClasses, cmdClasses, cmdClasses);

            ApplicationService.AddMultiChannel(Primary);
            ApplicationService.AddMultiChannel(Slave);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S0 | SecurityVersions.S2);
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Primary);

            var addRes = Service.IncludeNode(Primary, Slave);
            AssertIsTrue(addRes);

            ReportService.LogText("Add support");

            ApplicationService.AddSupportCc(Slave, COMMAND_CLASS_WAKE_UP.ID);
            ApplicationService.AddSupportCc(Slave.EndPoint(1), COMMAND_CLASS_WAKE_UP.ID);
            ApplicationService.AddSupportCc(Slave, COMMAND_CLASS_BATTERY.ID);
            ApplicationService.AddSupportCc(Slave.EndPoint(1), COMMAND_CLASS_BATTERY.ID);

            ReportService.LogText("Request from root");
            var res = ComplianceService.RequestData(Primary, addRes.AddNodeResult.Node, new COMMAND_CLASS_WAKE_UP.WAKE_UP_INTERVAL_GET(), new COMMAND_CLASS_WAKE_UP.WAKE_UP_INTERVAL_REPORT(), 20000);
            AssertIsTrue(res);
            //AssertIsTrue(((COMMAND_CLASS_BATTERY.BATTERY_REPORT)res.Command).batteryLevel == 0xFF);

            ReportService.LogText("Request from endpoint");
            res = ComplianceService.RequestData(Primary, new Node(addRes.AddNodeResult.Node.Id, 1), new COMMAND_CLASS_WAKE_UP.WAKE_UP_INTERVAL_GET(), new COMMAND_CLASS_WAKE_UP.WAKE_UP_INTERVAL_REPORT(), 20000);
            AssertIsTrue(res);
            //AssertIsTrue(((COMMAND_CLASS_BATTERY.BATTERY_REPORT)res.Command).batteryLevel == 0x01);
            AssertIsTrue(res.Node.EndPointId == 1);

        }
    }
}