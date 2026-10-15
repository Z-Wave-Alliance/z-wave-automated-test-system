// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("Multicast Serial Api Slave", Tags.Association)]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.ControllerStaticLib)]
    [DeviceMapping(4, LibTypes.EndDeviceLib)]
    [TestInterfaceDeviceMapping(5, LibTypes.DoorLockKeyPad)]
    class appbasic_S2Multicast_SerialApiSlave : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Ctrl1 = new Alias(2);
            Alias Ctrl2 = new Alias(3);
            Alias Slave = new Alias(4);
            Alias TestSlave = new Alias(5);

            Alias[] ALL = new Alias[] { Primary, Ctrl1, Ctrl2, Slave };

            ParallelService.SetDefault(ALL);

            ApplicationService.AddSupervision(Primary);
            ApplicationService.AddSupervision(Ctrl1);
            ApplicationService.AddSupervision(Ctrl2);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S0 | SecurityVersions.S2);
            ApplicationService.AddSecurity(Ctrl1, SecurityVersions.S0 | SecurityVersions.S2);
            ApplicationService.AddSecurity(Ctrl2, SecurityVersions.S0 | SecurityVersions.S2);

            ApplicationService.AddTransportService(Primary);
            ApplicationService.AddTransportService(Ctrl1);
            ApplicationService.AddTransportService(Ctrl2);

            var supportedCC = new byte[] { COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID, COMMAND_CLASS_BASIC.ID };
            Service.ApplicationNodeInformation(Ctrl1, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER, supportedCC);
            Service.ApplicationNodeInformation(Slave, DeviceOptions.FreqListeningMode250ms, (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED, supportedCC);
            Service.SoftReset(Slave);

            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, new byte[] { 0xC0 }.Concat(new byte[15]).ToArray(), SecuritySchemes.S2_UNAUTHENTICATED);
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, new byte[] { 0xC1 }.Concat(new byte[15]).ToArray(), SecuritySchemes.S2_AUTHENTICATED);
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, new byte[] { 0xC2 }.Concat(new byte[15]).ToArray(), SecuritySchemes.S2_ACCESS);


            var addRes = Service.IncludeNode(Primary, Slave);
            AssertIsTrue(addRes, "Slave Id = " + addRes.AddNodeResult.Node);
            AssertIsTrue(SubstituteStatuses.Done == addRes.AddNodeResult.SubstituteStatus);
            var slaveId = addRes.AddNodeResult.Node;

            var removeTestRes = TestInterfaceService.ExcludeNode(Primary, TestSlave);
            var addTestRes = TestInterfaceService.IncludeNode(Primary, TestSlave);
            AssertIsTrue(addTestRes, "TestSlave Id = " + addTestRes.AddNodeResult.Node);
            AssertIsTrue(SubstituteStatuses.Done == addTestRes.AddNodeResult.SubstituteStatus);
            var testSlaveId = addTestRes.AddNodeResult.Node;

            addRes = Service.IncludeNode(Primary, Ctrl1);
            AssertIsTrue(addRes, "Ctrl1 Id = " + addRes.AddNodeResult.Node);
            AssertIsTrue(SubstituteStatuses.Done == addRes.AddNodeResult.SubstituteStatus);
            var ctrl1Id = addRes.AddNodeResult.NodeId;

            addRes = Service.IncludeNode(Primary, Ctrl2);
            AssertIsTrue(addRes, "Ctrl2 Id = " + addRes.AddNodeResult.Node);
            AssertIsTrue(SubstituteStatuses.Done == addRes.AddNodeResult.SubstituteStatus);
            var ctrl2Id = addRes.AddNodeResult.NodeId;

            var data = new COMMAND_CLASS_BASIC.BASIC_SET();
            for (int i = 0; i < 5; i++)
            {
                var sendRes = ComplianceService.SendDataEx(Slave, 0x01, data, SecuritySchemes.S2_ACCESS);
                Service.SetSleepMode(Slave, SleepModes.FrequentlyListening, 1);
                TestInterfaceService.SoftReset(Slave);
                var sendMultiRes = ComplianceService.SendDataMultiEx(Slave, new Node[] { 0x01, ctrl1Id, ctrl2Id }, 0x02, data, SecuritySchemes.S2_ACCESS);
            }

            ReportService.LogText("Sync Dut with Secondary Controllers");
            ComplianceService.RequestNodeInfo(Ctrl1, testSlaveId);
            ComplianceService.RequestNodeInfo(Ctrl2, testSlaveId);
            ComplianceService.SendData(Primary, testSlaveId, new COMMAND_CLASS_ASSOCIATION_V2.ASSOCIATION_SET()
            {
                groupingIdentifier = 0x01,
                nodeId = new byte[] { ctrl1Id, ctrl2Id }
            });


            for (int i = 0; i < 5; i++)
            {
                ComplianceService.RequestData(Primary, testSlaveId, new COMMAND_CLASS_ASSOCIATION_V2.ASSOCIATION_GET()
                {
                    groupingIdentifier = 0x01
                },
                new COMMAND_CLASS_ASSOCIATION_V2.ASSOCIATION_REPORT());
                Delay(0 + i * 500);
                TestInterfaceService.SoftReset(TestSlave);
                Delay(2000);
            }

        }
    }
}
