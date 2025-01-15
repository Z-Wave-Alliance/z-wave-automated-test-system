// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Linq;
using System.Collections.Generic;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Test Repeater S2 SendData")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    [DeviceMapping(4, ChipSeries.Any, LibTypes.EndDeviceLib)]
    [DeviceMapping(5, ChipSeries.Any, LibTypes.EndDeviceLib)]
    [DeviceMapping(6, LibTypes.EndDeviceLib)]
    public class appbasic_TestRepeaterS2SendData : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave = new Alias(2);
            Alias SlaveR1 = new Alias(3);
            Alias SlaveR2 = new Alias(4);
            Alias SlaveR3 = new Alias(5);
            Alias Flirs = new Alias(6);
            Alias[] ALL = new Alias[] { Primary, Slave, SlaveR1, SlaveR2, SlaveR3, Flirs };

            ParallelService.Reset(ALL);
            var cmdClasses = new byte[] {
                COMMAND_CLASS_ALARM.ID,
                COMMAND_CLASS_TRANSPORT_SERVICE.ID,
                COMMAND_CLASS_SUPERVISION.ID,
                COMMAND_CLASS_SECURITY.ID,
                COMMAND_CLASS_SECURITY_2.ID,
                COMMAND_CLASS_ASSOCIATION.ID
            };

            Service.ApplicationNodeInformationCmdClasses(Slave, cmdClasses, cmdClasses, cmdClasses);
            Service.ApplicationNodeInformationCmdClasses(SlaveR1, cmdClasses, cmdClasses, cmdClasses);
            Service.ApplicationNodeInformationCmdClasses(SlaveR2, cmdClasses, cmdClasses, cmdClasses);
            Service.ApplicationNodeInformationCmdClasses(SlaveR3, cmdClasses, cmdClasses, cmdClasses);
            Service.ApplicationNodeInformation(Flirs, DeviceOptions.FreqListeningMode1000ms,
                   (byte)GenericTypes.ENTRY_CONTROL, (byte)SpecificTypes.NOT_USED, cmdClasses);
            Service.SoftReset(Flirs);
            ApplicationService.AddSecurity(Primary, SecurityVersions.S0 | SecurityVersions.S2);
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Primary);



            var dlRes = Service.IncludeNode(Primary, Flirs);
            Delay(1000);
            Service.PowerMgmtCancel(Flirs, 0);
            Service.SetRFReceiveMode(Flirs, false);
            var sr1Res = Service.IncludeNode(Primary, SlaveR1);
            Service.SetRFReceiveMode(Flirs, true);
            var sendRes = ComplianceService.SendDataEx(SlaveR1, dlRes.AddNodeResult.Node, new COMMAND_CLASS_BASIC.BASIC_REPORT(), SecuritySchemes.S2_ACCESS);
            AssertIsTrue(sendRes);

            var sr2Res = Service.IncludeNode(Primary, SlaveR2);
            var sr3Res = Service.IncludeNode(Primary, SlaveR3);
            var sRes = Service.IncludeNode(Primary, Slave);


            sendRes = ComplianceService.SendDataEx(Slave, dlRes.AddNodeResult.Node, new COMMAND_CLASS_BASIC.BASIC_REPORT(), SecuritySchemes.S2_ACCESS);
            AssertIsTrue(sendRes);
        }
    }
}