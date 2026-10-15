// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
﻿using Zats.Services;

namespace ZatsTests.Security_v2
{
    [ZatsTestCase("Security v2 Slave SerialAPI - Synchronization")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]//Modify with your serial API sdk 6.

    class Serial_API_Synchronization : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Primary = new Alias(1);
            Alias Slave = new Alias(2);

            Alias[] ALL = new Alias[] { Primary, Slave };

            ParallelService.Reset(ALL);

            ApplicationService.AddSecurity(Primary, SecurityVersions.S2);
            ApplicationService.AddBasic(Primary);

            byte[] wrongSPAN = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                                   0x09, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };

            #region 1.- Add Serial API to primary PC Controller.

            ReportService.LogTitle("1.- Add Serial API slave to primary PC Controller.");
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

            Delay(2000);

            ApplicationService.AddBasicEx(Slave, SecuritySchemes.S2_UNAUTHENTICATED);
            ApplicationService.SecurityTestSettings.SetActiveSecurityScheme(Primary, Slave, SecuritySchemes.S2_UNAUTHENTICATED);

            Service.RequestData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());

            Delay(1000);

            ApplicationService.SecurityTestSettings.SetSpanS2(Primary, wrongSPAN, 1);

            Service.RequestData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());

            Delay(3000);

            Service.RequestData(Primary, Slave, new COMMAND_CLASS_BASIC.BASIC_GET(), new COMMAND_CLASS_BASIC.BASIC_REPORT());

            Delay(3000);       
        
        }
    }
}
