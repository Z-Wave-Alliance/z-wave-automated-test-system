// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;
using System.Linq;
using System;

namespace ZatsTests
{
    [ZatsTestCase(@"
    -SERIAL_API_SETUP_CMD_SUPPORTED SerialAPI Controller",
                      Tags.SYS,
                      Tags.Brief,
                      Tags.PCController)]
    [DeviceMapping(1, ChipSeries.ZW070x, LibTypes.ControllerStaticLib)]
    public class appbasic_SetupCmdSupport : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Dut = new Alias(1);

            ReportService.LogText("Start");

            var capRes = Service.SerialApiGetCapabilities(Dut);
            AssertIsTrue(capRes && capRes.SupportedSerialApiCommands != null && capRes.SupportedSerialApiCommands.Any(x => x == SerialApiCommands.Setup),
                "Controller supports CmdSerialApiSetup",
                "Controller doesn't support CmdSerialApiSetup - Test will be invalid!");

            Service.SetDefault(Dut);
            var zt = ZnifferService.ExpectData(2000).SetUpWatch(new ZWAVE_CMD_CLASS.NODE_INFO());
            var ztLR = ZnifferService.ExpectData(2000).SetUpWatch(new COMMAND_CLASS_ZWAVE_LONG_RANGE.ZWAVE_LR_CMD_NODE_INFO_FRAME());
            using (ZnifferService.Settings.IsExpectingFailure.Set())
            {
                ComplianceService.SendNodeInfo(Dut, 0xFF, TX_OPTIONS);
                ComplianceService.SendNodeInfo(Dut, 0xFFF, TX_OPTIONS);
                Inconclusive(zt.WaitCompletedSignal(), "ZWAVE_CMD_CLASS");
                Inconclusive(!ztLR.WaitCompletedSignal(), "No COMMAND_CLASS_ZWAVE_LONG_RANGE");
            }

            ReportService.LogText("Enable LR - SERIAL_API_SETUP_CMD_NODEID_BASETYPE_SET");
            Service.SerialApiSetNodeIdBaseType(Dut, 2);

            zt = ZnifferService.ExpectData(2000).SetUpWatch(new ZWAVE_CMD_CLASS.NODE_INFO());
            ztLR = ZnifferService.ExpectData(2000).SetUpWatch(new COMMAND_CLASS_ZWAVE_LONG_RANGE.ZWAVE_LR_CMD_NODE_INFO_FRAME());
            ComplianceService.SendNodeInfo(Dut, 0xFF, TX_OPTIONS);
            ComplianceService.SendNodeInfo(Dut, 0xFFF, TX_OPTIONS);
            Inconclusive(zt.WaitCompletedSignal(), "ZWAVE_CMD_CLASS");
            Inconclusive(ztLR.WaitCompletedSignal(), "COMMAND_CLASS_ZWAVE_LONG_RANGE");


            ReportService.LogText("Get region - SERIAL_API_SETUP_CMD_RF_REGION_GET");
            // In old versions RfRegions was US_LR_BACKUP, but that was not supported for a long time.
            var getRes = Service.GetRfRegion(Dut); // This test case should return with US_LR region
            var testRg = RfRegions.US_LR;
            Inconclusive(getRes,
               "1 GetRfRegion = " + getRes.RfRegion,
               "1 Failed getRes.RfRegion");

            ReportService.LogText("Set region - SERIAL_API_SETUP_CMD_RF_REGION_SET ");
            var setRes = Service.SetRfRegion(Dut, testRg);
            Inconclusive(setRes,
                "Set region to " + testRg,
                "Failed Set region");
            Service.SoftReset(Dut);
            Delay(2000, "SoftReset");
            getRes = Service.GetRfRegion(Dut);
            Inconclusive(getRes,
                "2 GetRfRegion = " + Enum.GetName(typeof(RfRegions), getRes.RfRegion),
                "2 Failed getRes.RfRegion");
            Inconclusive(testRg == getRes.RfRegion, "Set == Get", "Set <> Get");

            ReportService.LogText("Get Max Payload Size LR - SERIAL_API_SETUP_CMD_TX_GET_MAX_LR_PAYLOAD_SIZE ");
            var mpLrRes = Service.GetMaxLRPayloadSize(Dut);
            Inconclusive(mpLrRes,
                "GetMaxPayloadSize LR =" + mpLrRes.ReturnValue,
                "Unsupported GetMaxPayloadSize LR");


            ReportService.LogText("Get Max Payload Size - SERIAL_API_SETUP_CMD_TX_GET_MAX_PAYLOAD_SIZE  ");
            var mpRes = Service.GetMaxPayloadSize(Dut);
            Inconclusive(mpRes,
                "GetMaxPayloadSize =" + mpRes,
                "Failed GetMaxPayloadSize");


            ReportService.LogText("Get Default Tx PowerLevel - SERIAL_API_SETUP_CMD_TX_POWERLEVEL_GET");
            var txPl = Service.GetDefaultTxPowerLevel(Dut);
            Inconclusive(txPl,
                $"GetMaxPayloadSize NormalTxPower={txPl.NormalTxPower}, Measured0dBmPower={txPl.Measured0dBmPower}",
                "Failed GetMaxPayloadSize");

            ReportService.LogText("Set Default Tx PowerLevel - SERIAL_API_SETUP_CMD_TX_POWERLEVEL_SET");
            var settxPl = Service.SetDefaultTxPowerLevel(Dut, 30, 30);
            Inconclusive(settxPl,
                $"SetDefaultTxPowerLevel NormalTxPower=30, Measured0dBmPower=30",
                "Failed SetDefaultTxPowerLevel");
        }
    }
}