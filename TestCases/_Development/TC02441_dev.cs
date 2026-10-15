// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
﻿using System;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Security v2 PC Controller S2MEC - MOS Extension")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    [DeviceMapping(4, LibTypes.EndDeviceLib)]
    class TC02441_dev : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias primary = new Alias(1);
            Alias dut = new Alias(2);
            Alias slave1 = new Alias(3);
            Alias slave2 = new Alias(4);

            Alias[] All = new Alias[] { primary, dut, slave1, slave2 };

            ParallelService.SetDefault(All);

            Service.ApplicationNodeInformation(primary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER,
                new byte[]
                {
                    COMMAND_CLASS_ZWAVEPLUS_INFO.ID,
                    COMMAND_CLASS_VERSION.ID,
                    COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID,
                    COMMAND_CLASS_POWERLEVEL.ID,
                    COMMAND_CLASS_APPLICATION_STATUS.ID,
                    COMMAND_CLASS_ASSOCIATION.ID,
                    COMMAND_CLASS_ASSOCIATION_GRP_INFO.ID,
                    COMMAND_CLASS_CONFIGURATION.ID,
                    COMMAND_CLASS_CRC_16_ENCAP.ID,
                    COMMAND_CLASS_DEVICE_RESET_LOCALLY.ID,
                    COMMAND_CLASS_SECURITY_2.ID,
                    COMMAND_CLASS_SUPERVISION.ID,
                    COMMAND_CLASS_TRANSPORT_SERVICE.ID,
                    COMMAND_CLASS_INCLUSION_CONTROLLER.ID
                });

            Service.ApplicationNodeInformation(dut, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER,
                new byte[]
                {
                    COMMAND_CLASS_ZWAVEPLUS_INFO.ID,
                    COMMAND_CLASS_VERSION.ID,
                    COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID,
                    COMMAND_CLASS_POWERLEVEL.ID,
                    COMMAND_CLASS_APPLICATION_STATUS.ID,
                    COMMAND_CLASS_ASSOCIATION.ID,
                    COMMAND_CLASS_ASSOCIATION_GRP_INFO.ID,
                    COMMAND_CLASS_CONFIGURATION.ID,
                    COMMAND_CLASS_CRC_16_ENCAP.ID,
                    COMMAND_CLASS_DEVICE_RESET_LOCALLY.ID,
                    COMMAND_CLASS_SECURITY_2.ID,
                    COMMAND_CLASS_SUPERVISION.ID,
                    COMMAND_CLASS_TRANSPORT_SERVICE.ID,
                    COMMAND_CLASS_INCLUSION_CONTROLLER.ID
                });

            Service.ApplicationNodeInformation(slave1, true, (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                new byte[] { COMMAND_CLASS_SECURITY_2.ID });
            Service.ApplicationNodeInformation(slave2, true, (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                new byte[] { COMMAND_CLASS_SECURITY_2.ID });

            ApplicationService.AddSupervision(primary);
            ApplicationService.AddSupervision(dut);

            ApplicationService.AddSecurity(primary, SecurityVersions.S2);
            ApplicationService.AddSecurity(dut, SecurityVersions.S2);

            ApplicationService.SecurityTestSettings.ClearTestExtensionsS2(primary);
            ApplicationService.SecurityTestSettings.ClearTestExtensionsS2(dut);

            ApplicationService.AddResponse(primary, new COMMAND_CLASS_BASIC.BASIC_REPORT(), new COMMAND_CLASS_BASIC.BASIC_SET());
            ApplicationService.AddResponse(dut, new COMMAND_CLASS_BASIC.BASIC_REPORT(), new COMMAND_CLASS_BASIC.BASIC_SET());

            ApplicationService.SecurityTestSettings.NetworkKeyChanged(primary, ZnifferService.SetSecurityNetworkKey);
            Service.SetSUCNodeID(primary, primary);

            #region 1.
            ReportService.LogText("1. Inclusion is successful and that security 2 is enabled");
            var addRes = Service.IncludeNode(primary, dut);
            AssertIsTrue(addRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done, "Adding DUT inclusion is successful and that security 2 is enabled");
            var slave1DSK = Service.GetDsk(slave1, 2);
            var addSlaveRes = Service.AddNode(primary, slave1);
            AssertIsTrue(addRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done, "Adding Slave1 inclusion is successful and that security 2 is enabled");
            var slave2DSK = Service.GetDsk(slave2, 2);
            addSlaveRes = Service.AddNode(primary, slave2);
            AssertIsTrue(addRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done, "Adding Slave2 inclusion is successful and that security 2 is enabled");
            #endregion

            #region 2.

            var zt = ZnifferService.ExpectData(15000);
            zt.SetUpWatchS2(0x02, 0x01, null, null, (byte)SecurityS2ExtensionTypes.Mos, false, 1);
            ComplianceService.SendDataMulti(primary, new Node[] { 0x02, 0x03, 0x04 }, new COMMAND_CLASS_BASIC.BASIC_SET(), TX_OPTIONS);
            var ztRes = zt.WaitCompletedSignal();
            AssertIsTrue(ztRes, "completed");
            var Response = (COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION)(ztRes.Frames[0].Payload);
            ReportService.LogText("2. Joining DUT sends the MOS extension");
            AssertIsTrue(Response.properties1.encryptedExtension == 0x00, "MOS extension sent unencrypted [009F.01.03.11.026]");
            AssertIsTrue(Response.vg1[0].extensionLength == 0x02, "MOS extension Length field is set to 2 [009F.01.03.11.027]");
            AssertIsTrue(Response.vg1[0].properties1.type == 0x04, "MOS extension type is set 4 [009F.01.03.11.028]");
            AssertIsTrue(Response.vg1[0].properties1.moreToFollow == 0x00, "MOS extension MoreToFollow is set 0 [009F.01.03.11.02A]");
            AssertIsTrue(Response.vg1[0].properties1.critical == 0x00, "MOS extension critical is set 0 [009F.01.03.11.02C]");
            #endregion

            #region 3.
            ApplicationService.SecurityTestSettings.ClearMPANTable(primary);
            #endregion

            #region 4.
            Random random = new Random();
            byte[] Value = new byte[16];
            var testExtensionSettings = new TestExtensionS2Settings(SecurityS2MessageTypes.SinglecastWithMos,
                                                               SecurityS2ExtensionTypes.Mos,
                                                               SecurityS2TestExtensionsActions.Add);
            random.NextBytes(Value);
            testExtensionSettings.Value = Value;
            testExtensionSettings.IsEncrypted = true;
            testExtensionSettings.IsCritical = false;
            testExtensionSettings.ExtensionLength = 0x2;
            testExtensionSettings.IsMoreToFollow = false;
            testExtensionSettings.NumOfUsage = 1;

            ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(primary, testExtensionSettings);

            zt = ZnifferService.ExpectData(10000);
            zt.SetUpWatchS2(0x02, 0x01, null, null, (byte)SecurityS2ExtensionTypes.Mpan, false, 1);
            ComplianceService.SendDataMulti(dut, new Node[] { 0x01, 0x03, 0x04 }, new COMMAND_CLASS_BASIC.BASIC_SET(), TX_OPTIONS);
            using (ZnifferService.Settings.IsExpectingFailure.Set())
            {
                ztRes = zt.WaitCompletedSignal();
            }
            AssertIsTrue(!ztRes, "4. DUT rejects the singlecast follow-up replied issued by including PC Controller (1) [009F.01.03.11.026]");
            #endregion

            #region 5.
            ApplicationService.SecurityTestSettings.RemoveMPANRecord(primary, 0x01, 0x02);
            #endregion

            #region 6.
            ApplicationService.SecurityTestSettings.ClearTestExtensionsS2(primary);

            testExtensionSettings = new TestExtensionS2Settings(SecurityS2MessageTypes.SinglecastWithMos,
                                                               SecurityS2ExtensionTypes.Mos,
                                                               SecurityS2TestExtensionsActions.Add);
            random.NextBytes(Value);
            testExtensionSettings.Value = Value;
            testExtensionSettings.IsEncrypted = false;
            testExtensionSettings.IsCritical = false;
            testExtensionSettings.ExtensionLength = 0xFF;
            testExtensionSettings.IsMoreToFollow = false;
            testExtensionSettings.NumOfUsage = 1;
            ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(primary, testExtensionSettings);

            zt = ZnifferService.ExpectData(10000);
            zt.SetUpWatchS2(0x02, 0x01, null, null, (byte)SecurityS2ExtensionTypes.Mpan, true, 1);
            ComplianceService.SendDataMulti(dut, new Node[] { 0x01, 0x03, 0x04 }, new COMMAND_CLASS_BASIC.BASIC_SET(), TX_OPTIONS);
            using (ZnifferService.Settings.IsExpectingFailure.Set())
            {
                ztRes = zt.WaitCompletedSignal();
            }
            AssertIsTrue(!ztRes, "6. DUT rejects the singlecast follow-up replied issued by including PC Controller (1) [009F.01.03.11.027]");
            #endregion

            #region 7.
            ApplicationService.SecurityTestSettings.RemoveMPANRecord(primary, 0x01, 0x02);
            #endregion

            #region 8.
            ApplicationService.SecurityTestSettings.ClearTestExtensionsS2(primary);
            testExtensionSettings = new TestExtensionS2Settings(SecurityS2MessageTypes.SinglecastWithMos,
                                                               SecurityS2ExtensionTypes.Test,
                                                               SecurityS2TestExtensionsActions.Add);
            random.NextBytes(Value);
            testExtensionSettings.Value = Value;
            testExtensionSettings.IsEncrypted = false;
            testExtensionSettings.IsCritical = false;
            testExtensionSettings.ExtensionLength = 0x2;
            testExtensionSettings.IsMoreToFollow = false;
            testExtensionSettings.NumOfUsage = 1;
            ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(primary, testExtensionSettings);

            zt = ZnifferService.ExpectData(10000);
            zt.SetUpWatchS2(0x02, 0x01, null, null, (byte)SecurityS2ExtensionTypes.Mpan, true, 1);
            ComplianceService.SendDataMulti(dut, new Node[] { 0x01, 0x03, 0x04 }, new COMMAND_CLASS_BASIC.BASIC_SET(), TX_OPTIONS);
            using (ZnifferService.Settings.IsExpectingFailure.Set())
            {
                ztRes = zt.WaitCompletedSignal();
            }
            AssertIsTrue(!ztRes, "8. DUT rejects the singlecast follow-up replied issued by including PC Controller (1) [009F.01.03.11.028]");
            #endregion
        }
    }
}
