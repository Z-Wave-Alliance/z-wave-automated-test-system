// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
﻿using System;
using System.Linq;
using Zats.Services;

namespace ZatsTests.Security_v2
{
    [ZatsTestCase("Serial API test - Authenticating and decripting all network keys")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]

    class Serial_API_DevTest_JBU : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave = new Alias(2);

            Alias[] ALL = new Alias[] { Primary, Slave };

            ParallelService.Reset(ALL);

            //Get nodes Library, Supported API commands, Id, HomeId
            ParallelService.SerialApiGetInitData(ALL);
            ParallelService.SerialApiGetCapabilities(ALL);
            ParallelService.MemoryGetId(ALL);

            ApplicationService.AddBasic(Slave);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S2 | SecurityVersions.S0);

            #region 1.- Inclusion

            ReportService.LogTitle("1.- Add sdk 670 slave to primary PC Controller.");
            ReportService.LogText("Starting Inclussion");

            var addRes = Service.AddNode(Primary, Slave);

            //Delay(3000);

            Assert.IsTrue(addRes);
            Assert.IsTrue(addRes.SetLearnMode);
            Assert.AreEqual(SubstituteStatuses.Done, addRes.SubstituteStatus);

            ReportService.LogOk("Inclusion Successful");

            //Delay(4000);

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

            //ApplicationService.AddResponse(Slave, new COMMAND_CLASS_VERSION.VERSION_REPORT(), new COMMAND_CLASS_VERSION.VERSION_GET());

            //Service.SendBroadCastNodeInfo(Slave, TransmitOptions.None);

            #endregion

            byte[] VerGet = new COMMAND_CLASS_VERSION.VERSION_GET();

            #region 2.- Send Encripted using S2 Network Key - Unathenticated

            SecuritySchemes[] netkeys = { SecuritySchemes.S2_UNAUTHENTICATED, SecuritySchemes.S2_AUTHENTICATED, SecuritySchemes.S2_ACCESS };
            SecuritySchemes[] exp_sec_schemes = { SecuritySchemes.S2_UNAUTHENTICATED, SecuritySchemes.S2_AUTHENTICATED, SecuritySchemes.S2_ACCESS };

            string[] netkeys_str = { "Unathenticated", "Authenticated", "Access" };

            for (int i = 0; i < 3; i++)
            {

                ReportService.LogTitle("2.- Send Encripted to sdk670 slave using S2 Network Key - " + netkeys_str[i]);

                ApplicationService.SecurityTestSettings.SetActiveSecurityScheme(Primary, Slave, netkeys[i]);


                ReportService.LogText("Sending Version Get Encripted with the " + netkeys_str[i] + " key");

                var ExpRes = AsyncService.ExpectData(Slave, new COMMAND_CLASS_VERSION.VERSION_GET(), 20000);

                //var ReqRes = Service.RequestData(Primary, Slave, new COMMAND_CLASS_VERSION.VERSION_GET(), new COMMAND_CLASS_VERSION.VERSION_REPORT());
                Service.SendData(Primary, Slave, new COMMAND_CLASS_VERSION.VERSION_GET());

                var decriptedFrame = ExpRes.WaitCompletedSignal();

                Assert.IsTrue(decriptedFrame);

                AssertIsTrue(decriptedFrame.Command.SequenceEqual(VerGet),
                    "Slave can authenticate and decript frames encripted with " + netkeys_str[i] + " key", "Version Get not received");
                AssertIsTrue(exp_sec_schemes[i] == decriptedFrame.SecurityScheme, "Received using Scheme: "
                    + decriptedFrame.SecurityScheme);

            }
            #endregion

            foreach (SecuritySchemes sec_scheme in Enum.GetValues(typeof(SecuritySchemes)))
            {
                if (sec_scheme == SecuritySchemes.S0)
                    ApplicationService.SetSecurityVersionForNode(Primary, Slave, SecurityVersions.S0 | SecurityVersions.S2);
                var doorLockRep = new COMMAND_CLASS_DOOR_LOCK.DOOR_LOCK_OPERATION_REPORT();
                ReportService.LogText("Sending from slave with key " + sec_scheme);
                // TODO: Set up expectData on ZATS to verify the message is indeed received
                var eToken = AsyncService.ExpectData(Primary, doorLockRep, 2000);
                var sendRes = Service.SendDataEx(Slave, Primary, doorLockRep, TX_OPTIONS, sec_scheme);
                var frame = eToken.WaitCompletedSignal();
                Assert.AreEqual(sec_scheme, frame.SecurityScheme);
            }

            Service.SendBroadCastNodeInfo(Slave, TX_OPTIONS);

            ApplicationService.SecurityTestSettings.SetActiveSecurityScheme(Primary, Slave, SecuritySchemes.S2_AUTHENTICATED);
            byte[] securityCommandsSupportedGet = new COMMAND_CLASS_SECURITY_2.SECURITY_2_COMMANDS_SUPPORTED_GET();
            Service.SendData(Primary, Slave, securityCommandsSupportedGet);
        }
    }
}
