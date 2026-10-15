// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("Development TEST Case - check that node adds evev if brake inclusion -  ZF-394")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [TestInterfaceDeviceMapping(3, "TI4")]
    public class appbasic_AddNode_BrakeOnAssignId : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias MainController = new Alias(1);
            Alias SISController = new Alias(2);
            Alias SlaveNode = new Alias(3);
            Alias[] ALL = { MainController, SISController };

            ReportService.LogText("Started");
            ParallelService.Reset(ALL);

            ReportService.LogText("1. Add Secondary Controller to network");
            BasicResult.AddNode addRes = Service.AddNode(MainController, SISController);
            AssertIsTrue(addRes, "Successfully added SISController: " + addRes.NodeId, "Unable to add SISController");

            ReportService.LogText("2. Enable SIS functionality on SISController");
            ZnifferToken.ExpectData zt = ZnifferService.ExpectData(5000);
            zt.SetUpWatch(new ZWAVE_CMD_CLASS.CMD_SET_SUC_ACK() { result = 0x80, properties1 = 0x01 }, HeaderTypes.SINGLECAST, true);
            Service.SetSUCNodeID(MainController, SISController, true, 0x01, true);
            ZnifferResult.ExpectData zr = zt.WaitCompletedSignal();
            AssertIsTrue(zr, "Set SUC Ack detected", "Set SUC Ack not detected");

            ReportService.LogText("4. Add Slave Node with MainController");
            BasicResult.AddNode ret;
            //CommandClassValueTestInterfaceService.SampleButtonActions(SlaveNode).Reset();;
            
            ReportService.LogText("Remove node");
            var removeToken = AsyncService.RemoveNode(MainController);
            TestInterfaceService.SetLearnMode(SlaveNode);
            var removeRes = removeToken.WaitCompletedSignal();
            Delay(1500, "Learn mode completed");


            //
            ReportService.LogText("Add node start");
            var addToken = AsyncService.AddNode(MainController);
            TestInterfaceService.SetLearnMode(SlaveNode);
            //instead of Zniffer Expect Assigh Id frame - better to use next method:
            AsyncService.WaitNodeStatus(MainController, 5000, NodeStatuses.NodeFound);

            // After which we can soft reset controller, to emulated disconect:
            Service.SoftReset(MainController);

            // Also is needed to cancel previous token:
            AsyncService.CancelToken(MainController, addToken);
            
            //Verification that node is available:
            ReportService.LogText("Request data from Slave");
            var res = ComplianceService.RequestData(SISController, 0x03, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());
            AssertIsTrue(res);
        }
    }
}