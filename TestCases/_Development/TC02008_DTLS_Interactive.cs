// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Net;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase("Security v2 Zip Gateway successful secured inclusion")]
    [ZipDeviceMapping(2, "z2")]
    [ZatsTestCaseOptions(ZWaveSet.SmartStart, false)]

    class TC02008_DTLS_Interactive : ZatsCodeInteractiveTestCase
    {
        protected override void ExecuteTest()
        {
            //Aliases
            Alias ZipController = new Alias(2);
            Alias ZipAssociatedDestination = new Alias(3);


            //Reset devices

            MessageBox.Show("Reset DUT Controller", "USER INSTRUCTION");

            #region Ip discovery

            ////This will get the Ip addresses on oyur setup, I'm still missing a way to select the right one....so far I choose the number manually
            var host = Dns.GetHostEntry(Dns.GetHostName());

            IPAddress PcAddressLinkGw = host.AddressList[1];
            //The IP address of the local PC's interface connected to Z/IP GW
            var GwAddresInfo = ZipService.GetIpAddress(ZipController, 0x01);
            IPAddress GwAddress = GwAddresInfo.IpAddress;
            foreach (IPAddress addrss in host.AddressList)
            {
                if (GwAddress.GetAddressBytes()[0] == addrss.GetAddressBytes()[0] && GwAddress.GetAddressBytes()[1] == addrss.GetAddressBytes()[1])
                {
                    PcAddressLinkGw = addrss;
                }
            }
            byte[] unsIPv6addr = PcAddressLinkGw.GetAddressBytes();
            #endregion

            #region Setting and configuring Unsolicited Destination for Gateway

            ushort zipPort = 41230;
            ushort newUnsolPort = zipPort;
            byte[] newUnsPort = BitConverter.GetBytes(newUnsolPort);
            Array.Reverse(newUnsPort);
            IPEndPoint unsolEndPoint = new IPEndPoint(IPAddress.IPv6Any, zipPort);
            byte[] AckUdpPack = new byte[] { 0x23, 0x02, 0x40, 0, 0, 0, 0 };

            ReportService.LogText("Setting a Unsolicited Destination");
            ZipService.SendData(ZipController, new COMMAND_CLASS_ZIP_GATEWAY.UNSOLICITED_DESTINATION_SET()
            {
                unsolicitedIpv6Destination = unsIPv6addr,
                unsolicitedDestinationPort = newUnsPort
            });
            ReportService.LogText("Verifing new Unsolicited IPv6 address for Z/IP GW");
            var token = ZipAsyncService.ExpectData(ZipController, new COMMAND_CLASS_ZIP_GATEWAY.UNSOLICITED_DESTINATION_REPORT(), 5000);
            ZipService.SendData(ZipController, new COMMAND_CLASS_ZIP_GATEWAY.UNSOLICITED_DESTINATION_GET());
            var expectedResult = token.WaitCompletedSignal();
            if (expectedResult)
            {
                COMMAND_CLASS_ZIP_GATEWAY.UNSOLICITED_DESTINATION_REPORT result = expectedResult.ReceivedData;
                ReportService.LogText("Current Unsolicited Destination - " + result.unsolicitedIpv6Destination.ToHexString() +
                    ":" + result.unsolicitedDestinationPort.ToHexString());

                int unsPort = (result.unsolicitedDestinationPort[0] << 8) + result.unsolicitedDestinationPort[1];
                ReportService.LogText("Z/IP GW is using the port - " + unsPort);
            }

            ZipService.RegisterUnsolicitedZipDevice(ZipAssociatedDestination, new IPEndPoint(ZipService.GetIpAddress(ZipController, 0x01).IpAddress, zipPort));

            #endregion

            //Reset ZIP device
            ZipService.SetDefault(ZipController);

            #region 1.- Including PC Controller to GW

            ReportService.LogTitle("1.- Including PC Controller to GW");

            ApplicationService.SecurityTestSettings.IncludeSettings.IsInteractiveMode = true;
            var AddZipToken = ZipAsyncService.AddNode(ZipController, 60000);

            MessageBox.Show("Put DUT Controller in Learn Mode", "USER INSTRUCTION");

            var AddZipRes = AddZipToken.WaitCompletedSignal();
            AssertIsTrue(AddZipRes, "Inclusion Succeeded", "Inclusion Failed");
            Delay(3000);

            ReportService.LogTitle("1.1- Removing controller from GW");

            var RemoveZipToken = ZipAsyncService.RemoveNode(ZipController, Modes.NodeAny, 60000);

            MessageBox.Show("Put DUT Controller in Learn Mode.", "USER INSTRUCTION");

            var RemoveZipRes = RemoveZipToken.WaitCompletedSignal();

            #endregion

            //Delay(30000);

            #region 2.- Including GW to PC Controller
            //PC Controller as SIS
            MessageBox.Show("Set DUT Controller as SIS.", "USER INSTRUCTION");

            //Getting the GW DSK
            var ExpRes = ZipAsyncService.ExpectData(ZipController, new COMMAND_CLASS_NETWORK_MANAGEMENT_BASIC_V2.DSK_REPORT(), 10000);
            ZipService.SendData(ZipController, new COMMAND_CLASS_NETWORK_MANAGEMENT_BASIC_V2.DSK_GET());
            COMMAND_CLASS_NETWORK_MANAGEMENT_BASIC_V2.DSK_REPORT ExpDSK = ExpRes.WaitCompletedSignal().ReceivedData;
            Delay(2000);

            ReportService.LogTitle("2.- Including GW to PC Controller");
            int dsk = (ExpDSK.dsk[0] << 8) + ExpDSK.dsk[1];

            MessageBox.Show("Start Add Node on DUT Controller (DSK=" + dsk.ToString("00000") + ")", "USER INSTRUCTION");

            var learnRes = ZipService.LearnMode(ZipController, LearnModes.LearnModeClassic, 30000);

            Delay(20000);

            AssertIsTrue(learnRes);

            ReportService.LogOk("Inclusion Successful");

            #endregion
        }
    }
}
