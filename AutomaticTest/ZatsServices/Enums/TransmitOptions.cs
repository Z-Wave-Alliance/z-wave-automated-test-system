// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;

namespace Zats.Services
{
    /// <summary>
    /// Transmit Options enumeration.
    /// </summary>
    [Flags]
    public enum TransmitOptions
    {
        /// <summary>
        /// No Request.
        /// </summary>
        None = ZWave.Enums.TransmitOptions.TransmitOptionNone,
        /// <summary>
        /// Request acknowledge from destination node.
        /// </summary>  
        Ack = ZWave.Enums.TransmitOptions.TransmitOptionAcknowledge,
        /// <summary>
        /// Request retransmission via repeater nodes (at normal output power level).
        /// </summary>
        AutoRoute = ZWave.Enums.TransmitOptions.TransmitOptionAutoRoute,
        /// <summary>
        /// Request with low power option.
        /// </summary>
        LowPower = ZWave.Enums.TransmitOptions.TransmitOptionLowPower,
        /// <summary>
        /// Request with no route option.
        /// </summary>
        NoRoute = ZWave.Enums.TransmitOptions.TransmitOptionNoRoute,
        /// <summary>
        /// Request Explore Frame route resolution if all else fails
        /// </summary>
        Explore = ZWave.Enums.TransmitOptions.TransmitOptionExplore,
        /// <summary>
        /// Request without retransmission. Is used for ERTT 
        /// </summary>
        NoRetransmit = ZWave.Enums.TransmitOptions.TransmitOptionNoRetransmit
    }
}
