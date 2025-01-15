// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Security S2 Request Data Test case")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    public class appbasic_SecureS2RequestData : ZatsCodeTestCase
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

            var zcToken = ZnifferService.CollectData(5 * 1000 * 60);
            zcToken.SetUpWatch(null, HeaderTypes.SINGLECAST, false);
            var reqResult = Service.RequestData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());
            AssertIsTrue(reqResult, "Basic Report received on Primary",
                "Basic Report was not received on Primary");

            ReportService.LogText("Duplicate request data after nodes being Synced");
            reqResult = Service.RequestData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());
            AssertIsTrue(reqResult, "Basic Report received on Primary",
                "Basic Report was not received on Primary");
            var zcRes = zcToken.WaitCompletedSignal();
        }
    }
}