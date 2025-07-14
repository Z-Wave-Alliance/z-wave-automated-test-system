// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using ZWave;

namespace Zats.Services
{
    public interface IFirmwareUpdateMD
    {
        BasicResult.Void EnableSupport(params Alias[] controlles);
        BasicResult.Void EnableSupport(byte version, params Alias[] controlles);
    }
}
