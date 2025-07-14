// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Linq;
using Utils;
using ZWave.Enums;
using NUnit.Framework;

namespace ZnifferApplicationIntegrationTests
{
    public class ZnifferTestsBase
    {
        protected string COM1; // Controller
        protected string COM2; // Slave
        protected string COMZ; // Zniffer

        protected const int POW = 10;
        protected const TransmitOptions TXO = 
            TransmitOptions.TransmitOptionAcknowledge | 
            TransmitOptions.TransmitOptionAutoRoute | 
            TransmitOptions.TransmitOptionExplore;
        protected const int TIMEOUT = 10000;

        public ZnifferTestsBase()
        {
            var devicesConfig = ConfigLoader.SourcesProfile;
            var staticControllers = devicesConfig.SerialSources.Where(source => source is StaticControllerSource).ToArray();
            Assert.NotNull(staticControllers);
            Assert.GreaterOrEqual(staticControllers.Length, 1);
            var slaves = devicesConfig.SerialSources.Where(source => source is SlaveSource).ToArray();
            Assert.NotNull(slaves);
            Assert.GreaterOrEqual(slaves.Length, 1);
            COM1 = staticControllers[0].PortAlias;
            COM2 = slaves[0].PortAlias;
            Assert.NotNull(devicesConfig.ZnifferSource);
            COMZ = devicesConfig.ZnifferSource.PortAlias;
        }
    }
}
