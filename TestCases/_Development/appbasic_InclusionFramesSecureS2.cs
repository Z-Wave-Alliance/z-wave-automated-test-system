// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Security 2 Testing Inclusion Frames Test case", Tags.Long, Tags.S2)]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    public class appbasic_InclusionFramesSecureS2 : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);
            Alias[] ALL = new Alias[] { Primary, Secondary };

            ParallelService.Reset(ALL);

            Service.ApplicationNodeInformation(Secondary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER, COMMAND_CLASS_SECURITY_2.ID);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            ApplicationService.AddSecurity(Secondary, SecurityVersions.S2);

            //Example usage
            //ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary, SecurityS2TestFrames.KEXGet, new COMMAND_CLASS_SECURITY_2.KEX_GET());
            //ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Primary, SecurityS2TestFrames.KEXGet, 9000);
            //ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Primary, SecurityS2TestFrames.KEXGet, false);
            //ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2(Primary, SecurityS2TestFrames.KEXGet, null);
            //Note: if you set up temp and null as network key then default Temp key will be used.
            //ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2Temp(Primary, SecurityS2TestFrames.KEXGet);


            #region Command
            foreach (SecurityS2TestFrames item in Enum.GetValues(typeof(SecurityS2TestFrames)))
            {
                ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Primary, item, new COMMAND_CLASS_BASIC.BASIC_GET());
                ApplicationService.SecurityTestSettings.SetInclusionFrameCommandS2(Secondary, item, new COMMAND_CLASS_BASIC.BASIC_GET());

                var addToken = AsyncService.AddNode(Primary, Modes.NodeAny, 30000);
                var learnToken = AsyncService.SetLearnMode(Secondary);
                var addRes = addToken.WaitCompletedSignal();
                var learnRes = learnToken.WaitCompletedSignal();
                AssertIsTrue(addRes);
                AssertIsTrue(learnRes);
                AssertIsTrue(addRes.SubstituteStatus == SubstituteStatuses.Failed);
                AssertIsTrue(learnRes.SubstituteStatus == SubstituteStatuses.Failed);

                ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Primary);
                ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Secondary);
                ParallelService.Reset(ALL);
            }
            #endregion

            #region Delay
            foreach (SecurityS2TestFrames item in Enum.GetValues(typeof(SecurityS2TestFrames)))
            {
                ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Primary, item, 10000);
                ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(Secondary, item, 10000);

                var addToken = AsyncService.AddNode(Primary, Modes.NodeAny, 30000);
                var learnToken = AsyncService.SetLearnMode(Secondary);
                var addRes = addToken.WaitCompletedSignal();
                var learnRes = learnToken.WaitCompletedSignal();
                AssertIsTrue(addRes);
                AssertIsTrue(learnRes);
                AssertIsTrue(addRes.SubstituteStatus == SubstituteStatuses.Failed);
                AssertIsTrue(learnRes.SubstituteStatus == SubstituteStatuses.Failed);

                ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Primary);
                ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Secondary);
                ParallelService.Reset(ALL);
            }
            #endregion

            #region PlainCommand
            foreach (SecurityS2TestFrames item in Enum.GetValues(typeof(SecurityS2TestFrames)))
            {
                ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Primary, item, false);
                ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Secondary, item, false);

                var addToken = AsyncService.AddNode(Primary, Modes.NodeAny, 30000);
                var learnToken = AsyncService.SetLearnMode(Secondary);
                var addRes = addToken.WaitCompletedSignal();
                var learnRes = learnToken.WaitCompletedSignal();
                AssertIsTrue(addRes);
                AssertIsTrue(learnRes);
                AssertIsTrue(addRes.SubstituteStatus == SubstituteStatuses.Failed);
                AssertIsTrue(learnRes.SubstituteStatus == SubstituteStatuses.Failed);

                ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Primary);
                ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Secondary);
                ParallelService.Reset(ALL);
            }
            #endregion

            #region CustomEncryption
            foreach (SecurityS2TestFrames item in Enum.GetValues(typeof(SecurityS2TestFrames)))
            {
                ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Primary, item, true);
                ApplicationService.SecurityTestSettings.SetInclusionFrameEncryptedS2(Secondary, item, true);
                ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2(Primary, item, new byte[16]);
                ApplicationService.SecurityTestSettings.SetInclusionFrameNetworkKeyS2(Secondary, item, new byte[16]);


                var addToken = AsyncService.AddNode(Primary, Modes.NodeAny, 30000);
                var learnToken = AsyncService.SetLearnMode(Secondary);
                var addRes = addToken.WaitCompletedSignal();
                var learnRes = learnToken.WaitCompletedSignal();
                AssertIsTrue(addRes);
                AssertIsTrue(learnRes);
                AssertIsTrue(addRes.SubstituteStatus == SubstituteStatuses.Failed);
                AssertIsTrue(learnRes.SubstituteStatus == SubstituteStatuses.Failed);

                ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Primary);
                ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Secondary);
                ParallelService.Reset(ALL);
            }
            #endregion

        }
    }
}