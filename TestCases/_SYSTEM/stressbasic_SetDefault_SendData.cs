// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Stres Test Set Default")]
    [DeviceMapping(1, LibTypes.Controller)]
    [DeviceMapping(2, LibTypes.Controller)]
    [DeviceMapping(3, LibTypes.Controller)]
    [DeviceMapping(4, LibTypes.EndDeviceAny)]
    [DeviceMapping(5, LibTypes.EndDeviceAny)]
    public class stressbasic_SetDefault_SendData : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Controller1 = new Alias(1);
            Alias Controller2 = new Alias(2);
            Alias Slave1 = new Alias(3);
            Alias Slave2 = new Alias(4);
            Alias Slave3 = new Alias(5);

            Alias[] allDevices = new Alias[]
            {
                Controller1,
                Controller2,
                Slave1,
                Slave2,
                Slave3
            };

            ParallelService.SetDefault(allDevices);

            foreach (var item in allDevices)
            {
                var zt = ZnifferService.ExpectData(1000);
                zt.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_SET() { value = (byte)item.Value }, HeaderTypes.BROADCAST, true);
                ComplianceService.SendData(item, 255, new COMMAND_CLASS_BASIC.BASIC_SET() { value = (byte)item.Value });
                var zr = zt.WaitCompletedSignal();
                Assert.IsTrue(zr, "NODE_INFO" + item.Value);
            }

            var addRes = Service.AddNode(Controller1, Controller2);
            AssertIsTrue(addRes, "Inclusion succeed");

            ApplicationService.AddBasic(Controller1);
            ApplicationService.AddBasic(Controller2);
            var reqRes = Service.RequestData(Controller1, Controller2, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());
            AssertIsTrue(reqRes, "Request BASIC_REPORT");
        }
    }
}