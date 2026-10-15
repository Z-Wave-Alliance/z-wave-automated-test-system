// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Enums
{
    /// <summary>
    /// Programmer Command Types enumeration.
    /// </summary>
    public enum ProgrammerCommandTypes : byte
    {
        /// <summary>
        /// Commant to write a specific value to block of EEPROM address.
        /// </summary>
        FUNC_ID_BLOCK_SET_EEP = 0x05,
        /// <summary>
        /// Command to get the firmware�s version number.
        /// </summary>
        FUNC_ID_ZW_GET_VERSION = 0x06,
        /// <summary>
        /// Command to read Home Id from the EEPROM.
        /// </summary>
        FUNC_ID_MEMORY_GET_ID = 0x07,
        /// <summary>
        /// Command to read a byte from the EEPROM.
        /// </summary>
        FUNC_ID_MEMORY_GET_BYTE = 0x08,
        /// <summary>
        /// Command to write a byte from the EEPROM.
        /// </summary>
        FUNC_ID_MEMORY_PUT_BYTE = 0x09,
        /// <summary>
        /// Command to read data bytes from the EEPROM.
        /// </summary>
        FUNC_ID_MEMORY_GET_BUFFER = 0x10,
        /// <summary>
        /// Command to write data bytes from the EEPROM.
        /// </summary>
        FUNC_ID_MEMORY_PUT_BUFFER = 0x11,
        /// <summary>
        /// Command to write Home Id to the EEPROM.
        /// </summary>
        FUNC_ID_STORE_HOMEID = 0x13,
        /// <summary>
        /// Command to erase the Atmel Flash (from 0x00000 to 0x1EFFF).
        /// </summary>
        FUNC_ID_M128_CHIP_ERASE = 0x16,
        /// <summary>
        /// (Not implemented).
        /// </summary>
        FUNC_ID_M128_GET_BLOCK_SIZE = 0x17,
        /// <summary>
        /// Command to write block of data bytes to the Atmel Flash.
        /// </summary>
        FUNC_ID_M128_BLOCK_WRITE = 0x18,
        /// <summary>
        /// Command to read block of data bytes to the Atmel Flash.
        /// </summary>
        FUNC_ID_M128_BLOCK_READ = 0x19,
        /// <summary>
        /// (Not implemented).
        /// </summary>
        FUNC_ID_M128_GET_LOCKBITS = 0x20,
        /// <summary>
        /// (Not implemented).
        /// </summary>
        FUNC_ID_M128_GET_FUSEBITS_LOW = 0x21,
        /// <summary>
        /// (Not implemented).
        /// </summary>
        FUNC_ID_M128_GET_FUSEBITS_HIGH = 0x22,
        /// <summary>
        /// (Not implemented).
        /// </summary>
        FUNC_ID_M128_GET_FUSEBITS_EXT = 0x23,
        /// <summary>
        /// Command to exit from the boot loader mode.
        /// </summary>
        FUNC_ID_EXIT_PROG_MODE = 0x24,
        /// <summary>
        /// Command to enter to the programmer mode (For now it is used only to detect the presence of the HW).
        /// </summary>
        FUNC_ID_CONNECT_PROGRAMMER = 0x25,
        /// <summary>
        /// Command to get the boot loader SE version <see cref="FUNC_ID_ZW_GET_VERSION" />.
        /// </summary>
        FUNC_ID_M128_GET_SW_VER = 0x26,
        /// <summary>
        /// Command to enter to the boot loader mode.
        /// </summary>
        FUNC_ID_M128_ENTER_PROG_MODE = 0x27,
        /// <summary>
        /// Command to enable the Flash programming interface.
        /// </summary>
        FUNC_ID_ZW0x0x_PROG_ENABLE = 0x40,
        /// <summary>
        /// Command to erase the ZW0x0x chip.
        /// </summary>
        FUNC_ID_ZW0x0x_CHIP_ERASE = 0x41,
        /// <summary>
        /// Command to read a ZW0x0 Flash page.
        /// </summary>
        FUNC_ID_ZW0x0x_READ_PAGE = 0x42,
        /// <summary>
        /// Command to write a ZW0x0 Flash page.
        /// </summary>
        FUNC_ID_ZW0x0x_WRITE_PAGE = 0x43,
        /// <summary>
        /// Command to read the ZW0x0x Flash lock bits setting.
        /// </summary>
        FUNC_ID_ZW0x0x_READ_LOCKBITS = 0x44,
        /// <summary>
        /// Command to write the ZW0x0x Flash lock bits setting.
        /// </summary>
        FUNC_ID_ZW0x0x_WRITE_LOCKBITS = 0x45,
        /// <summary>
        /// Command to set the Flash write cycle.
        /// </summary>
        FUNC_ID_ZW0x0x_SET_WRITE_CYCLE = 0x46,
        /// <summary>
        /// Command to read ZW0x0x Flash signature byte.
        /// </summary>
        FUNC_ID_ZW0x0x_READ_SIG_BYTE = 0x47,
        /// <summary>
        /// Command to release the ZW0x0x Flash programming interface (ZW0x0x will run in normal mode).
        /// </summary>
        FUNC_ID_ZW0x0x_PROG_RELEASE = 0x48,
        /// <summary>
        /// Command to write the Z-Wave RF setting to the RF option Flash page.
        /// </summary>
        FUNC_ID_ZW0x0x_SET_RF_OPT = 0x49,
        /// <summary>
        /// Command to read the Z-Wave RF setting to the RF option Flash page.
        /// </summary>
        FUNC_ID_ZW0x0x_GET_RF_OPT = 0x50,
        /// <summary>
        /// Command to enable/disable the interface used to Write/Read from the EEPROM.
        /// </summary>
        FUNC_ID_TOGGLE_EEPROM_IF = 0x51,
        /// <summary>
        /// Command to erase a ZW0x0x Flash page
        /// </summary>
        FUNC_ID_ZW0x0x_ERASE_PAGE = 0x52,
        /// <summary>
        /// Command to turn on/off the programmer status Leds.
        /// </summary>
        FUNC_ID_PROGRAMMER_SET_LED = 0x53,
        /// <summary>
        /// Command to write the ZW0x0x Flash lock bits setting.
        /// </summary>
        FUNC_ID_ZW5XX_WRITE_LOCKBIT = 0x54,
        /// <summary>
        /// Command to read the ZW0x0x Flash lock bits setting.
        /// </summary>
        FUNC_ID_ZW5XX_READ_LOCKBIT = 0x55,
        ///////// <summary>
        ///////// Command to write data to the Flash page write buffer.
        ///////// </summary>
        //////FUNC_ID_ZW0x0x_LOAD_FLASH_PAGE = 0x54,
        ///////// <summary>
        ///////// Command to start the Flash page writing process.
        ///////// </summary>
        //////FUNC_ID_ZW0x0x_START_FLASH_WRITE = 0x55,
        /// <summary>
        /// Command to read a ZW040x SRAM page.
        /// </summary>
        FUNC_ID_ZW0x0x_SRAM_READ_PAGE = 0x60,
        /// <summary>
        /// Command to write a ZW040x SRAM page.
        /// </summary>
        FUNC_ID_ZW0x0x_SRAM_WRITE_PAGE = 0x61,
        /// <summary>
        /// Command to enable execute out of SRAM mode on the ZW040x chip.
        /// </summary>
        FUNC_ID_ZW0x0x_SRAM_EXECUTE = 0x62,
        /// <summary>
        /// Command to set modem bit on the ZW040x chip.
        /// </summary>
        FUNC_ID_ZW0x0x_MODEM_BIT_WRITE = 0x63,
        /// <summary>
        /// Command to read status from the ZW040x chip.
        /// </summary>
        FUNC_ID_ZW0x0x_STATUS_READ = 0x64,
        /// <summary>
        /// Command to enable Development mode on the ZW040x chip.
        /// </summary>
        FUNC_ID_ZW0x0x_DEV_MODE_ENABLE = 0x65,
        /// <summary>
        /// Command to check CRC on the ZW040x chip.
        /// </summary>
        FUNC_ID_ZW0x0x_CRC_CHECK_RUN = 0x66,
        /// <summary>
        /// Command to check APM mode on the ZW050x chip.
        /// </summary>
        FUNC_ID_ZW050x_GET_PROG_MODE = 0xFF - 4,
        /// <summary>
        /// Command to check state on the ZW050x chip.
        /// </summary>
        FUNC_ID_ZW0x0x_CHECK_STATE = 0xFF - 3,
        /// <summary>
        /// Command to set byte in NVR space on the ZW050x chip.
        /// </summary>
        FUNC_ID_ZW050x_SET_NVR = 0xFF - 2,
        /// <summary>
        /// Command to read byte from NVR space on the ZW050x chip.
        /// </summary>
        FUNC_ID_ZW050x_READ_NVR = 0xFF - 1,
        /// <summary>
        /// Reset 500 series chip.
        /// </summary>
        FUNC_ID_ZW050x_RESET = 0xFF,
        /// <summary>
        /// Push button was pressed on Programmer PCB.
        /// </summary>
        FUNC_ID_BUTTON_PRESSED = 0x67,
        /// <summary>
        /// MTP Interface toggle.
        /// </summary>
        FUNC_ID_TOGGLE_MTP_IF = 0x68,
        /// <summary>
        /// MTP fill by value.
        /// </summary>
        FUNC_ID_MTP_FILL = 0x69,
        /// <summary>
        /// MTP page read.
        /// </summary>
        FUNC_ID_MTP_READ_PAGE = 0x6A,
        /// <summary>
        /// MTP page write.
        /// </summary>
        FUNC_ID_MTP_WRITE_PAGE = 0x6B,
        /// <summary>
        /// Command to read write OTP stats from the ZW040x chip.
        /// </summary>
        FUNC_ID_ZW0x0x_WRITE_OTP_STATS_READ = 0x6C,
        /// <summary>
        /// Command to start calibration the ZW040x chip.
        /// </summary>
        FUNC_ID_CALIBRATION_START = 0x71,
        /// <summary>
        /// Command to stop calibration the ZW040x chip.
        /// </summary>
        FUNC_ID_CALIBRATION_STOP = 0x72,

    }
}
