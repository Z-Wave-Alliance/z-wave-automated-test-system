// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"
    -SetDefault
    -Add Bridge controller to Static
    -Add Virtual Node to Static
    -Send Data from Virtual To Static")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerBridgeLib)]
    public class appbasic_AddNodeVirtualAndSendDataBridgeSecure : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1, "Primary");
            Alias Bridge = new Alias(2, "Bridge");
            Alias[] ALL = new Alias[] { Primary, Bridge};

            ReportService.LogText("Start");

            Service.ApplicationNodeInformation(Primary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER,
                new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_CONTROLLER_REPLICATION.ID, COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID,
                    COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID});
            Service.ApplicationNodeInformation(Bridge, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER,
                new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_CONTROLLER_REPLICATION.ID, COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID, 
                    COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID });
            //Enabled Security on virtual nodes:
            Service.ApplicationVirtualDeviceNodeInformation(Bridge, true, (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.PC_CONTROLLER, 
                new byte[] { COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID, COMMAND_CLASS_BASIC.ID });

            ParallelService.SetDefault(ALL);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            ApplicationService.AddSecurity(Bridge, SecurityVersions.S2);

            ReportService.LogText("1. Add Bridge to Primary securely");
            var addRes = Service.AddNode(Primary, Bridge);
            AssertIsTrue(addRes);
            AssertIsTrue(SubstituteStatuses.Done == addRes.SubstituteStatus);

            ReportService.LogText("2. Add Virtual to Primary securely");
            var addVritualRes = Service.AddNodeVirtual(Primary, Bridge);
            AssertIsTrue(addVritualRes);
            AssertIsTrue(SubstituteStatuses.Done == addVritualRes.SubstituteStatus);
            ReportService.LogText("2.1  Request Node Info from the new added Virtual Node");
            ComplianceService.RequestNodeInfo(Primary, addVritualRes.NodeId);
            var isVirtualRes = Service.IsVirtualNode(Bridge, addVritualRes.NodeId);
            AssertIsTrue(isVirtualRes);
            AssertIsTrue(isVirtualRes.RetValue);

            ReportService.LogText("3. Send Data from Virtual to Primary");
            var expectToken = AsyncService.ExpectData(Primary, new COMMAND_CLASS_BASIC.BASIC_SET(), 2000);
            var sendRes = Service.SendDataBridge(Bridge, addVritualRes.NodeId, Primary, new COMMAND_CLASS_BASIC.BASIC_SET(), TX_OPTIONS);
            var expectRes = expectToken.WaitCompletedSignal();

            AssertIsTrue(expectRes);
        }
    }
}