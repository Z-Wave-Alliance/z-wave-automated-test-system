// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Z-Wave Alliance https://z-wavealliance.org
using System;
using System.Collections.Generic;
using ZatsServices_netcore.CommandClassesImpl.Definitions.Interfaces;

namespace Zats.Services.CommandClassesImpl.Definitions.Interfaces.ActiveSchedule
{
    public interface IActiveScheduleSupportData : INodeItem
    {
        byte MinimumVersionSupportingActiveSchedule { get; }
        bool ActiveScheduleEnabled { get; set; }
        UInt16 NumberOfSupportedTargets { get; }
        UInt16 NumberOfSupportedYearDaySchedules { get; set; }
        UInt16 NumberOfSupportedDailyRepeatingSchedules { get; set; }
        List<IActiveScheduleTarget> Targets { get; }
        bool SchedulesMetadataAllowed { get; }
    }

    /// <summary>
    /// Helper class for Active Schedule UI control identifiers and operations.
    /// Provides reusable functionality for any Command Class supporting Active Schedules.
    /// </summary>
    public static class ActiveScheduleUiHelper
    {
        /// <summary>
        /// Gets the list of UiIds for schedule-related controls that should be shown/hidden
        /// based on the ActiveScheduleEnabled flag
        /// </summary>
        public static readonly string[] ScheduleRelatedControlUiIds = new[]
        {
            "NumberOfSupportedYearDaySchedules",
            "NumberOfSupportedDailyRepeatingSchedules",
            "_NumberOfSupportedYearDaySchedules",      // separator
            "_NumberOfSupportedDailyRepeatingSchedules" // separator
        };

        /// <summary>
        /// Gets the list of possible UiIds for the ActiveScheduleEnabled checkbox control.
        /// Different Command Classes might use different names for this property.
        /// </summary>
        public static readonly string[] ActiveScheduleEnabledControlUiIds = new[]
        {
            "ActiveScheduleEnabled",  // Standard name
            "UserScheduleSupport"     // Used by User Credential CC
        };
    }
}
