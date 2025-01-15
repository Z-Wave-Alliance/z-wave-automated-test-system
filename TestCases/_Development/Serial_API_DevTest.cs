// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
﻿using System;
using System.Linq;
using Zats.Services;

namespace ZatsTests.Security_v2
{
    [ZatsTestCase("Serial API test - Authenticating and decripting all network keys")]
    [DeviceMapping(1, "StaticController1", LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, "Slave1", LibTypes.EndDeviceLib)]

    class Serial_API_DevTest : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave = new Alias(2);

            Alias[] ALL = new Alias[] { Primary, Slave };

            ParallelService.Reset(ALL);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            ApplicationService.AddBasic(Primary);

            #region 1.- Add Serial API to primary PC Controller.

            ReportService.LogTitle("1.- Add slave to primary PC Controller.");
            ReportService.LogText("Starting Inclussion");

            var addRes = Service.AddNodeWithDSK(Primary, Slave, 56990);

            Assert.IsTrue(addRes);
            Assert.IsTrue(addRes.SetLearnMode);
            Assert.AreEqual(SubstituteStatuses.Done, addRes.SubstituteStatus);

            ReportService.LogOk("Inclusion Successful");

            byte[] NetKeyS0 = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S0);
            byte[] SharedNetKey = ApplicationService.SecurityTestSettings.GetNetworkKeyS2Temp(Primary);
            byte[] NetKeyS2cls0 = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_UNAUTHENTICATED);
            byte[] NetKeyS2cls1 = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_AUTHENTICATED);
            byte[] NetKeyS2cls2 = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Primary, SecuritySchemes.S2_ACCESS);

            ReportService.LogText("The Network Keys are:");
            ReportService.LogText("S0 Network Key: " + NetKeyS0.ToHexString());
            ReportService.LogText("Shared Network Key: " + SharedNetKey.ToHexString());
            ReportService.LogText("S2 Class 0 Network Key: " + NetKeyS2cls0.ToHexString());
            ReportService.LogText("S2 Class 1 Network Key: " + NetKeyS2cls1.ToHexString());
            ReportService.LogText("S2 Class 2 Network Key: " + NetKeyS2cls2.ToHexString());

            #endregion

            byte[] BasicGet = new COMMAND_CLASS_BASIC.BASIC_GET();

            #region 2.- Send Encripted using S2 Network Key - Unathenticated

            SecuritySchemes[] netkeys = { SecuritySchemes.S2_UNAUTHENTICATED, SecuritySchemes.S2_AUTHENTICATED, SecuritySchemes.S2_ACCESS };
            SecuritySchemes[] SecSchem = { SecuritySchemes.S2_UNAUTHENTICATED, SecuritySchemes.S2_AUTHENTICATED, SecuritySchemes.S2_ACCESS };

            string[] netkeys_str = { "Unathenticated", "Authenticated", "Access" };
            byte[][] netkeys_byte = { NetKeyS2cls0, NetKeyS2cls1, NetKeyS2cls2, NetKeyS0 };
            BasicToken.ExpectData ExpRes;

            for (int i = 0; i < 3; i++)
            {

                ReportService.LogTitle("2.- Send Encripted to sdk670 slave using S2 Network Key - " + netkeys_str[i]);

                ApplicationService.SecurityTestSettings.SetActiveSecurityScheme(Primary, Slave, netkeys[i]);

                ZnifferService.SetSecurityNetworkKeyS2(netkeys_byte[i]);

                ReportService.LogText("Sending Version Get Encripted with the " + netkeys_str[i] + " key");

                ExpRes = AsyncService.ExpectData(Slave, new COMMAND_CLASS_BASIC.BASIC_GET(), 20000);

                Service.SendData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_GET());

                Delay(2000);

                var decriptedFrame = ExpRes.WaitCompletedSignal();

                Assert.IsTrue(decriptedFrame);

                AssertIsTrue(decriptedFrame.Command.SequenceEqual(BasicGet),
                    "Slave can authenticate and decript frames encripted with " + netkeys_str[i] + " key", "Version Get not received");

            }
            #endregion
            
            int ind = 0;

            foreach (SecuritySchemes sec_scheme in Enum.GetValues(typeof(SecuritySchemes)))
            {
                if (sec_scheme != SecuritySchemes.S0)
                {

                    ExpRes = AsyncService.ExpectData(Primary, new COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_REPORT(), 20000);

                    var doorLockRep = new COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_REPORT();
                    ReportService.LogText("Sending from slave with key " + sec_scheme);

                    var sendRes = Service.SendDataEx(Slave, Primary, doorLockRep, sec_scheme);

                    AssertIsTrue(ExpRes.WaitCompletedSignal(), "Version Get received and authenticated at controller",
                        "Version Get not received nor authenticated at controller");

                    ZnifferService.SetSecurityNetworkKeyS2(netkeys_byte[ind]);
                }
                ind++;
            }
        }
    }
}
