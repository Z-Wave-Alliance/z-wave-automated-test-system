// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Response data with. Endpoint nodes")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class appbasic_Supervision : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave = new Alias(2);

            ReportService.LogText("Start");
            ParallelService.SetDefault(Primary, Slave);

            var cmdClasses = new byte[] {
                COMMAND_CLASS_ALARM.ID,
                COMMAND_CLASS_TRANSPORT_SERVICE.ID,
                COMMAND_CLASS_SUPERVISION.ID,
                COMMAND_CLASS_SECURITY.ID,
                COMMAND_CLASS_SECURITY_2.ID,
                COMMAND_CLASS_ASSOCIATION.ID
            };
            Service.ApplicationNodeInformationCmdClasses(Slave, cmdClasses, cmdClasses, cmdClasses);

            ApplicationService.AddMultiChannel(Primary);
            ApplicationService.AddSupervision(Primary);
            ApplicationService.AddSecurity(Primary, SecurityVersions.S0 | SecurityVersions.S2);
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Primary);
            Service.SetSecurityInclusionRequestedKeys(Slave, new[] { SecuritySchemes.S2_UNAUTHENTICATED, SecuritySchemes.S2_AUTHENTICATED });

            var addRes = Service.IncludeNode(Primary, Slave);
            Service.GetSecurityKeys(Slave);
            AssertIsTrue(addRes);

            List<CommandClassSupport> supportedCCs = new List<CommandClassSupport>();
            supportedCCs.Add(new ZWavePlusInfoSupport() { Version = 2, Secure = false });
            //supportedCCs.Add(new AssociationSupport(new List<AssociationGroupEx>()) { Version = 2, Secure = false });

            ApplicationService.SetupRootDevice(
                Slave,                                              // deviceNumber                                            
                DeviceOptions.Listening,                            // deviceOptions
                (byte)GenericTypes.SWITCH_BINARY,                   // genericDeviceClass
                (byte)SpecificTypes.NOT_USED,                       // specificDeviceClass
                true,                                               // multiChannel
                true,                                               // supervision
                false,                                              // crc16
                SecurityVersions.S2 | SecurityVersions.S0,          // securityVersions
                true,                                               // transportService
                supportedCCs);                                      // commandClassSupportList

            var st = ApplicationService.AddSupervision(Slave.EndPoint(1));

            var res = ComplianceService.RequestDataEx(Primary, addRes.AddNodeResult.Node, new COMMAND_CLASS_ZWAVEPLUS_INFO.ZWAVEPLUS_INFO_GET(), SecuritySchemes.S2_ACCESS, new COMMAND_CLASS_ZWAVEPLUS_INFO.ZWAVEPLUS_INFO_REPORT(), 2000);
            Inconclusive(res);

            ApplicationService.SetupEndPoint(
              Slave, 1,                                              // deviceNumber                                            
              (byte)GenericTypes.SWITCH_BINARY,                      // genericDeviceClass
              (byte)SpecificTypes.NOT_USED,                          // specificDeviceClass
              true,
              SecurityVersions.S2 | SecurityVersions.S0,
              supportedCCs);                                         // commandClassSupportList

            var supervisionCallbacks = new Dictionary<byte, Action<COMMAND_CLASS_SUPERVISION.SUPERVISION_GET, COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT>>
            {
                {
                    COMMAND_CLASS_BASIC.ID,
                    (x, y) =>
                    {
                        switch (x.encapsulatedCommand[1])
                        {
                            case COMMAND_CLASS_SWITCH_BINARY.SWITCH_BINARY_SET.ID:
                                var binarySwitchSetCmd = (COMMAND_CLASS_SWITCH_BINARY.SWITCH_BINARY_SET)x.encapsulatedCommand.ToArray();

                                if (binarySwitchSetCmd.switchValue >= 0x00 && binarySwitchSetCmd.switchValue <= 0x63 || binarySwitchSetCmd.switchValue == 0xFF)
                                {
                                    y.status = 0xFF;                        // SUCCESS
                                }
                                else
                                {
                                    y.status = 0x02;                        // FAIL
                                }
                                break;
                            default:
                                y.status = 0x00;                            // NO_SUPPORT
                                break;
                        }
                    }
                }
            };

            st.SetSupervisionReportCallbacks(supervisionCallbacks);

            res = ComplianceService.RequestDataEx(Primary,
                new Node(addRes.AddNodeResult.Node.Id, 1),
                new COMMAND_CLASS_SUPERVISION.SUPERVISION_GET() { encapsulatedCommand = new List<byte>((byte[])new COMMAND_CLASS_LOCK.LOCK_SET()), encapsulatedCommandLength = 3 },
                SecuritySchemes.S2_ACCESS,
                new COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT(), 2000);
            Inconclusive(res, "");
            res = ComplianceService.RequestDataEx(Primary,
                new Node(addRes.AddNodeResult.Node.Id, 1),
                new COMMAND_CLASS_SUPERVISION.SUPERVISION_GET() { encapsulatedCommand = new List<byte>((byte[])new COMMAND_CLASS_BASIC.BASIC_SET()), encapsulatedCommandLength = 3 },
                SecuritySchemes.NONE,
                new COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT(), 2000);
            Inconclusive(res);

            res = ComplianceService.RequestDataEx(Primary,
                addRes.AddNodeResult.Node,
                new COMMAND_CLASS_SUPERVISION.SUPERVISION_GET() { encapsulatedCommand = new List<byte>((byte[])new COMMAND_CLASS_LOCK.LOCK_SET()), encapsulatedCommandLength = 3 },
                SecuritySchemes.S2_ACCESS,
                new COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT(), 2000);
            Inconclusive(res);
            res = ComplianceService.RequestDataEx(Primary,
                addRes.AddNodeResult.Node,
                new COMMAND_CLASS_SUPERVISION.SUPERVISION_GET() { encapsulatedCommand = new List<byte>((byte[])new COMMAND_CLASS_BASIC.BASIC_SET()), encapsulatedCommandLength = 3 },
                SecuritySchemes.NONE,
                new COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT(), 2000);
            Inconclusive(res);



            using (Service.Settings.IsExpectingFailure.Set())
            {
                res = ComplianceService.RequestDataEx(Primary, new Node(addRes.AddNodeResult.Node.Id, 1), new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_GROUPINGS_GET(), SecuritySchemes.NONE, new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_GROUPINGS_REPORT(), 2000);
            }
            Inconclusive(!res);

            res = ComplianceService.RequestDataEx(Primary, new Node(addRes.AddNodeResult.Node.Id, 1), new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_GROUPINGS_GET(), SecuritySchemes.S2_ACCESS, new COMMAND_CLASS_ASSOCIATION.ASSOCIATION_GROUPINGS_REPORT(), 2000);
            Inconclusive(res);
        }
    }
}