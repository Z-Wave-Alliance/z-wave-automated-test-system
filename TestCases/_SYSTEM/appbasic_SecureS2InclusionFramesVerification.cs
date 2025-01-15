// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Security S2 inclusion steps verification")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    public class appbasic_SecureS2InclusionFramesVerification : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);
            Alias[] ALL = new Alias[] { Primary, Secondary };

            ParallelService.Reset(ALL);

            ApplicationService.AddBasic(Secondary);
            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            ApplicationService.AddSecurity(Secondary, SecurityVersions.S2);


            var kexGetToken = AsyncService.ExpectData(Secondary, new COMMAND_CLASS_SECURITY_2.KEX_GET());
            var kexReportToken = AsyncService.ExpectData(Primary, new COMMAND_CLASS_SECURITY_2.KEX_REPORT());
            var kexSetToken = AsyncService.ExpectData(Secondary, new COMMAND_CLASS_SECURITY_2.KEX_SET());
            var publicKeyReportAToken = AsyncService.ExpectData(Primary, new COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT());
            var publicKeyReportBToken = AsyncService.ExpectData(Secondary, new COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT());
            var kexSetEchoToken = AsyncService.ExpectData(Primary, new COMMAND_CLASS_SECURITY_2.KEX_SET());
            var kexReportEchoToken = AsyncService.ExpectData(Secondary, new COMMAND_CLASS_SECURITY_2.KEX_REPORT());
            var networkKeyGetToken = AsyncService.ExpectData(Primary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET());
            var networkKeyReportToken = AsyncService.ExpectData(Secondary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT());
            var networkKeyVerifyToken = AsyncService.ExpectData(Primary, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_VERIFY());
            

            var addToken = AsyncService.AddNodeWithDSK(Primary, 34080);
            AsyncService.WaitNodeStatus(Primary, 500, NodeStatuses.LearnReady);
            var learnToken = AsyncService.SetLearnMode(Secondary);
            var addRes = addToken.WaitCompletedSignal();
            var learnRes = learnToken.WaitCompletedSignal();

            var kexGetRes = kexGetToken.WaitCompletedSignal();
            var kexReportRes = kexReportToken.WaitCompletedSignal();
            var kexSetRes = kexSetToken.WaitCompletedSignal();
            var publicKeyReportARes = publicKeyReportAToken.WaitCompletedSignal();
            var publicKeyReportBRes = publicKeyReportBToken.WaitCompletedSignal();
            var kexSetEchoRes = kexSetEchoToken.WaitCompletedSignal();
            var kexReportEchoRes = kexReportEchoToken.WaitCompletedSignal();
            var networkKeyGetRes = networkKeyGetToken.WaitCompletedSignal();
            var networkKeyReportRes = networkKeyReportToken.WaitCompletedSignal();
            var networkKeyVerifyRes = networkKeyVerifyToken.WaitCompletedSignal();

            //Verify Inclusion was successful
            AssertIsTrue(addRes);
            AssertIsTrue(learnRes);
            
            //Verify Inclusion steps
            AssertIsTrue(kexGetRes);
            AssertIsTrue(kexReportRes);
            AssertIsTrue(kexSetRes);
            AssertIsTrue(publicKeyReportARes);
            AssertIsTrue(publicKeyReportBRes);
            AssertIsTrue(kexSetEchoRes);
            AssertIsTrue(kexReportEchoRes);
            AssertIsTrue(networkKeyGetRes);
            AssertIsTrue(networkKeyReportRes);
            AssertIsTrue(networkKeyVerifyRes);
        }
    }
}