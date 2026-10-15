// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Security 2 Inclusion Flow check with custom, but correct, frames ")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    public class appbasic_InclusionSecureS2FlowCheck : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);
            Alias[] ALL = new Alias[] { Primary, Secondary };

            ParallelService.SetDefault(ALL);

            Service.ApplicationNodeInformation(Secondary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_SECURITY_2.ID, COMMAND_CLASS_SECURITY.ID });

            ApplicationService.AddSecurity(Primary, SecurityVersions.S2 | SecurityVersions.S0);
            ApplicationService.AddSecurity(Secondary, SecurityVersions.S2 | SecurityVersions.S0);
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Primary);
            ApplicationService.SecurityTestSettings.IncludeSettings.JoinDSK = Service.GetDsk(Secondary, 2);

            #region KEX_GET
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary,
                        SecurityS2TestFrames.KEXGet, new COMMAND_CLASS_SECURITY_2.KEX_GET());
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Primary,
                SecurityS2TestFrames.KEXGet, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Primary,
                SecurityS2TestFrames.KEXGet, false);
            #endregion

            #region KEX_SET
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary,
                        SecurityS2TestFrames.KEXSet, new COMMAND_CLASS_SECURITY_2.KEX_SET
                        {
                            grantedKeys = 0x87,
                            selectedEcdhProfile = 0x01,
                            selectedKexScheme = 0x02
                        });
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Primary,
                SecurityS2TestFrames.KEXSet, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Primary,
                SecurityS2TestFrames.KEXSet, false);
            #endregion

            #region PUBLIC_KEY_REPORT
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Primary,
                SecurityS2TestFrames.PublicKeyReportA, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Primary,
                SecurityS2TestFrames.PublicKeyReportA, false);
            #endregion

            #region KEXReportEcho
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary,
                        SecurityS2TestFrames.KEXReportEcho, new COMMAND_CLASS_SECURITY_2.KEX_REPORT
                        {
                            properties1 = new COMMAND_CLASS_SECURITY_2.KEX_REPORT.Tproperties1
                            {
                                echo = 0x01
                            },
                            supportedEcdhProfiles = 0x01,
                            supportedKexSchemes = 0x02,
                            requestedKeys = 0x87
                        });
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Primary,
                SecurityS2TestFrames.KEXReportEcho, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Primary,
                SecurityS2TestFrames.KEXReportEcho, true);
            ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2Temp(Primary,
                SecurityS2TestFrames.KEXReportEcho);
            #endregion

            #region NetworkKeyReport_S2Unauthenticated
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary,
                        SecurityS2TestFrames.NetworkKeyReport_S2Unauthenticated, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT
                        {
                            grantedKey = (byte)NetworkKeyFlags.Unathenticated,
                            networkKey = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_UNAUTHENTICATED)
                        });
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Primary,
                SecurityS2TestFrames.NetworkKeyReport_S2Unauthenticated, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Primary,
                SecurityS2TestFrames.NetworkKeyReport_S2Unauthenticated, true);
            ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2Temp(Primary,
                SecurityS2TestFrames.NetworkKeyReport_S2Unauthenticated);
            #endregion

            #region NetworkKeyReport_S2Authenticated
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary,
                        SecurityS2TestFrames.NetworkKeyReport_S2Authenticated, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT
                        {
                            grantedKey = (byte)NetworkKeyFlags.Authenticated,
                            networkKey = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_AUTHENTICATED)
                        });
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Primary,
                SecurityS2TestFrames.NetworkKeyReport_S2Authenticated, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Primary,
                SecurityS2TestFrames.NetworkKeyReport_S2Authenticated, true);
            ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2Temp(Primary,
                SecurityS2TestFrames.NetworkKeyReport_S2Authenticated);
            #endregion

            #region NetworkKeyReport_S2Access
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary,
                        SecurityS2TestFrames.NetworkKeyReport_S2Access, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT
                        {
                            grantedKey = (byte)NetworkKeyFlags.Access,
                            networkKey = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_ACCESS)
                        });
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Primary,
                SecurityS2TestFrames.NetworkKeyReport_S2Access, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Primary,
                SecurityS2TestFrames.NetworkKeyReport_S2Access, true);
            ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2Temp(Primary,
                SecurityS2TestFrames.NetworkKeyReport_S2Access);
            #endregion

            #region NetworkKeyReport_S0
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary,
                        SecurityS2TestFrames.NetworkKeyReport_S0, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT
                        {
                            grantedKey = (byte)NetworkKeyFlags.S0,
                            networkKey = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S0)
                        });
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Primary,
                SecurityS2TestFrames.NetworkKeyReport_S0, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Primary,
                SecurityS2TestFrames.NetworkKeyReport_S0, true);
            ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2Temp(Primary,
                SecurityS2TestFrames.NetworkKeyReport_S0);
            #endregion

            #region TransferEndA_S2Unauthenticated
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary,
                        SecurityS2TestFrames.TransferEndA_S2Unauthenticated, new COMMAND_CLASS_SECURITY_2.SECURITY_2_TRANSFER_END
                        {
                            properties1 = new COMMAND_CLASS_SECURITY_2.SECURITY_2_TRANSFER_END.Tproperties1
                            {
                                keyVerified = 0x01
                            }
                        });
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Primary,
                SecurityS2TestFrames.TransferEndA_S2Unauthenticated, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Primary,
                SecurityS2TestFrames.TransferEndA_S2Unauthenticated, true);
            ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2Temp(Primary,
                SecurityS2TestFrames.TransferEndA_S2Unauthenticated);
            #endregion

            #region TransferEndA_S2Authenticated
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary,
                        SecurityS2TestFrames.TransferEndA_S2Authenticated, new COMMAND_CLASS_SECURITY_2.SECURITY_2_TRANSFER_END
                        {
                            properties1 = new COMMAND_CLASS_SECURITY_2.SECURITY_2_TRANSFER_END.Tproperties1
                            {
                                keyVerified = 0x01
                            }
                        });
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Primary,
                SecurityS2TestFrames.TransferEndA_S2Authenticated, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Primary,
                SecurityS2TestFrames.TransferEndA_S2Authenticated, true);
            ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2Temp(Primary,
                SecurityS2TestFrames.TransferEndA_S2Authenticated);
            #endregion

            #region TransferEndA_S2Access
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary,
                        SecurityS2TestFrames.TransferEndA_S2Access, new COMMAND_CLASS_SECURITY_2.SECURITY_2_TRANSFER_END
                        {
                            properties1 = new COMMAND_CLASS_SECURITY_2.SECURITY_2_TRANSFER_END.Tproperties1
                            {
                                keyVerified = 0x01
                            }
                        });
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Primary,
                SecurityS2TestFrames.TransferEndA_S2Access, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Primary,
                SecurityS2TestFrames.TransferEndA_S2Access, true);
            ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2Temp(Primary,
                SecurityS2TestFrames.TransferEndA_S2Access);
            #endregion

            #region TransferEndA_S0
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary,
                        SecurityS2TestFrames.TransferEndA_S0, new COMMAND_CLASS_SECURITY_2.SECURITY_2_TRANSFER_END
                        {
                            properties1 = new COMMAND_CLASS_SECURITY_2.SECURITY_2_TRANSFER_END.Tproperties1
                            {
                                keyVerified = 0x01
                            }
                        });
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Primary,
                SecurityS2TestFrames.TransferEndA_S0, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Primary,
                SecurityS2TestFrames.TransferEndA_S0, true);
            ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2Temp(Primary,
                SecurityS2TestFrames.TransferEndA_S0);
            #endregion

            #region KEXReport
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Secondary,
                        SecurityS2TestFrames.KEXReport, new COMMAND_CLASS_SECURITY_2.KEX_REPORT
                        {
                            supportedEcdhProfiles = 0x01,
                            supportedKexSchemes = 0x02,
                            requestedKeys = 0x87
                        });
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Secondary,
                SecurityS2TestFrames.KEXReport, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Secondary,
                SecurityS2TestFrames.KEXReport, false);
            #endregion

            #region PublicKeyReportB
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Secondary,
                SecurityS2TestFrames.PublicKeyReportB, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Secondary,
                SecurityS2TestFrames.PublicKeyReportB, false);
            #endregion

            #region KEXSetEcho
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Secondary,
                        SecurityS2TestFrames.KEXSetEcho, new COMMAND_CLASS_SECURITY_2.KEX_SET
                        {
                            properties1 = new COMMAND_CLASS_SECURITY_2.KEX_SET.Tproperties1
                            {
                                echo = 0x01
                            },
                            selectedKexScheme = 0x02,
                            grantedKeys = 0x87,
                            selectedEcdhProfile = 0x01
                        });
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Secondary,
                SecurityS2TestFrames.KEXSetEcho, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Secondary,
                SecurityS2TestFrames.KEXSetEcho, true);
            ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2Temp(Secondary,
                SecurityS2TestFrames.KEXSetEcho);
            #endregion

            #region NetworkKeyGet_S2Unauthenticated
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Secondary,
                        SecurityS2TestFrames.NetworkKeyGet_S2Unauthenticated, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET
                        {
                            requestedKey = 1//(byte)SecuritySchemes.S2_UNAUTHENTICATED

                        });
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Secondary,
                SecurityS2TestFrames.NetworkKeyGet_S2Unauthenticated, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Secondary,
                SecurityS2TestFrames.NetworkKeyGet_S2Unauthenticated, true);
            ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2Temp(Secondary,
                SecurityS2TestFrames.NetworkKeyGet_S2Unauthenticated);
            #endregion

            #region NetworkKeyGet_S2Authenticated
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Secondary,
                        SecurityS2TestFrames.NetworkKeyGet_S2Authenticated, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET
                        {
                            requestedKey = 2//(byte)SecuritySchemes.S2_AUTHENTICATED
                        });
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Secondary,
                SecurityS2TestFrames.NetworkKeyGet_S2Authenticated, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Secondary,
                SecurityS2TestFrames.NetworkKeyGet_S2Authenticated, true);
            ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2Temp(Secondary,
                SecurityS2TestFrames.NetworkKeyGet_S2Authenticated);
            #endregion

            #region NetworkKeyGet_S2Access
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Secondary,
                        SecurityS2TestFrames.NetworkKeyGet_S2Access, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET
                        {
                            requestedKey = 4//(byte)SecuritySchemes.S2_ACCESS
                        });
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Secondary,
                SecurityS2TestFrames.NetworkKeyGet_S2Access, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Secondary,
                SecurityS2TestFrames.NetworkKeyGet_S2Access, true);
            ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2Temp(Secondary,
                SecurityS2TestFrames.NetworkKeyGet_S2Access);
            #endregion

            #region NetworkKeyGet_S0
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Secondary,
                        SecurityS2TestFrames.NetworkKeyGet_S0, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET
                        {
                            requestedKey = 0x80//(byte)SecuritySchemes.S0
                        });
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Secondary,
                SecurityS2TestFrames.NetworkKeyGet_S0, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Secondary,
                SecurityS2TestFrames.NetworkKeyGet_S0, true);
            ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2Temp(Secondary,
                SecurityS2TestFrames.NetworkKeyGet_S0);
            #endregion

            #region NetworkKeyVerify_S2Unauthenticated
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Secondary,
                        SecurityS2TestFrames.NetworkKeyVerify_S2Unauthenticated, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_VERIFY());
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Secondary,
                SecurityS2TestFrames.NetworkKeyVerify_S2Unauthenticated, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Secondary,
                SecurityS2TestFrames.NetworkKeyVerify_S2Unauthenticated, true);
            ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2(Secondary,
                SecurityS2TestFrames.NetworkKeyVerify_S2Unauthenticated, ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_UNAUTHENTICATED));
            #endregion

            #region NetworkKeyVerify_S2Authenticated
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Secondary,
                        SecurityS2TestFrames.NetworkKeyVerify_S2Authenticated, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_VERIFY());
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Secondary,
                SecurityS2TestFrames.NetworkKeyVerify_S2Authenticated, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Secondary,
                SecurityS2TestFrames.NetworkKeyVerify_S2Authenticated, true);
            ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2(Secondary,
                SecurityS2TestFrames.NetworkKeyVerify_S2Authenticated, ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_AUTHENTICATED));
            #endregion

            #region NetworkKeyVerify_S2Access
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Secondary,
                        SecurityS2TestFrames.NetworkKeyVerify_S2Access, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_VERIFY());
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Secondary,
                SecurityS2TestFrames.NetworkKeyVerify_S2Access, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Secondary,
                SecurityS2TestFrames.NetworkKeyVerify_S2Access, true);
            ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2(Secondary,
                SecurityS2TestFrames.NetworkKeyVerify_S2Access, ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_ACCESS));
            #endregion

            #region NetworkKeyVerify_S0
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Secondary,
                        SecurityS2TestFrames.NetworkKeyVerify_S0, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_VERIFY());
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Secondary,
                SecurityS2TestFrames.NetworkKeyVerify_S0, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Secondary,
                SecurityS2TestFrames.NetworkKeyVerify_S0, true);
            ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2(Secondary,
                SecurityS2TestFrames.NetworkKeyVerify_S0, ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S0));
            #endregion

            #region TransferEndB
            ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Secondary,
                        SecurityS2TestFrames.TransferEndB, new COMMAND_CLASS_SECURITY_2.SECURITY_2_TRANSFER_END
                        {
                            properties1 = new COMMAND_CLASS_SECURITY_2.SECURITY_2_TRANSFER_END.Tproperties1
                            {
                                keyRequestComplete = 1,
                                keyVerified = 0
                            }
                        });
            ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Secondary,
                SecurityS2TestFrames.TransferEndB, 9000);
            ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Secondary,
                SecurityS2TestFrames.TransferEndB, true);
            ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2Temp(Secondary,
                SecurityS2TestFrames.TransferEndB);
            #endregion

            var addToken = AsyncService.AddNode(Primary, Modes.NodeAny, 5 * 60 * 1000);
            var learnToken = AsyncService.SetLearnMode(Secondary, LearnModes.LearnModeClassic, 5 * 60 * 1000);
            var addRes = addToken.WaitCompletedSignal();
            var learnRes = learnToken.WaitCompletedSignal();
            AssertIsTrue(addRes);
            AssertIsTrue(learnRes);
            AssertIsTrue(addRes.SubstituteStatus == SubstituteStatuses.Done);
            AssertIsTrue(learnRes.SubstituteStatus == SubstituteStatuses.Done);
        }
    }
}