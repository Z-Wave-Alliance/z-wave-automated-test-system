// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;
using System.Linq;

namespace ZatsTests
{
    [ZatsTestCase("Controller Request Node Neighbors Discovery (alternative update, 0x68) test")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.Controller)]
    [TestInterfaceDeviceMapping(3, LibTypes.SensorPIR)]
    [TestInterfaceDeviceMapping(4, LibTypes.DoorLockKeyPad)]
    public class appbasic_RequestNodeNeighborDiscovery : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias dut = new Alias(1);
            Alias secondary = new Alias(2);
            Alias sensorPIR = new Alias(3);
            Alias doorLockKeyPad = new Alias(4);

            Alias[] All = new Alias[] { dut, secondary };

            ParallelService.Reset(All);

            ApplicationService.AddSecurity(dut, SecurityVersions.S2);
            ApplicationService.AddSecurity(secondary, SecurityVersions.S2);

            ReportService.LogText("1. DUT includes Controller(2)");
            var addRes = Service.IncludeNode(dut, secondary);
            AssertIsTrue(addRes, "1. Successful inclusion");
            var secondaryNode = addRes.AddNodeResult.Node;

            ZWAVE_CMD_CLASS.FIND_NODES_IN_RANGE nodesInRangExpResult = new ZWAVE_CMD_CLASS.FIND_NODES_IN_RANGE();
            ZWAVE_CMD_CLASS.NODE_RANGE_INFO nodeRangeInfoExpResult = new ZWAVE_CMD_CLASS.NODE_RANGE_INFO();

            //ReportService.LogText("2. DUT Neighbors Update Controller(2)");
            //var zt1 = ZnifferService.ExpectData(10000);
            //zt1.SetUpWatch(0x01, secondaryNode, nodesInRangExpResult);
            //zt1.SetUpWatch(secondaryNode, 0x01, nodeRangeInfoExpResult);

            //var reqRes1 = Service.RequestNodeNeighborDiscovery(dut, secondary, 0);

            //var ztRes1 = zt1.WaitCompletedSignal();
            //AssertIsTrue(reqRes1 && ztRes1, "2. DUT sends Find Nodes In Range, Controller(2) responds with Node Range Info");
            //nodesInRangExpResult = ztRes1.Frames[0].Payload;
            //nodeRangeInfoExpResult = ztRes1.Frames[1].Payload;
            //AssertIsTrue(nodesInRangExpResult.maskByteS.Contains(1), "FIND_NODES_IN_RANGE Mask Bytes = 1");
            //AssertIsTrue(nodeRangeInfoExpResult.maskByteS.Contains(1), "NODE_RANGE_INFO Mask Bytes = 1");

            ReportService.LogText("3. DUT includes Slaves");
            TestInterfaceService.ExcludeNode(dut, sensorPIR);
            var tiAddRes2 = TestInterfaceService.IncludeNode(dut, sensorPIR);
            AssertIsTrue(tiAddRes2);

            TestInterfaceService.ExcludeNode(dut, doorLockKeyPad);
            var tiAddRes3 = TestInterfaceService.IncludeNode(dut, doorLockKeyPad);
            AssertIsTrue(tiAddRes3);


            ReportService.LogText("5. DUT Request Neighbors Discovery from Secondary for each Node type");

            ReportService.LogText("5.1. Node type = 0");
            var zt = ZnifferService.ExpectData(10000);
            zt.SetUpWatch(0x01, secondaryNode, null, new ZWAVE_CMD_CLASS.FIND_NODES_IN_RANGE(), null, null, false, 2, null);
            zt.SetUpWatch(secondaryNode, 0x01, null, new ZWAVE_CMD_CLASS.NODE_RANGE_INFO(), null, null, false, 2, null);
            var reqRes = Service.RequestNodeNeighborDiscovery(dut, secondary, 0);
            var ztRes = zt.WaitCompletedSignal();
            AssertIsTrue(reqRes && ztRes, "5.1. DUT sends Find Nodes In Range, Controller(2) responds with Node Range Info");
            nodesInRangExpResult = ztRes.Frames[0].Payload;
            nodeRangeInfoExpResult = ztRes.Frames[1].Payload;
            ZWAVE_CMD_CLASS.FIND_NODES_IN_RANGE nodesInRangeResultFLiRS = ztRes.Frames[2].Payload;
            ZWAVE_CMD_CLASS.NODE_RANGE_INFO nodeRangeInfoResultFLiRS = ztRes.Frames.Last().Payload;
            AssertIsTrue(nodesInRangExpResult.maskByteS.Contains(0x01), "FIND_NODES_IN_RANGE Mask Bytes = 1");
            AssertIsTrue(nodeRangeInfoExpResult.maskByteS.Contains(0x01), "NODE_RANGE_INFO Mask Bytes = 1");
            AssertIsTrue(nodesInRangeResultFLiRS.maskByteS.Contains(0x08), "FIND_NODES_IN_RANGE Mask Bytes = 8 - FLiRS id");
            AssertIsTrue(nodeRangeInfoResultFLiRS.maskByteS.Contains(0x08), "NODE_RANGE_INFO Mask Bytes = 8 - FLiRS id");
            Inconclusive(reqRes.NeighborUpdateStatus == RequestNeighborUpdateStatuses.Done, "Request Node Neighbor Discovery Status = " + reqRes.NeighborUpdateStatus);

            Delay(2345, "Strange separator");

            ReportService.LogText("5.2. Node type = 1");
            zt = ZnifferService.ExpectData(10000);
            zt.SetUpWatch(0x01, secondaryNode, null, new ZWAVE_CMD_CLASS.FIND_NODES_IN_RANGE(), null, null, false, 2, null);
            zt.SetUpWatch(secondaryNode, 0x01, null, new ZWAVE_CMD_CLASS.NODE_RANGE_INFO(), null, null, false, 2, null);
            reqRes = Service.RequestNodeNeighborDiscovery(dut, secondary, 1);
            ztRes = zt.WaitCompletedSignal();
            AssertIsTrue(reqRes && ztRes, "5.2. DUT sends Find Nodes In Range, Controller(2) responds with Node Range Info");
            nodesInRangExpResult = ztRes.Frames[0].Payload;
            nodeRangeInfoExpResult = ztRes.Frames[1].Payload;
            nodesInRangeResultFLiRS = ztRes.Frames[2].Payload;
            nodeRangeInfoResultFLiRS = ztRes.Frames.Last().Payload;
            AssertIsTrue(nodesInRangExpResult.maskByteS.Contains(0x01), "FIND_NODES_IN_RANGE Mask Bytes = 1");
            AssertIsTrue(nodeRangeInfoExpResult.maskByteS.Contains(0x01), "NODE_RANGE_INFO Mask Bytes = 1");
            AssertIsTrue(nodesInRangeResultFLiRS.maskByteS.Contains(0x08), "FIND_NODES_IN_RANGE Mask Bytes = 8 - FLiRS id");
            AssertIsTrue(nodeRangeInfoResultFLiRS.maskByteS.Contains(0x08), "NODE_RANGE_INFO Mask Bytes = 8 - FLiRS id");
            Inconclusive(reqRes.NeighborUpdateStatus == RequestNeighborUpdateStatuses.Done, "Request Node Neighbor Discovery Status = " + reqRes.NeighborUpdateStatus);

            Delay(2345, "Strange separator");

            ReportService.LogText("5.3. Node type = 2");
            /// When the API is called with the node-id of the listening node and the nodes to discovery are FLIRS nodes
            /// 1. Controller will send find_nodes_in_range for each FLIRS node in the network where the listening node is the only node in the bitmask
            /// 2. The Controller will create node-id bitmask for each FLIRS node that can reach the listening node
            /// 3. The Controller will update the routing info for the listening node
            zt = ZnifferService.ExpectData(20000);
            zt.SetUpWatch(0x01, tiAddRes3.AddNodeResult.Node, new ZWAVE_CMD_CLASS.FIND_NODES_IN_RANGE());
            zt.SetUpWatch(tiAddRes3.AddNodeResult.Node, null, new ZWAVE_CMD_CLASS.NODE_RANGE_INFO());
            reqRes = Service.RequestNodeNeighborDiscovery(dut, secondary, 2);
            ztRes = zt.WaitCompletedSignal();
            AssertIsTrue(reqRes && ztRes, "5.3. DUT sends Find Nodes In Range, Controller(2) responds with Node Range Info, Controller will send find_nodes_in_range for each FLIRS node in the network");
            nodesInRangExpResult = ztRes.Frames[0].Payload;
            nodeRangeInfoExpResult = ztRes.Frames.Last().Payload;
            AssertIsTrue(nodesInRangExpResult.maskByteS.Contains(0x02), "FIND_NODES_IN_RANGE Mask Bytes = 2 - the listening node is the only node in the bitmask");
            AssertIsTrue(nodeRangeInfoExpResult.maskByteS.Contains(0x02), "NODE_RANGE_INFO Mask Bytes = 2 - the listening node is the only node in the bitmask");
            Inconclusive(reqRes.NeighborUpdateStatus == RequestNeighborUpdateStatuses.Done, "Request Node Neighbor Discovery Status = " + reqRes.NeighborUpdateStatus);

            Delay(2345, "Strange separator");

            ReportService.LogText("6.Request Discoverty with Node type = 0, Dest FLiRS");
            zt = ZnifferService.ExpectData(20000);
            zt.SetUpWatch(0x01, tiAddRes3.AddNodeResult.Node, new ZWAVE_CMD_CLASS.FIND_NODES_IN_RANGE());
            zt.SetUpWatch(tiAddRes3.AddNodeResult.Node, null, new ZWAVE_CMD_CLASS.NODE_RANGE_INFO());
            TestInterfaceService.SoftReset(sensorPIR);
            reqRes = ComplianceService.RequestNodeNeighborDiscovery(dut, tiAddRes3.AddNodeResult.Node, 0);
            ztRes = zt.WaitCompletedSignal();
            AssertIsTrue(reqRes && ztRes, "6. DUT sends Find Nodes In Range, Controller(2) responds with Node Range Info, Controller will send find_nodes_in_range for each FLIRS node in the network");
            nodesInRangExpResult = ztRes.Frames[0].Payload;
            nodeRangeInfoExpResult = ztRes.Frames.Last().Payload;
            //6
            AssertIsTrue(nodesInRangExpResult.maskByteS.Contains(0x03), "FIND_NODES_IN_RANGE Mask Bytes = 7 - the listening node is the only node in the bitmask");
            AssertIsTrue(nodeRangeInfoExpResult.maskByteS.Contains(0x03), "NODE_RANGE_INFO Mask Bytes = 7 - the listening node is the only node in the bitmask");
            Inconclusive(reqRes.NeighborUpdateStatus == RequestNeighborUpdateStatuses.Done, "Request Node Neighbor Discovery Status = " + reqRes.NeighborUpdateStatus);

            Delay(2345, "Strange separator");

            ReportService.LogText("7.Request Discoverty with Node type = 2, Dest FLiRS");
            zt = ZnifferService.ExpectData(20000);
            zt.SetUpWatch(0x01, tiAddRes3.AddNodeResult.Node, new ZWAVE_CMD_CLASS.FIND_NODES_IN_RANGE());
            zt.SetUpWatch(tiAddRes3.AddNodeResult.Node, null, new ZWAVE_CMD_CLASS.NODE_RANGE_INFO());
            TestInterfaceService.SoftReset(sensorPIR);
            reqRes = ComplianceService.RequestNodeNeighborDiscovery(dut, tiAddRes3.AddNodeResult.Node, 2);
            ztRes = zt.WaitCompletedSignal();
            AssertIsTrue(reqRes && ztRes, "7. DUT sends Find Nodes In Range, Controller(2) responds with Node Range Info, Controller will send find_nodes_in_range for each FLIRS node in the network");
            nodesInRangExpResult = ztRes.Frames[0].Payload;
            nodeRangeInfoExpResult = ztRes.Frames.Last().Payload;
            AssertIsTrue(nodesInRangExpResult.maskByteS.Contains(0x08), "Contains itself as only one FLiRS");
            AssertIsTrue(!nodeRangeInfoExpResult.maskByteS.Contains(0x08), "Can't detect itself");
            Inconclusive(reqRes.NeighborUpdateStatus == RequestNeighborUpdateStatuses.Failed, "Request Node Neighbor Discovery Status = " + reqRes.NeighborUpdateStatus);



            Delay(2345, "Strange separator");

            ReportService.LogText("8. Request Discoverty with Node type = 0, Dest Sleeping Node");
            zt = ZnifferService.ExpectData(20000);
            zt.SetUpWatch(0x01, tiAddRes2.AddNodeResult.Node, new ZWAVE_CMD_CLASS.FIND_NODES_IN_RANGE());
            zt.SetUpWatch(tiAddRes2.AddNodeResult.Node, null, new ZWAVE_CMD_CLASS.NODE_RANGE_INFO());
            TestInterfaceService.SoftReset(sensorPIR);
            reqRes = ComplianceService.RequestNodeNeighborDiscovery(dut, tiAddRes2.AddNodeResult.Node, 0);
            ztRes = zt.WaitCompletedSignal();
            AssertIsTrue(reqRes && ztRes, "8. DUT sends Find Nodes In Range, Controller(2) responds with Node Range Info, Controller will send find_nodes_in_range for each FLIRS node in the network");
            nodesInRangExpResult = ztRes.Frames[0].Payload;
            nodeRangeInfoExpResult = ztRes.Frames[1].Payload;
            AssertIsTrue(nodesInRangExpResult.maskByteS.Contains(0x03), "FIND_NODES_IN_RANGE Mask Bytes = 3 - the listening node is the only node in the bitmask (1,2)");
            AssertIsTrue(nodeRangeInfoExpResult.maskByteS.Contains(0x03), "NODE_RANGE_INFO Mask Bytes = 3 - the listening node is the only node in the bitmask (1,2)");
            Inconclusive(reqRes.NeighborUpdateStatus == RequestNeighborUpdateStatuses.Done, "Request Node Neighbor Discovery Status = " + reqRes.NeighborUpdateStatus);

            Delay(2345, "Strange separator");

            ReportService.LogText("9. Request Discoverty with Node type = 2, Dest Sleeping Node");
            zt = ZnifferService.ExpectData(20000);
            zt.SetUpWatch(0x01, tiAddRes3.AddNodeResult.Node, new ZWAVE_CMD_CLASS.FIND_NODES_IN_RANGE()); //(!) to FLiRS
            zt.SetUpWatch(tiAddRes3.AddNodeResult.Node, null, new ZWAVE_CMD_CLASS.NODE_RANGE_INFO());
            TestInterfaceService.SoftReset(sensorPIR);
            reqRes = ComplianceService.RequestNodeNeighborDiscovery(dut, tiAddRes2.AddNodeResult.Node, 2);
            ztRes = zt.WaitCompletedSignal();
            AssertIsTrue(reqRes && ztRes, "9. DUT sends Find Nodes In Range, Controller(2) responds with Node Range Info, Controller will send find_nodes_in_range for each FLIRS node in the network");
            nodesInRangExpResult = ztRes.Frames[0].Payload;
            nodeRangeInfoExpResult = ztRes.Frames[1].Payload;
            AssertIsTrue(nodesInRangExpResult.maskByteS.Contains(0x04), "FIND_NODES_IN_RANGE Mask Bytes = 4 - NON Listening aked from FLIRS");
            AssertIsTrue(nodeRangeInfoExpResult.maskByteS.Contains(0x04), "NODE_RANGE_INFO Mask Bytes = 4 - NON Listening aked from FLIRS");
            Inconclusive(reqRes.NeighborUpdateStatus == RequestNeighborUpdateStatuses.Done, "Request Node Neighbor Discovery Status = " + reqRes.NeighborUpdateStatus);

        }
    }
}
