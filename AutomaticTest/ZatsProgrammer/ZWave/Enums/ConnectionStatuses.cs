// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;

namespace ZWave.Enums
{
    /// <summary>
    /// Connection Statuses enumeration.
    /// </summary>
    public enum ConnectionStatuses
    {
        /// <summary>
        /// Initial status of <see cref="ZWave.Devices.IDevice"></see> connection.
        /// </summary>
        None,
        /// <summary>
        /// <see cref="ZWave.Devices.IDevice"></see> conection established.
        /// </summary>
        Opened,
        /// <summary>
        /// <see cref="ZWave.Devices.IDevice"></see> connection correctly closed.
        /// </summary>
        Closed,
        /// <summary>
        /// <see cref="ZWave.Devices.IDevice"></see> connection lost or invalid.
        /// </summary>
        Invalid,
    }
}
