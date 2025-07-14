// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services
{
    /// <summary>
    /// Inclusion Statuses values cluster static values
    /// </summary>
    public class InclusionStatus
    {
        public const string None = UicApplication.Enums.InclusionStatus.None;
        public const string S0 = UicApplication.Enums.InclusionStatus.S0;
        public const string S2_Unauthenticated = UicApplication.Enums.InclusionStatus.S2_Unauthenticated;
        public const string S2_Authenticated = UicApplication.Enums.InclusionStatus.S2_Authenticated;
        public const string S2_Access = UicApplication.Enums.InclusionStatus.S2_Access;
    }
}
