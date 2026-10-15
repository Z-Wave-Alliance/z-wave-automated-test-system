// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Verify TO#6118 and TO#6122. FirmwareUpdate")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    public class appbasic_FirmwareUpdateAllDevices : ZatsCodeInteractiveTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1, "Primary");

            ReportService.LogText("Start");
            LibTypes[] devices = new LibTypes[] {
                LibTypes.DoorLockKeyPad, LibTypes.SensorPIR, LibTypes.SwitchOnOff};
            foreach (var deviceType in devices)
            {

                Service.Reset(Primary);
                ApplicationService.AddSecurity(Primary, SecurityVersions.S0);

                #region Show Dialog To User (Manual interaction)
                DialogResult boxEarlyResponse = MessageBox.Show(string.Format("Push button on {0} node to remove it from network.", deviceType),
                           "Firmware Update TC", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk);
                if (boxEarlyResponse == DialogResult.Cancel)
                {
                    Fail("TC Cancelled");
                }
                #endregion
                var remToken = AsyncService.RemoveNode(Primary, Modes.NodeAny, 10000);
                remToken.WaitCompletedSignal();

                #region Show Dialog To User (Manual interaction)
                boxEarlyResponse = MessageBox.Show(string.Format("Push button on {0} node to add it to network.", deviceType),
                           "Firmware Update TC", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk);
                if (boxEarlyResponse == DialogResult.Cancel)
                {
                    Fail("TC Cancelled");
                }
                #endregion
                var addToken = AsyncService.AddNode(Primary, Modes.NodeAny, 20000);
                var addRes = addToken.WaitCompletedSignal();
                AssertIsTrue(addRes, "Node added successfully NodeId=" + addRes.NodeId,
                    "Failed to add node to network");

                ComplianceService.SendData(Primary, addRes.NodeId, new COMMAND_CLASS_ASSOCIATION_V2.ASSOCIATION_SET
                {
                    groupingIdentifier = 0x01,
                    nodeId = new byte[] { (byte)0x01 }
                });

                var assocGetResult = ComplianceService.RequestData(Primary, addRes.NodeId,
                    new COMMAND_CLASS_ASSOCIATION_V2.ASSOCIATION_GET() { groupingIdentifier = 0x01 },
                    new COMMAND_CLASS_ASSOCIATION_V2.ASSOCIATION_REPORT());

                COMMAND_CLASS_ASSOCIATION_V2.ASSOCIATION_REPORT ccReport2 = assocGetResult.Command;
                byte[] nodeIdsPrev = ccReport2.nodeid.ToArray();

                string filePath = ApplicationService.GetFile(deviceType, ChipSeries.Default, Context.Frequency, DevKits.Default);
                //Request FIRMWARE UPDATE MD COMMAND CLASS VERSION and wait for report
                COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_GET versionCmdClassGetData = new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_GET();
                versionCmdClassGetData.requestedCommandClass = COMMAND_CLASS_FIRMWARE_UPDATE_MD_V2.ID;

                COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_REPORT versionCmdClassReportData = new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_REPORT();

                var requestVersionRes = ComplianceService.RequestData(Primary, addRes.NodeId,
                    versionCmdClassGetData,
                    TX_OPTIONS,
                    versionCmdClassReportData, 3000);

                if (requestVersionRes == true && requestVersionRes.Command != null)
                {
                    versionCmdClassReportData = requestVersionRes.Command;
                    if (versionCmdClassReportData.commandClassVersion == 0x02)
                    {

                        var statusByte = ComplianceService.FirmwareUpdateV2(Primary, addRes.NodeId, filePath);
                        AssertIsTrue(statusByte == 0xFF, "Updated!", "Update failed!");
                    }
                    else if (versionCmdClassReportData.commandClassVersion == 0x03)
                    {
                        var statusByte = ComplianceService.FirmwareUpdateV3(Primary, addRes.NodeId, filePath);
                        AssertIsTrue(statusByte == 0xFF, "Updated!", "Update failed!");
                    }
                }

                Delay(5000, "Wait device init");

                assocGetResult = ComplianceService.RequestData(Primary, addRes.NodeId,
                    new COMMAND_CLASS_ASSOCIATION_V2.ASSOCIATION_GET() { groupingIdentifier = 0x01 },
                    new COMMAND_CLASS_ASSOCIATION_V2.ASSOCIATION_REPORT());

                ccReport2 = assocGetResult.Command;
                byte[] nodeIdsNew = ccReport2.nodeid.ToArray();
                AssertIsTrue(nodeIdsNew.SequenceEqual(nodeIdsPrev),
                    "Firmware Update completed correctly. NVM preserved.",
                    "Firmware Update failed - NVM NOT preserved.");
            }
        }
    }
}