// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Enums
{
    /// <summary>
    /// Chip Types enumeration.
    /// </summary>
    public enum ChipTypes : byte
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        UNKNOWN = 0x00,
        /// <summary>
        /// ZW010x chipt type (100 series).
        /// </summary>
        ZW010x = 0x01,
        /// <summary>
        /// ZW020x chipt type (200 series).
        /// </summary>
        ZW020x = 0x02,
        /// <summary>
        /// ZW030x chipt type (300 series).
        /// </summary>
        ZW030x = 0x03,
        /// <summary>
        /// ZW050x chipt type (500 series).
        /// </summary>
        ZW050x = 0x05

    }
}
