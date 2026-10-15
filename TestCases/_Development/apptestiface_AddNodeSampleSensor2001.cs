// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System.Linq;
using System.Collections.Generic;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Test Case Custom Sensor 20Q1")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [TestInterfaceDeviceMapping(2, ChipSeries.ZW070x, LibTypes.SensorPIR)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class apptestiface_AddNodeSampleSensor2001 : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave = new Alias(2);

            Service.SetDefault(Primary);
            Service.MemoryGetId(Primary);

            var dsk = new byte[2];

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


        }
    }
}