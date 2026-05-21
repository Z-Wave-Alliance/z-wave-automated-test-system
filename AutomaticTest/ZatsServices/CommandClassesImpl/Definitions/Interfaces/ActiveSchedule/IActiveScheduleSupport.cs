// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Z-Wave Alliance https://z-wavealliance.org
using System;

namespace Zats.Services.CommandClassesImpl.Definitions.Interfaces.ActiveSchedule
{
    public interface IActiveScheduleSupport
    {
        byte TargetCC { get; }

        IActiveScheduleSupportData ScheduleSupportData { get; }

        bool IsActiveScheduleTargetCaptableByVersion();

        void UpdateTarget(UInt16? targetId = null);

        bool CheckForTargetsSchedules(IActiveScheduleTarget target);


        bool AddOrModifyYearDaySchedule(UInt16 targetId, UInt16 scheduleSlot, ActiveYearDaySchedule yearDaySchedule);

        void EraseYearDaySchedule(UInt16 targetId, UInt16 scheduleSlot);

        void EraseYearDaySchedules(UInt16 targetId);

        void EraseYearDaySchedules();


        bool AddOrModifyDailyRepeatingSchedule(UInt16 targetId, UInt16 scheduleSlot, ActiveDailyRepeatingSchedule dailyRepeatingSchedule);

        void EraseDailyRepeatingSchedule(UInt16 targetId, UInt16 scheduleSlot);

        void EraseDailyRepeatingSchedules(UInt16 targetId);

        void EraseDailyRepeatingSchedules();
    }
}
