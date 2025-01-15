// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;
using System.Linq;
using System;

namespace ZatsTests
{
    [ZatsTestCase("Verify that DCDC mode can be set on SerialAPI Controller version > 9", 
        Tags.SYS, Tags.Brief, Tags.PCController)]
    [DeviceMapping(1, ChipSeries.ZW070x, LibTypes.ControllerStaticLib, RfRegion = RfRegions.US_LR)]
    public class appbasic_Get_Set_DcdcMode : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Dut = new Alias(1);

            ReportService.LogTitle("Start");
            var capRes = Service.SerialApiGetCapabilities(Dut);
            AssertIsTrue(capRes && capRes.SupportedSerialApiCommands != null &&
                capRes.SupportedSerialApiCommands.Any(x => x == SerialApiCommands.GetDcdcMode) &&
                capRes.SupportedSerialApiCommands.Any(x => x == SerialApiCommands.SetDcdcMode),
                "Controller supports CmdGetDcdcMode(0xDE) & CmdSetDcdcMode(0xDF)",
                "Controller doesn't support CmdGetDcdcMode(0xDE) & CmdSetDcdcMode(0xDF)!");

            ParallelService.Reset(Dut);

            ReportService.LogText("1. Get the actual DCDC converter value");
            var getRes = Service.GetDcdcMode(Dut);
            AssertIsTrue(getRes,
               "GetDcdcMode = " + getRes.DcdcMode,
               "Failed GetDcdcMode");
            AssertIsTrue(getRes.DcdcMode == DcdcModes.Auto || getRes.DcdcMode == DcdcModes.DCDCLowNoise, "Default Mode");
            var pmRes = ProgrammerService.GetPowerMeasure(Dut, 500);
            AssertIsTrue(pmRes.Current > 9 && pmRes.Current < 11, "in 5. Current consumption ~10 mA = " + pmRes.Current.ToString());


            ReportService.LogText("2. Set DUT's DCDC converter to bypass(0x01) mode");
            var testValue = DcdcModes.ByPass;
            var setRes = Service.SetDcdcMode(Dut, testValue);
            AssertIsTrue(setRes, "SetDcdcMode completed", "SetDcdcMode failed");

            ReportService.LogText("3. Get the actual DCDC converter value");
            getRes = Service.GetDcdcMode(Dut);
            AssertIsTrue(getRes && getRes.DcdcMode == testValue, "GetDcdcMode = " + getRes.DcdcMode, "Failed GetDcdcMode");
            pmRes = ProgrammerService.GetPowerMeasure(Dut, 500);
            AssertIsTrue(pmRes.Current > 14 && pmRes.Current < 16, "in 3. Current consumption ~15 mA = " + pmRes.Current.ToString());


            ReportService.LogText("4. Set DUT's DCDC converter to DCDCLowNoise(0x02) mode");
            testValue = DcdcModes.DCDCLowNoise;
            setRes = Service.SetDcdcMode(Dut, testValue);
            AssertIsTrue(setRes, "SetDcdcMode completed", "SetDcdcMode failed");

            ReportService.LogText("5. Get the actual DCDC converter value");
            getRes = Service.GetDcdcMode(Dut);
            AssertIsTrue(getRes && getRes.DcdcMode == testValue, "GetDcdcMode = " + getRes.DcdcMode, "Failed GetDcdcMode");
            pmRes = ProgrammerService.GetPowerMeasure(Dut, 500);
            AssertIsTrue(pmRes.Current > 9 && pmRes.Current < 11, "in 5. Current consumption ~10 mA = " + pmRes.Current.ToString());

        }
    }
}