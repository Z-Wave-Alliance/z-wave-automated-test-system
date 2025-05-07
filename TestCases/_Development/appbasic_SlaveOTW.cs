// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"FirmwareUpdate OTW - SlaveEnhancedLib")]
    [DeviceMapping(1, "m15", LibTypes.EndDeviceLib)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class appbasic_SlaveOTW : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Slave = new Alias(1);
            ReportService.LogText("Start");
            Service.Reset(Slave);
            var versionRes = Service.GetVersion(Slave);

            string[] filePaths = new[]
            {
                @"D:\_TASKS\_OTU\serialapi_slave_enhanced_232_OTU_ZW050x_JP.ota", // file from 6.61
                @"D:\_TASKS\_OTU\serialapi_slave_enhanced_232_OTW_ZW050x_JP.otz"  // file from 6.70 or 6.80
            };
            foreach (var filePath in filePaths)
            {
                var initRes = Service.FirmwareUpdateNvmInit(Slave);
                Assert.IsTrue(initRes);
                Assert.IsTrue(initRes.IsSupported);

                var clearRes = Service.FirmwareUpdateNvmSetNewImage(Slave, false);
                Assert.IsTrue(clearRes);

                var getBeforeRes = Service.FirmwareUpdateNvmGetNewImage(Slave);
                Assert.IsTrue(getBeforeRes);
                Assert.IsFalse(getBeforeRes.IsNewImage);

                var writeRes = Service.FirmwareUpdateNvmWrite(Slave, 0x80, filePath);
                Assert.IsTrue(writeRes);
                Assert.IsTrue(writeRes.IsOk);

                var crcRes = Service.FirmwareUpdateNvmIsValidCrc16(Slave);
                Assert.IsTrue(crcRes);
                ReportService.LogText("CRC16 valid:" + crcRes.IsValid);
                ReportService.LogText("CRC16 value:" + crcRes.Crc16 + " (" + crcRes.Crc16.ToByteArray().ToHexString() + ")");

                var setRes = Service.FirmwareUpdateNvmSetNewImage(Slave, true);
                Assert.IsTrue(setRes);
                Assert.IsTrue(setRes.IsSet);

                var getAfterRes = Service.FirmwareUpdateNvmGetNewImage(Slave);
                Assert.IsTrue(getAfterRes);
                Assert.IsTrue(getAfterRes.IsNewImage);

                Service.SoftReset(Slave);

                ReportService.LogText("Wait for device reboot");
                if (filePath.EndsWith("otz"))
                {
                    Delay(40000);
                }
                else
                {
                    Delay(10000);
                }
                ReportService.LogText("Version before update: " + versionRes.Version);
                versionRes = Service.GetVersion(Slave);
                ReportService.LogText("Version after update: " + versionRes.Version);
            }
        }
    }
}