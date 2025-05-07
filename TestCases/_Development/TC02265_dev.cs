// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
﻿using System;
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("Security v2 PC Controller - S2 Transfer End - Included Node")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    class TC02265_dev : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);

            Alias[] ALL = new Alias[] { Primary, Secondary };
            ParallelService.Reset(ALL);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S2 | SecurityVersions.S0);
            ApplicationService.AddSecurity(Secondary, SecurityVersions.S2 | SecurityVersions.S0);
            ApplicationService.AddResponse(Secondary, new COMMAND_CLASS_BASIC.BASIC_REPORT(), new COMMAND_CLASS_BASIC.BASIC_GET());


            ReportService.LogText("1. Include Secondary to Primary controller and save the Access Key from Primary Controller.");
            BasicResult.AddNodeWithLearnMode addRes = Service.AddNode(Primary, Secondary);
            var addedNodeId = addRes.NodeId;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            AssertIsTrue(SubstituteStatuses.Done == addRes.SubstituteStatus,
                "In 1. verify that inclusion was successful and that security v2 is enabled.");

            byte[] NetKeyS2cls2 = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_ACCESS);
            var randomValueKey = new byte[16];
            new Random(byte.MaxValue).NextBytes(randomValueKey);

            for (int i = 0; i < 5; i++)
            {
                if (i < 4)
                {
                    ReportService.LogText((i * 4 + 2) + ". Send non-secured S2 Transfer End to secondary controller.");
                    var primaryListenToken = AsyncService.ListenData(Primary);
                    Service.SendDataEx(Primary, Secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_TRANSFER_END(), SecuritySchemes.NONE);
                    AsyncService.CancelToken(Primary, primaryListenToken);
                    var listenRes = ReceivedDataCollection.FromListenResult(primaryListenToken.WaitCompletedSignal());
                    var receivedCount = listenRes.ReceivedData.Where(x => x.Command[1] != COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT.ID).Count();
                    AssertIsTrue(receivedCount == 0, "In" + (i * 4 + 2) + " verify that the receiver rejects the Transfer End request.");

                    ReportService.LogText((i * 4 + 3) + ". Send S2 Message Encapsulated S2 Transfer End.");
                    primaryListenToken = AsyncService.ListenData(Primary);
                    Service.SendDataEx(Primary, Secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_TRANSFER_END(), SecuritySchemes.S2_ACCESS);
                    AsyncService.CancelToken(Primary, primaryListenToken);
                    listenRes = ReceivedDataCollection.FromListenResult(primaryListenToken.WaitCompletedSignal());
                    receivedCount = listenRes.ReceivedData.Where(x => x.Command[1] != COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT.ID).Count();
                    AssertIsTrue(receivedCount == 0, "In" + (i * 4 + 3) + " verify that the receiver rejects the Transfer End request.");

                    ReportService.LogText((i * 4 + 4) + ". Send S2 Message Encapsulated S2 Transfer End, overriding Access Key with the random network key:" + randomValueKey.ToHexString());
                    ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, randomValueKey, SecuritySchemes.S2_ACCESS);
                    primaryListenToken = AsyncService.ListenData(Primary);
                    Service.SendDataEx(Primary, Secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_TRANSFER_END(), SecuritySchemes.S2_ACCESS);
                    AsyncService.CancelToken(Primary, primaryListenToken);
                    ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, NetKeyS2cls2, SecuritySchemes.S2_ACCESS);
                    listenRes = ReceivedDataCollection.FromListenResult(primaryListenToken.WaitCompletedSignal());
                    receivedCount = listenRes.ReceivedData.Where(x => x.Command[1] != COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT.ID).Count();
                    AssertIsTrue(receivedCount == 0, "In" + (i * 4 + 4) + " verify that the receiver rejects the Transfer End request.");
                }
                else
                {
                    var zt = ZnifferService.ExpectData(10000);
                    zt.SetUpWatch(0x02, 0x01, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT(), HeaderTypes.SINGLECAST, false);
                    Service.SendDataEx(Primary, Secondary, new COMMAND_CLASS_BASIC_V2.BASIC_GET(), SecuritySchemes.S2_ACCESS);
                    var ztRes = zt.WaitCompletedSignal();
                    var isSOS = false;
                    if (ztRes && ztRes.Frames.Count > 0)
                    {
                        isSOS = ((COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT)ztRes.Frames[0].Payload).properties1.sos == 1;
                    }
                    AssertIsTrue(isSOS, "In 17. 21. verify responds with S2 Nonce Report with the flag SOS set to true.");
                }
                switch ((i * 4 + 5))
                {
                    case (5):
                        ReportService.LogText("5. Power reset secondary controller application.");
                        ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(Primary, 0x02);
                        break;
                    case (9):
                        ReportService.LogText("9. Power reset primary controller application.");
                        ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(Secondary, 0x01);
                        break;
                    case (13):
                        ReportService.LogText("13. Power reset primary controller and secondary controller application.");
                        ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(Primary, 0x02);
                        ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(Secondary, 0x01);
                        break;
                    case (17):
                        ReportService.LogText("17. Send a S2 Message Encapsulated BASIC GET to secondary controller using a wrong SPAN(In Primary PC Controller Node Settings press 'Next Span' more than 5 times).");
                        ApplicationService.SecurityTestSettings.NextSpanS2ForNode(Primary, 0x02, 6);
                        break;
                    case (21):
                        ReportService.LogText("21. Send a S2 Message Encapsulated BASIC GET to secondary controller using a wrong Network Key(in Security Settings override Access Key with random key 16 bytes)");
                        ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, randomValueKey, SecuritySchemes.S2_ACCESS);

                        break;
                    default:
                        break;
                }
            }
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, NetKeyS2cls2, SecuritySchemes.S2_ACCESS);

            ReportService.LogText("22. Send non-secured S2 KEX Get to secondary controller.)");
            var lt = AsyncService.ListenData(Primary);
            Service.SendDataEx(Primary, Secondary, new COMMAND_CLASS_SECURITY_2.KEX_GET(), SecuritySchemes.NONE);
            AsyncService.CancelToken(Primary, lt);
            var ltRes = ReceivedDataCollection.FromListenResult(lt.WaitCompletedSignal());
            AssertIsTrue(ltRes.Count == 0, "In 22. verify that the receiver ignores S2 KEX Get.");

            ReportService.LogText("23. Send S2 Message Encapsulated S2 KEX Get.");
            lt = AsyncService.ListenData(Primary);
            Service.SendDataEx(Primary, Secondary, new COMMAND_CLASS_SECURITY_2.KEX_GET(), SecuritySchemes.S2_ACCESS);
            AsyncService.CancelToken(Primary, lt);
            ltRes = ReceivedDataCollection.FromListenResult(lt.WaitCompletedSignal());
            var ltResCount = ltRes.ReceivedData.Where(x => x.Command[1] != COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT.ID).Count();
            AssertIsTrue(ltResCount == 0, "In 23. verify that the receiver ignores S2 KEX Get.");

            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(Secondary, 0x01);

            ReportService.LogText("24. Send S2 Message Encapsulated, overriding Access Key with the random network key, S2 KEX Get.");
            var zt1 = ZnifferService.ExpectData(10000);
            zt1.SetUpWatch(0x02, 0x01, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT(), HeaderTypes.SINGLECAST, false);
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, randomValueKey, SecuritySchemes.S2_ACCESS);
            Service.SendDataEx(Primary, Secondary, new COMMAND_CLASS_SECURITY_2.KEX_GET(), SecuritySchemes.S2_ACCESS);
            var ztRes1 = zt1.WaitCompletedSignal();
            var isSOS1 = false;
            if (ztRes1 && ztRes1.Frames.Count > 0)
            {
                isSOS1 = ((COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT)ztRes1.Frames[0].Payload).properties1.sos == 1;
            }
            AssertIsTrue(isSOS1, "In 17. 21. verify responds with S2 Nonce Report with the flag SOS set to true.");
        }

    }
}
