// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"FirmwareUpdate")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, "dd1", LibTypes.EndDeviceLib)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class appbasic_FirmwareUpdate : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1, "Primary");
            Alias Slave = new Alias(2);
            ReportService.LogText("Start");
            Service.Reset(Primary);
            Service.Reset(Slave);

            //ApplicationService.AddSecurity(Primary, SecurityVersions.S0 | SecurityVersions.S2);
            //ApplicationService.AddTransportService(Primary, 40);

            Service.ApplicationNodeInformation(Slave, true, (byte)GenericTypes.SWITCH_BINARY,
                (byte)SpecificTypes.NOT_USED,
                new byte[] 
                { 
                    ZWAVE_CMD_CLASS.ID,
                    COMMAND_CLASS_SWITCH_BINARY.ID,
                    COMMAND_CLASS_SECURITY.ID,
                    COMMAND_CLASS_SECURITY_2.ID,
                    COMMAND_CLASS_VERSION.ID,
                    COMMAND_CLASS_FIRMWARE_UPDATE_MD.ID,
                    COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID,
                    COMMAND_CLASS_ASSOCIATION.ID
                });
            ApplicationService.FirmwareUpdateMD.EnableSupport(Slave);
            ApplicationService.AddResponse(Slave,
                    new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_REPORT() { requestedCommandClass = COMMAND_CLASS_FIRMWARE_UPDATE_MD.ID, commandClassVersion = 0x05 },
                    new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_GET() { requestedCommandClass = COMMAND_CLASS_FIRMWARE_UPDATE_MD.ID });

            // change dsk integer if required
            ApplicationService.SecurityTestSettings.IncludeSettings.JoinDSK = 56990.ToByteArray(2);
            var addRes = Service.IncludeNode(Primary, Slave);

            // change path to ota hex file
            string filePath = @"D:\ToTest\serialapi_slave_enhanced_232_OTW_ZW050x_JP.otz";
            AssertIsTrue(filePath != null, "FilePath:>" + filePath, "File for FWUpdate not found");

            //Request FIRMWARE UPDATE MD COMMAND CLASS VERSION and wait for report
            COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_GET versionCmdClassGetData = new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_GET();
            versionCmdClassGetData.requestedCommandClass = COMMAND_CLASS_FIRMWARE_UPDATE_MD_V2.ID;
            COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_REPORT versionCmdClassReportData = new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_REPORT();
            var requestVersionRes = ComplianceService.RequestData(Primary, addRes.AddNodeResult.Node,
                versionCmdClassGetData,
                TX_OPTIONS,
                versionCmdClassReportData, 3000);

            if (requestVersionRes == true && requestVersionRes.Command != null)
            {
                versionCmdClassReportData = requestVersionRes.Command;
                if (versionCmdClassReportData.commandClassVersion == 0x02)
                {
                    var statusByte = ComplianceService.FirmwareUpdateV2(Primary, addRes.AddNodeResult.Node, filePath);
                    AssertIsTrue(statusByte == 0xFF, "Updated!", "Update failed!");
                }
                else if (versionCmdClassReportData.commandClassVersion == 0x03)
                {
                    var statusByte = ComplianceService.FirmwareUpdateV3(Primary, addRes.AddNodeResult.Node, filePath);
                    AssertIsTrue(statusByte == 0xFF, "Updated!", "Update failed!");
                }
                else if (versionCmdClassReportData.commandClassVersion == 0x04)
                {
                    var statusByte = ComplianceService.FirmwareUpdateV5(Primary, addRes.AddNodeResult.Node, filePath);
                    AssertIsTrue(statusByte == 0xFF, "Updated!", "Update failed!");
                }
                else if (versionCmdClassReportData.commandClassVersion == 0x05)
                {
                    var statusByte = ComplianceService.FirmwareUpdateV5(Primary, addRes.AddNodeResult.Node, filePath);
                    AssertIsTrue(statusByte == 0xFF, "Updated!", "Update failed!");
                }
            }
            Service.SoftReset(Slave);
            Delay(15000, "Device init");
            string curVersion = Service.GetVersion(Slave).Version;
            ReportService.LogText("Version after update: " + curVersion);
            
            
            Service.Reset(Slave);
        }
    }
}