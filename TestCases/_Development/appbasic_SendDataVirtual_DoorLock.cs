// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("appbasic_SendDataVirtual_DoorLock: Check WakeUp Beam is sent from virtual node to DoorLock")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerBridgeLib)]
    [DebugDeviceMapping(3, "dd1")]
    [TestInterfaceDeviceMapping(4, "TI1")]

    class appbasic_SendDataVirtual_DoorLock : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Controller = new Alias(1);
            Alias BrgController = new Alias(2);
            Alias DoorLock = new Alias(4);
            Alias DoorLockD = new Alias(3);

            //Prepare BasicApi ALL macro
            Alias[] ALL = new Alias[] { Controller, BrgController };

            //Reset devices
            ParallelService.SetDefault(ALL);

            #region 1. Inlcude Bridge to Controller

            ReportService.LogTitle("1. Inlcude Bridge sample app to Controller");
            var addBrg = Service.IncludeNode(Controller, BrgController);
            AssertIsTrue(addBrg);
            var brgNodeId = addBrg.AddNodeResult.Node;
            Delay(5000, "Delay for 5 seconds");

            #endregion

            #region Preparing the App to be used in the TC

            TestInterfaceService.ExcludeNode(Controller, DoorLock);

            #endregion

            #region 2. Inlcude Z-Wave+ sample app to Controller

            ReportService.LogTitle("2. Inlcude Z-Wave+ sample app to Controller");

            // Adding node to the Controller.
            var addExpRes = TestInterfaceService.IncludeNode(Controller, DoorLock, DoorLockD);
            AssertIsTrue(addExpRes);
            var addedNodeId = addExpRes.AddNodeResult.Node;

            #endregion

            #region 3. Add virtual node

            ReportService.LogTitle("3. Add virtual node");

            var addRes = Service.AddNodeVirtual(Controller, BrgController);
            AssertIsTrue(addRes, "Virtual node added with ID = " + addRes.NodeId, "Failed to add virtual node");
            byte virtNodeId = addRes.NodeId;

            #endregion

            #region 4. Send data from Controller to DoorLock

            ReportService.LogTitle("4. Send data from Controller to DoorLock");

            ComplianceService.SendData(Controller, addedNodeId, new COMMAND_CLASS_BASIC.BASIC_SET(), TransmitOptions.Ack);

            var sendRes = ComplianceService.SendData(Controller, addedNodeId, new COMMAND_CLASS_BASIC.BASIC_SET(), TransmitOptions.Ack);
            AssertIsTrue(sendRes && sendRes.TransmitStatus == TransmitStatuses.CompleteOk);

            #endregion

            #region 5. Send data from Bridge to DoorLock

            ReportService.LogTitle("5. Send data from Bridge to DoorLock");

            ComplianceService.SendData(BrgController, addedNodeId, new COMMAND_CLASS_BASIC.BASIC_SET(), TransmitOptions.Ack);

            sendRes = ComplianceService.SendData(BrgController, addedNodeId, new COMMAND_CLASS_BASIC.BASIC_SET(), TransmitOptions.Ack);
            AssertIsTrue(sendRes && sendRes.TransmitStatus == TransmitStatuses.CompleteOk);

            #endregion

            #region 6. Send data bridge from Bridge to DoorLock

            ReportService.LogTitle("6. Send data bridge from Bridge to DoorLock");

            ComplianceService.SendDataBridge(BrgController, brgNodeId, addedNodeId, new COMMAND_CLASS_BASIC.BASIC_SET(), TransmitOptions.Ack);

            sendRes = ComplianceService.SendDataBridge(BrgController, brgNodeId, addedNodeId, new COMMAND_CLASS_BASIC.BASIC_SET(), TransmitOptions.Ack);
            AssertIsTrue(sendRes && sendRes.TransmitStatus == TransmitStatuses.CompleteOk);

            #endregion

            #region 7. Send data bridge from Virtual node to DoorLock

            ReportService.LogTitle("7. Send data bridge from Virtual node to DoorLock");

            ComplianceService.SendDataBridge(BrgController, virtNodeId, addedNodeId, new COMMAND_CLASS_BASIC.BASIC_SET(), TransmitOptions.Ack);

            sendRes = ComplianceService.SendDataBridge(BrgController, virtNodeId, addedNodeId, new COMMAND_CLASS_BASIC.BASIC_SET(), TransmitOptions.Ack);
            AssertIsTrue(sendRes && sendRes.TransmitStatus == TransmitStatuses.CompleteOk);

            #endregion
        }
    }
}
