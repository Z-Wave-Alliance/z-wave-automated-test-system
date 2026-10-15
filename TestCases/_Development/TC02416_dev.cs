// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
﻿using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("To verify the successful inclusion of a PC Controller via CSA")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    class TC02416_dev : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias primary = new Alias(1);
            Alias secondary = new Alias(2);
            Alias[] ALL = new Alias[] { primary, secondary };
            ParallelService.Reset(ALL);

            BasicResult.AddNodeWithLearnMode addRes;
            ApplicationService.AddSecurity(primary, SecurityVersions.S2 | SecurityVersions.S0);
            ApplicationService.AddSecurity(secondary, SecurityVersions.S2 | SecurityVersions.S0);

            Service.ApplicationNodeInformation(primary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER,
                new byte[]
                {
                    COMMAND_CLASS_ZWAVEPLUS_INFO.ID,
                    COMMAND_CLASS_VERSION.ID,
                    COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID,
                    COMMAND_CLASS_POWERLEVEL.ID,
                    COMMAND_CLASS_APPLICATION_STATUS.ID,
                    COMMAND_CLASS_ASSOCIATION.ID,
                    COMMAND_CLASS_ASSOCIATION_GRP_INFO.ID,
                    COMMAND_CLASS_CONFIGURATION.ID,
                    COMMAND_CLASS_CRC_16_ENCAP.ID,
                    COMMAND_CLASS_DEVICE_RESET_LOCALLY.ID,
                    COMMAND_CLASS_SECURITY_2.ID,
                    COMMAND_CLASS_SUPERVISION.ID,
                    COMMAND_CLASS_TRANSPORT_SERVICE.ID,
                    COMMAND_CLASS_INCLUSION_CONTROLLER.ID
                });

            Service.ApplicationNodeInformation(secondary, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER,
                new byte[]
                {
                    COMMAND_CLASS_ZWAVEPLUS_INFO.ID,
                    COMMAND_CLASS_VERSION.ID,
                    COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID,
                    COMMAND_CLASS_POWERLEVEL.ID,
                    COMMAND_CLASS_APPLICATION_STATUS.ID,
                    COMMAND_CLASS_ASSOCIATION.ID,
                    COMMAND_CLASS_ASSOCIATION_GRP_INFO.ID,
                    COMMAND_CLASS_CONFIGURATION.ID,
                    COMMAND_CLASS_CRC_16_ENCAP.ID,
                    COMMAND_CLASS_DEVICE_RESET_LOCALLY.ID,
                    COMMAND_CLASS_SECURITY_2.ID,
                    COMMAND_CLASS_SUPERVISION.ID,
                    COMMAND_CLASS_TRANSPORT_SERVICE.ID,
                    COMMAND_CLASS_INCLUSION_CONTROLLER.ID
                });

            #region 1
            ReportService.LogText("1. Enabling inclusion with Client-side authentication on secondary controller.");
            ApplicationService.EnableCSA(secondary, true);
            #endregion

            #region 2
            ReportService.LogText("2. Start inclusion of secondary to primary controller.");
            Delay(2000, "! - Add Node");

            var primaryListenToken = AsyncService.ListenData(primary);
            var secondaryListenToken = AsyncService.ListenData(secondary);

            addRes = Service.AddNode(primary, secondary);
            Delay(2000, "! - Wait Learn Mode Completed");
            AsyncService.CancelToken(primary, primaryListenToken);
            AsyncService.CancelToken(secondary, secondaryListenToken);
            #endregion

            #region 3
            var addedNodeId = addRes.Node;
            AssertIsTrue(addRes, "DUT Id = " + addedNodeId);
            AssertIsTrue(SubstituteStatuses.Done == addRes.SubstituteStatus, "3. Inclusion is successful and security v2 is enabled.");

            BasicResult.ListenData result1 = primaryListenToken.WaitCompletedSignal();
            BasicResult.ListenData result2 = secondaryListenToken.WaitCompletedSignal();
            var dataCollection = ReceivedDataCollection.FromCombinedListenResults(result1, result2);

            AssertIsTrue(CommandsComparator.IsEquals(dataCollection.FirstCommand()?.Command, new COMMAND_CLASS_SECURITY_2.KEX_GET()));

            var res = dataCollection.NextCommand();
            AssertIsTrue(CommandsComparator.IsEquals(res?.Command, new COMMAND_CLASS_SECURITY_2.KEX_REPORT()));
            //Assert.AreEqual(0x01, ((COMMAND_CLASS_SECURITY_2.KEX_REPORT)res.CurrentCommand).properties1.requestCsa);

            AssertIsTrue(CommandsComparator.IsEquals(dataCollection.NextCommand()?.Command, new COMMAND_CLASS_SECURITY_2.KEX_SET()));
            AssertIsTrue(CommandsComparator.IsEquals(dataCollection.NextCommand()?.Command, new COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT()));

            res = dataCollection.NextCommand();
            AssertIsTrue(CommandsComparator.IsEquals(res?.Command, new COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT()));
            Assert.AreEqual(0x01, ((COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT)res?.Command).properties1.includingNode);

            AssertIsTrue(CommandsComparator.IsEquals(dataCollection.NextCommand()?.Command, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_GET()));

            res = dataCollection.NextCommand();
            AssertIsTrue(CommandsComparator.IsEquals(res?.Command, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT()));
            Assert.AreEqual(0x01, ((COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT)res?.Command).properties1.sos); // SOS

            res = dataCollection.NextCommand();
            AssertIsTrue(CommandsComparator.IsEquals(res?.Command, new COMMAND_CLASS_SECURITY_2.KEX_SET()));
            Assert.AreEqual(0x01, ((COMMAND_CLASS_SECURITY_2.KEX_SET)res?.Command).properties1.echo); // Echo

            res = dataCollection.NextCommand();
            AssertIsTrue(CommandsComparator.IsEquals(res?.Command, new COMMAND_CLASS_SECURITY_2.KEX_REPORT()));
            Assert.AreEqual(0x01, ((COMMAND_CLASS_SECURITY_2.KEX_REPORT)res?.Command).properties1.echo); // Echo

            for (int i = 0; i < 4; i++)
            {
                AssertIsTrue(CommandsComparator.IsEquals(dataCollection.NextCommand()?.Command, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_GET()));
                AssertIsTrue(CommandsComparator.IsEquals(dataCollection.NextCommand()?.Command, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_REPORT()));
                AssertIsTrue(CommandsComparator.IsEquals(dataCollection.NextCommand()?.Command, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_GET()));
                AssertIsTrue(CommandsComparator.IsEquals(dataCollection.NextCommand()?.Command, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT()));
                AssertIsTrue(CommandsComparator.IsEquals(dataCollection.NextCommand()?.Command, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_VERIFY()));
                AssertIsTrue(CommandsComparator.IsEquals(dataCollection.NextCommand()?.Command, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_GET()));
                AssertIsTrue(CommandsComparator.IsEquals(dataCollection.NextCommand()?.Command, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NONCE_REPORT()));
                AssertIsTrue(CommandsComparator.IsEquals(dataCollection.NextCommand()?.Command, new COMMAND_CLASS_SECURITY_2.SECURITY_2_TRANSFER_END()));
            }
            AssertIsTrue(CommandsComparator.IsEquals(dataCollection.NextCommand()?.Command, new COMMAND_CLASS_SECURITY_2.SECURITY_2_TRANSFER_END()));
            #endregion
        }
    }
}
