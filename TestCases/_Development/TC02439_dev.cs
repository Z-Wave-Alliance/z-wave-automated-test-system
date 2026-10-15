// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
﻿using System;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("DEV* Security v2 PC Controller S2MEC - MPAN Extension Group ID field")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    [DeviceMapping(4, LibTypes.EndDeviceLib)]
    [DeviceMapping(5, LibTypes.EndDeviceLib)]
    class TC02439_dev : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Controller1 = new Alias(1);
            Alias DUT = new Alias(2);
            Alias Slave1 = new Alias(3);
            Alias Slave2 = new Alias(4);
            Alias Slave3 = new Alias(5);
            Alias[] ALL = new Alias[] { Controller1, DUT, Slave1, Slave2, Slave3 };

            ParallelService.Reset(ALL);

            ApplicationService.AddSecurity(Controller1, SecurityVersions.S2);
            ApplicationService.AddSecurity(DUT, SecurityVersions.S2);

            Service.ApplicationNodeInformation(Controller1, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER,
                 new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_CONTROLLER_REPLICATION.ID,
                        COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID, COMMAND_CLASS_BASIC.ID});
            Service.ApplicationNodeInformation(DUT, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER,
                 new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_CONTROLLER_REPLICATION.ID,
                        COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID, COMMAND_CLASS_BASIC.ID});

            Service.ApplicationNodeInformation(Slave1, true, (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                new byte[] { COMMAND_CLASS_SECURITY_2.ID });
            Service.ApplicationNodeInformation(Slave2, true, (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                new byte[] { COMMAND_CLASS_SECURITY_2.ID });
            Service.ApplicationNodeInformation(Slave3, true, (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED,
                new byte[] { COMMAND_CLASS_SECURITY_2.ID });

            ApplicationService.AddBasicEx(Controller1);
            ApplicationService.AddBasicEx(DUT);

            Service.SetSUCNodeID(Controller1, Controller1);
            #region 1.
            ReportService.LogText("1.- Include joining DUT and the three Slaves to including PC Controller (1) network");
            var addRes = Service.AddNode(Controller1, DUT);
            AssertIsTrue(addRes, "In 1. verify that inclusion is successful and that security 2 is enabled, DUT Id = " + addRes.NodeId);
            AssertIsTrue(SubstituteStatuses.Done == addRes.SubstituteStatus);
            addRes = Service.AddNode(Controller1, Slave1);
            AssertIsTrue(addRes, "In 1. verify that inclusion is successful and S2 bootstrapping is performed, Slave1 Id = " + addRes.NodeId);
            AssertIsTrue(SubstituteStatuses.Done == addRes.SubstituteStatus);
            addRes = Service.AddNode(Controller1, Slave2);
            AssertIsTrue(addRes, "In 1. verify that inclusion is successful and S2 bootstrapping is performed, Slave2 Id = " + addRes.NodeId);
            AssertIsTrue(SubstituteStatuses.Done == addRes.SubstituteStatus);
            addRes = Service.AddNode(Controller1, Slave3);
            AssertIsTrue(addRes, "In 1. verify that inclusion is successful and S2 bootstrapping is performed, Slave3 Id = " + addRes.NodeId);
            AssertIsTrue(SubstituteStatuses.Done == addRes.SubstituteStatus);
            #endregion

            ReportService.LogText("!To sync SPAN");
            Service.SendData(Controller1, DUT, new COMMAND_CLASS_BASIC.BASIC_SET());
            Service.SendData(Controller1, Slave1, new COMMAND_CLASS_BASIC.BASIC_SET());
            Service.SendData(Controller1, Slave2, new COMMAND_CLASS_BASIC.BASIC_SET());
            Service.SendData(Controller1, Slave3, new COMMAND_CLASS_BASIC.BASIC_SET());

            #region 2.
            Service.RequestNetworkUpdate(DUT);

            Service.RequestNodeInfo(DUT, Controller1);
            Service.RequestNodeInfo(DUT, Slave1);
            Service.RequestNodeInfo(DUT, Slave2);
            Service.RequestNodeInfo(DUT, Slave3);

            var authKey = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(DUT, SecuritySchemes.S2_ACCESS);
            ZnifferService.SetSecuritySupport(true);
            ZnifferService.SetSecurityNetworkKeyS2(authKey);

            var ztRes = CheckSync(DUT, new Node[] { 1, 3, 4 }, new COMMAND_CLASS_BASIC.BASIC_SET(), SyncCondition.Required);
            AssertIsTrue(ztRes, "2. MPAN synchronization is required [009F.01.03.11.01A][009F.01.03.11.01B] ");

            #endregion

            #region 3.
            ztRes = CheckSync(DUT, new Node[] { 1, 3, 4 }, new COMMAND_CLASS_BASIC.BASIC_SET(), SyncCondition.NotRequired);
            AssertIsTrue(ztRes, "3. MPAN synchronization is not required [009F.01.03.11.01A][009F.01.03.11.01B] ");
            #endregion

            #region 4.
            ztRes = CheckSync(DUT, new Node[] { 3, 4, 5 }, new COMMAND_CLASS_BASIC.BASIC_SET(), SyncCondition.Required);
            AssertIsTrue(ztRes, "4. MPAN synchronization is required [009F.01.03.11.01A][009F.01.03.11.01B] ");
            #endregion

            #region 5.
            ztRes = CheckSync(DUT, new Node[] { 1, 4, 5 }, new COMMAND_CLASS_BASIC.BASIC_SET(), SyncCondition.Required);
            AssertIsTrue(ztRes, "5. MPAN synchronization is required [009F.01.03.11.01A][009F.01.03.11.01B] ");
            #endregion

            #region 6.
            ztRes = CheckSync(Controller1, new Node[] { 2, 3, 4 }, new COMMAND_CLASS_BASIC.BASIC_SET(), SyncCondition.Required);
            AssertIsTrue(ztRes, "6. MPAN synchronization is required [009F.01.03.11.01A][009F.01.03.11.01B] ");
            #endregion

            #region 7.
            // Repeat 2 to 6
            ztRes = CheckSync(DUT, new Node[] { 1, 3, 4 }, new COMMAND_CLASS_BASIC.BASIC_SET(), SyncCondition.NotRequired);
            AssertIsTrue(ztRes, "7. MPAN synchronization is not required [009F.01.03.11.01A][009F.01.03.11.01B] ");

            ztRes = CheckSync(DUT, new Node[] { 1, 3, 4 }, new COMMAND_CLASS_BASIC.BASIC_SET(), SyncCondition.NotRequired);
            AssertIsTrue(ztRes, "7. MPAN synchronization is not required [009F.01.03.11.01A][009F.01.03.11.01B] ");

            ztRes = CheckSync(DUT, new Node[] { 3, 4, 5 }, new COMMAND_CLASS_BASIC.BASIC_SET(), SyncCondition.NotRequired);
            AssertIsTrue(ztRes, "7. MPAN synchronization is not required [009F.01.03.11.01A][009F.01.03.11.01B] ");

            ztRes = CheckSync(DUT, new Node[] { 1, 4, 5 }, new COMMAND_CLASS_BASIC.BASIC_SET(), SyncCondition.NotRequired);
            AssertIsTrue(ztRes, "7. MPAN synchronization is not required [009F.01.03.11.01A][009F.01.03.11.01B] ");

            ztRes = CheckSync(Controller1, new Node[] { 2, 3, 4 }, new COMMAND_CLASS_BASIC.BASIC_SET(), SyncCondition.NotRequired);
            AssertIsTrue(ztRes, "7. MPAN synchronization is not required [009F.01.03.11.01A][009F.01.03.11.01B] ");
            #endregion

            #region 8.
            ztRes = CheckSync(Controller1, new Node[] { 2, 4, 5 }, new COMMAND_CLASS_BASIC.BASIC_SET(), SyncCondition.Required);
            AssertIsTrue(ztRes, "8. MPAN synchronization is not required [009F.01.03.11.01A][009F.01.03.11.01B] ");
            #endregion

            #region 9.
            // Repeat 3,4,5,6,8
            ztRes = CheckSync(DUT, new Node[] { 1, 3, 4 }, new COMMAND_CLASS_BASIC.BASIC_SET(), SyncCondition.NotRequired);
            AssertIsTrue(ztRes, "9. MPAN synchronization is not required [009F.01.03.11.01A][009F.01.03.11.01B] ");

            ztRes = CheckSync(DUT, new Node[] { 3, 4, 5 }, new COMMAND_CLASS_BASIC.BASIC_SET(), SyncCondition.NotRequired);
            AssertIsTrue(ztRes, "9. MPAN synchronization is not required [009F.01.03.11.01A][009F.01.03.11.01B] ");

            ztRes = CheckSync(DUT, new Node[] { 1, 4, 5 }, new COMMAND_CLASS_BASIC.BASIC_SET(), SyncCondition.NotRequired);
            AssertIsTrue(ztRes, "9. MPAN synchronization is not required [009F.01.03.11.01A][009F.01.03.11.01B] ");

            ztRes = CheckSync(Controller1, new Node[] { 2, 3, 4 }, new COMMAND_CLASS_BASIC.BASIC_SET(), SyncCondition.NotRequired);
            AssertIsTrue(ztRes, "9. MPAN synchronization is required [009F.01.03.11.01A][009F.01.03.11.01B] ");

            ztRes = CheckSync(Controller1, new Node[] { 2, 4, 5 }, new COMMAND_CLASS_BASIC.BASIC_SET(), SyncCondition.NotRequired);
            AssertIsTrue(ztRes, "9. MPAN synchronization is not required [009F.01.03.11.01A][009F.01.03.11.01B] ");
            #endregion
        }

        private bool CheckSync(Alias senderNumber, Node[] receiverNodes, byte[] data, SyncCondition condition)
        {
            ZnifferResult.ExpectData ztRes;
            bool res = false;
            var zt = ZnifferService.ExpectData(10000);

            foreach (var node in receiverNodes)
            {
                zt.SetUpWatch(node, (byte)senderNumber.Value, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT(), null, false);
            }

            var reqRes = ComplianceService.SendDataMulti(senderNumber, receiverNodes, data, TX_OPTIONS);

            using (ZnifferService.Settings.IsExpectingFailure.Set())
            {
                ztRes = zt.WaitCompletedSignal();
            }

            switch (condition)
            {
                case SyncCondition.NotRequired:
                    res = !ztRes;
                    break;
                case SyncCondition.Required:
                    res = ztRes.Frames.Count > 0;
                    break;
            }

            return res;
        }
        public enum SyncCondition { Required, NotRequired };
    }
}
