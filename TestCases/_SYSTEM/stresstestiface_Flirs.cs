// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Reflection;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("stresstestiface_Flirs_Ctrl714_Doorlock700", Tags.Long)]
    [DeviceMapping(1, ChipSeries.ZW070x, LibTypes.ControllerStaticLib)]
    [TestInterfaceDeviceMapping(2, ChipSeries.ZW070x, LibTypes.DoorLockKeyPad, Alias = "dut")]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class stresstestiface_Flirs_Ctrl714_Slave700 : stresstestiface_Flirs { }

    [ZatsTestCase("stresstestiface_Flirs_Ctrl500_Doorlock700", Tags.Long)]
    [DeviceMapping(1, ChipSeries.ZW050x, LibTypes.ControllerStaticLib)]
    [TestInterfaceDeviceMapping(2, ChipSeries.ZW070x, LibTypes.DoorLockKeyPad, Alias = "dut")]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class stresstestiface_Flirs_Ctrl500_Slave700 : stresstestiface_Flirs { }

    [ZatsTestCase("stresstestiface_Flirs_Ctrl500_Doorlock500", Tags.Long)]
    [DeviceMapping(1, ChipSeries.ZW050x, LibTypes.ControllerStaticLib)]
    [TestInterfaceDeviceMapping(2, ChipSeries.ZW050x, LibTypes.DoorLockKeyPad)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class stresstestiface_Flirs_Ctrl500_Slave500 : stresstestiface_Flirs { }

    [ZatsTestCase("stresstestiface_Flirs_Ctrl700_Doorlock500", Tags.Long)]
    [DeviceMapping(1, ChipSeries.ZW070x, LibTypes.ControllerStaticLib)]
    [TestInterfaceDeviceMapping(2, ChipSeries.ZW050x, LibTypes.DoorLockKeyPad)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class stresstestiface_Flirs_Ctrl700_Slave500 : stresstestiface_Flirs { }

    public class stresstestiface_Flirs : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave = new Alias(2);

            bool is700 = Context.IsZW070x(Slave);
            int Count = 500;

            var delay = 2317;

            var file = ReportService.CreateHtmlFile("qqq");
            file.HeadingL1($"From {Context.GetHostChipSeries(Primary)} Ctrl to {Context.GetHostChipSeries(Slave)} slave (Doorlock)");
            file.HeadingL2($"{Context.Frequency} frequency");
            file.Paragraph($"{GetType().Name}");
            file.Paragraph(ApplicationService.GetFile(Primary, DevKits.Default));
            file.Paragraph(ApplicationService.GetFile(Slave, DevKits.Default));

            foreach (var trun in new[] { DeviceOptions.FreqListeningMode250ms, DeviceOptions.FreqListeningMode1000ms })
            {
                var iter = new Iter(Context.IsZW070x(Primary));
                byte[] Frame = "820133".ToByteArray();
                if (trun == DeviceOptions.FreqListeningMode250ms)
                {
                    Frame = "200333".ToByteArray();
                }
                TestInterfaceService.SoftReset(Primary);
                TestInterfaceService.SoftReset(Slave);
                Service.SetDefault(Primary);

                BasicResult.IncludeNode addRes = null;
                for (int i = 0; i < 10; i++)
                {
                    var remRes = TestInterfaceService.ExcludeNode(Primary, Slave);
                    if (remRes)
                    {
                        addRes = TestInterfaceService.IncludeNode(Primary, Slave);
                        if (addRes && addRes.AddNodeResult)
                        {
                            break;
                        }
                        else
                        {
                            TestInterfaceService.SoftReset(Primary);
                            TestInterfaceService.SoftReset(Slave);
                            Service.SetDefault(Primary);
                        }
                    }
                    else
                    {
                        TestInterfaceService.SoftReset(Primary);
                        TestInterfaceService.SoftReset(Slave);
                        Service.SetDefault(Primary);
                    }
                }
                AssertIsTrue(addRes, "Inclusion");
                Delay(60000); // after non secure inclusion
                for (int i = 0; i < Count; i++)
                {
                    Delay(delay);
                    BasicResult.SendData sendRes = ComplianceService.SendData(Primary, addRes.AddNodeResult.Node, Frame);
                    ReportService.LogText($"{i}. {sendRes.TransmitStatus}, RSpeed{sendRes.RouteSpeed}, TTicks:{sendRes.TransmitTicks}, RTicks:{sendRes.ElapsedMs / 10}, TxCH:{sendRes.LastTxChannelNo}, AckCH:{sendRes.AckChannelNo}");
                    iter.Parse(sendRes);
                }
                iter.Report();
                using (var table = file.Table("Test", "Count", "Retransmit Rate, %", $"Avg Ticks{Environment.NewLine}(Transmit ticks)", $"Avg Delta Ticks{Environment.NewLine}(Delta timestamps)"))
                {
                    table.AddRow($"{trun}", $"{iter.TxTotalCount}(${iter.TxErrorCount})", $"{iter.TxRTriesRate.ToString("0.00")}", $"{iter.SendTmean}", $"{iter.RealTmean}");
                }
                using (var table = file.StackedColumnChart(900, 400, "aa", "bb"))
                {
                    foreach (var item in iter.DeltaTicksHistogram)
                    {
                        table.AddRow($"{item.Key}", $"{item.Value}");
                    }
                }
            }
        }
    }
}