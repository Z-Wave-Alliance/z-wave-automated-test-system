// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Security v2 PC Controller - S2 Public Key Report Command")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    public class TC02463_dev : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Controller1 = new Alias(1);
            Alias DUT = new Alias(2);
            Alias[] ALL = new Alias[] { Controller1, DUT };
            ParallelService.SetDefault(ALL);

            BasicResult.AddNodeWithLearnMode addRes;
            ZnifferToken.ExpectData znifferToken;
            ZnifferResult.ExpectData zniRes;

            ApplicationService.AddSecurity(Controller1, SecurityVersions.S2 | SecurityVersions.S0);
            ApplicationService.AddSecurity(DUT, SecurityVersions.S2 | SecurityVersions.S0);


            ReportService.LogText("1. Set PC Controller 1 to send Public Key Report with reserved field set to a random value");
            ReportService.LogText("(in S2 Test Scheme in 'Message overriders' select PublicKeyReportA, mark Command and write 9F 08 03, click Set and OK).");
            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Controller1);
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Controller1, SecurityS2TestFrames.PublicKeyReportA, new byte[] { 0x9F, 0x08, 0x03 });
            
            ReportService.LogText("1.2 Include DUT to PC Controller 1. DUT sends Public Key Report to PC Controller 1.");
            znifferToken = ZnifferService.ExpectData(20000);
            znifferToken.SetUpWatch(0x02, 0x01, null, new COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT(), null, null, true, 1, false);

            addRes = Service.AddNode(Controller1, DUT);
            
            var addedNodeId = addRes.Node;
            AssertIsTrue(addRes, "In 1. verify that the inclusion was successful, DUT Id = " + addedNodeId);
            zniRes = znifferToken.WaitCompletedSignal();
            AssertIsTrue(zniRes, "In 1.2 :");
            COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT pkr = zniRes.Frames[0].Payload;
            Assert.AreEqual(0, pkr.properties1.reserved, "-DUT sends Public Key Report with reserved field set to 0 [009F.01.08.11.002]");
            Assert.AreEqual(0, pkr.ecdhPublicKey[0], "-DUT hides the first two bytes of its ECDH Public Key[009F.01.08.11.008]");
            Assert.AreEqual(0, pkr.ecdhPublicKey[1], "-DUT hides the first two bytes of its ECDH Public Key[009F.01.08.11.008]");
            Assert.AreEqual(0, pkr.properties1.includingNode, "-DUT sets the Public Key Report Including Node field to 0 [009F.01.08.11.005]");


            ReportService.LogText("1.3 PC Controller 1 sends Public Key Report to DUT.");
            znifferToken = ZnifferService.ExpectData(20000);
            znifferToken.SetUpWatch(addedNodeId, 0x01, new byte[] { COMMAND_CLASS_SECURITY_2.ID }, null, false);
            
            ApplicationService.SuspendSecurity(Controller1);
            Service.SendData(Controller1, DUT, new COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT());
            ApplicationService.ResumeSecurity(Controller1);
            
            using (ZnifferService.Settings.IsExpectingFailure.Set())
            {
                zniRes = znifferToken.WaitCompletedSignal();
            }
            AssertIsTrue(!zniRes, "In 1.3 DUT ignores the received Public Key Report [009F.01.08.11.001].");

            ReportService.LogText("1.4 Reset PC Controller 1 and DUT. Clear 'Command'.");
            ParallelService.Reset(ALL);
            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Controller1);

            ReportService.LogText("2. Include PC Controller 1 to DUT.");
            ZnifferService.SetSecuritySupport(false);
            znifferToken = ZnifferService.ExpectData(60000);
            znifferToken.SetUpWatch(0x01, 0x02, new COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT(), null, false);
            
            addRes = Service.AddNode(DUT, Controller1);

            addedNodeId = addRes.NodeId;
            AssertIsTrue(addRes, "verify that the inclusion was successful, DUT Id = " + addedNodeId);
            zniRes = znifferToken.WaitCompletedSignal();
            AssertIsTrue(zniRes, "In 2: Public Key Report received ****");
            pkr = zniRes.Frames[0].Payload;
            Assert.AreEqual(1, pkr.properties1.includingNode, "In 2. verify that DUT sets the Public Key Report Including Node field to 1 [009F.01.08.11.003].");

            
            ReportService.LogText("2.1 Reset PC Controller 1 and DUT.");
            ParallelService.Reset(ALL);


            Console.WriteLine("3. Set PC Controller 1 to only request Unauthenticated key.");
            ApplicationService.EnableNetworkKey(Controller1, SecuritySchemes.S2_ACCESS, false);
            ApplicationService.EnableNetworkKey(Controller1, SecuritySchemes.S2_AUTHENTICATED, false);
            
            ReportService.LogText("3.1Include PC Controller 1 to DUT.");
            ZnifferService.SetSecuritySupport(false);
            znifferToken = ZnifferService.ExpectData(60000);
            znifferToken.SetUpWatch(0x01, 0x02, new COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT(), null, false);

            addRes = Service.AddNode(DUT, Controller1);

            addedNodeId = addRes.NodeId;
            AssertIsTrue(addRes, "In 3.1 verify that the inclusion was successful, DUT Id = " + addedNodeId);
            zniRes = znifferToken.WaitCompletedSignal();
            AssertIsTrue(zniRes, "In 3.2 : Public Key Report received ");
            pkr = zniRes.Frames[0].Payload;
            AssertIsTrue(0 != pkr.ecdhPublicKey[0], "In 3. verify that DUT does not hide any of its ECDH bytes in Public Key Report [009F.01.08.11.00B].");
            AssertIsTrue(0 != pkr.ecdhPublicKey[1], "In 3. verify that DUT does not hide any of its ECDH bytes in Public Key Report [009F.01.08.11.00B].");
            
        }
    }
}