// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services
{
    public enum SecuritySchemes
    {
        NONE = ZWave.Enums.SecuritySchemes.NONE,
        S0 = ZWave.Enums.SecuritySchemes.S0,
        S2_ACCESS = ZWave.Enums.SecuritySchemes.S2_ACCESS,
        S2_AUTHENTICATED = ZWave.Enums.SecuritySchemes.S2_AUTHENTICATED,
        S2_UNAUTHENTICATED = ZWave.Enums.SecuritySchemes.S2_UNAUTHENTICATED,
        S2_TEMP = ZWave.Enums.SecuritySchemes.S2_TEMP
    }

    public static class SecuritySchemeSet
    {
        public static SecuritySchemes[] ALL = {
            SecuritySchemes.S2_ACCESS,
            SecuritySchemes.S2_AUTHENTICATED,
            SecuritySchemes.S2_UNAUTHENTICATED,
            SecuritySchemes.S0
        };

        public static SecuritySchemes[] S0 = {
            SecuritySchemes.S0
        };

        public static SecuritySchemes[] ALLS2 = {
            SecuritySchemes.S2_ACCESS,
            SecuritySchemes.S2_AUTHENTICATED,
            SecuritySchemes.S2_UNAUTHENTICATED
        };
    }
}
