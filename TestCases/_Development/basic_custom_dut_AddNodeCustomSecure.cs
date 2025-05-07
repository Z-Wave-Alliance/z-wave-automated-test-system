// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"existing network with DUT
    -SendData 
    -Stress test")]
    [DeviceMapping(1, "moxa5", LibTypes.ControllerStaticLib)]
    public class basic_custom_dut_AddNodeCustomSecure : ZatsCodeInteractiveTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            ReportService.LogText("Start");

            //ApplicationService.AddSecurity(Primary, SecurityVersions.S0);
            //ApplicationService.SetSecurityTestNetworkKey(Primary, NetworkKey);

            ReportService.LogText("Removing Node");
            ManualService.RemoveNode(Primary, 60000);

            ReportService.LogText("Adding Node");
            byte nodeId = ManualService.AddNode(Primary, 60000).NodeId;


            BasicResult.RequestData resScheme = ComplianceService.RequestData(Primary, nodeId, new COMMAND_CLASS_SECURITY.SECURITY_SCHEME_GET() { supportedSecuritySchemes = 0x00 }, new COMMAND_CLASS_SECURITY.SECURITY_SCHEME_REPORT(), 10000);
            AssertIsTrue(resScheme, "SchemeReport State");

            ApplicationService.AddSecurity(Primary, SecurityVersions.S0, new byte[] { 0x20, 0x98 }); //default security key  = 01 01 01 01 ...
            ApplicationService.SetSecurityTestNetworkKey(Primary, "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00".ToByteArray());

            byte[] invalidNetworkKeySetCommand = "98 06 02 02 02 02 02 02 02 02 02 02 02 02 02 02 02 02 AA".ToByteArray();
            ComplianceService.SendData(Primary, nodeId, invalidNetworkKeySetCommand);
            ApplicationService.SetSecurityTestNetworkKey(Primary, "02 02 02 02 02 02 02 02 02 02 02 02 02 02 02 02".ToByteArray());
            using (Service.Settings.IsExpectingFailure.Set())
            {
                BasicResult.RequestData resKey = ComplianceService.RequestData(Primary, nodeId, null, new COMMAND_CLASS_SECURITY.NETWORK_KEY_VERIFY(), 10000);
                AssertIsTrue(!resKey, "NetworkKeyVerify State");
            }
        }
    }
}