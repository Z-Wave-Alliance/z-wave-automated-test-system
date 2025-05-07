// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"existing network with DUT
    -SendData 
    -Stress test")]
    [DeviceMapping(1, "moxa2", LibTypes.ControllerStaticLib)]
    public class stressbasic_custom_dut_RequestData2Secure : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            //-----------------------------------------------
            //Existing NETWORK_KEY
            byte[] NetworkKey = "37 18 E3 C6 E6 B4 0E 02 D3 31 A5 81 D9 57 07 03".ToByteArray();
            //
            //DUT NodeId
            byte DUT_NodeId1 = 6;
            byte DUT_NodeId2 = 7;
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

            ApplicationService.StartTrafficToNodeIdTest(Primary, DUT_NodeId1, Data, ExpectedData, IntervalMs, TimeoutMs);
            ApplicationService.StartTrafficToNodeIdTest(Primary, DUT_NodeId2, Data, ExpectedData, IntervalMs, TimeoutMs);
            for (int i = 0; i < 10; i++)
            {
                Delay(6000);
                BasicResult.Test res1 = ApplicationService.GetTrafficTestResult(Primary, DUT_NodeId1);
                BasicResult.Test res2 = ApplicationService.GetTrafficTestResult(Primary, DUT_NodeId2);
                ReportService.LogTitle(string.Format("Node1 after 6 sec: {0}({1}), {2}%", res1.Total, res1.Fails, res1.SuccessPercentRate));
                ReportService.LogTitle(string.Format("Node2 after 6 sec: {0}({1}), {2}%", res2.Total, res2.Fails, res2.SuccessPercentRate));
            }
            BasicResult.Test resTotal1 = ApplicationService.StopTrafficTest(Primary, DUT_NodeId1);
            BasicResult.Test resTotal2 = ApplicationService.StopTrafficTest(Primary, DUT_NodeId2);
            ReportService.LogTitle(string.Format("Total Node1: {0}({1}), {2}%", resTotal1.Total, resTotal1.Fails, resTotal1.SuccessPercentRate));
            ReportService.LogTitle(string.Format("Total Node2: {0}({1}), {2}%", resTotal2.Total, resTotal2.Fails, resTotal2.SuccessPercentRate));
        }
    }
}