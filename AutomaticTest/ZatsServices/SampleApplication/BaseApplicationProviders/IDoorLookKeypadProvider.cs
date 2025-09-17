// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services.SampleApplication
{
    public interface IDoorLookKeypadProvider : ISampleApplicationProviderBase
    {
        void BatteryReport();
        bool? IsBoltLocked();
        bool? IsLatchOpen();
        void ToggleDoorHandle();
        void ToggleDoorHandle(byte duration);
        void ToggleKeyPad();
    }
}