// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Long Range Smart inclusion with enable and disabled long range option")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [TestInterfaceDeviceMapping(2, LibTypes.SwitchOnOff, RfRegion = RfRegions.US_LR)]
    [TestInterfaceDeviceMapping(3, LibTypes.SwitchOnOff)]

    //For test
    //[DeviceMapping(11, ChipSeries.ZW050x, LibTypes.Controller, RfRegion = RfRegions.US)]
    //[DeviceMapping(12, LibTypes.Controller)]
    //[DeviceMapping(13, LibTypes.EndDeviceAny)]
    //[DeviceMapping(14, LibTypes.EndDeviceAny)]
    public class apptestiface_SmartStartLongRange : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Dut1 = new Alias(2);
            Alias Dut2 = new Alias(3);

            //Alias ut1 = new Alias(11);
            //Alias ut2 = new Alias(12);
            //Alias ut3 = new Alias(13);
            //Alias ut4 = new Alias(14);

            var slaves = new[] { Dut1, Dut2/*, ut1, ut2, ut3, ut4*/ };
            ParallelService.Reset(Primary /* , ut1, ut2, ut3, ut4 */);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S0 | SecurityVersions.S2);
            //ApplicationService.AddSecurity(ut1, SecurityVersions.S0 | SecurityVersions.S2);
            //ApplicationService.AddSecurity(ut2, SecurityVersions.S0 | SecurityVersions.S2);
            //ApplicationService.AddSecurity(ut3, SecurityVersions.S0 | SecurityVersions.S2);
            //ApplicationService.AddSecurity(ut4, SecurityVersions.S0 | SecurityVersions.S2);

            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Primary);
            var nkS2Acc = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_ACCESS, false);
            var nkS2AccLr = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_ACCESS, true);
            var nkS2Auth = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_AUTHENTICATED, false);
            var nkS2AuthLr = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_AUTHENTICATED, true);

            AssertIsTrue(!Equals(nkS2Acc, nkS2AccLr), "S2_ACCESS is different from LR");
            AssertIsTrue(!Equals(nkS2Auth, nkS2AuthLr), "S2_AUTHENTICATED is different from LR");

            var nodes = new Node[10];
            int i = 0;
            foreach (var slave in slaves)
            {
                ApplicationService.SecurityTestSettings.IncludeSettings.SkipSmartStartLR = i % 2 != 0;
                if (Context.GetLibrary(slave) == LibTypes.SwitchOnOff)
                {
                    if (!TestInterfaceService.ExcludeNode(Primary, slave))
                    {
                        Inconclusive(TestInterfaceService.ExcludeNode(Primary, slave));
                    }
                    var addRes = TestInterfaceService.IncludeNode(Primary, slave);
                    if (!addRes)
                    {
                        ReportService.LogWarning("Something happend: Retry");
                        addRes = TestInterfaceService.IncludeNode(Primary, slave);
                    }
                    if (addRes.AddNodeResult.Node.Id > 1022)
                    {
                        ReportService.LogWarning("Catched " + addRes.AddNodeResult.Node.Id);
                    }
                    ReportService.LogText("Added = " + addRes.AddNodeResult.Node.Id);
                    nodes[i++] = addRes.AddNodeResult.Node;
                }
                else
                {
                    var addRes = Service.IncludeNode(Primary, slave);
                    if (!addRes)
                    {
                        ReportService.LogWarning("Something happend: Retry");
                        addRes = Service.IncludeNode(Primary, slave);
                    }
                    if (addRes.AddNodeResult.Node.Id > 1022)
                    {
                        ReportService.LogWarning("Catched " + addRes.AddNodeResult.Node.Id);
                    }
                    ReportService.LogText("Added = " + addRes.AddNodeResult.Node.Id);
                    nodes[i++] = addRes.AddNodeResult.Node;
                }
            }

            var expTkn = AsyncService.ExpectData(Primary, new COMMAND_CLASS_VERSION.VERSION_REPORT());
            ComplianceService.SendData(Primary, nodes[0], new COMMAND_CLASS_VERSION.VERSION_GET());
            var resultLR = expTkn.WaitCompletedSignal();
            AssertIsTrue(resultLR, "DUT 1 responsed");
            AssertIsTrue(resultLR.RxSubstituteInfo.SubstituteStatus == SubstituteStatuses.Done, "securelly");

            expTkn = AsyncService.ExpectData(Primary, new COMMAND_CLASS_VERSION.VERSION_REPORT());
            ComplianceService.SendData(Primary, nodes[1], new COMMAND_CLASS_VERSION.VERSION_GET());
            var result = expTkn.WaitCompletedSignal();
            AssertIsTrue(result, "DUT 2 responsed");
            AssertIsTrue(result.RxSubstituteInfo.SubstituteStatus == SubstituteStatuses.Done, "securelly");

            AssertIsTrue(resultLR.SecurityScheme == result.SecurityScheme, "Used the same schemes");

            //Switch netkeys:
            //ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, nkS2Auth, SecuritySchemes.S2_AUTHENTICATED, true);
            //ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, nkS2AuthLr, SecuritySchemes.S2_AUTHENTICATED, false);

            using (Service.Settings.IsExpectingFailure.Set())
            {
                ApplicationService.SecurityTestSettings.SetActiveSecurityScheme(Primary, nodes[0], SecuritySchemes.S2_AUTHENTICATED, false);
                expTkn = AsyncService.ExpectData(Primary, new COMMAND_CLASS_VERSION.VERSION_REPORT());
                ComplianceService.SendData(Primary, nodes[0], new COMMAND_CLASS_VERSION.VERSION_GET());
                resultLR = expTkn.WaitCompletedSignal();
                Inconclusive(!resultLR, "DUT 1 ignores RequestData");

                ApplicationService.SecurityTestSettings.SetActiveSecurityScheme(Primary, nodes[1], SecuritySchemes.S2_AUTHENTICATED, true);
                expTkn = AsyncService.ExpectData(Primary, new COMMAND_CLASS_VERSION.VERSION_REPORT());
                ComplianceService.SendData(Primary, nodes[1], new COMMAND_CLASS_VERSION.VERSION_GET());
                result = expTkn.WaitCompletedSignal();
                Inconclusive(!result, "DUT 2 ignores RequestData");
            }

            foreach (var slave in slaves)
            {
                AssertIsTrue(TestInterfaceService.ExcludeNode(Primary, slave));
            }
        }
    }
}
