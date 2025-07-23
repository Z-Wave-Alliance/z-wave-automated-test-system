// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services.SampleApplication
{
    public interface IWallControllerProvider : ISampleApplicationProviderBase
    {
        void HoldKey01(byte duration);
        void HoldKey02(byte duration);
        void HoldKey03(byte duration);
        void Key01();
        void Key02();
        void Key03();
    }
}