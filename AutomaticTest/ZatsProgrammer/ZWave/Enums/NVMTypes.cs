// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Enums
{
    /// <summary>
    /// External Non-Volatile Memory (NVM) chip types enumeration.
    /// </summary>
    public enum NVMTypes : uint
    {
        /// <summary>
        /// Bus and protocol type mask.
        /// </summary>
        NVMTypeMask = 0xFF000000,
        /// <summary>
        /// Bus and protocol type bits position.
        /// </summary>
        NVMTypePos = 24,
        /// <summary>
        /// Chip Manufacturer ID mask. 0x00 - unknown manufacturer id.
        /// </summary>
        NVMManufacturerMask = 0x00FF0000,
        /// <summary>
        /// Chip Manufacturer ID bits position.
        /// </summary>
        NVMManufacturerPos = 16,
        /// <summary>
        /// Chip Device ID mask. 0x00 - unknown device id.
        /// </summary>
        NVMDeviceMask = 0x0000FF00,
        /// <summary>
        /// Chip Device ID bits position.
        /// </summary>
        NVMDevicePos = 8,
        /// <summary>
        /// Chip Memory size mask. Memory size = num * NVMSizeUnit KBytes. 0x00 - unknown size.
        /// </summary>
        NVMSizeMask = 0x000000FF,
        /// <summary>
        /// Chip Memory size bits position.
        /// </summary>
        NVMSizePos = 0,
        /// <summary>
        /// Chip Memory size unit in kilo bytes (KB).
        /// </summary>
        NVMSizeUnit = 8,

        /// <summary>
        /// Invalid, cant detect, error.
        /// </summary>
        NVMInvalid = 0x00000000,
        /// <summary>
        /// No errors, but chip type is unknown at this stage.
        /// </summary>
        NVMUnknown = 0x00010000,
        /// <summary>
        /// Serial EEPROM chip with SPI bus protocol like in Atmel AT25128 serial EEPROM and compatible (ST95128, etc.).
        /// </summary>
        NVMSerialEEPROM = 0x01000000,
        /// <summary>
        /// Serial Flash chip on SPI bus.
        /// </summary>
        NVMSerialFlash = 0x02000000,
        /// <summary>
        /// M25PE10VP (Micron)                 0x02208010   serial FLASH 128 KB  (1-MBIT)
        /// </summary>
        NVMMicronM25PE10VP = 0x02208010,
        /// <summary>
        /// M25PE20VP (Micron)                 0x02208020   serial FLASH 256 KB  (2-MBIT)
        /// </summary>
        NVMMicronM25PE20VP = 0x02208020,
        /// <summary>
        /// M25PE40VP (Numonyx)              0x02208040   serial FLASH 512 KB  (4-MBIT)
        /// </summary>
        NVMNumonyxM25PE40VP = 0x02208040,
        ///// <summary>
        ///// AT25128A (Atmel)                0x01FFFFFF   EEPROM 16 KB
        ///// </summary>
        //NVMAtmelAT25128A = 0x01FFFFFF,
        /// <summary>
        /// S25CM01A  (SeikoInstruments)  0x01FFFFFF   EEPROM 128 KB (1-MBIT) 
        /// </summary>
        NVMSeikoInstrumentsS25CM01A = 0x01FFFFFF,
        /// <summary>
        /// 25PE80VP  (STMicro)                 0x02208014   serial FLASH 1 MB    (8-MBIT)
        /// </summary>
        NVMSTMicro25PE80VP = 0x02208014,
        /// <summary>
        /// 25PE16VP  (STMicro)                 0x02208015   serial FLASH 2 MB    (16-MBIT) 
        /// </summary>
        NVMSTMicro25PE16VP = 0x02208015,
        /// <summary>
        /// AT25XE021A (Adestotech)        0x021F8212   serial FLASH 256 KB  (2-MBIT)
        /// </summary>
        NVMAdestotechAT25XE021A = 0x021F8212,
        /// <summary>
        /// AT25XE041B (Adestotech)       0x021F8213   serial FLASH 512 KB  (4-MBIT)
        /// </summary>
        NVMAdestotechAT25XE041B = 0x021F8213,
        /// <summary>
        /// AT45DB021E (Adestotech)        0x021F8112   serial FLASH 256 KB  (2-MBIT)
        /// </summary>
        NVMAdestotechAT45DB021E = 0x021F8112,
        /// <summary>
        /// AT45DB041E (Adestotech)       0x021F8113   serial FLASH 512 KB  (4-MBIT)
        /// </summary>
        NVMAdestotechAT45DB041E = 0x021F8113,
        /// <summary>
        /// AT45DB081E (Adestotech)       0x021F8114   serial FLASH 1 MB    (8-MBIT)
        /// </summary>
        NVMAdestotechAT45DB081E = 0x021F8114,
        /// <summary>
        /// AT45DB161E (Adestotech)       0x021F8115   serial FLASH 2 MB    (16-MBIT)
        /// </summary>
        NVMAdestotechAT45DB161E = 0x021F8115,
        /// <summary>
        /// AT45DB321E (Adestotech)       0x021F8116   serial FLASH 4 MB    (32-MBIT)
        /// </summary>
        NVMAdestotechAT45DB321E = 0x021F8116,
        /// <summary>
        /// AT45DB641E (Adestotech)       0x021F8117   serial FLASH 8 MB    (64-MBIT)
        /// </summary>
        NVMAdestotechAT45DB641E = 0x021F8117,
        /// <summary>
        /// SST25PF020B (Microchip)       0x02BF258C   serial FLASH 256 KB  (2-MBIT)
        /// </summary>
        NVMMicrochipSST25PF020B = 0x02BF258C,
        /// <summary>
        /// GD25Q21 (GigaDevices)      0x02C84012   serial FLASH 256 KB   (2-MBIT)
        /// </summary>
        NVMGigaDevicesGD25Q21 = 0x02C84012,
        /// <summary>
        /// GD25Q41 (GigaDevices)      0x02C84013   serial FLASH 512 KB   (4-MBIT)
        /// </summary>
        NVMGigaDevicesGD25Q41 = 0x02C84013,
        /// <summary>
        /// GD25Q80 (GigaDevices)      0x02C84014   serial FLASH 1 MB   (8-MBIT)
        /// </summary>
        NVMGigaDevicesGD25Q80 = 0x02C84014,
        /// <summary>
        /// GD25Q16 (GigaDevices)      0x02C84015   serial FLASH 2 MB   (16-MBIT)
        /// </summary>
        NVMGigaDevicesGD25Q16 = 0x02C84015,
        /// <summary>
        /// GD25Q32 (GigaDevices)      0x02C84016   serial FLASH 4 MB   (32-MBIT)
        /// </summary>
        NVMGigaDevicesGD25Q32 = 0x02C84016,
        /// <summary>
        /// GD25Q64 (GigaDevices)      0x02C84017   serial FLASH 8 MB   (64-MBIT)
        /// </summary>
        NVMGigaDevicesGD25Q64 = 0x02C84017,
        /// <summary>
        /// GD25Q128 (GigaDevices)      0x02C84018   serial FLASH 16 MB   (128-MBIT)
        /// </summary>
        NVMGigaDevicesGD25Q128 = 0x02C84018,
    }
}
