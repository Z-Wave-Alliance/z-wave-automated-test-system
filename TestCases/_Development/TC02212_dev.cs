// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
﻿using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("S2 + Transport Service: Default Case - PC Controller")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    class TC02212_dev : ZatsCodeTestCase
    {

        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave = new Alias(2);
            Alias[] ALL = new Alias[] { Primary, Slave };

            ParallelService.Reset(ALL);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            ApplicationService.AddSecurity(Slave, SecurityVersions.S2);

            ReportService.LogText("1. Include slave enhanced to primary controller.");
            var addRes = Service.AddNode(Primary, Slave);
            byte slId = addRes.NodeId;
            AssertIsTrue(addRes, "Added node id =" + slId);
            AssertIsTrue(addRes.SubstituteStatus == SubstituteStatuses.Done, "In 1. verify that inclusion was successful and that security v2 is enabled.");

            ApplicationService.AddTransportService(Primary);
            ApplicationService.AddTransportService(Slave);
            var key = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_ACCESS);
            ZnifferService.SetSecuritySupport(true);
            ZnifferService.SetSecurityNetworkKeyS2(key);
            var zt = ZnifferService.ExpectData(10000);
            zt.SetUpWatch(0x01, slId, null, new COMMAND_CLASS_TRANSPORT_SERVICE_V2.COMMAND_FIRST_SEGMENT(),
                HeaderTypes.SINGLECAST, null, false, false);
            zt.SetUpWatch(0x01, slId, null, new COMMAND_CLASS_TRANSPORT_SERVICE_V2.COMMAND_SUBSEQUENT_SEGMENT(),
                HeaderTypes.SINGLECAST, null, false, 4, false);
            zt.SetUpWatch(slId, 0x01, null, new COMMAND_CLASS_TRANSPORT_SERVICE_V2.COMMAND_SEGMENT_COMPLETE(),
                HeaderTypes.SINGLECAST, null, false, false);

            ReportService.LogText("2. Send a frame of 120 bytes from primary to slave.");
            var data2test = new byte[120];
            data2test[0] = COMMAND_CLASS_BASIC.ID;
            data2test[1] = COMMAND_CLASS_BASIC.BASIC_GET.ID;
            var sd = Service.SendData(Primary, Slave, data2test);
            Delay(1000);
            var ztRes = zt.WaitCompletedSignal();

            AssertIsTrue(sd, "In 2. verify:");
            AssertIsTrue(ztRes, " - 4 fragments are sent.- The fragments are sent S2 Encapsulated.- Once the 4 fragments have been sent, the receiver will answer back with FragmentComplete(OK)");

        }
    }
}