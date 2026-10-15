// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using ZWave;

namespace Zats.Services.SampleApplication
{
    public interface IIOHandler
    {
        ReturnValueResult ToggleButton(byte buttonNumber);
        ReturnValueResult PressButton(byte buttonNumber, byte times);
        ReturnValueResult HoldButton(byte buttonNumber, byte duration);
        bool? GetLedState(byte? ledsMask);
        bool IsLearnModeActive(int durationMs = 30000);
    }
}
