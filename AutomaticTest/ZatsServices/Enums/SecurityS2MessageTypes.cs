// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services
{
    /// <summary>
    /// Diferent S2 Message Encapsulation types, regarding functionality
    /// </summary>
    public enum SecurityS2MessageTypes
    {
        SinglecastAll = ZWave.BasicApplication.Security.MessageTypes.SinglecastAll,
        MulticastAll = ZWave.BasicApplication.Security.MessageTypes.MulticastAll,
        SinglecastWithSpan = ZWave.BasicApplication.Security.MessageTypes.SinglecastWithSpan,
        SinglecastWithMpan = ZWave.BasicApplication.Security.MessageTypes.SinglecastWithMpan,
        SinglecastWithMpanGrp = ZWave.BasicApplication.Security.MessageTypes.SinglecastWithMpanGrp,
        SinglecastWithMos = ZWave.BasicApplication.Security.MessageTypes.SinglecastWithMos
    }
}
