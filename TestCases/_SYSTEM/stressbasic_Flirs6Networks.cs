// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("stressbasic_Flirs_Ctrl714_Slave700")]
    [DeviceMapping(1, ChipSeries.ZW070x, LibTypes.ControllerBridgeLib)]
    [DeviceMapping(2, ChipSeries.ZW070x, LibTypes.EndDeviceLib)]
    [DeviceMapping(3, ChipSeries.ZW070x, LibTypes.ControllerBridgeLib)]
    [DeviceMapping(4, ChipSeries.ZW070x, LibTypes.EndDeviceLib)]
    [DeviceMapping(5, ChipSeries.ZW070x, LibTypes.ControllerBridgeLib)]
    [DeviceMapping(6, ChipSeries.ZW070x, LibTypes.EndDeviceLib)]
    [DeviceMapping(7, ChipSeries.ZW070x, LibTypes.ControllerBridgeLib)]
    [DeviceMapping(8, ChipSeries.ZW070x, LibTypes.EndDeviceLib)]
    [DeviceMapping(9, ChipSeries.ZW070x, LibTypes.ControllerBridgeLib)]
    [DeviceMapping(10, ChipSeries.ZW070x, LibTypes.EndDeviceLib)]
    [DeviceMapping(11, ChipSeries.ZW070x, LibTypes.ControllerBridgeLib)]
    [DeviceMapping(12, ChipSeries.ZW070x, LibTypes.EndDeviceLib)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class stressbasic_Flirs6Networks : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias[] Primary = new Alias[] { new Alias(1), new Alias(3), new Alias(5), new Alias(7), new Alias(9), new Alias(11) };
            Alias[] Slave = new Alias[] { new Alias(2), new Alias(4), new Alias(6), new Alias(8), new Alias(10), new Alias(12) };
            Alias[] All = Primary.Concat(Slave).ToArray();
            int n = 6;
            int m = 3; // disabled slaves
            bool is700 = Context.IsZW070x(Slave[0]);

            int[] delay = new int[] { 20000, 20000, 20000, 5000, 5000, 3000 };
            foreach (var trun in Context.ChannelConfig != ChannelConfigs.Ch12 ? new[] { DeviceOptions.FreqListeningMode1000ms } : new[] { DeviceOptions.FreqListeningMode250ms, DeviceOptions.FreqListeningMode1000ms })
            {

                byte[] Frame = "820133".ToByteArray();
                if (trun == DeviceOptions.FreqListeningMode250ms)
                {
                    Frame = "200333".ToByteArray();
                }

                foreach (var item in All)
                {
                    TestInterfaceService.SoftReset(item);
                }
                ParallelService.SetDefault(All);

                foreach (var item in Slave)
                {
                    Service.ApplicationNodeInformation(item, trun,
                        (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                        new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SWITCH_BINARY.ID });
                    Service.SoftReset(item);
                }

                BasicResult.IncludeNodeWithLearnMode[] addRes = new BasicResult.IncludeNodeWithLearnMode[n];
                BasicResult.MemoryGetId[] memRes = new BasicResult.MemoryGetId[n];
                Node lastAddedNodeId = 0;
                for (int i = 0; i < n; i++)
                {
                    for (int k = 0; k < 10; k++)
                    {
                        memRes[i] = Service.MemoryGetId(Primary[i]);
                        for (int v = 0; v < lastAddedNodeId.Id; v++)
                        {
                            Service.SetVirtualDeviceLearnMode(Primary[i], 0, VirtualDeviceLearnModes.Add);
                        }

                        addRes[i] = Service.IncludeNode(Primary[i], Slave[i], 65000);
                        if (addRes[i] && addRes[i].AddNodeResult && addRes[i].SetLearnMode)
                        {

                            break;
                        }
                        else
                        {
                            TestInterfaceService.SoftReset(Primary[i]);
                            TestInterfaceService.SoftReset(Slave[i]);
                            ParallelService.SetDefault(Primary[i], Slave[i]);
                        }
                    }
                    AssertIsTrue(addRes[i], "Inclusion");
                    lastAddedNodeId = addRes[i].AddNodeResult.Node;
                    if (is700)
                    {
                        if (i < m)
                        {
                            Service.SetDefault(Slave[i]); // disable
                        }
                        else
                        {
                            Delay(2000);
                            Service.PowerMgmtCancel(Slave[i], 0);
                        }
                    }
                }

                HtmlFile[] file = new HtmlFile[n];
                Tuple<BasicToken.Void, Iter>[] tuples = new Tuple<BasicToken.Void, Iter>[n];

                for (int i = 0; i < n; i++)
                {
                    file[i] = ReportService.CreateHtmlFile($"Network{i + 1}");
                    file[i].HeadingL1($"Test on {DateTime.Now.Date.ToShortDateString()}");
                    file[i].HeadingL1($"6 networks in parallel");
                    file[i].HeadingL2($"{Context.Frequency} frequency");
                    file[i].HeadingL3($"Network {i + 1}: {memRes[i].HomeId.ToHexString()}");
                    file[i].Paragraph($"{GetType().Name}");
                    file[i].Paragraph(ApplicationService.GetFile(Primary[i], DevKits.Default));
                    file[i].Paragraph(ApplicationService.GetFile(Slave[i], DevKits.Default));
                }

                for (int i = 0; i < n; i++)
                {
                    var it = new Iter(Context.IsZW070x(Primary[i]));
                    var tk = AsyncService.NoiseData(Primary[i], addRes[i].AddNodeResult.Node, Frame, TX_OPTIONS, x =>
                    {
                        ReportService.LogText($"{i}. {x.TransmitStatus}, RSpeed{x.RouteSpeed}, TTicks:{x.TransmitTicks}, RTicks:{x.ElapsedMs / 10}");
                        it.Parse(x);
                    }, delay[i], 20000);
                    tuples[i] = new Tuple<BasicToken.Void, Iter>(tk, it);
                }

                Delay(7200000);

                for (int i = 0; i < n; i++)
                {
                    AsyncService.CancelToken(Primary[i], tuples[i].Item1);
                    tuples[i].Item1.WaitCompletedSignal();
                    var it = tuples[i].Item2;
                    it.Report();
                    using (var table = file[i].Table("Test", "Count", "Retransmit Rate, %", $"Avg Ticks{Environment.NewLine}(Transmit ticks)", $"Avg Delta Ticks{Environment.NewLine}(Delta timestamps)"))
                    {
                        table.AddRow($"{trun}", $"{it.TxTotalCount}({it.TxErrorCount})", $"{it.TxRTriesRate.ToString("0.00")}", $"{it.SendTmean}", $"{it.RealTmean}");
                    }

                    using (var table = file[i].Table("status", "count"))
                    {
                        foreach (var item in it.TxCount)
                        {
                            table.AddRow($"{item.Key}", $"{item.Value}");
                        }
                    }

                    using (var table = file[i].Table("Avg Delta Ticks (Delta timestamps) histogram", "Retry count histogram"))
                    {
                        using (var row = table.Row())
                        {
                            using (row.Cell())
                            {
                                using (var chart = file[i].StackedColumnChart(800, 400, "aa", "bb"))
                                {
                                    foreach (var item in it.DeltaTicksHistogram)
                                    {
                                        chart.AddRow($"{item.Key}", $"{item.Value}");
                                    }
                                }
                            }
                            using (row.Cell())
                            {
                                using (var chart = file[i].StackedColumnChart(200, 400, "aa", "bb"))
                                {
                                    foreach (var item in it.RetryHistogram)
                                    {
                                        chart.AddRow($"{item.Key}", $"{item.Value}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}