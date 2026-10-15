// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("After Remove Failing Node function on SIS and SetDefault(sis, 2nd node) cannot add node 2nd node to SIS")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    [DeviceMapping(4, LibTypes.ControllerStaticLib)]
    public class TC_BadAddingNodeAfterReplaceFailed : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias SISController = new Alias(1);
            Alias PrimaryController = new Alias(2);
            Alias SlaveAPI = new Alias(3);
            Alias InclusionController = new Alias(4);

            Alias[] ALL = { SISController, PrimaryController, SlaveAPI, InclusionController };

            ReportService.LogText("Started");
            ParallelService.Reset(ALL);

            BasicResult.MemoryGetId memRes = Service.MemoryGetId(PrimaryController);
            byte[] homeId = memRes.HomeId;
            ReportService.LogText("Home ID: " + homeId.ToHexString());

            ReportService.LogText("1. Add SISController to network");
            BasicResult.AddNode addRes = Service.AddNode(PrimaryController, SISController);
            AssertIsTrue(addRes, "Successfully added SISController: " + addRes.NodeId, "Unable to add SISController");

            // Configure the static controller to be a SIS
            ZnifferToken.ExpectData zt = ZnifferService.ExpectData(5000);
            zt.SetUpWatch(new ZWAVE_CMD_CLASS.CMD_SET_SUC_ACK() { result = 0x80, properties1 = 0x01 }, HeaderTypes.SINGLECAST, true);
            Service.SetSUCNodeID(PrimaryController, SISController, true, 0x01, true);
            ZnifferResult.ExpectData zr = zt.WaitCompletedSignal();
            AssertIsTrue(zr, "Set SUC Ack detected", "Set SUC Ack not detected");

            ReportService.LogText("2. Add SlaveApi to the network");
            addRes = Service.AddNode(PrimaryController, SlaveAPI);
            AssertIsTrue(addRes, "Successfully added SlaveAPI: " + addRes.NodeId, "Unable to add SlaveAPI");

            ReportService.LogText("2. Add InclusionController to the network");
            addRes = Service.AddNode(PrimaryController, InclusionController);
            AssertIsTrue(addRes, "Successfully added InclusionController: " + addRes.NodeId, "Unable to add InclusionController");

            ReportService.LogText("4. Send Basic Get frame to the SlaveAPI from SISController");
            zt = ZnifferService.ExpectData(5000);
            zt.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_GET(), HeaderTypes.SINGLECAST, false);
            Service.SendData(SISController, SlaveAPI, new COMMAND_CLASS_BASIC.BASIC_GET());
            zr = zt.WaitCompletedSignal();
            AssertIsTrue(zr, "Basic Get detected", "Basic Get not detected");

            ReportService.LogText("5. Reset SlaveApi");
            Service.SetDefault(SlaveAPI);

            ReportService.LogText("6. Send Basic Get frame to the failing SlaveAPI");
            zt = ZnifferService.ExpectData(5000);
            zt.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_GET(), HeaderTypes.SINGLECAST, false);
            Service.SendData(SISController, SlaveAPI, new COMMAND_CLASS_BASIC.BASIC_GET());
            zr = zt.WaitCompletedSignal();
            AssertIsTrue(zr, "Basic Get detected", "Basic Get not detected");

            ReportService.LogText("7. Remove failing SlaveApi with \"Remove Failing Node\" function");
            Service.RemoveFailedNodeId(SISController, SlaveAPI);

            //not necessary
            ReportService.LogText("8. Check that removing failing SlaveAPI was successful");
            memRes = Service.MemoryGetId(SlaveAPI);
            byte[] removedNodeHomeId = memRes.HomeId;
            AssertIsTrue(!removedNodeHomeId.SequenceEqual(homeId), "Success to remove SlaveAPI", "Unable to remove SlaveApi");

            ReportService.LogText("9. Reset InclusionController");
            Service.SetDefault(InclusionController);

            ReportService.LogText("10. Send Basic Get frame to the failing InclusionController");
            zt = ZnifferService.ExpectData(5000);
            zt.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_GET(), HeaderTypes.SINGLECAST, false);
            Service.SendData(SISController, InclusionController, new COMMAND_CLASS_BASIC.BASIC_GET());
            zr = zt.WaitCompletedSignal();
            AssertIsTrue(zr, "Basic Get detected", "Basic Get not detected");

            //not necessary
            ReportService.LogText("11. Remove failing InclusionController");
            Service.RemoveFailedNodeId(SISController, InclusionController);
            //not necessary
            ReportService.LogText("12. Check that removing failing InclusionController was successful");
            memRes = Service.MemoryGetId(InclusionController);
            removedNodeHomeId = memRes.HomeId;
            AssertIsTrue(!removedNodeHomeId.SequenceEqual(homeId), "Success to remove SlaveAPI", "Unable to remove SlaveApi");

            //If simple add node with out  SetDefault result will be correct
            //ReportService.LogText("simple add");
            //addRes = Service.AddNode(SISController, InclusionController);
            //AssertIsTrue(addRes, "ok", "ne ok");
            ReportService.LogText("reset");
            ParallelService.SetDefault(SISController, InclusionController);
            ReportService.LogText("repeat add");
            addRes = Service.AddNode(SISController, InclusionController);
            if (!addRes)
            {
                //small fix
                ReportService.LogWarning("Addnode failed. Need delay 2secs. Second attempt");
                Delay(2000);
                addRes = Service.AddNode(SISController, InclusionController);
                if (!addRes)
                {
                    Fail("Cannot add node InclusionController");
                }
            }
            
            //Another way to solve it
            //Service.DeleteReturnRoute(PrimaryController, InclusionController);
            //Service.DeleteReturnRoute(PrimaryController, SISController);
            //or
            //Service.RemoveNode(PrimaryController, InclusionController, SISController, SlaveAPI);
        }
    }
}