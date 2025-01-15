// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Enums
{
    /// <summary>
    /// Led States enumeration.
    /// </summary>
    public enum LedStates : byte
    {
        /// <summary>
        /// ON state.
        /// </summary>
        ON = 0xff,
        /// <summary>
        /// OFF state.
        /// </summary>
        OFF = 0x00
    }
}
