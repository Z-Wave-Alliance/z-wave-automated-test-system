// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Z-Wave Alliance https://z-wavealliance.org
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using Utils;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.Devices;

namespace Zats.Services
{
    [CommandClassName("Time Command Class")]
    [CommandClassId(COMMAND_CLASS_TIME_V3.ID)]
    [MinVersion(1)]
    [MaxVersion(3)]
    public class TimeSupport : CommandClassSupport
    {
        private TimeSupportData SupportData { get; set; }

        public override CommandClassSupportData CommandClassSupportData
        {
            get { return SupportData; }
            set { SupportData = (TimeSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;


        public TimeSupport()
            : this(new TimeSupportData() { }) { }

        public TimeSupport(TimeSupportData timeSupportData)
            : base(COMMAND_CLASS_TIME_V3.ID)
        {
            SupportData = timeSupportData;
        }


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
                bool isAnsweringAllowed = IsAnsweringAllowed();

                ResultForSv = SvResultNames.Undefined;
                if (command != null && command.Length > 1)
                {
                    byte[] rpt = null;

                    if (command[1] == COMMAND_CLASS_TIME_V3.TIME_GET.ID && isAnsweringAllowed)
                    {
                        rpt = (COMMAND_CLASS_TIME_V3.TIME_REPORT)SupportData;
                    }
                    else if (command[1] == COMMAND_CLASS_TIME_V3.DATE_GET.ID && isAnsweringAllowed)
                    {
                        rpt = (COMMAND_CLASS_TIME_V3.DATE_REPORT)SupportData;
                    }
                    else if (command[1] == COMMAND_CLASS_TIME_V3.TIME_OFFSET_GET.ID && isAnsweringAllowed)
                    {
                        rpt = (COMMAND_CLASS_TIME_V3.TIME_OFFSET_REPORT)SupportData;
                    }
                    else if (command[1] == COMMAND_CLASS_TIME_V3.TIME_SET.ID && isHandlingAllowed)
                    {
                        ResultForSv = HandleTimeSet(command, sendSrcNode);
                    }
                    else if (command[1] == COMMAND_CLASS_TIME_V3.TIME_REPORT.ID && isHandlingAllowed)
                    {
                        // CC:008A.03.09.12.001 accept TIME_REPORT for time synchronization
                        HandleTimeReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_TIME_V3.DATE_SET.ID && isHandlingAllowed)
                    {
                        ResultForSv = HandleDateSet(command, sendSrcNode);
                    }
                    else if (command[1] == COMMAND_CLASS_TIME_V3.DATE_REPORT.ID && isHandlingAllowed)
                    {
                        // according to CC:008A.03.09.12.001 accept TIME_REPORT for time synchronization (but not in the spec, yet)
                        HandleDateReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_TIME_V3.TIME_OFFSET_SET.ID && isHandlingAllowed)
                    {
                        // Not supported
                        ResultForSv = SvResultNames.Failed;
                    }


                    if (rpt != null && isAnsweringAllowed)
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
            {
                if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != CommandClassId)
                {
                    supervisionReport.status = (byte)SvStatusNames.NoSupport;
                    return;
                }

                supervisionReport.status = (byte)SvStatusNames.Fail;  // FAIL as default
                switch (supervisionGet.encapsulatedCommand[1])
                {
                    case COMMAND_CLASS_TIME_V3.TIME_SET.ID:
                    case COMMAND_CLASS_TIME_V3.TIME_OFFSET_SET.ID:
                    case COMMAND_CLASS_TIME_V3.DATE_SET.ID:
                        if (ResultForSv == SvResultNames.Successful)
                            supervisionReport.status = (byte)SvStatusNames.Success;
                        break;
                }
                ResultForSv = SvResultNames.Undefined;
            }
        }

        private SvResultNames HandleTimeSet(COMMAND_CLASS_TIME_V3.TIME_SET command, NodeTag sendSrcNode)
        {
            var svResult = HandleTimeSet(command.properties1.hour, command.minute, command.second);

            if (svResult == SvResultNames.Successful)
            {
                SendUnsolicitedReport((COMMAND_CLASS_TIME_V3.TIME_REPORT)SupportData, sendSrcNode);
            }

            return svResult;
        }

        private void HandleTimeReport(COMMAND_CLASS_TIME_V3.TIME_REPORT command)
        {
            HandleTimeSet(command.properties1.hourLocalTime, command.minuteLocalTime, command.secondLocalTime);
        }

        private SvResultNames HandleTimeSet(byte hour, byte minute, byte second)
        {
            if (SupportData.TimeSource != TimeSource.ZWave && SupportData.RtcFailure == false)
            {
                // CC: 008A.03.02.11.006 ignore TIME_SET if Time Source is not Z-Wave and RTC is working
                return SvResultNames.Failed;
            }

            DateTime newDateTime = new DateTime(
                SupportData.DeviceDateTime.Year,
                SupportData.DeviceDateTime.Month,
                SupportData.DeviceDateTime.Day,
                hour,
                minute,
                second);

            SupportData.InternalOffset = newDateTime - DateTime.Now;

            SupportDelegates?.DeviceDataUpdate(this);

            return SvResultNames.Successful;
        }

        private SvResultNames HandleDateSet(COMMAND_CLASS_TIME_V3.DATE_SET command, NodeTag sendSrcNode)
        {
            var svResult = HandleDateSet(
                Tools.ByteArrayToUInt16(command.year),
                command.month,
                command.day);

            if (svResult == SvResultNames.Successful)
            {
                SendUnsolicitedReport((COMMAND_CLASS_TIME_V3.DATE_REPORT)SupportData, sendSrcNode);
            }
            return svResult;
        }

        private void HandleDateReport(COMMAND_CLASS_TIME_V3.DATE_REPORT command)
        {
            HandleDateSet(
                Tools.ByteArrayToUInt16(command.year),
                command.month,
                command.day);
        }

        private SvResultNames HandleDateSet(UInt16 year, byte month, byte day)
        {
            if (SupportData.TimeSource != TimeSource.ZWave && SupportData.RtcFailure == false)
            {
                // CC: 008A.03.02.11.006 ignore TIME_SET if Time Source is not Z-Wave and RTC is working
                return SvResultNames.Failed;
            }

            DateTime newDateTime = new DateTime(
                year,
                month,
                day,
                SupportData.DeviceDateTime.Hour,
                SupportData.DeviceDateTime.Minute,
                SupportData.DeviceDateTime.Second);

            SupportData.InternalOffset = newDateTime - DateTime.Now;

            SupportDelegates?.DeviceDataUpdate(this);

            return SvResultNames.Successful;
        }


        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is TimeSupportData newData)
            {
                bool timeChanged = false;
                bool dateChanged = false;

                if (SupportData.InternalOffset != newData.InternalOffset)
                {
                    if (SupportData.DeviceDateTime.Hour != newData.DeviceDateTime.Hour ||
                        SupportData.DeviceDateTime.Minute != newData.DeviceDateTime.Minute ||
                        SupportData.DeviceDateTime.Second != newData.DeviceDateTime.Second)
                    {
                        timeChanged = true;
                    }
                    if (SupportData.DeviceDateTime.Year != newData.DeviceDateTime.Year ||
                        SupportData.DeviceDateTime.Month != newData.DeviceDateTime.Month ||
                        SupportData.DeviceDateTime.Day != newData.DeviceDateTime.Day)
                    {
                        dateChanged = true;
                    }
                }

                SupportData.TimeSource = newData.TimeSource;
                SupportData.RtcFailure = newData.RtcFailure;
                SupportData.InternalOffset = newData.InternalOffset;

                SupportDelegates?.DeviceDataUpdate?.Invoke(this);

                if (timeChanged)
                {
                    SendUnsolicitedReport((COMMAND_CLASS_TIME_V3.TIME_REPORT)SupportData, NodeTag.Empty);
                }

                if (dateChanged)
                {
                    SendUnsolicitedReport((COMMAND_CLASS_TIME_V3.DATE_REPORT)SupportData, NodeTag.Empty);
                }

            }
        }

        public override void StartEmulation()
        {
            SupportData.InternalOffset = TimeSpan.Zero;

            if (SupportData.InitOnEmulationStart)
            {
                SupportData.InternalOffset = SupportData.InitialDateTime - DateTime.Now;
            }
            SupportDelegates?.DeviceDataUpdate?.Invoke(this);
        }
    }

    public class TimeSupportData : CommandClassSupportData
    {
        public TimeSpan InternalOffset = TimeSpan.Zero;

        [ParameterSeparator(1, "Initialize Clock at Startup")]
        [CustomDataParameter(1, "Date / Time", CommandClassesImpl.UiControl.DateTimePicker, CommandClassesImpl.InputType.none, EditType.Config)]
        public DateTime InitialDateTime { get; set; } = DateTime.Now;

        [CustomDataParameter(1, "Initialize on Startup", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config)]
        public bool InitOnEmulationStart { get; set; } = false;


        [JsonIgnore]
        [ParameterSeparator(1, "Runtime Data")]
        [CustomDataParameter(1, "Date / Time", CommandClassesImpl.UiControl.DateTimePicker, CommandClassesImpl.InputType.none, EditType.Edit)]
        public DateTime DeviceDateTime
        {
            get { return DateTime.Now.Add(InternalOffset); }
            set { InternalOffset = value - DateTime.Now; } 
        }

        [CustomDataParameter(3, "Time Source", CommandClassesImpl.UiControl.ListComboBox, CommandClassesImpl.InputType.Enum, EditType.Config | EditType.Edit, typeof(TimeSource))]
        public TimeSource TimeSource { get; set; } = TimeSource.ZWave;

        [CustomDataParameter(1, "RTC Failure", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config | EditType.Edit)]
        public bool RtcFailure { get; set; } = false;


        public static implicit operator COMMAND_CLASS_TIME_V3.TIME_REPORT(TimeSupportData data)
        {
            COMMAND_CLASS_TIME_V3.TIME_REPORT rpt = new COMMAND_CLASS_TIME_V3.TIME_REPORT();
            rpt.properties1.rtcFailure = data.RtcFailure ? (byte)1 : (byte)0;
            rpt.properties1.timeSource = (byte)data.TimeSource;
            rpt.properties1.hourLocalTime = (byte)data.DeviceDateTime.Hour;
            rpt.minuteLocalTime = (byte)data.DeviceDateTime.Minute;
            rpt.secondLocalTime = (byte)data.DeviceDateTime.Second;
            return rpt;
        }

        public static implicit operator COMMAND_CLASS_TIME_V3.DATE_REPORT(TimeSupportData data)
        {
            COMMAND_CLASS_TIME_V3.DATE_REPORT rpt = new COMMAND_CLASS_TIME_V3.DATE_REPORT();
            rpt.year = Tools.GetBytes((UInt16)data.DeviceDateTime.Year);
            rpt.month = (byte)data.DeviceDateTime.Month;
            rpt.day = (byte)data.DeviceDateTime.Day;
            return rpt;
        }

        public static implicit operator COMMAND_CLASS_TIME_V3.TIME_OFFSET_REPORT(TimeSupportData data)
        {
            // Do not return any offest info (not implemented, and handled as CC:008A.02.05.13.001, which means another source is used to get these information)

            COMMAND_CLASS_TIME_V3.TIME_OFFSET_REPORT rpt = new COMMAND_CLASS_TIME_V3.TIME_OFFSET_REPORT();
            return rpt;
        }
    }

    public enum TimeSource
    {
        [Description("Z-Wave")]
        ZWave = 0,
        [Description("GPS / Satellite Nav")]
        GPS_SatelliteNav = 1,
        [Description("Wi-Fi / Internet")]
        WiFi_Internet = 2,
        [Description("RESERVED")]
        Reserved = 3
    }
}
