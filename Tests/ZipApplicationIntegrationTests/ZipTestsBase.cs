// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ZWave.Enums;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Security;
using NUnit.Framework;
using Utils;
using ZWave.Devices;
using ZWave.BasicApplication.Devices;

namespace ZipApplicationIntegrationTests
{
    public class ZipTestsBase
    {
        protected string COM1; // Controller
        protected string COM2; // EndDevice
        protected string ZIP_ADDR; // Zip Gateway
        protected int ZIP_UDP_PORT;
        protected ushort ZIP_DTLS_PORT;
        protected string PSK;
        protected const TransmitOptions TXO =
            TransmitOptions.TransmitOptionAcknowledge |
            TransmitOptions.TransmitOptionAutoRoute |
            TransmitOptions.TransmitOptionExplore;

        private byte[][] _networkKeys = new byte[SecurityManagerInfo.NETWORK_KEYS_COUNT][];
        protected ReadOnlyCollection<byte[]> NETWORK_KEYS
        {
            get { return Array.AsReadOnly(_networkKeys); }
        }

        public ZipTestsBase()
        {
            var devicesConfig = ConfigLoader.SourcesProfile;
            var staticControllers = devicesConfig.SerialSources.Where(source => source is StaticControllerSource).ToArray();
            Assert.NotNull(staticControllers);
            Assert.GreaterOrEqual(staticControllers.Length, 1);
            var slaves = devicesConfig.SerialSources.Where(source => source is EndDeviceSource).ToArray();
            Assert.NotNull(slaves);
            Assert.GreaterOrEqual(slaves.Length, 1);
            COM1 = staticControllers[0].PortAlias;
            COM2 = slaves[0].PortAlias;
            Assert.NotNull(devicesConfig.ZipSource);
            ZIP_ADDR = devicesConfig.ZipSource.AddressIPv6;
            ZIP_DTLS_PORT = (ushort)devicesConfig.ZipSource.DtlsPort;
            PSK = devicesConfig.ZipSource.Psk;
            ZIP_UDP_PORT = devicesConfig.ZipSource.UdpPort;

            Assert.NotNull(devicesConfig.Security);
            Assert.NotNull(devicesConfig.Security.NetworkKeys);
            Assert.AreEqual(SecurityManagerInfo.NETWORK_KEYS_COUNT, devicesConfig.Security.NetworkKeys.Length);
            for (int i = 0; i < _networkKeys.Length; i++)
            {
                if (devicesConfig.Security.NetworkKeys[i] != null)
                {
                    _networkKeys[i] = devicesConfig.Security.NetworkKeys[i];
                }
            }
        }

        protected SecurityManager CreateSecurityManager(Device device)
        {
            device.Network.IsEnabledS0 = true;
            device.Network.IsEnabledS2_UNAUTHENTICATED = true;
            device.Network.IsEnabledS2_AUTHENTICATED = true;
            device.Network.IsEnabledS2_ACCESS = true;
            device.Network.ResetAndEnableAndSelfRestore();
            device.GetPRK();
            var ret = new SecurityManager(device.Network, null, device.PRK);
            ret.SecurityManagerInfo.SetNetworkKey(NETWORK_KEYS[7], ZWave.Enums.SecuritySchemes.S0, false);
            ret.SecurityManagerInfo.SetNetworkKey(NETWORK_KEYS[0], ZWave.Enums.SecuritySchemes.S2_UNAUTHENTICATED, false);
            ret.SecurityManagerInfo.SetNetworkKey(NETWORK_KEYS[1], ZWave.Enums.SecuritySchemes.S2_AUTHENTICATED, false);
            ret.SecurityManagerInfo.SetNetworkKey(NETWORK_KEYS[2], ZWave.Enums.SecuritySchemes.S2_ACCESS, false);
            ret.SecurityManagerInfo.ActivateNetworkKeyS0();
            ret.SecurityManagerInfo.CheckIfSupportSecurityCC = false;
            return ret;
        }
    }
}
