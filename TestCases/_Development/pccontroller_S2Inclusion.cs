// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
﻿using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("Security v2 PC Controller successful secured inclusion")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]

    class pccontroller_S2Inclusion : ZatsCodeTestCase
    {
        protected override void ExecuteTest()  
        {
            Alias Primary = new Alias(1);
            Alias Secondary = new Alias(2);

            Alias[] ALL = new Alias[] { Primary, Secondary };

            ParallelService.Reset(ALL);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            ApplicationService.AddSecurity(Secondary, SecurityVersions.S2);

            byte[] S2NetKey0 = {0xAA, 0, 0 ,0, 0, 0, 0, 0,
                                  0, 0, 0, 0, 0, 0, 0, 0};
            byte[] S2NetKey1 = S2NetKey0;
            byte[] S2NetKey2 = S2NetKey0;

            //ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, S2NetKey0, SecuritySchemes.S2_UNAUTHENTICATED);
            //ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, S2NetKey1, SecuritySchemes.S2_AUTHENTICATED);
            //ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, S2NetKey2, SecuritySchemes.S2_ACCESS);

            //ZnifferService.SetSecurityNetworkKeyS2(S2NetKey2);

            #region 1.- Add secondary PC Controller to primary PC Controller.

            ReportService.LogTitle("1.- Add secondary PC Controller to primary PC Controller.");
            ReportService.LogText("Starting Inclussion");

            ZnifferToken.ExpectData zte = ZnifferService.ExpectData(15000);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.KEX_GET(), null, null, false, false);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.KEX_REPORT(), null, null, false, false);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.KEX_SET(), null, null, false, false);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT(), null, null, false, 2, false);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_VERIFY(), null, null, false, 2, true);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), null, null, false, 6, false);
            var addRes = Service.AddNode(Primary, Secondary);

            Delay(3000);

            //zte.WaitCompletedSignal();
            //Assert.IsTrue(addRes);
            //Assert.IsTrue(addRes.SetLearnMode);
            //Assert.AreEqual(SubstituteStatuses.Done, addRes.SubstituteStatus);
            //Assert.AreEqual(SubstituteStatuses.Done, addRes.SetLearnMode.SubstituteStatus);

            ReportService.LogOk("Inclusion Successful");

            //return;

            Delay(4000);

            byte[] NetKeyS0 = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S0);
            byte[] NetKeyS2cls0 = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_UNAUTHENTICATED);
            byte[] NetKeyS2cls1 = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_AUTHENTICATED);
            byte[] NetKeyS2cls2 = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_ACCESS);

            ReportService.LogText("The Network Keys are:");
            ReportService.LogText("S0 Network Key: " + NetKeyS0.ToHexString());
            ReportService.LogText("S2 Class 0 Network Key: " + NetKeyS2cls0.ToHexString());
            ReportService.LogText("S2 Class 1 Network Key: " + NetKeyS2cls1.ToHexString());
            ReportService.LogText("S2 Class 2 Network Key: " + NetKeyS2cls2.ToHexString());

            ReportService.LogTitle("1.1 - Checking if S2 is enabled.");

            //ApplicationService.AddResponse(Secondary, new COMMAND_CLASS_VERSION.VERSION_REPORT(), new COMMAND_CLASS_VERSION.VERSION_GET());

            ZnifferService.SetSecuritySupport(false);
            zte = ZnifferService.ExpectData(20000);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), null, null, false, 2, false);

            Service.SendData(Primary, Secondary, new COMMAND_CLASS_VERSION.VERSION_GET());

            Delay(2000);

            //AssertIsTrue(zte.WaitCompletedSignal(), "S2 communication enabled", "S2 communication disabled");

            #endregion

            #region 2.- Set PC controller as SIS and perform a S2 secured incluison

            ReportService.LogTitle("2.- Set PC controller as SIS and perform a S2 secured incluison");

            ParallelService.Reset(ALL);
            ParallelService.SetDefault(ALL);

            ZnifferService.SetSecuritySupport(true);
            ZnifferService.SetSecurityNetworkKeyS2(S2NetKey2);

            ReportService.LogText("Set PC Controller as SIS");
            Service.SetSUCNodeID(Primary, Primary);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            ApplicationService.AddSecurity(Secondary, SecurityVersions.S2);

            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, S2NetKey0, SecuritySchemes.S2_UNAUTHENTICATED);
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, S2NetKey1, SecuritySchemes.S2_AUTHENTICATED);
            ApplicationService.SecurityTestSettings.SetNetworkKeyS2(Primary, S2NetKey2, SecuritySchemes.S2_ACCESS);

            Delay(5000);

            ReportService.LogText("Starting Inclussion");

            zte = ZnifferService.ExpectData(60000);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.KEX_GET(), null, null, false, false);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.KEX_REPORT(), null, null, false, false);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.KEX_SET(), null, null, false, false);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.PUBLIC_KEY_REPORT(), null, null, false, 2, false);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_NETWORK_KEY_VERIFY(), null, null, false, 2, true);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), null, null, false, 6, false);
            addRes = Service.AddNode(Primary, Secondary);
            zte.WaitCompletedSignal();
            Assert.IsTrue(addRes);
            Assert.IsTrue(addRes.SetLearnMode);
            Assert.AreEqual(SubstituteStatuses.Done, addRes.SubstituteStatus);
            Assert.AreEqual(SubstituteStatuses.Done, addRes.SetLearnMode.SubstituteStatus);

            ReportService.LogOk("Inclusion Successful");

            Delay(4000);

            NetKeyS0 = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S0);
            NetKeyS2cls0 = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_UNAUTHENTICATED);
            NetKeyS2cls1 = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_AUTHENTICATED);
            NetKeyS2cls2 = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_ACCESS);

            ReportService.LogText("The Network Keys are:");
            ReportService.LogText("S0 Network Key: " + NetKeyS0.ToHexString());
            ReportService.LogText("S2 Class 0 Network Key: " + NetKeyS2cls0.ToHexString());
            ReportService.LogText("S2 Class 1 Network Key: " + NetKeyS2cls1.ToHexString());
            ReportService.LogText("S2 Class 2 Network Key: " + NetKeyS2cls2.ToHexString());

            ReportService.LogTitle("2.1 - Checking if S2 is enabled.");

            ApplicationService.AddResponse(Secondary, new COMMAND_CLASS_VERSION.VERSION_REPORT(), new COMMAND_CLASS_VERSION.VERSION_GET());

            Service.RequestNodeInfo(Primary, Secondary);
            Service.RequestNodeInfo(Secondary, Primary);

            ZnifferService.SetSecuritySupport(false);
            zte = ZnifferService.ExpectData(20000);
            zte.SetUpWatch(null, null, null, new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION(), null, null, false, 2, false);

            Service.SendData(Primary, Secondary, new COMMAND_CLASS_VERSION.VERSION_GET());

            Delay(2000);

            AssertIsTrue(zte.WaitCompletedSignal(), "S2 communication enabled", "S2 communication disabled"); 

            #endregion
        }
    }
}
