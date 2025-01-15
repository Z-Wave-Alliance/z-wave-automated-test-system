// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
﻿using Zats.Services;

namespace ZatsTests.Security_v2
{
    [ZatsTestCase("Security v2 SerialAPI Slave - Inclusion - Invalid Security Scheme and Supported Curves ")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, "Slave1", LibTypes.EndDeviceLib)]//Modify with your serial API sdk 6.

    class Serial_API_KEXFailTest : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave = new Alias(2);

            Alias[] ALL = new Alias[] { Primary, Slave };

            ParallelService.Reset(ALL);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            ApplicationService.AddBasic(Primary);

            byte[] FakeKEXSetKey = new COMMAND_CLASS_SECURITY_2.KEX_SET()
            {
                grantedKeys = 0x00,
                selectedEcdhProfile = 0x01,
                selectedKexScheme = 0x02
            };

            byte[] FakeKEXSetEDCH = new COMMAND_CLASS_SECURITY_2.KEX_SET()
            {
                grantedKeys = 0x01,
                selectedEcdhProfile = 0,
                selectedKexScheme = 0x02
            };

            byte[] FakeKEXSetSchemes = new COMMAND_CLASS_SECURITY_2.KEX_SET()
            {
                grantedKeys = 0x01,
                selectedEcdhProfile = 0x01,
                selectedKexScheme = 0
            };

            byte[][] FakeKEX = { FakeKEXSetKey, FakeKEXSetSchemes, FakeKEXSetEDCH };

            for (int ind = 0; ind < 3; ind++)
            {
                ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary, SecurityS2TestFrames.KEXSet, FakeKEX[ind]);

                #region 1.- Add Serial API to primary PC Controller.

                ReportService.LogTitle("1.- Add Serial API slave to primary PC Controller.");
                ReportService.LogText("Starting Inclussion");

                var zt = ZnifferService.ExpectData(20000);
                zt.SetUpWatch(new COMMAND_CLASS_SECURITY_2.KEX_FAIL(){});

                var addRes = Service.AddNodeWithDSK(Primary, Slave, 56990);

                Assert.IsTrue(addRes);
                Assert.IsTrue(addRes.SetLearnMode);
                Assert.AreEqual(SubstituteStatuses.Failed, addRes.SubstituteStatus);

                ReportService.LogOk("Secured Inclusion Failed");

                var KexFail = zt.WaitCompletedSignal();

                if (KexFail.Frames[0].Destination != 0x01)
                {
                    ReportService.LogWarning("Destination differs from controller node ID");
                }
                else
                {
                    ReportService.LogOk("Destination matches controller node ID");
                }

                if (KexFail.Frames[0].Payload[2] != ind + 1 )
                {
                    ReportService.LogWarning("Wrong KEX Fail type");
                }
                else
                {
                    ReportService.LogOk("Correct KEX Fail type");
                }

                byte[] NetKeyS0 = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S0);
                byte[] SharedNetKey = ApplicationService.SecurityTestSettings.GetNetworkKeyS2Temp(Primary);
                byte[] NetKeyS2cls0 = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_UNAUTHENTICATED);
                byte[] NetKeyS2cls1 = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_AUTHENTICATED);
                byte[] NetKeyS2cls2 = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_ACCESS);

                ReportService.LogText("The Network Keys are:");
                ReportService.LogText("S0 Network Key: " + NetKeyS0.ToHexString());
                ReportService.LogText("Shared Network Key: " + SharedNetKey.ToHexString());
                ReportService.LogText("S2 Class 0 Network Key: " + NetKeyS2cls0.ToHexString());
                ReportService.LogText("S2 Class 1 Network Key: " + NetKeyS2cls1.ToHexString());
                ReportService.LogText("S2 Class 2 Network Key: " + NetKeyS2cls2.ToHexString());

                #endregion

                #region 2.- Reset Devices and Clear Delays

                ReportService.LogTitle("2.- Reset Devices and Clear Delays");
                ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Primary);
                ParallelService.Reset(ALL);

                #endregion
            }
        }
    }
}
