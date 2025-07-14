// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com

namespace Zats.Services
{
    public enum SucUpdateStatuses
    {
        Done = ZWave.BasicApplication.Enums.SucUpdateStatuses.Done,
        Abort = ZWave.BasicApplication.Enums.SucUpdateStatuses.Abort,
        Wait = ZWave.BasicApplication.Enums.SucUpdateStatuses.Wait,
        Disabled = ZWave.BasicApplication.Enums.SucUpdateStatuses.Disabled,
        Overflow = ZWave.BasicApplication.Enums.SucUpdateStatuses.Overflow,
    }
}
