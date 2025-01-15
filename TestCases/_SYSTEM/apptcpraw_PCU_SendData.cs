// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.IO;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("Power Supply and Measument tool", Tags.SYS, Tags.Brief)]
    [DeviceMapping(1, LibTypes.ControllerBridgeLib)]
    [DeviceMapping(2, LibTypes.DoorLockKeyPad, Alias = "power")]
    [InstrumentMapping(3, InstrumentTypes.PsuE364x)]
    [InstrumentMapping(4, InstrumentTypes.Dmm7510)]
    public class apptcpraw_PCU_SendData : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Dut = new Alias(2);
            Alias PSU = new Alias(3);
            Alias DMM = new Alias(4);
            var setVRes = InstrumentsService.PsuE364x.SetVoltage(PSU, 1, 3.3F);
            AssertIsTrue(setVRes, $"set voltage level {setVRes.ReceivedValue}");

            ApplicationService.AddSecurity(Primary, SecurityVersions.S0 | SecurityVersions.S2);
            Service.Reset(Primary);

            var filePath = ApplicationService.GetFile(Dut, DevKits.Default, ".hex");
            AssertIsTrue(filePath != null && File.Exists(filePath));
            DmmTraceLog trace;
            using (InstrumentsService.Dmm7510.CaptureTrace(DMM, Dmm7510Modes.Current100mV, out trace))
            {
                Delay(2000);
               // ProgrammerService.ProgramSample(Dut, filePath);
            }
            //InstrumentsService.Dmm7510.AboveThreshholdElapsed(DMM, Dmm7510Modes.Current100mV, )

            //float inclusionIntegral = trace.Integral();
            //AssertIsTrue(inclusionIntegral < 1000);

            //ReportService.LogText("Add slave dsk to the provisioning list");
            //ApplicationService.Provisioning.Add(Primary, Dut, SecuritySchemeSet.ALL);
            //var addToken = AsyncService.ExpectSmartStartAdd(Primary, 60000);
            //ReportService.LogText("Wait for smart start inclusion completed");
            //var addRes = addToken.WaitCompletedSignal();

            //AssertIsTrue(addRes);
            //AssertIsTrue(addRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done);
            //AssertIsTrue(addRes.AddNodeResult.Node.Id > 1, " Node Id = " + addRes.AddNodeResult.Node);

            //Delay(5000);

            //var getVRes = InstrumentsService.PsuE364x.GetVoltage(PSU, 1);
            //AssertIsTrue(getVRes, $"get voltage level {getVRes.ReceivedValue}");
        }
    }
}