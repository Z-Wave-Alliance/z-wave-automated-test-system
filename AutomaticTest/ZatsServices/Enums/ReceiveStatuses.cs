// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;

namespace Zats.Services
{
    [Flags]
    public enum ReceiveStatuses
    {
        RoutedBusy = ZWave.Enums.ReceiveStatuses.RoutedBusy,
        LowPower = ZWave.Enums.ReceiveStatuses.LowPower,
        TypeBroad = ZWave.Enums.ReceiveStatuses.TypeBroad,
        TypeMulti = ZWave.Enums.ReceiveStatuses.TypeMulti,
        TypeExplore = ZWave.Enums.ReceiveStatuses.TypeExplore,
        ForeignFrame = ZWave.Enums.ReceiveStatuses.ForeignFrame
    }
}
