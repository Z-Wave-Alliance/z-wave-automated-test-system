// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System.Linq;
using System.Collections.Generic;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Security S0 Network Key Set override")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    public class stressbasic_SetDefault_RemoveNode : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            for (int i = 0; i < 100; i++)
            {
                Service.SetDefault(Primary);
                Service.ApplicationNodeInformation(Primary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER, new byte[] { 0, 32, 0x84 });
                var token = AsyncService.RemoveNode(Primary);
                Delay(200);
                AsyncService.CancelToken(Primary, token);
                var result = token.WaitCompletedSignal();
            }
        }
    }
}