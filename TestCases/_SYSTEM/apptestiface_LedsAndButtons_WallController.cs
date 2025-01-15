// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.WallController)]
    public class apptestiface_LedsAndButtons_WallController : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);

            Service.SetDefault(Primary);
            ApplicationService.AddSupervision(Primary);
            ApplicationService.AddSecurity(Primary, SecurityVersions.S0 | SecurityVersions.S2);
     

            AssertIsTrue(TestInterfaceService.ExcludeNode(Primary, Secondary));
            var includeRes = TestInterfaceService.IncludeNode(Primary, Secondary);
            AssertIsTrue(includeRes);
            AssertIsTrue(includeRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done);

            Delay(16000);
            var watcher = ZnifferService.ExpectData(2000);
            watcher.SetUpWatch(new COMMAND_CLASS_CENTRAL_SCENE_V3.CENTRAL_SCENE_NOTIFICATION());
            TestInterfaceService.ButtonActions.WallController_Key01(Secondary);
            watcher.WaitCompletedSignal();

            Delay(3000);
            watcher = ZnifferService.ExpectData(2000);
            watcher.SetUpWatch(new COMMAND_CLASS_SUPERVISION.SUPERVISION_GET());
            TestInterfaceService.ButtonActions.WallController_Key02(Secondary);

            watcher.WaitCompletedSignal();

            Delay(3000);
            watcher = ZnifferService.ExpectData(2000);
            watcher.SetUpWatch(new COMMAND_CLASS_SUPERVISION.SUPERVISION_GET());
            TestInterfaceService.ButtonActions.WallController_Key03(Secondary);
            watcher.WaitCompletedSignal();
        }
    }
}
