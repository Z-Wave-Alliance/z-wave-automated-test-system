// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Enums
{
    /// <summary>
    /// Flash Programming Statuses enumeration.
    /// </summary>
    public enum FlashProgrammingStatuses
    {
        /// <summary>
        /// Done.
        /// </summary>
        DONE = '\r',
        /// <summary>
        /// Success.
        /// </summary>
        SUCCESS = '1',
        /// <summary>
        /// Fail.
        /// </summary>
        FAIL = '0'
    }
    /// <summary>
    /// Programming Statuses enumeration.
    /// </summary>
    public enum ProgrammingStatuses : byte
    {
        /// <summary>
        /// Flash sync error.
        /// </summary>
        FlashSyncErr = 0,
        /// <summary>
        /// Flash program pass.
        /// </summary>
        FlashProgPass = 1,
        /// <summary>
        /// Flash program error.
        /// </summary>
        FlashProgErr = 2,
        /// <summary>
        /// SRAM write error.
        /// </summary>
        SRAMWriteErr = 3,
        /// <summary>
        /// SRAM write pass.
        /// </summary>
        SRAMWritePass = 4,
        /// <summary>
        /// Error.
        /// </summary>
        Err = 5,
        /// <summary>
        /// Pass.
        /// </summary>
        Pass = 6,
    }
    /// <summary>
    /// Write Page Statuses enumeration.
    /// </summary>
    public enum WritePageStatuses : byte
    {
        /// <summary>
        /// Write operation failed.
        /// </summary>
        Failed = 0x30, 
        /// <summary>
        /// Write operation succeeded.
        /// </summary>
        Succeeded = 0x31 
    }
}
