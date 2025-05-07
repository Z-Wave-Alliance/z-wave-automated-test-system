// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Transport Service through repeaters to FLiRS")]
    [DeviceMapping(1, LibTypes.Controller)]
    [DeviceMapping(2, LibTypes.EndDeviceAny)]
    [DeviceMapping(3, LibTypes.EndDeviceAny)]
    [DeviceMapping(4, LibTypes.EndDeviceAny)]
    [DeviceMapping(5, LibTypes.EndDeviceAny)]
    [TestInterfaceDeviceMapping(10, LibTypes.DoorLockKeyPad)]


    public class appbasic_TransportServiceToFlirs : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            var testc = 2;
            byte[] _datagram = new byte[100];
            _datagram[0] = COMMAND_CLASS_VERSION.ID;
            _datagram[1] = COMMAND_CLASS_VERSION.VERSION_REPORT.ID;

            Alias primary = new Alias(1);
            Alias r1Alias = new Alias(2);
            Alias r2Alias = new Alias(3);
            Alias r3Alias = new Alias(4);
            Alias r4Alias = new Alias(5);
            Alias tiDLKPAlias = new Alias(10);
            Alias[] ALL = new Alias[] { primary, r1Alias, r2Alias, r3Alias, r4Alias };

            ParallelService.Reset(ALL);
            //ApplicationService.AddSecurity(primary, SecurityVersions.S2 | SecurityVersions.S0);

            var addRes1 = Service.IncludeNode(primary, r1Alias);
            var addRes2 = Service.IncludeNode(primary, r2Alias);
            var addRes3 = Service.IncludeNode(primary, r3Alias);
            var addRes4 = Service.IncludeNode(primary, r4Alias);
            var r1Node = addRes1.AddNodeResult.Node;
            var r2Node = addRes2.AddNodeResult.Node;
            var r3Node = addRes3.AddNodeResult.Node;
            var r4Node = addRes4.AddNodeResult.Node;


            //test
            var expected = new COMMAND_CLASS_VERSION.VERSION_GET();
            var et = AsyncService.ExpectData(r1Alias, expected);
            ComplianceService.SendData(primary, r1Node, expected);
            var erRes = et.WaitCompletedSignal();
            AssertIsTrue(erRes, $"Repeater #1 recieved data from primary: " +
                $"Dst: {erRes.DestNode};" +
                $"Src: {erRes.Node};" +
                $"Sec: {erRes.SecurityScheme};" +
                $"CMD: {erRes.Command.ToHexString()}");
            //endtest
            ApplicationService.AddTransportService(primary);

            TestInterfaceService.ExcludeNode(primary, tiDLKPAlias);

            var doorlockNode = TestInterfaceService.IncludeNode(primary, tiDLKPAlias).AddNodeResult.Node;

            ComplianceService.SetPriorityRoute(primary, doorlockNode, r1Node, 0, 0, 0, 3);
            ComplianceService.SendNop(primary, doorlockNode, TX_OPTIONS);
            for (int i = 0; i < testc; i++)
            {
                Delay(2000, "Let Door Lock sleep for WUPs");
                TesMethod(primary, doorlockNode, _datagram);
            }

            ComplianceService.SetPriorityRoute(primary, doorlockNode, r1Node, r2Node, 0, 0, 3);
            ComplianceService.SendNop(primary, doorlockNode, TX_OPTIONS);
            for (int i = 0; i < testc; i++)
            {
                Delay(2000, "Let Door Lock sleep for WUPs");
                TesMethod(primary, doorlockNode, _datagram);
            }

            ComplianceService.SetPriorityRoute(primary, doorlockNode, r1Node, r2Node, r3Node, 0, 3);
            ComplianceService.SendNop(primary, doorlockNode, TX_OPTIONS);
            for (int i = 0; i < testc; i++)
            {
                Delay(2000, "Let Door Lock sleep for WUPs");
                TesMethod(primary, doorlockNode, _datagram);
            }

            ComplianceService.SetPriorityRoute(primary, doorlockNode, r1Node, r2Node, r3Node, r4Node, 3);
            ComplianceService.SendNop(primary, doorlockNode, TX_OPTIONS);

            for (int i = 0; i < testc; i++)
            {
                Delay(2000, "Let Door Lock sleep for WUPs");
                TesMethod(primary, doorlockNode, _datagram);
            }


            Service.SetRFReceiveMode(r1Alias, false);
            ComplianceService.SendNop(primary, doorlockNode, TX_OPTIONS);

            for (int i = 0; i < testc; i++)
            {
                Delay(2000, "Let Door Lock sleep for WUPs");
                TesMethod(primary, doorlockNode, _datagram);
            }

        }


        private void TesMethod(Alias sender, Node dest, byte[] data)
        {
            var et_TS_C = AsyncService.ExpectData(sender, new COMMAND_CLASS_TRANSPORT_SERVICE_V2.COMMAND_SEGMENT_COMPLETE());
            var et_TS_W = AsyncService.ExpectData(sender, new COMMAND_CLASS_TRANSPORT_SERVICE_V2.COMMAND_SEGMENT_WAIT());

            using (Service.Settings.IsExpectingFailure.Set())
            {

                var sdRes = ComplianceService.SendData(sender, dest, data);
                var b = sdRes.TransmitStatus == TransmitStatuses.CompleteOk;
                var etRes_TS_C = et_TS_C.WaitCompletedSignal();
                var etRes_TS_W = et_TS_W.WaitCompletedSignal();

                AssertIsTrue(etRes_TS_C, "COMMAND_SEGMENT_COMPLETE");
                Inconclusive(!etRes_TS_W, "No COMMAND_SEGMENT_WAIT - ok");
            }
        }
    }
}