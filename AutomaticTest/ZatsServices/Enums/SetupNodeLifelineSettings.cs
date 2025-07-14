// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;

[Flags]
public enum SetupNodeLifelineSettings
{
    Default = ZWave.BasicApplication.Enums.SetupNodeLifelineSettings.Default,
    SkipAllSteps = ZWave.BasicApplication.Enums.SetupNodeLifelineSettings.SkipAllSteps,
    IsDeleteReturnRoute = ZWave.BasicApplication.Enums.SetupNodeLifelineSettings.IsDeleteReturnRoute,
    IsAssignReturnRoute = ZWave.BasicApplication.Enums.SetupNodeLifelineSettings.IsAssignReturnRoute,
    IsAssociationCreate = ZWave.BasicApplication.Enums.SetupNodeLifelineSettings.IsAssociationCreate,
    IsMultichannelAssociationCreate = ZWave.BasicApplication.Enums.SetupNodeLifelineSettings.IsMultichannelAssociationCreate,
    IsWakeUpCapabilities = ZWave.BasicApplication.Enums.SetupNodeLifelineSettings.IsWakeUpCapabilities,
    IsWakeUpInterval = ZWave.BasicApplication.Enums.SetupNodeLifelineSettings.IsWakeUpInterval,
    IsSetAsSisAutomatically = ZWave.BasicApplication.Enums.SetupNodeLifelineSettings.IsSetAsSisAutomatically,
    IsBasedOnZwpRoleType = ZWave.BasicApplication.Enums.SetupNodeLifelineSettings.IsBasedOnZwpRoleType,
    SetAllSteps = ZWave.BasicApplication.Enums.SetupNodeLifelineSettings.SetAllSteps,
    DisableAutoSisOnly = ZWave.BasicApplication.Enums.SetupNodeLifelineSettings.DisableAutoSisOnly
}