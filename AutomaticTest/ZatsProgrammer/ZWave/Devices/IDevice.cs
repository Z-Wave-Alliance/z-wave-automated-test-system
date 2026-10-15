// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;
using ZWave.Enums;

namespace ZWave.Devices
{
    /// <summary>
    /// This interface provides methods that allow the client program using Z-Wave Device common Serial API.
    /// </summary>
    public interface IDevice : IProgrammableDevice, IDisposable
    {
        /// <summary>
        /// Occurs when connection status changed.
        /// </summary>
        event Events.StatusChangedEventHandler ConnectionStatusChanged;
        /// <summary>
        /// Occurs when ApplicationCommandHandler command received.
        /// </summary>
        event Events.DeviceAppCommandHandlerEventHandler ApplicationCommandHandlerEvent;
        /// <summary>
        /// Gets or sets the Serial Port name.
        /// </summary>
        /// <value>The serial port.</value>
        string SerialPort { get; set; }
        /// <summary>
        /// Gets or sets the <see cref="IDeviceMemory"></see>.
        /// </summary>
        /// <value>The memory.</value>
        IDeviceMemory Memory { get; set; }
        /// <summary>
        /// Gets or sets the <see cref="IDeviceFlash"/>.
        /// </summary>
        /// <value>The flash.</value>
        IDeviceFlash Flash { get; set; }
        /// <summary>
        /// Gets or sets the connection status.
        /// </summary>
        /// <value>The connection status.</value>
        ConnectionStatuses ConnectionStatus { get; set; }
        /// <summary>
        /// Resets this node.
        /// </summary>
        void Reset();
        /// <summary>
        /// Erases the chip.
        /// </summary>
        /// <param name="isAsic">if set to <c>true</c> [is asic].</param>
        void EraseChip(bool isAsic);
        /// <summary>
        /// Reads the lock bits.
        /// </summary>
        /// <returns>Returns the ZW0x0x Lock Bits.</returns>
        byte ReadLockBits();
        /// <summary>
        /// Writes the lock bits.
        /// </summary>
        /// <returns>Readed ZW0x0x Lock Bits</returns>
        bool WriteLockBits(byte lockBits);
        /// <summary>
        /// Sets the default write cycle time.
        /// </summary>
        /// <returns>Returns <c>true</c>, if write thime was set succesfully, else <c>false</c>.</returns>
        bool SetWriteCycleTime();
        /// <summary>
        /// Sets the write cycle time.
        /// </summary>
        /// <param name="xtalFrequencyMHz">X-Tal Frequency of th Z-Wave mudule in MHz</param>
        /// <returns>Returns <c>true</c>, if write thime was set succesfully, else <c>false</c>.</returns>
        bool SetWriteCycleTime(float xtalFrequencyMHz);
        /// <summary>
        /// Calculate the default write cycle time c value.
        /// </summary>
        /// <returns>Returns c - is the value set by the �Set Write Cycle Time� instruction.</returns>
        byte CalculateWriteCycle();
        /// <summary>
        /// Calculate the write cycle time c value.
        /// </summary>
        /// <param name="xtalFrequencyMHz">X-Tal Frequency of th Z-Wave mudule in MHz</param>
        /// <returns>Returns c - is the value set by the �Set Write Cycle Time� instruction.</returns>
        byte CalculateWriteCycle(float xtalFrequencyMHz);
        /// <summary>
        /// Opens connection with the specified port name.
        /// </summary>
        /// <param name="portName">COM port name.</param>
        void Open(string portName);
        /// <summary>
        /// Closes the connection.
        /// </summary>
        void Close();
        /// <summary>
        /// Sets the working mode.See <see cref="WorkingModes"/>
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        bool SetMode(WorkingModes mode);
        /// <summary>
        /// Set the bit that identifies the Chip as a �Modem� device
        /// </summary>
        /// <returns><c>true</c>, if modem bits set ok.</returns>
        bool SetModemBits();
        /// <summary>
        /// Sets the boot loader mode.
        /// </summary>
        /// <param name="enable">if set to <c>true</c> enable.</param>
        /// <returns></returns>
        bool SetBootLoaderMode(bool enable);
        /// <summary>
        /// Gets the current firmware version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="revision">The revision.</param>
        /// <returns></returns>
        bool GetCurrentFirmwareVersion(out int version, out int revision);
        /// <summary>
        /// Start/stop calibration.
        /// </summary>
        /// <param name="enable">if set to <c>true</c> [enable].</param>
        /// <returns></returns>
        bool Calibration(bool enable);
    }
}
