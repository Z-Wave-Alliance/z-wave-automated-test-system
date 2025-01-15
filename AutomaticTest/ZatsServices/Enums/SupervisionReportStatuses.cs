// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services
{
    public enum SupervisionReportStatuses
    {
        SUCCESS = ZWave.Enums.SupervisionReportStatuses.SUCCESS,
        NO_SUPPORT = ZWave.Enums.SupervisionReportStatuses.NO_SUPPORT,
        WORKING = ZWave.Enums.SupervisionReportStatuses.WORKING,
        FAIL = ZWave.Enums.SupervisionReportStatuses.FAIL,
        Test = ZWave.Enums.SupervisionReportStatuses.Test
    }
}
