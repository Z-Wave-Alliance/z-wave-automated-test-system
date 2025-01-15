// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Reflection;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("stressbasic_Flirs_Ctrl714_Slave700", Tags.Long)]
    [DeviceMapping(1, ChipSeries.ZW070x, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, ChipSeries.ZW070x, LibTypes.EndDeviceLib, Alias = "dut")]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class stressbasic_Flirs_Ctrl714_Slave700 : stressbasic_Flirs { }

    [ZatsTestCase("stressbasic_Flirs_Ctrl500_Slave700", Tags.Long)]
    [DeviceMapping(1, ChipSeries.ZW050x, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, ChipSeries.ZW070x, LibTypes.EndDeviceLib, Alias = "dut")]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class stressbasic_Flirs_Ctrl500_Slave700 : stressbasic_Flirs { }

    [ZatsTestCase("stressbasic_Flirs_Ctrl500_Slave500", Tags.Long)]
    [DeviceMapping(1, ChipSeries.ZW050x, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, ChipSeries.ZW050x, LibTypes.EndDeviceLib)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class stressbasic_Flirs_Ctrl500_Slave500 : stressbasic_Flirs { }

    [ZatsTestCase("stressbasic_Flirs_Ctrl700_Slave500", Tags.Long)]
    [DeviceMapping(1, ChipSeries.ZW070x, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, ChipSeries.ZW050x, LibTypes.EndDeviceLib)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class stressbasic_Flirs_Ctrl700_Slave500 : stressbasic_Flirs { }

    public class stressbasic_Flirs : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave = new Alias(2);

            bool is700 = Context.IsZW070x(Slave);
            int Count = 500;

            int delay = 117;
            if (is700)
            {
                delay = 2317;
            }

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
                ParallelService.SetDefault(Primary, Slave);
                Service.ApplicationNodeInformation(Slave, trun,
                    (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SWITCH_BINARY.ID });
                Service.SoftReset(Slave);
                BasicResult.IncludeNodeWithLearnMode addRes = null;
                for (int i = 0; i < 10; i++)
                {
                    addRes = Service.IncludeNode(Primary, Slave, 65000);
                    if (addRes && addRes.AddNodeResult && addRes.SetLearnMode)
                    {
                        break;
                    }
                    else
                    {
                        TestInterfaceService.SoftReset(Primary);
                        TestInterfaceService.SoftReset(Slave);
                        ParallelService.SetDefault(Primary, Slave);
                    }
                }
                AssertIsTrue(addRes, "Inclusion");
                if (is700)
                {
                    Delay(delay);
                    Service.PowerMgmtCancel(Slave, 0);
                }
                for (int i = 0; i < Count; i++)
                {
                    Delay(delay);
                    if (!is700)
                    {
                        Service.SetSleepMode(Slave, SleepModes.FrequentlyListening, 0);
                    }
                    BasicResult.SendData sendRes = Service.SendData(Primary, Slave, Frame);
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