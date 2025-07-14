// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("Security v2 PC Controller - Inclusion - Interrupt Points",
        Tags.Long, Tags.S2, Tags.PCController)]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    class TC02155_dev : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);
            Alias[] ALL = new Alias[] { Primary, Secondary };
            ParallelService.Reset(ALL);

            BasicResult.AddNodeWithLearnMode addRes;
            byte[] testCommand= new byte[16];

            ApplicationService.AddSecurity(Primary, SecurityVersions.S2 | SecurityVersions.S0);
            ApplicationService.AddSecurity(Secondary, SecurityVersions.S2 | SecurityVersions.S0);
            ReportService.LogText("1. Add secondary PC controller to primary PC controller but taking in to account at the primary controller, in Security Test schema:");
            

            #region 1.1
            ReportService.LogText("1.1 Send the wrong KEX SET: Schema Command: {9F 06 00 00 FF 00} (non-valid ECDH profile).");
            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Primary);
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary, SecurityS2TestFrames.KEXSet, new byte[] { 0x9F, 0x06, 0x00, 0x00, 0xFF, 0x00 });

            addRes = Service.AddNode(Primary, Secondary);

            var addedNodeId = addRes.Node;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            Assert.IsFalse(SubstituteStatuses.Done == addRes.SubstituteStatus, "In 1.1 verify that secure inclusion is interrupted");
            #endregion


            #region 1.2
            ParallelService.Reset(ALL);
            ReportService.LogText("1.2 Send the wrong Public Key value: Schema Command: {9F 08 01 + any number of random Bytes} (including node).");
            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Primary);
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary, SecurityS2TestFrames.PublicKeyReportA, new byte[] { 0x9F, 0x08, 0x01, 0x00, 0xFF, 0x00 });

            addRes = Service.AddNode(Primary, Secondary);

            addedNodeId = addRes.NodeId;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            Assert.IsFalse(SubstituteStatuses.Done == addRes.SubstituteStatus, "In 1.2 verify that secure inclusion is interrupted");
            #endregion


            #region 1.3
            ParallelService.Reset(ALL);
            ReportService.LogText("1.3 Send the wrong KEX Report Echo: Schema Command: {9F 06 01 00 FF 00} (non-valid ECDH profile).");
            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Primary);
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary, SecurityS2TestFrames.KEXReportEcho, new byte[] { 0x9F, 0x06, 0x01, 0x00, 0xFF, 0x00 });

            addRes = Service.AddNode(Primary, Secondary);

            addedNodeId = addRes.Node;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            Assert.IsFalse(SubstituteStatuses.Done == addRes.SubstituteStatus, "In 1.3 verify that secure inclusion is interrupted");
            #endregion

            
            #region 1.4
            ParallelService.Reset(ALL);
            ReportService.LogText("1.4.1 Send S2 Network Key Report but with a wrong network key (repeat for all Unauthenticated, Authenticated, Access & S0): Schema Command: {9F 0A 00 + 16 random Bytes} (Unauthenticated).");
            testCommand = new byte[19];
            testCommand[0] = 0x9f;
            testCommand[1] = 0x0A;
            testCommand[2] = 0x00;

            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Primary);
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary, SecurityS2TestFrames.NetworkKeyReport_S2Unauthenticated, testCommand);

            addRes = Service.AddNode(Primary, Secondary);

            addedNodeId = addRes.NodeId;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            Assert.IsFalse(SubstituteStatuses.Done == addRes.SubstituteStatus, "In 1.4.1 verify that secure inclusion is interrupted");

            //

            ParallelService.Reset(ALL);
            ReportService.LogText("1.4.2 Send S2 Network Key Report but with a wrong network key (repeat for all Unauthenticated, Authenticated, Access & S0): Schema Command: {9F 0A 01 + 16 random Bytes} (Authenticated).");
            testCommand[2] = 0x01;

            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Primary);
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary, SecurityS2TestFrames.NetworkKeyReport_S2Authenticated, testCommand);

            addRes = Service.AddNode(Primary, Secondary);

            addedNodeId = addRes.Node;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            Assert.IsFalse(SubstituteStatuses.Done == addRes.SubstituteStatus, "In 1.4.2 verify that secure inclusion is interrupted");

            //

            ParallelService.Reset(ALL);
            ReportService.LogText("1.4.3 Send S2 Network Key Report but with a wrong network key (repeat for all Unauthenticated, Authenticated, Access & S0): Schema Command: {9F 0A 02 + 16 random Bytes} (Access).");
            testCommand[2] = 0x02;

            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Primary);
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary, SecurityS2TestFrames.NetworkKeyReport_S2Access, testCommand);

            addRes = Service.AddNode(Primary, Secondary);

            addedNodeId = addRes.Node;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            Assert.IsFalse(SubstituteStatuses.Done == addRes.SubstituteStatus, "In 1.4.3 verify that secure inclusion is interrupted");

            //

            ParallelService.Reset(ALL);
            ReportService.LogText("1.4.4 Send S2 Network Key Report but with a wrong network key (repeat for all Unauthenticated, Authenticated, Access & S0): Schema Command: {9F 0A 07 + 16 random Bytes} (S0).");
            testCommand[2] = 0x07;

            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Primary);
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary, SecurityS2TestFrames.NetworkKeyReport_S0, testCommand);

            addRes = Service.AddNode(Primary, Secondary);

            addedNodeId = addRes.Node;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            Assert.IsFalse(SubstituteStatuses.Done == addRes.SubstituteStatus, "In 1.4.4 verify that secure inclusion is interrupted");
            #endregion


            #region 1.5
            ParallelService.Reset(ALL);
            ReportService.LogText("1.5 Send S2 Nonce Report without SOS nor MOS flag set and empty REI: Schema Command: {9F 02 00 00}.");
            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Primary);
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary, SecurityS2TestFrames.NonceReport, new byte[] { 0x9f, 0x02, 0x00, 0x00 });

            addRes = Service.AddNode(Primary, Secondary);

            addedNodeId = addRes.Node;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            Assert.IsFalse(SubstituteStatuses.Done == addRes.SubstituteStatus, "In 1.5 verify that secure inclusion is interrupted");
            #endregion


            #region 1.6
            ParallelService.Reset(ALL);
            ReportService.LogText("1.6 Send S2 Nonce Report with both SOS and MOS flag set and empty REI: Schema Command: {9F 02 00 03}.");
            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Primary);
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary, SecurityS2TestFrames.NonceReport, new byte[] { 0x9f, 0x02, 0x00, 0x03 });

            addRes = Service.AddNode(Primary, Secondary);

            addedNodeId = addRes.Node;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            Assert.IsFalse(SubstituteStatuses.Done == addRes.SubstituteStatus, "In 1.6 verify that secure inclusion is interrupted");
            #endregion


            #region 1.7
            ParallelService.Reset(ALL);
            ReportService.LogText("1.7 Send S2 Nonce Report with SOS set and MOS flag not set and a static REI: Schema Command: {9F 02 00 01 + 16 random Bytes}.");
            testCommand = new byte[20];
            testCommand[0] = 0x9f;
            testCommand[1] = 0x02;
            testCommand[2] = 0x00;
            testCommand[1] = 0x01;

            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Primary);
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary, SecurityS2TestFrames.NonceReport, testCommand);
            
            addRes = Service.AddNode(Primary, Secondary);

            addedNodeId = addRes.Node;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            Assert.IsFalse(SubstituteStatuses.Done == addRes.SubstituteStatus, "In 1.7 verify that secure inclusion is interrupted");
            #endregion


            
            #region 1.8
            ParallelService.Reset(ALL);
            ReportService.LogText("1.8 Send S2 Nonce Report with SOS not set and MOS flag set and empty REI: Schema Command: {9F 02 00 02}.");
            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Primary);
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary, SecurityS2TestFrames.NonceReport, new byte[] { 0x9f, 0x02, 0x00, 0x02 });
            
            addRes = Service.AddNode(Primary, Secondary);

            addedNodeId = addRes.NodeId;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            Assert.IsFalse(SubstituteStatuses.Done == addRes.SubstituteStatus, "In 1.8 verify that secure inclusion is interrupted");
            #endregion

            
            ReportService.LogText("2. Reset both controllers.");
            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Secondary);
            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Primary);
            ParallelService.Reset(ALL);

            
            ReportService.LogText("3. Add secondary PC controller to primary PC controller but taking in to account at the secondary controller:");
            
            #region 3.1
            ReportService.LogText("3.1 Send the wrong KEX Report Schema Command: {9F 05 00 00 FF 00} (non-valid ECDH profile).");
            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Secondary);
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Secondary, SecurityS2TestFrames.KEXReport, new byte[] { 0x9f, 0x05, 0x00, 0x00, 0x0FF, 0x00 });
            
            addRes = Service.AddNode(Primary, Secondary);

            addedNodeId = addRes.Node;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            Assert.IsFalse(SubstituteStatuses.Done == addRes.SubstituteStatus, "In 3.1 verify that secure inclusion is interrupted");
            #endregion


            #region 3.2
            ParallelService.Reset(ALL);
            ReportService.LogText("3.2 Send the wrong Public Key value. Schema Command: {9F 08 00 + any random number of bytes} (included node).");
            testCommand = new byte[19];
            testCommand[0] = 0x9f;
            testCommand[1] = 0x08;
            testCommand[2] = 0x00;

            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Secondary);
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Secondary, SecurityS2TestFrames.KEXReport, testCommand);

            addRes = Service.AddNode(Primary, Secondary);

            addedNodeId = addRes.Node;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            Assert.IsFalse(SubstituteStatuses.Done == addRes.SubstituteStatus, "In 3.2 verify that secure inclusion is interrupted");
            #endregion

            #region 3.3
            ParallelService.Reset(ALL);
            ReportService.LogText("3.3 Send the wrong KEX Set Echo Schema Command: {9F 06 01 00 FF 00} (Echo & non-valid ECDH profile).");
            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Secondary);
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Secondary, SecurityS2TestFrames.KEXSetEcho, new byte[] { 0x9F, 0x06, 0x01, 0x00, 0xFF, 0x00 });

            addRes = Service.AddNode(Primary, Secondary);

            addedNodeId = addRes.Node;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            Assert.IsFalse(SubstituteStatuses.Done == addRes.SubstituteStatus, "In 3.3 verify that secure inclusion is interrupted");
            #endregion


            #region 3.4
            ParallelService.Reset(ALL);
            ReportService.LogText("3.4 Send the Nonce Report without SOS nor MOS flag set and empty REI: Schema Command: {9F 02 00 00}.");
            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Secondary);
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Secondary, SecurityS2TestFrames.NonceReport, new byte[] { 0x9F, 0x02, 0x00, 0x00 });

            addRes = Service.AddNode(Primary, Secondary);

            addedNodeId = addRes.Node;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            Assert.IsFalse(SubstituteStatuses.Done == addRes.SubstituteStatus, "In 3.4 verify that secure inclusion is interrupted");
            #endregion


            #region 3.5
            ParallelService.Reset(ALL);
            ReportService.LogText("3.5 Send S2 Nonce Report with both SOS and MOS flag set and empty REI: Schema Command: {9F 02 00 03}.");
            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Secondary);
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Secondary, SecurityS2TestFrames.NonceReport, new byte[] { 0x9F, 0x02, 0x00, 0x03 });

            addRes = Service.AddNode(Primary, Secondary);

            addedNodeId = addRes.Node;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            Assert.IsFalse(SubstituteStatuses.Done == addRes.SubstituteStatus, "In 3.5 verify that secure inclusion is interrupted");
            #endregion


            #region 3.6
            ParallelService.Reset(ALL);
            ReportService.LogText("3.6 Send S2 Nonce Report with SOS set and MOS flag not set and a static REI: Schema Command: {9F 02 00 01 + 16 random Bytes}.");
            testCommand = new byte[20];
            testCommand[0] = 0x9F;
            testCommand[1] = 0x02;
            testCommand[2] = 0x00;
            testCommand[3] = 0x01;

            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Secondary);
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Secondary, SecurityS2TestFrames.NonceReport, testCommand);

            addRes = Service.AddNode(Primary, Secondary);

            addedNodeId = addRes.Node;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            Assert.IsFalse(SubstituteStatuses.Done == addRes.SubstituteStatus, "In 3.6 verify that secure inclusion is interrupted");
            #endregion


            #region 3.7
            ParallelService.Reset(ALL);
            ReportService.LogText("3.7 Send S2 Nonce Report with SOS not set and MOS flag set and empty REI: Schema Command: {9F 02 00 02}.");
            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Secondary);
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Secondary, SecurityS2TestFrames.NonceReport, new byte[] { 0x9F, 0x02, 0x00, 0x02 });

            addRes = Service.AddNode(Primary, Secondary);

            addedNodeId = addRes.Node;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            Assert.IsFalse(SubstituteStatuses.Done == addRes.SubstituteStatus, "In 3.7 verify that secure inclusion is interrupted");
            #endregion


            #region 3.8
            ReportService.LogText("3.8 Send S0/S2 Network Key Verify with a wrong Network Key verify, in security test schema*: ");
            ReportService.LogText("'Is Encrypted', selected and activated; 'Network Key', selected and filled with 16 random Bytes; 'Is Temp Network Key', selected and activated;");

            ParallelService.Reset(ALL);
            ReportService.LogText("3.8.1 S0 Network key.");
            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Secondary);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Secondary, SecurityS2TestFrames.NetworkKeyVerify_S0, true);
            ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2Temp(Secondary, SecurityS2TestFrames.NetworkKeyVerify_S0, new byte[16]);

            addRes = Service.AddNode(Primary, Secondary);

            addedNodeId = addRes.Node;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            Assert.IsFalse(SubstituteStatuses.Done == addRes.SubstituteStatus, "In 3.8.1 verify that secure inclusion is interrupted");

            //

            ParallelService.Reset(ALL);
            ReportService.LogText("3.8.2 S2 Network key class0.");
            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Secondary);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Secondary, SecurityS2TestFrames.NetworkKeyVerify_S2Access, true);
            ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2Temp(Secondary, SecurityS2TestFrames.NetworkKeyVerify_S2Access, new byte[16]);

            addRes = Service.AddNode(Primary, Secondary);

            addedNodeId = addRes.Node;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            Assert.IsFalse(SubstituteStatuses.Done == addRes.SubstituteStatus, "In 3.8.2 verify that secure inclusion is interrupted");
            
            //

            ParallelService.Reset(ALL);
            ReportService.LogText("3.8.3 S2 Network key class 1.");
            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Secondary);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Secondary, SecurityS2TestFrames.NetworkKeyVerify_S2Authenticated, true);
            ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2Temp(Secondary, SecurityS2TestFrames.NetworkKeyVerify_S2Authenticated, new byte[16]);

            addRes = Service.AddNode(Primary, Secondary);

            addedNodeId = addRes.Node;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            Assert.IsFalse(SubstituteStatuses.Done == addRes.SubstituteStatus, "In 3.8.3 verify that secure inclusion is interrupted");

            //

            ParallelService.Reset(ALL);
            ReportService.LogText("3.8.4 S2 Network key class 2.");
            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Secondary);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Secondary, SecurityS2TestFrames.NetworkKeyVerify_S2Unauthenticated, true);
            ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2Temp(Secondary, SecurityS2TestFrames.NetworkKeyVerify_S2Unauthenticated, new byte[16]);

            addRes = Service.AddNode(Primary, Secondary);

            addedNodeId = addRes.Node;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            Assert.IsFalse(SubstituteStatuses.Done == addRes.SubstituteStatus, "In 3.8.4 verify that secure inclusion is interrupted");
            
            #endregion

        }
    }
}
