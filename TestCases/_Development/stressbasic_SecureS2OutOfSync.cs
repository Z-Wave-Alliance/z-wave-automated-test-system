// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Security S2 Stress Out Of Sync Request Data Test case")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    public class stressbasic_SecureS2OutOfSync : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);
            Alias[] ALL = new Alias[] { Primary, Secondary };

            ParallelService.Reset(ALL);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S0);
            ApplicationService.AddSecurity(Secondary, SecurityVersions.S0);
            var zcToken = ZnifferService.CollectData();
            zcToken.SetUpWatch(null, HeaderTypes.SINGLECAST, false);
            Service.AddNode(Primary, Secondary);

            ApplicationService.SetSecurityVersionForNode(Primary, Secondary, SecurityVersions.S2);
            ApplicationService.SetSecurityVersionForNode(Secondary, Primary, SecurityVersions.S2);

            ApplicationService.AddResponse(Secondary, new COMMAND_CLASS_BASIC.BASIC_REPORT(), new COMMAND_CLASS_BASIC.BASIC_GET());

            var expectToken = AsyncService.ExpectData(Secondary, new COMMAND_CLASS_BASIC.BASIC_GET(), 10000);
            
            Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());

            AssertIsTrue(expectToken.WaitCompletedSignal(), "Basic Set received on Secondary",
                "Basic Set was note received on Secondary");

            Service.SetRFReceiveMode(Secondary, false);
            for (int i = 0; i < 5; i++)
            {
                Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
            }
            Service.SetRFReceiveMode(Secondary, true);
            Service.RequestData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET(), 
                new COMMAND_CLASS_BASIC.BASIC_REPORT());
            var zcRes = zcToken.WaitCompletedSignal();

            int basicGetCounter = 0;
            int basicReportCounter = 0;
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
                        else if (payload[1] == 0x03)
                        {
                            basicReportCounter++;
                        }
                    }
                }
            }
            AssertIsTrue(basicReportCounter >= 1, "Report was sent after sync.",
                "Report was not sent after sync.");
        }
    }
}