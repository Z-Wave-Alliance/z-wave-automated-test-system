// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
﻿using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("Security v2 Multi Hop link")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [TestInterfaceDeviceMapping(3, "TI1")]
    class TC02245_dev : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            const byte EXPECTED_EXTENSION_TYPE_SPAN = 0x01;

            Alias primary = new Alias(1);
            Alias secondary = new Alias(2);
            Alias slave1 = new Alias(3);

            Alias[] ALL = new Alias[] { primary, secondary };
            ParallelService.Reset(ALL);

            ApplicationService.AddSecurity(primary, SecurityVersions.S2 | SecurityVersions.S0);
            ApplicationService.AddSecurity(secondary, SecurityVersions.S2 | SecurityVersions.S0);
            ApplicationService.AddResponse(secondary, new COMMAND_CLASS_BASIC.BASIC_REPORT(), new COMMAND_CLASS_BASIC.BASIC_GET());

            ReportService.LogText("1. Add secondary controller and switch on/off to primary controller network.");
            ReportService.LogText("Secondary controller:");
            BasicResult.AddNodeWithLearnMode addRes = Service.AddNode(primary, secondary);
            var addedNodeId = addRes.NodeId;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            AssertIsTrue(SubstituteStatuses.Done == addRes.SubstituteStatus,
                "In 1. verify that inclusion was successful and that security v2 is enabled.");
            Service.SendData(primary, secondary, new COMMAND_CLASS_BASIC.BASIC_SET()); // Sync.

            var addSwitchRes = TestInterfaceService.IncludeNode(primary, slave1);
            var slaveNodeId = addSwitchRes.AddNodeResult.Node;
            AssertIsTrue(addSwitchRes, "Node Id = " + slaveNodeId);
            AssertIsTrue(addSwitchRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done,
                    "In 1. verify that inclusion was successful and that security v2 is enabled.");

            ReportService.LogText("1.1 Remove the antennas from primary and secondary controllers.");
            Service.SetPriorityRoute(primary, secondary, slaveNodeId, 0, 0, 0, 0);


            ReportService.LogText("2. Send BASIC GET from primary controller to secondary controller.");
            var zt = ZnifferService.ExpectData(5000);
            zt.SetUpWatch(new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_GET());
            zt.SetUpWatch(new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT());
            var rd = Service.RequestData(primary, secondary, new COMMAND_CLASS_BASIC_V2.BASIC_GET(), new COMMAND_CLASS_BASIC_V2.BASIC_REPORT());
            AssertIsTrue(rd && rd.RxSubstituteInfo.SubstituteStatus == SubstituteStatuses.Done, "In 2. verify that the secured encapsulated BASIC GET is transmitted,");
            using (ZnifferService.Settings.IsExpectingFailure.Set())
            {
                var ztRes1 = zt.WaitCompletedSignal();
                AssertIsTrue(!ztRes1, "!but no nonce get or nonce report is transmitted.");
            }
            ReportService.LogText("3. Reset SPAN in both PC Controller applications (in Node Settings press 'Next Span' more then 5 times).");
            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(primary, 0x02);
            ApplicationService.SecurityTestSettings.NextSpanS2ForNode(primary, 0x02, 6);
            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(secondary, 0x01);
            ApplicationService.SecurityTestSettings.NextSpanS2ForNode(secondary, 0x01, 6);

            ReportService.LogText("4. Send BASIC GET from primary controller to secondary controller.");
            zt = ZnifferService.ExpectData(10000);
            zt.SetUpWatch(0x01, 0x02, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), HeaderTypes.ROUTED_SINGLECAST, false);
            rd = Service.RequestData(primary, secondary, new COMMAND_CLASS_BASIC_V2.BASIC_GET(), new COMMAND_CLASS_BASIC_V2.BASIC_REPORT());
            AssertIsTrue(rd && rd.RxSubstituteInfo.SubstituteStatus == SubstituteStatuses.Done, "In 4. verify that the secured encapsulated BASIC GET is transmitted,");
            var ztRes = zt.WaitCompletedSignal();

            Delay(500, "Sniffer issue?");
            AssertIsTrue(ztRes && EXPECTED_EXTENSION_TYPE_SPAN == GetExtensionTypeFromS2me(ztRes.Frames[0].Payload), "after the SPAN is established.");


            ReportService.LogText("5. Reset SPAN in primary controller application.");
            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(secondary, 0x01);
            ApplicationService.SecurityTestSettings.NextSpanS2ForNode(secondary, 0x01, 6);

            ReportService.LogText("6. Send BASIC GET from primary controller to secondary controller");
            zt = ZnifferService.ExpectData(10000);
            zt.SetUpWatch(0x01, 0x02, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), HeaderTypes.ROUTED_SINGLECAST, null, false, 4);
            rd = Service.RequestData(primary, secondary, new COMMAND_CLASS_BASIC_V2.BASIC_GET(), new COMMAND_CLASS_BASIC_V2.BASIC_REPORT());
            AssertIsTrue(rd && rd.RxSubstituteInfo.SubstituteStatus == SubstituteStatuses.Done, "In 6. verify that the secured encapsulated BASIC GET is transmitted,");
            var ztcRes = zt.WaitCompletedSignal();
            AssertIsTrue(ztcRes && EXPECTED_EXTENSION_TYPE_SPAN == GetExtensionTypeFromS2me(ztcRes.Frames.Last().Payload), "after the SPAN is established.");


            ReportService.LogText("7. Reset SPAN in secondary controller application.");
            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(primary, 0x02);
            ApplicationService.SecurityTestSettings.NextSpanS2ForNode(primary, 0x02, 6);

            ReportService.LogText("8. Send BASIC GET from primary controller to secondary controller.");
            zt = ZnifferService.ExpectData(5000);
            zt.SetUpWatch(0x01, 0x02, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), HeaderTypes.ROUTED_SINGLECAST, false);
            rd = Service.RequestData(primary, secondary, new COMMAND_CLASS_BASIC_V2.BASIC_GET(), new COMMAND_CLASS_BASIC_V2.BASIC_REPORT());
            AssertIsTrue(rd && rd.RxSubstituteInfo.SubstituteStatus == SubstituteStatuses.Done, "In 8. verify that the secured encapsulated BASIC GET is transmitted,");
            ztRes = zt.WaitCompletedSignal();
            AssertIsTrue(ztRes && EXPECTED_EXTENSION_TYPE_SPAN == GetExtensionTypeFromS2me(ztRes.Frames[0].Payload), "after the SPAN is established.");
        }

        private byte GetExtensionTypeFromS2me(byte[] payload)
        {
            var s2me = (COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION)(payload);
            return s2me.vg1[0].properties1.type;
        }
    }
}
