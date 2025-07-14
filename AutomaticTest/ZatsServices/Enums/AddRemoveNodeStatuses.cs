// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;

namespace Zats.Services
{
    public enum AddRemoveNodeStatuses
    {
        None = ZWave.BasicApplication.Enums.AddRemoveNodeStatuses.None,
        Added = ZWave.BasicApplication.Enums.AddRemoveNodeStatuses.Added,
        Replicated = ZWave.BasicApplication.Enums.AddRemoveNodeStatuses.Replicated,
        Replaced = ZWave.BasicApplication.Enums.AddRemoveNodeStatuses.Replaced
    }
}
