// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"
    -SetDefault,
    -SetSIS,
    -Add/Remove controller. SecureS2 with CSA", Tags.S2, Tags.PCController)]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    public class appbasic_AddRemoveNodeControllerSecureS2WithCsa : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1, "First");
            Alias Secondary = new Alias(2, "SIS");

            ParallelService.Reset(Primary, Secondary);
            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            ApplicationService.AddSecurity(Secondary, SecurityVersions.S2);


            ReportService.LogTitle("S2 CSA inclusion");
            ReportService.LogText("1. Add second controller with first using CSA");
            ApplicationService.EnableCSA(Secondary, true);
            var addRes = Service.IncludeNode(Primary, Secondary);
            Assert.IsTrue(addRes);
            Assert.AreEqual(SubstituteStatuses.Done, addRes.AddNodeResult.SubstituteStatus);
            Assert.IsTrue(addRes.SetLearnMode);
            Assert.AreEqual(SubstituteStatuses.Done, addRes.SetLearnMode.SubstituteStatus);
            ApplicationService.EnableCSA(Secondary, false);
        }
    }
}