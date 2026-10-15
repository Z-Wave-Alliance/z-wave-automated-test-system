// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;
using System;

namespace ZatsTests
{
    [ZatsTestCase(@"FirmwareUpdate MD Support test:
Slave Enhanced Library Update: 6.51 -> 6.61 -> 6.80
")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, "dd1", LibTypes.EndDeviceLib)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class appbasic_FirmwareUpdate_RiseUpVersion : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1, "Primary");
            Alias Slave = new Alias(2);
            ReportService.LogText("Start");
            Service.Reset(Primary);
            Service.Reset(Slave);
            
            Service.ApplicationNodeInformation(Slave, true, (byte)GenericTypes.SWITCH_BINARY,
                (byte)SpecificTypes.NOT_USED,
                new byte[] 
                { 
                    ZWAVE_CMD_CLASS.ID,
                    COMMAND_CLASS_SWITCH_BINARY.ID,
                    COMMAND_CLASS_VERSION.ID,
                    COMMAND_CLASS_FIRMWARE_UPDATE_MD.ID,
                    COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID
                });
            ApplicationService.FirmwareUpdateMD.EnableSupport(Slave);
            ApplicationService.AddResponse(Slave,
                new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_REPORT() { requestedCommandClass = COMMAND_CLASS_FIRMWARE_UPDATE_MD.ID, commandClassVersion = 0x05 },
                new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_GET() { requestedCommandClass = COMMAND_CLASS_FIRMWARE_UPDATE_MD.ID });
            ReportService.LogText(" --  Include Slave   --");
            var addRes = Service.IncludeNode(Primary, Slave);

            string filePath_6_61 = @"H:\SW\DevKit\6_61_01\TR19\dk_otu\SDK_v6_61_01\ProductPlus\Bin\SerialAPI_Slave_Enhanced_232\serialapi_slave_enhanced_232_OTU_ZW050x_ANZ.ota";
            AssertIsTrue(filePath_6_61 != null, "FilePath to 6.61:>" + filePath_6_61, "File for FWUpdate not found");
            string filePath_6_80 = @"H:\SW\DevKit\6_80_00\TR10_B76\dkt\SDK_v6_80_00_TR10_B76\ProductPlus\Bin\SerialAPI_Slave_Enhanced_232\serialapi_slave_enhanced_232_OTW_ZW050x_ANZ.otz";
            AssertIsTrue(filePath_6_80 != null, "FilePath to 6.80:>" + filePath_6_80, "File for FWUpdate not found");
            string[] paths = new string[] { filePath_6_61, filePath_6_80 };
            foreach (var item in paths)
            {
                string beforeUpdate = Service.GetVersion(Slave).Version;
                
                ReportService.LogTitle("Firmware update to " + item);
                ReportService.LogText("1. Get Supported version of Firmwareupdate MD");
                COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_GET versionCmdClassGetData = new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_GET();
                versionCmdClassGetData.requestedCommandClass = COMMAND_CLASS_FIRMWARE_UPDATE_MD_V2.ID;
                COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_REPORT versionCmdClassReportData = new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_REPORT();
                var requestVersionRes = ComplianceService.RequestData(Primary, addRes.AddNodeResult.Node,
                    versionCmdClassGetData,
                    TX_OPTIONS,
                    versionCmdClassReportData, 3000);

                AssertIsTrue(requestVersionRes && requestVersionRes.Command != null, "Command Class Version report detect");
                versionCmdClassReportData = requestVersionRes.Command;
                AssertIsTrue(versionCmdClassReportData.commandClassVersion == 0x05, "Firmware Update version = 5");

                ReportService.LogText("2. Start Firmware Update MD v5");
                var statusByte = ComplianceService.FirmwareUpdateV5(Primary, addRes.AddNodeResult.Node, item);
                AssertIsTrue(statusByte == 0xFF, "Updated!", "Update failed!");

                //Check Version
                ReportService.LogText("3. Soft Reset Slave");
                Service.SoftReset(Slave);
                Delay(41000, "3. Soft Reset Slave");
                
                string afterUpdate = Service.GetVersion(Slave).Version;
                ReportService.LogText("Version before update " + beforeUpdate);
                ReportService.LogText("Version after update " + afterUpdate);
                AssertIsTrue(beforeUpdate != afterUpdate, "Slave Library Version changes", "Slave Library Version not changes");
            }

        }
    }
}