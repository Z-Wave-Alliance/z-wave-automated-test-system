// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"
    -SetDefault,
    -SetSIS,
    -Add/Remove controller. NonSecure, SecureS0, SecureS2")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.ControllerStaticLib)]
    public class appbasic_AddRemoveNodeControllerSecureS2 : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            #region Aliases
            Alias First = new Alias(1, "First");
            Alias SecondAsSIS = new Alias(2, "SIS");
            Alias Third = new Alias(3, "Third");
            Alias[] ALL = new Alias[] { First, SecondAsSIS, Third };
            #endregion

            #region Reset
            ParallelService.Reset(ALL);
            #endregion

            #region Add Security support
            ApplicationService.AddSecurity(First, SecurityVersions.S2);
            ApplicationService.AddSecurity(SecondAsSIS, SecurityVersions.S2);
            ApplicationService.AddSecurity(Third, SecurityVersions.S2);
            #endregion

            BasicResult.AddNodeWithLearnMode addRes = null;

            ReportService.LogTitle("S2 inclusion");
            ReportService.LogText("Add second controller with first");
            addRes = Service.AddNode(First, SecondAsSIS);
            Assert.IsTrue(addRes);
            Assert.AreEqual(SubstituteStatuses.Done, addRes.SubstituteStatus);
            Assert.IsTrue(addRes.SetLearnMode);
            Assert.AreEqual(SubstituteStatuses.Done, addRes.SetLearnMode.SubstituteStatus);

            ParallelService.Reset(ALL);

            ReportService.LogTitle("S2 inclusion");
            ReportService.LogText("Add second controller with first");
            addRes = Service.AddNode(First, SecondAsSIS);
            Assert.IsTrue(addRes);
            Assert.AreEqual(SubstituteStatuses.Done, addRes.SubstituteStatus);
            Assert.IsTrue(addRes.SetLearnMode);
            Assert.AreEqual(SubstituteStatuses.Done, addRes.SetLearnMode.SubstituteStatus);

            ReportService.LogText("Set second as SIS");
            Service.SetSUCNodeID(First, SecondAsSIS);

            ReportService.LogText("Add third controller with SIS(second)");
            addRes = Service.AddNode(SecondAsSIS, Third);
            Assert.IsTrue(addRes);
            Assert.AreEqual(SubstituteStatuses.Done, addRes.SubstituteStatus);
            Assert.IsTrue(addRes.SetLearnMode);
            Assert.AreEqual(SubstituteStatuses.Done, addRes.SetLearnMode.SubstituteStatus);

            ReportService.LogText("Remove all with first controller");
            Service.RemoveNodes(First, SecondAsSIS, Third);

            ReportService.LogText("Add second controller with first");
            addRes = Service.AddNode(First, SecondAsSIS);
            Assert.IsTrue(addRes);
            Assert.AreEqual(SubstituteStatuses.Done, addRes.SubstituteStatus);
            Assert.IsTrue(addRes.SetLearnMode);
            Assert.AreEqual(SubstituteStatuses.Done, addRes.SetLearnMode.SubstituteStatus);

            ReportService.LogText("Set second as SIS"); 
            Service.SetSUCNodeID(First, SecondAsSIS);

            ReportService.LogText("Add third controller with SIS(second)");
            addRes = Service.AddNode(SecondAsSIS, Third);
            Assert.IsTrue(addRes);
            Assert.AreEqual(SubstituteStatuses.Done, addRes.SubstituteStatus);
            Assert.IsTrue(addRes.SetLearnMode);
            Assert.AreEqual(SubstituteStatuses.Done, addRes.SetLearnMode.SubstituteStatus);

            ReportService.LogText("Remove all with first controller");
            Service.RemoveNodes(First, SecondAsSIS, Third);

        }
    }
}