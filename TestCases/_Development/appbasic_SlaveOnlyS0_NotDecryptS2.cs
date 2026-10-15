// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"FirmwareUpdate OTW - SlaveEnhancedLib")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class appbasic_SlaveOnlyS0_NotDecryptS2 : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave = new Alias(2);
            ReportService.LogText("Start");
            
            // Reset devices
            Service.Reset(Primary);
            Service.Reset(Slave);

            // Add security CC support
            ApplicationService.AddSecurity(Primary, SecurityVersions.S0 | SecurityVersions.S2);

            // Configure S2 inclusion grant only S0
            ApplicationService.SecurityTestSettings.IncludeSettings.GrantedSchemes = new[] { SecuritySchemes.S0 };

            // Perform Inclusion
            var addRes = Service.AddNode(Primary, Slave);

            AssertIsTrue(addRes.SubstituteStatus == SubstituteStatuses.Done, "Inclusion and S2 bootstraping are successful", "Inclusion and S2 bootstraping failed");

            // Set active S2 scheme on Primary for Slave node
            ApplicationService.SecurityTestSettings.SetActiveSecurityScheme(Primary, addRes.NodeId, SecuritySchemes.S2_ACCESS);

            // Start listen data on Slave
            var listenToken = AsyncService.ListenData(Slave);

            // Send data using active S2 scheme
            Service.SendData(Primary, Slave, new COMMAND_CLASS_ZWAVEPLUS_INFO.ZWAVEPLUS_INFO_GET());

            // Wait for any data on slave for 1 sec
            Delay(1000);

            // Cancel listen data on Slave and get listen results
            AsyncService.CancelToken(Slave, listenToken);
            var listenRes = listenToken.WaitCompletedSignal();

            // Assert that no data received on Slave
            AssertIsTrue(listenRes.ReceivedData.Count == 0, "No data received as expected.");
        }
    }
}