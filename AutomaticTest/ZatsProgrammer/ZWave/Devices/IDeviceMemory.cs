// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;
using ZWave.Enums;

namespace ZWave.Devices
{
    /// <summary>
    /// This interface provides properties and methods that allow the client program using EEPROM Serial API.
    /// </summary>
    public interface IDeviceMemory
    {
        /// <summary>
        /// Occurs when progress changed.
        /// </summary>
        event Events.ProgressChangedEventHandler ProgressChanged;
        /// <summary>
        /// Gets or sets the <see cref="IDevice" />.
        /// </summary>
        /// <value>The device.</value>
        IDevice Device
        {
            get;
            set;
        }
        /// <summary>
        /// Gets or sets the buffer.
        /// </summary>
        /// <value>The buffer.</value>
        byte[] Buffer { get; set; }
        /// <summary>
        /// Gets the size of the EEPROM.
        /// </summary>
        /// <value>The size of the eeprom.</value>
        int EepromSize
        {
            get;
            set;
        }
        /// <summary>
        /// Switches the EEPROM.
        /// </summary>
        /// <param name="state">if set to <c>true</c> State.</param>
        /// <returns>External NVM chip type</returns>
        NVMTypes SwitchEEPROM(bool state, int defaultMemorySize);
        /// <summary>
        /// Clears the EEPROM.
        /// </summary>
        /// <param name="eepromDataRaw">The EEPROM empty data raw.</param>
        void ClearEEPROM(byte[] eepromDataRaw);
        /// <summary>
        /// Reads the EEPROM.
        /// </summary>
        void ReadEEPROM();
        /// <summary>
        /// Writes the EEPROM.
        /// </summary>
        /// <param name="startAddress">The start address.</param>
        /// <param name="eepromDataRaw">The eeprom data raw.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        bool WriteEEPROM(int startAddress, byte[] eepromDataRaw, ushort length);
        /// <summary>
        /// Writes the EEPROM.
        /// </summary>
        /// <param name="eepromDataRaw">The eeprom data raw.</param>
        /// <returns></returns>
        bool WriteEEPROM(byte[] eepromDataRaw);
        /// <summary>
        /// Reads the home id.
        /// </summary>
        /// <returns></returns>
        byte[] ReadHomeId();
        /// <summary>
        /// Writes the home id.
        /// </summary>
        /// <param name="homeId">The home id.</param>
        /// <returns></returns>
        bool WriteHomeId(byte[] homeId);
        /// <summary>
        /// Compares the EEPROM.
        /// </summary>
        /// <param name="eepromEmptyDataRaw">The eeprom empty data raw.</param>
        /// <returns></returns>
        bool Compare(byte[] eepromEmptyDataRaw);
        /// <summary>
        /// Compares the EEPROM and outs differences.
        /// </summary>
        /// <param name="flashDataRaw">The eeprom raw from file.</param>
        /// <param name="diffs">Out param for differences.</param>
        /// <returns></returns>
        bool Compare(byte[] flashDataRaw, out SortedList<int, List<byte[]>> diffs);
        /// <summary>
        /// Writes the SRAM.
        /// </summary>
        /// <param name="sramDataRaw">The sram data raw.</param>
        /// <param name="verify">if set to <c>true</c> [verify].</param>
        /// <returns></returns>
        bool WriteSRAM(byte[] sramDataRaw, bool verify);
        /// <summary>
        /// Reads the SRAM.
        /// </summary>
        /// <returns></returns>
        bool ReadSRAM();
        /// <summary>
        /// Reads the SRAM page.
        /// </summary>
        /// <param name="pageIndex">The page Index.</param>
        /// <returns>The page buffer </returns>
        byte[] ReadSRAMPage(int pageIndex);
        /// <summary>
        /// Writes the SRAM RF options.
        /// </summary>
        /// <param name="rfSettings">The RF settings.</param>
        /// <returns></returns>
        WriteRfOptionsStatuses WriteSRAMRfOptions(IFlashSettings rfSettings);
        /// <summary>
        /// Reads the SRAM RF options.
        /// </summary>
        /// <returns>The RF settings.</returns>
        IFlashSettings ReadSRAMRfOptions();
        /// <summary>
        /// Gets the size of the MTP.
        /// </summary>
        /// <value>The size of the MTP Memory.</value>
        int MtpSize
        {
            get;
        }
        /// <summary>
        /// Switches the MTP Interface.
        /// </summary>
        /// <param name="state">If set to <c>true</c> - activate MTP Interface. If set to <c>false</c> - deactivate MTP Interface.</param>
        /// <returns><c>true</c> if operation completed successfuly</returns>
        bool SwitchMTP(bool state);
        /// <summary>
        /// Fill the entire MTP Memory with specified value (Clear MTP).
        /// </summary>
        /// <param name="fillValue">The value, which will be written to each byte of the MTP Memory.</param>
        /// <returns><c>true</c> if operation completed successfuly</returns>
        bool FillMTP(byte fillValue);
        /// <summary>
        /// Reads the MTP.
        /// </summary>
        /// <returns><c>true</c> if operation completed successfuly</returns>
        bool ReadMTP();
        /// <summary>
        /// Writes the MTP.
        /// </summary>
        /// <param name="mtpDataRaw">The MTP Memory data raw.</param>
        /// <param name="verify">if set to <c>true</c> [verify].</param>
        /// <returns><c>true</c> if operation completed successfuly</returns>
        bool WriteMTP(byte[] mtpDataRaw, bool verify);
        byte[] ReadEEPROM(int index, int length);
    }
}
