// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System.Linq;
using System.Threading.Tasks;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("stress test FLiRS",
        Tags.Long)]
    [DeviceMapping(1, ChipSeries.ZW070x, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, ChipSeries.ZW070x, LibTypes.EndDeviceLib)]
    [DeviceMapping(3, ChipSeries.ZW070x, LibTypes.EndDeviceLib)]
    [DeviceMapping(4, ChipSeries.ZW070x, LibTypes.EndDeviceLib)]
    [DeviceMapping(5, ChipSeries.ZW070x, LibTypes.ControllerStaticLib)]
    [DeviceMapping(6, ChipSeries.ZW070x, LibTypes.EndDeviceLib)]
    [DeviceMapping(7, ChipSeries.ZW070x, LibTypes.EndDeviceLib)]
    [DeviceMapping(8, ChipSeries.ZW070x, LibTypes.EndDeviceLib)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class stressbasic_Flirs2Networks : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias PrimaryA = new Alias(1);
            Alias[] SlavesA = new[] { new Alias(2), new Alias(3), new Alias(4) };
            Alias PrimaryB = new Alias(5);
            Alias[] SlavesB = new[] { new Alias(6), new Alias(7), new Alias(8) };

            int Count = 500000;
            byte[] FrameA = "820133".ToByteArray();
            byte[] FrameB = "200333".ToByteArray();
            int delayA = 113;
            int delayB = 173;

            ParallelService.SetDefault(PrimaryA, PrimaryB);
            ParallelService.SetDefault(SlavesA);
            ParallelService.SetDefault(SlavesB);

            Service.WatchDogStart(PrimaryA);
            Service.WatchDogStart(PrimaryB);

            foreach (var slave in SlavesA.Concat(SlavesB))
            {
                Service.ApplicationNodeInformation(slave, DeviceOptions.FreqListeningMode1000ms,
                    (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SWITCH_BINARY.ID });
                Service.SoftReset(slave);
            }

            foreach (var slave in SlavesA)
            {
                var addRes = Service.IncludeNode(PrimaryA, slave, 65000);
                AssertIsTrue(addRes, "Inclusion A");
                Delay(1000);
            }

            foreach (var slave in SlavesB)
            {
                var addRes = Service.IncludeNode(PrimaryB, slave, 65000);
                AssertIsTrue(addRes, "Inclusion B");
                Delay(1000);

            }

            foreach (var slave in SlavesA.Concat(SlavesB))
            {
                Service.PowerMgmtCancel(slave, 0);
            }

            var taskA = Task.Run(() =>
            {
                DoWork(PrimaryA, SlavesA, Count, FrameA, delayA, "network A", true);
            });
            var taskB = Task.Run(() =>
            {
                DoWork(PrimaryB, SlavesB, Count, FrameB, delayB, "network B", false);
            });

            taskA.Wait();
            taskB.Wait();
        }

        private void DoWork(Alias primary, Alias[] slaves, int count, byte[] frame, int delay, string network, bool waitSleepTimeout)
        {
            int countError = 0;
            int countTransmitOK = 0;
            int countTransmitTotal = 0;
            int countTicksOK = 0;
            int countTicksTotal = 0;

            for (int i = 0; i < count; i++)
            {
                ReportService.LogText($"next round {network}");
                 if (waitSleepTimeout)
                {
                    Delay(2113 - delay * 2);
                }
                foreach (var slave in slaves)
                {
                    Delay(delay);
                    var sendRes = Service.SendData(primary, slave, frame);
                    if (sendRes)
                    {
                        countTransmitTotal++;
                        countTicksTotal += sendRes.TransmitTicks;
                        if (sendRes.TransmitStatus == TransmitStatuses.CompleteOk)
                        {
                            countTransmitOK++;
                            countTicksOK += sendRes.TransmitTicks;
                        }
                    }
                    else
                    {
                        countError++;
                    }
                }
                if (i % 100 == 0)
                {
                    ReportService.LogInfo($"={network} countError={countError}, countTransmitOK={countTransmitOK}, countTransmitTotal={countTransmitTotal}, countTicksOK={countTicksOK}, countTicksTotal={countTicksTotal}");
                }
            }
        }
    }
}