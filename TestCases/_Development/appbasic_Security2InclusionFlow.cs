// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Security 2 Inclusion Workflow Test case")]
    [DeviceMapping(1, LibTypes.Controller)]
    [DeviceMapping(2, LibTypes.Controller)]
    public class appbasic_Security2InclusionFlow : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {

            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);
            Alias[] ALL = new Alias[] { Primary, Secondary };

            ParallelService.Reset(ALL);

            Service.AddNode(Primary, Secondary);
            Service.MemoryGetId(Primary);
            Service.MemoryGetId(Secondary);

            #region local variables declaration

            var networkKeyS2 = new byte[16];
            var networkKeyS2Last = new byte[16];
            networkKeyS2Last[0] = 0xFF;

            var primarySecretKey = new byte[]
            {
                0x77, 0x07, 0x6d, 0x0a, 0x73, 0x18, 0xa5, 0x7d,
                0x3c,0x16, 0xc1, 0x72, 0x51, 0xb2, 0x66, 0x45 ,
                0xdf,0x4c, 0x2f,0x87,0xeb,0xc0,0x99,0x2a,0xb1,
                0x77,0xfb,0xa5,0x1d,0xb9,0x2c,0x2a};
            var secondarySecretKey = new byte[]
            {
                0x5d,0xab,0x08,0x7e,0x62,0x4a,0x8a,0x4b
        ,0x79,0xe1,0x7f,0x8b,0x83,0x80,0x0e,0xe6
        ,0x6f,0x3b,0xb1,0x29,0x26,0x18,0xb6,0xfd
        ,0x1c,0x2f,0x8b,0x27,0xff,0x88,0xe0,0xeb};

            var expectedPrimaryPublicKey = new byte[]
            {
                0x85,0x20,0xf0,0x09,0x89,0x30,0xa7,0x54
          ,0x74,0x8b,0x7d,0xdc,0xb4,0x3e,0xf7,0x5a
          ,0x0d,0xbf,0x3a,0x0d,0x26,0x38,0x1a,0xf4
          ,0xeb,0xa4,0xa9,0x8e,0xaa,0x9b,0x4e,0x6a};

            var expectedSecondaryPublicKey = new byte[]
            {
                0xde,0x9e,0xdb,0x7d,0x7b,0x7d,0xc1,0xb4
          ,0xd3,0x5b,0x61,0xc2,0xec,0xe4,0x35,0x37
          ,0x3f,0x83,0x43,0xc8,0x5b,0x78,0x67,0x4d
          ,0xad,0xfc,0x7e,0x14,0x6f,0x88,0x2b,0x4f};

            var expectedSharedKey = new byte[]
            {
                0x4a,0x5d,0x9d,0x5b,0xa4,0xce,0x2d,0xe1
          ,0x72,0x8e,0x3b,0xf4,0x80,0x35,0x0f,0x25
          ,0xe0,0x7e,0x21,0xc9,0x47,0xd1,0x9e,0x33
          ,0x76,0xf0,0x9b,0x3c,0x1e,0x16,0x17,0x42};

            #endregion

            byte[] realPrimaryPublic = null; // ApplicationService.SecurityTestSettings.GeneratePublicKey(primarySecretKey);

            AssertIsTrue(realPrimaryPublic.SequenceEqual(expectedPrimaryPublicKey), "Primary public is correct",
                "Primary public fail");


            byte[] realSecondaryPublic = null; // ApplicationService.SecurityTestSettings.GeneratePublicKey(secondarySecretKey);
            AssertIsTrue(realSecondaryPublic.SequenceEqual(expectedSecondaryPublicKey), "Secondary public is correct",
                "Secondary public fail");


            byte[] realSharedKey = null; //  ApplicationService.SecurityTestSettings.GenerateSharedKey(secondarySecretKey, realPrimaryPublic);
            AssertIsTrue(realSharedKey.SequenceEqual(expectedSharedKey), "Primary shared is correct",
                "Primary shared fail");
            //realSharedKey = ApplicationService.SecurityTestSettings.GenerateSharedKey(primarySecretKey, realSecondaryPublic);
            AssertIsTrue(realSharedKey.SequenceEqual(expectedSharedKey), "Secondary shared is correct",
                "Secondary shared fail");


            #region A1
            var kexGetCmd = new COMMAND_CLASS_SECURITY_2.KEX_GET();
            var kexReportmd = new COMMAND_CLASS_SECURITY_2.KEX_REPORT();
            var kexSetCmd = new COMMAND_CLASS_SECURITY_2.KEX_SET();
            var kexFailCmd = new COMMAND_CLASS_SECURITY_2.KEX_FAIL();
            #endregion

            #region A2
            var publicKeyPrimaryRptCmd = new COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT();
            var publicKeySecondaryRptCmd = new COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT();
            var nonceGetCmd = new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_GET();
            var nonceRptCmd = new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT();
            #endregion

            #region A3
            var kexReportEchoCmd = new COMMAND_CLASS_SECURITY_2.KEX_REPORT();
            var kexSetEchoCmd = new COMMAND_CLASS_SECURITY_2.KEX_SET();
            #endregion


            #region A4
            var networkKeyGetCmd = new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET();
            var networkKeyReportCmd = new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT();
            nonceGetCmd = new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_GET();
            nonceRptCmd = new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT();
            #endregion

            #region A5

            var networkKeyVerifyCmd = new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_VERIFY();
            var transferEndCmd = new COMMAND_CLASS_SECURITY_2.SECURITY_2_TRANSFER_END();
            transferEndCmd = new COMMAND_CLASS_SECURITY_2.SECURITY_2_TRANSFER_END();

            #endregion

            kexReportmd.requestedKeys = (byte)RequestedKeys.Security2ClassKey2;
            kexReportmd.supportedEcdhProfiles = 0;
            kexReportmd.supportedKexSchemes = 1;
            ApplicationService.AddResponse(Secondary, kexReportmd, kexGetCmd);

            var kexRepData = Service.RequestData(Primary, Secondary, kexGetCmd,
                kexReportmd);

            if (kexRepData.Command != null) kexReportEchoCmd = kexRepData.Command;
            kexReportEchoCmd.properties1.echo = 0x01;

            kexSetCmd.selectedEcdhProfile = 0;
            kexSetCmd.selectedKexScheme = 1;
            kexSetCmd.grantedKeys = (byte)RequestedKeys.Security2ClassKey2;

            publicKeyPrimaryRptCmd.ecdhPublicKey = realPrimaryPublic;
            publicKeyPrimaryRptCmd.properties1.includingNode = 1;

            publicKeySecondaryRptCmd.ecdhPublicKey = realSecondaryPublic;
            publicKeySecondaryRptCmd.properties1.includingNode = 0;

            ApplicationService.AddResponse(Secondary, publicKeySecondaryRptCmd, kexSetCmd);

            var exPrimaryToken = AsyncService.ExpectData(Primary, new COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT());
            var kexSetData = Service.RequestData(Primary, Secondary, kexSetCmd,
                publicKeySecondaryRptCmd);
            if (kexSetData.Command != null) kexSetEchoCmd = kexSetData.Command;
            kexSetEchoCmd.properties1.echo = 0x01;

            var exPrimaryRes = exPrimaryToken.WaitCompletedSignal();
            byte[] secondarySentPublic = new byte[32];
            if (exPrimaryRes.Command != null)
            {
                secondarySentPublic =
                    ((COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT)exPrimaryRes.Command).ecdhPublicKey.ToArray();
            }
            byte[] primaryShared = null; // ApplicationService.SecurityTestSettings.GenerateSharedKey(primarySecretKey, secondarySentPublic);


            var exSecondaryToken = AsyncService.ExpectData(Secondary, new COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT());
            Service.SendData(Primary, Secondary, publicKeyPrimaryRptCmd); //NOTE manually sending key report
            var exSecondaryRes = exSecondaryToken.WaitCompletedSignal();
            byte[] primarySentPublic = new byte[32];
            if (exSecondaryRes.Command != null)
            {
                primarySentPublic =
                    ((COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT)exSecondaryRes.Command).ecdhPublicKey.ToArray();
            }
            byte[] secondaryShared = null; // ApplicationService.SecurityTestSettings.GenerateSharedKey(secondarySecretKey, primarySentPublic);
            AssertIsTrue(secondaryShared.SequenceEqual(primaryShared), "Primary shared equals to Secondary shared ",
                "Primary shared does NOT equal to Secondary shared ");


            byte[] prk = null; // ApplicationService.SecurityTestSettings.TempNetworkKeyExtract(secondaryShared, primarySentPublic, secondarySentPublic);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S0);
            ApplicationService.AddSecurity(Secondary, SecurityVersions.S0);
            ApplicationService.SetSecurityVersionForNode(Primary, Secondary, SecurityVersions.S2);
            ApplicationService.SetSecurityVersionForNode(Secondary, Primary, SecurityVersions.S2);

            ApplicationService.SecurityTestSettings.SetNetworkKeyS2Temp(Primary, prk);
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2Temp(Secondary, prk);

            Service.SendData(Secondary, Primary, kexSetEchoCmd);
            Service.SendData(Primary, Secondary, kexReportEchoCmd);

            ApplicationService.AddResponse(Primary, networkKeyReportCmd, networkKeyGetCmd);
            var networkKeyReportRes = Service.RequestData(Secondary, Primary, networkKeyGetCmd, networkKeyReportCmd);
            AssertIsTrue(networkKeyReportRes, "NETWORK_KEY_REPORT received on Secondary",
                "NETWORK_KEY_REPORT was not received on Secondary");

            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(Primary, 0x02);
            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(Secondary, 0x01);
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, networkKeyS2Last, SecuritySchemes.S2_UNAUTHENTICATED);
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Secondary, networkKeyS2Last, SecuritySchemes.S2_UNAUTHENTICATED);


            transferEndCmd.properties1.keyRequestComplete = 0x01;
            transferEndCmd.properties1.keyVerified = 0x01;
            ApplicationService.AddResponse(Secondary, transferEndCmd, transferEndCmd);
            //ApplicationService.AddResponse(Primary, transferEndCmd, networkKeyVerifyCmd);


            exPrimaryToken = AsyncService.ExpectData(Primary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_VERIFY());
            Service.SendData(Secondary, Primary, networkKeyVerifyCmd);
            exPrimaryRes = exPrimaryToken.WaitCompletedSignal();
            AssertIsTrue(exPrimaryRes, "Network Key Verify received on primary",
                "Network Key Verify wasn't received on primary");
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2Temp(Primary, prk);
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2Temp(Secondary, prk);
            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(Primary, 0x02);
            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(Secondary, 0x01);

            var requestTransferEndRes = Service.RequestData(Primary, Secondary, transferEndCmd, transferEndCmd);
            AssertIsTrue(requestTransferEndRes, "Transfer End received on Primary",
               "Transfer End was not received on Primary");


            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(Primary, 0x02);
            ApplicationService.SecurityTestSettings.FreeSpanS2ForNode(Secondary, 0x01);
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, networkKeyS2Last, SecuritySchemes.S2_UNAUTHENTICATED);
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Secondary, networkKeyS2Last, SecuritySchemes.S2_UNAUTHENTICATED);
            Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_SET { value = 0xFF });

            Service.SendData(Secondary, Primary, new COMMAND_CLASS_BASIC.BASIC_SET { value = 0xFF });


        }
    }
}