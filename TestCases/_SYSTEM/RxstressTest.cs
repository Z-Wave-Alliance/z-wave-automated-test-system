// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;
using Zats.Services;
using System.Threading;
using System.Linq;
using System;
using System.Net;

namespace ZatsTests
{
    [ZatsTestCase("Zip Gateway - Transport Service memory leak")]
    [ZipDeviceMapping(1, "z2")]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]

    public class RxstressTest : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            int numReports = 100;

            #region Aliases
            Alias ZipController = new Alias(1);
            Alias Controller1 = new Alias(2);

            Alias ZipAssociatedDestination = new Alias(5);
            Alias ZipCtrlUnsolDestination = new Alias(6);

            Alias[] ALL = new Alias[] { Controller1 };
            #endregion

            #region Reset

            ZipService.SetDefault(ZipController);

            ParallelService.Reset(ALL);

            #endregion

            #region Options

            ZipApplicationService.AutoConfigUnsolicitedDestination(ZipController, ZipAssociatedDestination);

            //ApplicationService.SetApplicationCmd(Controller1, false, false, LibTypes.ControllerStaticLib);

            #endregion

            byte Controller1Id;

            #region 1.- Include PC Controller 1, 2 and the slave to GW network

            ReportService.LogTitle("1.- Include PC Controller 1, 2 and the slave to GW network");

            ReportService.LogText("---- Include Controller 1 ----");

            //var ZipAddRes = ZipService.AddNodeWithDSK(ZipController, Controller1);
            var ZipAddExpRes = ZipAsyncService.AddNode(ZipController, 60000);
            var SetLearnExpRes = AsyncService.SetLearnMode(Controller1, LearnModes.LearnModeClassic, 60000);

            var ZipAddRes = ZipAddExpRes.WaitCompletedSignal();
            SetLearnExpRes.WaitCompletedSignal();
            Controller1Id = ZipAddRes.NodeId;

            Inconclusive(ZipAddRes.Status == 0x06, "Inclusion Successful", "Inclusion Failed");

            ZipService.RegisterZipDevice(ZipController, Controller1, Controller1Id);

            #endregion

            //ZipService.RegisterUnsolicitedZipDevice(ZipCtrlUnsolDestination, 
            //    new IPEndPoint(ZipService.GetIpAddress(ZipController, Controller1).IpAddress, 41230));
            Random RandomGenerator = new Random();

            #region 2.- Sending frames to 700s GW with payload 20 - 30 - 40

            int[] PayloadLenght = new int[] { 4, 40 };
            float[] FailContainer = new float[PayloadLenght.Length];
            float[] FailContainerRx = new float[PayloadLenght.Length];

            BasicToken.SendData SendExpRes = null;
            BasicToken.ExpectData DataExpRes = null;
            ZipToken.ExpectData ZipExpRes = null;
            ZipToken.SendData ZipSendExpRes = null;
            ZipResult.SendData ZipSendRes = null;

            //Delay(5000);
            byte[] Payload = new byte[PayloadLenght[0]];
            RandomGenerator.NextBytes(Payload);
            //Payload[0] = COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID;
            //Payload[1] = COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_GET.ID;
            //Payload[0] = COMMAND_CLASS_BASIC_V2.ID;
            //Payload[1] = COMMAND_CLASS_BASIC_V2.BASIC_REPORT.ID;

            //SendExpRes = AsyncService.SendData(Controller1, 0x01, Payload, TransmitOptions.Ack | TransmitOptions.AutoRoute | TransmitOptions.Explore);
            //Delay(5000);

            for (int indi = 0; indi < PayloadLenght.Length; indi++)
            {
                ReportService.LogTitle(" -------------- Running payload length of: " + PayloadLenght[indi].ToString() + " --------------");

                Payload = new byte[PayloadLenght[indi]];

                FailContainer[indi] = 0;
                FailContainerRx[indi] = 0;

                for (int indy = 2; indy < numReports; indy++)
                {
                    #region Rx Case
                    //RandomGenerator.NextBytes(Payload);
                    //Payload[0] = COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID;
                    //Payload[1] = COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_GET.ID;

                    //DataExpRes = AsyncService.ExpectData(Controller1, new COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_REPORT(), 3000);

                    //timeStamp = DateTime.Now.ToLongDateString();
                    //SendExpRes = AsyncService.SendData(Controller1, 0x01, Payload, TransmitOptions.Ack|TransmitOptions.AutoRoute|TransmitOptions.Explore);

                    //SendRes = SendExpRes.WaitCompletedSignal();

                    //if(!SendRes.HasTxTransmitReport)
                    //{
                    //    ReportService.LogError(timeStamp);
                    //    Inconclusive("Failed to deliver frame with following payload: " + Payload.ToHexString());
                    //    FailContainer[indi]++;
                    //}

                    //if(!DataExpRes.WaitCompletedSignal())
                    //{                        
                    //    ReportService.LogError(timeStamp);
                    //    Inconclusive("Failed to receive Powerlevel Report from frame with payload: " + Payload.ToHexString());
                    //    FailContainerRx[indi]++;
                    //}

                    //SendExpRes.Dispose();
                    //Delay(300);
                    #endregion

                    #region Tx Case
                    RandomGenerator.NextBytes(Payload);
                    Payload[0] = COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID;
                    Payload[1] = COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_GET.ID;

                    ZipExpRes = ZipAsyncService.ExpectData(Controller1, new COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_REPORT(), 3000);
                    DataExpRes = AsyncService.ExpectData(Controller1, new COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_GET(), 3000);

                    TimeSpan timeStamp = DateTime.Now.TimeOfDay;
                    ZipSendExpRes = ZipAsyncService.SendData(Controller1, Payload, 3000);

                    ZipSendRes = ZipSendExpRes.WaitCompletedSignal();

                    if (DataExpRes.WaitCompletedSignal())
                    {
                        Payload[0] = COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID;
                        Payload[1] = COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_REPORT.ID;
                        SendExpRes = AsyncService.SendData(Controller1, DataExpRes.WaitCompletedSignal().NodeId,
                            Payload, TransmitOptions.Explore);
                    }
                    if (ZipSendRes.IsNAckReceived)
                    {
                        ReportService.LogError(timeStamp.ToString());
                        Inconclusive("Failed to deliver frame with following payload: " + Payload.ToHexString());
                        FailContainer[indi]++;
                    }

                    if (!ZipExpRes.WaitCompletedSignal())
                    {
                        ReportService.LogError(timeStamp.ToString());
                        Inconclusive("Failed to receive Powerlevel Report from frame with payload: " + Payload.ToHexString());
                        FailContainerRx[indi]++;
                    }

                    DataExpRes.Dispose();
                    ZipSendExpRes.Dispose();
                    ZipExpRes.Dispose();

                    Delay(200);
                    #endregion
                }

                Delay(10000);
            }

            #endregion

            float[] ResultContainer = new float[FailContainer.Length];

            for (int indi = 0; indi < PayloadLenght.Length; indi++)
            {
                ReportService.LogText("Percentage of error on payload lenght " + PayloadLenght[indi]);
                ResultContainer[indi] = (FailContainer[indi] / ((float)numReports)) * 100;
                ReportService.LogError(ResultContainer[indi].ToString() + "%");
            }

            for (int indi = 0; indi < PayloadLenght.Length; indi++)
            {
                ReportService.LogText("Percentage of error on payload lenght " + PayloadLenght[indi]);
                ResultContainer[indi] = (FailContainerRx[indi] / ((float)numReports)) * 100;
                ReportService.LogError(ResultContainer[indi].ToString() + "%");
            }
        }
    }
}
