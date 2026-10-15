// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"SetDefault, Add/Remove controller, Add/Remove slave", Tags.SYS, Tags.PCController)]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [MultiDeviceMapping(3, LibTypes.EndDeviceLib, ChipSeries = ChipSeries.ZW070x)]
    [MultiDeviceMapping(4, LibTypes.SwitchOnOff, ChipSeries = ChipSeries.ZW070x)]
    public class appbasic_diag_AddRemoveNode : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);
            AliasGroup Slaves = Context.GetAliasGroup(3);
            ReportService.LogText("Start");

            ParallelService.SetDefault(Primary, Secondary);
            ParallelService.SetDefault(Slaves.Aliases);

            Service.SetRfRegion(Primary, RfRegions.US);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            ApplicationService.AddSecurity(Secondary, SecurityVersions.S2);

            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Primary);

            var addRes = Service.AddNode(Primary, Secondary);
            AssertIsTrue(addRes, "Controller added to the network NID:" + addRes.Node.ToString(), "Unable to add: Controller");

            foreach (var Slave in Slaves)
            {
                addRes = Service.AddNode(Primary, Slave);
                AssertIsTrue(addRes, "Slave added to the network NID:" + addRes.Node.ToString(), "Unable to add: Slave");
            }
            var remRes = Service.RemoveNode(Primary, Secondary);
            AssertIsTrue(remRes, "Controller removed from the network NID:" + remRes.NodeId.ToString(), "Unable to remove: Controller");
            foreach (var Slave in Slaves)
            {
                remRes = Service.RemoveNode(Primary, Slave);
                AssertIsTrue(remRes, "Slave removed from the network NID:" + remRes.NodeId.ToString(), "Unable to remove: Slave");
            }
        }
    }
}