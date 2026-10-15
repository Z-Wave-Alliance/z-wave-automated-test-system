// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
using System;
using System.Collections.Generic;

namespace Zats.Services.CommandClassesImpl.Definitions.Interfaces.ActiveSchedule
{
    public interface IActiveScheduleTarget
    {
        UInt16 TargetId { get; }
        bool ScheduleEnabled { get; set; }
        List<ActiveYearDaySchedule> YearDaySchedules { get; set; }
        List<ActiveDailyRepeatingSchedule> DailyRepeatingSchedules { get; set; }
    }
}
