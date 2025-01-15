// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("Security v2 PC Controller S2MEC - MPAN Extension")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    //[TestInterfaceDeviceMapping(3, "TI1", LibTypes.SwitchOnOff)]
    //[TestInterfaceDeviceMapping(4, "TI2", LibTypes.SwitchOnOff)]
    class TC02437_dev : ZatsCodeTestCase
    {
        const byte MPAN_EXT_LENGTH = 19;
        const byte MPAN = 0x02;

        private BasicResult.AddNode TestInterface_AddNode(Alias InclusionController, Alias TINode, int dsk)
        {
            BasicResult.AddNode ret = null;
            //Remove
            ReportService.LogText(" Exclude from any net");
            var RmvExpRes = AsyncService.RemoveNode(InclusionController);
            TestInterfaceService.SetLearnMode(TINode);
            var remRes = RmvExpRes.WaitCompletedSignal();
            //Add
            ReportService.LogText("Include node to Primary");
            var AddExpRes = AsyncService.AddNodeWithDSK(InclusionController, dsk);
            TestInterfaceService.SetLearnMode(TINode);
            var addTestInterfaceResult = AddExpRes.WaitCompletedSignal();
            ret = addTestInterfaceResult;
            return ret;
        }

        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Dut = new Alias(2);
            //Alias Slave1 = new Alias(3);
            //Alias Slave2 = new Alias(4);
            Alias[] ALL = new Alias[] { Primary, Dut };
            ParallelService.SetDefault(ALL);

            Service.ApplicationNodeInformation(Primary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER,
                new byte[]
                {
                    COMMAND_CLASS_VERSION.ID,
                    COMMAND_CLASS_CONTROLLER_REPLICATION.ID,
                    COMMAND_CLASS_SUPERVISION.ID,
                    COMMAND_CLASS_SECURITY.ID,
                    COMMAND_CLASS_SECURITY_2.ID,
                    COMMAND_CLASS_BASIC.ID
                });
            ApplicationService.AddSupervision(Primary);
            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            ApplicationService.AddTransportService(Primary, 100);

            Service.ApplicationNodeInformation(Dut, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER,
                new byte[]
                {
                    COMMAND_CLASS_VERSION.ID,
                    COMMAND_CLASS_CONTROLLER_REPLICATION.ID,
                    COMMAND_CLASS_SUPERVISION.ID,
                    COMMAND_CLASS_SECURITY.ID,
                    COMMAND_CLASS_SECURITY_2.ID,
                    COMMAND_CLASS_BASIC.ID
                });
            ApplicationService.AddSupervision(Dut);
            ApplicationService.AddSecurity(Dut, SecurityVersions.S2);
            ApplicationService.AddTransportService(Dut, 100);
            ApplicationService.SecurityTestSettings.NetworkKeyChanged(Primary, ZnifferService.SetSecurityNetworkKey);



            ReportService.LogText("Set SIS Primary");
            Service.SetSUCNodeID(Primary, Primary);

            ReportService.LogText("1.- Include DUT and the two Slaves to PC Controller (1) network");
            var addRes = Service.AddNode(Primary, Dut);
            byte DUTNodeID = addRes.NodeId;
            AssertIsTrue(addRes && SubstituteStatuses.Done == addRes.SubstituteStatus,
                "In 1. verify that inclusion is successful and that security 2 is enabled");

            //var addSwitchRes = TestInterface_AddNode(Primary, Slave1, 56990);
            //var slaveId1 = addSwitchRes.NodeId;
            //AssertIsTrue(addSwitchRes && addSwitchRes.SubstituteStatus == SubstituteStatuses.Done,
            //    "In 1. verify that inclusion is successful and that security 2 is enabled: " + slaveId1);

            //addSwitchRes = TestInterface_AddNode(Primary, Slave2, 56990);
            //var slaveId2 = addSwitchRes.NodeId;
            //AssertIsTrue(addSwitchRes && addSwitchRes.SubstituteStatus == SubstituteStatuses.Done,
            //    "In 1. verify that inclusion is successful and that security 2 is enabled: " + slaveId2);

            //ReportService.LogText("! - RequestNetworkUpdate & RequestNodeInfo");
            //Service.RequestNetworkUpdate(Dut);
            //Service.RequestNodeInfo(Dut, Primary);
            //ComplianceService.RequestNodeInfo(Dut, slaveId1);
            //Delay(1000);
            //ComplianceService.RequestNodeInfo(Dut, slaveId2);
            //Delay(1000);

            ReportService.LogText("!Sync SPAN");
            var data2send = new COMMAND_CLASS_BASIC.BASIC_SET();
            Service.SendData(Dut, Primary, data2send);
            Delay(2000, "SendData");

            
            ReportService.LogText("2.- DUT sends a multicast S2 message encapsulation to PC Controller (1) and to Slaves 1 and 2.");
            var zt = ZnifferService.ExpectData(10000);
            zt.SetUpWatchS2(DUTNodeID, 0x01, null, null, 0x02, true, 1);
            //zt.SetUpWatch(null, null, null, HeaderTypes.ALL_SINGLECAST, false);
            var nodeIds = new Node[] { 0x01, /*slaveId1, slaveId2*/ };
            ComplianceService.SendDataMulti(Dut, nodeIds, data2send, TX_OPTIONS);
            Delay(2000, "SendDataMulti");
            var ztRes = zt.WaitCompletedSignal();
            AssertIsTrue(ztRes, "In 2. verify that:- After PC Controller (1) has sent a S2 message encapsulation containing the MOS extension" +
                            "DUT responds with a S2 message encapsulation containing the MPAN extension ");
            var enEx = ztRes.Frames[0].Payload;
            AssertIsTrue(ztRes.Frames[0].Payload != null, "- The MPAN extension is sent encrypted [009F.01.03.11.014]");
            var ext = ztRes.Frames[0].Extensions[0];
            AssertIsTrue(ext.extensionLength == 0x13, "- The MPAN extension Length field is set to 19 [009F.01.03.11.015]");
            AssertIsTrue(ext.properties1.type == (byte)SecurityS2ExtensionTypes.Mpan, "- The MPAN extension Type field is set to 2 [009F.01.03.11.016]");
            AssertIsTrue(ext.properties1.critical == 0x01, "- The MPAN extension Critical field is set to 1 [009F.01.03.11.017]");
            AssertIsTrue(ext.properties1.moreToFollow== 0x00, "- The MPAN extension More to Follow field is set to 0 [009F.01.03.11.018]");
            AssertIsTrue(ext.extension[0] == 0x01, "- The MPAN extension Group ID field matches the MGRP extension Group ID field on the multicast message encapsulation frame [009F.01.03.11.019]");


            byte groupID = 0x00;
            for (int i = 0; i < 4; i++)
            {
                ReportService.LogText("3.- PC Controller (1) sends a multicast S2 message encapsulation to DUT and to Slaves 1 and 2.");
                ApplicationService.SecurityTestSettings.ClearTestExtensionsS2(Primary);
                zt = ZnifferService.ExpectData(20000);
                if (i == 0)
                {
                    ReportService.LogText("3.1- After DUT has sent a singlecast S2 message encapsulation containing the MOS extension, PC Controller (1) responds with a S2 message encapsulation containing the MPAN extension unencrypted");
                    var tes = new TestExtensionS2Settings(SecurityS2MessageTypes.SinglecastWithMpan, SecurityS2ExtensionTypes.Mpan, SecurityS2TestExtensionsActions.AddOrModify)
                        {
                            IsEncrypted = false,
                            NumOfUsage = 1
                        };
                    ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(Primary, tes);
                    zt.SetUpWatch(0x01, DUTNodeID, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), null, null, false, 1, false);
                }
                else if (i == 1)
                {
                    ReportService.LogText("5.- Repeat point 3., 3.1, and 3.2 but instead of sending MPAN extension unencrypted, modify its Length field to 0xFF");
                    var tes = new TestExtensionS2Settings(SecurityS2MessageTypes.SinglecastWithMpan, SecurityS2ExtensionTypes.Mpan, SecurityS2TestExtensionsActions.AddOrModify)
                    {
                        ExtensionLength = 0xFF,
                        NumOfUsage = 1
                    };
                    ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(Primary, tes);
                    tes = new TestExtensionS2Settings(SecurityS2MessageTypes.SinglecastWithMpan, SecurityS2ExtensionTypes.Mpan, SecurityS2TestExtensionsActions.Delete);
                    ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(Primary, tes);
                    
                    zt.SetUpWatchS2(0x01, DUTNodeID, null, new byte[] { 0xFF, 0x42 }, 0x00, true, 1);

                }
                else if (i == 2)
                {
                    ReportService.LogText("7.- Repeat point 3., 3.1, and 3.2 but instead of sending MPAN extension unencrypted, modify its Type field to 0xFF");
                    var tes = new TestExtensionS2Settings(SecurityS2MessageTypes.SinglecastWithMpan, SecurityS2ExtensionTypes.Test, SecurityS2TestExtensionsActions.AddOrModify)
                        {
                            NumOfUsage = 1
                        };
                    ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(Primary, tes);
                    zt.SetUpWatchS2(0x01, DUTNodeID, null, null, 0x00, true, 1);
                }
                else if (i == 3)
                {
                    ReportService.LogText("9.- Repeat point 3., 3.1, and 3.2 but instead of sending MPAN extension unencrypted, modify its Critical field to 0");
                    var tes = new TestExtensionS2Settings(SecurityS2MessageTypes.SinglecastWithMpan, SecurityS2ExtensionTypes.Mpan, SecurityS2TestExtensionsActions.AddOrModify)
                    {
                        IsCritical = false,
                        NumOfUsage = 1
                    };
                    ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(Primary, tes);
                    zt.SetUpWatchS2(0x01, DUTNodeID, null, null, (byte)SecurityS2ExtensionTypes.Mpan, true, 1);
                }
                ApplicationService.SecurityTestSettings.ClearMPANTable(Dut);// to add MOS extension and resync MPAN 
                nodeIds = new Node[] { DUTNodeID, /*slaveId1, slaveId2*/ };
                ComplianceService.SendDataMulti(Primary, nodeIds, data2send, TX_OPTIONS);
                Delay(2000, "SendDataMulti");

                ztRes = zt.WaitCompletedSignal();
                AssertIsTrue(ztRes, "Primary respond with MPAN extension");
                if (i == 0)
                {
                    groupID = ((COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION)ztRes.Frames[0].Payload).vg1[0].extension[0];
                }
                byte[] newMpanState = ApplicationService.SecurityTestSettings.GetMPANState(Primary, Primary, groupID);
                byte[] currMpanState = ApplicationService.SecurityTestSettings.GetMPANState(Dut, Primary, groupID);
                AssertIsTrue(newMpanState != null && currMpanState != null && !currMpanState.SequenceEqual(newMpanState),
                    "In 3.1 verify that DUT ignored the MPAN extension [009F.01.03.11.014]");

                #region 3.2
                ReportService.LogText("3.2 - PC Controller (1) sends a multicast S2 message encapsulation to DUT and to Slaves 1 and 2.");
                ApplicationService.SecurityTestSettings.ClearTestExtensionsS2(Primary);
                zt = ZnifferService.ExpectData(10000);
                zt.SetUpWatch(DUTNodeID, 0x01, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT(), null, null, false, 1, false);
                zt.SetUpWatchS2(DUTNodeID, 0x01, null, new COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT(), 0x04, true, 1);
                nodeIds = new Node[] { DUTNodeID, /*slaveId1, slaveId2*/ };
                ComplianceService.SendDataMulti(Primary, nodeIds, data2send, TX_OPTIONS);
                Delay(2000, "SendDataMulti");
                using (ZnifferService.Settings.IsExpectingFailure.Set())
                {
                    ztRes = zt.WaitCompletedSignal();
                }
                AssertIsTrue(ztRes.Frames.Count > 0, "In 3.2 verify that MPAN resynchronization is required between PC Controller (1) and DUT[009F.01.03.11.014]");
                #endregion


                #region 4. | 6. | 8.
                ReportService.LogText("4./6./8.- PC Controller (1) sends a singlecast S2 Message Encapsulation to DUT adding MGRP extension with a Group ID field set to 0xFF");
                var tes1 = new TestExtensionS2Settings(SecurityS2MessageTypes.SinglecastAll, SecurityS2ExtensionTypes.MpanGrp, SecurityS2TestExtensionsActions.AddOrModify)
                {
                    Value = new byte[] { 0xFF }
                };
                ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(Primary, tes1);
                Service.SendData(Primary, Dut, data2send);
                Delay(2000, "SendData");
                ApplicationService.SecurityTestSettings.ClearTestExtensionsS2(Primary);
                #endregion
            }

        }
    }
}