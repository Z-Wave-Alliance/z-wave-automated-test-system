// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"OTA FW Update for Door Lock in S2 Network over 5 hops")]
    [DeviceMapping(1, LibTypes.Controller, Hop = "A")]
    [DeviceMapping(2, LibTypes.EndDeviceAny)]
    [DeviceMapping(3, LibTypes.EndDeviceAny)]
    [DeviceMapping(4, LibTypes.EndDeviceAny)]
    [DeviceMapping(5, LibTypes.EndDeviceAny)]
    [TestInterfaceDeviceMapping(6, LibTypes.DoorLockKeyPad)]
    [MeshNetworkRouterMapping(7, "r1")]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class OTA_Routed : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Rep1 = new Alias(2);
            Alias Rep2 = new Alias(3);
            Alias Rep3 = new Alias(4);
            Alias Rep4 = new Alias(5);
            Alias DUT = new Alias(6);
            Alias Router = new Alias(7);

            ParallelService.SetDefault(Primary, Rep1, Rep2, Rep3, Rep4);

            ReportService.LogTitle("Enable all channels");
            NetworkRouterService.SetChannels(Router, DbLevels.db0);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Primary);
            ApplicationService.SetApplicationCmd(Rep1, false, true, LibTypes.SwitchOnOff);
            ApplicationService.SetApplicationCmd(Rep2, false, true, LibTypes.SwitchOnOff);
            ApplicationService.SetApplicationCmd(Rep3, false, true, LibTypes.SwitchOnOff);
            ApplicationService.SetApplicationCmd(Rep4, false, true, LibTypes.SwitchOnOff);


            ushort fragmentS2_2Ch = 28;
            ushort fragmentS2_3Ch = 132;
            ushort fragmentS2_LR = 128;

            byte[] freq3Ch = new byte[] { 10, 14, 15, 16, 17, 18, 19, 24, 28 };

            // Service.SetDefault(Primary);
            Service.Reset(Primary);
            ApplicationService.AddSecurity(Primary, SecurityVersions.S2 | SecurityVersions.S0);
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Primary);

            var fromFilePath = ApplicationService.GetFile(DUT, DevKits.Default, ".hex");
            var toFilePath01 = ApplicationService.GetFile(DUT, DevKits.Default, ".gbl");
            var toFilePath02 = ApplicationService.GetFile(DUT, "_v255", DevKits.Default, ".gbl");

            AssertIsTrue(fromFilePath != null, $"File for FWUpdate from: {fromFilePath}", "File for FWUpdate _from_ not found");
            AssertIsTrue(toFilePath01 != null, $"File for FWUpdate to: {toFilePath01}", "File for FWUpdate _to_ not found");
            AssertIsTrue(toFilePath02 != null, $"File for FWUpdate to: {toFilePath02}", "File for FWUpdate _to_ not found");

            AssertIsTrue(toFilePath01 != toFilePath02);

            ProgrammerService.ProgramOTABootloader(DUT);
            ProgrammerService.ProgramSample(DUT, fromFilePath);

            ReportService.LogTitle("Excluding DUT");
            var tiExcludeRes = TestInterfaceService.ExcludeNode(Primary, DUT);
            AssertIsTrue(tiExcludeRes, "The DUT is excluded", "Exclusion of the DUT failed");

            ReportService.LogTitle("Including Repeaters");
            var rep1InclRes = Service.IncludeNode(Primary, Rep1);
            AssertIsTrue(rep1InclRes, "Successful inclusion of Repeater 1", "Inclusion of Repeater 1 failed");
            var rep2InclRes = Service.IncludeNode(Primary, Rep2);
            AssertIsTrue(rep2InclRes, "Successful inclusion of Repeater 2", "Inclusion of Repeater 2 failed");
            var rep3InclRes = Service.IncludeNode(Primary, Rep3);
            AssertIsTrue(rep2InclRes, "Successful inclusion of Repeater 3", "Inclusion of Repeater 3 failed");
            var rep4InclRes = Service.IncludeNode(Primary, Rep4);
            AssertIsTrue(rep2InclRes, "Successful inclusion of Repeater 4", "Inclusion of Repeater 4 failed");
            var repeaterIds = new byte[] { rep1InclRes.AddNodeResult.NodeId, rep2InclRes.AddNodeResult.NodeId, rep3InclRes.AddNodeResult.NodeId, rep4InclRes.AddNodeResult.NodeId };


            ReportService.LogTitle("Building route between controller and the DUT");
            NetworkRouterService.SetChannels(Router, DbLevels.db95);
            NetworkRouterService.SetChannel(Router, Primary, Rep1, DbLevels.db25);
            NetworkRouterService.SetChannel(Router, Rep1, Rep2, DbLevels.db25);
            NetworkRouterService.SetChannel(Router, Rep2, Rep3, DbLevels.db25);
            NetworkRouterService.SetChannel(Router, Rep3, Rep4, DbLevels.db25);
            NetworkRouterService.SetChannel(Router, Rep4, DUT, DbLevels.db25);

            ReportService.LogTitle("Including the DUT via NWI");
            var routInclT = ZnifferService.ExpectData(30000).SetUpWatch(new COMMAND_CLASS_SECURITY_2.KEX_GET(), HeaderTypes.ROUTED_SINGLECAST, false);

            ApplicationService.SecurityTestSettings.IncludeSettings.TimeoutMs = 50000;
            ApplicationService.SecurityTestSettings.IncludeSettings.IncludeMode = Modes.NodeOptionNetworkWide | Modes.NodeAny;
            var NWIres = TestInterfaceService.IncludeNode(Primary, DUT);
            var nodeID = NWIres.AddNodeResult.Node;
            var routInclR = routInclT.WaitCompletedSignal();
            AssertIsTrue(NWIres.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done, "Successful S2 inclusion of the DUT", "S2 inclusion of the DUT failed");
            AssertIsTrue(routInclR, "Inclusion was performed via repeaters", "Inclusion was performed in direct range");
            AssertIsTrue(routInclR.Frames[0].Repeaters.SequenceEqual(repeaterIds), "All repeaters were used during NWI");

            Delay(10000);


            foreach (var toFilePath in new[] { toFilePath02 })
            {

                var rpt0 = new COMMAND_CLASS_VERSION_V3.VERSION_ZWAVE_SOFTWARE_REPORT();
                var verRes0 = ComplianceService.RequestData(Primary, nodeID, new COMMAND_CLASS_VERSION_V3.VERSION_ZWAVE_SOFTWARE_GET(), rpt0);
                AssertIsTrue(verRes0 && verRes0.Command != null);
                rpt0 = verRes0.Command;

                ReportService.LogText("Version before update ");
                var beforeUpdate = ApplicationService.ZWaveClasses.GetFullDescription(rpt0);
                ReportService.LogTitle(beforeUpdate);

                ReportService.LogTitle("Firmware update to ");
                ReportService.LogText("1. Get Supported version of Firmwareupdate MD");
                COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_GET versionCmdClassGetData = new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_GET();
                versionCmdClassGetData.requestedCommandClass = COMMAND_CLASS_FIRMWARE_UPDATE_MD_V2.ID;
                COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_REPORT versionCmdClassReportData = new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_REPORT();
                var requestVersionRes = ComplianceService.RequestData(Primary, nodeID,
                    versionCmdClassGetData,
                    TX_OPTIONS,
                    versionCmdClassReportData, 3000);

                AssertIsTrue(requestVersionRes && requestVersionRes.Command != null, "Command Class Version report detect");
                versionCmdClassReportData = requestVersionRes.Command;
                AssertIsTrue(versionCmdClassReportData.commandClassVersion == 0x05, "Firmware Update version = 5");

                ReportService.LogText("2. Start Firmware Update MD v5");

                //var mdReportT = ZnifferService.ExpectData(60000);
                //var updMDReportT = ZnifferService.ExpectData(60000);
                //var updMDStatReportT = ZnifferService.ExpectData(80000);
                //mdReportT.SetUpWatch(new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_REPORT());
                //updMDReportT.SetUpWatch(new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REPORT());
                //updMDStatReportT.SetUpWatch(new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_STATUS_REPORT());
                System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();

                var statusByte = ComplianceService.FirmwareUpdateV5(Primary, nodeID, toFilePath);

                stopwatch.Stop();
                ReportService.LogText($"OTA firmware update takes {stopwatch.Elapsed.Hours} h, {stopwatch.Elapsed.Minutes} m and {stopwatch.Elapsed.Seconds} s");
                if (stopwatch.Elapsed.Hours * 60 + stopwatch.Elapsed.Minutes + stopwatch.Elapsed.Seconds / 60 <= 6)
                {
                    ReportService.LogOk("OTA firmware update time does not exceed 5 minutes");
                }
                else
                {
                    ReportService.LogError("OTA firmware update time exceeds 5 minutes");
                }
                //var mdReportR = mdReportT.WaitCompletedSignal();
                //var updMDReportR = updMDReportT.WaitCompletedSignal();
                //var updMDStatReportR = updMDStatReportT.WaitCompletedSignal();
                //Inconclusive(mdReportR, "Firmware MD Report is sent", "No Firmware MD Report is detected");
                //Inconclusive(updMDReportR, "Firmware Update MD Report is sent", "No Firmware Update MD Report is detected");
                //Inconclusive(updMDReportR, "Firmware Update MD Status Report is sent", "No Firmware Update MD Status Report is detected");

                //ushort fragmentMD = ((COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_REPORT)mdReportR.Frames[0].OriginalPayload).maxFragmentSize[0];
                //fragmentMD = (ushort)((fragmentMD << 8) + ((COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_REPORT)mdReportR.Frames[0].OriginalPayload).maxFragmentSize[1]);
                //ushort fragmentUpdMD = (ushort)((COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REPORT)updMDReportR.Frames[0].OriginalPayload).data.Count;

                //ushort fragSize;
                //if (Context.IsLongRange(DUT))
                //{
                //    fragSize = fragmentS2_LR;
                //}
                //else
                //{
                //    if (freq3Ch.Contains(mdReportR.Frames[0].Frequency))
                //    { fragSize = fragmentS2_3Ch; }
                //    else
                //    { fragSize = fragmentS2_2Ch; }
                //}
                //Inconclusive(fragSize == fragmentMD, $"Max Fragment Size of Firmware MD Report complies with the specs {fragSize} byte", $"Max Fragment Size from Firmware MD Report is {fragmentMD} byte while specs state {fragSize} byte");
                //Inconclusive(fragmentMD == fragmentUpdMD, "Payload length complies with Max Fragment Size", $"Payload length is {fragmentUpdMD} byte while Max Fragment Size is {fragmentMD}");

                if (toFilePath == toFilePath01)
                {
                    AssertIsTrue(statusByte == 0x04, "Updated rejected!", "Update not rejected!");
                }
                else if (toFilePath == toFilePath02)
                {
                    AssertIsTrue(statusByte == 0xFF, "Updated!", "Update failed!");
                }

                Delay(10000);
                TestInterfaceService.SoftReset(DUT);
                Delay(10000);


                var rpt1 = new COMMAND_CLASS_VERSION_V3.VERSION_ZWAVE_SOFTWARE_REPORT();
                var verRes1 = ComplianceService.RequestData(Primary, NWIres.AddNodeResult.Node, new COMMAND_CLASS_VERSION_V3.VERSION_ZWAVE_SOFTWARE_GET(), rpt1);
                AssertIsTrue(verRes1 && verRes1.Command != null);
                rpt1 = verRes1.Command;

                ReportService.LogText("Version after update ");
                var afterUpdate = ApplicationService.ZWaveClasses.GetFullDescription(rpt1);
                ReportService.LogTitle(afterUpdate);

                if (toFilePath == toFilePath01)
                {
                    AssertIsTrue(beforeUpdate == afterUpdate, "Sample Version not change", "Sample Version changes");
                }
                else if (toFilePath == toFilePath02)
                {
                    AssertIsTrue(beforeUpdate != afterUpdate, "Sample Version changes", "Sample Version doesn't change");
                }
            }





            //var replyT = AsyncService.ExpectData(Primary, new COMMAND_CLASS_DOOR_LOCK_V3.DOOR_LOCK_CONFIGURATION_REPORT(), 30000);
            //var res = ComplianceService.SendData(Primary, NWIres.AddNodeResult.Node, new COMMAND_CLASS_DOOR_LOCK_V3.DOOR_LOCK_CONFIGURATION_GET());
            //var replyR = replyT.WaitCompletedSignal();
            //ReportService.LogInfo($"Sent DoorLock configuration Get: status:{res.TransmitStatus} route schema:{res.RouteScheme} repeaters:{res.Repeaters.ToHexString()}");
            //AssertIsTrue(res.TransmitStatus == TransmitStatuses.CompleteOk, "Transmission was successful", "Failed transmission from Controller");
            //AssertIsTrue(res.Repeaters.Length != 0, "Routed Communication from Controller to the DUT", "Direct communication from Controller to the DUT");
            //AssertIsTrue(replyR, "DUT replied to the Controller", "No reply from the DUT was received");

            //var NWERes = TestInterfaceService.ExcludeNode(Primary, DUT, Modes.NodeOptionNetworkWide | Modes.NodeAny);
            //AssertIsTrue(NWERes, "NWE of the DUT was successful", "NWE of the DUT failed");

        }
    }
}