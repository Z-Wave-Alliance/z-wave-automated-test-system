using System;
using System.Collections.Generic;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.Devices;

namespace Zats.Services
{
    [CommandClassName("Schedule Entry Lock Command Class")]
    [CommandClassId(COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V4.ID)]
    [MinVersion(1)]
    [MaxVersion(4)]

    public class ScheduleEntryLockSupport : CommandClassSupport
    {
        private ScheduleEntryLockSupportData scheduleEntryLockSupportData;
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return scheduleEntryLockSupportData; }
            set { scheduleEntryLockSupportData = (ScheduleEntryLockSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public ScheduleEntryLockSupport()
            : this(new ScheduleEntryLockSupportData() { })
        { }

        public ScheduleEntryLockSupport(ScheduleEntryLockSupportData scheduleEntryLockSupportData)
            : base(COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V4.ID)
        {
            this.scheduleEntryLockSupportData = scheduleEntryLockSupportData;
        }

        protected override void OnHandledDelayed(DataReceivedUnit ou)
        {
            ou.SetNextActionItems();

            if (MatchesDestinationEndPoint())
            {
                /*if (UserCodeSupportData.Users.Count == 0)
                {
                    InitializeUsersForCTT();
                    InitializeCapabilitiesForCTT();
                }*/
                var sendDstNode = new NodeTag(ReceivedAchData.DstNode.Id, DstNode.EndPointId);
                var sendSrcNode = ReceivedAchData.SrcNode;
                byte[] command = ReceivedAchData.Command;
                var scheme = (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme;
                bool isHandlingAllowed = IsHandlingAllowed(Network, command, scheme, Secure);

                ResultForSv = SvResultNames.Undefined;
                if (isHandlingAllowed && command != null && command.Length > 1 && scheduleEntryLockSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_SCHEDULE_ENTRY_LOCK.SCHEDULE_ENTRY_LOCK_ENABLE_SET.ID)
                    {
                        ResultForSv = HandleScheduleEntryLockEnableSet_V1(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SCHEDULE_ENTRY_LOCK.SCHEDULE_ENTRY_LOCK_ENABLE_ALL_SET.ID)
                    {
                        HandleScheduleEntryLockEnableAllSet(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SCHEDULE_ENTRY_LOCK.SCHEDULE_ENTRY_TYPE_SUPPORTED_GET.ID && !IsSupervised())
                    {
                        if (Version < 3)
                        {
                            rpt = PrepareScheduleEntryLockSupportedReport_V1();
                        }
                        else
                        {
                            rpt = PrepareScheduleEntryLockSupportedReport_V3();
                        }
                    }
                    else if (command[1] == COMMAND_CLASS_SCHEDULE_ENTRY_LOCK.SCHEDULE_ENTRY_LOCK_WEEK_DAY_SET.ID)
                    {
                        ResultForSv = HandleScheduleEntryLockWeekDaySet(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SCHEDULE_ENTRY_LOCK.SCHEDULE_ENTRY_LOCK_WEEK_DAY_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareScheduleEntryLockWeekDayReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SCHEDULE_ENTRY_LOCK.SCHEDULE_ENTRY_LOCK_YEAR_DAY_SET.ID)
                    {
                        ResultForSv = HandleScheduleEntryLockYearDaySet(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SCHEDULE_ENTRY_LOCK.SCHEDULE_ENTRY_LOCK_YEAR_DAY_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareScheduleEntryLockYearDayReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V2.SCHEDULE_ENTRY_LOCK_TIME_OFFSET_SET.ID)
                    {
                        HandleScheduleEntryLockTimeOffsetSet(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V2.SCHEDULE_ENTRY_LOCK_TIME_OFFSET_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareScheduleEntryLockTimeOffsetReport();
                    }
                    else if (command[1] == COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V3.SCHEDULE_ENTRY_LOCK_DAILY_REPEATING_SET.ID && Version > 2)
                    {
                        ResultForSv = HandleScheduleEntryLockDailyRepeatingSet(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V3.SCHEDULE_ENTRY_LOCK_DAILY_REPEATING_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareScheduleEntryLockDailyRepeatingReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V4.EXTENDED_SCHEDULE_ENTRY_LOCK_ENABLE_SET.ID)
                    {
                        ResultForSv = HandleExtendedScheduleEntryLockEnableSet_V4(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V4.EXTENDED_SCHEDULE_ENTRY_LOCK_WEEK_DAY_SCHEDULE_SET.ID)
                    {
                        ResultForSv = HandleExtendedScheduleEntryLockWeekDaySet(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V4.EXTENDED_SCHEDULE_ENTRY_LOCK_WEEK_DAY_SCHEDULE_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareExtendedScheduleEntryLockWeekDayReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V4.EXTENDED_SCHEDULE_ENTRY_LOCK_YEAR_DAY_SCHEDULE_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareExtendedScheduleEntryLockYearDayReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V4.EXTENDED_SCHEDULE_ENTRY_LOCK_YEAR_DAY_SCHEDULE_SET.ID)
                    {
                        ResultForSv = HandleExtendedScheduleEntryLockYearDaySet(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V4.EXTENDED_SCHEDULE_ENTRY_LOCK_DAILY_REPEATING_SET.ID)
                    {
                        ResultForSv = HandleExtendedScheduleEntryLockDailyRepeatingSet(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V4.EXTENDED_SCHEDULE_ENTRY_LOCK_DAILY_REPEATING_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareExtendedScheduleEntryLockDailyRepeatingReport(command);
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

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet,
                                                  COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != CommandClassId)
            {
                supervisionReport.status = (byte)SvStatusNames.NoSupport;
                return;
            }

            supervisionReport.status = (byte)SvStatusNames.Fail;  // FAIL as default
            //switch (supervisionGet.encapsulatedCommand[1])
            //{
            //    case COMMAND_CLASS_USER_CODE_V2.USER_CODE_SET.ID:
            //        if (ResultForSv == SvResultNames.Successful)
            //        {
            //            supervisionReport.status = (byte)SvStatusNames.Success;
            //        }
            //        break;
            //    case COMMAND_CLASS_USER_CODE_V2.EXTENDED_USER_CODE_SET.ID:
            //        if (ResultForSv == SvResultNames.Successful)
            //        {
            //            supervisionReport.status = (byte)SvStatusNames.Success;
            //        }
            //        break;
            //    case COMMAND_CLASS_USER_CODE_V2.ADMIN_CODE_SET.ID:
            //        if (ResultForSv == SvResultNames.Successful)
            //        {
            //            supervisionReport.status = (byte)SvStatusNames.Success;
            //        }
            //        break;
            //    case COMMAND_CLASS_USER_CODE_V2.USER_CODE_KEYPAD_MODE_SET.ID:
            //        if (ResultForSv == SvResultNames.Successful)
            //        {
            //            supervisionReport.status = (byte)SvStatusNames.Success;
            //        }
            //        break;
            //}
            ResultForSv = SvResultNames.Undefined;
        }



        private SvResultNames HandleScheduleEntryLockEnableSet_V1(COMMAND_CLASS_SCHEDULE_ENTRY_LOCK.SCHEDULE_ENTRY_LOCK_ENABLE_SET cmd)
        {
            var resultForSv = SvResultNames.Failed;

            var scheduleSlot = scheduleEntryLockSupportData.ScheduleSlots.Find(x => x.UserIdentifier == cmd.userIdentifier);

            if (scheduleSlot != null)
            {
                scheduleSlot.Enabled = cmd.enabled.Value == 0x01;
                resultForSv = SvResultNames.Successful;
            }

            return resultForSv;
        }

        private SvResultNames HandleExtendedScheduleEntryLockEnableSet_V4(COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V4.EXTENDED_SCHEDULE_ENTRY_LOCK_ENABLE_SET cmd)
        {
            var resultForSv = SvResultNames.Failed;

            var userId = (cmd.userIdentifierMsb.Value << 8) | cmd.userIdentifierLsb;

            var scheduleSlot = scheduleEntryLockSupportData.ScheduleSlots.Find(x => x.UserIdentifier == userId);

            if (scheduleSlot != null)
            {
                scheduleSlot.Enabled = cmd.properties1.enabled == 0x01;
                resultForSv = SvResultNames.Successful;
            }

            return resultForSv;
        }

        private void HandleScheduleEntryLockEnableAllSet(COMMAND_CLASS_SCHEDULE_ENTRY_LOCK.SCHEDULE_ENTRY_LOCK_ENABLE_ALL_SET cmd)
        {
            scheduleEntryLockSupportData.ScheduleSlots.ForEach(x => x.Enabled = cmd.enabled.Value == 0x01);
        }


        private byte[] PrepareScheduleEntryLockSupportedReport_V1()
        {
            return new COMMAND_CLASS_SCHEDULE_ENTRY_LOCK.SCHEDULE_ENTRY_TYPE_SUPPORTED_REPORT()
            {
                numberOfSlotsWeekDay = scheduleEntryLockSupportData.SupportedNumberOfSlotsWeekDay,
                numberOfSlotsYearDay = scheduleEntryLockSupportData.SupportedNumberOfSlotsYearDay
            };
        }

        private byte[] PrepareScheduleEntryLockSupportedReport_V3()
        {
            return new COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V3.SCHEDULE_ENTRY_TYPE_SUPPORTED_REPORT()
            {
                numberOfSlotsWeekDay = scheduleEntryLockSupportData.SupportedNumberOfSlotsWeekDay,
                numberOfSlotsYearDay = scheduleEntryLockSupportData.SupportedNumberOfSlotsYearDay,
                numberOfSlotsDailyRepeating = scheduleEntryLockSupportData.SupportedNumberOfSlotsDailyRepeating
            };
        }


        private SvResultNames HandleScheduleEntryLockWeekDaySet(COMMAND_CLASS_SCHEDULE_ENTRY_LOCK.SCHEDULE_ENTRY_LOCK_WEEK_DAY_SET cmd)
        {
            var schedule = new WeekDaySchedule()
            {
                Enabled = true,
                UserIdentifier = cmd.userIdentifier,
                SlotId = cmd.scheduleSlotId,
                DayOfWeek = cmd.dayOfWeek,
                StartHour = cmd.startHour,
                StartMinute = cmd.startMinute,
                StopHour = cmd.stopHour,
                StopMinute = cmd.stopMinute
            };

            return HandleScheduleEntryLockWeekDaySet((ScheduleSetAction)cmd.setAction.Value, schedule);
        }

        private SvResultNames HandleExtendedScheduleEntryLockWeekDaySet(COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V4.EXTENDED_SCHEDULE_ENTRY_LOCK_WEEK_DAY_SCHEDULE_SET cmd)
        {
            var schedule = new WeekDaySchedule()
            {
                Enabled = true,
                UserIdentifier = (ushort)(cmd.userIdentifierMsb << 8 | cmd.userIdentifierLsb),
                SlotId = cmd.scheduleSlotId,
                DayOfWeek = cmd.dayOfWeek,
                StartHour = cmd.startHour,
                StartMinute = cmd.startMinute,
                StopHour = cmd.stopHour,
                StopMinute = cmd.stopMinute
            };

            return HandleScheduleEntryLockWeekDaySet((ScheduleSetAction)cmd.setAction.Value, schedule);
        }

        private SvResultNames HandleScheduleEntryLockWeekDaySet(ScheduleSetAction setAction, WeekDaySchedule schedule)
        {
            if (schedule.SlotId > scheduleEntryLockSupportData.SupportedNumberOfSlotsWeekDay)
            {
                return SvResultNames.Failed;
            }

            if (schedule.IsValid() == false && setAction == ScheduleSetAction.Modify)
            {
                return SvResultNames.Failed;
            }

            if (scheduleEntryLockSupportData.ScheduleSlots.Find(x => x.UserIdentifier == schedule.UserIdentifier && x.SlotId == schedule.SlotId && x is WeekDaySchedule) is WeekDaySchedule existingSchedule)
            {
                scheduleEntryLockSupportData.ScheduleSlots.Remove(existingSchedule);
            }

            if (setAction == ScheduleSetAction.Modify)
            {
                scheduleEntryLockSupportData.ScheduleSlots.Add(schedule);
            }

            return SvResultNames.Successful;
        }


        private byte[] PrepareScheduleEntryLockWeekDayReport(COMMAND_CLASS_SCHEDULE_ENTRY_LOCK.SCHEDULE_ENTRY_LOCK_WEEK_DAY_GET cmd)
        {
            if (scheduleEntryLockSupportData.ScheduleSlots.Find(x => x.UserIdentifier == cmd.userIdentifier && x.SlotId == cmd.scheduleSlotId && x is WeekDaySchedule) is WeekDaySchedule scheduleSlot)
            {
                return new COMMAND_CLASS_SCHEDULE_ENTRY_LOCK.SCHEDULE_ENTRY_LOCK_WEEK_DAY_REPORT()
                {
                    userIdentifier = (byte)scheduleSlot.UserIdentifier,
                    scheduleSlotId = (byte)scheduleSlot.SlotId,
                    dayOfWeek = (byte)scheduleSlot.DayOfWeek,
                    startHour = (byte)scheduleSlot.StartHour,
                    startMinute = (byte)scheduleSlot.StartMinute,
                    stopHour = (byte)scheduleSlot.StopHour,
                    stopMinute = (byte)scheduleSlot.StopMinute
                };
            }
            else
            {
                return new COMMAND_CLASS_SCHEDULE_ENTRY_LOCK.SCHEDULE_ENTRY_LOCK_WEEK_DAY_REPORT()
                {
                    userIdentifier = cmd.userIdentifier,
                    scheduleSlotId = cmd.scheduleSlotId,
                    dayOfWeek = 0xFF,
                    startHour = 0xFF,
                    startMinute = 0xFF,
                    stopHour = 0xFF,
                    stopMinute = 0xFF,
                };
            }
        }

        private byte[] PrepareExtendedScheduleEntryLockWeekDayReport(COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V4.EXTENDED_SCHEDULE_ENTRY_LOCK_WEEK_DAY_SCHEDULE_GET cmd)
        {
            ushort userId = (ushort)(cmd.userIdentifierMsb << 8 | cmd.userIdentifierLsb);

            if (scheduleEntryLockSupportData.ScheduleSlots.Find(x => x.UserIdentifier == userId && x.SlotId == cmd.scheduleSlotId && x is WeekDaySchedule) is WeekDaySchedule scheduleSlot)
            {
                return new COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V4.EXTENDED_SCHEDULE_ENTRY_LOCK_WEEK_DAY_SCHEDULE_REPORT()
                {
                    userIdentifierMsb = (byte)((scheduleSlot.UserIdentifier >> 8) & 0xFF),
                    userIdentifierLsb = (byte)(scheduleSlot.UserIdentifier & 0xFF),
                    scheduleSlotId = (byte)scheduleSlot.SlotId,
                    dayOfWeek = (byte)scheduleSlot.DayOfWeek,
                    startHour = (byte)scheduleSlot.StartHour,
                    startMinute = (byte)scheduleSlot.StartMinute,
                    stopHour = (byte)scheduleSlot.StopHour,
                    stopMinute = (byte)scheduleSlot.StopMinute
                }; ;
            }
            else
            {
                return new COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V4.EXTENDED_SCHEDULE_ENTRY_LOCK_WEEK_DAY_SCHEDULE_REPORT()
                {
                    userIdentifierMsb = cmd.userIdentifierMsb,
                    userIdentifierLsb = cmd.userIdentifierLsb,
                    scheduleSlotId = cmd.scheduleSlotId,
                    dayOfWeek = 0xFF,
                    startHour = 0xFF,
                    startMinute = 0xFF,
                    stopHour = 0xFF,
                    stopMinute = 0xFF,
                };
            }
        }



        private byte[] PrepareScheduleEntryLockYearDayReport(COMMAND_CLASS_SCHEDULE_ENTRY_LOCK.SCHEDULE_ENTRY_LOCK_YEAR_DAY_GET cmd)
        {
            if (scheduleEntryLockSupportData.ScheduleSlots.Find(x => x.UserIdentifier == cmd.userIdentifier && x.SlotId == cmd.scheduleSlotId && x is YearDaySchedule) is YearDaySchedule scheduleSlot)
            {
                return new COMMAND_CLASS_SCHEDULE_ENTRY_LOCK.SCHEDULE_ENTRY_LOCK_YEAR_DAY_REPORT()
                {
                    userIdentifier = (byte)scheduleSlot.UserIdentifier,
                    scheduleSlotId = (byte)scheduleSlot.SlotId,
                    startYear = (byte)scheduleSlot.StartYear,
                    startMonth = (byte)scheduleSlot.StartMonth,
                    startDay = (byte)scheduleSlot.StartDay,
                    startHour = (byte)scheduleSlot.StartHour,
                    startMinute = (byte)scheduleSlot.StartMinute,
                    stopYear = (byte)scheduleSlot.StopYear,
                    stopMonth = (byte)scheduleSlot.StopMonth,
                    stopDay = (byte)scheduleSlot.StopDay,
                    stopHour = (byte)scheduleSlot.StopHour,
                    stopMinute = (byte)scheduleSlot.StopMinute
                };
            }
            else
            {
                return new COMMAND_CLASS_SCHEDULE_ENTRY_LOCK.SCHEDULE_ENTRY_LOCK_YEAR_DAY_REPORT()
                {
                    userIdentifier = cmd.userIdentifier,
                    scheduleSlotId = cmd.scheduleSlotId,
                    startYear = 0xFF,
                    startMonth = 0xFF,
                    startDay = 0xFF,
                    startHour = 0xFF,
                    startMinute = 0xFF,
                    stopYear = 0xFF,
                    stopMonth = 0xFF,
                    stopDay = 0xFF,
                    stopHour = 0xFF,
                    stopMinute = 0xFF,
                };
            }
        }

        private byte[] PrepareExtendedScheduleEntryLockYearDayReport(COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V4.EXTENDED_SCHEDULE_ENTRY_LOCK_YEAR_DAY_SCHEDULE_GET cmd)
        {
            ushort userId = (ushort)(cmd.userIdentifierMsb << 8 | cmd.userIdentifierLsb);

            if (scheduleEntryLockSupportData.ScheduleSlots.Find(x => x.UserIdentifier == userId && x.SlotId == cmd.scheduleSlotId && x is YearDaySchedule) is YearDaySchedule scheduleSlot)
            {
                return new COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V4.EXTENDED_SCHEDULE_ENTRY_LOCK_YEAR_DAY_SCHEDULE_REPORT()
                {
                    userIdentifierMsb = (byte)((scheduleSlot.UserIdentifier >> 8) & 0xFF),
                    userIdentifierLsb = (byte)(scheduleSlot.UserIdentifier & 0xFF),
                    scheduleSlotId = scheduleSlot.SlotId,
                    startYear = scheduleSlot.StartYear,
                    startMonth = scheduleSlot.StartMonth,
                    startDay = scheduleSlot.StartDay,
                    startHour = scheduleSlot.StartHour,
                    startMinute = scheduleSlot.StartMinute,
                    stopYear = scheduleSlot.StopYear,
                    stopMonth = scheduleSlot.StopMonth,
                    stopDay = scheduleSlot.StopDay,
                    stopHour = scheduleSlot.StopHour,
                    stopMinute = scheduleSlot.StopMinute
                };
            }
            else
            {
                return new COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V4.EXTENDED_SCHEDULE_ENTRY_LOCK_YEAR_DAY_SCHEDULE_REPORT()
                {
                    userIdentifierMsb = cmd.userIdentifierMsb,
                    userIdentifierLsb = cmd.userIdentifierLsb,
                    scheduleSlotId = cmd.scheduleSlotId,
                    startYear = 0xFF,
                    startMonth = 0xFF,
                    startDay = 0xFF,
                    startHour = 0xFF,
                    startMinute = 0xFF,
                    stopYear = 0xFF,
                    stopMonth = 0xFF,
                    stopDay = 0xFF,
                    stopHour = 0xFF,
                    stopMinute = 0xFF,
                };
            }
        }


        private SvResultNames HandleScheduleEntryLockYearDaySet(COMMAND_CLASS_SCHEDULE_ENTRY_LOCK.SCHEDULE_ENTRY_LOCK_YEAR_DAY_SET cmd)
        {
            var schedule = new YearDaySchedule()
            {
                Enabled = true,
                UserIdentifier = cmd.userIdentifier,
                SlotId = cmd.scheduleSlotId,
                StartYear = cmd.startYear,
                StartMonth = cmd.startMonth,
                StartDay = cmd.startDay,
                StartHour = cmd.startHour,
                StartMinute = cmd.startMinute,
                StopYear = cmd.stopYear,
                StopMonth = cmd.stopMonth,
                StopDay = cmd.stopDay,
                StopHour = cmd.stopHour,
                StopMinute = cmd.stopMinute,
            };

            return HandleScheduleEntryLockYearDaySet((ScheduleSetAction)cmd.setAction.Value, schedule);
        }

        private SvResultNames HandleExtendedScheduleEntryLockYearDaySet(COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V4.EXTENDED_SCHEDULE_ENTRY_LOCK_YEAR_DAY_SCHEDULE_SET cmd)
        {
            ushort userId = (ushort)(cmd.userIdentifierMsb << 8 | cmd.userIdentifierLsb);

            var schedule = new YearDaySchedule()
            {
                Enabled = true,
                UserIdentifier = userId,
                SlotId = cmd.scheduleSlotId,
                StartYear = cmd.startYear,
                StartMonth = cmd.startMonth,
                StartDay = cmd.startDay,
                StartHour = cmd.startHour,
                StartMinute = cmd.startMinute,
                StopYear = cmd.stopYear,
                StopMonth = cmd.stopMonth,
                StopDay = cmd.stopDay,
                StopHour = cmd.stopHour,
                StopMinute = cmd.stopMinute,
            };

            return HandleScheduleEntryLockYearDaySet((ScheduleSetAction)cmd.setAction.Value, schedule);
        }

        private SvResultNames HandleScheduleEntryLockYearDaySet(ScheduleSetAction setAction, YearDaySchedule schedule)
        {
            if (schedule.SlotId > scheduleEntryLockSupportData.SupportedNumberOfSlotsYearDay)
            {
                return SvResultNames.Failed;
            }

            if (schedule.IsValid() == false && setAction == ScheduleSetAction.Modify)
            {
                return SvResultNames.Failed;
            }

            if (scheduleEntryLockSupportData.ScheduleSlots.Find(x => x.UserIdentifier == schedule.UserIdentifier && x.SlotId == schedule.SlotId && x is YearDaySchedule) is YearDaySchedule existingSchedule)
            {
                scheduleEntryLockSupportData.ScheduleSlots.Remove(existingSchedule);
            }

            if (setAction == ScheduleSetAction.Modify)
            {
                scheduleEntryLockSupportData.ScheduleSlots.Add(schedule);
            }

            return SvResultNames.Successful;
        }


        private void HandleScheduleEntryLockTimeOffsetSet(COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V2.SCHEDULE_ENTRY_LOCK_TIME_OFFSET_SET cmd)
        {
            scheduleEntryLockSupportData.HourTZO = (sbyte)((cmd.properties1.signTzo == 0x01 ? -1 : 1) * cmd.properties1.hourTzo);
            scheduleEntryLockSupportData.MinuteTZO = cmd.minuteTzo;
            scheduleEntryLockSupportData.MinuteOffsetDST = (sbyte)((cmd.properties2.signOffsetDst == 0x01 ? -1 : 1) * cmd.properties2.minuteOffsetDst);
        }

        private byte[] PrepareScheduleEntryLockTimeOffsetReport()
        {
            var rpt = new COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V2.SCHEDULE_ENTRY_LOCK_TIME_OFFSET_REPORT()
            {
                minuteTzo = scheduleEntryLockSupportData.MinuteTZO
            };
            rpt.properties1.signTzo = (byte)(scheduleEntryLockSupportData.HourTZO < 0 ? 0x01 : 0x00);
            rpt.properties1.hourTzo = (byte)Math.Abs(scheduleEntryLockSupportData.HourTZO);
            rpt.properties2.signOffsetDst = (byte)(scheduleEntryLockSupportData.MinuteOffsetDST < 0 ? 0x01 : 0x00);
            rpt.properties2.minuteOffsetDst = (byte)Math.Abs(scheduleEntryLockSupportData.MinuteOffsetDST);

            return rpt;
        }


        private SvResultNames HandleScheduleEntryLockDailyRepeatingSet(COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V3.SCHEDULE_ENTRY_LOCK_DAILY_REPEATING_SET cmd)
        {
            var schedule = new DailyRepeatingSchedule()
            {
                Enabled = true,
                UserIdentifier = cmd.userIdentifier,
                SlotId = cmd.scheduleSlotId,
                WeekDayBitmask = (ScheduleWeekDayBitmask)cmd.weekDayBitmask.Value,
                StartHour = cmd.startHour,
                StartMinute = cmd.startMinute,
                DurationHour = cmd.durationHour,
                DurationMinute = cmd.durationMinute
            };

            return HandleScheduleEntryLockDailyRepeatingSet((ScheduleSetAction)cmd.setAction.Value, schedule);
        }

        private SvResultNames HandleExtendedScheduleEntryLockDailyRepeatingSet(COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V4.EXTENDED_SCHEDULE_ENTRY_LOCK_DAILY_REPEATING_SET cmd)
        {
            ushort userId = (ushort)(cmd.userIdentifierMsb << 8 | cmd.userIdentifierLsb);

            var schedule = new DailyRepeatingSchedule()
            {
                Enabled = true,
                UserIdentifier = userId,
                SlotId = cmd.scheduleSlotId,
                WeekDayBitmask = (ScheduleWeekDayBitmask)cmd.weekDayBitmask.Value,
                StartHour = cmd.startHour,
                StartMinute = cmd.startMinute,
                DurationHour = cmd.durationHour,
                DurationMinute = cmd.durationMinute
            };

            return HandleScheduleEntryLockDailyRepeatingSet((ScheduleSetAction)cmd.setAction.Value, schedule);
        }

        private SvResultNames HandleScheduleEntryLockDailyRepeatingSet(ScheduleSetAction setAction, DailyRepeatingSchedule schedule)
        {
            if (schedule.SlotId > scheduleEntryLockSupportData.SupportedNumberOfSlotsDailyRepeating)
            {
                return SvResultNames.Failed;
            }

            if (schedule.IsValid() == false && setAction == ScheduleSetAction.Modify)
            {
                return SvResultNames.Failed;
            }

            if (scheduleEntryLockSupportData.ScheduleSlots.Find(x => x.UserIdentifier == schedule.UserIdentifier && x.SlotId == schedule.SlotId && x is DailyRepeatingSchedule) is DailyRepeatingSchedule existingSchedule)
            {
                scheduleEntryLockSupportData.ScheduleSlots.Remove(existingSchedule);
            }

            if (setAction == ScheduleSetAction.Modify)
            {
                scheduleEntryLockSupportData.ScheduleSlots.Add(schedule);
            }

            return SvResultNames.Successful;
        }

        private byte[] PrepareScheduleEntryLockDailyRepeatingReport(COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V3.SCHEDULE_ENTRY_LOCK_DAILY_REPEATING_GET cmd)
        {
            if (scheduleEntryLockSupportData.ScheduleSlots.Find(x => x.UserIdentifier == cmd.userIdentifier && x.SlotId == cmd.scheduleSlotId && x is DailyRepeatingSchedule) is DailyRepeatingSchedule scheduleSlot)
            {
                return new COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V3.SCHEDULE_ENTRY_LOCK_DAILY_REPEATING_REPORT()
                {
                    userIdentifier = (byte)scheduleSlot.UserIdentifier,
                    scheduleSlotId = (byte)scheduleSlot.SlotId,
                    weekDayBitmask = (byte)scheduleSlot.WeekDayBitmask,
                    startHour = (byte)scheduleSlot.StartHour,
                    startMinute = (byte)scheduleSlot.StartMinute,
                    durationHour = (byte)scheduleSlot.DurationHour,
                    durationMinute = (byte)scheduleSlot.DurationMinute
                };
            }
            else
            {
                return new COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V3.SCHEDULE_ENTRY_LOCK_DAILY_REPEATING_REPORT()
                {
                    userIdentifier = cmd.userIdentifier,
                    scheduleSlotId = cmd.scheduleSlotId,
                    weekDayBitmask = 0x00,
                    startHour = 0x00,
                    startMinute = 0x00,
                    durationHour = 0x00,
                    durationMinute = 0x00
                };
            }
        }
        private byte[] PrepareExtendedScheduleEntryLockDailyRepeatingReport(COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V4.EXTENDED_SCHEDULE_ENTRY_LOCK_DAILY_REPEATING_GET cmd)
        {
            ushort userId = (ushort)(cmd.userIdentifierMsb << 8 | cmd.userIdentifierLsb);

            if (scheduleEntryLockSupportData.ScheduleSlots.Find(x => x.UserIdentifier == userId && x.SlotId == cmd.scheduleSlotId && x is DailyRepeatingSchedule) is DailyRepeatingSchedule scheduleSlot)
            {
                return new COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V4.EXTENDED_SCHEDULE_ENTRY_LOCK_DAILY_REPEATING_REPORT()
                {
                    userIdentifierMsb = (byte)((scheduleSlot.UserIdentifier >> 8) & 0xFF),
                    userIdentifierLsb = (byte)(scheduleSlot.UserIdentifier & 0xFF),
                    scheduleSlotId = scheduleSlot.SlotId,
                    weekDayBitmask = (byte)scheduleSlot.WeekDayBitmask,
                    startHour = scheduleSlot.StartHour,
                    startMinute = scheduleSlot.StartMinute,
                    durationHour = scheduleSlot.DurationHour,
                    durationMinute = scheduleSlot.DurationMinute
                };
            }
            else
            {
                return new COMMAND_CLASS_SCHEDULE_ENTRY_LOCK_V4.EXTENDED_SCHEDULE_ENTRY_LOCK_DAILY_REPEATING_REPORT()
                {
                    userIdentifierMsb = cmd.userIdentifierMsb,
                    userIdentifierLsb = cmd.userIdentifierLsb,
                    scheduleSlotId = cmd.scheduleSlotId,
                    startHour = 0x00,
                    startMinute = 0x00,
                    durationHour = 0x00,
                    durationMinute = 0x00,
                };
            }
        }


        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is ScheduleEntryLockSupportData newData)
            {
            }
        }
    }

    public class ScheduleEntryLockSupportData : CommandClassSupportData
    {
        public DateTime FixedDateTimeUTC { get; set; } = DateTime.UtcNow;

        public bool UseFixedDateTime = false;

        public byte SupportedNumberOfSlotsWeekDay { get; set; } = 128;

        public byte SupportedNumberOfSlotsYearDay { get; set; } = 128;

        public byte SupportedNumberOfSlotsDailyRepeating { get; set; } = 128;

        public sbyte HourTZO { get; set; }
        public byte MinuteTZO { get; set; }
        public sbyte MinuteOffsetDST { get; set; }

        public List<Schedule> ScheduleSlots { get; } = new List<Schedule>();

        public bool CheckUsersScheduleForAccess(ushort uniqueUserId)
        {
            var usersSchedules = ScheduleSlots.FindAll(x => x.UserIdentifier == uniqueUserId);

            if (usersSchedules.Count == 0)
            {
                return true;
            }
            else
            {
                var currentTime = UseFixedDateTime ? FixedDateTimeUTC : DateTime.Now;

                foreach (var schedule in usersSchedules)
                {
                    if (schedule.HasUserAccess(currentTime))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

    }

    public class Schedule
    {
        public ushort UserIdentifier { get; set; }
        public byte SlotId { get; set; }

        public bool Enabled { get; set; }

        public virtual bool HasUserAccess(DateTime now)
        {
            return Enabled == false;
        }

    }

    public class WeekDaySchedule : Schedule
    {
        public byte DayOfWeek { get; set; }
        public byte StartHour { get; set; }
        public byte StartMinute { get; set; }
        public byte StopHour { get; set; }
        public byte StopMinute { get; set; }

        public bool IsValid()
        {
            return
                SlotId > 0 &&
                DayOfWeek < 7 &&
                StartHour < 24 &&
                StartMinute < 60 &&
                StopHour < 24 &&
                StopMinute < 60 &&
                GetCompareTimeStamp(StartHour, StartMinute) < GetCompareTimeStamp(StopHour, StopMinute);
        }

        public new bool HasUserAccess(DateTime now)
        {
            return base.HasUserAccess(now) || IsDateWithinTimeFence(now);

        }

        private bool IsDateWithinTimeFence(DateTime date)
        {
            var dateCompareTimeStamp = GetCompareTimeStamp(date.Hour, date.Minute);

            return DayOfWeek == (byte)date.DayOfWeek &&
                GetCompareTimeStamp(StartHour, StartMinute) <= dateCompareTimeStamp &&
                GetCompareTimeStamp(StopHour, StopMinute) > dateCompareTimeStamp;
        }

        private int GetCompareTimeStamp(int hour, int minute)
        {
            // calculate a kind of timestamp in minutes for comparing start / stop times
            return hour * 60 + minute;
        }
    }

    public class YearDaySchedule : Schedule
    {
        public byte StartYear { get; set; }
        public byte StartMonth { get; set; }
        public byte StartDay { get; set; }
        public byte StartHour { get; set; }
        public byte StartMinute { get; set; }
        public byte StopYear { get; set; }
        public byte StopMonth { get; set; }
        public byte StopDay { get; set; }
        public byte StopHour { get; set; }
        public byte StopMinute { get; set; }

        public bool IsValid()
        {
            return
                SlotId > 0 &&
                StartYear < 99 &&
                StartMonth > 0 &&
                StartMonth < 13 &&
                StartDay > 0 &&
                StartDay < 32 &&
                StartHour < 24 &&
                StartMinute < 60 &&
                StopYear <= 99 &&
                StopMonth > 0 &&
                StopMonth < 13 &&
                StopDay > 0 &&
                StopDay < 32 &&
                StopHour < 24 &&
                StopMinute < 60 &&
                GetCompareTimeStamp(StartYear, StartMonth, StartDay, StartHour, StartMinute) < GetCompareTimeStamp(StopYear, StopMonth, StopDay, StopHour, StopMinute);
        }

        public new bool HasUserAccess(DateTime now)
        {
            return base.HasUserAccess(now) || IsDateWithinTimeFence(now);

        }

        private bool IsDateWithinTimeFence(DateTime date)
        {
            var dateCompareTimeStamp = GetCompareTimeStamp(date.Year, date.Month, date.Day, date.Hour, date.Minute);

            return
                GetCompareTimeStamp(StartYear, StartMonth, StartDay, StartHour, StartMinute) <= dateCompareTimeStamp &&
                GetCompareTimeStamp(StopYear, StopMonth, StopDay, StopHour, StopMinute) > dateCompareTimeStamp;
        }

        private int GetCompareTimeStamp(int year, int month, int day, int hour, int minute)
        {
            // calculate a kind of timestamp in minutes for comparing start / stop times
            return year * 12 * 31 * 24 * 60 + month * 31 * 24 * 60 + day * 24 * 60 + hour * 60 + minute;
        }

    }

    public class DailyRepeatingSchedule : Schedule
    {
        public ScheduleWeekDayBitmask WeekDayBitmask { get; set; }
        public byte StartHour { get; set; }
        public byte StartMinute { get; set; }
        public byte DurationHour { get; set; }
        public byte DurationMinute { get; set; }

        public bool IsValid()
        {
            return
                SlotId > 0 &&
                (byte)WeekDayBitmask < 128 &&
                StartHour < 24 &&
                StartMinute < 60 &&
                DurationHour < 24 &&
                DurationMinute < 60;

        }

        public new bool HasUserAccess(DateTime now)
        {
            return base.HasUserAccess(now) || IsDateWithinTimeFence(now);

        }

        private bool IsDateWithinTimeFence(DateTime date)
        {
            var dateCompareTimeStamp = GetCompareTimeStamp(date.Hour, date.Minute);

            return ((byte)WeekDayBitmask & (0x01 << (byte)date.DayOfWeek)) > 0 &&
                GetCompareTimeStamp(StartHour, StartMinute) <= dateCompareTimeStamp &&
                GetCompareTimeStamp(StartHour + DurationHour, StartMinute + DurationMinute) > dateCompareTimeStamp;
        }

        private int GetCompareTimeStamp(int hour, int minute)
        {
            // calculate a kind of timestamp in minutes for comparing start / stop times
            return hour * 60 + minute;
        }

    }

    public enum ScheduleSetAction
    {
        EraseSlot = 0x00,
        Modify = 0x01,
    }

    [Flags]
    public enum ScheduleWeekDayBitmask
    {
        Sunday = 0x01,
        Monday = 0x02,
        Tuesday = 0x04,
        Wednesday = 0x08,
        Thursday = 0x10,
        Friday = 0x20,
        Saturday = 0x40,
    }
}
