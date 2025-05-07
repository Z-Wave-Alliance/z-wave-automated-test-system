// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
﻿using Zats.Services;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace ZatsTests
{
    [ZatsTestCase("Security v2 Doorlock successful secured inclusion")]
    [DeviceMapping(1, LibTypes.ControllerBridgeLib)]
    [TestInterfaceDeviceMapping(2, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(3, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(4, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(5, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(6, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(7, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(8, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(9, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(10, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(11, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(12, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(13, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(14, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(15, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(16, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(17, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(18, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(19, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(20, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(21, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(22, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(23, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(24, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(25, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(26, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(27, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(28, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(29, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(30, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(31, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(32, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(33, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(34, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(35, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(36, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(37, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(38, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(39, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(40, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(41, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(42, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(43, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(44, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(45, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(46, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(47, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(48, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(49, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(50, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(51, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(52, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(53, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(54, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(55, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(56, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(57, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(58, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(59, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(60, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(61, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(62, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(63, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(64, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(65, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(66, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(67, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(68, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(69, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(70, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(71, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(72, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(73, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(74, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(75, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(76, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(77, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(78, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(79, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(80, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(81, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(82, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(83, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(84, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(85, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(86, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(87, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(88, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(89, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(90, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(91, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(92, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(93, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(94, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(95, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(96, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(97, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(98, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(99, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(100, LibTypes.SwitchOnOff)]
    [TestInterfaceDeviceMapping(101, LibTypes.SwitchOnOff)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]

    class Latency_Large : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);

            List<Alias> Aliases = new List<Alias>();
            List<Node> Nodes = new List<Node>();

            for (int i = 2; i <= 101; i++)
            {
                Aliases.Add(new Alias((ushort)i));
            }

            Alias[] ALL = new Alias[] { Primary };

            ParallelService.Reset(ALL);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            ApplicationService.AddResponseEx(Primary, new COMMAND_CLASS_WAKE_UP.WAKE_UP_NO_MORE_INFORMATION(), new COMMAND_CLASS_WAKE_UP.WAKE_UP_NOTIFICATION());
            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(Primary);

            #region 1.- Inlcude end nodes to Primary Controller

            ReportService.LogTitle("1- Inlcude Z-Wave+ sample app to Primary");

            foreach (Alias curAlias in Aliases)
            {
                int iter = 0;
                bool inclusionRes = false;
                do
                {
                    TestInterfaceService.SoftReset(curAlias);
                    //reset
                    TestInterfaceService.ExcludeNode(Primary, curAlias);
                    // Adding node to the Controller.
                    var AddExpRes = TestInterfaceService.IncludeNode(Primary, curAlias);
                    inclusionRes = (AddExpRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done);
                    //AssertIsTrue(AddExpRes.AddNodeResult.SubstituteStatus == SubstituteStatuses.Done, $"Successful S2 inclusion. Node {AddExpRes.AddNodeResult.Node}", $"S2 inclusion fails. Node {AddExpRes.AddNodeResult.Node}");
                    if (inclusionRes)
                    {
                        Nodes.Add(AddExpRes.AddNodeResult.Node);
                        ReportService.LogInfo(TestInterfaceService.GetDSK(curAlias, 16).ToHexString());
                        ReportService.LogOk($"Successful S2 inclusion. Node {AddExpRes.AddNodeResult.Node}");
                    }
                    iter++;
                } while (!inclusionRes && iter <= 3);
                AssertIsTrue(inclusionRes, "Successful S2 inclusion.", "S2 inclusion fails. Node");
                Delay(1000);
            }
            Delay(20000);

            #endregion


            #region 2. Sending frames to the end nodes in the network

            int lostFrames = 0;
            List<Node> lostNodes = new List<Node>();
            List<int> rawData = new List<int>();
            List<int> rawDataTick = new List<int>();
            int[] chartData = new int[52];
            int[] chartDataTick = new int[52];

            for (int i = 0; i < 10; i++)
            {
                foreach (Node curNode in Nodes)
                {
                    var reqRes = ComplianceService.SendData(Primary, curNode, new COMMAND_CLASS_BASIC_V2.BASIC_SET());
                    if (reqRes.TransmitStatus == TransmitStatuses.CompleteOk)
                    {
                        rawData.Add(reqRes.ElapsedMs);
                        rawDataTick.Add(reqRes.TransmitTicks * 10);
                        ReportService.LogOk($"Node {curNode}. Frame transmitted. Millisec - {reqRes.ElapsedMs} Ticks - {reqRes.TransmitTicks}");
                        int index = reqRes.ElapsedMs / 10;
                        int indexTick = reqRes.TransmitTicks - 1;
                        if (indexTick < 0) indexTick = 0;
                        if (indexTick > 50) indexTick = 51;
                        if (index > 50) index = 51;
                        chartData[index]++;
                        chartDataTick[indexTick]++;
                    }
                    else
                    {
                        ReportService.LogError($"Node {curNode}. Frame lost");
                        lostNodes.Add(curNode);
                        lostFrames++;
                    }
                }
            }
            #endregion

            #region 3 Data output

            using (StreamWriter sw = File.CreateText(@"c:\work\LatencyRawData.csv"))
            {
                sw.WriteLine($"Lost, {lostFrames}");
                foreach (Node lostNode in lostNodes)
                {
                    sw.WriteLine(lostNode.ToString());
                }
                sw.WriteLine("Latency ms, Latency by ticks ms");
                for (int i = 0; i < rawData.Count; i++)
                {
                    sw.WriteLine($"{rawData[i]}, {rawDataTick[i]}");
                }
            }

            using (StreamWriter sw = File.CreateText(@"c:\work\LatencyChartData.csv"))
            {
                sw.WriteLine($"Lost, {lostFrames}");
                sw.WriteLine("Latency ms, Nodes Elapsed, Nodes Ticks");
                for (int i = 0; i < 52; i++)
                {
                    sw.WriteLine($"{(i + 1) * 10}, {chartData[i]}, {chartDataTick[i]}");
                }
            }


            #endregion


            //#region 9. SendData to DoorLocks

            //ReportService.LogTitle("9. SendData to DoorLocks");

            //for (int j = 0; j < 1000; j++)//12 hour
            ////for (int j = 0; j < 100; j++)
            //{
            //    for (int i = 0; i < DoorLock.Length; i++)
            //    {
            //        int count = 0;
            //        ApplicationService.SecurityTestSettings.SetActiveSecurityScheme(Inclusion, NodeID_DL[i], SecuritySchemes.S2_ACCESS);
            //        ComplianceService.SetSecurityVersionForNode(Inclusion, NodeID_DL[i], SecurityVersions.S2);
            //        for (int k = 0; k < 5; k++)
            //        {
            //            var ReqRes = ComplianceService.RequestData(Inclusion, NodeID_DL[i], new COMMAND_CLASS_VERSION.VERSION_GET(), new COMMAND_CLASS_VERSION.VERSION_REPORT());
            //            Delay(1000);
            //            if(ReqRes)
            //            {
            //                count++;
            //            }
            //        }
            //        Inconclusive(count != 0, "9. Version Report fra DoorLock " + NodeID_DL[i] + " received", "9. Version Report fra DoorLock " + NodeID_DL[i] + " not received");


            //        Delay(1000);
            //    }
            //}


            //#endregion

            //#region 10. Exclude all nodes with Inclusion Controller

            //ReportService.LogTitle("10. Exclude all nodes with Inclusion Controller");

            //for (int i = 0; i < SensorPIR.Length; i++)
            //{
            //    //reset
            //    var excludeRes = TestInterfaceService.ExcludeNode(Inclusion, SensorPIR[i]);
            //    Inconclusive(excludeRes, "Exclusion of SensorPIR[" + (i + 1) + "] were successful", "Exclusion of SensorPIR[" + (i + 1) + "] failed");
            //}

            //for (int i = 0; i < DoorLock.Length; i++)
            //{
            //    //reset
            //    var excludeRes = TestInterfaceService.ExcludeNode(Inclusion, DoorLock[i]);
            //    Inconclusive(excludeRes, "Exclusion of DoorLock[" + (i + 1) + "] were successful", "Exclusion of DoorLock[" + (i + 1) + "] failed");
            //}

            //for (int i = 0; i < WallController.Length; i++)
            //{
            //    //reset
            //    var excludeRes = TestInterfaceService.ExcludeNode(Inclusion, WallController[i]);
            //    Inconclusive(excludeRes, "Exclusion of WallController[" + (i + 1) + "] were successful", "Exclusion of WallController[" + (i + 1) + "] failed");
            //}

            //for (int i = 0; i < PowerStrip.Length; i++)
            //{
            //    //reset
            //    var excludeRes = TestInterfaceService.ExcludeNode(Inclusion, PowerStrip[i]);
            //    Inconclusive(excludeRes, "Exclusion of PowerStrip[" + (i + 1) + "] were successful", "Exclusion of PowerStrip[" + (i + 1) + "] failed");
            //}


            //for (int i = 0; i < Switch.Length; i++)
            //{
            //    //reset
            //    var excludeRes = TestInterfaceService.ExcludeNode(Inclusion, Switch[i]);
            //    Inconclusive(excludeRes, "Exclusion of Switch[" + (i + 1) + "] were successful", "Exclusion of Switch[" + (i + 1) + "] failed");
            //}
            //#endregion

            //#region 10. Check that SISController's node list is identical with Inclusion Controller's node list

            //byte[] resultSIS = Service.SerialApiGetInitData(SISController).IncludedNodes;
            //byte[] resultInclusion = Service.SerialApiGetInitData(Inclusion).IncludedNodes;


            //AssertIsTrue(resultSIS.SequenceEqual(resultInclusion), "SISController NodeList is the same as Inclusion Controller node list", "SISController NodeList is not the same as Inclusion Controller node list");

            //#endregion
        }
    }
}
