// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Zniffer SetUpWatch Secure Inclusion")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class appzniffer_SetUpWatchSecureInclusion : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave = new Alias(2);

            ReportService.LogText("Start.");
            ParallelService.SetDefault(Primary, Slave);

            ReportService.LogText("Set up security cmd class support at Slave.");
            var cmdClasses = new byte[] { COMMAND_CLASS_ALARM.ID, COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID };
            Service.ApplicationNodeInformationCmdClasses(Slave, cmdClasses, cmdClasses, cmdClasses);

            ReportService.LogText("Set up security cmd class support at Primary.");
            ApplicationService.AddSecurity(Primary, SecurityVersions.S0 | SecurityVersions.S2);

            ReportService.LogText("Set up automatic registering network keys at zniffer service.");
            ApplicationService.SecurityTestSettings.NetworkKeyChanged(Primary, ZnifferService.SetSecurityNetworkKey);

            ReportService.LogText("Set up 2 zniffer watches in parallel.");
            var zt1 = ZnifferService.ExpectData(300 * 1000);
            zt1.SetUpWatch(new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET(), null, false);
            var zt2 = ZnifferService.ExpectData(300 * 1000);
            zt2.SetUpWatch(new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_VERIFY(), null, false);

            ReportService.LogText("Perform inclusion.");
            var addRes = Service.IncludeNode(Primary, Slave);

            ReportService.LogText("Cancel zniffer watches if they still running. Doesn't affect already completed watches.");
            ZnifferService.CancelToken(zt1);
            ZnifferService.CancelToken(zt2);

            ReportService.LogText("Wait for complete zniffer watches.");
            var zr1 = zt1.WaitCompletedSignal();
            var zr2 = zt2.WaitCompletedSignal();

            AssertIsTrue(zr1, $"received at {zr1.Frames.FirstOrDefault()?.CreatedAt:HH:mm:ss.fff}", "missing");
            AssertIsTrue(zr2, $"received at {zr2.Frames.FirstOrDefault()?.CreatedAt:HH:mm:ss.fff}", "missing");

            AssertIsTrue(addRes, "inclusion completed");
        }
    }
}