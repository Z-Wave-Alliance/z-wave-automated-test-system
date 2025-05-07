// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("Get Max Payload Size SerialAPI test + send data max")]
    [DeviceMapping(1, "TEST", LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    class appbasic_GetMaxPayloadSize_SendData : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);
            Alias[] ALL = new Alias[] { Primary, Secondary };
            ParallelService.SetDefault(ALL);

            Service.AddNode(Primary, Secondary);

            var res = Service.GetMaxPayloadSize(Primary);
            Inconclusive(res, "GetMaxPayloadSize Primary");
            if (res)
            {
                byte i = 1;
                byte[] sendData = new byte[res.ReturnValue].Select(x => x = i++).ToArray();
                sendData[0] = 0x20;
                sendData[1] = 0x01;
                var sdRes = Service.SendData(Primary, Secondary, sendData);
                Assert.IsTrue(sdRes);

                i = 1;
                sendData = new byte[res.ReturnValue + 2].Select(x => x = i++).ToArray();
                sendData[0] = 0x20;
                sendData[1] = 0x01;
                using (Service.Settings.IsExpectingFailure.Set())
                {
                    sdRes = Service.SendData(Primary, Secondary, sendData);
                    Assert.IsFalse(sdRes);
                }
            }

            Delay(500, " Step 2");

            res = Service.GetMaxPayloadSize(Secondary);
            Inconclusive(res, "GetMaxPayloadSize Secondary");
            if (res)
            {
                byte i = 1;
                byte[] sendData = new byte[res.ReturnValue].Select(x => x = i++).ToArray();
                sendData[0] = 0x20;
                sendData[1] = 0x01;
                var sdRes = Service.SendData(Secondary, Primary, sendData);
                Assert.IsTrue(sdRes);

                i = 1;
                sendData = new byte[res.ReturnValue + 2].Select(x => x = i++).ToArray();
                sendData[0] = 0x20;
                sendData[1] = 0x01;
                using (Service.Settings.IsExpectingFailure.Set())
                {
                    sdRes = Service.SendData(Secondary, Primary, sendData);
                    Assert.IsFalse(sdRes);
                }
            }
        }
    }
}