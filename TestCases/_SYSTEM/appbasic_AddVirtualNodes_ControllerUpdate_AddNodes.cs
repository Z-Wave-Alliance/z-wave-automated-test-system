// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"",
                      Tags.SYS,
                      Tags.Brief,
                      Tags.PCController)]
    [DeviceMapping(1, LibTypes.ControllerBridgeLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class appbasic_AddVirtualNodes_ControllerUpdate_AddNodes : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);
            Alias Slave = new Alias(3);
            Alias[] ALL = new Alias[] { Primary, Secondary, Slave };

            ReportService.LogText("Start");

            ParallelService.SetDefault(ALL);

            ReportService.LogTitle($"Make SIS and add Inclusion controller");
            Service.SetSUCNodeID(Primary, Primary);
            Service.AddNode(Primary, Secondary);

            int virtualNetworkSize = 50;
            ReportService.LogTitle($"Create big network with {virtualNetworkSize} virtual nodes");
            Service.ApplicationVirtualDeviceNodeInformation(Primary, true, (byte)GenericTypes.SWITCH_MULTILEVEL, 0x00, new byte[] { 0x20 });
            for (int i = 0; i < virtualNetworkSize; i++)
            {
                var learnRes = Service.SetVirtualDeviceLearnMode(Primary, 0x00, VirtualDeviceLearnModes.Add);
            }

            ReportService.LogTitle($"Update Inclusion controller");
            Service.RequestNetworkUpdate(Secondary);

            int extraNetworkSize = 29;
            ReportService.LogTitle($"Create extra network with add/remove {extraNetworkSize} nodes");
            for (int i = 0; i < extraNetworkSize; i++)
            {
                Service.AddNode(Secondary, Slave);
                Service.SetDefault(Slave);
            }
        }
    }
}