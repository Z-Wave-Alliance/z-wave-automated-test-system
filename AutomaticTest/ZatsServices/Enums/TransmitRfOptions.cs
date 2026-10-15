// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
namespace Zats.Services
{
    public enum TransmitRfOptions
    {
        RfOptionSpeedNone = ZWave.Enums.TransmitRfOptions.RfOptionSpeedNone,
        RfOptionSpeed9600 = ZWave.Enums.TransmitRfOptions.RfOptionSpeed9600,
        RfOptionSpeed40k = ZWave.Enums.TransmitRfOptions.RfOptionSpeed40k,
        RfOptionSpeed100k = ZWave.Enums.TransmitRfOptions.RfOptionSpeed100k,
        RfOptionSpeedLR100k = ZWave.Enums.TransmitRfOptions.RfOptionSpeed200k
    }
}
