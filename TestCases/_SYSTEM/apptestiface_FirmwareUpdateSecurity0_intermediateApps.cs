// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System.Collections.Generic;
using Zats.Services;
using ZatsTests;

namespace ZatsTests
{   /// <summary>
    /// Use this Interface for S0 updating from an non-S2-compatible SDK (6.5x / 6.6x) through intermediate apps (6.61.01 TR19) to another one that supports S2: ex: 6.61.01 -> intermediate -> 6.81.01.
    /// </summary>
    public class apptestiface_FirmwareUpdateSecurity0_intermediateApps : ZatsCodeTestCase
    {
        public string _targetSDK;
        public string _intermediateSDK;
        public SecuritySchemes _currentScheme;
    
        protected override void ExecuteTest()
        {
            Alias Controller = new Alias(1);
            Alias Slave = new Alias(2);
            LibTypes currentLibrary = TestInterfaceService.GetLibrary(Slave);
            byte Firmware1, subFirmware1, Firmware2 = 0x00, SubFirmware2 = 0x00, Protocol1, SubProtocol1, Protocol2 = 0x00, SubProtocol2 = 0x00;
            ReportService.LogTitle("Curent library to update: " + currentLibrary.ToString() + " target SDK: " + _targetSDK);


            Service.Reset(Controller);
            ApplicationService.AddSecurity(Controller, SecurityVersions.S0 | SecurityVersions.S2);
            ApplicationService.EnableNetworkKey(Controller, SecuritySchemes.S2_ACCESS, false);
            ApplicationService.EnableNetworkKey(Controller, SecuritySchemes.S2_AUTHENTICATED, false);
            ApplicationService.EnableNetworkKey(Controller, SecuritySchemes.S2_UNAUTHENTICATED, false);

            TestInterfaceService.ExcludeNode(Controller, Slave);
            var AddExpRes = TestInterfaceService.IncludeNode(Controller, Slave);
            AssertIsTrue(AddExpRes/*.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done*/,
                /*"S0 */"inclusion successful",
                /*"S0 */"inclusion failed");
            Node DutId = AddExpRes.AddNodeResult.Node;
            //ComplianceService.SetSecurityForNode(Controller, DutId, true);

            var expRepo = AsyncService.ExpectData(Controller, new COMMAND_CLASS_VERSION_V2.VERSION_REPORT(), 3000);
            var sendGet = AsyncService.SendData(Controller, DutId, new COMMAND_CLASS_VERSION_V2.VERSION_GET(), TransmitOptions.Ack);

            COMMAND_CLASS_VERSION_V2.VERSION_REPORT verReport = expRepo.WaitCompletedSignal().Command;
            Firmware1 = verReport.firmware0Version;
            subFirmware1 = verReport.firmware0SubVersion;
            Protocol1 = verReport.zWaveProtocolVersion;
            SubProtocol1 = verReport.zWaveProtocolSubVersion;
            ReportService.LogText("Current version, Protocol: " + Protocol1.ToString() + " " + SubProtocol1.ToString() +
                " Firmware: " + Firmware1.ToString() + " " + subFirmware1.ToString());

            if (_intermediateSDK != null)
            {
                this.ControllerFirmwareUpdateNodeId(Controller, 0x02, _intermediateSDK);
                Delay(60000, "wait a minute for the EEPROM to finish updating the Flash");

                #region Verifying that Slave responds after S0 FWU OTA
                expRepo = AsyncService.ExpectData(Controller, new COMMAND_CLASS_VERSION_V2.VERSION_REPORT(), 3000);
                sendGet = AsyncService.SendData(Controller, DutId, new COMMAND_CLASS_VERSION_V2.VERSION_GET(), TransmitOptions.Ack);

                using (Service.Settings.IsExpectingFailure.Set())
                {
                    verReport = expRepo.WaitCompletedSignal().Command;
                }
                if (verReport != null)
                    ReportService.LogOk("The DUT responds after intermediate OTA");
                else
                    ReportService.LogError("The DUT does not respond after intermediate OTA");

                #endregion
            }

            this.ControllerFirmwareUpdateNodeId(Controller, DutId, _targetSDK);

            Delay(60000, "wait a minute for the EEPROM to finish updating the Flash");
            #region Verifying that Slave responds after S2 FWU OTZ
            expRepo = AsyncService.ExpectData(Controller, new COMMAND_CLASS_VERSION_V2.VERSION_REPORT(), 3000);
            sendGet = AsyncService.SendData(Controller, DutId, new COMMAND_CLASS_VERSION_V2.VERSION_GET(), TransmitOptions.Ack);

            using (Service.Settings.IsExpectingFailure.Set())
            {
                verReport = expRepo.WaitCompletedSignal().Command;
            }
            if (verReport != null)
            {
                Firmware2 = verReport.firmware0Version;
                SubFirmware2 = verReport.firmware0SubVersion;
                Protocol2 = verReport.zWaveProtocolVersion;
                SubProtocol2 = verReport.zWaveProtocolSubVersion;
                ReportService.LogText("After Update version, Protocol: " + Protocol2.ToString() + " " + SubProtocol2.ToString() +
               " Firmware: " + Firmware2.ToString() + " " + SubFirmware2.ToString());
                if (!((Firmware1 == Firmware2) && (subFirmware1 == SubFirmware2)) && !((Protocol1 == Protocol2) && (SubProtocol1 == SubProtocol2)))
                {
                    ReportService.LogOk("The firmware version is different");
                }
                else
                    ReportService.LogError("The firmware version is the same");

            }
            else//If DUT does not respond, it may have reset itself
            {
                Delay(3000);
                var ZExpectData = ZnifferService.ExpectData(5000);
                ZExpectData.SetUpWatch(null, 255, new ZWAVE_CMD_CLASS.NODE_INFO(), HeaderTypes.BROADCAST, false);
                TestInterfaceService.SetLearnMode(Slave);
                var ZExpecVar = ZExpectData.WaitCompletedSignal().Frames;
                //Check in NIF if Node Id is 0 and/or if Home Id is different from the current network
                if (ZExpecVar.Count == 0)
                    ReportService.LogError("No NIF detected, try verifying manually");

                else if (ZExpecVar.Count >= 1)
                {
                    var NifNodeID = ZExpecVar[0].Source;
                    //byte NifHomeID = ZExpecVar[0].HomeId;
                    if (NifNodeID == DutId)
                    {
                        ReportService.LogOk("The DUT retains its node ID, trying once again");

                        expRepo = AsyncService.ExpectData(Controller, new COMMAND_CLASS_VERSION_V2.VERSION_REPORT(), 3000);
                        sendGet = AsyncService.SendDataEx(Controller, DutId, new COMMAND_CLASS_VERSION_V2.VERSION_GET(), TransmitOptions.Ack, TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY, _currentScheme, TransmitOptions2.NONE);

                        using (Service.Settings.IsExpectingFailure.Set())
                        {
                            verReport = expRepo.WaitCompletedSignal().Command;
                        }
                        if (verReport != null)
                            ReportService.LogOk("The DUT reresponded");
                        else
                        {
                            ReportService.LogText("Sending a power cycle and trying once again");
                            TestInterfaceService.SoftReset(Slave);
                            Delay(2000);
                            expRepo = AsyncService.ExpectData(Controller, new COMMAND_CLASS_VERSION_V2.VERSION_REPORT(), 3000);
                            sendGet = AsyncService.SendDataEx(Controller, DutId, new COMMAND_CLASS_VERSION_V2.VERSION_GET(), TransmitOptions.Ack, TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY, _currentScheme, TransmitOptions2.NONE);

                            using (Service.Settings.IsExpectingFailure.Set())
                            {
                                verReport = expRepo.WaitCompletedSignal().Command;
                            }
                            if (verReport != null)
                                ReportService.LogOk("The DUT reresponded");
                            else
                                ReportService.LogError("The DUT Did not respond, check manually");
                        }
                    }
                    else if (NifNodeID == 0x0)
                        ReportService.LogError("DUT has changed its ID to 000");
                    else
                        ReportService.LogError("DUT has changed its ID to some other random value");
                }
            }
            #endregion

            //Requesting NIF, verifying S2 is present, excluding then S2 inclusion
            bool S2Present = false;
            var nifFrame = ComplianceService.RequestNodeInfo(Controller, DutId);
            byte[] openCC = nifFrame.NodeInfo;
            foreach (byte cc in openCC)
                if (cc == COMMAND_CLASS_SECURITY_2.ID)
                    S2Present = true;

            //determine the presence of S2 in the NIF
            Inconclusive(S2Present, "S2 is present in the new Node Informatio", "S2 is missing in the new Node Information");

            //Reinclude node, assert the version has not changed and that it works as expected
            TestInterfaceService.ExcludeNode(Controller, Slave);

            //ApplicationService.AddSecurity(Controller, SecurityVersions.S2 | SecurityVersions.S0);            
            ApplicationService.EnableNetworkKey(Controller, SecuritySchemes.S0, true);
            ApplicationService.EnableNetworkKey(Controller, SecuritySchemes.S2_ACCESS, true);
            ApplicationService.EnableNetworkKey(Controller, SecuritySchemes.S2_AUTHENTICATED, true);
            ApplicationService.EnableNetworkKey(Controller, SecuritySchemes.S2_UNAUTHENTICATED, true);

            AddExpRes = TestInterfaceService.IncludeNode(Controller, Slave);
            AssertIsTrue(AddExpRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done,
                "S0 inclusion successful",
                "S0 inclusion failed");
            DutId = AddExpRes.AddNodeResult.Node;

            Delay(2000);
            ApplicationService.ResumeSecurity(Controller);
            expRepo = AsyncService.ExpectData(Controller, new COMMAND_CLASS_VERSION_V2.VERSION_REPORT(), 3000);
            sendGet = AsyncService.SendData(Controller, DutId, new COMMAND_CLASS_VERSION_V2.VERSION_GET(), TransmitOptions.Ack);

            using (Service.Settings.IsExpectingFailure.Set())
            {
                verReport = expRepo.WaitCompletedSignal().Command;
            }

            if (((verReport.firmware0Version == Firmware2) && (verReport.firmware0SubVersion == SubFirmware2)) &&
                ((verReport.zWaveProtocolVersion == Protocol2) && (verReport.zWaveProtocolSubVersion == SubProtocol2)))
            {
                ReportService.LogOk("The firmware version is the expected");
            }
            else
            {
                ReportService.LogError("The firmware version is not the expected");
            }
        }

        public void ControllerFirmwareUpdateNodeId(Alias controller, Node nodeId, string filePath)
        {
            //Request FIRMWARE UPDATE MD COMMAND CLASS VERSION and wait for report
            COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_GET versionCmdClassGetData = new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_GET();
            versionCmdClassGetData.requestedCommandClass = COMMAND_CLASS_FIRMWARE_UPDATE_MD_V2.ID;
            COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_REPORT versionCmdClassReportData = new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_REPORT();
            var requestVersionRes = ComplianceService.RequestData(controller, nodeId,
                versionCmdClassGetData,
                TX_OPTIONS,
                versionCmdClassReportData, 3000);

            if (requestVersionRes == true && requestVersionRes.Command != null)
            {
                versionCmdClassReportData = requestVersionRes.Command;
                if (versionCmdClassReportData.commandClassVersion == 0x02)
                {
                    var statusByte = ComplianceService.FirmwareUpdateV2(controller, nodeId, filePath);
                    AssertIsTrue(statusByte == 0xFF, "Updated!", "Update failed!");
                }
                else if (versionCmdClassReportData.commandClassVersion == 0x03)
                {
                    var statusByte = ComplianceService.FirmwareUpdateV3(controller, nodeId, filePath);
                    AssertIsTrue(statusByte == 0xFF, "Updated!", "Update failed!");
                }
                else if (versionCmdClassReportData.commandClassVersion == 0x04)
                {
                    var statusByte = ComplianceService.FirmwareUpdateV5(controller, nodeId, filePath);
                    AssertIsTrue(statusByte == 0xFF, "Updated!", "Update failed!");
                }
                else if (versionCmdClassReportData.commandClassVersion == 0x05)
                {
                    var statusByte = ComplianceService.FirmwareUpdateV5(controller, nodeId, filePath);
                    AssertIsTrue(statusByte == 0xFF, "Updated!", "Update failed!");
                }
            }
            Delay(15000, "Device init");
        }
    }
}
