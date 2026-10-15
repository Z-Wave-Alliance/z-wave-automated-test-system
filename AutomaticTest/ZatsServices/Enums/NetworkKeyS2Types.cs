// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
namespace Zats.Services
{
    public enum NetworkKeyFlags
    {
        Unathenticated = ZWave.Enums.NetworkKeyS2Flags.S2Class0,
        Authenticated = ZWave.Enums.NetworkKeyS2Flags.S2Class1,
        Access = ZWave.Enums.NetworkKeyS2Flags.S2Class2,
        S0 = ZWave.Enums.NetworkKeyS2Flags.S0,
    }
}
