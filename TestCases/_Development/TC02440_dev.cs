// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;
using System.Linq;

namespace ZatsTests
{
    [ZatsTestCase(@"Security v2 PC Controller S2MEC - MGRP Extension")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    [DeviceMapping(4, LibTypes.EndDeviceLib)]
    class TC02440_dev : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias primary = new Alias(1);
            Alias dut = new Alias(2);
            Alias slave1 = new Alias(3);
            Alias slave2 = new Alias(4);

            Alias[] all = new Alias[] { primary, dut, slave1, slave2 };

            ParallelService.Reset(all);

            Service.ApplicationNodeInformation(slave1, true, (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_SECURITY_2.ID });
            ApplicationService.AddSecurity(slave1, SecurityVersions.S2);
            Service.ApplicationNodeInformation(slave2, true, (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_SECURITY_2.ID });

            ApplicationService.AddSupervision(primary);
            ApplicationService.AddSecurity(primary, SecurityVersions.S2);
            ApplicationService.AddTransportService(primary, 100);
            ApplicationService.AddSupervision(dut);
            ApplicationService.AddSecurity(dut, SecurityVersions.S2);
            ApplicationService.AddTransportService(dut, 100);

            var accessKey = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(primary, SecuritySchemes.S2_ACCESS);
            ReportService.LogText("Active network key: " + accessKey.ToHexString());
            ZnifferService.SetSecuritySupport(true);
            ZnifferService.SetSecurityNetworkKeyS2(accessKey);

            #region 1
            ReportService.LogText("1.- Include DUT and the three Slaves to PC Controller (1) network");
            var addRes = Service.AddNode(primary, dut);
            var dutNodeId = addRes.NodeId;
            AssertIsTrue(addRes, "Added DUT node id = " + dutNodeId);
            AssertIsTrue(SubstituteStatuses.Done == addRes.SubstituteStatus, "Successful securely inclusion");
            var slave1DSK = Service.GetDsk(slave1, 2);
            addRes = Service.AddNode(primary, slave1);
            var slaveId1 = addRes.NodeId;
            AssertIsTrue(addRes, "Added Slave #1 node id = " + addRes.NodeId);
            AssertIsTrue(SubstituteStatuses.Done == addRes.SubstituteStatus, "Successful securely inclusion");
            var slave2DSK = Service.GetDsk(slave2, 2);
            addRes = Service.AddNode(primary, slave2);
            var slaveId2 = addRes.NodeId;
            AssertIsTrue(addRes, "Added Slave #2 node id = " + addRes.NodeId);
            AssertIsTrue(SubstituteStatuses.Done == addRes.SubstituteStatus, "Successful securely inclusion");
            #endregion


            ReportService.LogText("!Sync SPAN");
            var data2send = new COMMAND_CLASS_BASIC.BASIC_SET();
            Service.SendData(dut, primary, data2send);
            Delay(2000, "SendData");


            ReportService.LogText("2.- DUT sends a multicast S2 message encapsulation to PC Controller (1) and to Slaves 1 and 2.");
            #region 2
            var zt = ZnifferService.ExpectData(10000);
            zt.SetUpWatchS2(dutNodeId, 0x01, null, null, (byte)SecurityS2ExtensionTypes.MpanGrp, false, 1); //3
            var nodeIds = new Node[] { 0x01 }; // slaveId1, slaveId2
            ComplianceService.SendDataMulti(dut, nodeIds, data2send, TX_OPTIONS);
            Delay(2000, "SendDataMulti");
            var ztRes = zt.WaitCompletedSignal();
            AssertIsTrue(ztRes, "-The MGRP extension is included in all S2 Multicast and S2 Singlecast follow-up frames [009F.01.03.11.01C]");
            var enEx = (COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION)ztRes.Frames[0].Payload;
            AssertIsTrue(enEx.vg1.Count == 1, "-DUT does not send the MGRP extension together with the MPAN extension [009F.01.03.11.020]");
            AssertIsTrue(enEx.properties1.encryptedExtension == 0, "- DUT sends the MGRP extension unencrypted [009F.01.03.11.021]");
            AssertIsTrue(enEx.vg1[0].extensionLength == 3, "- DUT sets the MGRP extension Length field to 3 [009F.01.03.11.022]");
            AssertIsTrue(enEx.vg1[0].properties1.type == 3, "- DUT sets the MGRP extension Type field to 3 [009F.01.03.11.023]");
            AssertIsTrue(enEx.vg1[0].properties1.critical == 1, "- DUT sets the MGRP extension Critical flag to 1 [009F.01.03.11.024]");
            AssertIsTrue(enEx.vg1[0].properties1.moreToFollow == 0, "- DUT sets the MGRP extension More to Follow flag to 0 [009F.01.03.11.025]");
            #endregion


            ReportService.LogText("3.- Mark 'Force multicast' and enable Test Schema in PC Controller (1), send a S2 Message encapsulation Basic Get to DUT without the MGRP extensions");
            #region 3
            //(mark "Cleanup existing extensions first"):
            ApplicationService.SecurityTestSettings.ClearTestExtensionsS2(primary);
            var tes = new TestExtensionS2Settings(SecurityS2MessageTypes.MulticastAll, SecurityS2ExtensionTypes.MpanGrp, SecurityS2TestExtensionsActions.Delete);
            ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(primary, tes);
            //{Message type=MulticastAll, Extension type=Test, Extension=1 random Byte, enabled Ecrypted=0, enabled Critical=1, enabled length=3, disabled More to follow, enabled Usage=1}. Press "Add" button.
            tes = new TestExtensionS2Settings(SecurityS2MessageTypes.MulticastAll, SecurityS2ExtensionTypes.Test, SecurityS2TestExtensionsActions.Add)
            {
                Value = new byte[1],
                IsEncrypted = false,
                IsCritical = true,
                ExtensionLength = 3,
                NumOfUsage = 1
            };
            ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(primary, tes);
            tes = new TestExtensionS2Settings(SecurityS2MessageTypes.SinglecastAll, SecurityS2ExtensionTypes.MpanGrp, SecurityS2TestExtensionsActions.Delete);
            ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(primary, tes);
            //{Message type=SinglecastALL, Extension type=Test, Extension=1 random Byte, enabled Ecrypted=0, enabled Critical=1, enabled length=3, disabled More to follow, enabled Usage=1}. Press "Add" button.
            tes = new TestExtensionS2Settings(SecurityS2MessageTypes.SinglecastAll, SecurityS2ExtensionTypes.Test, SecurityS2TestExtensionsActions.Add)
            {
                Value = new byte[1],
                IsEncrypted = false,
                IsCritical = true,
                ExtensionLength = 3,
                NumOfUsage = 1
            };
            ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(primary, tes);
            //(each will be attached as extension only once to the Multicast and to the Singlecast respectively)

            zt = ZnifferService.ExpectData(10000);
            zt.SetUpWatch(dutNodeId, 0x01, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), false);
            zt.SetUpWatchS2(dutNodeId, 0x01, null, null, (byte)SecurityS2ExtensionTypes.Mos, false, 1);
            nodeIds = new Node[] { 0x02 };
            ComplianceService.SendDataMulti(primary, nodeIds, data2send, TX_OPTIONS);
            Delay(2000, "SendDataMulti");
            using (ZnifferService.Settings.IsExpectingFailure.Set())
            {
                ztRes = zt.WaitCompletedSignal();
            }
            AssertIsTrue(!ztRes, "In 3. verify that joining PC Controller rejects the multicast frame [009F.01.03.11.01C]");
            #endregion


            ReportService.LogText("4.- Disable Test Schema in, PC Controller (1) and send a S2 Multicast Message encapsulation Basic Get to DUT ");
            //('Force multicast' still enabled). Save the Group Id number to use it further in the TC.
            #region 4
            ApplicationService.SecurityTestSettings.ClearTestExtensionsS2(primary);
            zt = ZnifferService.ExpectData(10000);
            zt.SetUpWatchS2(0x01, dutNodeId, null, null, (byte)SecurityS2ExtensionTypes.MpanGrp, false, 1);
            zt.SetUpWatchS2(dutNodeId, 0x01, null, null, (byte)SecurityS2ExtensionTypes.Mos, false, 1);
            nodeIds = new Node[] { 0x02 };
            ComplianceService.SendDataMulti(primary, nodeIds, data2send, TX_OPTIONS);
            Delay(2000, "SendDataMulti");
            ztRes = zt.WaitCompletedSignal();
            AssertIsTrue(ztRes, "In 4. verify that resynchronization is required [009F.01.03.11.01C] (DUT sends Nonce Report with MOS active OR attaches MOS extension to S2MEC and PC Controller 1 sends S2MEC with MPAN extension).");
            byte groupId = ((COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION)ztRes.Frames.First(x => x.Source == 0x01).Payload).vg1[0].extension[0];
            ReportService.LogText("Groupd Id = " + groupId);
            #endregion


            ReportService.LogText("5.- PC Controller (1) sends a S2 Multicast Message encapsulation Basic Get to DUT .");
            #region 5
            //('Force multicast' enabled for the rest of the TC)
            zt = ZnifferService.ExpectData(10000);
            zt.SetUpWatch(dutNodeId, 0x01, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT(), false);
            zt.SetUpWatchS2(dutNodeId, 0x01, null, null, (byte)SecurityS2ExtensionTypes.Mos, false, 1);
            nodeIds = new Node[] { 0x02 };
            ComplianceService.SendDataMulti(primary, nodeIds, data2send, TX_OPTIONS);
            Delay(2000, "SendDataMulti");
            using (ZnifferService.Settings.IsExpectingFailure.Set())
            {
                ztRes = zt.WaitCompletedSignal();
            }
            AssertIsTrue(!ztRes, "In 5. verify that resynchronization is not required [009F.01.03.11.01E]");
            #endregion


            ReportService.LogText("6.- Press 'Clear' button to clear the extensions.");
            #region 6
            ApplicationService.SecurityTestSettings.ClearTestExtensionsS2(primary);
            //Enable Test Schema, mark "Cleanup existing extensions first". 
            ReportService.LogText("PC Controller (1) sends a S2 Multicast Message encapsulation Basic Get to DUT with an incorrect MGRP Extension Group ID:");
            ReportService.LogText("{Message type=SinglecastAll, Extension type=MGRP, Extension=a value different from the saved Byte, disabled Ecrypted, enabled Critical=1, enabled length=3, disabled More to follow, enabled Usage=1}.");
            tes = new TestExtensionS2Settings(SecurityS2MessageTypes.SinglecastAll, SecurityS2ExtensionTypes.MpanGrp, SecurityS2TestExtensionsActions.AddOrModify)
            {
                Value = new byte[1] { (byte)(groupId + 0x10) },
                IsEncrypted = false,
                IsCritical = true,
                ExtensionLength = 3,
                IsMoreToFollow = false,
                NumOfUsage = 1
            };
            ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(primary, tes);
            //Press "Add" button.");
            zt = ZnifferService.ExpectData(10000);
            zt.SetUpWatchS2(dutNodeId, 0x01, null, null, (byte)SecurityS2ExtensionTypes.Mos, false, 1);
            nodeIds = new Node[] { 0x02 };
            ComplianceService.SendDataMulti(primary, nodeIds, data2send, TX_OPTIONS);
            Delay(2000, "SendDataMulti");
            ztRes = zt.WaitCompletedSignal();
            AssertIsTrue(ztRes, "In 6. verify that DUT replies with a S2 Nonce Report with MOS flag active or, with a S2 Message Encapsulation with the MOS extension included [009F.01.03.11.02B]");
            #endregion


            ReportService.LogText("7.- Press 'Clear' button to clear the extensions.");
            #region 7
            ApplicationService.SecurityTestSettings.ClearTestExtensionsS2(primary);
            ReportService.LogText("PC Controller (1) sends a S2 Multicast Message encapsulation Basic Get to DUT,on the first singlecast follow-up frame PC Controller (1) adds MGRP and MPAN extensions:");
            ReportService.LogText("{Message type=SinglecastMpanGrp, Extension type=MGRP, Extension=the saved Byte, disabled Ecrypted, enabled Critical=1, enabled length=3, disabled More to follow, enabled Usage=1}.");
            tes = new TestExtensionS2Settings(SecurityS2MessageTypes.SinglecastWithMpanGrp, SecurityS2ExtensionTypes.MpanGrp, SecurityS2TestExtensionsActions.AddOrModify)
            {
                Value = new byte[1] { groupId },
                IsCritical = true,
                ExtensionLength = 3,
                NumOfUsage = 1
            };
            //Press "Add" button.
            ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(primary, tes);

            ReportService.LogText("{Message type=SinglecastMpanGrp, Extension type=MPAN, Extension=16 random Bytes (in Hex), disabled Ecrypted, enabled Critical=1, enabled length=10Hex(16 dec), disabled More to follow, enabled Usage=1}.");
            tes = new TestExtensionS2Settings(SecurityS2MessageTypes.SinglecastWithMpanGrp, SecurityS2ExtensionTypes.Mpan, SecurityS2TestExtensionsActions.AddOrModify)
            {
                Value = new byte[16],
                IsCritical = true,
                ExtensionLength = 16,
                NumOfUsage = 1
            };
            //Press "Add" button.
            ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(primary, tes);

            zt = ZnifferService.ExpectData(10000);
            zt.SetUpWatch(dutNodeId, 0x01, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), false);
            zt.SetUpWatchS2(dutNodeId, 0x01, null, null, (byte)SecurityS2ExtensionTypes.Mos, false, 1);
            nodeIds = new Node[] { 0x02 };
            ComplianceService.SendDataMulti(primary, nodeIds, data2send, TX_OPTIONS);
            Delay(2000, "SendDataMulti");
            using (ZnifferService.Settings.IsExpectingFailure.Set())
            {
                ztRes = zt.WaitCompletedSignal();
            }
            AssertIsTrue(!ztRes, "In 7. verify that joining DUT rejects the singlecast follow-up with both extensions [009F.01.03.11.020]");
            #endregion


            ReportService.LogText("8.- Press 'Clear' button to clear the extensions.");
            #region 8
            ApplicationService.SecurityTestSettings.ClearTestExtensionsS2(primary);
            //Enable Test Schema, mark "Cleanup existing extensions first". 
            ReportService.LogText("PC Controller (1) sends a S2 Multicast Message encapsulation Basic Get to DUT,on the first singlecast follow-up frame PC Controller (1) sends the MGRP extension encrypted:");
            ReportService.LogText("{Message type=SinglecastMpanGrp, Extension type=MGRP, Extension=the saved Byte, enabled Ecrypted=1, enabled Critical=1, enabled length=3, disabled More to follow, enabled Usage=1}.");
            tes = new TestExtensionS2Settings(SecurityS2MessageTypes.SinglecastWithMpanGrp, SecurityS2ExtensionTypes.MpanGrp, SecurityS2TestExtensionsActions.AddOrModify)
            {
                Value = new byte[1] { groupId },
                IsEncrypted = true,
                IsCritical = true,
                ExtensionLength = 3,
                NumOfUsage = 1
            };
            ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(primary, tes);
            //Press "Add" button.;
            zt = ZnifferService.ExpectData(10000);
            zt.SetUpWatch(dutNodeId, 0x01, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), false);
            zt.SetUpWatchS2(dutNodeId, 0x01, null, null, (byte)SecurityS2ExtensionTypes.Mos, false, 1);
            nodeIds = new Node[] { 0x02 };
            ComplianceService.SendDataMulti(primary, nodeIds, data2send, TX_OPTIONS);
            Delay(2000, "SendDataMulti");
            using (ZnifferService.Settings.IsExpectingFailure.Set())
            {
                ztRes = zt.WaitCompletedSignal();
            }
            AssertIsTrue(!ztRes, "In 8. verify that joining DUT rejects the singlecast follow-up [009F.01.03.11.021]");
            #endregion


            ReportService.LogText("9.- Press 'Clear' button to clear the extensions.");
            #region 9
            ApplicationService.SecurityTestSettings.ClearTestExtensionsS2(primary);
            //Enable Test Schema, mark "Cleanup existing extensions first". 
            ReportService.LogText("PC Controller (1) sends a S2 Multicast Message encapsulation Basic Get to DUT, on the first singlecast follow-up frame PC Controller (1) sets the MGRP Length field to 0xFF:");
            ReportService.LogText("{Message type=SinglecastMpanGrp, Extension type=MGRP, Extension=the saved Byte, disabled Ecrypted, enabled Critical=1, enabled length=FF (255 dec), disabled More to follow, enabled Usage=1}.");
            tes = new TestExtensionS2Settings(SecurityS2MessageTypes.SinglecastWithMpanGrp, SecurityS2ExtensionTypes.MpanGrp, SecurityS2TestExtensionsActions.AddOrModify)
            {
                Value = new byte[1] { groupId },
                IsCritical = true,
                ExtensionLength = 255,
                NumOfUsage = 1
            };
            ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(primary, tes);
            //Press "Add" button.;
            zt = ZnifferService.ExpectData(10000);
            zt.SetUpWatch(dutNodeId, 0x01, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), false);
            zt.SetUpWatchS2(dutNodeId, 0x01, null, null, (byte)SecurityS2ExtensionTypes.Mos, false, 1);
            nodeIds = new Node[] { 0x02 };
            ComplianceService.SendDataMulti(primary, nodeIds, data2send, TX_OPTIONS);
            Delay(2000, "SendDataMulti");
            using (ZnifferService.Settings.IsExpectingFailure.Set())
            {
                ztRes = zt.WaitCompletedSignal();
            }
            AssertIsTrue(!ztRes, "In 9. verify that joining DUT rejects the singlecast follow-up [009F.01.03.11.022]");
            #endregion


            ReportService.LogText("10.- Press 'Clear' button to clear the extensions.");
            #region 10
            ApplicationService.SecurityTestSettings.ClearTestExtensionsS2(primary);
            //Enable Test Schema, mark "Cleanup existing extensions first". 
            ReportService.LogText("PC Controller (1) sends a S2 Multicast Message encapsulation Basic Get to DUT, on the first singlecast follow-up frame PC Controller (1) sets the MGRP Type field to 0xFF:");
            ReportService.LogText("{Message type=SinglecastMpanGrp, Extension type=TEST (0xFF), Extension=the saved Byte, disabled Ecrypted, enabled Critical=1, enabled length=3, disabled More to follow, enabled Usage=1}.");
            tes = new TestExtensionS2Settings(SecurityS2MessageTypes.SinglecastWithMpanGrp, SecurityS2ExtensionTypes.Test, SecurityS2TestExtensionsActions.AddOrModify)
            {
                Value = new byte[1] { groupId },
                IsCritical = true,
                ExtensionLength = 3,
                NumOfUsage = 1
            };
            ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(primary, tes);
            //Press "Add" button.;
            zt = ZnifferService.ExpectData(10000);
            zt.SetUpWatch(dutNodeId, 0x01, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), false);
            zt.SetUpWatchS2(dutNodeId, 0x01, null, null, (byte)SecurityS2ExtensionTypes.Mos, false, 1);
            nodeIds = new Node[] { 0x02 };
            ComplianceService.SendDataMulti(primary, nodeIds, data2send, TX_OPTIONS);
            Delay(2000, "SendDataMulti");
            using (ZnifferService.Settings.IsExpectingFailure.Set())
            {
                ztRes = zt.WaitCompletedSignal();
            }
            AssertIsTrue(!ztRes, "In 10. verify that joining DUT rejects the singlecast follow-up [009F.01.03.11.023]");
            #endregion


            ReportService.LogText("11.- Press 'Clear' button to clear the extensions.");
            #region 11
            ApplicationService.SecurityTestSettings.ClearTestExtensionsS2(primary);
            //Enable Test Schema, mark "Cleanup existing extensions first". 
            ReportService.LogText("PC Controller (1) sends a S2 Multicast Message encapsulation Basic Get to DUT, on the first singlecast follow-up frame PC Controller (1) sets the MGRP Critical flag to 0:");
            ReportService.LogText("{Message type=SinglecastAll, Extension type=MGRP, Extension=the saved Byte, disabled Ecrypted, enabled Critical=0, enabled length=3, disabled more to follow, enabled Usage=1}.");
            tes = new TestExtensionS2Settings(SecurityS2MessageTypes.SinglecastAll, SecurityS2ExtensionTypes.MpanGrp, SecurityS2TestExtensionsActions.AddOrModify)
            {
                Value = new byte[1] { groupId },
                IsCritical = false,
                ExtensionLength = 3,
                NumOfUsage = 1
            };
            ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(primary, tes);
            //Press "Add" button.;
            zt = ZnifferService.ExpectData(10000);
            zt.SetUpWatch(dutNodeId, 0x01, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), false);
            zt.SetUpWatchS2(dutNodeId, 0x01, null, null, (byte)SecurityS2ExtensionTypes.Mos, false, 1);
            nodeIds = new Node[] { 0x02 };
            ComplianceService.SendDataMulti(primary, nodeIds, data2send, TX_OPTIONS);
            Delay(2000, "SendDataMulti");
            using (ZnifferService.Settings.IsExpectingFailure.Set())
            {
                ztRes = zt.WaitCompletedSignal();
            }
            AssertIsTrue(!ztRes, "In 11. verify that joining DUT resynchronizes due to a random group number. CC: [009F.01.03.11.024] applies only for when the extension is unknown and Critical =1.");
            #endregion
        }
    }
}
