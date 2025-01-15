// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Enums
{
    /// <summary>
    /// Working Modes enumeration. Used only for 400 series.
    /// </summary>
    public enum WorkingModes
    {
        /// <summary>
        /// None.
        /// </summary>
        None,
        /// <summary>
        /// Normal mode.
        /// </summary>
        Normal,
        /// <summary>
        /// Development mode.
        /// </summary>
        Development,
        /// <summary>
        /// Execute Out Of SRAM mode.
        /// </summary>
        ExecuteOutOfSram,
        /// <summary>
        /// Programming mode.
        /// </summary>
        Programming
    }
}
