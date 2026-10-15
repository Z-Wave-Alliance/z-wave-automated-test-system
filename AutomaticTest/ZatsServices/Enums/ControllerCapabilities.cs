// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
namespace Zats.Services
{
    /// <summary>
    /// Controller Capabilities enumeration
    /// </summary>
    public enum ControllerCapabilities : byte
    {
        /// <summary>
        /// Return value mask for ZWaveGetControllerCapabilities
        /// Controller is secondary on current Z-Wave network
        /// </summary>
        IS_SECONDARY = ZWave.Enums.ControllerCapabilities.IS_SECONDARY,
        /// <summary>
        /// Return value mask for ZWaveGetControllerCapabilities
        /// </summary>
        ON_OTHER_NETWORK = ZWave.Enums.ControllerCapabilities.ON_OTHER_NETWORK,
        /// <summary>
        /// Return value mask for ZWaveGetControllerCapabilities
        /// Controller is a member of a Z-Wave network with a NodeID Server present
        /// </summary>
        NODEID_SERVER_PRESENT = ZWave.Enums.ControllerCapabilities.NODEID_SERVER_PRESENT,
        /// <summary>
        /// Return value mask for ZWaveGetControllerCapabilities
        /// Controller is the original owner of the current Z-Wave network HomeID
        /// </summary>
        IS_REAL_PRIMARY = ZWave.Enums.ControllerCapabilities.IS_REAL_PRIMARY,
        /// <summary>
        /// Return value mask for ZWaveGetControllerCapabilities
        /// Controller is the SUC in current Z-WAve network
        /// </summary>
        IS_SUC = ZWave.Enums.ControllerCapabilities.IS_SUC
    }
}