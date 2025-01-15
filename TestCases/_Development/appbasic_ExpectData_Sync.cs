// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
﻿using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("appbasic_expectData_Sync")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    public class appbasic_expectData_Sync : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias MainController = new Alias(1);
            Alias SlaveNode = new Alias(2);
            Alias[] ALL = { MainController, SlaveNode};

            ReportService.LogText("Started");
            ParallelService.Reset(ALL);

            ReportService.LogText("Add slave node");
            byte slaveNodeId = Service.AddNode(MainController, SlaveNode).NodeId;

            ApplicationService.StartTrafficToNodeIdTest(SlaveNode, 0x01, new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V2.FIRMWARE_UPDATE_MD_GET(), 20000);

            var res = Service.ExpectData(MainController, new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V2.FIRMWARE_UPDATE_MD_GET(), 20000);
            
            Inconclusive(res, "Catch", "Miss");
            Inconclusive(res.Command != null, res.Command.ToHexString(), "Miss");
            ApplicationService.StopTrafficTest(SlaveNode, 0x01);
        }
    }
}
