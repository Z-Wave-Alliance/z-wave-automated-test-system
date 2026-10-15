// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
﻿using System.Linq;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("Assign Priority Return Route works correct")]
    [DeviceMapping(1, LibTypes.ControllerBridgeLib)]
    [DeviceMapping(2, LibTypes.EndDeviceAny)]


    public class TestSendData : ZatsCodeTestCase
    {

        protected override void ExecuteTest()
        {
            Alias StaticController = new Alias(1);
            Alias SlaveRouting = new Alias(2);

            Alias[] ALL = { StaticController, SlaveRouting };

            Service.ApplicationNodeInformation(StaticController, DeviceOptions.Listening,
                (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER,
                new byte[] { COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID });


            var cmdClasses = new byte[] { COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID, COMMAND_CLASS_BASIC.ID };
            //Service.ApplicationNodeInformationCmdClasses(SlaveRouting, cmdClasses, cmdClasses, cmdClasses);


            //ApplicationService.AddSecurity(StaticController, SecurityVersions.S2);


            ReportService.LogText("Started");
            ParallelService.Reset(ALL);

            ApplicationService.SecurityTestSettings.SetPredefinedNetworkKeys(StaticController);

            byte speed = 4; // for 100kbps (9,6kbps = 1; 40kbps = 2)

            // 1. Add all 5 SlaveNodes with MainController.
            ReportService.LogText("1. Add all 5 slave nodes with the Main Controller");

            var addRes = Service.AddNode(StaticController, SlaveRouting);
            AssertIsTrue(addRes, "Successfully added SlaveRouting1 to Primary Controller", "Unable to add SlaveRouting1 to Primary Controller");
            Service.MemoryGetId(SlaveRouting);



            ReportService.LogText("3.Disable RF connection on Controller nodeID1.");
            Service.SetRFReceiveMode(StaticController, false);

            ReportService.LogText("4.Send Basic Set to Controller nodeID1 from Slave nodeID5");
            var ztc = ZnifferService.ExpectData(10000);
            ztc.SetUpWatch(5, 1, 0, 2, new byte[] { 2, 3 }, null, new COMMAND_CLASS_BASIC.BASIC_SET());
            BasicResult.SendData resp = Service.SendData(SlaveRouting, StaticController, new COMMAND_CLASS_BASIC.BASIC_SET() { value = 0xFF }, TransmitOptions.Ack | TransmitOptions.AutoRoute);
            var zrc = ztc.WaitCompletedSignal();
            AssertIsTrue(zrc, "Basic Set is detected", "No Basic Set is detected");
        }
    }
}