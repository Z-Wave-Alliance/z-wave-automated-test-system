// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
﻿using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("appbasic_expectData_Sync")]
    [DeviceMapping(1, "dd1", LibTypes.Controller)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.ControllerStaticLib)]
    public class appbasic_ExpectData_PMode : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias MainController = new Alias(1);
            Alias Node1 = new Alias(2);
            Alias Node2 = new Alias(3);
            Alias[] ALL = { MainController, Node1, Node2 };

            ParallelService.Reset(ALL);

            var addResult1 = Service.AddNode(MainController, Node1);
            var addResult2 = Service.AddNode(MainController, Node2);

            Service.SetPromiscuousMode(MainController, true);

            ReportService.LogTitle("1. SendData");
            // Prepare catch the frame
            var expectToken = AsyncService.ExpectData(MainController, new COMMAND_CLASS_BASIC.BASIC_REPORT(), 2000);
            // Send frame
            Service.SendData(Node1, Node2, new COMMAND_CLASS_BASIC.BASIC_REPORT());
            // Wait until frame catched or timeout expired
            var expectResult = expectToken.WaitCompletedSignal();

            // Asserts
            Assert.IsTrue(expectResult);
            Assert.AreEqual(addResult1.NodeId, expectResult.NodeId);
            Assert.AreEqual(addResult2.NodeId, expectResult.DestNodeId);

            ReportService.LogTitle("1. SendDataMulti");
            // Prepare catch the frame
            expectToken = AsyncService.ExpectData(MainController, new COMMAND_CLASS_BASIC.BASIC_REPORT(), 2000);
            // Send frame
            Service.SendDataMulti(Node1, new Alias[] { Node2 }, new COMMAND_CLASS_BASIC.BASIC_REPORT(), TX_OPTIONS);
            // Wait until frame catched or timeout expired
            expectResult = expectToken.WaitCompletedSignal();

            // Asserts
            Assert.IsTrue(expectResult);
            Assert.AreEqual(addResult1.NodeId, expectResult.NodeId);
            Assert.AreEqual(0, expectResult.DestNodeId);
        }
    }
}
