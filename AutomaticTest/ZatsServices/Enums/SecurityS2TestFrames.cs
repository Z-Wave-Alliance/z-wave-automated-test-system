// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services
{
    /// <summary>
    /// Represents inclusion frames flow.
    /// </summary>
    public enum SecurityS2TestFrames
    {
        KEXGet = ZWave.BasicApplication.Security.SecurityS2TestFrames.KEXGet,
        KEXReport = ZWave.BasicApplication.Security.SecurityS2TestFrames.KEXReport,
        KEXSet = ZWave.BasicApplication.Security.SecurityS2TestFrames.KEXSet,
        PublicKeyReportB = ZWave.BasicApplication.Security.SecurityS2TestFrames.PublicKeyReportB,
        PublicKeyReportA = ZWave.BasicApplication.Security.SecurityS2TestFrames.PublicKeyReportA,
        KEXSetEcho = ZWave.BasicApplication.Security.SecurityS2TestFrames.KEXSetEcho,
        KEXReportEcho = ZWave.BasicApplication.Security.SecurityS2TestFrames.KEXReportEcho,
        NetworkKeyGet_S0 = ZWave.BasicApplication.Security.SecurityS2TestFrames.NetworkKeyGet_S0,
        NetworkKeyReport_S0 = ZWave.BasicApplication.Security.SecurityS2TestFrames.NetworkKeyReport_S0,
        NetworkKeyVerify_S0 = ZWave.BasicApplication.Security.SecurityS2TestFrames.NetworkKeyVerify_S0,
        NetworkKeyGet_S2Unauthenticated = ZWave.BasicApplication.Security.SecurityS2TestFrames.NetworkKeyGet_S2Unauthenticated,
        NetworkKeyReport_S2Unauthenticated = ZWave.BasicApplication.Security.SecurityS2TestFrames.NetworkKeyReport_S2Unauthenticated,
        NetworkKeyVerify_S2Unauthenticated = ZWave.BasicApplication.Security.SecurityS2TestFrames.NetworkKeyVerify_S2Unauthenticated,
        NetworkKeyGet_S2Authenticated = ZWave.BasicApplication.Security.SecurityS2TestFrames.NetworkKeyGet_S2Authenticated,
        NetworkKeyReport_S2Authenticated = ZWave.BasicApplication.Security.SecurityS2TestFrames.NetworkKeyReport_S2Authenticated,
        NetworkKeyVerify_S2Authenticated = ZWave.BasicApplication.Security.SecurityS2TestFrames.NetworkKeyVerify_S2Authenticated,
        NetworkKeyGet_S2Access = ZWave.BasicApplication.Security.SecurityS2TestFrames.NetworkKeyGet_S2Access,
        NetworkKeyReport_S2Access = ZWave.BasicApplication.Security.SecurityS2TestFrames.NetworkKeyReport_S2Access,
        NetworkKeyVerify_S2Access = ZWave.BasicApplication.Security.SecurityS2TestFrames.NetworkKeyVerify_S2Access,
        TransferEndA_S0 = ZWave.BasicApplication.Security.SecurityS2TestFrames.TransferEndA_S0,
        TransferEndA_S2Unauthenticated = ZWave.BasicApplication.Security.SecurityS2TestFrames.TransferEndA_S2Unauthenticated,
        TransferEndA_S2Authenticated = ZWave.BasicApplication.Security.SecurityS2TestFrames.TransferEndA_S2Authenticated,
        TransferEndA_S2Access = ZWave.BasicApplication.Security.SecurityS2TestFrames.TransferEndA_S2Access,
        TransferEndB = ZWave.BasicApplication.Security.SecurityS2TestFrames.TransferEndB,
        NonceGet = ZWave.BasicApplication.Security.SecurityS2TestFrames.NonceGet,
        NonceReport = ZWave.BasicApplication.Security.SecurityS2TestFrames.NonceReport,
        MessageEncapsulation = ZWave.BasicApplication.Security.SecurityS2TestFrames.MessageEncapsulation,
        CommandsSupportedReport = ZWave.BasicApplication.Security.SecurityS2TestFrames.CommandsSupportedReport,
        InclusionInititate1 = ZWave.BasicApplication.Security.SecurityS2TestFrames.InclusionInititate1,
        InclusionInititate2 = ZWave.BasicApplication.Security.SecurityS2TestFrames.InclusionInititate2,
        InclusionComplete1 = ZWave.BasicApplication.Security.SecurityS2TestFrames.InclusionComplete1,
        InclusionComplete2 = ZWave.BasicApplication.Security.SecurityS2TestFrames.InclusionComplete2
    }
}
