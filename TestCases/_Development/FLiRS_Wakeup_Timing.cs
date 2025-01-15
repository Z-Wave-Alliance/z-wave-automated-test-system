// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Diagnostics;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("Measure the time needed to beam a flirs node", Tags.Long)]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    public class FLiRS_Wakeup_Timing : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias PrimaryController = new Alias(1);
            Alias SlaveDest = new Alias(2);

            Alias[] ALL = new Alias[] { PrimaryController, SlaveDest };

            ReportService.LogText("Started");
            ParallelService.Reset(ALL);

            ReportService.LogText("1. Set SlaveDest to be a FLiRS node");
            Service.ApplicationNodeInformation(SlaveDest, DeviceOptions.FreqListeningMode1000ms,
                (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SWITCH_BINARY.ID });

            ParallelService.SetDefault(SlaveDest);

            ReportService.LogText("3 - Include SlaveDest using Primary controller");
            var addRes = Service.AddNode(PrimaryController, SlaveDest);
            AssertIsTrue(addRes, "Successfully added SlaveDest, Id: " + addRes.NodeId, "Unable to add SlaveDest");

            Stopwatch sw = new Stopwatch();
            System.Random randgen = new System.Random();

            for (int i = 0; i < 1000; i++)
            {
                ReportService.LogText("5 - Set SlaveDest in FLiRS sleep mode");
                Service.SetSleepMode(SlaveDest, SleepModes.FrequentlyListening, 0);
                int rdelay = 300 + randgen.Next(900);
                Delay(rdelay); //Let the FLiRS node go to sleep

                ReportService.LogText("9 - Send a basic set frame from PrimaryController to SlaveDest");
                //var eToken = AsyncService.ExpectData(SlaveDest, new COMMAND_CLASS_BASIC.BASIC_SET());
                sw.Start();
                var br = Service.SendData(PrimaryController, SlaveDest, new COMMAND_CLASS_BASIC.BASIC_SET(),TransmitOptions.Ack|TransmitOptions.NoRoute);
                //BasicResult.ExpectData br = eToken.WaitCompletedSignal();
                //AsyncService.CancelToken(SlaveDest, eToken);
                sw.Stop();
                TimeSpan ts = sw.Elapsed;

                // Format and display the TimeSpan value.
                string elapsedTime = String.Format("Wakuplatency: {0:00}{1:00}",
                    ts.Seconds,
                    ts.Milliseconds);
                //var elapsed = sw.ElapsedMilliseconds;
                sw.Reset();
                //AssertIsTrue(br, "BASIC_SET detected", "BASIC_SET not detected");
                if (!br)
                {
                    //Workaround for waking up a node
                    //Service.SendData(PrimaryController, SlaveDest, new byte[] { 0x20, 0x01 });
                    ReportService.LogText("SendData failed");
                    elapsedTime = elapsedTime + " #failed";
                }

                ReportService.LogText(elapsedTime);
            }
        }
    }
}
