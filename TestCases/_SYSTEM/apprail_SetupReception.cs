// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"How to Set Up for Reception of Z-Wave Frames, section 4.5.5 'INS14283'")]
    [RailDeviceMapping(1, "N1")]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    public class apprail_SetupReception : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias rt1 = new Alias(1);
            Alias ctrl = new Alias(2);
            Alias slv = new Alias(3);

            var frequency = Context.Frequency;

            byte MAX_Z_FRAME_LEN = 60;
            
            ReportService.LogInfo("Reset chip to start from a known state:");
            TestInterfaceService.SoftReset(rt1);
            RailService.Reset(rt1);

            ReportService.LogInfo("Setup RAIL device:");
            RailService.SetRx(rt1, false);
            RailService.ControlZWaveMode(rt1, true);
            RailService.SetTxLength(rt1, MAX_Z_FRAME_LEN);
            RailService.SetZWaveRegion(rt1, frequency);
            RailService.ConfigRXChannelHopping(rt1, frequency);
            RailService.EnableRxChannelHopping(rt1, true);


            ReportService.LogText("Create network:");
            ParallelService.Reset(ctrl, slv);
            Service.IncludeNode(ctrl, slv);


            ReportService.LogInfo("Enable reception of Z-Wave frames on the RAIL device:");
            RailService.SetRx(rt1, true);

            //string lastRec = string.Empty;
            //var receiveTkn = RailService.Listen(rt1, (x)=> 
            //{
            //    lastRec = x;
            //    ReportService.LogText(x);
            //});
            RailService.StartListen(rt1);
            for (int i = 0; i < 10; i++)
            {
                var zt = ZnifferService.ExpectData(2000).SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_SET());
                Service.SendData(ctrl, slv, new COMMAND_CLASS_BASIC.BASIC_SET());
                //var reqRes = RailService.RequestWithDelay(rt1, Environment.NewLine, 500);
                //var res = RailService.ParsetOutput(reqRes);
                //var resFr = res.FirstOrDefault(x => x.Contains("payload:"));
                var zr = zt.WaitCompletedSignal();
                //if (zr && zr.Frames.Count > 0 && resFr != null)
                //{
                //    var act = zr.Frames[0].DataBuffer.ToHexString();
                //    var exp = resFr.Replace("payload: ", "").Replace("0x", "");

                //    ReportService.LogWarning(
                //        Environment.NewLine +
                //        $"znif: {act}" + Environment.NewLine +
                //        $"rail: {exp}");
                //}
            }
            //receiveTkn.WaitCompletedSignal();

            var receiveRes = RailService.StopListen(rt1);
            List<string[]> parsed = new List<string[]>();
            foreach (var item in receiveRes)
            {
                parsed.Add(RailService.ParsetOutput(item));
            }
        }

    }
}