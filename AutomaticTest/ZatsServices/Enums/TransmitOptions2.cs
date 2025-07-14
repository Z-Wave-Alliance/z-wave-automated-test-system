// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;

namespace Zats.Services
{
    /// <summary>
    /// Transmit Options enumeration.
    /// </summary>
    [Flags]
    public enum TransmitOptions2
    {
        /// <summary>
        /// The frame should be sent in one long frame (but only supported at 100K speed, not 40/9.6K).
        /// </summary>
        NONE = ZWave.Enums.TransmitOptions2.NONE,
        /// <summary>
        /// The frame should be Transport Service Encapsulated
        /// </summary>  
        TRANSMIT_OPTION_2_TRANSPORT_SERVICE = ZWave.Enums.TransmitOptions2.TRANSMIT_OPTION_2_TRANSPORT_SERVICE,
    }
}
