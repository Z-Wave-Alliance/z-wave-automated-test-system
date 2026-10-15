// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.Devices;
using ZatsServices_netcore.CommandClassesImpl.Definitions.Interfaces;
using Zats.Services.CommandClassesImpl.Definitions.Interfaces.ActiveSchedule;
using Utils;

namespace Zats.Services
{
    [CommandClassName("Active Schedule Command Class")]
    [CommandClassId(COMMAND_CLASS_ACTIVE_SCHEDULE.ID)]
    [MinVersion(1)]
    [MaxVersion(1)]
    public class ActiveScheduleSupport : CommandClassSupport
    {
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return null; }
            set {  }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public List<IActiveScheduleSupport> ActiveScheduleSupportingCCs { get; set; } = new List<IActiveScheduleSupport>();
        
        private List<IActiveScheduleSupport> EnabledActiveScheduleSupportingCCs
        {
            get 
            { 
                return ActiveScheduleSupportingCCs.Where(c => 
                        c.ScheduleSupportData != null 
                        && c.IsActiveScheduleTargetCaptableByVersion() 
                        && c.ScheduleSupportData.ActiveScheduleEnabled).ToList(); 
            }
        }

        public ActiveScheduleSupport()
            : base(COMMAND_CLASS_ACTIVE_SCHEDULE.ID)
        { }

        protected override void OnHandledDelayed(DataReceivedUnit ou)
        {
            ou.SetNextActionItems();

            if (MatchesDestinationEndPoint())
            {
                var sendDstNode = new NodeTag(ReceivedAchData.DstNode.Id, DstNode.EndPointId);
                var sendSrcNode = ReceivedAchData.SrcNode;
                byte[] command = ReceivedAchData.Command;
                var scheme = ReceivedAchData.SecurityScheme;
                bool isHandlingAllowed = IsHandlingAllowed(Network, command, scheme, Secure);

                ResultForSv = SvResultNames.Undefined;
                if (isHandlingAllowed && command != null && command.Length > 1)
                {
                    byte[] rpt = null;

                    if (command[1] == COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_CAPABILITIES_GET.ID)
                    {
                        rpt = PrepareCapabilitiesReport();
                    }
                    else if (command[1] == COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_ENABLE_SET.ID)
                    {
                        ResultForSv = HandleScheduleEnableSet(command, sendSrcNode);
                    }
                    else if (command[1] == COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_ENABLE_GET.ID)
                    {
                        rpt = PrepareScheduleEnableReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_YEAR_DAY_SCHEDULE_SET.ID)
                    {
                        ResultForSv = HandleYearDayScheduleSet(command, sendSrcNode);
                    }
                    else if (command[1] == COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_YEAR_DAY_SCHEDULE_GET.ID)
                    {
                        rpt = PrepareYearDayScheduleReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_DAILY_REPEATING_SCHEDULE_SET.ID)
                    {
                        ResultForSv = HandleDailyRepeatingScheduleSet(command, sendSrcNode);
                    }
                    else if (command[1] == COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_DAILY_REPEATING_SCHEDULE_GET.ID)
                    {
                        rpt = PrepareDailyRepeatingScheduleReport(command);
                    }

                    if (rpt != null && IsAnsweringAllowed())
                    {
                        ApiOperation sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command, sendDstNode.EndPointId);
                }
            }
        }

        private COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_CAPABILITIES_REPORT PrepareCapabilitiesReport()
        {
            var rpt = new COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_CAPABILITIES_REPORT() { };

            rpt.numberOfSupportedTargetCcs = (byte)EnabledActiveScheduleSupportingCCs.Count();
            rpt.supportedTargetCc = EnabledActiveScheduleSupportingCCs.Select(c => c.TargetCC).ToArray();
            rpt.vg1NumberOfSupportedTargets.AddRange(
                EnabledActiveScheduleSupportingCCs.Select(c =>
                    new COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_CAPABILITIES_REPORT.TVG1NUMBEROFSUPPORTEDTARGETS() {
                        numberOfSupportedTargets = Tools.GetBytes(c.ScheduleSupportData.NumberOfSupportedTargets)
                    }
                )
            );

            rpt.vg2NumberOfSupportedYearDaySchedulesPerTarget.AddRange(
                EnabledActiveScheduleSupportingCCs.Select(c =>
                    new COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_CAPABILITIES_REPORT.TVG2NUMBEROFSUPPORTEDYEARDAYSCHEDULESPERTARGET() {
                        numberOfSupportedYearDaySchedulesPerTarget = Tools.GetBytes(c.ScheduleSupportData.NumberOfSupportedYearDaySchedules)
                    }
                )
            );

            rpt.vg3NumberOfSupportedDailyRepeatingSchedulesPerTarget.AddRange(
                EnabledActiveScheduleSupportingCCs.Select(c =>
                    new COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_CAPABILITIES_REPORT.TVG3NUMBEROFSUPPORTEDDAILYREPEATINGSCHEDULESPERTARGET() {
                        numberOfSupportedDailyRepeatingSchedulesPerTarget = Tools.GetBytes(c.ScheduleSupportData.NumberOfSupportedDailyRepeatingSchedules)
                    }
                )
            );

            return rpt;
        }

        private SvResultNames HandleScheduleEnableSet(COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_ENABLE_SET command, NodeTag srcNode)
        {
            byte targetCc = command.targetCc;
            UInt16 targetId = Tools.ByteArrayToUInt16(command.targetId);

            return EnableScheduleTarget(targetCc, targetId, command.properties1.enabled != 0, srcNode) ? SvResultNames.Successful : SvResultNames.Failed;
        }

        private bool EnableScheduleTarget(byte targetCc, UInt16 targetId, bool enable, NodeTag srcNode)
        {
            IActiveScheduleSupport supportingCc = EnabledActiveScheduleSupportingCCs.FirstOrDefault(x => x.TargetCC == targetCc);
            IActiveScheduleTarget target = supportingCc?.GetTarget(targetId);

            if (target == null) return false; // Target CC / Target ID not found

            if (target.ScheduleEnabled != enable)
            {
                target.ScheduleEnabled = enable;
                supportingCc.UpdateTarget(targetId);

                SendUnsolicitedReport(
                    PrepareScheduleEnableReport(targetCc, targetId, ActiveScheduleReportCode.ScheduleModifiedZWave),
                    srcNode);
            }


            return true;
        }

        private COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_ENABLE_REPORT PrepareScheduleEnableReport(
            COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_ENABLE_GET command)
        {
            byte targetCc = command.targetCc;
            UInt16 targetId = Tools.ByteArrayToUInt16(command.targetId);

            return PrepareScheduleEnableReport(targetCc, targetId, ActiveScheduleReportCode.ResponseToGet);
        }

        private COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_ENABLE_REPORT PrepareScheduleEnableReport(
            byte targetCc, UInt16 targetId, ActiveScheduleReportCode reportCode)
        {
            IActiveScheduleTarget target = EnabledActiveScheduleSupportingCCs.GetTarget(targetCc, targetId);

            var rpt = new COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_ENABLE_REPORT()
            {
                targetCc = targetCc,
                targetId = Tools.GetBytes(targetId),
            };
            rpt.properties1.reportCode = (byte)reportCode;
            rpt.properties1.enabled = (byte)(target != null ? (target.ScheduleEnabled ? 1 : 0) : 0);
            return rpt;
        }

        private SvResultNames HandleYearDayScheduleSet(
            COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_YEAR_DAY_SCHEDULE_SET command, NodeTag srcNode)
        {
            byte targetCc = command.targetCc;
            UInt16 targetId = Tools.ByteArrayToUInt16(command.targetId);
            UInt16 scheduleSlot = Tools.ByteArrayToUInt16(command.scheduleSlotId);
            byte setAction = command.properties1.setAction;

            if (setAction == (byte)ActiveScheduleSetAction.Erase)
            {
                if (targetCc == 0 && targetId == 0 && scheduleSlot == 0)
                {
                    // Erase all schedules of this type on the node (CC:00A4.01.06.11.007)
                    EnabledActiveScheduleSupportingCCs.ForEach(cc =>
                    {
                        cc.EraseYearDaySchedules();
                        cc.UpdateTarget();
                    });
                }
                else if (targetCc != 0)
                {
                    IActiveScheduleSupport correspondingCC = EnabledActiveScheduleSupportingCCs.FirstOrDefault(x => x.TargetCC == targetCc);

                    if (targetId == 0 && scheduleSlot == 0)
                    {
                        // Erase all schedules of this type attached to all Targets from the specified Target CC. (CC:00A4.01.06.11.007)
                        correspondingCC?.EraseYearDaySchedules();
                        correspondingCC?.UpdateTarget();

                    }
                    else if (targetId != 0 && scheduleSlot == 0)
                    {
                        // Erase all schedules of this type attached to the Target. (CC:00A4.01.06.11.007)
                        correspondingCC?.EraseYearDaySchedules(targetId);
                        correspondingCC?.UpdateTarget(targetId);
                    }
                    else
                    {
                        correspondingCC?.EraseYearDaySchedule(targetId, scheduleSlot);
                        correspondingCC?.UpdateTarget(targetId);
                    }
                }

                // prepare auto report and return SV success (it can never fail because even when a schedule does not exist, the outcome is as expected)
                SendUnsolicitedReport(
                    PrepareYearDayScheduleReport(targetCc, targetId, scheduleSlot, ActiveScheduleReportCode.ScheduleModifiedZWave), 
                    srcNode);
                return SvResultNames.Successful;
            }
            else if (setAction == (byte)ActiveScheduleSetAction.Modify)
            {
                // create or modify schedule

                IActiveScheduleSupport correspondingCC = EnabledActiveScheduleSupportingCCs.FirstOrDefault(x => x.TargetCC == targetCc);

                if (correspondingCC != null && correspondingCC.GetTarget(targetId) is IActiveScheduleTarget target)
                {
                    UInt16 startYear = Tools.ByteArrayToUInt16(command.startYear);
                    UInt16 stopYear = Tools.ByteArrayToUInt16(command.stopYear);

                    // CC:00A4.01.06.11.008 / CC:00A4.01.06.11.013: start MUST be a valid timestamp
                    if (!ActiveScheduleExtensions.TryParseYearDayScheduleTimestamp(
                            startYear, command.startMonth, command.startDay, command.startHour, command.startMinute,
                            out DateTime startDate))
                    {
                        return SvResultNames.Failed;
                    }

                    DateTime? endDate = null;

                    if (ActiveScheduleExtensions.AreAllYearDayStopParametersZero(
                            stopYear, command.stopMonth, command.stopDay, command.stopHour, command.stopMinute))
                    {
                        // CC:00A4.01.06.11.014: zeroed stop — interpretation is defined by the Target CC
                        endDate = null;
                    }
                    else if (ActiveScheduleExtensions.TryParseYearDayScheduleTimestamp(
                                 stopYear, command.stopMonth, command.stopDay, command.stopHour, command.stopMinute,
                                 out DateTime stopDate))
                    {
                        // CC:00A4.01.06.11.002: stop MUST occur after start
                        if (stopDate <= startDate)
                        {
                            return SvResultNames.Failed;
                        }

                        endDate = stopDate;
                    }
                    else
                    {
                        return SvResultNames.Failed;
                    }

                    var schedule = new ActiveYearDaySchedule()
                    {
                        Slot = scheduleSlot,
                        StartDate = startDate,
                        EndDate = endDate
                    };

                        var svResult = SvResultNames.Successful;


                        if (command.properties2.metadataLength > 0)
                        {
                            if (correspondingCC.ScheduleSupportData.SchedulesMetadataAllowed)
                            {
                                schedule.MetaData = command.metadata?.ToArray();
                            }
                            else
                            {
                                schedule.MetaData = null;
                                svResult = SvResultNames.Failed;
                            }
                        }

                        target.ScheduleEnabled = true; // enabling schedule when scheduele has been set successfully (CC:00A4.01.06.11.001)

                        if (target.YearDaySchedules.Find(s => s.Slot == scheduleSlot) is ActiveYearDaySchedule existingSchedule)
                        {
                            // schedule slot exists, check if modification is needed
                            if (existingSchedule.Equals(schedule) == false)
                            {
                                if (correspondingCC.AddOrModifyYearDaySchedule(targetId, scheduleSlot, schedule))
                                {
                                    correspondingCC.UpdateTarget(targetId);

                                    SendUnsolicitedReport(
                                        PrepareYearDayScheduleReport(targetCc, targetId, scheduleSlot, ActiveScheduleReportCode.ScheduleModifiedZWave),
                                        srcNode);
                                }
                                else
                                {
                                    svResult = SvResultNames.Failed;
                                }
                            }
                            return svResult;
                        }
                        else
                        {
                            // add new schedule
                            if (scheduleSlot <= correspondingCC.ScheduleSupportData.NumberOfSupportedYearDaySchedules)
                            {
                                if (correspondingCC.AddOrModifyYearDaySchedule(targetId, scheduleSlot, schedule))
                                {
                                    correspondingCC.UpdateTarget(targetId);

                                    SendUnsolicitedReport(
                                        PrepareYearDayScheduleReport(targetCc, targetId, scheduleSlot, ActiveScheduleReportCode.ScheduleModifiedZWave),
                                        srcNode);
                                }
                                else
                                {
                                    svResult = SvResultNames.Failed;
                                }

                                return svResult;
                            }
                        }
                }
            }

            return SvResultNames.Failed;
        }

        private byte[] PrepareYearDayScheduleReport(
            COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_YEAR_DAY_SCHEDULE_GET command)
        {
            byte targetCc = command.targetCc;
            UInt16 targetId = Tools.ByteArrayToUInt16(command.targetId);
            UInt16 scheduleSlot = Tools.ByteArrayToUInt16(command.scheduleSlotId);
            return PrepareYearDayScheduleReport(targetCc, targetId, scheduleSlot, ActiveScheduleReportCode.ResponseToGet);
        }

        private byte[] PrepareYearDayScheduleReport(
            byte targetCc, UInt16 targetId, UInt16 scheduleSlot, ActiveScheduleReportCode reportCode)
        {
            var correspondingCC = EnabledActiveScheduleSupportingCCs.FirstOrDefault(x => x.TargetCC == targetCc);
            IActiveScheduleTarget target = correspondingCC.GetTarget(targetId);

            // CC:00A4.01.07.11.004: return the first occupied slot when slotId = 0 is requested
            ActiveYearDaySchedule schedule = scheduleSlot > 0 ?
                target?.YearDaySchedules.FirstOrDefault(s => s.Slot == scheduleSlot) :
                target?.YearDaySchedules.OrderBy(s => s.Slot).FirstOrDefault();

            // CC:00A4.01.07.11.005: ignore get command when any of the requested values are not in the respective provided range
            if (correspondingCC == null ||
                correspondingCC.ScheduleSupportData == null ||
                targetId > correspondingCC.ScheduleSupportData.NumberOfSupportedTargets ||
                scheduleSlot > correspondingCC.ScheduleSupportData.NumberOfSupportedYearDaySchedules)
            {
                return null;
            }

            var rpt = new COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_YEAR_DAY_SCHEDULE_REPORT()
            {
                targetCc = targetCc,
                targetId = Tools.GetBytes(targetId),
                // CC:00A4.01.08.11.004: an empty/erased slot still echoes the requested Schedule Slot ID
                // (with zeroed time fields). Only the slotId = 0 "first occupied" request stays 0 when empty.
                scheduleSlotId = Tools.GetBytes(schedule?.Slot ?? scheduleSlot),
                nextScheduleSlotId = Tools.GetBytes(target?.YearDaySchedules.GetNextScheduleSlot(schedule?.Slot ?? scheduleSlot) ?? 0)
            };
            rpt.properties1.reportCode = (byte)reportCode;

            rpt.startYear = Tools.GetBytes((UInt16)(schedule?.StartDate.Year ?? 0));
            rpt.startMonth = (byte) (schedule?.StartDate.Month ?? 0);
            rpt.startDay = (byte)(schedule?.StartDate.Day ?? 0);
            rpt.startHour = (byte)(schedule?.StartDate.Hour ?? 0);
            rpt.startMinute = (byte)(schedule?.StartDate.Minute ?? 0);

            rpt.stopYear = Tools.GetBytes((UInt16)(schedule?.EndDate?.Year ?? 0));
            rpt.stopMonth = (byte)(schedule?.EndDate?.Month ?? 0);
            rpt.stopDay = (byte)(schedule?.EndDate?.Day ?? 0);
            rpt.stopHour = (byte)(schedule?.EndDate?.Hour ?? 0);
            rpt.stopMinute = (byte)(schedule?.EndDate?.Minute ?? 0);

            if (correspondingCC?.ScheduleSupportData?.SchedulesMetadataAllowed ?? false)
            {
                rpt.properties2.metadataLength = (byte)(schedule?.MetaData?.Length ?? 0);
                rpt.metadata = rpt.properties2.metadataLength > 0 ? schedule?.MetaData : null;
            }

            return rpt;
        }

        private SvResultNames HandleDailyRepeatingScheduleSet(
            COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_DAILY_REPEATING_SCHEDULE_SET command, NodeTag srcNode)
        {
            byte targetCc = command.targetCc;
            UInt16 targetId = Tools.ByteArrayToUInt16(command.targetId);
            UInt16 scheduleSlot = Tools.ByteArrayToUInt16(command.scheduleSlotId);
            byte setAction = command.properties1.setAction;

            if (setAction == (byte)ActiveScheduleSetAction.Erase)
            {
                if (targetCc == 0 && targetId == 0 && scheduleSlot == 0)
                {
                    // Erase all schedules of this type on the node (CC:00A4.01.06.11.007)
                    EnabledActiveScheduleSupportingCCs.ForEach(cc =>
                    {
                        cc.EraseDailyRepeatingSchedules();
                        cc.UpdateTarget();
                    });
                }
                else if (targetCc != 0)
                {
                    IActiveScheduleSupport correspondingCC = EnabledActiveScheduleSupportingCCs.FirstOrDefault(x => x.TargetCC == targetCc);

                    if (targetCc != 0 && targetId == 0 && scheduleSlot == 0)
                    {
                        // Erase all schedules of this type attached to all Targets from the specified Target CC. (CC:00A4.01.06.11.007)
                        correspondingCC?.EraseDailyRepeatingSchedules();
                        correspondingCC?.UpdateTarget();
                    }
                    else if (targetCc != 0 && targetId != 0 && scheduleSlot == 0)
                    {
                        // Erase all schedules of this type attached to the Target. (CC:00A4.01.06.11.007)
                        correspondingCC?.EraseDailyRepeatingSchedules(targetId);
                        correspondingCC?.UpdateTarget(targetId);
                    }
                    else
                    {
                        correspondingCC?.EraseDailyRepeatingSchedule(targetId, scheduleSlot);
                        correspondingCC?.UpdateTarget(targetId);
                    }
                }

                // prepare auto report and return SV success (it can never fail because even when a schedule does not exist, the outcome is as expected)
                SendUnsolicitedReport(
                    PrepareDailyRepeatingScheduleReport(targetCc, targetId, scheduleSlot, ActiveScheduleReportCode.ScheduleModifiedZWave), 
                    srcNode);
                return SvResultNames.Successful;
            }
            else if (setAction == (byte)ActiveScheduleSetAction.Modify)
            {
                // create or modify schedule

                IActiveScheduleSupport correspondingCC = EnabledActiveScheduleSupportingCCs.FirstOrDefault(x => x.TargetCC == targetCc);

                if (correspondingCC != null && correspondingCC.GetTarget(targetId) is IActiveScheduleTarget target)
                {
                    if (command.startHour > 23 || command.startMinute > 59 ||
                        command.durationHour > 23 || command.durationMinute > 59 ||
                        (command.weekDayBitmask.Value & 0x7F) == 0) // CC:00A4.01.09.11.013
                    {
                        return SvResultNames.Failed;
                    }

                    var schedule = new ActiveDailyRepeatingSchedule()
                    {
                        Slot = scheduleSlot,
                        DaysOfWeek = command.weekDayBitmask.Value.GetDaysOfWeekFromBitmask(),
                        StartHour = command.startHour,
                        StartMinute = command.startMinute,
                        DurationHour = command.durationHour,
                        DurationMinute = command.durationMinute
                    };

                    var svResult = SvResultNames.Successful;

                    if (command.properties2.metadataLength > 0)
                    {
                        if (correspondingCC.ScheduleSupportData.SchedulesMetadataAllowed)
                        {
                            schedule.MetaData = command.metadata?.ToArray();
                        }
                        else
                        {
                            schedule.MetaData = null;
                            svResult = SvResultNames.Failed;
                        }
                    }
                        
                    target.ScheduleEnabled = true; // enabling schedule when scheduele has been set successfully (CC:00A4.01.06.11.001)
                    
                    if (target.DailyRepeatingSchedules.Find(s => s.Slot == scheduleSlot) is ActiveDailyRepeatingSchedule existingSchedule)
                    {
                        // schedule slot exists, check if modification is needed
                        if (existingSchedule.Equals(schedule) == false)
                        {
                            if (correspondingCC.AddOrModifyDailyRepeatingSchedule(targetId, scheduleSlot, schedule))
                            {
                                correspondingCC.UpdateTarget(targetId);

                                SendUnsolicitedReport(
                                    PrepareDailyRepeatingScheduleReport(targetCc, targetId, scheduleSlot, ActiveScheduleReportCode.ScheduleModifiedZWave),
                                    srcNode);
                            }
                            else
                            {
                                svResult = SvResultNames.Failed;
                            }
                        }
                        return svResult;

                    }
                    else
                    {
                        // add new schedule
                        if (scheduleSlot <= correspondingCC.ScheduleSupportData.NumberOfSupportedDailyRepeatingSchedules)
                        {
                            if (correspondingCC.AddOrModifyDailyRepeatingSchedule(targetId, scheduleSlot, schedule))
                            {
                                correspondingCC.UpdateTarget(targetId);

                                SendUnsolicitedReport(
                                    PrepareDailyRepeatingScheduleReport(targetCc, targetId, scheduleSlot, ActiveScheduleReportCode.ScheduleModifiedZWave),
                                    srcNode);
                            }
                            else
                            {
                                svResult = SvResultNames.Failed;
                            }
                            return svResult;
                        }
                    }
                }
            }

            return SvResultNames.Failed;
        }

        private byte[] PrepareDailyRepeatingScheduleReport(
            COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_DAILY_REPEATING_SCHEDULE_GET command)
        {
            byte targetCc = command.targetCc;
            UInt16 targetId = Tools.ByteArrayToUInt16(command.targetId);
            UInt16 scheduleSlot = Tools.ByteArrayToUInt16(command.scheduleSlotId);
            return PrepareDailyRepeatingScheduleReport(targetCc, targetId, scheduleSlot, ActiveScheduleReportCode.ResponseToGet);
        }

        private byte[] PrepareDailyRepeatingScheduleReport(
            byte targetCc, UInt16 targetId, UInt16 scheduleSlot, ActiveScheduleReportCode reportCode)
        {
            var correspondingCC = EnabledActiveScheduleSupportingCCs.FirstOrDefault(x => x.TargetCC == targetCc);
            IActiveScheduleTarget target = correspondingCC.GetTarget(targetId);

            // CC:00A4.01.0A.11.004: return the first occupied slot when slotId = 0 is requested
            ActiveDailyRepeatingSchedule schedule = scheduleSlot > 0 ? 
                target?.DailyRepeatingSchedules.FirstOrDefault(s => s.Slot == scheduleSlot) :
                target?.DailyRepeatingSchedules.OrderBy(s => s.Slot).FirstOrDefault();

            // CC:00A4.01.0A.11.005: ignore get command when any of the requested values are not in the respective provided range
            if (correspondingCC == null ||
                correspondingCC.ScheduleSupportData == null ||
                targetId > correspondingCC.ScheduleSupportData.NumberOfSupportedTargets ||
                scheduleSlot > correspondingCC.ScheduleSupportData.NumberOfSupportedDailyRepeatingSchedules)
            {
                return null;
            }

            var rpt = new COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_DAILY_REPEATING_SCHEDULE_REPORT()
            {
                targetCc = targetCc,
                targetId = Tools.GetBytes(targetId),
                // CC:00A4.01.0B.11.x: an empty/erased slot still echoes the requested Schedule Slot ID
                // (with zeroed schedule fields). Only the slotId = 0 "first occupied" request stays 0 when empty.
                scheduleSlotId = Tools.GetBytes(schedule?.Slot ?? scheduleSlot),
                nextScheduleSlotId = Tools.GetBytes(target?.DailyRepeatingSchedules.GetNextScheduleSlot(schedule?.Slot ?? scheduleSlot) ?? 0)
            };
            rpt.properties1.reportCode = (byte)reportCode;

            rpt.weekDayBitmask = (byte)(schedule != null ? schedule.DaysOfWeek.Aggregate(0, (acc, day) => acc | (int)day) : 0);
            rpt.startHour = (byte)(schedule?.StartHour ?? 0);
            rpt.startMinute = (byte)(schedule?.StartMinute ?? 0);
            rpt.durationHour = (byte)(schedule?.DurationHour ?? 0);
            rpt.durationMinute = (byte)(schedule?.DurationMinute ?? 0);

            if (correspondingCC?.ScheduleSupportData?.SchedulesMetadataAllowed ?? false)
            {
                rpt.properties2.metadataLength = (byte)(schedule?.MetaData?.Length ?? 0);
                rpt.metadata = rpt.properties2.metadataLength > 0 ? schedule?.MetaData : null;
            }

            return rpt;
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet,
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != CommandClassId)
            {
                supervisionReport.status = (byte)SvStatusNames.NoSupport;
                return;
            }

            supervisionReport.status = (byte)SvStatusNames.Fail;  // FAIL as default
            switch (supervisionGet.encapsulatedCommand[1])
            {
                case COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_ENABLE_SET.ID:
                case COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_DAILY_REPEATING_SCHEDULE_SET.ID:
                case COMMAND_CLASS_ACTIVE_SCHEDULE.ACTIVE_SCHEDULE_YEAR_DAY_SCHEDULE_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte)SvStatusNames.Success;
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }


        public override void UpdateSupportData(CommandClassSupportData data)
        {
            // Nothing to do - all properties are tied to other CC implementations by implementing IActiveSchedule* interfaces.
        }
    }

    public class ActiveScheduleSupportData : CommandClassSupportData
    {
        // dummy class to repesent this CC within EDE's Edit Command Classes List
    }

    public class ActiveSchedule
    {
        public int NestedId { get; set; }

        public UInt16 Slot { get; set; }

        [CustomDataParameter(1, "Meta Data", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.HexByteArray, EditType.Config | EditType.Edit)]
        public byte[] MetaData { get; set; }

        public virtual bool IsActive(DateTime now)
        {
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override bool Equals(object other)
        {
            if (other is ActiveSchedule otherSchedule)
            {
                if (Slot != otherSchedule.Slot) return false;
                if ((MetaData == null) != (otherSchedule.MetaData == null)) return false;
                if (MetaData != null && otherSchedule.MetaData != null)
                {
                    if (MetaData.Length != otherSchedule.MetaData.Length) return false;
                    for (int i = 0; i < this.MetaData.Length; i++)
                    {
                        if (MetaData[i] != otherSchedule.MetaData[i]) return false;
                    }
                }

                return true;
            }

            return false;
        }
    }

    public class ActiveYearDaySchedule : ActiveSchedule, INestedItem
    {
        [CustomDataParameter(1, "Start Date", CommandClassesImpl.UiControl.DateTimePicker, CommandClassesImpl.InputType.none, EditType.Config | EditType.Edit)]
        public DateTime StartDate { get; set; }

        [CustomDataParameter(1, "End Date", CommandClassesImpl.UiControl.DateTimePicker, CommandClassesImpl.InputType.none, EditType.Config | EditType.Edit)]
        public DateTime? EndDate { get; set; }

        public override bool IsActive(DateTime now)
        {
            // CC:0083.02.00.11.024: zeroed stop parameters — active in perpetuity after start.
            // Currently only User Credential is supported as a Target CC; additional targets
            // may define a different interpretation of a null EndDate.
            return StartDate <= now && (EndDate == null || EndDate >= now);
        }

        public override string ToString()
        {
            return $"Year Day ({Slot}): From {StartDate:yyyy-MM-dd HH:mm} to {EndDate:yyyy-MM-dd HH:mm}" + 
                $" [Metadata: {string.Join(", ", (MetaData ?? Array.Empty<byte>()).Select(x => x.ToString("X2")))}]";
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object other)
        {
            if (other is ActiveYearDaySchedule otherSchedule && base.Equals(other))
            {
                if (StartDate != otherSchedule.StartDate) return false;
                if (EndDate != otherSchedule.EndDate) return false;

                return true;
            }

            return false;
        }
    }

    public class ActiveDailyRepeatingSchedule : ActiveSchedule, INestedItem
    {
        [ParameterSeparator(1, "Days of Week")]
        [CustomDataParameter(1, "Week Days", CommandClassesImpl.UiControl.ListCheckBox, CommandClassesImpl.InputType.Enum, EditType.Config | EditType.Edit)]
        public List<DaysOfWeek> DaysOfWeek { get; set; } = new List<DaysOfWeek>();

        [CustomDataParameter(1, "Start Hour", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Int_0_24, EditType.Config | EditType.Edit)]
        public byte StartHour { get; set; }

        [CustomDataParameter(1, "Start Minute", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Int_0_59, EditType.Config | EditType.Edit)]
        public byte StartMinute { get; set; }

        [CustomDataParameter(1, "Duration Hour", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Int_0_24, EditType.Config | EditType.Edit)]
        public byte DurationHour { get; set; }

        [CustomDataParameter(1, "Duration Minute", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Int_0_59, EditType.Config | EditType.Edit)]
        public byte DurationMinute { get; set; }

        public override bool IsActive(DateTime now)
        {
            int minuteOfToday = now.Hour * 60 + now.Minute;

            int minuteOfStartDay = StartHour * 60 + StartMinute;
            int timeFenceLengthInMinutes = DurationHour * 60 + DurationMinute;

            // max DurationHour is 23 and max DurationMinute is 59, so maximum spanned days is 1
            bool isSpanningAcrossDays = (minuteOfStartDay + timeFenceLengthInMinutes) > 24 * 60;

            if (isSpanningAcrossDays == false)
            {
                if (DaysOfWeek.AnyMatches(now) && minuteOfToday >= minuteOfStartDay && minuteOfToday <= minuteOfStartDay + timeFenceLengthInMinutes)
                {
                    return true;
                }
            }
            else
            {
                if (DaysOfWeek.AnyMatches(now) && minuteOfToday >= minuteOfStartDay ||
                    DaysOfWeek.AnyMatches(now.AddDays(-1)) && minuteOfToday <= (minuteOfStartDay + timeFenceLengthInMinutes) % (24 * 60))
                {
                    return true;
                }
            }

            return false;
        }

        public override string ToString()
        {
            string days = string.Join(", ", DaysOfWeek);
            return $"Daily Repeating ({Slot}): From {StartHour:D2}:{StartMinute:D2} for: {DurationHour:D2}:{DurationMinute:D2} on {days}" +
                $" [Metadata: {string.Join(", ", (MetaData ?? Array.Empty<byte>()).Select(x => x.ToString("X2")))}]";
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object other)
        {
            if (other is ActiveDailyRepeatingSchedule otherSchedule && base.Equals(other))
            {
                if (StartHour != otherSchedule.StartHour) return false;
                if (StartMinute != otherSchedule.StartMinute) return false;
                if (DurationHour != otherSchedule.DurationHour) return false;
                if (DurationMinute != otherSchedule.DurationMinute) return false;
                if (!DaysOfWeek.ToHashSet().SetEquals(otherSchedule.DaysOfWeek)) return false;
                return true;
            }

            return false;
        }
    }

    public enum  DaysOfWeek 
    {
        [Description("0x01 Sunday")]
        Sunday = 1,
        [Description("0x02 Monday")]
        Monday = 2,
        [Description("0x04 Tuesday")]
        Tuesday = 4,
        [Description("0x08 Wednesday")]
        Wednesday = 8,
        [Description("0x10 Thursday")]
        Thursday = 16,
        [Description("0x20 Friday")]
        Friday = 32,
        [Description("0x40 Saturday")]
        Saturday = 64
    }

    public enum ActiveScheduleSetAction
    {
        Erase = 0,
        Modify = 1,
    }

    public enum ActiveScheduleReportCode
    {
        ResponseToGet = 0,
        SchedulesModifiedExternally = 1,
        ScheduleModifiedZWave = 2,
    }

    public static class ActiveScheduleExtensions
    {
        public static bool AreAllYearDayStopParametersZero(
            UInt16 stopYear, byte stopMonth, byte stopDay, byte stopHour, byte stopMinute)
        {
            return stopYear == 0 && stopMonth == 0 && stopDay == 0 && stopHour == 0 && stopMinute == 0;
        }

        public static bool TryParseYearDayScheduleTimestamp(
            UInt16 year, byte month, byte day, byte hour, byte minute, out DateTime result)
        {
            result = default;

            if (year == 0 || month < 1 || month > 12 || day < 1 || day > 31 || hour > 23 || minute > 59)
            {
                return false;
            }

            try
            {
                result = new DateTime(year, month, day, hour, minute, 0);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        public static IActiveScheduleTarget GetTarget(this IEnumerable<IActiveScheduleSupport> scheduleSupportingCCs, byte targetCc, UInt16 targetId)
        {
            var correspondingCC = scheduleSupportingCCs.FirstOrDefault(x => x.TargetCC == targetCc);
            if (correspondingCC == null) return null; // Target CC not supported
            return correspondingCC.GetTarget(targetId);
        }

        public static IActiveScheduleTarget GetTarget(this IActiveScheduleSupport scheduleSupportingCC, UInt16 targetId)
        {
            if (scheduleSupportingCC == null) return null;
            IActiveScheduleTarget target = scheduleSupportingCC.ScheduleSupportData.GetTarget(targetId);
            return target;
        }

        public static IActiveScheduleTarget GetTarget(this IActiveScheduleSupportData scheduleSupportData, UInt16 targetId)
        {
            if (scheduleSupportData == null) return null;
            if (targetId >= scheduleSupportData.NumberOfSupportedTargets) return null; // Target ID out of range
            IActiveScheduleTarget target = scheduleSupportData.Targets.Find(t => t.TargetId == targetId);
            return target;
        }

        public static UInt16 GetNextScheduleSlot(this IEnumerable<ActiveSchedule> schedules, UInt16 currentSlot)
        {
            if (schedules == null || !schedules.Any()) return 0;

            var remainingSlots = schedules.Where(s => s.Slot > currentSlot);
            var nextSchedule = remainingSlots.OrderBy(s => s.Slot).FirstOrDefault();
            return nextSchedule?.Slot ?? 0;
        }

        public static List<DaysOfWeek> GetDaysOfWeekFromBitmask(this byte bitmask)
        {
            List<DaysOfWeek> days = new List<DaysOfWeek>();
            foreach (DaysOfWeek day in Enum.GetValues(typeof(DaysOfWeek)))
            {
                if ((bitmask & (byte)day) != 0)
                {
                    days.Add(day);
                }
            }
            return days;
        }

        public static bool IsAnyActive(this IEnumerable<ActiveSchedule> schedules, DateTime now)
        {
            foreach (var schedule in schedules)
            {
                if (schedule.IsActive(now))
                    return true;
            }
            return false;
        }

        public static bool AnyMatches(this List<DaysOfWeek> daysOfWeek, DateTime now)
        {
            return daysOfWeek.Any(d => d.Matches(now.DayOfWeek));
        }

        public static bool Matches(this DaysOfWeek activeScheduleWeekDay, DayOfWeek weekDay)
        {
            return activeScheduleWeekDay.ConvertToSystemWeekDay() == weekDay;
        }

        public static DayOfWeek ConvertToSystemWeekDay (this DaysOfWeek activeScheduleWeekDay)
        {
            switch (activeScheduleWeekDay)
            {
                case DaysOfWeek.Sunday:
                    return DayOfWeek.Sunday;
                case DaysOfWeek.Monday:
                    return DayOfWeek.Monday;
                case DaysOfWeek.Tuesday:
                    return DayOfWeek.Tuesday;
                case DaysOfWeek.Wednesday:
                    return DayOfWeek.Wednesday;
                case DaysOfWeek.Thursday:
                    return DayOfWeek.Thursday;
                case DaysOfWeek.Friday:
                    return DayOfWeek.Friday;
                case DaysOfWeek.Saturday:
                    return DayOfWeek.Saturday;
                default:
                    return DayOfWeek.Sunday;
            }
        }

        public static DaysOfWeek ConvertToActiveScheduleWeekDay(this DayOfWeek systemWeekDay)
        {
            switch (systemWeekDay)
            {
                case DayOfWeek.Sunday:
                    return DaysOfWeek.Sunday;
                case DayOfWeek.Monday:
                    return DaysOfWeek.Monday;
                case DayOfWeek.Tuesday:
                    return DaysOfWeek.Tuesday;
                case DayOfWeek.Wednesday:
                    return DaysOfWeek.Wednesday;
                case DayOfWeek.Thursday:
                    return DaysOfWeek.Thursday;
                case DayOfWeek.Friday:
                    return DaysOfWeek.Friday;
                case DayOfWeek.Saturday:
                    return DaysOfWeek.Saturday;
                default:
                    return DaysOfWeek.Sunday;
            }
        }
    }
}
