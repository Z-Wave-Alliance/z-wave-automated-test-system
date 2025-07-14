// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services
{
    /// <summary>
    /// Diferent S2 Message Encapsulation Extensions
    /// </summary>
    public enum SecurityS2ExtensionTypes
    {
        Span = ZWave.Security.ExtensionTypes.Span,
        Mpan = ZWave.Security.ExtensionTypes.Mpan,
        MpanGrp = ZWave.Security.ExtensionTypes.MpanGrp,
        Mos = ZWave.Security.ExtensionTypes.Mos,
        Test = ZWave.Security.ExtensionTypes.Test
    }
}
