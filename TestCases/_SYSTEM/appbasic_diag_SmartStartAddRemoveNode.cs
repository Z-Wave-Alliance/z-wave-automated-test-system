// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"SetDefault, Smart Start/Remove slave", Tags.SYS, Tags.PCController)]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [MultiDeviceMapping(2, LibTypes.EndDeviceLib)]
    public class appbasic_diag_SmartStartAddRemoveNode : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            AliasGroup Slaves = Context.GetAliasGroup(2);
            ReportService.LogText("Start");

            ParallelService.Reset(Primary);
            ParallelService.SetDefault(Slaves.Aliases);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Primary);

            foreach (var Slave in Slaves)
            {
                if (Context.IsLongRange(Primary))
                {
                    ApplicationService.Provisioning.Add(Primary, Slave, SecuritySchemeSet.ALL, Modes.NodeOptionNetworkWide | Modes.NodeOptionLongRange);
                }
                else
                {
                    ApplicationService.Provisioning.Add(Primary, Slave, SecuritySchemeSet.ALL, Modes.NodeOptionNetworkWide);
                }
                var addToken = AsyncService.ExpectSmartStartAdd(Primary, 300000);
                for (int i = 0; i < 4; i++)
                {
                    var learnRes = Service.SetLearnMode(Slave, LearnModes.LearnModeSmartStart | LearnModes.LearnModeNetworkMask, 50000);
                    if (learnRes)
                    {
                        break;
                    }
                    else
                    {
                        ReportService.LogWarning($"SetLearnMode expired, attempt:{i}");
                    }
                }
                var addRes = addToken.WaitCompletedSignal();
                if (addRes)
                {
                    ReportService.LogText($"Slave added to the network NID: {addRes.AddNodeResult.Node.ToString()}");
                }
                else
                {
                    ReportService.LogWarning($"Unable to add: Slave: {Context.GetDataSource(Slave)}");
                }
            }
        }
    }
}