// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Create network
    -routing 
    -NWE")]
    [DeviceMapping(1, "m1", LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, "m2", LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, "m3", LibTypes.ControllerStaticLib)]
    public class basic_routing_NWE : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1, "Primary");
            Alias Repeater = new Alias(2, "Repeater");
            Alias Secondary = new Alias(3, "Secondary");

            ReportService.LogText("Start");
            ParallelService.SetDefault(Primary, Secondary, Repeater);

            Service.AddNode(Primary, Repeater);

            BasicResult.AddNode addResult = Service.AddNode(Primary, Secondary, Modes.NodeAny | Modes.NodeOptionNetworkWide | Modes.NodeOptionHighPower, LearnModes.LearnModeNWI, 10000);

            if (addResult)
            {
                Service.RequestNodeInfo(Primary, Secondary);

                ReportService.LogText("Check if Routing exists....");
                Delay(5000);

                BasicResult.RemoveNode removeResult = Service.RemoveNode(Primary, Secondary,
                    Modes.NodeAny | Modes.NodeOptionNetworkWide | Modes.NodeOptionHighPower, LearnModes.LearnModeNWE, 10000);
            }
        }
    }
}