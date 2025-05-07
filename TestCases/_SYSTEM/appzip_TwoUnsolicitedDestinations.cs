// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Net;
using Zats.Services;

namespace ZatsTests
{
    [ZatsTestCase(@"To verify that we handle two unsolicited destinations")]
    [ZipDeviceMapping(1, "z2")]
    [DeviceMapping(2, LibTypes.EndDeviceLib)]
    public class appzip_TwoUnsolicitedDestinations : ZatsCodeTestCase
    {
        static Alias Slave = new Alias(2);

        protected override void ExecuteTest()
        {
            #region Aliases 
            Alias ZipController = new Alias(1);
            Alias ZipUnsolicitedListener = new Alias(3); // Zip Unsolicited Listener 1
            Alias ZipUnsolicitedListener2 = new Alias(4); // Zip Unsolicited Listener 2
            Alias ZipNodeUnsolicitedListener = new Alias(5);
            Alias ZipNodeUnsolicitedListener2 = new Alias(6);
            Alias[] ALL = new Alias[] { Slave };

            #endregion

            #region Reset

            ZipService.SetDefault(ZipController);
            ParallelService.Reset(ALL);

            #endregion

            #region Options

            #region Ip discovery

            //This will get the Ip addresses on oyur setup, I'm still missing a way to select the right one....so far I choose the number manually
            var host = Dns.GetHostEntry(Dns.GetHostName());

            //The IP address of the local PC's interface connected to Z/IP GW
            //byte[] unsIPv6addr = host.AddressList[6].GetAddressBytes(); //The IPv6 address of the current PC

            #endregion

            #region Configuring Unsolicited Destination for Gateway

            ushort zipPort = 41230;
            ushort zipPort2 = 41231;
            ZipService.AddSecondaryUnsolicitedListener(ZipController, zipPort2); 
            ZipService.RegisterUnsolicitedZipDevice(ZipUnsolicitedListener, new IPEndPoint(IPAddress.Parse("fd00:aaaa::3"), zipPort));
            ZipService.RegisterUnsolicitedZipDevice(ZipUnsolicitedListener2, new IPEndPoint(IPAddress.Parse("fd00:aaaa::3"), zipPort2));

            #endregion

            #endregion

            //Test Case Start

            ZipResult.AddNode ZipAddRes;
            var tokenUnsolicited1 = ZipAsyncService.ExpectData(ZipUnsolicitedListener, new COMMAND_CLASS_NETWORK_MANAGEMENT_PROXY_V2.NODE_LIST_REPORT(), 60000);
            var tokenUnsolicited2 = ZipAsyncService.ExpectData(ZipUnsolicitedListener2, new COMMAND_CLASS_NETWORK_MANAGEMENT_PROXY_V2.NODE_LIST_REPORT(), 60000);
            ZipAddRes = ZipService.AddNodeWithDSK(ZipController, Slave, new byte[] { 0xDE, 0x9E });
            var resUnsolicited1 = tokenUnsolicited1.WaitCompletedSignal();
            var resUnsolicited2 = tokenUnsolicited2.WaitCompletedSignal();
            Assert.IsTrue(resUnsolicited1);
            Assert.IsTrue(resUnsolicited2);
            
            //ZipService.AddSecondaryUnsolicitedListener(ZipController, zipPort2); 
            //ZipService.RegisterUnsolicitedZipDevice(ZipNodeUnsolicitedListener, new IPEndPoint(IPAddress.Parse("fd00:bbbb::6"), zipPort));
            //ZipService.RegisterUnsolicitedZipDevice(ZipNodeUnsolicitedListener2, new IPEndPoint(IPAddress.Parse("fd00:bbbb::6"), zipPort2));
            //tokenUnsolicited1 = ZipAsyncService.ExpectData(ZipNodeUnsolicitedListener, new COMMAND_CLASS_BASIC_V2.BASIC_SET(), 5000);
            //tokenUnsolicited2 = ZipAsyncService.ExpectData(ZipNodeUnsolicitedListener2, new COMMAND_CLASS_BASIC_V2.BASIC_SET(), 5000);
            //ComplianceService.SendData(Slave, 0x01, new COMMAND_CLASS_BASIC_V2.BASIC_SET());
            //resUnsolicited1 = tokenUnsolicited1.WaitCompletedSignal();
            //resUnsolicited2 = tokenUnsolicited2.WaitCompletedSignal();
            //Assert.IsTrue(resUnsolicited1);
            //Assert.IsTrue(resUnsolicited2);
        }
    }
}