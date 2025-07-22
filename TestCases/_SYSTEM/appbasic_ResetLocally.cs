// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;
using System.Linq;
using System.Collections.Generic;

namespace ZatsTests
{
    /// <summary>
    /// Base test case for:
    /// DoorLockKeyPad <see cref="TC01334"/>,
    /// SensorPIR <see cref="TC01321"/>,
    /// WallController <see cref="TC01869"/>,
    /// PowerStrip <see cref="TC01862"/>,
    /// SwitchOnOff <see cref="TC01934"/>.
    /// MultilevelSensor <see cref="C67803"/>.
    /// </summary>
    public class appbasic_ResetLocally : ZatsCodeTestCase
    {
        protected string dutName = "Not Set!";
        protected byte group2_ID = 0x02;
        protected override void ExecuteTest()
        {
            Alias Controller = new Alias(1);
            Alias dut = new Alias(2);
            Alias[] ALL = new Alias[] { Controller };

            ReportService.LogTitle("Start");

            ParallelService.SetDefault(ALL);
            ApplicationService.AddSecurity(Controller, SecurityVersions.S2);
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Controller);
            BasicResult.MemoryGetId memRes = Service.MemoryGetId(Controller);

            ReportService.LogText("1. Add all Slave nodes to PC Controller. DUT:" + dutName);
            TestInterfaceService.ExcludeNode(Controller, dut);
            var incR = TestInterfaceService.IncludeNode(Controller, dut);
            AssertIsTrue(incR, "Successful Inclusion of the " + dutName);
            AssertIsTrue(incR.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done,
                "S2 Bootstrapping successful",
                "S2 Bootstrapping failed");
            var dutId = incR.AddNodeResult.Node;

            ReportService.LogText("2. From PC Controller send CC Association Group Info Get to " + dutName);
            var groups = ApplicationService.Associations.GetGroups(Controller, dutId);
            Inconclusive(groups.Groups != null, "Count = " + groups.Groups.Count, "no groups");

            ReportService.LogText("2. and read in Report for every Group how many nodes supported in the Group.");
            COMMAND_CLASS_ASSOCIATION_V3.ASSOCIATION_GET reqCmd = new COMMAND_CLASS_ASSOCIATION_V3.ASSOCIATION_GET();
            COMMAND_CLASS_ASSOCIATION_V3.ASSOCIATION_REPORT rptCmd = new COMMAND_CLASS_ASSOCIATION_V3.ASSOCIATION_REPORT();
            reqCmd.groupingIdentifier = 0x01;
            var reqRes = ComplianceService.RequestData(Controller, dutId, reqCmd, rptCmd);
            rptCmd = reqRes.Command;
            AssertIsTrue(reqRes, "ASSOCIATION_REPORT max nodes:" + rptCmd.maxNodesSupported);


            ReportService.LogText($"Do this step if LifeLine supports 5 nodes (ZDK 6.60.00, 6.51.06)." +
                $"Create Associations from {dutName} nodeID2 to all Switches in Group 1 (LifeLine) so node ID2 has associations to 5 nodes in LifeLine (1[PC controller], 3, 4, 5, 6)." +
                $"ZDK 6.51.07 supports only 1 node in LifeLine.");
            var virtNids = new List<byte>();
            if (rptCmd.maxNodesSupported > 1)
                for (int i = 0; i < rptCmd.maxNodesSupported; i++)
                {
                    BasicResult.SetLearnMode virtAddRes = Service.SetVirtualDeviceLearnMode(Controller, 0, VirtualDeviceLearnModes.Add);
                    AssertIsTrue(virtAddRes, "Successfully added Virtual Node, Id: " + virtAddRes.Node, "Unable to add Virtual Node");
                    ApplicationService.Associations.AssociationCreate(Controller, dutId, 0x01, virtAddRes.NodeId);
                    virtNids.Add(virtAddRes.NodeId);
                }

            var grNodes = ApplicationService.Associations.GetNodes(Controller, dutId, 0x01);
            AssertIsTrue(grNodes.Nodes != null, "Associated nodes:" + string.Join(",", grNodes.Nodes.Select(x => x.ToString())), "Failed get nodes from group 1");

            ReportService.LogText("4. Initiate Device reset locally (currently HOLD S1 for 10 seconds) on node ID2.");
            var tsZT = ZnifferService.CollectData(30000).SetUpWatch(new COMMAND_CLASS_DEVICE_RESET_LOCALLY.DEVICE_RESET_LOCALLY_NOTIFICATION(), HeaderTypes.SINGLECAST, false);

            TestInterfaceService.HMIActions.Sample_Reset(dut);

            Delay(12000);
            var tsZR = tsZT.WaitCompletedSignal();
            AssertIsTrue(tsZR.Frames.Count > 0, "Make sure that node ID2 sends 'Device Reset Locally Notification'");
            if (rptCmd.maxNodesSupported > 1)
            {
                Inconclusive(tsZR.Frames.Count != 0, "to all five nodes in the Group 1 (if support for 5 nodes)");
            }
            Inconclusive(tsZR.Frames.Any(x => x.Destination == 0x01), "nodeID01 if support for one node in LifeLine. ");

            ReportService.LogTitle("4.1. Send Node Info from DUT:" + dutName);

            var zToken1 = ZnifferService.ExpectData(10000);
            var zToken2 = ZnifferService.ExpectData(10000);
            zToken1.SetUpWatch(0, 255, memRes.HomeId, new ZWAVE_CMD_CLASS.NODE_INFO(), HeaderTypes.BROADCAST, false, false);
            zToken2.SetUpWatch(0, 255, new ZWAVE_CMD_CLASS.NODE_INFO());

            Delay(1000);
            TestInterfaceService.HMIActions.Sample_LearnMode(dut);

            AssertIsTrue(!zToken1.WaitCompletedSignal() && zToken2.WaitCompletedSignal(),
                                    "Node ID changed to 0x00, with different homeID successfully",
                                    "Failed to change Node ID to 0x00");


            ReportService.LogText("6. Add sample to PC Controller again.");
            incR = TestInterfaceService.IncludeNode(Controller, dut);
            AssertIsTrue(incR, "Successful Inclusion of the " + dutName);
            AssertIsTrue(incR.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done,
                "S2 Bootstrapping successful",
                "S2 Bootstrapping failed");
            dutId = incR.AddNodeResult.Node;


            ReportService.LogText("7. Remove association to nodeID 1 in Group 1 and create associations to the rest of slave nodes in Group 2 and 3.");
            ApplicationService.Associations.RemoveNodeId(Controller, dutId, 0x01, 0x01);
            BasicResult.AssociationGetNodes nids2 = null;
            if (rptCmd.maxNodesSupported > 1 && groups.Groups.Count > 1)
            {
                foreach (var item in virtNids)
                {
                    ApplicationService.Associations.AssociationCreate(Controller, dutId, group2_ID, item);
                }
                nids2 = ApplicationService.Associations.GetNodes(Controller, dutId, group2_ID);
            }
            ReportService.LogText("8. Initiate Device reset locally (currently HOLD S1 for 10 seconds) on node ID2.");
            var zt = ZnifferService.ExpectData(30000).SetUpWatch(new COMMAND_CLASS_DEVICE_RESET_LOCALLY.DEVICE_RESET_LOCALLY_NOTIFICATION(), HeaderTypes.BROADCAST, false);

            TestInterfaceService.HMIActions.Sample_Reset(dut);
            var zr = zt.WaitCompletedSignal();
            {
                AssertIsTrue(!zr, "8. No Broadcast.");
            }


            ReportService.LogTitle("8.1. Send Node Info from DUT:" + dutName);
            zToken1 = ZnifferService.ExpectData(10000);
            zToken2 = ZnifferService.ExpectData(10000);
            zToken1.SetUpWatch(0, 255, memRes.HomeId, new ZWAVE_CMD_CLASS.NODE_INFO(), HeaderTypes.BROADCAST, false, false);
            zToken2.SetUpWatch(0, 255, new ZWAVE_CMD_CLASS.NODE_INFO());

            Delay(1000);
            for (int i = 0; i < 3; i++)
            {
                TestInterfaceService.HMIActions.Sample_LearnMode(dut);
            }

            AssertIsTrue(!zToken1.WaitCompletedSignal() && zToken2.WaitCompletedSignal(),
                                    "Node ID changed to 0x00, with different homeID successfully",
                                    "Failed to change Node ID to 0x00");
        }
    }
}