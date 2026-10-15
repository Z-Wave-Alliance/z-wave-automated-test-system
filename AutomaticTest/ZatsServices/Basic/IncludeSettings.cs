// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
namespace Zats.Services
{
    public class IncludeSettings : IIncludeSettings
    {
        public int? TimeoutMs { get; set; }
        public bool CancelDSKInputDialog { get; set; }
        public bool CancelGrantedSchemes { get; set; }
        public SecuritySchemes[] GrantedSchemes { get; set; }
        public SecuritySchemes[] RequestedSchemes { get; set; }
        public byte[] JoinCSA { get; set; }
        public byte[] JoinDSK { get; set; }
        public bool IsInteractiveMode { get; set; }
        public bool SkipSmartStartLR { get; set; }
        public int? InclusionControllerInitiateRequestTimeoutMs { get; set; }

        // Disable AutoSis for Zats by default!
        public SetupNodeLifelineSettings SetupNodeLifelineSettings { get; set; } = SetupNodeLifelineSettings.DisableAutoSisOnly;
        public Modes IncludeMode { get; set; } = Modes.NodeAny;

        public void Clear()
        {
            IsInteractiveMode = false;
            CancelDSKInputDialog = false;
            CancelGrantedSchemes = false;
            GrantedSchemes = null;
            RequestedSchemes = null;
            JoinCSA = null;
            JoinDSK = null;
            SkipSmartStartLR = false;
            SetupNodeLifelineSettings = SetupNodeLifelineSettings.DisableAutoSisOnly;
            TimeoutMs = null;
            InclusionControllerInitiateRequestTimeoutMs = null;
            IncludeMode = Modes.NodeAny;
        }
    }
}
