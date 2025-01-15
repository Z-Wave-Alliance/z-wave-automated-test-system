// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services
{
    /// <summary>
    /// Transmit Statuses enumeration.
    /// </summary>
    public enum TransmitStatuses
    {
        /// <summary>
        /// [0x00] Successfully
        /// </summary>
        CompleteOk = ZWave.Enums.TransmitStatuses.CompleteOk,
        /// <summary>
        /// [0x01] No acknowledge is received before timeout from the destination node. 
        /// Acknowledge is discarded in case it is received after the timeout.
        /// </summary>
        CompleteNoAcknowledge = ZWave.Enums.TransmitStatuses.CompleteNoAcknowledge,
        /// <summary>
        /// [0x02] Not possible to transmit data because the Z-Wave network is busy (jammed).
        /// </summary>
        CompleteFail = ZWave.Enums.TransmitStatuses.CompleteFail,
        /// <summary>
        /// [0x04] No route found in Assign Route.
        /// </summary>
        CompleteNoRoute = ZWave.Enums.TransmitStatuses.CompleteNoRoute,
        /// <summary>
        /// [0x05] No Communication ACK received.
        /// </summary>
        NoAcknowledge = ZWave.Enums.TransmitStatuses.NoAcknowledge,
        /// <summary>
        /// [0x06] No response received.
        /// </summary>
        ResMissing = ZWave.Enums.TransmitStatuses.ResMissing
    }
}
