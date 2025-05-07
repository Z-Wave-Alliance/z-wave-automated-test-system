// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;
using System.IO;
using System.Collections.Generic;


namespace ZatsTests
{
    [ZatsTestCase("Testing latency with different payloads in chains of repeaters",
        Tags.Long)]
    [DeviceMapping(1, LibTypes.ControllerBridgeLib)]
    [DeviceMapping(2, LibTypes.EndDeviceAny)]
    [DeviceMapping(3, LibTypes.EndDeviceAny)]
    [DeviceMapping(4, LibTypes.EndDeviceAny)]
    [DeviceMapping(5, LibTypes.EndDeviceAny)]
    [DeviceMapping(6, LibTypes.ControllerBridgeLib)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class Latency_Multihop : ZatsCodeTestCase
    {
        const int testRuns = 100;
        const int passLimit = 5;

        protected override void ExecuteTest()
        {
            Alias Source = new Alias(1);
            Alias Repeater_1 = new Alias(2);
            Alias Repeater_2 = new Alias(3);
            Alias Repeater_3 = new Alias(4);
            Alias Repeater_4 = new Alias(5);
            Alias Destination = new Alias(6);
            Alias[] ALL = new Alias[] { Source, Destination, Repeater_1, Repeater_2, Repeater_3, Repeater_4 };
            Alias[] REPEATERS = new Alias[] { Repeater_1, Repeater_2, Repeater_3, Repeater_4 };

            Service.ApplicationNodeInformation(Source, DeviceOptions.Listening,
                (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER,
                new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID });

            Service.ApplicationNodeInformation(Destination, DeviceOptions.Listening,
                (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER,
                new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID });

            ApplicationService.AddResponse(Destination, new COMMAND_CLASS_BASIC.BASIC_REPORT(), new COMMAND_CLASS_BASIC.BASIC_GET());


            var cmdClasses = new byte[] { COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID, COMMAND_CLASS_BASIC.ID };
            //Service.ApplicationNodeInformationCmdClasses(Repeater_1, cmdClasses, cmdClasses, cmdClasses);
            //Service.ApplicationNodeInformationCmdClasses(Repeater_2, cmdClasses, cmdClasses, cmdClasses);
            //Service.ApplicationNodeInformationCmdClasses(Repeater_3, cmdClasses, cmdClasses, cmdClasses);
            //Service.ApplicationNodeInformationCmdClasses(Repeater_4, cmdClasses, cmdClasses, cmdClasses);
 

            //ApplicationService.AddSecurity(Source, SecurityVersions.S2);
            //ApplicationService.AddSecurity(Destination, SecurityVersions.S2);

            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Source);

            // Payloads from 10 to 48 bytes
            byte[] payload10 = new byte[] { 32, 2, 3, 4, 5, 6, 7, 8, 9, 16 };
            byte[] payload20 = new byte[] { 32, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 32 };
            byte[] payload30 = new byte[] { 32, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 48 };
            byte[] payload32 = new byte[] { 32, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 50 };
            byte[] payload34 = new byte[] { 32, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 52 };
            byte[] payload36 = new byte[] { 32, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 54 };
            byte[] payload38 = new byte[] { 32, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 56 };
            byte[] payload40 = new byte[] { 32, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 64 };
            byte[] payload42 = new byte[] { 32, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 66 };
            byte[] payload44 = new byte[] { 32, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 68 };
            byte[] payload46 = new byte[] { 32, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 70 };
            byte[] payload47 = new byte[] { 32, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 71 };
            byte[] payload48 = new byte[] { 32, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 72 };


            var payloads = new[]
            {
                payload10, payload20, payload30, payload32, payload34, payload36,
                payload38, payload40, payload42, payload44, payload46, payload48
            };
            var buildNetwork = true;

            var addDestRes = Service.IncludeNode(Source, Destination).AddNodeResult;
            AssertIsTrue(addDestRes, "Successfully added Destination node", "Destination node inclusion failed");
            var addRep1Res = Service.IncludeNode(Source, Repeater_1).AddNodeResult;
            AssertIsTrue(addRep1Res, "Successfully included Repeater 1", "Repeater 1 inclusion failed");
            var rep1Node = addRep1Res.Node;
            var addRep2Res = Service.IncludeNode(Source, Repeater_2).AddNodeResult;
            AssertIsTrue(addRep2Res, "Successfully included Repeater 2", "Repeater 2 inclusion failed");
            var rep2Node = addRep2Res.Node;
            var addRep3Res = Service.IncludeNode(Source, Repeater_3).AddNodeResult;
            AssertIsTrue(addRep3Res, "Successfully included Repeater 3", "Repeater 3 inclusion failed");
            var rep3Node = addRep3Res.Node;
            var addRep4Res = Service.IncludeNode(Source, Repeater_4).AddNodeResult;
            AssertIsTrue(addRep4Res, "Successfully included Repeater 4", "Repeater 4 inclusion failed");
            var rep4Node = addRep1Res.Node;

            Node[] hop1 = new Node[] { 0, 0, 0, 0 };
            Node[] hop2 = new Node[] { rep1Node, 0, 0, 0 };
            Node[] hop3 = new Node[] { rep1Node, rep2Node, 0, 0 };
            Node[] hop4 = new Node[] { rep1Node, rep2Node, rep3Node, 0 };
            Node[] hop5 = new Node[] { rep1Node, rep2Node, rep3Node, rep4Node };

            var hops = new[] { hop1, hop2, hop3, hop4, hop5 };

            List<string> outLines = new List<string>();
            

            foreach (var hopchain in hops)
            {
                Service.SetPriorityRoute(Source, Destination, hopchain[0], hopchain[1], hopchain[2], hopchain[3], 0x01);
                string successLine = "RTT";
                string lossLine = "Loss";
                foreach (var payload in payloads)
                {
                    Service.SendData(Source, Destination, payload);

                    double successCount = 0;
                    int lossCount = 0;
                    double roundTrip = 0.0;
                    Delay(1000);
                    for (int i = 0; i < 2; i++)
                    {
                        var replyT = AsyncService.ExpectData(Source, new COMMAND_CLASS_BASIC_V2.BASIC_REPORT(), 5000);
                        Service.SendData(Source, Destination, payload);
                        var replyR = replyT.WaitCompletedSignal();
                        Delay(1000);
                        if (replyR)
                        {
                            successCount++;
                            roundTrip += replyR.ElapsedMs;
                        }
                        else
                        {
                            lossCount++;
                        }
                    }
                    double averageRTT = roundTrip / successCount;
                    successLine += $",{averageRTT}";
                    lossLine += $",{lossCount}";
                }
                outLines.Add(successLine);
                outLines.Add(lossLine);
            }

            using (StreamWriter outData = File.CreateText(@"c:\work\latency.csv"))
            {
                foreach (string line in outLines)
                {
                    outData.WriteLine(line);
                }
            }


            //for (int p = 0; p < payloads.Length; p++)
            //{
            //    int count = 0;
            //    var payload = payloads[p];
            //    ReportService.LogText($"Try #{p} to transmit {testRuns} frames with payload size of {payload.Length} bytes");
            //    for (int i = 0; i < testRuns; i++)
            //    {
            //        if (buildNetwork)
            //        {
            //            ParallelService.Reset(ALL);
            //            var addDestRes = Service.AddNode(ControllerNode, DestinationNode);
            //            AssertIsTrue(addDestRes, "Destination node inclusion");
            //            var addRep2Res = Service.AddNode(ControllerNode, Repeater_2);
            //            rep2 = addRep2Res.NodeId;
            //            AssertIsTrue(addRep2Res, "Repeater 2 node inclusion");
            //            Service.SetRFReceiveMode(DestinationNode, false);
            //            var addRep3Res = Service.AddNode(ControllerNode, Repeater_3);
            //            rep3 = addRep3Res.NodeId;
            //            AssertIsTrue(addRep2Res, "Repeater 3 node inclusion");
            //            Service.SetRFReceiveMode(Repeater_2, false);
            //            var addRep4Res = Service.AddNode(ControllerNode, Repeater_4);
            //            rep4 = addRep4Res.NodeId;
            //            AssertIsTrue(addRep4Res, "Repeater 4 node inclusion");
            //            Service.SetRFReceiveMode(Repeater_3, false);
            //            var addRep5Res = Service.AddNode(ControllerNode, Repeater_5);
            //            rep5 = addRep5Res.NodeId;
            //            AssertIsTrue(addRep5Res, "Repeater 5 node inclusion");
            //            Service.SetRFReceiveMode(Repeater_4, false);
            //            var addRCtrlRes = Service.AddNode(ControllerNode, RoutedController);
            //            AssertIsTrue(addRCtrlRes, "Routed Controller node inclusion");
            //            Service.AddNode(ControllerNode, Repeater_5);

            //            Service.SetRFReceiveMode(DestinationNode, true);
            //            Service.SetRFReceiveMode(Repeater_2, true);
            //            Service.SetRFReceiveMode(Repeater_3, true);
            //            Service.SetRFReceiveMode(Repeater_4, true);

            //            var send0Res = Service.SendData(RoutedController, DestinationNode, new COMMAND_CLASS_BASIC.BASIC_SET(), TransmitOptions.AutoRoute | TransmitOptions.Ack);
            //            AssertIsTrue(send0Res.TransmitStatus == TransmitStatuses.CompleteOk
            //                && send0Res.RepeatersCount == 4
            //                && send0Res.Repeaters.Contains(rep5)
            //                && send0Res.Repeaters.Contains(rep4)
            //                && send0Res.Repeaters.Contains(rep3)
            //                && send0Res.Repeaters.Contains(rep2),
            //                "Routed network successfully established", "Routed network not estableshed");
            //            buildNetwork = false;
            //        }

            //        var sendRes = Service.SendData(RoutedController, DestinationNode, payload, TransmitOptions.AutoRoute | TransmitOptions.Ack);
            //        if (sendRes.TransmitStatus == TransmitStatuses.CompleteOk
            //            && sendRes.RepeatersCount == 4
            //            && sendRes.Repeaters.Contains(rep5)
            //            && sendRes.Repeaters.Contains(rep4)
            //            && sendRes.Repeaters.Contains(rep3)
            //            && sendRes.Repeaters.Contains(rep2))
            //        {
            //            // success
            //        }
            //        else
            //        {
            //            // fail
            //            count++;
            //            ReportService.LogWarning("Rebuild network after unsuccessful attempt");
            //            buildNetwork = true;
            //        }
            //        ReportService.LogText($"Try #{p}.{i} Payload {payload.Length} bytes. Number of routing error in the session : {count}");
            //    }
            //    AssertIsTrue(count <= passLimit, $"Transmission session finished - payload size {payload.Length} bytes, lost frames {count}, total trnasmissions {testRuns}",
            //        $"Number of times (from {testRuns} times) Send Data (payload {payload.Length}) failes: {count} - Failure rate {(count * 100 / testRuns)}%");
            //}
        }
    }
}

