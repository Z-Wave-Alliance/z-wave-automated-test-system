// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System.Linq;
using System.Collections.Generic;
using Zats.Services;
using System.Threading;

namespace ZatsTests
{
    [ZatsTestCase(@"appbasic_AddNode_SmartStart")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, ChipSeries.Any, LibTypes.EndDeviceLib)]
    [DeviceMapping(3, ChipSeries.Any, LibTypes.EndDeviceLib, RfRegion = RfRegions.US)]
    [DeviceMapping(4, ChipSeries.ZW050x, LibTypes.EndDeviceLib, RfRegion = RfRegions.US)]
    public class appbasic_AddNode_SmartStart : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave1 = new Alias(2);
            Alias Slave2 = new Alias(3);
            Alias Slave3 = new Alias(4);
            Alias[] ALL = new Alias[] { Primary, Slave1, Slave2, Slave3 };
            var cmdClasses = new[] { COMMAND_CLASS_ALARM.ID, COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID };

            ParallelService.Reset(ALL);
            ApplicationService.AddSecurity(Primary, SecurityVersions.S0 | SecurityVersions.S2);
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Primary);

            ApplicationService.Provisioning.Add(Primary, Slave1, SecuritySchemeSet.ALL);
            ApplicationService.Provisioning.Add(Primary, new[] { Slave2, Slave3 }, SecuritySchemeSet.ALL);

            ApplicationService.Provisioning.Remove(Primary, Slave3);

            // var includeToken = AsyncService.ExpectZW(Slave1, "00 50 ? 00", 60000); // assign complete callback


            //------------------------------------------------------------------------------------------------------------------
            ReportService.LogText("Include slave 1");
            //------------------------------------------------------------------------------------------------------------------
            Service.ApplicationNodeInformationCmdClasses(Slave1, cmdClasses, cmdClasses, cmdClasses);
            if (Context.IsLongRange(Primary) && Context.IsLongRange(Slave1))
            {
                var icnlRes1 = Service.IncludeNode(Primary, Slave1, 60000);
                AssertIsTrue(icnlRes1);
                AssertIsTrue(icnlRes1.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done);
                AssertIsTrue(icnlRes1.AddNodeResult.Node.Id > 255, " Node Id = " + icnlRes1.AddNodeResult.Node);
                AssertIsTrue(icnlRes1.SetLearnMode);
                AssertIsTrue(icnlRes1.SetLearnMode.Node.Id > 255, " Node Id = " + icnlRes1.AddNodeResult.Node);
            }
            else
            {
                var addToken1 = AsyncService.ExpectSmartStartAdd(Primary, 60000);
                var learnToken1 = AsyncService.SetLearnMode(Slave1, LearnModes.LearnModeSmartStart | LearnModes.LearnModeNetworkMask);
                var lrRes = learnToken1.WaitCompletedSignal();
                var addRes1 = addToken1.WaitCompletedSignal();
                AssertIsTrue(addRes1);
                AssertIsTrue(addRes1.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done);
                AssertIsTrue(addRes1.AddNodeResult.Node.Id < 255, " Node Id = " + addRes1.AddNodeResult.Node);
                AssertIsTrue(lrRes);
            }
            var skres = Service.GetSecurityKeys(Slave1);
            AssertIsTrue(skres && skres.SecurityKeysMask > 0);

            var remRes = Service.ExcludeNode(Primary, Slave1);
            if (!remRes) Service.Reset(Slave1);

            ApplicationService.SecurityTestSettings.IncludeSettings.SkipSmartStartLR = true;
            var icnlRes = Service.IncludeNode(Primary, Slave1, 60000);
            AssertIsTrue(icnlRes);
            AssertIsTrue(icnlRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done);
            AssertIsTrue(icnlRes.AddNodeResult.Node.Id < 255);
            AssertIsTrue(icnlRes.SetLearnMode);
            skres = Service.GetSecurityKeys(Slave1);
            AssertIsTrue(skres && skres.SecurityKeysMask > 0);


            //------------------------------------------------------------------------------------------------------------------
            ReportService.LogText("Include slave 2 - security fail");
            //------------------------------------------------------------------------------------------------------------------
            var addToken = AsyncService.ExpectSmartStartAdd(Primary, 60000);
            var lmRes = Service.SetLearnMode(Slave2, LearnModes.LearnModeSmartStart | LearnModes.LearnModeNetworkMask, 50000);
            var addRes = addToken.WaitCompletedSignal();
            AssertIsTrue(addRes);
            AssertIsTrue(lmRes);
            AssertIsTrue(addRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.Failed);
            skres = Service.GetSecurityKeys(Slave2);
            AssertIsTrue(skres && skres.SecurityKeysMask == 0);


            //------------------------------------------------------------------------------------------------------------------
            ReportService.LogText("Exclude slave l");
            //------------------------------------------------------------------------------------------------------------------
            remRes = Service.ExcludeNode(Primary, Slave1);
            AssertIsTrue(remRes);


            //------------------------------------------------------------------------------------------------------------------
            ReportService.LogText("Include slave 3");
            //------------------------------------------------------------------------------------------------------------------
            Service.ApplicationNodeInformationCmdClasses(Slave3, cmdClasses, cmdClasses, cmdClasses);

            addToken = AsyncService.ExpectSmartStartAdd(Primary, 60000);
            ApplicationService.Provisioning.Add(Primary, Slave3, SecuritySchemeSet.ALL);
            lmRes = Service.SetLearnMode(Slave3, LearnModes.LearnModeSmartStart | LearnModes.LearnModeNetworkMask, 50000);
            addRes = addToken.WaitCompletedSignal();
            AssertIsTrue(addRes);
            AssertIsTrue(lmRes);
            AssertIsTrue(addRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done);
            skres = Service.GetSecurityKeys(Slave3);
            AssertIsTrue(skres && skres.SecurityKeysMask > 0);
        }
    }
}