// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("basic_SetSleepMode_WUT")]
    [DeviceMapping(1, LibTypes.ControllerPortableLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    public class appbasic_SetSleepMode_WUT : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias PortableController = new Alias(1);
            Alias StaticController = new Alias(2);
            Alias Slave = new Alias(3);
            Alias[] ALL = { PortableController, StaticController, Slave };

            ReportService.LogText("Started");
            ParallelService.Reset(ALL);

            ReportService.LogText("Add StaticController with PortableController");
            BasicResult.AddNode addRes = Service.AddNode(PortableController, StaticController);
            byte nid2 = addRes.NodeId;
            AssertIsTrue(addRes, "Successfully added StaticController id: " + nid2, "Unable to add StaticController");

            ReportService.LogText("Set StaticController as SUC");
            ZnifferToken.ExpectData zt = ZnifferService.ExpectData(5000);
            zt.SetUpWatch(new ZWAVE_CMD_CLASS.CMD_SET_SUC_ACK() { result = 0x80, properties1 = 0x00 }, HeaderTypes.SINGLECAST, true);
            Service.SetSUCNodeID(PortableController, StaticController, true, 0x00, true);
            ZnifferResult.ExpectData zr = zt.WaitCompletedSignal();
            AssertIsTrue(zr, "Set SUC Ack detected", "Set SUC Ack not detected");

            ReportService.LogText("Power off StaticController");
            Service.SetRFReceiveMode(StaticController, false);

            ReportService.LogText("Add Slave node with PortableController");
            addRes = Service.AddNode(PortableController, Slave);
            byte nid1 = addRes.NodeId;
            AssertIsTrue(addRes, "Successfully added Slave node id: " + nid1, "Unable to add Slave node");

            ReportService.LogText("Set PortableController in SleepMode");
            zt = ZnifferService.ExpectData(5000);
            zt.SetUpWatch(new ZWAVE_CMD_CLASS.NEW_NODE_REGISTERED(), HeaderTypes.SINGLECAST, false);
            Service.SetSleepMode(PortableController, SleepModes.WUT, 0);
            zr = zt.WaitCompletedSignal();
            AssertIsTrue(zr, "NEW_NODE_REGISTERED detected", "NEW_NODE_REGISTERED not detected");

            ReportService.LogText("Wait long enough for the controller comes back from Sleepmode");
            Delay(2000);
            AssertIsTrue(Service.Reconnect(PortableController), "Reconnect after sleep mode"); //Reconnect after sleep mode(UZB reinit issue)

            ReportService.LogText("Set PortableController in SleepMode");
            zt = ZnifferService.ExpectData(5000);
            zt.SetUpWatch(new ZWAVE_CMD_CLASS.NEW_NODE_REGISTERED(), HeaderTypes.SINGLECAST, false);
            Service.SetSleepMode(PortableController, SleepModes.WUT, 0);
            zr = zt.WaitCompletedSignal();
            AssertIsTrue(zr, "NEW_NODE_REGISTERED detected", "NEW_NODE_REGISTERED not detected");
        }
    }
}
