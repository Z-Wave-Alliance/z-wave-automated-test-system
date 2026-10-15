// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("Security v2 ZIP Gateway managing multiple group key")]
    [DeviceMapping(1, LibTypes.ControllerStaticLib)]
    [ZipDeviceMapping(2, "z2")]
    [DeviceMapping(3, LibTypes.EndDeviceLib)]
    [DeviceMapping(4, LibTypes.EndDeviceLib)]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]

    class TC02016_DTLS_Interactive : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias Controller = new Alias(1);
            Alias ZipController = new Alias(2);
            Alias Slave1 = new Alias(3);
            Alias Slave2 = new Alias(4);

            //Prepare BasicApi ALL macro
            Alias[] ALL = new Alias[] { Controller, Slave1, Slave2 };

            //Reset devices
            ParallelService.Reset(ALL);

            //Set node information
            Service.ApplicationNodeInformation(Controller, true, (byte)GenericTypes.STATIC_CONTROLLER, (byte)SpecificTypes.PC_CONTROLLER,
                new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_CONTROLLER_REPLICATION.ID, COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID, 
                    COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID, COMMAND_CLASS_BASIC.ID});
            Service.ApplicationNodeInformation(Slave1, true, (byte)GenericTypes.SWITCH_MULTILEVEL, (byte)SpecificTypes.NOT_USED,
                new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_CONTROLLER_REPLICATION.ID, COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID,
                    COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID, COMMAND_CLASS_BASIC.ID});
            Service.ApplicationNodeInformation(Slave2, true, (byte)GenericTypes.SWITCH_MULTILEVEL, (byte)SpecificTypes.NOT_USED,
                new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_CONTROLLER_REPLICATION.ID, COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID,
                    COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID, COMMAND_CLASS_BASIC.ID});

            Service.ApplicationNodeInformationCmdClasses(Controller,
                    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_CONTROLLER_REPLICATION.ID, COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID, 
                        COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID, COMMAND_CLASS_BASIC.ID},
                    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID, COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID },
                    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_CONTROLLER_REPLICATION.ID });
            Service.ApplicationNodeInformationCmdClasses(Slave1,
                    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_CONTROLLER_REPLICATION.ID, COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID, 
                        COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID, COMMAND_CLASS_BASIC.ID},
                    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID, COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID },
                    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_CONTROLLER_REPLICATION.ID });
            Service.ApplicationNodeInformationCmdClasses(Slave2,
                    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_CONTROLLER_REPLICATION.ID, COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID, 
                        COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID, COMMAND_CLASS_BASIC.ID},
                    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_MANUFACTURER_SPECIFIC.ID, COMMAND_CLASS_SECURITY.ID, COMMAND_CLASS_SECURITY_2.ID },
                    new byte[] { COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_BASIC.ID, COMMAND_CLASS_CONTROLLER_REPLICATION.ID });

            //Get nodes Library, Supported API commands, Id, HomeId
            ParallelService.SerialApiGetInitData(ALL);
            ParallelService.SerialApiGetCapabilities(ALL);
            ParallelService.MemoryGetId(ALL);

            //Reset ZIP device
            ZipService.SetDefault(ZipController);
            Delay(2000);
            ApplicationService.AddSecurity(Controller, SecurityVersions.S2);

            #region 1.- Including PC Controller and Slaves to GW

            byte ZipID = 0x01;

            ReportService.LogTitle("1.1- Including Controller to GW");
            var AddZipRes = ZipService.AddNodeWithDSK(ZipController, Controller, 34080);
            byte CtrlID = AddZipRes.NodeId;
            AssertIsTrue(AddZipRes, "Inclusion Succeeded", "Inclusion Failed");
            Delay(3000);

            ReportService.LogTitle("1.2- Including Slave 1 to GW");
            AddZipRes = ZipService.AddNodeWithDSK(ZipController, Slave1, 56990);
            byte Slv1ID = AddZipRes.NodeId;
            AssertIsTrue(AddZipRes, "Inclusion Succeeded", "Inclusion Failed");
            Delay(3000);

            ReportService.LogTitle("1.3- Including Slave 2 to GW");
            AddZipRes = ZipService.AddNodeWithDSK(ZipController, Slave2, 56990);
            byte Slv2ID = AddZipRes.NodeId;
            AssertIsTrue(AddZipRes, "Inclusion Succeeded", "Inclusion Failed");

            Delay(10000);

            byte[] NetKeyS0 = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Controller, SecuritySchemes.S0);
            byte[] SharedNetKey = ApplicationService.SecurityTestSettings.GetNetworkKeyS2Temp(Controller);
            byte[] NetKeyS2cls0 = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Controller, SecuritySchemes.S2_UNAUTHENTICATED);
            byte[] NetKeyS2cls1 = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Controller, SecuritySchemes.S2_AUTHENTICATED);
            byte[] NetKeyS2cls2 = ApplicationService.SecurityTestSettings.GetNetworkKeyS2(Controller, SecuritySchemes.S2_ACCESS);


            ReportService.LogText("The Network Keys are:");
            ReportService.LogText("S0 Network Key: " + NetKeyS0.ToHexString());
            ReportService.LogText("Shared Network Key: " + SharedNetKey.ToHexString());
            ReportService.LogText("S2 Class 0 Network Key: " + NetKeyS2cls0.ToHexString());
            ReportService.LogText("S2 Class 1 Network Key: " + NetKeyS2cls1.ToHexString());
            ReportService.LogText("S2 Class 2 Network Key: " + NetKeyS2cls2.ToHexString());

            #endregion

            Delay(10000);

            ReportService.LogTitle("GW Receiving");

            #region 2.- Controller and Slaves send to GW using a different Network Key each of them

            //Adding Responses at nodes
            ApplicationService.AddBasicEx(Controller);
            ApplicationService.AddBasicEx(Slave1);
            ApplicationService.AddBasicEx(Slave2);

            ReportService.LogTitle("2. Controller and Slaves send to GW using a different Network Key each of them");

            //Controller
            ReportService.LogText("Controller Sending with unauthenticated key");
            ApplicationService.SecurityTestSettings.SetActiveSecurityScheme(Controller, ZipID, SecuritySchemes.S2_UNAUTHENTICATED);

            Delay(2000);

            var ExpRes = AsyncService.ExpectData(Controller, new COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_REPORT(), 20000);
            ComplianceService.SendData(Controller, ZipID, new COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_GET());

            var RxFrame = ExpRes.WaitCompletedSignal();

            AssertIsTrue(RxFrame, "Controller Received Version Report", "Version Report not Received");

            Delay(5000);

            //Slave1
            ReportService.LogText("Slave1 Sending with authenticated key");

            ExpRes = AsyncService.ExpectData(Slave1, new COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_REPORT());
            ComplianceService.SendDataEx(Slave1, ZipID, new COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_GET(),
                TransmitOptions.Ack|TransmitOptions.AutoRoute|TransmitOptions.Explore,
                TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY, SecuritySchemes.S2_AUTHENTICATED);

            AssertIsTrue(ExpRes.WaitCompletedSignal(), "Received Version Report", "Version Report not Received");

            Delay(5000);

            //Slave2
            ExpRes = AsyncService.ExpectData(Slave2, new COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_REPORT());
            ComplianceService.SendDataEx(Slave2, ZipID, new COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_GET(),
                TransmitOptions.Ack | TransmitOptions.AutoRoute | TransmitOptions.Explore,
                TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY, SecuritySchemes.S2_ACCESS);

            AssertIsTrue(ExpRes.WaitCompletedSignal(), "Received Version Report", "Version Report not Received");

            #endregion

            #region 3.- Sending from  Controller and Slaves using a different Network key

            ReportService.LogTitle("3. Controller and Slaves send to GW using a different Network Key each of them");

            //Controller
            ReportService.LogText("Controller Sending with Authenticated key");
            ApplicationService.SecurityTestSettings.SetActiveSecurityScheme(Controller, ZipID, SecuritySchemes.S2_AUTHENTICATED);

            Delay(5000);

            ExpRes = AsyncService.ExpectData(Controller, new COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_REPORT());
            Delay(1000);
            ComplianceService.SendData(Controller, ZipID, new COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_GET());

            AssertIsTrue(ExpRes.WaitCompletedSignal(), "Controller Received Version Report", "Version Report not Received");

            Delay(5000);

            //Slave1
            ReportService.LogText("Slave1 Sending with Access key");

            ExpRes = AsyncService.ExpectData(Slave1, new COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_REPORT());
            Delay(1000);
            ComplianceService.SendDataEx(Slave1, ZipID, new COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_GET(), TransmitOptions.Ack,
                TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY, SecuritySchemes.S2_ACCESS);

            AssertIsTrue(ExpRes.WaitCompletedSignal(), "Received Version Report", "Version Report not Received");

            Delay(5000);

            //Slave2
            ReportService.LogText("Slave2 Sending with Unauthenticated key");

            ExpRes = AsyncService.ExpectData(Slave2, new COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_REPORT());
            Delay(1000);
            ComplianceService.SendDataEx(Slave2, ZipID, new COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_GET(), TransmitOptions.Ack,
                TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY, SecuritySchemes.S2_UNAUTHENTICATED);

            AssertIsTrue(ExpRes.WaitCompletedSignal(), "Received Version Report", "Version Report not Received");

            #endregion

            ReportService.LogTitle("GW Transmitting");

            byte[] ExtHeaderUnauthenticated = new byte[] { 0x84, 0x02, 0x01, 0 };
            byte[] ExtHeaderAuthenticated = new byte[] { 0x84, 0x02, 0x02, 0 };
            byte[] ExtHeaderAccess = new byte[] { 0x84, 0x02, 0x04, 0 };

            #region 2- Sending from GW using the extended header to select the network key

            ReportService.LogTitle("2- Sending from GW using the extended header to select the network key");
            
            //Controller
            ReportService.LogText("GW sending with Unauthenticated key to Controller");

            Delay(5000);

            ExpRes = AsyncService.ExpectData(Controller, new COMMAND_CLASS_BASIC.BASIC_GET(), 20000);

            ZipService.SendData(Controller, ExtHeaderUnauthenticated, new COMMAND_CLASS_BASIC.BASIC_GET(), true);            

            try
            {
                Inconclusive(ExpRes.WaitCompletedSignal().SecurityScheme == SecuritySchemes.S2_UNAUTHENTICATED, "Security Scheme is correct", "Security Scheme is not correct");
            }
            catch
            {
                Inconclusive("Unexpected behavior occurred");
            }

            //Slave1
            ReportService.LogText("GW sending with Authenticated key to Slave 1");

            Delay(5000);

            ExpRes = AsyncService.ExpectData(Slave1, new COMMAND_CLASS_BASIC.BASIC_GET(), 20000);

            ZipService.SendData(Slave1, ExtHeaderAuthenticated, new COMMAND_CLASS_BASIC.BASIC_GET(), true);

            try
            {
                Inconclusive(ExpRes.WaitCompletedSignal().SecurityScheme == SecuritySchemes.S2_AUTHENTICATED, "Security Scheme is correct", "Security Scheme is not correct");
            }
            catch
            {
                Inconclusive("Unexpected behavior occurred");
            }

            //Slave2
            ReportService.LogText("GW sending with Access key to Slave 2");

            Delay(5000);

            ExpRes = AsyncService.ExpectData(Slave2, new COMMAND_CLASS_BASIC.BASIC_GET(), 20000);

            ZipService.SendData(Slave2, ExtHeaderAccess, new COMMAND_CLASS_BASIC.BASIC_GET(), true);

            try
            {
                Inconclusive(ExpRes.WaitCompletedSignal().SecurityScheme == SecuritySchemes.S2_ACCESS, "Security Scheme is correct", "Security Scheme is not correct");
            }
            catch
            {
                Inconclusive("Unexpected behavior occurred");
            }

            #endregion

            #region 3- Sending from GW using the extended header to select the network key - different network key

            ReportService.LogTitle("3- Sending from GW using the extended header to select the network key - different network key");

            //Controller
            ReportService.LogText("GW sending with Authenticated key to Controller");

            Delay(5000);

            ExpRes = AsyncService.ExpectData(Controller, new COMMAND_CLASS_BASIC.BASIC_GET(), 20000);

            ZipService.SendData(Controller, ExtHeaderAuthenticated, new COMMAND_CLASS_BASIC.BASIC_GET(), true);
                        

            try
            {
                Inconclusive(ExpRes.WaitCompletedSignal().SecurityScheme == SecuritySchemes.S2_AUTHENTICATED, "Security Scheme is correct", "Security Scheme is not correct");
            }
            catch
            {
                Inconclusive("Unexpected behavior occurred");
            }

            //Slave1
            ReportService.LogText("GW sending with Access key to Slave 1");

            Delay(5000);

            ExpRes = AsyncService.ExpectData(Slave1, new COMMAND_CLASS_BASIC.BASIC_GET(), 20000);

            ZipService.SendData(Slave1, ExtHeaderAccess, new COMMAND_CLASS_BASIC.BASIC_GET(), true);

            try
            {
                Inconclusive(ExpRes.WaitCompletedSignal().SecurityScheme == SecuritySchemes.S2_ACCESS, "Security Scheme is correct", "Security Scheme is not correct");
            }
            catch
            {
                Inconclusive("Unexpected behavior occurred");
            }

            //Slave2
            ReportService.LogText("GW sending with Unauthenticated key to Slave 2");

            Delay(5000);

            ExpRes = AsyncService.ExpectData(Slave2, new COMMAND_CLASS_BASIC.BASIC_GET(), 20000);

            ZipService.SendData(Slave2, ExtHeaderUnauthenticated, new COMMAND_CLASS_BASIC.BASIC_GET(), true);

            try
            {
                Inconclusive(ExpRes.WaitCompletedSignal().SecurityScheme == SecuritySchemes.S2_UNAUTHENTICATED, "Security Scheme is correct", "Security Scheme is not correct");
            }
            catch
            {
                Inconclusive("Unexpected behavior occurred");
            }

            #endregion

            Delay(5000);
        }
    }
}
