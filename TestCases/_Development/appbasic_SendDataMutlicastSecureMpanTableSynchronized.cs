// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Send data multicast")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.ControllerStaticLib)]
    public class appbasic_SendDataMutlicastSecureMpanTableSynchronized : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Sender = new Alias(1);
            Alias Receiver1 = new Alias(2);
            Alias Receiver2 = new Alias(3);
            Alias[] ALL = new Alias[] { Sender, Receiver1, Receiver2 };

            ParallelService.Reset(ALL);

            ApplicationService.AddSecurity(Sender, SecurityVersions.S2);
            ApplicationService.AddSecurity(Receiver1, SecurityVersions.S2);
            ApplicationService.AddSecurity(Receiver2, SecurityVersions.S2);

            Service.AddNode(Sender, Receiver1);
            Service.AddNode(Sender, Receiver2);

            var dataToSend = new byte[] { 0xC8, 0xA5, 0x2C, 0x9F, 0x1A, 0x7B, 0x68 };

            var expectedData1Token = AsyncService.ExpectData(Receiver1, dataToSend, 6000);
            var expectedData2Token = AsyncService.ExpectData(Receiver2, dataToSend, 6000);

            var sendDataMultiRes = Service.SendDataMulti(Sender, new Alias[] { Receiver1, Receiver2 }, dataToSend, TransmitOptions.Ack | TransmitOptions.AutoRoute);
            
            var expectRes1 = expectedData1Token.WaitCompletedSignal();
            var expectRes2 = expectedData2Token.WaitCompletedSignal();

            AssertIsTrue(sendDataMultiRes);
            AssertIsTrue(expectRes1);
            AssertIsTrue(expectRes2);
        }
    }
}