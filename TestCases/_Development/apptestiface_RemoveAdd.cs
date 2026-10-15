// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
﻿using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("test remove/add")]
    [DeviceMapping(1, "m1", LibTypes.ControllerStaticLib)]
    [TestInterfaceDeviceMapping(2, "TI3")]
    public class apptestiface_RemoveAdd : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Controller = new Alias(1);
            Alias Slave = new Alias(2);

            ReportService.LogText("Reset controller");
            Service.Reset(Controller);
            ApplicationService.AddSecurity(Controller, SecurityVersions.S2 | SecurityVersions.S0);

            ReportService.LogText("Remove node");
            var removeToken = AsyncService.RemoveNode(Controller);
            TestInterfaceService.SetLearnMode(Slave);
            var removeRes = removeToken.WaitCompletedSignal();

            ReportService.LogText("Add node");
            var addToken = AsyncService.AddNode(Controller);
            TestInterfaceService.SetLearnMode(Slave);
            var addRes = addToken.WaitCompletedSignal();
        }
    }
}