// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"existing network with DUT
    -SendData 
    -Stress test")]
    [DeviceMapping(1, "moxa1", LibTypes.ControllerStaticLib)]
    public class stressbasic_custom_dut_RequestDataSecure : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            //-----------------------------------------------
            //Existing NETWORK_KEY
            byte[] NetworkKey = "37 18 E3 C6 E6 B4 0E 02 D3 31 A5 81 D9 57 07 03".ToByteArray();
            //
            //DUT NodeId
            byte DUT_NodeId = 6;
            //
            //Request command
            byte[] Data = new COMMAND_CLASS_VERSION.VERSION_GET();
            //
            //Report Command
            byte[] ExpectedData = new COMMAND_CLASS_VERSION.VERSION_REPORT();
            //
            //Interval between requests, ms
            int IntervalMs = 100;
            //
            //Expected Report timeout, ms
            int TimeoutMs = 20000;
            //-----------------------------------------------

            ReportService.LogText("Start");

            ApplicationService.AddSecurity(Primary, SecurityVersions.S0);
            ApplicationService.SetSecurityTestNetworkKey(Primary, NetworkKey);

            ApplicationService.StartTrafficToNodeIdTest(Primary, DUT_NodeId, Data, ExpectedData, IntervalMs, TimeoutMs);
            for (int i = 0; i < 10; i++)
            {
                Delay(6000);
                BasicResult.Test res = ApplicationService.GetTrafficTestResult(Primary, DUT_NodeId);
                ReportService.LogTitle(string.Format("after 6 sec: {0}({1}), {2}%", res.Total, res.Fails, res.SuccessPercentRate));
            }
            BasicResult.Test resTotal = ApplicationService.StopTrafficTest(Primary, DUT_NodeId);
            ReportService.LogTitle(string.Format("Total: {0}({1}), {2}%", resTotal.Total, resTotal.Fails, resTotal.SuccessPercentRate));
        }
    }
}