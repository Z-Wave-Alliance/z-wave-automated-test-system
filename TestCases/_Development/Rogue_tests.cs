// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("Last repeater beams to FLiRS node ")]
    [DeviceMapping(1, LibTypes.ControllerBridgeLib)]
    [DeviceMapping(2, LibTypes.ControllerBridgeLib)]
    [DeviceMapping(3, LibTypes.ControllerBridgeLib)]
    [DeviceMapping(4, LibTypes.EndDeviceLib)]
    [DeviceMapping(5, LibTypes.EndDeviceLib)]
    [DeviceMapping(6, LibTypes.EndDeviceLib)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]
    public class Rogue_tests : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Ctrl1 = new Alias(1);
            Alias Ctrl2 = new Alias(2);
            Alias Ctrl3 = new Alias(3);
            Alias FLiRS1 = new Alias(4);
            Alias FLiRS2 = new Alias(5);
            Alias Slave = new Alias(6);

            Alias[] ALL = new Alias[] { Ctrl1, Ctrl2, Ctrl3, FLiRS1, FLiRS2, Slave };

            ReportService.LogText("Started");

            bool is700 = Context.IsZW070x(Ctrl1);

            // Set FLiRS1 and FLiRS2 to be FLIRS node
            Service.ApplicationNodeInformation(FLiRS1, DeviceOptions.FreqListeningMode1000ms,
                (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SWITCH_BINARY.ID });
            
            Service.ApplicationNodeInformation(FLiRS2, DeviceOptions.FreqListeningMode250ms,
                (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SWITCH_BINARY.ID });

            Service.ApplicationNodeInformation(Ctrl1, DeviceOptions.NoneListening, (byte)GenericTypes.GENERIC_CONTROLLER, (byte)SpecificTypes.NOT_USED,
                new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_ZWAVEPLUS_INFO.ID, COMMAND_CLASS_FIRMWARE_UPDATE_MD.ID, COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID });

            ParallelService.Reset(ALL);

            ReportService.LogText("1. Add Slave to Ctrl1 ");
            Service.IncludeNode(Ctrl1, Slave);

            ReportService.LogText("2.Inlcude FLiRS1 using Ctrl1 as primary controller");
            Service.IncludeNode(Ctrl1, FLiRS1);

            ReportService.LogText("3.Inlcude FLiRS2 using Ctrl1 as primary controller");
            Service.IncludeNode(Ctrl1, FLiRS2);

            if (is700)
            {
                Service.PowerMgmtCancel(FLiRS1, 0x00);
                Service.PowerMgmtCancel(FLiRS2, 0x00);
            }
            else
            {
                Service.SetSleepMode(FLiRS1, SleepModes.FrequentlyListening, 0);
                Service.SetSleepMode(FLiRS2, SleepModes.FrequentlyListening, 0);
            }
            Delay(10000); // allow flirs sleep

            ReportService.LogText("4.Turn off the receiver on FLiRS1");
            Service.SetRFReceiveMode(FLiRS1, false);

            ReportService.LogText("5.Turn off the receiver on FLiRS2");
            Service.SetRFReceiveMode(FLiRS2, false);

            ReportService.LogText("6. Add Ctrl2 to Ctrl1");
            Service.IncludeNode(Ctrl1, Ctrl2);

            ReportService.LogText("7.Turn on the receiver on FLiRS1");
            Service.SetRFReceiveMode(FLiRS1, true);

            ReportService.LogText("8.Turn on the receiver on FLiRS2");
            Service.SetRFReceiveMode(FLiRS2, true);
            Delay(2000);



            ReportService.LogText("9.Send a frame from Ctrl2 to FLiRS1");
            ZnifferToken.ExpectData ztW = ZnifferService.ExpectData(5000);
            ztW.SetUpWatch(null, HeaderTypes.WAKEUP_BEAM, false);
            ZnifferToken.ExpectData ztB = ZnifferService.ExpectData(5000);
            ztB.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_SET() { value = 0xFF }, HeaderTypes.ROUTED_SINGLECAST, true);
            ZnifferToken.ExpectData ztR = ZnifferService.ExpectData(5000);
            ztR.SetUpWatch(0x03, null, null, HeaderTypes.ROUTED_ACKNOWLEDGE, false);
            Service.SendData(Ctrl2, FLiRS1, new COMMAND_CLASS_BASIC.BASIC_SET() { value = 0xFF }, TransmitOptions.Ack | TransmitOptions.AutoRoute);
            ZnifferResult.ExpectData zrW = ztW.WaitCompletedSignal();
            ZnifferResult.ExpectData zrB = ztB.WaitCompletedSignal();
            ZnifferResult.ExpectData zrR = ztR.WaitCompletedSignal();
            Inconclusive(zrW,
                "9. Slave1 sends out a 1000ms wakeup beam before repeating the frame",
                "9. Slave1 didn't sends out a 1000ms wakeup beam before repeating the frame");
            Inconclusive(zrB,
               "9. Frame is routed through Ctrl2 -> Slave1 -> FLiRS1 ",
               "9. Frame is not routed through Ctrl2 -> Slave1 -> FLiRS1 ");
            Inconclusive(zrR,
               "9. Routed ACK frame detected",
               "9. Routed ACK frame not detected");

            ReportService.LogText("10.Send a frame from Ctrl2 to FLiRS2");
            ztW = ZnifferService.ExpectData(5000);
            ztW.SetUpWatch(null, HeaderTypes.WAKEUP_BEAM, false);
            ztB = ZnifferService.ExpectData(5000);
            ztB.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_SET() { value = 0xFF }, HeaderTypes.ROUTED_SINGLECAST, true);
            ztR = ZnifferService.ExpectData(5000);
            ztR.SetUpWatch(0x04, null, null, HeaderTypes.ROUTED_ACKNOWLEDGE, false);
            Service.SendData(Ctrl2, FLiRS2, new COMMAND_CLASS_BASIC.BASIC_SET() { value = 0xFF }, TransmitOptions.Ack | TransmitOptions.AutoRoute);
            zrW = ztW.WaitCompletedSignal();
            zrB = ztB.WaitCompletedSignal();
            zrR = ztR.WaitCompletedSignal();
            Inconclusive(zrW,
                "10. Slave1 sends out a 1000ms wakeup beam before repeating the frame",
                "10. Slave1 didn't sends out a 1000ms wakeup beam before repeating the frame");
            Inconclusive(zrB,
               "10. Frame is routed through Ctrl2 -> Slave1 -> FLiRS2 ",
               "10. Frame is not routed through Ctrl2 -> Slave1 -> FLiRS2 ");
            Inconclusive(zrR,
               "10. Routed ACK frame detected",
               "10. Routed ACK frame not detected");



            ReportService.LogText("11.Remove Slave");
            Service.RemoveNode(Ctrl1, Slave);

            ReportService.LogText("12.Remove Ctrl2");
            Service.ExcludeNode(Ctrl1, Ctrl2);
            BasicResult.MemoryGetId memRes = Service.MemoryGetId(Ctrl2);
            ReportService.LogText("Ctrl2 node ID after remove: " + memRes.NodeId);
            Service.Reset(Ctrl2);

            ReportService.LogText("13.Replicate from Ctrl1 to Ctrl2");
            Service.AddNode(Ctrl1, Ctrl2);

            ReportService.LogText("14.Turn off the receiver on FLiRS1");
            Service.SoftReset(FLiRS1);
            Service.SetRFReceiveMode(FLiRS1, false);

            ReportService.LogText("15.Turn off the receiver on FLiRS2");
            Service.SoftReset(FLiRS2);
            Service.SetRFReceiveMode(FLiRS2, false);

            ReportService.LogText("16. Add Ctrl3 to Ctrl1");
            Service.AddNode(Ctrl1, Ctrl3);

            Service.AddNode(Ctrl1, Ctrl2);

            ReportService.LogText("17.Turn on the receiver on FLiRS1");
            Service.SetRFReceiveMode(FLiRS1, true);
          

            ReportService.LogText("18.Turn on the receiver on FLiRS2");
            Service.SetRFReceiveMode(FLiRS2, true);
        
            Delay(10000); // allow flirs sleep

            if (is700)
            {
                Service.PowerMgmtCancel(FLiRS1, 0x00);
                Service.PowerMgmtCancel(FLiRS2, 0x00);
            }
            else
            {
                Service.SetSleepMode(FLiRS1, SleepModes.FrequentlyListening, 0);
                Service.SetSleepMode(FLiRS2, SleepModes.FrequentlyListening, 0);
            }
            Delay(10000); // allow flirs sleep

            ReportService.LogText("19. Send a frame from Ctrl3 to FLiRS1");
            ztW = ZnifferService.ExpectData(15000);
            ztW.SetUpWatch(null, HeaderTypes.WAKEUP_BEAM, false);
            ztB = ZnifferService.ExpectData(15000);
            ztB.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_SET() { value = 0xFF }, HeaderTypes.ROUTED_SINGLECAST, true);
            ztR = ZnifferService.ExpectData(15000);
            ztR.SetUpWatch(null, HeaderTypes.ROUTED_ACKNOWLEDGE, false);
            Service.SendData(Ctrl3, FLiRS1, new COMMAND_CLASS_BASIC.BASIC_SET() { value = 0xFF }, TransmitOptions.Ack | TransmitOptions.AutoRoute);
            zrW = ztW.WaitCompletedSignal();
            zrB = ztB.WaitCompletedSignal();
            zrR = ztR.WaitCompletedSignal();
            Inconclusive(zrW,
               "19. Ctrl2 sends out a 1000ms wakeup beam before repeating the frame",
               "19. Ctrl2 didn't sends out a 1000ms wakeup beam before repeating the frame");
            Inconclusive(zrB,
               "19. Frame is routed through Ctrl3 -> Ctrl2 -> FLiRS1 ",
               "19. Frame is not routed through Ctrl3 -> Ctrl2 -> FLiRS1 ");
            Inconclusive(zrR,
               "19. Routed ACK frame detected",
               "19. Routed ACK frame not detected");

            Delay(2000);

            ReportService.LogText("20. Send a frame from Ctrl3 to FLiRS2");
            ztW = ZnifferService.ExpectData(5000);
            ztW.SetUpWatch(null, HeaderTypes.WAKEUP_BEAM, false);
            ztB = ZnifferService.ExpectData(5000);
            ztB.SetUpWatch(new COMMAND_CLASS_BASIC.BASIC_SET() { value = 0xFF }, HeaderTypes.ROUTED_SINGLECAST, true);
            ztR = ZnifferService.ExpectData(5000);
            ztR.SetUpWatch(null, HeaderTypes.ROUTED_ACKNOWLEDGE, false);
            Service.SendData(Ctrl3, FLiRS2, new COMMAND_CLASS_BASIC.BASIC_SET() { value = 0xFF }, TransmitOptions.Ack | TransmitOptions.AutoRoute);
            zrW = ztW.WaitCompletedSignal();
            zrB = ztB.WaitCompletedSignal();
            zrR = ztR.WaitCompletedSignal();
            Inconclusive(zrW,
               "20. Ctrl2 sends out a 1000ms wakeup beam before repeating the frame",
               "20. Ctrl2 didn't sends out a 1000ms wakeup beam before repeating the frame");
            Inconclusive(zrB,
               "20. Frame is routed through Ctrl3 -> Ctrl2 -> FLiRS2 ",
               "20. Frame is not routed through Ctrl3 -> Ctrl2 -> FLiRS2 ");
            Inconclusive(zrR,
               "20. Routed ACK frame detected",
               "20. Routed ACK frame not detected");
        }
    }
}