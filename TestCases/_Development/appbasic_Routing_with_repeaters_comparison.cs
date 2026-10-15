// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("basic_Routing_with_repeaters_comparison")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    public class appbasic_Routing_with_repeaters_comparison : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias MainController = new Alias(1);
            Alias StatitcController = new Alias(2);
            Alias DestinationNode = new Alias(3);
            Alias[] ALL = { MainController, DestinationNode, StatitcController };

            ReportService.LogText("Started");
            ParallelService.Reset(ALL);

            ReportService.LogText("Add Destination node with MainController");
            BasicResult.AddNode addRes = Service.AddNode(MainController, DestinationNode);
            byte nid1 = addRes.NodeId;
            AssertIsTrue(addRes, "Successfully added Destination node id: " + nid1, "Unable to add Destination node");

            ReportService.LogText("Disable RF at Destination Node.");
            Service.SetRFReceiveMode(DestinationNode, false);

            ReportService.LogText("Add StatitcController with MainController");
            addRes = Service.AddNode(MainController, StatitcController);
            byte nid2 = addRes.NodeId;
            AssertIsTrue(addRes, "Successfully added StatitcController id: " + nid2, "Unable to add StatitcController");

            ReportService.LogText("Enable RF at Destination Node.");
            Service.SetRFReceiveMode(DestinationNode, true);

            byte[] payload = new COMMAND_CLASS_BASIC.BASIC_SET() { value = 0xFF };
            TransmitOptions txOpt = TransmitOptions.AutoRoute | TransmitOptions.Ack;

            ReportService.LogText("Routed Send data to slave1");
            ZnifferToken.ExpectData zt = ZnifferService.ExpectData(5000);
            zt.SetUpWatch(nid2, nid1, null, payload, HeaderTypes.ROUTED_SINGLECAST, null, true, 2);
            zt.SetUpWatch(nid1, nid2, null, null, HeaderTypes.ROUTED_ACKNOWLEDGE, null, false, 2);
            Service.SendData(StatitcController, DestinationNode, payload, txOpt);
            ZnifferResult.ExpectData zr = zt.WaitCompletedSignal();

            if (zr)
            {
                ReportService.LogOk("Routed send data to dest node");

                ReportService.LogText("Check repeater");

                for (int i = 0; i < zr.Frames.Count(); i++)
                {
                    var frame = zr.Frames[i];
                    if (frame.HeaderType.BelongsTo(HeaderTypes.ROUTED_ACKNOWLEDGE))
                    {
                        AssertIsTrue(frame.Repeaters.Contains(0x01), "Reapeter is MainController");
                    }
                }

            }
            else
            {
                Fail("No Routed send data ");
            }
        }
    }
}
