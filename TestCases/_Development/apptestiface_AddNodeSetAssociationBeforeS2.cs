// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System.Linq;
using System.Collections.Generic;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Set Association before Security S2 Bootstrapping")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [TestInterfaceDeviceMapping(2, LibTypes.SwitchOnOff)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class apptestiface_AddNodeSetAssociationBeforeS2 : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave = new Alias(2);

            Service.SetDefault(Primary);
            Service.MemoryGetId(Primary);

            var dsk = TestInterfaceService.GetDSK(Slave, 2);
            byte ascnGroupId = 1;

            ReportService.LogText("Setup Security Manager");
            ApplicationService.AddSecurity(Primary, SecurityVersions.S0 | SecurityVersions.S2);
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Primary);

            ReportService.LogTitle("1. Include node with protocol inclusion only");
            ApplicationService.SuspendSecurity(Primary);
            TestInterfaceService.ExcludeNode(Primary, Slave);
            var incRes = TestInterfaceService.AddNode(Primary, Slave);
            AssertIsTrue(incRes);
            var nodeId = incRes.NodeId;
            ApplicationService.ResumeSecurity(Primary);

            ReportService.LogTitle("2. Perform Security S2 Bootstrapping");
            ReportService.LogText("Start S2 Bootstrapping");
            var resBoot = ApplicationService.SecurityS2.Bootstrap(Primary, nodeId, dsk);
            ReportService.LogText("Stop S2 Bootstrapping");

            ReportService.LogTitle("3. Verify that Association is empty");
            ApplicationService.SecurityTestSettings.SetActiveSecurityScheme(Primary, nodeId, resBoot.HighestScheme);
            var ascnGroup = ComplianceService.RequestData(Primary, nodeId,
                new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_GET { groupingIdentifier = 0x01 },
                new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REPORT());
            AssertIsTrue(ascnGroup, "Get Association completed", "Get Association failed");
            AssertIsTrue(!((COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REPORT)ascnGroup.Command).nodeId.Any(),
                $"There are no nodes in the association group {ascnGroupId}",
                $"There are nodes in the association group {ascnGroupId}");

            ReportService.LogTitle("4. Include node with protocol inclusion only");
            ApplicationService.SuspendSecurity(Primary);
            TestInterfaceService.ExcludeNode(Primary, Slave);
            incRes = TestInterfaceService.AddNode(Primary, Slave);
            AssertIsTrue(incRes);
            nodeId = incRes.NodeId;

            ReportService.LogTitle("5. Setup Association before Security S2 Bootstrapping");
            ComplianceService.SendData(Primary, nodeId,
                new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_SET { groupingIdentifier = 0x01, nodeId = new List<byte>(new byte[] { 0x01 }) });
            ApplicationService.ResumeSecurity(Primary);

            ReportService.LogTitle("6. Perform Security S2 Bootstrapping");
            ReportService.LogText("Start S2 Bootstrapping");
            resBoot = ApplicationService.SecurityS2.Bootstrap(Primary, nodeId, dsk);
            ReportService.LogText("Stop S2 Bootstrapping");

            ReportService.LogTitle("7. Verify that Association is empty");
            ApplicationService.SecurityTestSettings.SetActiveSecurityScheme(Primary, nodeId, resBoot.HighestScheme);
            ascnGroup = ComplianceService.RequestData(Primary, nodeId,
                new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_GET { groupingIdentifier = 0x01 },
                new COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REPORT());
            AssertIsTrue(ascnGroup, "Get Association completed", "Get Association failed");
            AssertIsTrue(!((COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V3.MULTI_CHANNEL_ASSOCIATION_REPORT)ascnGroup.Command).nodeId.Any(),
                $"There are no nodes in the association group {ascnGroupId}",
                $"There are nodes in the association group {ascnGroupId}");
        }
    }
}