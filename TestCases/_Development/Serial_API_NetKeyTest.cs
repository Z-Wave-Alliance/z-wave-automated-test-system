// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests.Security_v2
{
    [ZatsTestCase("Serial API test - Authenticating and decripting all network keys")]
    [DeviceMapping(1, "StaticController1", LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, "Slave1", LibTypes.EndDeviceLib)] //Change this to indicate the comport of the sdk 6.70

    class Serial_API_NetKeyTest : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave = new Alias(2);

            Alias[] ALL = new Alias[] { Primary, Slave };

            ParallelService.Reset(ALL);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            ApplicationService.AddBasic(Primary);
            byte[] KEXframe = new COMMAND_CLASS_SECURITY_2.KEX_SET() { };

            for (int ind = 0; ind < 3; ind++)
            {
                switch (ind)
                {
                    case 0:
                        KEXframe = new COMMAND_CLASS_SECURITY_2.KEX_SET()
                        {
                            grantedKeys = 0x1,
                            selectedKexScheme = 0x2,
                            selectedEcdhProfile = 0x1
                        };
                        ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary, SecurityS2TestFrames.KEXSet, KEXframe);
                        ReportService.LogTitle(" *** Unathenticated Network Key ***");
                        break;
                    case 1:
                        KEXframe = new COMMAND_CLASS_SECURITY_2.KEX_SET()
                        {
                            grantedKeys = 0x2,
                            selectedKexScheme = 0x2,
                            selectedEcdhProfile = 0x1
                        };
                        ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary, SecurityS2TestFrames.KEXSet, KEXframe);
                        ReportService.LogTitle(" *** Authenticated Network Key ***");
                        break;
                    case 2:
                        KEXframe = new COMMAND_CLASS_SECURITY_2.KEX_SET()
                        {
                            grantedKeys = 0x4,
                            selectedKexScheme = 0x2,
                            selectedEcdhProfile = 0x1
                        };
                        ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary, SecurityS2TestFrames.KEXSet, KEXframe);
                        ReportService.LogTitle(" *** Access Network Key ***");
                        break;
                }

                #region 1.- Add Serial API to primary PC Controller.

                ReportService.LogTitle("1.- Add secondary PC Controller to primary PC Controller.");
                ReportService.LogText("Starting Inclussion");

                var addRes = Service.AddNodeWithDSK(Primary, Slave, 56990);

                Assert.IsTrue(addRes);
                Assert.IsTrue(addRes.SetLearnMode);
                Assert.AreEqual(SubstituteStatuses.Done, addRes.SubstituteStatus);
                //Assert.AreEqual(SubstituteStatuses.Done, addRes.SetLearnMode.SubstituteStatus);

                ReportService.LogOk("Inclusion Successful");

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

                #region 2.- Transmit to Z-Wave Sample app

                for (int ind_2 = 0; ind_2 < 3; ind_2++)
                {
                    switch (ind_2)
                    {
                        case 0:
                            ApplicationService.SecurityTestSettings.SetActiveSecurityScheme(Primary, Slave, SecuritySchemes.S2_UNAUTHENTICATED);
                            ApplicationService.AddBasicEx(Slave, SecuritySchemes.S2_UNAUTHENTICATED);
                            ReportService.LogText("Testing Unathenticated Network Key");
                            ZnifferService.SetSecurityNetworkKeyS2(NetKeyS2cls0);
                            break;
                        case 1:
                            ApplicationService.SecurityTestSettings.SetActiveSecurityScheme(Primary, Slave, SecuritySchemes.S2_AUTHENTICATED);
                            ApplicationService.AddBasicEx(Slave, SecuritySchemes.S2_AUTHENTICATED);
                            ReportService.LogText("Testing Authenticated Network Key");
                            ZnifferService.SetSecurityNetworkKeyS2(NetKeyS2cls1);
                            break;
                        case 2:
                            ApplicationService.SecurityTestSettings.SetActiveSecurityScheme(Primary, Slave, SecuritySchemes.S2_ACCESS);
                            ApplicationService.AddBasicEx(Slave, SecuritySchemes.S2_ACCESS);
                            ReportService.LogText("Testing Access Network Key");
                            ZnifferService.SetSecurityNetworkKeyS2(NetKeyS2cls2);
                            break;
                    }

                    ReportService.LogText("Sending Basic Get");
                    Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_GET());
                    var ReqRes = AsyncService.ExpectData(Slave, new COMMAND_CLASS_BASIC.BASIC_GET(), 4000); ;

                    var isAuthenticated = ReqRes.WaitCompletedSignal();

                    if (isAuthenticated && ind == ind_2)
                    {
                        ReportService.LogOk("Basic Report Received");
                    }
                    if (isAuthenticated && ind != ind_2)
                    {
                        ReportService.LogWarning("Basic Report Received using a wrong network key");
                    }
                    if (!isAuthenticated && ind == ind_2)
                    {
                        ReportService.LogWarning("Basic Report not Received Seding with the correct network key");
                    }
                    if (!isAuthenticated && ind != ind_2)
                    {
                        ReportService.LogOk("Basic Report not Received Seding with the wrong network key");
                    }
                    Delay(2000);
                }
                #endregion

                #region 3.- Removing Serial API Slave from Controller

                Delay(2000);
                Service.RemoveNode(Primary, Slave);

                #endregion
            }
        }
    }
}
