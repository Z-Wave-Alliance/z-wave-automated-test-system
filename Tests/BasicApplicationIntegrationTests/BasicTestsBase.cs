// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Linq;
using System.Collections.ObjectModel;
using Utils;
using ZWave.Enums;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Security;
using NUnit.Framework;

namespace BasicApplicationIntegrationTests
{
    public class BasicTestsBase
    {
        protected string COM_CTRL_BRIDGE_1;
        protected string COM_CTRL_STATIC_1;
        protected string COM_CTRL_STATIC_2;
        protected string COM_CTRL_STATIC_3;
        protected string COM_SLAVE_1;
        protected string COM_SLAVE_2;
        protected string COM_SLAVE_3;

        private byte[] SLAVE_DSK;
        private byte[] CONTROLLER_DSK = new byte[] { 0x85, 0x20 };

        protected const int INCLUSION_TIMEOUT = 20000;

        protected const TransmitOptions TXO =
            TransmitOptions.TransmitOptionAcknowledge |
            TransmitOptions.TransmitOptionAutoRoute |
            TransmitOptions.TransmitOptionExplore;

        private byte[][] _networkKeys = new byte[SecurityManagerInfo.NETWORK_KEYS_COUNT][];
        protected ReadOnlyCollection<byte[]> NETWORK_KEYS
        {
            get { return Array.AsReadOnly(_networkKeys); }
        }

        public BasicTestsBase()
        {
            var devicesConfig = ConfigLoader.SourcesProfile;
            var staticControllers = devicesConfig.SerialSources.Where(source => source is StaticControllerSource).ToArray();
            Assert.NotNull(staticControllers);
            Assert.GreaterOrEqual(staticControllers.Length, 3);
            var slaves = devicesConfig.SerialSources.Where(source => source is EndDeviceSource).ToArray();
            Assert.NotNull(slaves);
            Assert.GreaterOrEqual(slaves.Length, 3);
            var bridges = devicesConfig.SerialSources.Where(source => source is BridgeSource).ToArray();
            Assert.NotNull(bridges);
            Assert.GreaterOrEqual(bridges.Length, 1);
            COM_CTRL_BRIDGE_1 = bridges[0].PortAlias;
            COM_CTRL_STATIC_1 = staticControllers[0].PortAlias;
            COM_CTRL_STATIC_2 = staticControllers[1].PortAlias;
            COM_CTRL_STATIC_3 = staticControllers[2].PortAlias;
            COM_SLAVE_1 = slaves[0].PortAlias;
            COM_SLAVE_2 = slaves[1].PortAlias;
            COM_SLAVE_3 = slaves[2].PortAlias;

            Assert.NotNull(devicesConfig.Security);
            Assert.NotNull(devicesConfig.Security.Dsk);
            SLAVE_DSK = new byte[devicesConfig.Security.Dsk.Length];
            Array.Copy(devicesConfig.Security.Dsk, SLAVE_DSK, SLAVE_DSK.Length);

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

        protected void SetupSecurity(Device device, SecurityUnderTest SecurityUnderTest, bool isEnableDSK)
        {
            switch (SecurityUnderTest)
            {
                case SecurityUnderTest.S2:
                    SetupSecurityS2Only(device, isEnableDSK);
                    break;
                case SecurityUnderTest.S0:
                    SetupSecurityS0Only(device, isEnableDSK);
                    break;
                case SecurityUnderTest.None:
                default:
                    break;
            }
        }

        protected void SetupSecurity(Device device, bool enableDsk)
        {
            device.MemoryGetId();
            device.GetPRK();
            device.Network.IsEnabledS0 = true;
            device.Network.IsEnabledS2_UNAUTHENTICATED = true;
            device.Network.IsEnabledS2_AUTHENTICATED = true;
            device.Network.IsEnabledS2_ACCESS = true;
            device.Network.ResetAndEnableAndSelfRestore();

            var securityManager = new SecurityManager(device.Network, null, device.PRK);
            byte[] dskToEnter = CONTROLLER_DSK;
            if (enableDsk)
            {
                dskToEnter = SLAVE_DSK;   
            }

            securityManager.SecurityManagerInfo.DSKNeededCallback = (nodeId, dsk) =>
            {
                return dskToEnter;
            };

            securityManager.SecurityManagerInfo.SetNetworkKey(NETWORK_KEYS[7], ZWave.Enums.SecuritySchemes.S0, false);
            securityManager.SecurityManagerInfo.SetNetworkKey(NETWORK_KEYS[0], ZWave.Enums.SecuritySchemes.S2_UNAUTHENTICATED, false);
            securityManager.SecurityManagerInfo.SetNetworkKey(NETWORK_KEYS[1], ZWave.Enums.SecuritySchemes.S2_AUTHENTICATED, false);
            securityManager.SecurityManagerInfo.SetNetworkKey(NETWORK_KEYS[2], ZWave.Enums.SecuritySchemes.S2_ACCESS, false);
            securityManager.SecurityManagerInfo.ActivateNetworkKeyS0();
            securityManager.SecurityManagerInfo.CheckIfSupportSecurityCC = false;
            device.SessionClient.AddSubstituteManager(securityManager,
                securityManager.CreateSecurityReportTask(),
                securityManager.CreateSecurityS2ReportTask());
            device.Network.SetCommandClassesSecureVirtual(new byte[] { 0x20 });
            securityManager.SecurityManagerInfo.SetTestSecretKeyS2(new byte[] { 
                    0x77, 0x07, 0x6d, 0x0a, 0x73, 0x18, 0xa5, 0x7d, 
                    0x3c, 0x16, 0xc1, 0x72, 0x51, 0xb2, 0x66, 0x45, 
                    0xdf, 0x4c, 0x2f, 0x87, 0xeb, 0xc0, 0x99, 0x2a, 
                    0xb1, 0x77, 0xfb, 0xa5, 0x1d, 0xb9, 0x2c, 0x2a });
        }

        protected void SetupSecurityS0Only(Device device, bool enableDsk)
        {
            SetupSecurity(device, enableDsk);
            device.Network.IsEnabledS2_UNAUTHENTICATED = false;
            device.Network.IsEnabledS2_AUTHENTICATED = false;
            device.Network.IsEnabledS2_ACCESS = false;
            device.Network.ResetAndEnableAndSelfRestore();
        }

        protected void SetupSecurityS2Only(Device device, bool enableDsk)
        {
            SetupSecurity(device, enableDsk);
            device.Network.IsEnabledS0 = false;
            device.Network.ResetAndEnableAndSelfRestore();
        }
    }

    public enum SecurityUnderTest
    {
        None = 0,
        S0 = 1,
        S2 = 2
    }
}