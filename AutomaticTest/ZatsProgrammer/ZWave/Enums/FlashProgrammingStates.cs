// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Enums
{
    /// <summary>
    /// Flash Programming States enumeration.
    /// </summary>
    public enum FlashProgrammingStates
    {
        /// <summary>
        /// Erase flash.
        /// </summary>
        FLASH_ERASE = 1,
        /// <summary>
        /// Program flash.
        /// </summary>
        FLASH_PROGRAM = 2,
        /// <summary>
        /// Program flash progress.
        /// </summary>
        FLASH_PROGRAM_PROGRESS = 3,
        /// <summary>
        /// Read flash progress.
        /// </summary>
        FLASH_READING_PROGRESS = 4,
        /// <summary>
        /// Verify flash.
        /// </summary>
        FLASH_VERIFY = 5,
        /// <summary>
        /// Read flash.
        /// </summary>
        FLASH_READING = 6,
        /// <summary>
        /// Verify flash succesful.
        /// </summary>
        FLASH_VERIFY_OK = 7,
        /// <summary>
        /// Verify flash failed.
        /// </summary>
        FLASH_VERIFY_FAIL = 8,
        /// <summary>
        /// Sync flash error.
        /// </summary>
        FLASH_SYNC_ERROR = 9,
        /// <summary>
        /// Read flash error
        /// </summary>
        FLASH_READING_ERROR = 10,
        /// <summary>
        /// Programming port open error.
        /// </summary>
        PROG_PORT_OPEN_ERROR = 11,
        /// <summary>
        /// 
        /// </summary>
        EEPROM_PROG_DL = 12,
        /// <summary>
        /// Program EEPROM.
        /// </summary>
        EEPROM_PROGRAM = 13,
        /// <summary>
        /// Program EEPROM progress.
        /// </summary>
        EEPROM_PROGRAM_PROGRESS = 14,
        /// <summary>
        /// Read EEPROM.
        /// </summary>
        EEPROM_READING = 15,
        /// <summary>
        /// Read EEPROM progress.
        /// </summary>
        EEPROM_READING_PROGRESS = 16,
        /// <summary>
        /// Write EEPROM empty data.
        /// </summary>
        EEPROM_ZEROFILL = 17,
        /// <summary>
        /// Write EEPROM error.
        /// </summary>
        EEPROM_WRITE_ERROR = 18,
        /// <summary>
        /// Programming communication error.
        /// </summary>
        PROG_COM_ERROR = 19
    }
}
