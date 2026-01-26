// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Test buttons interface")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [TestInterfaceDeviceMapping(2, LibTypes.PowerStrip, Alias = "d1")]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class apptestiface_LedsAndButtons_PowerStrip : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias PowerStrip = new Alias(2);

            Service.SetDefault(Primary);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Primary);

            AssertIsTrue(TestInterfaceService.ExcludeNode(Primary, PowerStrip));
            var includeRes = TestInterfaceService.IncludeNode(Primary, PowerStrip);
            AssertIsTrue(includeRes && includeRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done);
            var powerStripId = includeRes.AddNodeResult.Node;

            ReportService.LogTitle("BTN0 toggle");
            var expDataToken = AsyncService.ExpectData(Primary, powerStripId, new COMMAND_CLASS_SWITCH_BINARY_V2.SWITCH_BINARY_REPORT(), 2000);
            TestInterfaceService.HMIActions.PowerStrip_OnOff(PowerStrip);
            Inconclusive(expDataToken.WaitCompletedSignal());

            expDataToken = AsyncService.ExpectData(Primary, powerStripId, new COMMAND_CLASS_SWITCH_BINARY_V2.SWITCH_BINARY_REPORT(), 2000);
            TestInterfaceService.ButtonSinglePress_UserInput(PowerStrip, 0x01);
            Inconclusive(expDataToken.WaitCompletedSignal());

            ReportService.LogTitle("BTN2 toggle");
            expDataToken = AsyncService.ExpectData(Primary, powerStripId, new COMMAND_CLASS_SWITCH_MULTILEVEL_V4.SWITCH_MULTILEVEL_REPORT(), 2000);
            TestInterfaceService.HMIActions.PowerStrip_DimOnOff(PowerStrip);
            Inconclusive(expDataToken.WaitCompletedSignal());

            expDataToken = AsyncService.ExpectData(Primary, powerStripId, new COMMAND_CLASS_SWITCH_MULTILEVEL_V4.SWITCH_MULTILEVEL_REPORT(), 2000);
            TestInterfaceService.ButtonSinglePress_UserInput(PowerStrip, 0x03);
            Inconclusive(expDataToken.WaitCompletedSignal());

            ReportService.LogTitle("BTN3 toggle");
            expDataToken = AsyncService.ExpectData(Primary, powerStripId, new COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_REPORT(), 33000);
            TestInterfaceService.HMIActions.PowerStrip_ToggleNotify(PowerStrip);
            Inconclusive(expDataToken.WaitCompletedSignal());

            //ReportService.LogTitle("BTN1 hold");
            //expDataToken = AsyncService.ExpectData(Primary, powerStripId, new COMMAND_CLASS_DEVICE_RESET_LOCALLY.DEVICE_RESET_LOCALLY_NOTIFICATION(), 10000);
            //TestInterfaceService.HMIActions.Sample_Reset(PowerStrip);
            //Inconclusive(expDataToken.WaitCompletedSignal());
        }
    }
}
