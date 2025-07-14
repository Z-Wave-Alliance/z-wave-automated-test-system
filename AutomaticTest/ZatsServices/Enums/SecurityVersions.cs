// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;

namespace Zats.Services
{
    /// <summary>
    /// Security Versions:
    /// </summary>
    [Flags]
    public enum SecurityVersions
    {
        None = 0x00,
        S0 = 0x01,
        S2 = 0x02
    }
}
