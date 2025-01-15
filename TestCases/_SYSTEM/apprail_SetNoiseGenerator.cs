// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"How to Set Up RAIL Test App to generate noise")]
    [RailDeviceMapping(1, "N1")]
    //[RailDeviceMapping(4, "N2")]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    public class apprail_SetNoiseGenerator : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias rt1 = new Alias(1);
            //Alias rt2 = new Alias(4);
            Alias ctrl = new Alias(2);
            Alias slv = new Alias(3);

            ParallelService.Reset(ctrl, slv);

            ApplicationService.AddSecurity(ctrl, SecurityVersions.S2);
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(ctrl);
            ApplicationService.SetApplicationCmd(slv, false, true, LibTypes.SwitchOnOff);

            ReportService.LogText("Reset chip to start from a known state:");
            TestInterfaceService.SoftReset(rt1);
            RailService.Reset(rt1);
            ReportService.LogInfo("Setup RAIL device:");
            RailService.DeviceSetup(rt1, Context.Frequency, 20, 50, Context.IsLongRange(ctrl) ? RailChannels.Ch3 : RailChannels.Ch0);

            
            ReportService.LogText("1. Create network:");
            var ar = Service.IncludeNode(ctrl, slv);
            AssertIsTrue(ar && ar.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done);
            var nid = ar.AddNodeResult.NodeId;
            
            ReportService.LogText("2. run test Send Data");
            //RailService.Reset(rt2);
            //RailService.DeviceSetup(rt2, Context.Frequency, 20, 50, RailChannels.Ch1);
            //RailService.SetTxTone(rt2, true);

            for (int i = 1; i < 16; i++)
            {
                RailService.SetRx(rt1, false);
                RailService.SetPower(rt1, i * 10);

                //Enable Noise
                RailService.SetTxTone(rt1, true);

                var backgroundRssiRes1 = Service.GetBackgroundRSSI(ctrl);
                Delay(1000);
                Service.ClearTxTimers(ctrl);
                Service.ClearNetworkStats(ctrl);

                var zt = ZnifferService.ExpectData(8000).SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_SET());
                var et = AsyncService.ExpectData(slv, new COMMAND_CLASS_BASIC.BASIC_SET(), 8000);

                var sd = Service.SendData(ctrl, slv, new COMMAND_CLASS_BASIC.BASIC_SET());

                ReportService.LogInfo($"Send Data completed with" + Environment.NewLine +
                    $" TransmitStatus = {sd.TransmitStatus}," + Environment.NewLine +
                    $" LastTxChannelNo = {sd.LastTxChannelNo}, " + Environment.NewLine +
                    $" MeasuredNoiseFloor = {sd.MeasuredNoiseFloor}, " + Environment.NewLine +
                    $" DestinationckMeasuredNoiseFloor = {sd.DestinationckMeasuredNoiseFloor}, " + Environment.NewLine +
                    $" DestinationAckMeasuredRSSI = {sd.DestinationAckMeasuredRSSI}");

                if (et.WaitCompletedSignal()) ReportService.LogOk("Slave received");
                else ReportService.LogError("Slave didn't receive");

                if (zt.WaitCompletedSignal()) ReportService.LogOk("Zniffer detected");
                else ReportService.LogError("Zniffer didn't detect");

                var txTimerRes = Service.GetTxTimer(ctrl);
                var nwStatRes = Service.GetNetworkStats(ctrl);
                var backgroundRssiRes2 = Service.GetBackgroundRSSI(ctrl);

                ReportService.LogInfo(i + ". RFTx LBTBackOffs: " + nwStatRes.RFTxLBTBackOffs);
                ReportService.LogInfo(i + ". RFRx ForeignHomeID: " + nwStatRes.RFRxForeignHomeID);
                ReportService.LogInfo(i + ". RFRx LRCErrors: " + nwStatRes.RFRxLRCErrors);
                ReportService.LogInfo(i + ". RFRx CRC16Errors: " + nwStatRes.RFRxCRC16Errors);

                //Disable Noise
                RailService.SetTxTone(rt1, false);
                var backgroundRssiRes3 = Service.GetBackgroundRSSI(ctrl);
                Delay(1000);
                Service.SendData(ctrl, slv, new byte[1]);
                var backgroundRssiRes4 = Service.GetBackgroundRSSI(ctrl);

                ReportService.LogText("RSSI 1 Noise ON:             " + string.Join(",", backgroundRssiRes1.BackgroundRSSILevels.Select(x => ((sbyte)x).ToString()).ToArray()));
                ReportService.LogText("RSSI 2 Noise ON after delay: " + string.Join(",", backgroundRssiRes2.BackgroundRSSILevels.Select(x => ((sbyte)x).ToString()).ToArray()));
                ReportService.LogText("RSSI 3 Noise OFF:            " + string.Join(",", backgroundRssiRes3.BackgroundRSSILevels.Select(x => ((sbyte)x).ToString()).ToArray()));
                ReportService.LogText("RSSI 4 Noise OFF after delay:" + string.Join(",", backgroundRssiRes4.BackgroundRSSILevels.Select(x => ((sbyte)x).ToString()).ToArray()));
            }
        }


    }
}