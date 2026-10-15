// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"
    -SetDefault
    -Add Slave to Bridge controller
    -Add Virtual Node to Bridge
    -Send Data from each Virtual node to Slave")]
    [DeviceMapping(1, LibTypes.ControllerBridgeLib)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    public class appbasic_SendDataBridgeWithVirtualNodesSecureS2 : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Bridge = new Alias(1, "Primary");
            Alias Slave = new Alias(2, "Bridge");
            Alias VirtualNode1 = new Alias(3);
            Alias VirtualNode2 = new Alias(4);
            Alias VirtualNode3 = new Alias(5);
            Alias VirtualNode4 = new Alias(6);
            Alias VirtualNode5 = new Alias(7);
            Alias VirtualNode6 = new Alias(8);
            Alias VirtualNode7 = new Alias(9);
            Alias VirtualNode8 = new Alias(10);
            Alias VirtualNode9 = new Alias(11);
            Alias VirtualNode10 = new Alias(12);
            Alias VirtualNode11 = new Alias(13);
            Alias[] ALL = new Alias[] { Bridge, Slave};

            ReportService.LogText("Start");

            Service.ApplicationNodeInformation(Bridge, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER,
                new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID});
            Service.ApplicationNodeInformation(Slave, true, (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID });

            ParallelService.SetDefault(ALL);

            ApplicationService.AddSecurity(Bridge, SecurityVersions.S2);

            ReportService.LogText("1. Add Slave to Bridge securely S2");
            var addRes = Service.AddNodeWithDSK(Bridge, Slave, 20280);
            AssertIsTrue(addRes);
            AssertIsTrue(SubstituteStatuses.Done == addRes.SubstituteStatus);

            ReportService.LogText("2. Add Virtual nodes to Bridge");
            var learnRes = Service.SetVirtualDeviceLearnMode(Bridge, 0x00, VirtualDeviceLearnModes.Add);
            AssertIsTrue(learnRes);


            var sendRes = ComplianceService.SendDataBridge(Bridge, learnRes.NodeId, addRes.NodeId, 
                new COMMAND_CLASS_VERSION.VERSION_GET(), TX_OPTIONS);

            sendRes = ComplianceService.SendDataBridge(Bridge, learnRes.NodeId, addRes.NodeId,
                new COMMAND_CLASS_VERSION.VERSION_GET(), TX_OPTIONS);
        }
    }
}