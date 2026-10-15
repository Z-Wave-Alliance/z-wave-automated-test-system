// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"
    -SetDefault
    -Add/Remove controller. Use low/high power
    -Add/Remove slave")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    public class appbasic_AddRemoveNodeSecure : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1, "Primary");
            Alias Secondary = new Alias(2, "Secondary");
            Alias Slave = new Alias(3, "Slave");
            Alias[] ALL = new Alias[] { Primary, Secondary, Slave };

            ReportService.LogText("Start");

            Service.ApplicationNodeInformation(Primary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_SECURITY.ID });
            Service.ApplicationNodeInformation(Secondary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_SECURITY.ID });
            Service.ApplicationNodeInformation(Slave, true, (byte)GenericTypes.SWITCH_MULTILEVEL, (byte)SpecificTypes.NOT_USED, new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_SECURITY.ID });

            ParallelService.SetDefault(ALL);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S0 | SecurityVersions.S2);
            ApplicationService.AddSecurity(Secondary, SecurityVersions.S0 | SecurityVersions.S2);

            BasicToken.AddNode addTkn = null;
            BasicResult.AddNode addRes = null;
            BasicToken.SetLearnMode learnTkn = null;
            BasicResult.SetLearnMode learnRes = null;

            ReportService.LogTitle("1. Set SIS at Primary");
            Service.SetSUCNodeID(Primary, Primary);

            ReportService.LogTitle("2. Adding Secondary to Primary");
            addTkn = AsyncService.AddNode(Primary);
            Assert.IsTrue(AsyncService.WaitNodeStatus(Primary, 2000, NodeStatuses.LearnReady));
            learnTkn = AsyncService.SetLearnMode(Secondary, LearnModes.LearnModeClassic);

            learnRes = learnTkn.WaitCompletedSignal();
            Assert.IsTrue(learnRes);
            Assert.AreEqual(SubstituteStatuses.Done, learnRes.SubstituteStatus);

            addRes = addTkn.WaitCompletedSignal();
            Assert.IsTrue(addRes);
            Assert.AreEqual(SubstituteStatuses.Done, addRes.SubstituteStatus);

            ReportService.LogTitle("3. Adding Slave to Secondary");
            addTkn = AsyncService.AddNode(Secondary);
            Assert.IsTrue(AsyncService.WaitNodeStatus(Secondary, 2000, NodeStatuses.LearnReady));
            learnRes = Service.SetLearnMode(Slave, LearnModes.LearnModeClassic);
            addRes = addTkn.WaitCompletedSignal();

            Assert.IsTrue(addRes);
            Assert.AreEqual(SubstituteStatuses.Done, addRes.SubstituteStatus);

            ReportService.LogTitle("4. Removing nodes");
            Service.RemoveNode(Primary, Secondary);
            Service.RemoveNode(Primary, Slave);

            ReportService.LogTitle("5. Suspend Security at Primary");
            ApplicationService.SuspendSecurity(Primary);

            ReportService.LogTitle("6. Adding Secondary to Primary");
            addTkn = AsyncService.AddNode(Primary);
            Assert.IsTrue(AsyncService.WaitNodeStatus(Primary, 2000, NodeStatuses.LearnReady));
            learnRes = Service.SetLearnMode(Secondary, LearnModes.LearnModeClassic);
            addRes = addTkn.WaitCompletedSignal();

            Assert.IsTrue(addRes);
            Assert.AreEqual(SubstituteStatuses.None, addRes.SubstituteStatus);
            Assert.IsTrue(learnRes);
            Assert.AreEqual(SubstituteStatuses.Failed, learnRes.SubstituteStatus);

            ReportService.LogTitle("7. Adding Slave to Secondary");
            addTkn = AsyncService.AddNode(Secondary);
            Assert.IsTrue(AsyncService.WaitNodeStatus(Secondary, 2000, NodeStatuses.LearnReady));
            learnRes = Service.SetLearnMode(Slave, LearnModes.LearnModeClassic, 60000);
            addRes = addTkn.WaitCompletedSignal();

            Assert.IsTrue(addRes);
            Assert.AreEqual(SubstituteStatuses.None, addRes.SubstituteStatus);
            //Assert.IsTrue(learnRes); SWPROT

            ReportService.LogTitle("8. Removing nodes");
            Service.RemoveNode(Primary, Secondary);
            Service.RemoveNode(Primary, Slave);

            ReportService.LogTitle("9. Resume Security at Primary");
            ApplicationService.ResumeSecurity(Primary);

            ReportService.LogTitle("10. Adding Secondary to Primary");
            addTkn = AsyncService.AddNode(Primary);
            Assert.IsTrue(AsyncService.WaitNodeStatus(Primary, 2000, NodeStatuses.LearnReady));
            learnRes = Service.SetLearnMode(Secondary, LearnModes.LearnModeClassic);
            addRes = addTkn.WaitCompletedSignal();

            Assert.IsTrue(addRes);
            Assert.AreEqual(SubstituteStatuses.Done, addRes.SubstituteStatus);
            Assert.IsTrue(learnRes);
            Assert.AreEqual(SubstituteStatuses.Done, learnRes.SubstituteStatus);

            ReportService.LogTitle("11. Adding Slave to Secondary");
            addTkn = AsyncService.AddNode(Secondary);
            Assert.IsTrue(AsyncService.WaitNodeStatus(Secondary, 2000, NodeStatuses.LearnReady));
            learnRes = Service.SetLearnMode(Slave, LearnModes.LearnModeClassic);
            addRes = addTkn.WaitCompletedSignal();

            Assert.IsTrue(addRes);
            Assert.AreEqual(SubstituteStatuses.Done, addRes.SubstituteStatus);
            Assert.IsTrue(learnRes);
            //Assert.AreEqual(SubstituteStatuses.Done, learnRes.SubstituteStatus);
            var res = Service.GetSecurityKeys(Slave);
            AssertIsTrue(res && res.SecurityKeysMask == 0x80);
        }
    }
}