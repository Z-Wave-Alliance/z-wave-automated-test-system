// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Linq;
using System.Collections.Generic;
using Zats.Services;
using System;

namespace ZatsTests
{
    [ZatsTestCase(@"Verify InclusionControllerInitiateRequestTimeoutMs")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.ControllerStaticLib)]
    public class appbasic_InclusionControllerInitiateRequestTimeoutMs : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias inclusionController = new Alias(1);
            Alias dut = new Alias(2);
            Alias controller3 = new Alias(3);

            ParallelService.Reset(inclusionController, dut, controller3);
            ApplicationService.AddSecurity(dut, SecurityVersions.S2 | SecurityVersions.S0);
            ApplicationService.AddSecurity(controller3, SecurityVersions.S2 | SecurityVersions.S0);
            ApplicationService.AddSecurity(inclusionController, SecurityVersions.S2 | SecurityVersions.S0);
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(dut);

            ReportService.LogText("1. DUT is set as SIS, DUT includes PC Controller (1) to its network");
            Service.SetSUCNodeID(dut, dut);
            var inclRes = Service.IncludeNode(dut, inclusionController);
            AssertIsTrue(inclRes, "DUT added Icnlusion");
            AssertIsTrue(inclRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done, "Securelly");

            var inclusionTimeoutMS = 60 * 1000;
            using (Service.Settings.IsExpectingFailure.Set())
            {
                var et = AsyncService.ExpectData(dut, new COMMAND_CLASS_INCLUSION_CONTROLLER.INITIATE());
                var etF = AsyncService.ExpectData(inclusionController, new COMMAND_CLASS_INCLUSION_CONTROLLER.COMPLETE(), inclusionTimeoutMS);
                ApplicationService.SecurityTestSettings.IncludeSettings.InclusionControllerInitiateRequestTimeoutMs = inclusionTimeoutMS - 10000;
                ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(dut, SecurityS2TestFrames.InclusionComplete1, 200000);
                var dtMem = DateTime.Now;
                inclRes = Service.IncludeNode(inclusionController, controller3, inclusionTimeoutMS);
                var timeSpent = DateTime.Now - dtMem;
                //AssertIsTrue(inclRes, "3rd controller added");
                var er = et.WaitCompletedSignal();
                AssertIsTrue(er, "Dut Received Initate frame");
                Inconclusive(er.SecurityScheme == SecuritySchemes.S2_ACCESS, "S2_ACCESS");
                AssertIsTrue(!etF.WaitCompletedSignal(), "No Comlete during 30000", "Detected Comlete during 30000");
                Inconclusive(timeSpent.TotalMilliseconds < inclusionTimeoutMS, $"{timeSpent.TotalMilliseconds} < {inclusionTimeoutMS} (ms)");
            }

            Service.Reset(controller3);
            Service.RequestNodeInfo(dut, inclusionController);

            using (Service.Settings.IsExpectingFailure.Set())
            {
                var et = AsyncService.ExpectData(dut, new COMMAND_CLASS_INCLUSION_CONTROLLER.INITIATE());
                var etF = AsyncService.ExpectData(inclusionController, new COMMAND_CLASS_INCLUSION_CONTROLLER.COMPLETE(), inclusionTimeoutMS);
                ApplicationService.SecurityTestSettings.IncludeSettings.InclusionControllerInitiateRequestTimeoutMs = null;
                ApplicationService.SecurityTestSettings.SetInclusionFrameDelayS2(dut, SecurityS2TestFrames.InclusionComplete1, 200000);
                var dtMem = DateTime.Now;
                inclRes = Service.IncludeNode(inclusionController, controller3, inclusionTimeoutMS);
                var timeSpent = DateTime.Now - dtMem;
                //AssertIsTrue(inclRes, "3rd controller added");
                var er = et.WaitCompletedSignal();
                AssertIsTrue(er, "Dut Received Initate frame");
                Inconclusive(er.SecurityScheme == SecuritySchemes.S2_ACCESS, "S2_ACCESS");
                AssertIsTrue(!etF.WaitCompletedSignal(), "No Comlete during 30000", "Detected Comlete during 30000");
                Inconclusive(timeSpent.TotalMilliseconds > inclusionTimeoutMS, $"{timeSpent.TotalMilliseconds} > {inclusionTimeoutMS} (ms)");
            }
        }
    }
}