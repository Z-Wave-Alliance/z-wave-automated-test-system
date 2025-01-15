// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Smart Start Inclusion of Door Lock via repeaters, Stress Test: SmartStartHop_dev", Tags.Long)]
    [DeviceMapping(1, LibTypes.Controller, Hop = "A")]
    [DeviceMapping(2, LibTypes.EndDeviceAny)]
    [DeviceMapping(3, LibTypes.EndDeviceAny)]
    [DeviceMapping(4, LibTypes.EndDeviceAny)]
    [TestInterfaceDeviceMapping(6, LibTypes.DoorLockKeyPad)]
    [MeshNetworkRouterMapping(7, "r1")]

    public class SmartStartHopStat_dev : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {

            Alias Primary = new Alias(1);
            Alias Rep1 = new Alias(2);
            Alias Rep2 = new Alias(3);
            Alias Rep3 = new Alias(4);
            Alias DUT = new Alias(6);
            Alias Router = new Alias(7);

            Alias[] ALL = new Alias[] { Primary, Rep1, Rep2, Rep3 };
            ParallelService.Reset(ALL);


            ApplicationService.AddSecurity(Primary, SecurityVersions.S2 | SecurityVersions.S0);
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Primary);
            ApplicationService.SetApplicationCmd(Rep1, true, true, LibTypes.SwitchOnOff);
            ApplicationService.SetApplicationCmd(Rep2, true, true, LibTypes.SwitchOnOff);
            ApplicationService.SetApplicationCmd(Rep3, true, true, LibTypes.SwitchOnOff);

            ReportService.LogTitle("Enable all channels");
            NetworkRouterService.SetChannels(Router, DbLevels.db0);

            ReportService.LogTitle("Excluding DUT");
            AssertIsTrue(TestInterfaceService.ExcludeNode(Primary, DUT),
                "Exclusion of the DUT was successful",
                "Exclusion of the DUT failed");

            ReportService.LogTitle("Including Repeaters");
            var rep1InclRes = Service.IncludeNode(Primary, Rep1);
            AssertIsTrue(rep1InclRes, "Successful inclusion of Repeater 1", "Inclusion of Repeater 1 failed");
            var rep2InclRes = Service.IncludeNode(Primary, Rep2);
            AssertIsTrue(rep2InclRes, "Successful inclusion of Repeater 2", "Inclusion of Repeater 2 failed");
            var rep3InclRes = Service.IncludeNode(Primary, Rep3);
            AssertIsTrue(rep2InclRes, "Successful inclusion of Repeater 3", "Inclusion of Repeater 3 failed");
            var repeaterIds = new byte[] { rep1InclRes.AddNodeResult.NodeId, rep2InclRes.AddNodeResult.NodeId, rep3InclRes.AddNodeResult.NodeId, 0 };


            ReportService.LogTitle("Building route between controller and the DUT");
            NetworkRouterService.SetChannels(Router, DbLevels.db95);
            NetworkRouterService.SetChannel(Router, Primary, Rep1, DbLevels.db25);
            NetworkRouterService.SetChannel(Router, Rep1, Rep2, DbLevels.db25);
            NetworkRouterService.SetChannel(Router, Rep2, Rep3, DbLevels.db25);
            NetworkRouterService.SetChannel(Router, Rep3, DUT, DbLevels.db25);


            int testIter = 200;

            int smartstartErrors = 0;
            int communicationErrors = 0;

            for (int i = 0; i <= testIter; i++)
            {
                TestInterfaceService.SoftReset(DUT);
                ReportService.LogTitle("The DUT is being included with Smart Start");
                ApplicationService.Provisioning.Add(Primary, DUT, SecuritySchemeSet.ALL);
                var addToken = AsyncService.ExpectSmartStartAdd(Primary, 300000);
                var addRes = addToken.WaitCompletedSignal();

                if (addRes && addRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done)
                {
                    ReportService.LogOk("S2 SmartStart inclusion of the DUT was successful");
                    ReportService.LogTitle("Sending Version Get from Controller to the DUT to check communication");

                    var replyToken = AsyncService.ExpectData(Primary, new COMMAND_CLASS_VERSION_V3.VERSION_REPORT(), 10000);
                    var sendRes = ComplianceService.SendData(Primary, addRes.AddNodeResult.Node, new COMMAND_CLASS_VERSION_V3.VERSION_GET());
                    var replyRes = replyToken.WaitCompletedSignal();

                    if (sendRes.TransmitStatus == TransmitStatuses.CompleteOk &&
                        sendRes.Repeaters.SequenceEqual(repeaterIds) &&
                        replyRes
                        )
                    {
                        ReportService.LogOk("Successful communication via repeaters");
                    }
                    else
                    {
                        ReportService.LogError("Communication error detected");
                        communicationErrors++;
                    }
                    /*ReportService.LogTitle("Reset node locally. It should be included back with Smart Start");
                    TestInterfaceService.ButtonActions.Sample_Reset(DUT);
                    Delay(20000, "Delay until reset is complete");*/

                    var NWERes = TestInterfaceService.ExcludeNode(Primary, DUT, Modes.NodeOptionNetworkWide | Modes.NodeAny);
                    AssertIsTrue(NWERes, "NWE of the DUT was successful", "NWE of the DUT failed");
                }
                else
                {
                    ReportService.LogError("SmartStart inclusion issue");
                    smartstartErrors++;
                }

            }

            double smartstartErrPercent = (double)((double)smartstartErrors / (double)testIter);
            double communicationErrPercent = (double)((double)communicationErrors / (double)(testIter - smartstartErrors));

            ReportService.LogText($"Smart Start failed in {smartstartErrPercent:%#0.00} of cases");
            ReportService.LogText($"Communication failed in {communicationErrors:%#0.00} of successful inclusions");

        }
    }
}