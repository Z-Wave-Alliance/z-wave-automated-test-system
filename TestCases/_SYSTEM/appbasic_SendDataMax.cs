// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("SendDataMax - Payload sent over 4 hops is not corrupted at destination node (support sag 14222)")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    public class appbasic_SendDataMax : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Main = new Alias(1);
            Alias Second = new Alias(2);
            Alias[] ALL = new Alias[] { Main, Second };

            ReportService.LogText("Started");
            ParallelService.SetDefault(ALL);

            byte[] msg = new COMMAND_CLASS_BASIC.BASIC_SET(); //{ 0x20, 0x01 };
            const int dataMinLen = 40; //NOTE: It works with 128 and above
            const int dataMaxLen = 200; //NOTE: It works with 128 and above

            const int testRuns = 1;

            //1.Add DestinationNode and 3 to ControllerNode
            Service.AddNode(Main, Second);
            
            SendDataToMax(Main, Second, msg, dataMinLen,
                dataMaxLen, testRuns, TransmitOptions.Ack | TransmitOptions.AutoRoute, false);
        }

        private void SendDataToMax(Alias sourceNode, Alias destinationNode, byte[] command,
            byte minMsgLen, byte maxMsgLen, int testRuns, TransmitOptions txOptions, bool single)
        {
            if (command.Length < 2)
                ReportService.LogError("Command Message lengh < 2!");
            minMsgLen = minMsgLen < 3 ? (byte)3 : minMsgLen;
            maxMsgLen = maxMsgLen < minMsgLen ? minMsgLen : maxMsgLen;
            string type = single ? "SEND_DATA_SINGLE" : "SEND_DATA_MULTI";
            ReportService.LogText("Send Data to Max: " + txOptions.ToString() + ": " + type);
            bool isTransmitted = true;
            // 6.SendData with 3 byte payload (0)and check received message with 3 byte payload (0)
            ReportService.LogText("Testing up to max lenght: " + maxMsgLen + " testruns");
            for (int i = minMsgLen; i < maxMsgLen && isTransmitted; i++)
            {
                //create frame
                byte[] data = new byte[i];
                command.CopyTo(data, 0);
                for (int j = 2; j < i; j++)
                {
                    data[j] = command.Length > j ? command[j] : (byte)i;
                }

                for (int j = 0; j < testRuns; j++)
                {
                    Delay(500);
                    ReportService.LogText(sourceNode.ToString() + " sending " + i + " bytes payload, run #" + (j + 1));
                    BasicToken.ExpectData token = AsyncService.ExpectData(destinationNode, data);
                    // Wait 1st fail to get max payload lengh
                    using (Service.Settings.IsExpectingFailure.Set())
                    {
                        TransmitStatuses txStatus;
                        if (single)
                        {
                            txStatus = Service.SendData(sourceNode, destinationNode, data, txOptions).TransmitStatus;
                        }
                        else
                        {
                            txStatus = Service.SendDataMulti(sourceNode, new Alias[] { destinationNode }, data, txOptions).TransmitStatus;
                        }
                        BasicResult.ExpectData res = token.WaitCompletedSignal();
                        if (txStatus == TransmitStatuses.CompleteOk)
                        {
                            if (res)
                            {
                                ReportService.LogOk("DestinationNode:" + destinationNode.ToString() + " received " + i +
                                    " bytes payload, frame# " + (j + 1));
                            }
                            else
                            {
                                ReportService.LogWarning("DestinationNode:" + destinationNode.ToString() +
                                    " did not receive frame " + (j + 1));
                                isTransmitted = false;
                            }
                        }
                        else
                        {
                            ReportService.LogError(sourceNode.ToString() + " failed to tramsmit " +
                                i + " bytes payload, frame# " + (j + 1));
                            i = maxMsgLen;
                            j = testRuns;
                        }
                    }
                }
            }
        }

    }
}
