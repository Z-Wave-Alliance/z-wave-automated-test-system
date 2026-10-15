// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("DEV* Multicast Synchronization test")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.ControllerStaticLib)]
    [DeviceMapping(4, LibTypes.EndDeviceLib)]
    [DeviceMapping(5, LibTypes.EndDeviceLib)]
    [TestInterfaceDeviceMapping(6, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(7, LibTypes.SwitchOnOff)]
    class appbasic_Security2_Multicast_SyncTest : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Ctrl1 = new Alias(2);
            Alias Ctrl2 = new Alias(3);
            Alias Slave1 = new Alias(4);
            Alias Slave2 = new Alias(5);
            Alias TestApp1 = new Alias(6);
            Alias TestApp2 = new Alias(7);
            Alias[] ALL = new Alias[] { Primary, Ctrl1, Ctrl2, Slave1, Slave2 };

            ParallelService.Reset(ALL);
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
            Service.ApplicationNodeInformation(Ctrl2, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER, supportedCC);
            Service.ApplicationNodeInformation(Slave1, true, (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED, supportedCC);
            Service.ApplicationNodeInformation(Slave2, true, (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED, supportedCC);

            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, new byte[] { 0xC0 }.Concat(new byte[15]).ToArray(), SecuritySchemes.S2_UNAUTHENTICATED);
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, new byte[] { 0xC1 }.Concat(new byte[15]).ToArray(), SecuritySchemes.S2_AUTHENTICATED);
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, new byte[] { 0xC2 }.Concat(new byte[15]).ToArray(), SecuritySchemes.S2_ACCESS);

            //CSL
            var addRes = Service.IncludeNode(Primary, Ctrl1);
            AssertIsTrue(addRes, "Dut1 Id = " + addRes.AddNodeResult.Node);
            AssertIsTrue(SubstituteStatuses.Done == addRes.AddNodeResult.SubstituteStatus);
            var dutNid1 = addRes.AddNodeResult.Node;

            addRes = Service.IncludeNode(Primary, Ctrl2);
            AssertIsTrue(addRes, "Dut2 Id = " + addRes.AddNodeResult.Node);
            AssertIsTrue(SubstituteStatuses.Done == addRes.AddNodeResult.SubstituteStatus);
            var dutNid2 = addRes.AddNodeResult.Node;

            addRes = Service.IncludeNode(Primary, Slave1);
            AssertIsTrue(addRes, "Slave1 Id = " + addRes.AddNodeResult.Node);
            AssertIsTrue(SubstituteStatuses.Done == addRes.AddNodeResult.SubstituteStatus);
            var slNid1 = addRes.AddNodeResult.Node;

            addRes = Service.IncludeNode(Primary, Slave2);
            AssertIsTrue(addRes, "Slave2 Id = " + addRes.AddNodeResult.Node);
            AssertIsTrue(SubstituteStatuses.Done == addRes.AddNodeResult.SubstituteStatus);
            var slNid2 = addRes.AddNodeResult.Node;

            TestInterfaceService.ExcludeNode(Primary, TestApp1);
            var tiAddRes = TestInterfaceService.IncludeNode(Primary, TestApp1);
            AssertIsTrue(tiAddRes, "TI Slave1 Id = " + tiAddRes.AddNodeResult.Node);
            AssertIsTrue(SubstituteStatuses.Done == tiAddRes.AddNodeResult.SubstituteStatus);
            var tiSlNid1 = tiAddRes.AddNodeResult.Node;

            TestInterfaceService.ExcludeNode(Primary, TestApp2);
            tiAddRes = TestInterfaceService.IncludeNode(Primary, TestApp2);
            AssertIsTrue(tiAddRes, "TI Slave2 Id = " + tiAddRes.AddNodeResult.Node);
            AssertIsTrue(SubstituteStatuses.Done == tiAddRes.AddNodeResult.SubstituteStatus);
            var tiSlNid2 = tiAddRes.AddNodeResult.Node;

            int mcRepeat = 2;
            var data2send = new COMMAND_CLASS_BASIC.BASIC_SET();
            var nodeIdsCase = new Node[][]
            { 
                //1 app
                //new Node[]{ dutNid1 },
                //new Node[]{ dutNid2 },
                //new Node[]{ slNid1 },
                //new Node[]{ slNid2 },
                new Node[]{ tiSlNid1 },
                new Node[]{ tiSlNid2 },
                //2 same app:
                new Node[]{ tiSlNid1, tiSlNid2 },
                new Node[]{ slNid1, slNid2 },
                new Node[]{ dutNid1, dutNid2 },
                //2 diff app:
                //new Node[]{ dutNid1, slNid1 },
                new Node[]{ dutNid1, tiSlNid1 },
                //new Node[]{ dutNid1, slNid1 },
                //new Node[]{ dutNid1, slNid2 },
                //new Node[]{ dutNid, slNid1, slNid2 }
            };

            foreach (var nodeIds in nodeIdsCase)
            {
                foreach (var nodeId in nodeIds)
                {
                    ComplianceService.SendData(Primary, nodeId, data2send);
                }

                for (int i = 0; i < mcRepeat; i++)
                {
                    var reqRes2 = ComplianceService.SendDataMulti(Primary, nodeIds, data2send, TX_OPTIONS);
                    Delay(2000);
                }
            }
        }
    }
}
