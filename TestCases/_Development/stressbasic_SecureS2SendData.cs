// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Security S2 Stress Send Data Test case")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    public class stressbasic_SecureS2SendData : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);
            Alias[] ALL = new Alias[] { Primary, Secondary };

            ParallelService.Reset(ALL);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S0);
            ApplicationService.AddSecurity(Secondary, SecurityVersions.S0);

            Service.AddNode(Primary, Secondary);

            ApplicationService.SetSecurityVersionForNode(Primary, Secondary, SecurityVersions.S2);
            ApplicationService.SetSecurityVersionForNode(Secondary, Primary, SecurityVersions.S2);
            
            ApplicationService.AddResponse(Secondary, new COMMAND_CLASS_BASIC.BASIC_REPORT(), new COMMAND_CLASS_BASIC.BASIC_GET());

            var zcToken = ZnifferService.CollectData();
            zcToken.SetUpWatch(null, HeaderTypes.SINGLECAST, false);
            for (int i = 0; i < 255; i++)
            {
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
            }
            var zcRes = zcToken.WaitCompletedSignal();

            int basicGetCounter = 0;
            foreach (var frame in zcRes.Frames)
            {
                var payload = frame.Payload;
                if (payload.Length >= 2)
                {
                    if (payload[0] == 0x20)
                    {
                        if (payload[1] == 0x02)
                        {
                            basicGetCounter++;
                        }
                    }
                }
            }
            AssertIsTrue(basicGetCounter >= 255, 
                string.Format("Basic Get sent {0} times", basicGetCounter),
                string.Format("Basic Get sent {0} times", basicGetCounter));
        }
    }
}