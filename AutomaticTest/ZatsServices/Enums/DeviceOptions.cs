// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services
{
    public enum DeviceOptions
    {
        NoneListening = ZWave.Enums.DeviceOptions.NoneListening,
        Listening = ZWave.Enums.DeviceOptions.Listening,
        OptionalFunctionality = ZWave.Enums.DeviceOptions.OptionalFunctionality,
        FreqListeningMode1000ms = ZWave.Enums.DeviceOptions.FreqListeningMode1000ms,
        FreqListeningMode250ms = ZWave.Enums.DeviceOptions.FreqListeningMode250ms
    }
}
