// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("Testing Network Statistics Serial API functions", Tags.Long)]
    [DeviceMapping(1, ChipSeries.ZW070x, LibTypes.ControllerBridgeLib, Alias = "ZG1")]
    [DeviceMapping(2, ChipSeries.ZW070x, LibTypes.ControllerBridgeLib, Alias = "ZG2")]
    [DeviceMapping(3, ChipSeries.ZW070x, LibTypes.EndDeviceLib, Alias = "FG1")]
    [DeviceMapping(4, ChipSeries.ZW070x, LibTypes.EndDeviceLib, Alias = "FG2")]

    public class NetworkStatsDirect_2Nets : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Controller_A = new Alias(1);
            Alias SlaveNode_A = new Alias(3);
            Alias Controller_B = new Alias(2);
            Alias SlaveNode_B = new Alias(4);

            Alias[] ALL = new Alias[] { Controller_A, SlaveNode_A, Controller_B, SlaveNode_B };

            ReportService.LogText("Started");
            ParallelService.Reset(ALL);

            byte[] homeID_A = Service.MemoryGetId(Controller_A).HomeId;

            ReportService.LogText("1. Add SlaveNode to PrimaryController. Network A");
            var addRes_A = Service.AddNode(Controller_A, SlaveNode_A);
            AssertIsTrue(addRes_A, "Successfully added SlaveNode, Id: " + addRes_A.NodeId, "Unable to add SlaveNode");

            ReportService.LogText("1. Add SlaveNode to PrimaryController. Network B");
            var addRes_B = Service.AddNode(Controller_B, SlaveNode_B);
            AssertIsTrue(addRes_B, "Successfully added SlaveNode, Id: " + addRes_B.NodeId, "Unable to add SlaveNode");


            var NS1 = Service.GetNetworkStats(Controller_A);
            var zTw = ZnifferService.ExpectData(10000);
            zTw.SetUpWatch(null, HeaderTypes.EXPLORER_NORMAL, false);
            var ztRw = zTw.WaitCompletedSignal();
            Delay(3000);
            Service.ClearNetworkStats(Controller_A);
            var zT = ZnifferService.CollectData();
            zT.SetUpWatch(null, null, false);
            var NS2 = Service.GetNetworkStats(Controller_A);
            for (int i = 0; i < 10; i++)
            {
                Service.SendData(Controller_B, SlaveNode_B, new COMMAND_CLASS_BASIC_V2.BASIC_GET());
                var lmT = AsyncService.AddNode(Controller_B);
                //Delay(50);
                AsyncService.CancelToken(Controller_B, lmT);
                Service.SendData(Controller_A, SlaveNode_A, new COMMAND_CLASS_BASIC_V2.BASIC_SET());
            }
            Delay(3000);
            var NS3 = Service.GetNetworkStats(Controller_A);
            var zR = zT.WaitCompletedSignal();
            int receivedFrames = 0;
            int transmittedFrames = 0;
            int foreignFrames = 0;
            foreach (ZnifferDataItem frame in zR.Frames)
            {
                if (frame.Source == 0x01 && frame.HomeId.StartsWith(homeID_A)) transmittedFrames++;
                if ((frame.Destination == 0x01 && frame.HomeId.StartsWith(homeID_A)) || (frame.Destination == 0xff)) receivedFrames++;
                if (!frame.HomeId.StartsWith(homeID_A)) foreignFrames++;
            }

            ReportService.LogText("RFTxFrames - " + NS3.RFTxFrames.ToString());
            ReportService.LogText("RFRxFrames - " + NS3.RFRxFrames.ToString());
            ReportService.LogText("RFTxLBTBackOffs - " + NS3.RFTxLBTBackOffs.ToString());
            ReportService.LogText("RFRxLRCErrors - " + NS3.RFRxLRCErrors.ToString());
            ReportService.LogText("RFRxCRC16Errors - " + NS3.RFRxCRC16Errors.ToString());
            ReportService.LogText("RFRxForeignHomeID - " + NS3.RFRxForeignHomeID.ToString());

            ReportService.LogTitle("Zniffer");
            ReportService.LogText("Transmitted - " + transmittedFrames.ToString());
            ReportService.LogText("Received - " + receivedFrames.ToString());
            ReportService.LogText("Foreign - " + foreignFrames.ToString());

            Delay(1000);
        }
    }
}