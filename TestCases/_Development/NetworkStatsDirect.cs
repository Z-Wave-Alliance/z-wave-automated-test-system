// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("Testing Network Statistics Serial API functions")]
    [DeviceMapping(1, ChipSeries.ZW070x, LibTypes.ControllerBridgeLib, Alias = "ZG1")]
    [DeviceMapping(2, ChipSeries.ZW070x, LibTypes.EndDeviceLib, Alias = "FG1")]

    public class NetworkStatsDirect : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Controller = new Alias(1);
            Alias SlaveNode = new Alias(2);

            Alias[] ALL = new Alias[] { Controller, SlaveNode };

            ReportService.LogText("Started");
            ParallelService.Reset(ALL);

            byte[] homeID_A = Service.MemoryGetId(Controller).HomeId;

            ReportService.LogText("1. Add SlaveNode to PrimaryController.");
            var addRes_A = Service.AddNode(Controller, SlaveNode);
            AssertIsTrue(addRes_A, "Successfully added SlaveNode, Id: " + addRes_A.NodeId, "Unable to add SlaveNode");

            var NS1 = Service.GetNetworkStats(Controller);
            var zTw = ZnifferService.ExpectData(10000);
            zTw.SetUpWatch(null, HeaderTypes.EXPLORER_NORMAL, false);
            var ztRw = zTw.WaitCompletedSignal();
            Delay(3000);
            Service.ClearNetworkStats(Controller);
            var zT = ZnifferService.CollectData();
            zT.SetUpWatch(null, null, false);
            var NS2 = Service.GetNetworkStats(Controller);
            for (int i = 0; i < 500; i++)
            {
                Service.SendData(Controller, SlaveNode, new COMMAND_CLASS_BASIC_V2.BASIC_SET());
            }
            Delay(3000);
            var NS3 = Service.GetNetworkStats(Controller);
            var zR = zT.WaitCompletedSignal();
            int receivedFrames = 0;
            int transmittedFrames = 0;
            int foreignFrames = 0;
            foreach (ZnifferDataItem frame in zR.Frames)
            {
                if (frame.Source == 0x01 && frame.HomeId.StartsWith(homeID_A)) transmittedFrames++;
                if ((frame.Destination == 0x01 && frame.HomeId.StartsWith(homeID_A)) || frame.Destination == 0xff) receivedFrames++;
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