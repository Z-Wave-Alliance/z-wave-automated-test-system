// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Listen Controller Update")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    public class appbasic_ListenControllerUpdate : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);
            Alias EndDevice = new Alias(3);
            Alias[] ALL = new Alias[] { Primary, Secondary, EndDevice };

            ReportService.LogText("Start");
            ParallelService.Reset(ALL);

            Service.ApplicationNodeInformationCmdClasses(EndDevice, new byte[] { 0x20 }, new byte[] { 0x20 }, new byte[] { 0x20 });
            Service.SetSecurityInclusionRequestedKeys(EndDevice, null);
            var addDevRes = Service.AddNode(Primary, EndDevice);
            AssertIsTrue(addDevRes && addDevRes.NodeId == 2, $"Slave added to the network, NodeId={addDevRes.NodeId}");
            var edevId = addDevRes.NodeId;

            var addSecRes = Service.AddNode(Primary, Secondary);
            AssertIsTrue(addSecRes && addSecRes.NodeId == 3, $"Controller added to the network, NodeId={addSecRes.NodeId}");
            var secId = addSecRes.NodeId;

            ReportService.LogText("Sending Data to Primary Controller");
            var listenDataToken = AsyncService.ListenData(Primary);
            var zToken = ZnifferService.ExpectData(5000);
            zToken.SetUpWatch(edevId, 1, new COMMAND_CLASS_BASIC.BASIC_GET());
            zToken.SetUpWatch(secId, 1, new COMMAND_CLASS_BASIC.BASIC_GET());
            Service.SendData(EndDevice, Primary, new COMMAND_CLASS_BASIC.BASIC_GET());
            Service.SendData(Secondary, Primary, new COMMAND_CLASS_BASIC.BASIC_GET());
            var zRes = zToken.WaitCompletedSignal();
            Inconclusive(zRes, "capture BasicGet frame");

            zToken = ZnifferService.ExpectData(5000);
            zToken.SetUpWatch(edevId, 1, new byte[1]);
            zToken.SetUpWatch(secId, 1, new byte[1]);
            Service.SendData(EndDevice, Primary, new byte[1]);
            Service.SendData(Secondary, Primary, new byte[1]);
            zRes = zToken.WaitCompletedSignal();
            Inconclusive(zRes, "capture NOP frame");

            AsyncService.CancelToken(Primary, listenDataToken);
            var listenDataRes = listenDataToken.WaitCompletedSignal();
            Inconclusive(listenDataRes.ReceivedData != null && listenDataRes.ReceivedData.Count == 2, "Primary Controller Listen Data");


            ReportService.LogText("Sending Data to EndDevice");
            var listenControllerUpdateToken = AsyncService.ListenControllerUpdate(EndDevice);
            listenDataToken = AsyncService.ListenData(EndDevice);
            zToken = ZnifferService.ExpectData(10000);
            zToken.SetUpWatch(1, edevId, new COMMAND_CLASS_BASIC.BASIC_GET());
            zToken.SetUpWatch(secId, edevId, new COMMAND_CLASS_BASIC.BASIC_GET());
            Service.SendData(Primary, EndDevice, new COMMAND_CLASS_BASIC.BASIC_GET());
            Service.SendData(Secondary, EndDevice, new COMMAND_CLASS_BASIC.BASIC_GET());
            zRes = zToken.WaitCompletedSignal();
            Inconclusive(zRes, "capture BasicGet frame");

            zToken = ZnifferService.ExpectData(5000);
            zToken.SetUpWatch(1, edevId, new byte[1]);
            zToken.SetUpWatch(secId, edevId, new byte[1]);
            Service.SendData(Primary, EndDevice, new byte[1]);
            Service.SendData(Secondary, EndDevice, new byte[1]);
            zRes = zToken.WaitCompletedSignal();
            Inconclusive(zRes, "capture NOP frame");

            AsyncService.CancelToken(EndDevice, listenDataToken);
            listenDataRes = listenDataToken.WaitCompletedSignal();
            Inconclusive(listenDataRes.ReceivedData != null && listenDataRes.ReceivedData.Count == 2, "EndDevice Listen Data");

            AsyncService.CancelToken(EndDevice, listenControllerUpdateToken);
            var listenControllerUpdateRes = listenControllerUpdateToken.WaitCompletedSignal();
            Inconclusive(listenControllerUpdateRes.ReceivedData != null && listenControllerUpdateRes.ReceivedData.Count == 2, "EndDevice Controller Update");

            ReportService.LogText("Sending Data to Secondary Controller");
            listenDataToken = AsyncService.ListenData(Secondary);
            zToken = ZnifferService.ExpectData(10000);
            zToken.SetUpWatch(1, secId, new COMMAND_CLASS_BASIC.BASIC_GET());
            zToken.SetUpWatch(edevId, secId, new COMMAND_CLASS_BASIC.BASIC_GET());
            Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
            Service.SendData(EndDevice, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
            zRes = zToken.WaitCompletedSignal();
            Inconclusive(zRes, "capture BasicGet frame");

            zToken = ZnifferService.ExpectData(5000);
            zToken.SetUpWatch(1, secId, new byte[1]);
            zToken.SetUpWatch(edevId, secId, new byte[1]);
            Service.SendData(Primary, Secondary, new byte[1]);
            Service.SendData(EndDevice, Secondary, new byte[1]);
            zRes = zToken.WaitCompletedSignal();
            Inconclusive(zRes, "capture NOP frame");

            AsyncService.CancelToken(Secondary, listenDataToken);
            listenDataRes = listenDataToken.WaitCompletedSignal();
            Inconclusive(listenDataRes.ReceivedData != null && listenDataRes.ReceivedData.Count == 2, "EndDevice Listen Data");


            Delay(30000);
        }
    }
}