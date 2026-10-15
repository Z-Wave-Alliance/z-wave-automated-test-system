// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Check unsolicited destination")]
    [ZipDeviceMapping(1, "z2")]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    public class zip_dtls_listener : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias ZipPrimary = new Alias(1, "ZipPrimary");
            Alias Secondary = new Alias(2, "Secondary");

            ReportService.LogText("Start");
            
            // Step 1
            Service.SetDefault(Secondary);
            ZipService.SetDefault(ZipPrimary);

            // Step 2
            ZipService.AddNode(ZipPrimary, Secondary);

            // Setup expected data.
            var dataFromNode = new COMMAND_CLASS_BASIC.BASIC_REPORT();
            ZnifferToken.ExpectData zt = ZnifferService.ExpectData(5000);
            zt.SetUpWatch(dataFromNode, HeaderTypes.SINGLECAST, false);
            var expectToken = ZipAsyncService.ExpectData(Secondary, dataFromNode, 10000);

            // Step 3
            ComplianceService.SendData(Secondary, 0x01, dataFromNode);

            // Step 4
            if (zt.WaitCompletedSignal())
            {
                ReportService.LogOk("Node successfully sent BASIC_REPORT to Z/IP Gateway");
            }
            else
            {
                Inconclusive("BASIC_REPORT sending to Z/IP Gateway failed");
            }

            // Step 5
            AssertIsTrue(expectToken.WaitCompletedSignal());
            Delay(1 * 2000);
            ZipService.RemoveNode(ZipPrimary, Secondary);
        }
    }
}