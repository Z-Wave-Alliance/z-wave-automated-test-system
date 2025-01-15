// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("RSSI level measured in SendData and in ApplicationCommandHandler on SlaveEnhanced ",
        Tags.Long,
        Tags.DEV)]
    [DeviceMapping(1, LibTypes.ControllerPortableLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
   // [DeviceMapping(2, "uzb1", LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.EndDeviceAny)]
    [DeviceMapping(4, LibTypes.EndDeviceAny)]
    [DeviceMapping(5, LibTypes.EndDeviceAny)]
    [DeviceMapping(6, LibTypes.EndDeviceAny)]
    [DeviceMapping(7, LibTypes.EndDeviceAny)]
    public class RSSIstress : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias DevelopmentController = new Alias(1);
            Alias StaticController = new Alias(2);
            Alias SlaveNode1 = new Alias(3);
            Alias SlaveNode2 = new Alias(4);
            Alias SlaveNode3 = new Alias(5);
            Alias SlaveNode4 = new Alias(6);
            Alias SlaveNode5 = new Alias(7);
            Alias[] ALL = { DevelopmentController, StaticController, SlaveNode1, SlaveNode2, SlaveNode3, SlaveNode4, SlaveNode5 };

            ReportService.LogText("Started");
            ParallelService.SetDefault(ALL);

            ReportService.LogText("1. Add SlaveNodes 1 and 2 with Portable Controller.");
            //Add SlaveNode1
            BasicResult.AddNode addRess = Service.AddNode(StaticController, SlaveNode1);
            AssertIsTrue(addRess, "Successfully added SlaveNode1: " + addRess.NodeId, "Unable to add SlaveNode1");
            Delay(10000);
            Service.MemoryGetId(SlaveNode1);

            //Add SlaveNode2
            addRess = Service.AddNode(StaticController, SlaveNode2);
            AssertIsTrue(addRess, "Successfully added SlaveNode2: " + addRess.NodeId, "Unable to add SlaveNode2");
            Delay(10000);
            Service.MemoryGetId(SlaveNode2);

            ReportService.LogText("2. Add SlaveNodes 3 with Portable Controller.");
            //Add SlaveNode3
            addRess = Service.AddNode(StaticController, SlaveNode3);
            AssertIsTrue(addRess, "Successfully added SlaveNode3: " + addRess.NodeId, "Unable to add SlaveNode3");
            Delay(10000);
            Service.MemoryGetId(SlaveNode3);


            ReportService.LogText("3. Add SlaveNodes 4 with Portable Controller.");
            //Add SlaveNode4
            addRess = Service.AddNode(StaticController, SlaveNode4);
            AssertIsTrue(addRess, "Successfully added SlaveNode4: " + addRess.NodeId, "Unable to add SlaveNode4");
            Delay(10000);
            Service.MemoryGetId(SlaveNode4);


            ReportService.LogText("4. Add SlaveNodes 5 with Portable Controller.");
            //Add SlaveNode5
            addRess = Service.AddNode(StaticController, SlaveNode5);
            AssertIsTrue(addRess, "Successfully added SlaveNode5: " + addRess.NodeId, "Unable to add SlaveNode5");
            Delay(10000);
            Service.MemoryGetId(SlaveNode5);




            //6. Assign Priority Return Route between Slave nodeID5 and Controller nodeID1 by MainController.
            ReportService.LogText("6. Assign Priority Return Route between Slave nodeID5 and Controller nodeID1 by MainController.");
            byte speed = 3; // for 100kbps (9,6kbps = 1; 40kbps = 2)
           
            BasicResult.SetPriorityRoute response = Service.SetPriorityRoute(StaticController, SlaveNode5, 0x02, 0x03, 0x04, 0x05, speed);
            int rep = 100000;
            int countACK_0 = 0;
            int countACK_1 = 0;
            int countACK_2 = 0;

            //Repeat this step 1000 times
            for (int j = 0; j < rep; j++)
            {
                ReportService.LogText("5. Send Basic Set frame to the SlaveNode1 from the StaticController");
            
                var res = Service.SendData(StaticController,SlaveNode5, new COMMAND_CLASS_BASIC.BASIC_GET(), TransmitOptions.Ack );
            
         
                ReportService.LogText("6. Read RSSI value on SlaveNode1 in CmdApplicationCommandHandler. Read RSSI value in Routed ACK frame from last repeater. Read RSSI value in SendData callback.");


                sbyte[] RSSI = res.RssiValuesIncoming;//index rssi value returned by SendData callback
                int AckChannel = res.AckChannelNo;
                if (AckChannel == 0)
                {
                    ReportService.LogText("ACK Channel field in SendData callback returns 0");
                    countACK_0++;
                }
                else if(AckChannel == 1)
                {
                    ReportService.LogText("ACK Channel field in SendData callback returns 1");
                    countACK_1++;
                }
                else
                {
                    ReportService.LogText("ACK Channel field in SendData callback returns 2");
                    countACK_2++;
                }

                if (res.TransmitStatus == TransmitStatuses.CompleteOk)
                {

                    int repeaters = res.RepeatersCount;
                    ReportService.LogText("Number of repeaters " + repeaters);

                    for (int b = 0; b < (repeaters + 1); b++)
                    {
                        ReportService.LogText("index rssi value returned by SendData callback for the  hop: " + b + "  " + RSSI[b]);

                        AssertIsTrue(RSSI[b] != 127, "Index rssi value returned by SendData callback for the hop " + b + " is not empty and Number of repeaters " + repeaters + "  ", "Index rssi value returned by SendData callback for the hop " + b + " is empty and Number of repeaters " + repeaters + " ");
                    }
                }
                else
                    Inconclusive(res.TransmitStatus == TransmitStatuses.CompleteOk, "SendData return OK", "SendData fails");

            }

            AssertIsTrue(countACK_0 != rep, "ACK Channel field in SendData callback returns different channels, countACK_0=" + countACK_0 + ", countACK_1 = " + countACK_1 + ", countACK_2 = " + countACK_2 ,
                "ACK Channel field in SendData callback returns only 0");


        }
    }
}
