// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
namespace Zats.Services
{
    public enum SleepModes
    {
        Idle = ZWave.BasicApplication.Enums.SleepModes.Idle,
        Stop = ZWave.BasicApplication.Enums.SleepModes.Stop,
        WUT = ZWave.BasicApplication.Enums.SleepModes.WUT,
        WUT_Fast = ZWave.BasicApplication.Enums.SleepModes.WUT_Fast,
        FrequentlyListening = ZWave.BasicApplication.Enums.SleepModes.FrequentlyListening,
    }
}
