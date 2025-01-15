// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Send data multicast")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.ControllerStaticLib)]
    public class appbasic_SendDataMutlicastSecure : ZatsCodeTestCase
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
            ApplicationService.AddTransportService(Sender);
            ApplicationService.AddTransportService(Receiver1);
            ApplicationService.AddTransportService(Receiver2);

            var addRes1 = Service.AddNode(Sender, Receiver1);
            var receiverNodeId1 = addRes1.NodeId;
            var addRes2 = Service.AddNode(Sender, Receiver2);
            var receiverNodeId2 = addRes2.NodeId;

            var dataToSend = new byte[] { 0x20, 0x03, 0x9E, 0x8D, 0xF5, 0x80, 0xC7, 0x8C, 0xA3, 0x58, 0x3, 0x33, 0xA8, 0x3C, 0x7E, 0x73, 0xE2, 0xC2, 0xF3, 0xAC, 0x37, 0x8D, 0x14, 0xD6, 0xDE, 0x71, 0xC8, 0xA5, 0x2C, 0x9F, 0x1A, 0x7B, 0x68 };

            const int MUTLICAST_FRAMES_COUNT = 2;
            // Only on-time MPAN and SPAN synchronization.
            // After synchronization only folowup singelcast will be sent after each multicast.
            //var zt = ZnifferService.ExpectData(10000);
            var zt = ZnifferService.CollectData().
                SetUpWatch(0x01, receiverNodeId1, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_GET(), HeaderTypes.SINGLECAST, null, false).
                SetUpWatch(receiverNodeId1, 0x01, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT(), HeaderTypes.SINGLECAST, null, false).
                SetUpWatch(0x01, receiverNodeId2, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_GET(), HeaderTypes.SINGLECAST, null, false).
                SetUpWatch(receiverNodeId2, 0x01, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT(), HeaderTypes.SINGLECAST, null, false);
                //SetUpWatch(0x01, receiverNodeId1, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(),
                //    HeaderTypes.SINGLECAST, null, false, MUTLICAST_FRAMES_COUNT).
                //SetUpWatch(0x01, receiverNodeId2, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(),
                //  HeaderTypes.SINGLECAST, null, false, MUTLICAST_FRAMES_COUNT)
            //
            for (int i = 0; i < MUTLICAST_FRAMES_COUNT; i++)
            {
                var expectedData1Token = AsyncService.ExpectData(Receiver1, dataToSend, 6000);
                var expectedData2Token = AsyncService.ExpectData(Receiver2, dataToSend, 6000);

                var sendDataMultiRes = Service.SendDataMulti(Sender, new Alias[] { Receiver1, Receiver2 }, dataToSend, TransmitOptions.Ack | TransmitOptions.AutoRoute);

                var expectRes1 = expectedData1Token.WaitCompletedSignal();
                var expectRes2 = expectedData2Token.WaitCompletedSignal();

                AssertIsTrue(sendDataMultiRes);
                AssertIsTrue(expectRes1);
                AssertIsTrue(dataToSend.SequenceEqual(expectRes1.Command));
                AssertIsTrue(expectRes2);
                AssertIsTrue(dataToSend.SequenceEqual(expectRes2.Command));
            }

            var znifferCollectRes = zt.WaitCompletedSignal();
            Assert.AreEqual(1, znifferCollectRes.FilterFrames(0x01, receiverNodeId1, COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_GET.ID).Count());
            Assert.AreEqual(1, znifferCollectRes.FilterFrames(receiverNodeId1, 0x01, COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT.ID).Count());
            Assert.AreEqual(1, znifferCollectRes.FilterFrames(0x01, receiverNodeId2, COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_GET.ID).Count());
            Assert.AreEqual(1, znifferCollectRes.FilterFrames(receiverNodeId2, 0x01, COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT.ID).Count());
        }
    }
}