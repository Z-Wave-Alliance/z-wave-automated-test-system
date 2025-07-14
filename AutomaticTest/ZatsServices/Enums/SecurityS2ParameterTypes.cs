// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services
{
    /// <summary>
    /// Diferent S2 Parameters Overridies Types
    /// </summary>
    public enum SecurityS2ParameterTypes
    {
        ReservedField = ZWave.BasicApplication.Security.ParameterS2Type.ReservedField,
        SecretKey = ZWave.BasicApplication.Security.ParameterS2Type.SecretKey,
        Sender_EI = ZWave.BasicApplication.Security.ParameterS2Type.Sender_EI,
        SequenceNo = ZWave.BasicApplication.Security.ParameterS2Type.SequenceNo,
        Span = ZWave.BasicApplication.Security.ParameterS2Type.Span,
    }
}
