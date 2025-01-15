// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"RAIL Test device Setup and Use demo test case")]
    [RailDeviceMapping(1, "N1")]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class apprail_SetupAndUse_Demo : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias rt1 = new Alias(1);

            bool is700 = Context.IsZW070x(rt1);
            var fr = Context.Frequency;
            

            var frame = new List<byte>(); //new byte[13]; //len - 2
            var hid = new byte[] { 0x00, 0xB0, 0xBA, 0x00 };
            Node srcId = 0x01;
            Node dstId = 100;
            byte prop1= 0x51; // HeaderType t, Speed Modified b, Low Power b, Ack b, Routed b
            byte prop2 = 0x08; //Seq no and rest
            var pld = RailService.PrepareFrame(hid, srcId, dstId, new COMMAND_CLASS_BASIC.BASIC_SET { value = 111 },
                            RailChannels.Ch0,
                            RailService.GetFrameProperties1());

            ReportService.LogText(" - - - Reset chip to start from a known state");
            TestInterfaceService.SoftReset(rt1);
            RailService.Reset(rt1);

            ReportService.LogText(" - - - Make sure to be in Idle mode and enable Zwave");

            ReportService.LogText("Disable receiver mode before setup");
            RailService.SetRx(rt1, false);

            ReportService.LogText("Setup for Z-Wave operation");
            RailService.ControlZWaveMode(rt1, true);

            ReportService.LogText("Setup TX frame length");
            RailService.SetTxLength(rt1, pld.Length);
            
            ReportService.LogText("Setup TX frame (full payload)");
            RailService.SetTxPayload(rt1, 0, pld);


            ReportService.LogText(" - - - Setup Channel");
            ReportService.LogText("Setup Z-Wave region to number x");
            RailService.SetZWaveRegion(rt1, RailZWaveRegions.US);

            ReportService.LogText("Set output power to setting number");
            RailService.SetPower(rt1, 30);

            ReportService.LogText("Setup Z-Wave channel");
            RailService.SetChannel(rt1, RailChannels.Ch0);
            
            ReportService.LogText(" - - - Start Tx Carrier mode: ");

            ReportService.LogText("Enable the CW");
            RailService.SetTxTone(rt1, true);
            Delay(20000 , "check zniffer..");
            RailService.SetTxTone(rt1, false);

            //End Setup: Idle - Off
            ReportService.LogText(" - - - Enable receiver mode after setup");
            RailService.SetTx(rt1, 0);
            Delay(20000, "check zniffer..");
            RailService.SetTx(rt1, 0);

            RailService.SetRx(rt1, false);

        }

    }
}