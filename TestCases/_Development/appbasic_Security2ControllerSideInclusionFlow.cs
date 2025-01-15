// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Security 2 Controller Side Inclusion Workflow Test case")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    public class appbasic_Security2ControllerSideInclusionFlow : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias SecondarySlave = new Alias(2);
            Alias[] ALL = new Alias[] { Primary, SecondarySlave };


            ParallelService.Reset(ALL);
            Service.MemoryGetId(Primary);
            Service.MemoryGetId(SecondarySlave);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S0 | SecurityVersions.S2);
            var addToken = AsyncService.AddNodeWithDSK(Primary, Modes.NodeAny, 56990);
            var learnToken = AsyncService.SetLearnMode(SecondarySlave);

            var addRes = addToken.WaitCompletedSignal();
            var learnRes = learnToken.WaitCompletedSignal();


            ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(
                Primary, 
                SecurityS2ExtensionTypes.Span, 
                SecurityS2MessageTypes.SinglecastAll,
                false, 
                new byte[] { 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01 }, 
                1, 
                false, 
                true, 
                0x12, 
                false);

            Service.SendData(Primary, SecondarySlave, new COMMAND_CLASS_BASIC.BASIC_SET());

            var extensionSinglecastSpan = new TestExtensionS2Settings(
                SecurityS2MessageTypes.SinglecastWithSpan, 
                SecurityS2ExtensionTypes.Span, 
                SecurityS2TestExtensionsActions.AddOrModify)
            {
                IsEncrypted = false,
                NumOfUsage = 1,
                IsMoreToFollow = false,
                IsCritical = true,
                ExtensionLength = 0x12,
                Value = new byte[] { 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01 }
            };
            ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(Primary, extensionSinglecastSpan);

            Service.SendData(Primary, SecondarySlave, new COMMAND_CLASS_BASIC.BASIC_SET());
        }
    }
}