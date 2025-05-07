// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Net;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"Test of inclusion controller CC")]
    [ZipDeviceMapping(1, "z2")]
    [DeviceMapping(2, LibTypes.ControllerStaticLib)]
    [DeviceMapping(3, LibTypes.ControllerStaticLib)]
    public class appzip_InclusionControllerAddNode : ZatsCodeTestCase
    {
        protected override void ExecuteTest()
        {
            Alias ZipController = new Alias(1);
            Alias InclusionController = new Alias(2);
            Alias ControllerAsNode = new Alias(3);

            //Prepare BasicApi ALL macro
            Alias[] ALL = new Alias[] { InclusionController, ControllerAsNode };

            Service.Reset(ControllerAsNode);
            Service.Reset(InclusionController);

            ZipService.SetDefault(ZipController);

            ApplicationService.AddSecurity(InclusionController, SecurityVersions.S2);
            ApplicationService.AddSecurity(ControllerAsNode, SecurityVersions.S2);

            Service.ApplicationNodeInformation(InclusionController, true, (byte)GenericTypes.STATIC_CONTROLLER,
                (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_SECURITY_2.ID });
            Service.ApplicationNodeInformation(ControllerAsNode, true, (byte)GenericTypes.STATIC_CONTROLLER,
                (byte)SpecificTypes.PC_CONTROLLER, new byte[] { COMMAND_CLASS_SECURITY_2.ID });

            //Reset devices
            //ParallelService.SetDefault(ALL);

            //Reset Zip
            //ZipService.SetDefault(ZipController);

            ReportService.LogWarning("Modify with you Zip Gateway Ipv6 address  - Missing features");

            //This will get the Ip addresses on oyur setup, I'm still missing a way to select the right one....so far I choose the number manually
            var host = Dns.GetHostEntry(Dns.GetHostName());

            //The IP address of the local PC's interface connected to Z/IP GW
            //byte[] unsIPv6addr = host.AddressList[2].GetAddressBytes(); //The IPv6 address of the current PC
            byte[] unsIPv6addr = host.AddressList[4].GetAddressBytes();
            byte[] unsIPv4addr = new byte[] { 0xC0, 0xA8, 0x01, 0xBC };    // 192:168:1:188 - the IPv4 address of the current PC
            byte[] zipIPv6addr = new byte[] { 0xFD, 0, 0xAD, 0xBE, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x03 }; //The IPv6 address of the current PC

            byte[] defUnsIPaddr = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            byte[] zeroUnsIPaddr = defUnsIPaddr;
            byte[] defUnsPort = new byte[] { 0, 0, 0, 0 };
            //the Network Key to be used in the Secure network
            byte[] NetKey = new byte[0];

            //Set up a UDP Listener
            ushort zipPort = 41230;
            byte[] newUnsPort = BitConverter.GetBytes(zipPort);
            Array.Reverse(newUnsPort);
            byte[] AckUdpPack = new byte[] { 0x23, 0x02, 0x40, 0, 0, 0, 0 };

            #region Set a new Unsolicited IPv6 address for Z/IP GW

            ReportService.LogTitle("Set a new Unsolicited IPv6 address for Z/IP GW");

            ZipService.SendData(ZipController, new COMMAND_CLASS_ZIP_GATEWAY.UNSOLICITED_DESTINATION_SET()
            {
                unsolicitedDestinationPort = newUnsPort,
                unsolicitedIpv6Destination = unsIPv6addr
            });


            ZipToken.ExpectData token = ZipAsyncService.ExpectData(ZipController, new COMMAND_CLASS_ZIP_GATEWAY.UNSOLICITED_DESTINATION_REPORT(), 5000);
            ZipService.SendData(ZipController, new COMMAND_CLASS_ZIP_GATEWAY.UNSOLICITED_DESTINATION_GET());
            ZipResult.ExpectData expectedResult = token.WaitCompletedSignal();
            if (expectedResult)
            {
                COMMAND_CLASS_ZIP_GATEWAY.UNSOLICITED_DESTINATION_REPORT result = expectedResult.ReceivedData;
                ReportService.LogText("Current Unsolicited Destination - " + result.unsolicitedIpv6Destination.ToHexString() +
                    ":" + result.unsolicitedDestinationPort.ToHexString());

                int unsPort = (result.unsolicitedDestinationPort[0] << 8) + result.unsolicitedDestinationPort[1];
                if (unsPort != zipPort)
                {
                    ReportService.LogWarning("Z/IP GW is not using the default port, but - " + unsPort);
                }
            }
            else
            {
                Fail("No Unsolicited Destination Report received from Z/IP GW");
            }
            #endregion

            //ZipService.SetTestInclusionValues(new byte[] { 0x85, 0x20 });
            var addRes = ZipService.AddNodeWithDSK(ZipController, InclusionController, new byte[] { 0x85, 0x20 });
            AssertIsTrue(addRes);

            var includeRes = Service.IncludeNode(InclusionController, ControllerAsNode);
            AssertIsTrue(includeRes);
        }
    }
}