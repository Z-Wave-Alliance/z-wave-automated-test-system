// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Add/Remove test controller",
                      Tags.SYS, Tags.Brief, Tags.PCController)]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.TestController)]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    public class appbasic_AddRemoveNodeTestController : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);
            Alias EndDevice = new Alias(3);
            Alias[] ALL = new Alias[] { Primary, Secondary, EndDevice };

            ReportService.LogText("Start");

            ParallelService.Reset(Primary, Secondary, EndDevice);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            ApplicationService.AddSecurity(Secondary, SecurityVersions.S2);
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Primary);

            ReportService.LogTitle("Include EndDevice");
            ApplicationService.Provisioning.Add(Primary, EndDevice, SecuritySchemeSet.ALL, Modes.NodeOptionNetworkWide | Modes.NodeOptionLongRange);
            var addEndDeviceToken = AsyncService.ExpectSmartStartAdd(Primary, 300000);
            var learnEndDeviceRes = Service.SetLearnMode(EndDevice, LearnModes.LearnModeSmartStart | LearnModes.LearnModeNetworkMask, 50000);
            if (!learnEndDeviceRes)
            {
                ReportService.LogWarning($"SetLearnMode expired");
            }
            var addEndDeviceRes = addEndDeviceToken.WaitCompletedSignal();
            AssertIsTrue(addEndDeviceRes,
                $"EndDevice added to the network NID: {addEndDeviceRes.AddNodeResult.Node.ToString()}",
                $"Unable to add: EndDevice: {Context.GetDataSource(EndDevice)}");

            ReportService.LogTitle("Include TestController");
            ApplicationService.Provisioning.Add(Primary, Secondary, SecuritySchemeSet.ALL, Modes.NodeOptionNetworkWide | Modes.NodeOptionLongRange);
            var addControllerToken = AsyncService.ExpectSmartStartAdd(Primary, 300000);
            var learnControllerRes = Service.SetLearnMode(Secondary, LearnModes.LearnModeSmartStart | LearnModes.LearnModeNetworkMask, 50000);
            if (!learnControllerRes)
            {
                ReportService.LogWarning($"SetLearnMode expired");
            }
            var addControllerRes = addControllerToken.WaitCompletedSignal();
            AssertIsTrue(addControllerRes,
                $"Secondary added to the network NID: {addControllerRes.AddNodeResult.Node.ToString()}",
                $"Unable to add: Secondary: {Context.GetDataSource(Secondary)}");

            ReportService.LogTitle("SendData from Primary to EndDevice");
            AssertIsTrue(Service.SendDataEx(Primary, EndDevice, new COMMAND_CLASS_BASIC.BASIC_SET(), SecuritySchemes.S2_ACCESS));
            ReportService.LogTitle("SendData from Primary to Secondary");
            AssertIsTrue(Service.SendDataEx(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_SET(), SecuritySchemes.S2_ACCESS));
            ReportService.LogTitle("SendData from EndDevice to Primary");
            AssertIsTrue(Service.SendDataEx(EndDevice, Primary, new COMMAND_CLASS_BASIC.BASIC_SET(), SecuritySchemes.S2_ACCESS));
            ReportService.LogTitle("SendData from EndDevice to Secondary");
            AssertIsTrue(Service.SendDataEx(EndDevice, Secondary, new COMMAND_CLASS_BASIC.BASIC_SET(), SecuritySchemes.S2_ACCESS));
            ReportService.LogTitle("SendData from Secondary to Primary");
            AssertIsTrue(Service.SendDataEx(Secondary, Primary, new COMMAND_CLASS_BASIC.BASIC_SET(), SecuritySchemes.S2_ACCESS));
            ReportService.LogTitle("SendData from Secondary to EndDevice");
            AssertIsTrue(Service.SendDataEx(Secondary, EndDevice, new COMMAND_CLASS_BASIC.BASIC_SET(), SecuritySchemes.S2_ACCESS));

            ReportService.LogTitle("Remove EndDevice");
            var removeEndDeviceRes = Service.RemoveNode(Primary, EndDevice, Modes.NodeAny | Modes.NodeOptionNetworkWide, 
                LearnModes.LearnModeNWE | LearnModes.LearnModeNetworkMask, 30000);
            AssertIsTrue(removeEndDeviceRes, "EndDevice successfully removed", "EndDevice failed to remove");

            ReportService.LogTitle("Remove Secondary");
            var removeSecondaryRes = Service.RemoveNode(Primary, Secondary, Modes.NodeAny | Modes.NodeOptionNetworkWide, 
                LearnModes.LearnModeNWE | LearnModes.LearnModeNetworkMask, 30000);
            AssertIsTrue(removeSecondaryRes, "Secondary successfully removed", "Secondary failed to remove");
        }
    }
}