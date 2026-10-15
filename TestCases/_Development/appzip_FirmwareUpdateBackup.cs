// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"FirmwareUpdate EEPROM Backup")]
    [ZipDeviceMapping(1, "z2")]
    public class appzip_FirmwareUpdateBackup : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1, "Primary");
            ReportService.LogTitle("FirmwareUpdate Backup/Restore");

            //Request FIRMWARE UPDATE MD COMMAND CLASS VERSION and wait for report
            COMMAND_CLASS_VERSION_V2.VERSION_COMMAND_CLASS_GET versionCmdClassGetData = new COMMAND_CLASS_VERSION_V2.VERSION_COMMAND_CLASS_GET();
            versionCmdClassGetData.requestedCommandClass = COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.ID;
            COMMAND_CLASS_VERSION_V2.VERSION_COMMAND_CLASS_REPORT versionCmdClassReportData = new COMMAND_CLASS_VERSION_V2.VERSION_COMMAND_CLASS_REPORT();

            byte[] backupData = null;
            var requestVersionRes = ZipService.RequestData(Primary, versionCmdClassGetData, versionCmdClassReportData, 3000);
            if (requestVersionRes == true && requestVersionRes.ReceivedData != null)
            {
                versionCmdClassReportData = requestVersionRes.ReceivedData;
                if (versionCmdClassReportData.commandClassVersion == 0x05)
                {
                    ReportService.LogText("Backup");
                    ZipResult.FirmwareBackup backupRes = ZipService.FirmwareBackupV5(Primary, 0x04, 30000);
                    AssertIsTrue(backupRes.PrepareStatus == 0xFF, "Valid combination");
                    backupData = backupRes.Data;

                    ReportService.LogText("Restore");
                    ZipResult.FirmwareUpdate updateRes = ZipService.FirmwareUpdateV5(Primary, backupData, 0x04, 30000);
                    AssertIsTrue(updateRes.RequestStatus == 0xFF, "Valid combination");
                    AssertIsTrue(updateRes.ReportStatus == 0xFF, "Update completed");
                }
            }
        }
    }
}