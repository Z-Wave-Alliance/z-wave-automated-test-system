// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("telnet based attenuator SwitchOnOff")]
    [DeviceMapping(1, LibTypes.ControllerBridgeLib)]
    [DeviceMapping(2, LibTypes.SwitchOnOff)]
    [InstrumentMapping(3, InstrumentTypes.AttenuatorRudat)]
    public class apptelnet_Rudat_SendData : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Dut = new Alias(2);
            Alias Att = new Alias(3);
            Alias[] ALL = new Alias[] { Primary };

            var filePath = ApplicationService.GetFile(Dut, DevKits.Default);
            AssertIsTrue(filePath != null && File.Exists(filePath));
            ProgrammerService.ProgramSample(Dut, filePath);

            var pwSetRes = Service.SetMaxLrTxPower(Primary, MaxLrTxPower.dBm14);
            var pwGetRes = Service.GetMaxLrTxPower(Primary);
            Service.EnableRadioPTI(Primary, true);
            Service.Reset(Primary);

            InstrumentsService.AttenuatorRudat.SetDbLevel(Att, 0);
            ApplicationService.AddSecurity(Primary, SecurityVersions.S0 | SecurityVersions.S2);
            if (Context.IsLongRange(Primary) && Context.IsLongRange(Dut))
            {
                ApplicationService.Provisioning.Add(Primary, Dut, SecuritySchemeSet.ALL, Modes.NodeOptionLongRange);
            }
            else
            {
                ApplicationService.Provisioning.Add(Primary, Dut, SecuritySchemeSet.ALL, Modes.NodeOptionHighPower);
            }
            var addToken = AsyncService.ExpectSmartStartAdd(Primary, 240000);
            var addRes = addToken.WaitCompletedSignal();
            AssertIsTrue(addRes, "Node added to the network NID:" + addRes.AddNodeResult.Node.ToString(), "Unable to add: Node");

            var tests = new List<Tuple<string, List<Tuple<int, int>>>>();
            tests.Add(new Tuple<string, List<Tuple<int, int>>>("TestOneDbStepDownUp", new List<Tuple<int, int>>(new[]
            {
                new Tuple<int,int>(95, 10),
                new Tuple<int,int>(94, 10),
                new Tuple<int,int>(93, 10),
                new Tuple<int,int>(92, 10),
                new Tuple<int,int>(91, 10),
                new Tuple<int,int>(90, 10),
                new Tuple<int,int>(89, 10),
                new Tuple<int,int>(88, 10),
                new Tuple<int,int>(87, 10),
                new Tuple<int,int>(86, 10),
                new Tuple<int,int>(85, 10),
                new Tuple<int,int>(84, 10),
                new Tuple<int,int>(83, 10),
                new Tuple<int,int>(82, 10),
                new Tuple<int,int>(81, 10),
                new Tuple<int,int>(80, 10),
                new Tuple<int,int>(79, 10),
                new Tuple<int,int>(78, 10),
                new Tuple<int,int>(77, 10),
                new Tuple<int,int>(76, 10),
                new Tuple<int,int>(75, 10),
                new Tuple<int,int>(74, 10),
                new Tuple<int,int>(73, 10),
                new Tuple<int,int>(72, 10),
                new Tuple<int,int>(71, 10),
                new Tuple<int,int>(70, 10),
                new Tuple<int,int>(71, 10),
                new Tuple<int,int>(72, 10),
                new Tuple<int,int>(73, 10),
                new Tuple<int,int>(74, 10),
                new Tuple<int,int>(75, 10),
                new Tuple<int,int>(76, 10),
                new Tuple<int,int>(77, 10),
                new Tuple<int,int>(78, 10),
                new Tuple<int,int>(79, 10),
                new Tuple<int,int>(80, 10),
                new Tuple<int,int>(81, 10),
                new Tuple<int,int>(82, 10),
                new Tuple<int,int>(83, 10),
                new Tuple<int,int>(84, 10),
                new Tuple<int,int>(85, 10),
                new Tuple<int,int>(86, 10),
                new Tuple<int,int>(87, 10),
                new Tuple<int,int>(88, 10),
                new Tuple<int,int>(89, 10),
                new Tuple<int,int>(90, 10),
                new Tuple<int,int>(91, 10),
                new Tuple<int,int>(92, 10),
                new Tuple<int,int>(93, 10),
                new Tuple<int,int>(94, 10),
                new Tuple<int,int>(95, 10),
            })));

            //tests.Add(new Tuple<string, List<Tuple<int, int>>>("Test - Step Down Response", new List<Tuple<int, int>>(new[]
            //{
            //    new Tuple<int,int>(110, 8),
            //    new Tuple<int,int>(0, 15),
            //    new Tuple<int,int>(110, 8),
            //    new Tuple<int,int>(20, 15),
            //    new Tuple<int,int>(110, 8),
            //    new Tuple<int,int>(40, 15),
            //    new Tuple<int,int>(110, 8),
            //    new Tuple<int,int>(60, 15),
            //    new Tuple<int,int>(110, 8),
            //    new Tuple<int,int>(80, 15),
            //    new Tuple<int,int>(110, 8),
            //    new Tuple<int,int>(100, 15),
            //})));

            //tests.Add(new Tuple<string, List<Tuple<int, int>>>("Test - Step Up Response", new List<Tuple<int, int>>(new[]
            //{
            //    new Tuple<int,int>(0, 8),
            //    new Tuple<int,int>(100, 15),
            //    new Tuple<int,int>(0, 8),
            //    new Tuple<int,int>(80, 15),
            //    new Tuple<int,int>(0, 8),
            //    new Tuple<int,int>(60, 15),
            //    new Tuple<int,int>(0, 8),
            //    new Tuple<int,int>(40, 15),
            //    new Tuple<int,int>(0, 8),
            //    new Tuple<int,int>(20, 15),
            //    new Tuple<int,int>(0, 8),
            //    new Tuple<int,int>(10, 15),
            //})));

            int testOrder = 0;
            foreach (var test in tests)
            {
                testOrder++;
                var senderFile = ReportService.CreateHtmlFile(new string(test.Item1.Where(x => char.IsLetterOrDigit(x) || x == '_').ToArray()));
                var tableColumns = new[]
                {
                    "Order"
                    , "Attenuator"
                    , "Transmit Status"
                    , "Route Tries"
                    , "Transmit Ticks"
                    , "Elapsed Ms"
                    , "Between Tx Ms"
                    , "Used Txpower"
                    , "Rssi Values Incoming 0"
                    , "Measured Noise Floor"
                    , "AckDestination Used Tx Power"
                    , "Destination Ack Measured RSSI"
                    , "Destination Ack Measured Noise Floor"
                };
                foreach (var maxPowerLevel in new[] { MaxLrTxPower.dBm14, MaxLrTxPower.dBm20 })
                {
                    foreach (var defaultPowerLevel in new sbyte[] { 0, 10, 33, 66, 99, 127 })
                    {
                        Service.SetDefaultTxPowerLevel(Primary, defaultPowerLevel, 0);
                        Service.SetMaxLrTxPower(Primary, maxPowerLevel);
                        Service.SoftReset(Primary);
                        pwGetRes = Service.GetMaxLrTxPower(Primary);
                        var defaultPwGetRes = Service.GetDefaultTxPowerLevel(Primary);
                        senderFile.HeadingL1(test.Item1
                            + " MaxLrTxPower set: " + maxPowerLevel
                            + " get:" + pwGetRes.MaxLrTxPower
                            + " DefaultTxPowerLevel set: " + defaultPowerLevel
                            + " get:" + defaultPwGetRes.NormalTxPower);
                        using (var senderTable = senderFile.Table(tableColumns))
                        {
                            foreach (var steps in test.Item2)
                            {
                                var dbLevel = steps.Item1;
                                var iterations = steps.Item2;
                                InstrumentsService.AttenuatorRudat.SetDbLevel(Att, dbLevel);
                                var res = InstrumentsService.AttenuatorRudat.GetDbLevel(Att);
                                AssertIsTrue(res, $"current attenuation level {res.ReceivedValue}");
                                byte[] data = new COMMAND_CLASS_BASIC.BASIC_SET();
                                for (int i = 0; i < iterations; i++)
                                {
                                    Delay(10);
                                    var sr = ComplianceService.SendDataEx(Primary, addRes.AddNodeResult.Node, data, SecuritySchemes.NONE);
                                    Inconclusive(sr);
                                    senderTable.AddRow(
                                        i.ToString()
                                        , dbLevel.ToString()
                                        , sr.TransmitStatus.ToString()
                                        , sr.RouteTries.ToString()
                                        , sr.TransmitTicks.ToString()
                                        , sr.ElapsedMs.ToString()
                                        , ""
                                        , sr.UsedTxpower.ToString()
                                        , sr.RssiValuesIncoming?[0].ToString()
                                        , sr.MeasuredNoiseFloor.ToString()
                                        , sr.AckDestinationUsedTxPower.ToString()
                                        , sr.DestinationAckMeasuredRSSI.ToString()
                                        , sr.DestinationckMeasuredNoiseFloor.ToString()
                                        );
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}