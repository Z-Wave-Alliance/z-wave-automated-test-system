// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
namespace Zats.Services
{
    public interface IIncludeSettings
    {
        int? TimeoutMs { get; set; }
        byte[] JoinDSK { get; set; }
        byte[] JoinCSA { get; set; }
        bool CancelDSKInputDialog { get; set; }
        bool CancelGrantedSchemes { get; set; }
        /// <summary>
        /// Used schemes to grant by Inclusion controller, null or empty - by default, None - disable all
        /// </summary>
        SecuritySchemes[] GrantedSchemes { get; set; }
        SecuritySchemes[] RequestedSchemes { get; set; }
        bool IsInteractiveMode { get; set; }
        bool SkipSmartStartLR { get; set; }
        int? InclusionControllerInitiateRequestTimeoutMs { get; set; }

        /// <summary>
        /// Disabled AutoSis for Zats by default!
        /// </summary>
        SetupNodeLifelineSettings SetupNodeLifelineSettings { get; set; }

        /// <summary>
        /// To set Add Mode for a controller during inclusion.
        /// </summary>
        Modes IncludeMode { get; set; }
        
        void Clear();
    }
}
