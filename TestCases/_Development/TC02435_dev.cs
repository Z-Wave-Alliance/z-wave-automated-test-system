// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
﻿using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("Security v2 PC Controller S2 Message Encapsulation - Valid Encrypted Extensions")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    class TC02435_dev : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);
            Alias[] ALL = new Alias[] { Primary, Secondary };
            ParallelService.SetDefault(ALL);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            ApplicationService.AddSecurity(Secondary, SecurityVersions.S2);
            ApplicationService.AddBasicEx(Secondary);

            var keyS2a = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_ACCESS);
            ZnifferService.SetSecuritySupport(true);
            ZnifferService.SetSecurityNetworkKeyS2(keyS2a);


            ReportService.LogText("1. PC Controller (1) adds DUT to its network");
            var addRes = Service.AddNode(Primary, Secondary);
            byte DUTNodeID = addRes.NodeId;
            AssertIsTrue(addRes && SubstituteStatuses.Done == addRes.SubstituteStatus,
                "In 1. verify that inclusion is successful and that security 2 is enabled");


            ReportService.LogText("! - clear msg extensions");
            Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_SET());


            ReportService.LogText("2. PC Controller (1) sends to DUT a S2 message encapsulation Basic Get command with an Extension Object Type Identifier Test=0xFF, Extension Critical field set to 0, Number of usage=1 & Extension Length=1.");
            ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(Primary,
                SecurityS2ExtensionTypes.Test, SecurityS2MessageTypes.SinglecastAll,
                null, new byte[0], 1, null, false, 1, true);
            var zt = ZnifferService.ExpectData(10000);
            zt.SetUpWatch(DUTNodeID, 0x01, null, new COMMAND_CLASS_BASIC.BASIC_REPORT(), null, null, false, true);
            Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
            AssertIsTrue(zt.WaitCompletedSignal(),
"In 2. verify that DUT ignores the extension but does not reject the message encapsulation and sends Basic Report [009F.01.03.11.00D][009F.01.03.11.009][009F.01.03.12.001].");

            ReportService.LogText("3. Go to Security Test Schema and delete or clear this extension");
            ApplicationService.SecurityTestSettings.ClearTestExtensionsS2(Primary);


            ReportService.LogText("4. PC Controller (1) sends to DUT a S2 message encapsulation command with an Extension Object Type Identifier of Test=0xFF, Extension Critical field set to 1, Number of usage=1 & Extension Length=1.");
            ApplicationService.SecurityTestSettings.SetS2MessageEncapsulationExtension(Primary,
                SecurityS2ExtensionTypes.Test, SecurityS2MessageTypes.SinglecastAll,
                null, new byte[0], 1, null, true, 1, true);
            zt = ZnifferService.ExpectData(10000);
            zt.SetUpWatch(DUTNodeID, 0x01, null, new COMMAND_CLASS_BASIC.BASIC_REPORT(), null, null, false, true);
            Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
            AssertIsTrue(!zt.WaitCompletedSignal(),
"In 4. verify that DUT rejects the message encapsulation [009F.01.03.11.00D][009F.01.03.11.008]");
        }
    }
}