// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;

namespace Zats.Services
{
    /// <summary>
    /// Bits 0-3 contains different modes. 
    /// Bits 4-7 specify different additional options
    /// </summary>
    public enum Modes
    {
        None = ZWave.Enums.Modes.None,
        NodeAny = ZWave.Enums.Modes.NodeAny,
        NodeAnyS2 = ZWave.Enums.Modes.NodeReserved,
        NodeController = ZWave.Enums.Modes.NodeController,
        NodeEndDevice = ZWave.Enums.Modes.NodeEndDevice, 
        NodeExisting = ZWave.Enums.Modes.NodeExisting, 
        NodeStop = ZWave.Enums.Modes.NodeStop,
        NodeStopFailed = ZWave.Enums.Modes.NodeStopFailed,

        NodeOptionLongRange = ZWave.Enums.Modes.NodeOptionLongRange, // Flag
        NodeOptionNetworkWide = ZWave.Enums.Modes.NodeOptionNetworkWide, // Flag
        NodeOptionHighPower = ZWave.Enums.Modes.NodeOptionNormalPower, // Flag
    }
}
