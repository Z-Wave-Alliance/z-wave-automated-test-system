// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Devices
{
    /// <summary>
    /// This interface provides properties that allow the client program using Flash Settings.
    /// </summary>
    public interface IFlashSettings
    {
        /// <summary>
        /// Gets or sets the frequency.
        /// </summary>
        /// <value>The frequency.</value>
        byte Frequency { get; set; }
        /// <summary>
        /// Gets or sets the IsFrequencySet.
        /// </summary>
        /// <value><c>true</c> or <c>false</c></value>
        bool IsFrequencySet { get; set; }
        /// <summary>
        /// Gets or sets the normal power.
        /// </summary>
        /// <value>The normal power.</value>
        byte NormalPower { get; set; }

        /// <summary>
        /// Gets or sets the normal power for channel 0.
        /// </summary>
        /// <value>The normal power for channel 0.</value>
        byte NormalPowerCh0 { get; set; }

        /// <summary>
        /// Gets or sets the normal power for channel 1.
        /// </summary>
        /// <value>The normal power for channel 1.</value>
        byte NormalPowerCh1 { get; set; }

        /// <summary>
        /// Gets or sets the normal power for channel 2.
        /// </summary>
        /// <value>The normal power for channel 2.</value>
        byte NormalPowerCh2 { get; set; }

        /// <summary>
        /// Gets or sets the IsNormalPowerSet.
        /// </summary>
        /// <value><c>true</c> or <c>false</c></value>
        bool IsNormalPowerSet { get; set; }

        /// <summary>
        /// Gets or sets the IsNormalPowerSet for channel 0.
        /// </summary>
        /// <value><c>true</c> or <c>false</c></value>
        bool IsNormalPowerSetCh0 { get; set; }

        /// <summary>
        /// Gets or sets the IsNormalPowerSet for channel 1.
        /// </summary>
        /// <value><c>true</c> or <c>false</c></value>
        bool IsNormalPowerSetCh1 { get; set; }

        /// <summary>
        /// Gets or sets the IsNormalPowerSet for channel 2.
        /// </summary>
        /// <value><c>true</c> or <c>false</c></value>
        bool IsNormalPowerSetCh2 { get; set; }

        /// <summary>
        /// Gets or sets the low power.
        /// </summary>
        /// <value>The low power.</value>
        byte LowPower { get; set; }

        /// <summary>
        /// Gets or sets the low power for channel 0.
        /// </summary>
        /// <value>The low power for channel 0.</value>
        byte LowPowerCh0 { get; set; }

        /// <summary>
        /// Gets or sets the low power for channel 1.
        /// </summary>
        /// <value>The low power for channel 1.</value>
        byte LowPowerCh1 { get; set; }

        /// <summary>
        /// Gets or sets the low power for channel 2.
        /// </summary>
        /// <value>The low power for channel 2.</value>
        byte LowPowerCh2 { get; set; }

        /// <summary>
        /// Gets or sets the IsLowPowerSet.
        /// </summary>
        /// <value><c>true</c> or <c>false</c></value>
        bool IsLowPowerSet { get; set; }

        /// <summary>
        /// Gets or sets the IsLowPowerSet for channel 0.
        /// </summary>
        /// <value><c>true</c> or <c>false</c></value>
        bool IsLowPowerSetCh0 { get; set; }

        /// <summary>
        /// Gets or sets the IsLowPowerSet for channel 1.
        /// </summary>
        /// <value><c>true</c> or <c>false</c></value>
        bool IsLowPowerSetCh1 { get; set; }

        /// <summary>
        /// Gets or sets the IsLowPowerSet for channel 2.
        /// </summary>
        /// <value><c>true</c> or <c>false</c></value>
        bool IsLowPowerSetCh2 { get; set; }

        /// <summary>
        /// Gets or sets the Rx match.
        /// </summary>
        /// <value>The Rx match.</value>
        byte RxMatch { get; set; }
        /// <summary>
        /// Gets or sets the IsRxMatchSet.
        /// </summary>
        /// <value><c>true</c> or <c>false</c></value>
        bool IsRxMatchSet { get; set; }
        /// <summary>
        /// Gets or sets the Tx match.
        /// </summary>
        /// <value>The Tx match.</value>
        byte TxMatch { get; set; }
        /// <summary>
        /// Gets or sets the IsTxMatchSet.
        /// </summary>
        /// <value><c>true</c> or <c>false</c></value>
        bool IsTxMatchSet { get; set; }

        /// <summary>
        /// Gets or sets the Calibration Value.
        /// </summary>
        /// <value>The Calibration Value</value>
        sbyte CalibrationValue { get; set; }

        /// <summary>
        /// Parse the memory buffer and store settings from it to this class.
        /// </summary>
        /// <param name="chipType">Chip type.</param>
        /// <param name="bufferAddress">Absulute address of the memory, stored in buffer.</param>
        /// <param name="buffer">Buffer with memory data.</param>
        /// <returns>None.</returns>
        void ParseBuffer(byte chipType, int bufferAddress, byte[] buffer);
        /// <summary>
        /// Store settings this class to the memory buffer.
        /// </summary>
        /// <param name="chipType">Chip type.</param>
        /// <param name="bufferAddress">Absulute address of the memory, stored in buffer.</param>
        /// <param name="buffer">Buffer with memory data.</param>
        /// <returns>None.</returns>
        bool StoreToBuffer(byte chipType, int bufferAddress, byte[] buffer);
    }
}
