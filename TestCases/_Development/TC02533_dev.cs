// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("DEV* Security v2 PC Controller - The joining node must store the received network key in non-volatile memory")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    class TC02533_dev : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Controller1 = new Alias(1);
            Alias DUT = new Alias(2);
            Alias[] ALL = new Alias[] { Controller1, DUT };
            ParallelService.Reset(ALL);

            ApplicationService.AddSecurity(Controller1, SecurityVersions.S2);
            ApplicationService.AddSecurity(DUT, SecurityVersions.S2);
            ApplicationService.AddResponse(DUT, new COMMAND_CLASS_VERSION.VERSION_REPORT(), new COMMAND_CLASS_VERSION.VERSION_GET());

            ReportService.LogText("1. Enable all security keys on both controllers, reset both controllers.");
            ReportService.LogText("2.Include DUT to PC Controller 1.");
            ApplicationService.SecurityTestSettings.ClearInclusionFramesS2(Controller1);
            var addRes = Service.AddNode(Controller1, DUT);
            AssertIsTrue(addRes, "In 1. verify that inclusion is successful and S2 bootstrapping is performed, DUT Id = " + addRes.NodeId);
            AssertIsTrue(SubstituteStatuses.Done == addRes.SubstituteStatus);

            ReportService.LogText("3. Send Version Get from PC Controller 1 to DUT");
            var reqRes3 = Service.RequestData(Controller1, DUT, new COMMAND_CLASS_VERSION.VERSION_GET(), new COMMAND_CLASS_VERSION.VERSION_REPORT());
            ReportService.LogText("In 3. Verify that DUT responds with Version Report S2 encrypted using its highest S2 Network key (Access Key)");
            AssertIsTrue(reqRes3, "Version Report");
            AssertIsTrue(reqRes3.RxSecurityScheme == SecuritySchemes.S2_ACCESS, "Access Key");


            ReportService.LogText("4. Power cycle static controller attached to DUT");
            Service.SoftReset(DUT);

            ReportService.LogText("5. Send Version Get from PC Controller 1 to DUT");
            var reqRes5 = Service.RequestData(Controller1, DUT, new COMMAND_CLASS_VERSION.VERSION_GET(), new COMMAND_CLASS_VERSION.VERSION_REPORT());
            ReportService.LogText("In 5. Verify that DUT responds with Version Report S2 encrypted using its highest S2 Network key (Access Key)");
            AssertIsTrue(reqRes5, "Version Report");
            AssertIsTrue(reqRes5.RxSecurityScheme == SecuritySchemes.S2_ACCESS, "Access Key");
        }
    }
}
