// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZWave.Enums;

namespace ZWave.Devices
{
    public interface IProgrammableDevice
    {
        /// <summary>
        /// Gets or sets the type of the chip.
        /// </summary>
        /// <value>The type of the chip.</value>
        byte ChipType { get; set; }
        /// <summary>
        /// Gets the size of the SRAM.
        /// </summary>
        /// <value>The size of the sram.</value>
        int SramSize { get; }
        /// <summary>
        /// Gets the size of the flash.
        /// </summary>
        /// <value>The size of the flash.</value>
        int FlashSize { get; }
        /// <summary>
        /// Sets the programming mode.
        /// </summary>
        /// <param name="enable">if set to <c>true</c> [enable].</param>
        /// <returns></returns>
        bool SetProgrammingMode(bool enable);
        /// <summary>
        /// Reads the signature bits.
        /// </summary>
        void ReadSignatureBits();
        /// <summary>
        /// Turns on/off busy indicator
        /// </summary>
        /// <param name="state"></param>
        void SetBusyLedState(bool state);
        /// <summary>
        /// Reads the RF options.
        /// </summary>
        /// <returns></returns>
        IFlashSettings FlashReadRfOptions();
        /// <summary>
        /// Writes the RF options.
        /// </summary>
        /// <param name="flashSettings">The flash settings.</param>
        /// <returns></returns>
        WriteRfOptionsStatuses FlashWriteRfOptions(IFlashSettings flashSettings);
        /// <summary>
        /// Writes byte to the NVR.
        /// </summary>
        bool WriteNvrByte(byte index, byte data);
        /// <summary>
        /// Reads the NVR.
        /// </summary>
        /// <param name="partNo">The part of the NVR.</param>
        /// <returns>NVR data</returns>
        byte[] ReadNvr(byte partNo);
        bool ReadNvrByte(byte index, out byte data, byte partNo);
        /// <summary>
        /// Write Lock Bit.
        /// </summary>
        /// <returns></returns>
        bool WriteLockBit(int num, byte lockData);
        /// <summary>
        /// Read Lock Bit.
        /// </summary>
        /// <returns></returns>
        byte ReadLockBit(int num);
    }
}
