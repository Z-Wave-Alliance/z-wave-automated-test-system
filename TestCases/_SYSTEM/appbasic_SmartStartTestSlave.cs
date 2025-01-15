// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Smart Start SysTest Slave")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.EndDeviceSysTestLib)]
    [DeviceMapping(3, LibTypes.EndDeviceSysTestLib)]
    public class appbasic_SmartStartTestSlave : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias SlaveSysTest1 = new Alias(2);
            Alias SlaveSysTest2 = new Alias(3);

            ReportService.LogText("Start");
            ParallelService.Reset(Primary, SlaveSysTest1, SlaveSysTest2);

            var cmdClasses = new byte[] {
                COMMAND_CLASS_ALARM.ID,
                COMMAND_CLASS_TRANSPORT_SERVICE.ID,
                COMMAND_CLASS_SUPERVISION.ID,
                COMMAND_CLASS_SECURITY.ID,
                COMMAND_CLASS_SECURITY_2.ID,
                COMMAND_CLASS_ASSOCIATION.ID
            };

            ReportService.LogText("Configure security on controller");
            ApplicationService.AddSecurity(Primary, SecurityVersions.S0 | SecurityVersions.S2);

            ReportService.LogText("Set network keys predefined values (optional)");
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Primary);

            ReportService.LogText("Set NIF on slave");
            Service.ApplicationNodeInformationCmdClasses(SlaveSysTest1, cmdClasses, cmdClasses, cmdClasses);
            Service.ApplicationNodeInformationCmdClasses(SlaveSysTest2, cmdClasses, cmdClasses, cmdClasses);

            ReportService.LogText("Disable embedded security on slave");
            Service.SetSecurityBypass(SlaveSysTest1, true);
            Service.SetSecurityBypass(SlaveSysTest2, true);

            ReportService.LogText("Configure security on slave");
            ApplicationService.AddSecurity(SlaveSysTest1, SecurityVersions.S0 | SecurityVersions.S2);
            ApplicationService.AddSecurity(SlaveSysTest2, SecurityVersions.S0 | SecurityVersions.S2);

            ReportService.LogText("Add slave dsk to the provisioning list");
            ApplicationService.Provisioning.Add(Primary, SlaveSysTest1, SecuritySchemeSet.ALL);
            ApplicationService.Provisioning.Add(Primary, SlaveSysTest2, SecuritySchemeSet.ALL);

            ReportService.LogText("Setup expect smart start inclusion");
            var addToken = AsyncService.ExpectSmartStartAdd(Primary, 60000);
            ReportService.LogText("Trigger smart start learn mode");
            var learnToken1 = AsyncService.SetLearnMode(SlaveSysTest1, LearnModes.LearnModeSmartStart | LearnModes.LearnModeNetworkMask);
            var learnToken2 = AsyncService.SetLearnMode(SlaveSysTest2, LearnModes.LearnModeSmartStart | LearnModes.LearnModeNetworkMask);
            ReportService.LogText("Wait for smart start inclusion completed");
            var addRes = addToken.WaitCompletedSignal();
            AsyncService.CancelToken(SlaveSysTest1, learnToken1);
            AsyncService.CancelToken(SlaveSysTest2, learnToken2);
            learnToken1.WaitCompletedSignal();
            learnToken2.WaitCompletedSignal();

            AssertIsTrue(addRes);
            AssertIsTrue(addRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done);
            AssertIsTrue(addRes.AddNodeResult.Node.Id > 1, " Node Id = " + addRes.AddNodeResult.Node);
        }
    }
}