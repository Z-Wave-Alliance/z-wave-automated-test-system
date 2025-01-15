// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("telnet based attenuator", Tags.SYS, Tags.Brief)]
    [DeviceMapping(1, LibTypes.ControllerBridgeLib)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    [InstrumentMapping(3, InstrumentTypes.AttenuatorRudat)]
    public class apptelnet_SendData : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);
            Alias Att = new Alias(3);
            Alias[] ALL = new Alias[] { Primary, Secondary };

            ReportService.LogText("Start");
            ParallelService.SetDefault(ALL);
            InstrumentsService.AttenuatorRudat.SetDbLevel(Att, 0);
            ApplicationService.AddSecurity(Primary, SecurityVersions.S0 | SecurityVersions.S2);
            Service.SetSecurityInclusionRequestedKeys(Secondary, new[] { SecuritySchemes.S2_ACCESS });
            ApplicationService.SecurityTestSettings.IncludeSettings.GrantedSchemes = new[] { SecuritySchemes.NONE };
            var addRes = Service.IncludeNode(Primary, Secondary);
            AssertIsTrue(addRes, "Node added to the network NID:" + addRes.AddNodeResult.Node.ToString(), "Unable to add: Node");

            var tests = new List<Tuple<string, List<Tuple<int, int>>>>();
            tests.Add(new Tuple<string, List<Tuple<int, int>>>("Test - Step Down Response", new List<Tuple<int, int>>(new[]
            {
                new Tuple<int,int>(110, 8),
                new Tuple<int,int>(0, 15),
                new Tuple<int,int>(110, 8),
                new Tuple<int,int>(20, 15),
                new Tuple<int,int>(110, 8),
                new Tuple<int,int>(40, 15),
                new Tuple<int,int>(110, 8),
                new Tuple<int,int>(60, 15),
                new Tuple<int,int>(110, 8),
                new Tuple<int,int>(80, 15),
                new Tuple<int,int>(110, 8),
                new Tuple<int,int>(100, 15),
            })));

            tests.Add(new Tuple<string, List<Tuple<int, int>>>("Test - Step Up Response", new List<Tuple<int, int>>(new[]
            {
                new Tuple<int,int>(0, 8),
                new Tuple<int,int>(100, 15),
                new Tuple<int,int>(0, 8),
                new Tuple<int,int>(80, 15),
                new Tuple<int,int>(0, 8),
                new Tuple<int,int>(60, 15),
                new Tuple<int,int>(0, 8),
                new Tuple<int,int>(40, 15),
                new Tuple<int,int>(0, 8),
                new Tuple<int,int>(20, 15),
                new Tuple<int,int>(0, 8),
                new Tuple<int,int>(10, 15),
            })));

            tests.Add(new Tuple<string, List<Tuple<int, int>>>("Test - Step Up Response (100)", new List<Tuple<int, int>>(new[]
            {
                new Tuple<int,int>(0, 8),
                new Tuple<int,int>(100, 15),
                new Tuple<int,int>(0, 8),
                new Tuple<int,int>(100, 15),
                new Tuple<int,int>(0, 8),
                new Tuple<int,int>(100, 15),
                new Tuple<int,int>(0, 8),
                new Tuple<int,int>(100, 15),
                new Tuple<int,int>(0, 8),
                new Tuple<int,int>(100, 15),
                new Tuple<int,int>(0, 8),
                new Tuple<int,int>(100, 15),
            })));

            tests.Add(new Tuple<string, List<Tuple<int, int>>>("Test - Step Up Response (105)", new List<Tuple<int, int>>(new[]
            {
                new Tuple<int,int>(0, 8),
                new Tuple<int,int>(105, 15),
                new Tuple<int,int>(0, 8),
                new Tuple<int,int>(105, 15),
                new Tuple<int,int>(0, 8),
                new Tuple<int,int>(105, 15),
                new Tuple<int,int>(0, 8),
                new Tuple<int,int>(105, 15),
                new Tuple<int,int>(0, 8),
                new Tuple<int,int>(105, 15),
                new Tuple<int,int>(0, 8),
                new Tuple<int,int>(105, 15),
            })));

            tests.Add(new Tuple<string, List<Tuple<int, int>>>("Test - Step 20 Up Response", new List<Tuple<int, int>>(new[]
            {
                new Tuple<int,int>(0, 8),
                new Tuple<int,int>(20, 15),
                new Tuple<int,int>(40, 15),
                new Tuple<int,int>(60, 15),
                new Tuple<int,int>(80, 15),
                new Tuple<int,int>(100, 15),
            })));

            tests.Add(new Tuple<string, List<Tuple<int, int>>>("Test - Step 10 Up Response", new List<Tuple<int, int>>(new[]
            {
                new Tuple<int,int>(0, 8),
                new Tuple<int,int>(10, 15),
                new Tuple<int,int>(20, 15),
                new Tuple<int,int>(30, 15),
                new Tuple<int,int>(40, 15),
                new Tuple<int,int>(50, 15),
                new Tuple<int,int>(60, 15),
                new Tuple<int,int>(70, 15),
                new Tuple<int,int>(80, 15),
                new Tuple<int,int>(90, 15),
                new Tuple<int,int>(100, 15),
            })));

            tests.Add(new Tuple<string, List<Tuple<int, int>>>("Test - Step 20 Down Response", new List<Tuple<int, int>>(new[]
            {
                new Tuple<int,int>(110, 8),
                new Tuple<int,int>(100, 15),
                new Tuple<int,int>(80, 15),
                new Tuple<int,int>(60, 15),
                new Tuple<int,int>(40, 15),
                new Tuple<int,int>(20, 15),
                new Tuple<int,int>(0, 15),
            })));

            tests.Add(new Tuple<string, List<Tuple<int, int>>>("Test - Step 10 Down Response", new List<Tuple<int, int>>(new[]
            {
                new Tuple<int,int>(110, 8),
                new Tuple<int,int>(100, 15),
                new Tuple<int,int>(90, 15),
                new Tuple<int,int>(80, 15),
                new Tuple<int,int>(70, 15),
                new Tuple<int,int>(60, 15),
                new Tuple<int,int>(50, 15),
                new Tuple<int,int>(40, 15),
                new Tuple<int,int>(30, 15),
                new Tuple<int,int>(20, 15),
                new Tuple<int,int>(10, 15),
                new Tuple<int,int>(0, 15),
            })));

            int testOrder = 0;
            foreach (var test in tests)
            {
                testOrder++;
                var senderFile = ReportService.CreateHtmlFile(new string(test.Item1.Where(x => char.IsLetterOrDigit(x) || x == '_').ToArray()));
                var tableColumns = new[]
                {
                    "Order"
                    , "Attenuator"
                    , "TransmitStatus"
                    , "RouteTries"
                    , "TransmitTicks"
                    , "ElapsedMs"
                    , "BetweenTxMs"
                    , "UsedTxpower"
                    , "RssiValuesIncoming_0"
                    , "MeasuredNoiseFloor"
                    , "AckDestinationUsedTxPower"
                    , "DestinationAckMeasuredRSSI"
                    , "DestinationckMeasuredNoiseFloor"
                };
                foreach (var interval in new[] { 0, 100 })
                {
                    senderFile.HeadingL1(test.Item1 + " delay: " + interval);
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
                                if (interval > 0)
                                {
                                    Delay(interval);
                                }
                                var sr = Service.SendDataEx(Primary, Secondary, data, SecuritySchemes.NONE);
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