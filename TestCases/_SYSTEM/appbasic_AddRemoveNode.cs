// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@" SetDefault, Add/Remove controller slave",
        Tags.SYS, Tags.Brief, Tags.PCController)]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    public class appbasic_AddRemoveNode : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);
            Alias EndDevice = new Alias(3);
            Alias[] ALL = new Alias[] { Primary, Secondary, EndDevice };

            ReportService.LogText("Start");
            ParallelService.Reset(ALL);

            var addRes = Service.AddNode(Primary, Secondary);
            AssertIsTrue(addRes && addRes.NodeId == 2, $"Controller added to the network, NodeId={addRes.NodeId}");

            Service.ApplicationNodeInformationCmdClasses(EndDevice, new byte[] { 0x20 }, new byte[] { 0x20 }, new byte[] { 0x20 });
            Service.SetSecurityInclusionRequestedKeys(EndDevice, null);
            addRes = Service.AddNode(Primary, EndDevice);
            AssertIsTrue(addRes && addRes.NodeId == 3, $"Slave added to the network, NodeId={addRes.NodeId}");

            var zToken = ZnifferService.ExpectData(10000);
            zToken.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_GET());
            Service.SendData(Primary, Secondary, new COMMAND_CLASS_BASIC.BASIC_GET());
            var zRes = zToken.WaitCompletedSignal();
            AssertIsTrue(zRes, "capture BasicGet frame");



            var remRes = Service.RemoveNode(Primary, Secondary);
            AssertIsTrue(remRes && remRes.NodeId == 2, $"Controller removed from the network, NodeId={remRes.NodeId}");


            addRes = Service.AddNode(Primary, EndDevice, 4000);
            //AssertIsTrue(addRes && addRes.NodeId == 3, $"Slave added to the network, NodeId={addRes.NodeId}");
            
            remRes = Service.RemoveNode(Primary, EndDevice);
            AssertIsTrue(remRes && remRes.NodeId == 3, $"Slave removed from the network, NodeId={remRes.NodeId}");
        }
    }
}