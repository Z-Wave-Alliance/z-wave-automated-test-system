// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
namespace Zats.Services.SampleApplication
{
    public interface ISwitchOnOffProvider : ISampleApplicationProviderBase
    {
        void HoldKey01(byte duration);
        bool? IsStatusOn();
        void OnOff();
    }
}