// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.IO;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"FirmwareUpdate")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [TestInterfaceDeviceMapping(2, LibTypes.SwitchOnOff)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class apptestiface_FirmwareUpdate_InvalidFragmentCrc_RetryRequest : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1, "Primary");
            Alias Slave = new Alias(2);
            bool is700 = Context.IsZW070x(Slave);

            Service.Reset(Primary);
            ApplicationService.AddSecurity(Primary, SecurityVersions.S0 | SecurityVersions.S2);
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Primary);

            if (is700)
            {
                var fromFilePath = ApplicationService.GetFile(Slave, DevKits.Default, ".hex");
                ProgrammerService.ProgramOTABootloader(Slave);
                ProgrammerService.ProgramSample(Slave, fromFilePath);
            }

            TestInterfaceService.ExcludeNode(Primary, Slave);
            var addRes = TestInterfaceService.IncludeNode(Primary, Slave);
            AssertIsTrue(addRes);
            var slaveId = addRes.AddNodeResult.Node;

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
                if (versionCmdClassReportData.commandClassVersion >= 4)
                {
                    var mdRep = ComplianceService.RequestData(Primary, slaveId,
                        new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_GET(),
                        new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_REPORT());
                    AssertIsTrue(mdRep);
                    COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_REPORT cmdMdRep = mdRep.Command;

                    var token = AsyncService.ExpectData(Primary, new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_GET(), 15000);

                    var cmdUmdReqGet = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_GET();
                    cmdUmdReqGet.checksum = "11 22".ToByteArray();
                    cmdUmdReqGet.firmwareId = cmdMdRep.firmware0Id;
                    cmdUmdReqGet.firmwareTarget = 0;
                    cmdUmdReqGet.fragmentSize = new byte[] { 0, 20 };
                    cmdUmdReqGet.hardwareVersion = cmdMdRep.hardwareVersion;
                    cmdUmdReqGet.manufacturerId = cmdMdRep.manufacturerId;
                    var umdReqRep = ComplianceService.RequestData(Primary, slaveId, cmdUmdReqGet,
                      new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_REPORT());
                    AssertIsTrue(umdReqRep);
                    COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_REPORT cmdUmdReqRep = umdReqRep.Command;

                    var umdGet = token.WaitCompletedSignal();
                    AssertIsTrue(umdGet);

                    COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_GET cmdUmdGet = umdGet.Command;
                    AssertIsTrue(cmdUmdGet.numberOfReports == 1);
                    AssertIsTrue(cmdUmdGet.reportNumber2 == 1);

                    token = AsyncService.ExpectData(Primary, new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_GET(), 15000);

                    var cmdUmdRep = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REPORT();
                    cmdUmdRep.checksum = "62 19".ToByteArray();
                    cmdUmdRep.reportNumber2 = 1;
                    cmdUmdRep.data = "80 00 01 5F 8E 3C C1 B9 AE 22 75 FF 02 75 E4 04 75 E4 01 75".ToByteArray();
                    ComplianceService.SendData(Primary, slaveId, cmdUmdRep);

                    umdGet = token.WaitCompletedSignal();
                    AssertIsTrue(umdGet);

                    cmdUmdGet = umdGet.Command;
                    AssertIsTrue(cmdUmdGet.numberOfReports == 1);
                    AssertIsTrue(cmdUmdGet.reportNumber2 == 2);


                    token = AsyncService.ExpectData(Primary, new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_GET(), 15000);

                    cmdUmdRep = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REPORT();
                    cmdUmdRep.checksum = "11 11".ToByteArray();
                    cmdUmdRep.reportNumber2 = 2;
                    cmdUmdRep.data = "80 00 01 5F 8E 3C C1 B9 AE 22 75 FF 02 75 E4 04 75 E4 01 75".ToByteArray();
                    ComplianceService.SendData(Primary, slaveId, cmdUmdRep);

                    umdGet = token.WaitCompletedSignal();
                    AssertIsTrue(umdGet);

                    cmdUmdGet = umdGet.Command;
                    AssertIsTrue(cmdUmdGet.numberOfReports == 1);
                    AssertIsTrue(cmdUmdGet.reportNumber2 == 2);

                    token = AsyncService.ExpectData(Primary, new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_STATUS_REPORT(), 15000);

                    cmdUmdRep = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REPORT();

                    cmdUmdRep.reportNumber2 = 2;
                    cmdUmdRep.properties1.last = 1;
                    cmdUmdRep.checksum = ((byte[])cmdUmdRep).GetCrc16Bytes(0, 4);
                    ComplianceService.SendData(Primary, slaveId, cmdUmdRep);

                    var umdStatusRep = token.WaitCompletedSignal();
                    AssertIsTrue(umdStatusRep);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}