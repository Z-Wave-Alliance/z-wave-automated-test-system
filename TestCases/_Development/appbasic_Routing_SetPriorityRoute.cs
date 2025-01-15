// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
﻿using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("basic_Routing_SetPriorityRoute")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    [DeviceMapping(4, LibTypes.EndDeviceLib)]
    public class appbasic_Routing_SetPriorityRoute : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias MainController = new Alias(1);
            Alias StatitcController = new Alias(2);
            Alias DestinationNode = new Alias(3);
            Alias Slave = new Alias(4);
            Alias[] ALL = { MainController, DestinationNode, StatitcController, Slave };

            ReportService.LogText("Started");
            ParallelService.Reset(ALL);

            ReportService.LogText("Add Destination node with ControllerNode");
            BasicResult.AddNode addRes = Service.AddNode(MainController, DestinationNode);
            byte nid1 = addRes.NodeId;
            AssertIsTrue(addRes, "Successfully added Destination node id: " + nid1, "Unable to add Destination node");

            //Service.SetRFReceiveMode(DestinationNode, false);

            ReportService.LogText("Add StatitcController node with ControllerNode");
            addRes = Service.AddNode(MainController, StatitcController);
            byte nid2 = addRes.NodeId;
            AssertIsTrue(addRes, "Successfully added Repeater Node id: " + nid2, "Unable to add Repeater Node");

            ReportService.LogText("Add Slave node with ControllerNode");
            addRes = Service.AddNode(MainController, Slave);
            byte nid3 = addRes.NodeId;
            AssertIsTrue(addRes, "Successfully added Slave Node id: " + nid3, "Unable to add Slave Node");

            //Service.SetRFReceiveMode(DestinationNode, true);

            byte hop0 = nid3;
            byte hop1 = nid2;
            byte hop2 = 0x00;
            byte hop3 = 0x00;
            byte speed = 3; //(100kbps by default)
            BasicResult.SetPriorityRoute res = Service.SetPriorityRoute(MainController, DestinationNode, hop0, hop1, hop2, hop3, speed);
            AssertIsTrue(res,
                    "Application Priority Route set for DestinationNode through repeaters" + new[] { hop0, hop1, hop2, hop3 }.ToHexString(),
                    "Application Priority Route for DestinationNode through repeaters " + new[] { hop0, hop1, hop2, hop3 }.ToHexString() + " not set");

            byte[] payload = new COMMAND_CLASS_BASIC.BASIC_SET() { value = 0xFF };
            TransmitOptions txOpt = TransmitOptions.AutoRoute | TransmitOptions.Ack;

            ReportService.LogText("Routed Send data to slave1");
            ZnifferToken.ExpectData zt = ZnifferService.ExpectData(5000);
            zt.SetUpWatch(0x01, nid1, null, payload, HeaderTypes.ROUTED_SINGLECAST, null, true, 2);
            zt.SetUpWatch(nid1, 0x01, null, null, HeaderTypes.ROUTED_ACKNOWLEDGE, null, false, 2);
            Service.SendData(MainController, DestinationNode, payload, txOpt);
            ZnifferResult.ExpectData zr = zt.WaitCompletedSignal();

            if (zr)
            {
                ReportService.LogOk("Routed send data to dest node");

                ReportService.LogText("Check repeater");

                for (int i = 0; i < zr.Frames.Count(); i++)
                {
                    var frame = zr.Frames[i];
                    if (HeaderTypes.ROUTED_ACKNOWLEDGE.Contains((byte)frame.HeaderType))
                    {
                        AssertIsTrue(frame.Repeaters[0] == hop0 && frame.Repeaters[1] == hop1, "Routed send data Ack though repeaters: " + new[] { hop0, hop1, hop2, hop3 }.ToHexString());
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
