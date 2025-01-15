// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
﻿using Zats.Services;

namespace ZatsTests.Security_v2
{
    [ZatsTestCase("Security v2 Slave SerialAPI - Reusing public key after inclusion")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, "Slave1", LibTypes.EndDeviceLib)]//Modify with your serial API sdk 6.
    [DeviceMapping(3, LibTypes.EndDeviceLib)]

    class Serial_API_DevTest_Compatibility : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave70 = new Alias(2);
            Alias Slave60 = new Alias(3);
                
            Alias[] ALL = new Alias[] { Primary, Slave70, Slave60 };

            ParallelService.Reset(ALL);

            //Get nodes Library, Supported API commands, Id, HomeId
            ParallelService.SerialApiGetInitData(ALL);
            ParallelService.SerialApiGetCapabilities(ALL);
            ParallelService.MemoryGetId(ALL);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S2 | SecurityVersions.S0);
            ApplicationService.AddSecurity(Slave60, SecurityVersions.S0);

            ApplicationService.AddBasic(Primary);
            ApplicationService.AddBasic(Slave60);

            #region 1.- Inclusion

            ReportService.LogTitle("1.1- Add sdk 670 slave to primary PC Controller.");
            ReportService.LogText("Starting Inclussion");

            var addRes = Service.AddNode(Primary, Slave70);

            Assert.IsTrue(addRes);
            Assert.IsTrue(addRes.SetLearnMode);
            Assert.AreEqual(SubstituteStatuses.Done, addRes.SubstituteStatus);

            ReportService.LogOk("Inclusion Successful");

            ReportService.LogTitle("1.2- Add sdk 660 slave to primary PC Controller.");
            ReportService.LogText("Starting Inclussion");

            addRes = Service.AddNode(Primary, Slave60);

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

            SecuritySchemes[] KeyTypes = { SecuritySchemes.S2_UNAUTHENTICATED, SecuritySchemes.S2_AUTHENTICATED,
                                               SecuritySchemes.S2_ACCESS};
            SecuritySchemes[] SecSchemes = { SecuritySchemes.S2_UNAUTHENTICATED, SecuritySchemes.S2_AUTHENTICATED,
                                               SecuritySchemes.S2_ACCESS};

            for (int ind = 0; ind < 3; ind++)
            {
                #region Slave from 6.70 acts as a receiver

                //Primary sending
                ApplicationService.SecurityTestSettings.SetActiveSecurityScheme(Primary, Slave70, KeyTypes[ind]);
                var ExpSlv = AsyncService.ExpectData(Slave70, new COMMAND_CLASS_BASIC.BASIC_GET());
                Service.SendData(Primary, Slave70, new COMMAND_CLASS_BASIC.BASIC_GET());
                Delay(1000);
                var ExpResSlv = ExpSlv.WaitCompletedSignal();
                AssertIsTrue(ExpResSlv, "Authentication achieved", "Authentication not achieved");

                //Slave sending
                ExpSlv = AsyncService.ExpectData(Slave70, new COMMAND_CLASS_BASIC.BASIC_GET());
                Service.SendData(Slave60, Slave70, new COMMAND_CLASS_BASIC.BASIC_GET());
                Delay(1000);
                ExpResSlv = ExpSlv.WaitCompletedSignal();
                AssertIsTrue(ExpResSlv, "Authentication achieved", "Authentication not achieved");

                #endregion

                #region Slave from 6.70 acts as a transmitter

                //Primary receiving
                ExpSlv = AsyncService.ExpectData(Primary, new COMMAND_CLASS_BASIC.BASIC_GET());
                var ExpCntrl = AsyncService.ExpectData(Slave70, new COMMAND_CLASS_BASIC.BASIC_REPORT());
                Service.SendDataEx(Slave70, Primary, new COMMAND_CLASS_BASIC.BASIC_GET(), SecSchemes[ind]);
                Delay(1000);
                ExpResSlv = ExpSlv.WaitCompletedSignal();
                var ExpResCtrl = ExpCntrl.WaitCompletedSignal();
                AssertIsTrue(ExpResCtrl && ExpResSlv, "Authentication achieved", "Authentication not achieved");

                //Slave receiving
                ExpSlv = AsyncService.ExpectData(Slave60, new COMMAND_CLASS_BASIC.BASIC_GET());
                ExpCntrl = AsyncService.ExpectData(Slave70, new COMMAND_CLASS_BASIC.BASIC_REPORT());
                Service.SendDataEx(Slave70, Slave60, new COMMAND_CLASS_BASIC.BASIC_GET(), SecuritySchemes.S0);
                Delay(1000);
                ExpResSlv = ExpSlv.WaitCompletedSignal();
                ExpResCtrl = ExpCntrl.WaitCompletedSignal();
                AssertIsTrue(ExpResCtrl && ExpResSlv, "Authentication achieved", "Authentication not achieved");

                #endregion
            }
        }
    }
}
