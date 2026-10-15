// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Security S2 Send Data Test case")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    public class appbasic_SecureS2SendData : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);
            Alias Slave = new Alias(3);
            Alias[] ALL = new Alias[] { Primary, Secondary, Slave };

            ParallelService.Reset(ALL);

            ApplicationService.AddBasic(Secondary);
            ApplicationService.AddBasicEx(Slave);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            ApplicationService.AddSecurity(Secondary, SecurityVersions.S2);

            Service.AddNode(Primary, Secondary);
            Service.AddNodeWithDSK(Primary, Slave, new byte[] { 0xCF, 0x07 });

            ReportService.LogTitle("Switching keys on sender");

            ReportService.LogText("request using default key");
            var requestRes = Service.RequestData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Assert.IsTrue(requestRes);

            ReportService.LogText("request using S2Class0 key");
            ApplicationService.SecurityTestSettings.SetActiveSecurityScheme(Primary, Secondary, SecuritySchemes.S2_UNAUTHENTICATED);
            requestRes = Service.RequestData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Assert.IsTrue(requestRes);
            requestRes = Service.RequestData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Assert.IsTrue(requestRes);
            Assert.AreEqual(SecuritySchemes.S2_UNAUTHENTICATED, requestRes.RxSecurityScheme);


            ReportService.LogText("request using S2Class1 key");
            ApplicationService.SecurityTestSettings.SetActiveSecurityScheme(Primary, Secondary, SecuritySchemes.S2_AUTHENTICATED);
            requestRes = Service.RequestData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Assert.IsTrue(requestRes);
            Assert.AreEqual(SecuritySchemes.S2_AUTHENTICATED, requestRes.RxSecurityScheme);

            ReportService.LogText("request using S2Class2 key");
            ApplicationService.SecurityTestSettings.SetActiveSecurityScheme(Primary, Secondary, SecuritySchemes.S2_ACCESS);
            requestRes = Service.RequestData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Assert.IsTrue(requestRes);
            Assert.AreEqual(SecuritySchemes.S2_ACCESS, requestRes.RxSecurityScheme);

            ReportService.LogText("change S2Class2 key on sender");
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, new byte[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 }, SecuritySchemes.S2_ACCESS);
            using (Service.Settings.IsExpectingFailure.Set())
            {
                requestRes = Service.RequestData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());
                Assert.IsFalse(requestRes);
            }

            ReportService.LogText("change S2Class2 key on receiver");
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Secondary, new byte[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 }, SecuritySchemes.S2_ACCESS);
            requestRes = Service.RequestData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());
            Assert.IsTrue(requestRes);
            Assert.AreEqual(SecuritySchemes.S2_ACCESS, requestRes.RxSecurityScheme);
        }
    }
}