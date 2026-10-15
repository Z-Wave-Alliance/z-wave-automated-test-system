// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;
using System.Linq;

/*
 * Z-Wave RF driver RAIL integration validation test Suite
 * VVVVVVVVVVVV
 * Transmit Z-Wave frames:
 * - NodeA (compiled using RAIL library Under Test - RUT) transmit singlecast, multicast and explorer frames to
 *   NodeB (compiled using latest RAIL library Accepted for Zwave usage - RAZ).
 * Test Regions:
 * - REGION_EU, REGION_US and REGION_KR
 * Verification:
 * - NodeB must be able to receive and correctly parse the transmitted frames
 * VVVVVVVVVVVV
 * VVVVVVVVVVVV
 * Receive Z-Wave frames:
 * - NodeB (compiled using RAZ) transmit singlecast, multicast and explorer frames to NodeA (compiled using RUT).
 * Test Regions:
 * - Done in REGION_EU, REGION_US and REGION_KR
 * Verification:
 * - NodeA must be able to receive and correctly parse the transmitted frames.
 * VVVVVVVVVVVV
 * 
 * FLiRS average Idle Power consumption:
 * - Measure the avarage power consumption of an included FLiRS node (compiled using RUT) when FLiRS node is in 
 *   FLiRS mode/Idle (no receive or transmit). 
 * Test Regions:
 * - Done in REGION_EU, REGION_US and REGION_KR	
 * Verification:
 * - For REGION_EU and REGION_US average power consumption no higher than ???. 
 *   For REGION_KR average power consumption no higher than ???
 * 
 * Transmit 2 channel Wakeup BEAM
 * - NodeA (compiled using RUT) transmit frame with BEAM to offline FLiRS node. 
 * Test Regions:
 * - Done in REGION_EU and REGION_US
 * Verification:
 * - In PC Zniffer can be seen a 1.1 second BEAM (230 BEAM fragments back to back) followed by the frame
 * 
 * Transmit 3 channel Wakeup BEAM
 * - NodeA (compiled using RUT) transmit frame with BEAM to offline FLiRS node.
 * Test Regions:
 * - Done in REGION_KR
 * Verification:
 * - 3 seconds of fragmented BEAM - 15 periods, where 1 period is defined as 100ms BEAM followed by 100ms of silence
 * 
 * Receive 2 channel BEAM in FLiRS mode
 * - NodeB (compiled using RAZ) transmit a frame with BEAM to a FLiRS node NodeA (compiled using RAIL library Under Test - RUT). 
 * Test Regions:
 * - Done in regions REGION_EU and REGION_US
 * Verification:
 * - NodeA wakes up and acknowledges the transmitted frame and is able to correctly parse the received frame
 * 
 * Receive 3 channel BEAM in FLiRS mode
 * - NodeB (compiled using RAZ) transmit a frame with BEAM to a FLiRS node NodeA (compiled using RAIL library Under Test - RUT). 
 * Test Regions:
 * - Done in REGION_KR
 * Verification:
 * - NodeA wakes up and acknowledges the fragmented BEAM and also acknowledges the transmitted frame and is able to 
 *   correctly parse the received frame
 * 
 * Ignore 2 channel BEAM from foreign Network
 * - NodeB (compiled using RAZ) transmit frame with BEAM to FliRS node xy. 
 *   NodeA (compiled using RUT) has nodeID xy, but are included in another network than NodeB. 
 * Test Regions:
 * - Done in REGION_EU and REGION_US
 * Verification:
 * - NodeA ignores the transmitted BEAM directed to the node with the same nodeID but on foreign Network
 * 
 * Z-Wave system stability test 1
 * - NodeA Controller (compiled using RUT) transmit 1000000 frames to NodeC Slave (compiled using RUT). 
 * Test Regions:
 * - Done in REGION_EU, REGION_US and REGION_KR
 * Verification:
 * - NodeA and NodeC are both still operable and functioning
 * 
 * Z-Wave system stability test 2
 * - NodeA Controller (compiled using RUT) transmit 100000 frames to NodeC FLiRS Slave (compiled using RUT). 
 * Test Regions:
 * - Done in REGION_EU, REGION_US and REGION_KR
 * Verification:
 * - NodeA and NodeC are both still operable and functioning
 * 
 */
namespace Zats
{
    [ZatsTestCase("protocolRailIntegrationTest - Z-Wave protocol RAIL integration Test Suite ")]
    // 500 series ControllerBridge and SlaveEnhanced from SDK 6_81
    //[DeviceMapping(1, ChipSeries.ZW050x, LibTypes.ControllerBridgeLib, FileNameFilter = "6_81")]
    //[DeviceMapping(2, ChipSeries.ZW050x, LibTypes.EndDevice, FileNameFilter = "6_81")]
    //[DeviceMapping(3, ChipSeries.ZW050x, LibTypes.EndDevice, FileNameFilter = "6_81")]
    // 700 series ControllerBridge and SlaveEnhanced compiled using RAIL library Under Test - RUT
    [DeviceMapping(4, ChipSeries.ZGM130S_BRD4207A, LibTypes.ControllerBridgeLib, FileNameFilter = "RUT")]
    [DeviceMapping(5, ChipSeries.ZGM130S_BRD4207A, LibTypes.EndDeviceLib, FileNameFilter = "_RUT")]
    [DeviceMapping(6, ChipSeries.ZGM130S_BRD4207A, LibTypes.EndDeviceLib, FileNameFilter = "_RUT")]
    // 700 series ControllerBridge and SlaveEnhanced compiled using latest RAIL library Accepted for Z-Wave usage - RAZ
    [DeviceMapping(7, ChipSeries.ZGM130S_BRD4207A, LibTypes.ControllerBridgeLib, FileNameFilter = "_RAZ")]
    [DeviceMapping(8, ChipSeries.ZGM130S_BRD4207A, LibTypes.EndDeviceLib, FileNameFilter = "_RAZ")]
    [DeviceMapping(9, ChipSeries.ZGM130S_BRD4207A, LibTypes.EndDeviceLib, FileNameFilter = "_RAZ")]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class protocolRailIntegrationTest : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            //Alias CtrlBridge_500 = new Alias(1);
            //Alias FLiRS1000ms_500 = new Alias(2);
            //Alias FLiRS250ms_500 = new Alias(3);
            Alias CtrlBridge_700_RUT = new Alias(4);
            Alias FLiRS1000ms_700_RUT = new Alias(5);
            Alias FLiRS250ms_700_RUT = new Alias(6);
            Alias SlaveEnhanced1_700_RUT = new Alias(5);
            Alias SlaveEnhanced2_700_RUT = new Alias(6);
            Alias CtrlBridge_700_RAZ = new Alias(7);
            Alias FLiRS1000ms_700_RAZ = new Alias(8);
            Alias FLiRS250ms_700_RAZ = new Alias(9);
            Alias SlaveEnhanced1_700_RAZ = new Alias(8);
            Alias SlaveEnhanced2_700_RAZ = new Alias(9);

            Alias[] ALL = new Alias[] { /*CtrlBridge_500, FLiRS1000ms_500, FLiRS250ms_500,*/ CtrlBridge_700_RUT, SlaveEnhanced1_700_RUT, SlaveEnhanced2_700_RUT, /*FLiRS1000ms_700_RUT, FLiRS250ms_700_RUT,*/ CtrlBridge_700_RAZ, SlaveEnhanced1_700_RUT, SlaveEnhanced2_700_RUT/*, FLiRS1000ms_700_RAZ, FLiRS250ms_700_RAZ*/ };

            ReportService.LogText("Started");

            // Set 500 series FLiRS 1000ms and FLiRS 250ms nodes to be FLIRS node
            //Service.ApplicationNodeInformation(FLiRS1000ms_500, DeviceOptions.FreqListeningMode1000ms,
            //    (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
            //    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SWITCH_BINARY.ID });

            //Service.ApplicationNodeInformation(FLiRS250ms_500, DeviceOptions.FreqListeningMode250ms,
            //    (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
            //    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SWITCH_BINARY.ID });

            // Set 700 series RUT FLiRS 1000ms and FLiRS 250ms nodes to be FLIRS node
            //Service.ApplicationNodeInformation(FLiRS1000ms_700_RUT, DeviceOptions.FreqListeningMode1000ms,
            //    (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
            //    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SWITCH_BINARY.ID });

            //Service.ApplicationNodeInformation(FLiRS250ms_700_RUT, DeviceOptions.FreqListeningMode250ms,
            //    (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
            //    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SWITCH_BINARY.ID });

            // Set 700 series RAZ FLiRS 1000ms and FLiRS 250ms nodes to be FLIRS node
            //Service.ApplicationNodeInformation(FLiRS1000ms_700_RAZ, DeviceOptions.FreqListeningMode1000ms,
            //    (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
            //    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SWITCH_BINARY.ID });

            //Service.ApplicationNodeInformation(FLiRS250ms_700_RAZ, DeviceOptions.FreqListeningMode250ms,
            //    (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
            //    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SWITCH_BINARY.ID });

            ParallelService.Reset(ALL);

            /*
             * Transmit Z-Wave frames:
             * - NodeA(compiled using RAIL library Under Test -RUT) transmit singlecast, multicast and explorer frames to
             *   NodeB(compiled using latest RAIL library Accepted for Zwave usage -RAZ).
             * 
             * Test Regions:
             * - REGION_EU, REGION_US and REGION_KR
             * 
             * Verification:
             * - NodeB must be able to receive and correctly parse the transmitted frames
             * 
             */

            ReportService.LogText("1. Add Slave_700_RAZ to CtrlBridge_700_RUT ");
            BasicResult.IncludeNodeWithLearnMode inclRes = Service.IncludeNode(CtrlBridge_700_RUT, SlaveEnhanced1_700_RAZ);

            //ReportService.LogText("4.Turn off the receiver on FLiRS1");
            //Service.SetRFReceiveMode(FLiRS1, false);

            Delay(2000);
            //if (is700)
            //{
            //    Service.PowerMgmtCancel(FLiRS1, 0x00);
            //    Service.PowerMgmtCancel(FLiRS2, 0x00);
            //}
            //else
            //{
            //    Service.SetSleepMode(FLiRS1, SleepModes.FrequentlyListening, 0);
            //    Service.SetSleepMode(FLiRS2, SleepModes.FrequentlyListening, 0);
            //}

            ReportService.LogText("Send a Singlecast frame from CtrlBridge_700_RUT to Slave_700_RAZ");
            //ZnifferToken.ExpectData ztW = ZnifferService.ExpectData(5000);
            //ztW.SetUpWatch(null, HeaderTypes.WAKEUP_BEAM, false);
            ZnifferToken.ExpectData ztB = ZnifferService.ExpectData(5000);
            ztB.SetUpWatch(1, inclRes.AddNodeResult.Node, new COMMAND_CLASS_BASIC.BASIC_SET() { value = 0xFF }, HeaderTypes.SINGLECAST, true);
            ZnifferToken.ExpectData ztR = ZnifferService.ExpectData(5000);
            ztR.SetUpWatch(inclRes.AddNodeResult.Node, 1, null, HeaderTypes.TRANSFER_ACKNOWLEDGE, false);
            byte[] cmdBasicSet = new COMMAND_CLASS_BASIC.BASIC_SET() { value = 0xFF };
            var expectToken = AsyncService.ExpectData(SlaveEnhanced1_700_RAZ, new COMMAND_CLASS_BASIC.BASIC_SET());
            Service.SendData(CtrlBridge_700_RUT, SlaveEnhanced1_700_RAZ, cmdBasicSet, TransmitOptions.Ack | TransmitOptions.AutoRoute);
            //ZnifferResult.ExpectData zrW = ztW.WaitCompletedSignal();
            var expectResult = expectToken.WaitCompletedSignal();
            ZnifferResult.ExpectData zrB = ztB.WaitCompletedSignal();
            ZnifferResult.ExpectData zrR = ztR.WaitCompletedSignal();

            Inconclusive(expectResult.Command.SequenceEqual(cmdBasicSet), "Pass", "Fail");
            //Inconclusive(zrW,
            //    "9. Slave1 sends out a 1000ms wakeup beam before repeating the frame",
            //    "9. Slave1 didn't sends out a 1000ms wakeup beam before repeating the frame");
            Inconclusive(zrB,
               "Frame is transmitted singlecast from CtrlBridge_700_RUT to Slave_700_RAZ ",
               "NO Frame is transmitted singlecast from CtrlBridge_700_RUT to Slave_700_RAZ ");
            Inconclusive(zrR,
               "Singlcast ACK transmitted from Slave_700_RAZ to CtrlBridge_700_RUT detected",
               "NO Singlcast ACK transmitted from Slave_700_RAZ to CtrlBridge_700_RUT detected");

            /*
             * Receive Z-Wave frames:
             * - NodeB(compiled using RAZ) transmit singlecast, multicast and explorer frames to NodeA(compiled using RUT).
             * 
             * Test Regions:
             * - Done in REGION_EU, REGION_US and REGION_KR
             * 
             * Verification:
             * - NodeA must be able to receive and correctly parse the transmitted frames.
             * 
             */

            ztB = ZnifferService.ExpectData(5000);
            ztB.SetUpWatch(inclRes.AddNodeResult.Node, 1, new COMMAND_CLASS_BASIC.BASIC_SET() { value = 0xFF }, HeaderTypes.MULTICAST, false);
            expectToken = AsyncService.ExpectData(CtrlBridge_700_RUT, new COMMAND_CLASS_BASIC.BASIC_SET());
            Service.SendDataMulti(SlaveEnhanced1_700_RAZ, new Alias[] { CtrlBridge_700_RUT }, cmdBasicSet, 0);
            //ZnifferResult.ExpectData zrW = ztW.WaitCompletedSignal();
            expectResult = expectToken.WaitCompletedSignal();
            zrB = ztB.WaitCompletedSignal();

            Inconclusive(expectResult.Command.SequenceEqual(cmdBasicSet),
                         "Multicast Basic Set frame transmitted from Slave_700_RAZ was received by CtrlBridge_700_RUT",
                         "NO Multicast Basic Set frame transmitted from Slave_700_RAZ was received by CtrlBridge_700_RUT");
            Inconclusive(zrB,
               "Multicast Frame is transmitted from Slave_700_RAZ to CtrlBridge_700_RUT ",
               "NO Multicast Frame is transmitted from Slave_700_RAZ to CtrlBridge_700_RUT");

            // Reset 
            ParallelService.Reset(ALL);

            if (Context.ChannelConfig == ChannelConfigs.Ch3)
            {
                // Set 500 series FLiRS Fragemented BEAM nodes to be FLIRS node
                //Service.ApplicationNodeInformation(FLiRS1000ms_500, DeviceOptions.FreqListeningMode1000ms,
                //    (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                //    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SWITCH_BINARY.ID });

                // Set 700 series RUT FLiRS Fragemented BEAM nodes to be FLIRS node
                Service.ApplicationNodeInformation(FLiRS1000ms_700_RUT, DeviceOptions.FreqListeningMode1000ms,
                    (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SWITCH_BINARY.ID });

                // Set 700 series RAZ FLiRS Fragemented BEAM nodes to be FLIRS node
                Service.ApplicationNodeInformation(FLiRS1000ms_700_RAZ, DeviceOptions.FreqListeningMode1000ms,
                    (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SWITCH_BINARY.ID });
            }
            else if (Context.ChannelConfig == ChannelConfigs.Ch12)
            {
                // Set 500 series FLiRS 1000ms and FLiRS 250ms nodes to be FLIRS node
                //Service.ApplicationNodeInformation(FLiRS1000ms_500, DeviceOptions.FreqListeningMode1000ms,
                //    (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                //    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SWITCH_BINARY.ID });

                //Service.ApplicationNodeInformation(FLiRS250ms_500, DeviceOptions.FreqListeningMode250ms,
                //    (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                //    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SWITCH_BINARY.ID });

                // Set 700 series RUT FLiRS 1000ms and FLiRS 250ms nodes to be FLIRS node
                Service.ApplicationNodeInformation(FLiRS1000ms_700_RUT, DeviceOptions.FreqListeningMode1000ms,
                    (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SWITCH_BINARY.ID });

                Service.ApplicationNodeInformation(FLiRS250ms_700_RUT, DeviceOptions.FreqListeningMode250ms,
                    (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SWITCH_BINARY.ID });

                // Set 700 series RAZ FLiRS 1000ms and FLiRS 250ms nodes to be FLIRS node
                Service.ApplicationNodeInformation(FLiRS1000ms_700_RAZ, DeviceOptions.FreqListeningMode1000ms,
                    (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SWITCH_BINARY.ID });

                Service.ApplicationNodeInformation(FLiRS250ms_700_RAZ, DeviceOptions.FreqListeningMode250ms,
                    (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SWITCH_BINARY.ID });
            }
            ParallelService.Reset(ALL);

            if (Context.ChannelConfig == ChannelConfigs.Ch12)
            {
                /*
                 * Receive 2 channel BEAM in FLiRS mode
                 * - NodeB(compiled using RAZ) transmit a frame with BEAM to a FLiRS node NodeA(compiled using RAIL library Under Test -RUT). 
                 * 
                 * Test Regions:
                 * - Done in regions REGION_EU and REGION_US
                 * 
                 * Verification:
                 * - NodeA wakes up and acknowledges the transmitted frame and is able to correctly parse the received frame
                 */

                inclRes = Service.IncludeNode(CtrlBridge_700_RAZ, SlaveEnhanced1_700_RUT);

                //ReportService.LogText("Send a frame from Ctrl2 to FLiRS2");
                //ztW = ZnifferService.ExpectData(5000);
                //ztW.SetUpWatch(null, HeaderTypes.WAKEUP_BEAM, false);
                //ztB = ZnifferService.ExpectData(5000);
                //ztB.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_SET() { value = 0xFF }, HeaderTypes.ROUTED_SINGLECAST, true);
                //ztR = ZnifferService.ExpectData(5000);
                //ztR.SetUpWatch(0x04, null, null, HeaderTypes.ROUTED_ACKNOWLEDGE, false);
                //Service.SendData(Ctrl2, FLiRS2, new COMMAND_CLASS_BASIC.BASIC_SET() { value = 0xFF }, TransmitOptions.Ack | TransmitOptions.AutoRoute);
                //zrW = ztW.WaitCompletedSignal();
                //zrB = ztB.WaitCompletedSignal();
                //zrR = ztR.WaitCompletedSignal();
                //Inconclusive(zrW,
                //    "10. Slave1 sends out a 1000ms wakeup beam before repeating the frame",
                //    "10. Slave1 didn't sends out a 1000ms wakeup beam before repeating the frame");
                //Inconclusive(zrB,
                //   "10. Frame is routed through Ctrl2 -> Slave1 -> FLiRS2 ",
                //   "10. Frame is not routed through Ctrl2 -> Slave1 -> FLiRS2 ");
                //Inconclusive(zrR,
                //   "10. Routed ACK frame detected",
                //   "10. Routed ACK frame not detected");

                /* 
                    * Ignore 2 channel BEAM from foreign Network
                    * - NodeB(compiled using RAZ) transmit frame with BEAM to FliRS node xy. 
                    *   NodeA(compiled using RUT) has nodeID xy, but are included in another network than NodeB. 
                    * 
                    * Test Regions:
                    * - Done in REGION_EU and REGION_US
                    * 
                    * Verification:
                    * - NodeA ignores the transmitted BEAM directed to the node with the same nodeID but on foreign Network
                    */
            }
            else if (Context.ChannelConfig == ChannelConfigs.Ch3)
            {

                /* 
                    * Receive 3 channel BEAM in FLiRS mode
                    * - NodeB(compiled using RAZ) transmit a frame with BEAM to a FLiRS node NodeA(compiled using RAIL library Under Test -RUT). 
                    * 
                    * Test Regions:
                    * - Done in REGION_KR
                    * 
                    * Verification:
                    * - NodeA wakes up and acknowledges the fragmented BEAM and also acknowledges the transmitted frame and is able to
                    *   correctly parse the received frame
                    */
            }

            /* 
                * Z-Wave system stability test 1
                * - NodeA Controller (compiled using RUT) transmit 1000000 frames to NodeC Slave (compiled using RUT). 
                * 
                * Test Regions:
                * - Done in REGION_EU, REGION_US and REGION_KR
                * 
                * Verification:
                * - NodeA and NodeC are both still operable and functioning
                * 
                */

            /*
                * Z-Wave system stability test 2
                * - NodeA Controller (compiled using RUT) transmit 100000 frames to NodeC FLiRS Slave (compiled using RUT). 
                * 
                * Test Regions:
                * - Done in REGION_EU, REGION_US and REGION_KR
                * 
                * Verification:
                * - NodeA and NodeC are both still operable and functioning
                * 
                */

            //ReportService.LogText("10.Send a frame from Ctrl2 to FLiRS2");
            //ztW = ZnifferService.ExpectData(5000);
            //ztW.SetUpWatch(null, HeaderTypes.WAKEUP_BEAM, false);
            //ztB = ZnifferService.ExpectData(5000);
            //ztB.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_SET() { value = 0xFF }, HeaderTypes.ROUTED_SINGLECAST, true);
            //ztR = ZnifferService.ExpectData(5000);
            //ztR.SetUpWatch(0x04, null, null, HeaderTypes.ROUTED_ACKNOWLEDGE, false);
            //Service.SendData(Ctrl2, FLiRS2, new COMMAND_CLASS_BASIC.BASIC_SET() { value = 0xFF }, TransmitOptions.Ack | TransmitOptions.AutoRoute);
            //zrW = ztW.WaitCompletedSignal();
            //zrB = ztB.WaitCompletedSignal();
            //zrR = ztR.WaitCompletedSignal();
            //Inconclusive(zrW,
            //    "10. Slave1 sends out a 1000ms wakeup beam before repeating the frame",
            //    "10. Slave1 didn't sends out a 1000ms wakeup beam before repeating the frame");
            //Inconclusive(zrB,
            //   "10. Frame is routed through Ctrl2 -> Slave1 -> FLiRS2 ",
            //   "10. Frame is not routed through Ctrl2 -> Slave1 -> FLiRS2 ");
            //Inconclusive(zrR,
            //   "10. Routed ACK frame detected",
            //   "10. Routed ACK frame not detected");



            //ReportService.LogText("11.Remove Slave");
            //Service.RemoveNode(Ctrl1, Slave);

            //ReportService.LogText("12.Remove Ctrl2");
            //Service.ExcludeNode(Ctrl1, Ctrl2);
            //BasicResult.MemoryGetId memRes = Service.MemoryGetId(Ctrl2);
            //ReportService.LogText("Ctrl2 node ID after remove: " + memRes.NodeId);
            //Service.Reset(Ctrl2);

            //ReportService.LogText("13.Replicate from Ctrl1 to Ctrl2");
            //Service.AddNode(Ctrl1, Ctrl2);

            //ReportService.LogText("14.Turn off the receiver on FLiRS1");
            //Service.SoftReset(FLiRS1);
            //Service.SetRFReceiveMode(FLiRS1, false);

            //ReportService.LogText("15.Turn off the receiver on FLiRS2");
            //Service.SoftReset(FLiRS2);
            //Service.SetRFReceiveMode(FLiRS2, false);

            //ReportService.LogText("16. Add Ctrl3 to Ctrl1");
            //Service.AddNode(Ctrl1, Ctrl3);

            //Service.AddNode(Ctrl1, Ctrl2);

            //ReportService.LogText("17.Turn on the receiver on FLiRS1");
            //Service.SetRFReceiveMode(FLiRS1, true);


            //ReportService.LogText("18.Turn on the receiver on FLiRS2");
            //Service.SetRFReceiveMode(FLiRS2, true);

            //Delay(10000); // allow flirs sleep

            //if (is700)
            //{
            //    Service.PowerMgmtCancel(FLiRS1, 0x00);
            //    Service.PowerMgmtCancel(FLiRS2, 0x00);
            //}
            //else
            //{
            //    Service.SetSleepMode(FLiRS1, SleepModes.FrequentlyListening, 0);
            //    Service.SetSleepMode(FLiRS2, SleepModes.FrequentlyListening, 0);
            //}
            //Delay(10000); // allow flirs sleep

            //ReportService.LogText("19. Send a frame from Ctrl3 to FLiRS1");
            //ztW = ZnifferService.ExpectData(15000);
            //ztW.SetUpWatch(null, HeaderTypes.WAKEUP_BEAM, false);
            //ztB = ZnifferService.ExpectData(15000);
            //ztB.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_SET() { value = 0xFF }, HeaderTypes.ROUTED_SINGLECAST, true);
            //ztR = ZnifferService.ExpectData(15000);
            //ztR.SetUpWatch(null, HeaderTypes.ROUTED_ACKNOWLEDGE, false);
            //Service.SendData(Ctrl3, FLiRS1, new COMMAND_CLASS_BASIC.BASIC_SET() { value = 0xFF }, TransmitOptions.Ack | TransmitOptions.AutoRoute);
            //zrW = ztW.WaitCompletedSignal();
            //zrB = ztB.WaitCompletedSignal();
            //zrR = ztR.WaitCompletedSignal();
            //Inconclusive(zrW,
            //   "19. Ctrl2 sends out a 1000ms wakeup beam before repeating the frame",
            //   "19. Ctrl2 didn't sends out a 1000ms wakeup beam before repeating the frame");
            //Inconclusive(zrB,
            //   "19. Frame is routed through Ctrl3 -> Ctrl2 -> FLiRS1 ",
            //   "19. Frame is not routed through Ctrl3 -> Ctrl2 -> FLiRS1 ");
            //Inconclusive(zrR,
            //   "19. Routed ACK frame detected",
            //   "19. Routed ACK frame not detected");

            //Delay(2000);

            //ReportService.LogText("20. Send a frame from Ctrl3 to FLiRS2");
            //ztW = ZnifferService.ExpectData(5000);
            //ztW.SetUpWatch(null, HeaderTypes.WAKEUP_BEAM, false);
            //ztB = ZnifferService.ExpectData(5000);
            //ztB.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_SET() { value = 0xFF }, HeaderTypes.ROUTED_SINGLECAST, true);
            //ztR = ZnifferService.ExpectData(5000);
            //ztR.SetUpWatch(null, HeaderTypes.ROUTED_ACKNOWLEDGE, false);
            //Service.SendData(Ctrl3, FLiRS2, new COMMAND_CLASS_BASIC.BASIC_SET() { value = 0xFF }, TransmitOptions.Ack | TransmitOptions.AutoRoute);
            //zrW = ztW.WaitCompletedSignal();
            //zrB = ztB.WaitCompletedSignal();
            //zrR = ztR.WaitCompletedSignal();
            //Inconclusive(zrW,
            //   "20. Ctrl2 sends out a 1000ms wakeup beam before repeating the frame",
            //   "20. Ctrl2 didn't sends out a 1000ms wakeup beam before repeating the frame");
            //Inconclusive(zrB,
            //   "20. Frame is routed through Ctrl3 -> Ctrl2 -> FLiRS2 ",
            //   "20. Frame is not routed through Ctrl3 -> Ctrl2 -> FLiRS2 ");
            //Inconclusive(zrR,
            //   "20. Routed ACK frame detected",
            //   "20. Routed ACK frame not detected");
        }
    }
}