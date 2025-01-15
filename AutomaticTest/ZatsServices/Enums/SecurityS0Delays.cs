// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services
{
    /// <summary>
    /// Delays:
    /// 
    ///     SRC:              DEST:
    ///      |
    ///   NonceGet delay
    ///      |
    ///      .---> Nonce Get ----->.
    ///                            |
    ///                       NonceReport delay
    ///                            |
    ///      .<--- Nonce Report <--.                   
    ///      |
    ///   Command delay
    ///      |
    ///      .---> Msg Encap ----->.
    ///      
    /// 
    /// All other delays are specific _command_ delays (delay after receiving nonce report from destination)
    /// </summary>
    public enum SecurityS0Delays
    {
        NonceGet = ZWave.BasicApplication.Security.SecurityS0Delays.NonceGet,
        NonceReport = ZWave.BasicApplication.Security.SecurityS0Delays.NonceReport,
        Command = ZWave.BasicApplication.Security.SecurityS0Delays.Command,
        SchemeGet = ZWave.BasicApplication.Security.SecurityS0Delays.SchemeGet,
        SchemeReport = ZWave.BasicApplication.Security.SecurityS0Delays.SchemeReport,
        NetworkKeySet = ZWave.BasicApplication.Security.SecurityS0Delays.NetworkKeySet,
        NetworkKeyVerify = ZWave.BasicApplication.Security.SecurityS0Delays.NetworkKeyVerify,
        SchemeInherit = ZWave.BasicApplication.Security.SecurityS0Delays.SchemeInherit,
        SchemeReportEnc = ZWave.BasicApplication.Security.SecurityS0Delays.SchemeReportEnc
    }
}
