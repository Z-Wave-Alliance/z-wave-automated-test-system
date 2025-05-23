using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Utils;
using Utils.UI.Enums;
using ZatsServices_netcore.CommandClassesImpl.Definitions.Interfaces;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Configuration;
using ZWave.Devices;
using ZWave.Enums;
using ZWave.Xml.Application;

// ToDo: Supervision Report Status 'Working' for NotificationSet (optional)
// Done: Support for pull nodes partially removed

namespace Zats.Services
{
    [CommandClassName("Notification Command Class")]
    [CommandClassId(COMMAND_CLASS_NOTIFICATION_V3.ID)]
    [MinVersion(1)]
    [MaxVersion(8)]
    public class NotificationSupport : CommandClassSupport
    {
        private NotificationSupportData NotificationSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return NotificationSupportData; }
            set { NotificationSupportData = (NotificationSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public const int HighestV2AlarmType = 0x0B;

        public NotificationSupport()
            : this(new NotificationSupportData() { })
        { }

        public NotificationSupport(NotificationSupportData notificationSupportData)
            : base(COMMAND_CLASS_NOTIFICATION_V3.ID)
        {
            NotificationSupportData = notificationSupportData;
        }

        protected override void OnHandledDelayed(DataReceivedUnit ou)
        {
            ou.SetNextActionItems();

            if (MatchesDestinationEndPoint())
            {
                var sendDstNode = new NodeTag(ReceivedAchData.DstNode.Id, DstNode.EndPointId);
                var sendSrcNode = ReceivedAchData.SrcNode;
                byte[] command = ReceivedAchData.Command;
                var scheme = (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme;
                bool isHandlingAllowed = IsHandlingAllowed(Network, command, scheme, Secure);

                ResultForSv = SvResultNames.Undefined;
                if (isHandlingAllowed && command != null && command.Length > 1 && NotificationSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_GET.ID)
                    {
                        // COMMAND_CLASS_ALARM.ALARM_GET.ID equals COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_GET.ID
                        if (command.Length == 3)
                        {
                            rpt = PrepareAlarmV1ReportCmd(command);
                        }
                        else if (command.Length == 4)
                        {
                            rpt = PrepareAlarmV2ReportCmd(command);
                        }
                        else /*if (command.Length == 5)*/
                        {
                            rpt = PrepareNotificationV3ReportCmd(command);
                        }

                    }
                    else if (command[1] == COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_SUPPORTED_GET.ID)
                    {
                        // COMMAND_CLASS_ALARM_V2.ALARM_TYPE_SUPPORTED_GET.ID equals COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_SUPPORTED_GET.ID
                        rpt = PrepareAlarmV2OrNotificationSupportedReport();
                    }
                    else if (command[1] == COMMAND_CLASS_NOTIFICATION_V8.EVENT_SUPPORTED_GET.ID)
                    {
                        rpt = PrepareEventSupportedReportCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_SET.ID)
                    {
                        // COMMAND_CLASS_ALARM_V2.ALARM_SET.ID equals COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_SET.ID
                        ResultForSv = HandleAlarmOrNotificationSetCmd(command, sendSrcNode);
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

        private byte[] PrepareAlarmV1Report(byte alarmType)
        {
            if (Version >= 1 && Version <= 8)
            {
                COMMAND_CLASS_ALARM.ALARM_REPORT rpt = new COMMAND_CLASS_ALARM.ALARM_REPORT() { };
                if (!NotificationSupportData.V1Alarm)
                {
                    rpt.alarmType = 0;
                    rpt.alarmLevel = 0;
                }
                else
                {
                    rpt.alarmType = alarmType;
                    rpt.alarmLevel = 0;
                }
                return rpt;
            }
            return null;
        }

        private byte[] PrepareAlarmV1ReportCmd(byte[] command)
        {
            if (Version >= 1 && Version <= 8)
            {
                COMMAND_CLASS_ALARM.ALARM_GET getCmd = command;
                return PrepareAlarmV1Report(getCmd.alarmType);
            }
            return null;
        }

        private byte[] PrepareAlarmV2Report(byte alarmType, byte zwaveAlarmType)
        {
            if (Version >= 2 && Version <= 8)
            {
                COMMAND_CLASS_ALARM_V2.ALARM_REPORT rpt = new COMMAND_CLASS_ALARM_V2.ALARM_REPORT() { };
                NotificationTypeData requestedNotification;

                // Table 88 (SDS13781 2020B), Required behavior when receiving Alarm Get, version 2
                if (zwaveAlarmType == 0x00)
                {
                    if (NotificationSupportData.V1Alarm)
                    {
                        // Simulate Zwave Alarm Type Support (y) = NO && V1 Alarm Type Support = YES for each V1 Alarm Type (x)
                        rpt.alarmType = alarmType;
                        rpt.alarmLevel = 0;
                        rpt.numberOfEventParameters = 0;
                        rpt.zwaveAlarmEvent = 0;
                        rpt.zwaveAlarmStatus = 0;
                        rpt.zwaveAlarmType = 0;
                        rpt.zensorNetSourceNodeId = 0;
                        return rpt;
                    }
                    return null;
                }
                if (zwaveAlarmType == 0xFF)
                {
                    // Simulate Zwave Alarm Type Support (y=0xFF)
                    requestedNotification = NotificationSupportData.Notifications.Find(type => (byte)type.NotificationType <= HighestV2AlarmType);
                }
                else
                {
                    requestedNotification = NotificationSupportData.Notifications.Find(type => (byte)type.NotificationType == zwaveAlarmType);
                }
                if (requestedNotification != null)
                {
                    // Table 90 (SDS13781 2020B), Push node responses to V2 Alarm Get (example)
                    rpt.alarmType = 0;
                    rpt.alarmLevel = 0;
                    rpt.zwaveAlarmStatus = (byte) requestedNotification.NotificationStatus; // 0x00 (UnsolicitedReportsDisabled) or 0xFF (UnsolicitedReportsEnabled)
                    rpt.zwaveAlarmType = (byte) requestedNotification.NotificationType;
                    rpt.zwaveAlarmEvent = 0xFE;                                             // 0xFE = Queue Idle state
                    rpt.zensorNetSourceNodeId = 0;
                    rpt.numberOfEventParameters = 0;
                    if (NotificationSupportData.IsPullNode)
                    {
                        rpt.zwaveAlarmStatus = 0x00;                                        // 0x00: Simulate empty queue (CC:0071.04.00.21.006)
                        rpt.zwaveAlarmEvent = 0xFE;                                         // 0xFE = Queue Idle state
                    }
                    return rpt;
                }
            }
            return null;
        }
        private byte[] PrepareAlarmV2ReportCmd(byte[] command)
        {
            if (Version >= 2 && Version <= 8)
            {
                COMMAND_CLASS_ALARM_V2.ALARM_GET getCmd = command;
                return PrepareAlarmV2Report(getCmd.alarmType, getCmd.zwaveAlarmType);
            }
            return null;
        }

        private byte[] PrepareNotificationV3Report(byte v1AlarmType, byte requestedType, byte requestedEvent)
        {
            if (Version >= 3 && Version <= 8)
            {
                COMMAND_CLASS_NOTIFICATION_V3.NOTIFICATION_REPORT rpt = new COMMAND_CLASS_NOTIFICATION_V3.NOTIFICATION_REPORT() { };
                NotificationTypeData requestedNotification;

                if (v1AlarmType != 0x00 && NotificationSupportData.V1Alarm)
                {
                    // Simulate V1 Alarm Type Support = YES for each V1 Alarm Type (x)
                    rpt.v1AlarmType = v1AlarmType;
                    rpt.v1AlarmLevel = 0;
                    rpt.notificationStatus = 0x00;
                    rpt.notificationType = 0x00;
                    rpt.mevent = 0x00;
                    rpt.properties1.eventParametersLength = 0;
                    rpt.properties1.sequence = 0;
                    return rpt;
                }
                if (requestedType == 0xFF)
                {
                    if (NotificationSupportData.IsPullNode)
                    {
                        if (NotificationSupportData.Notifications.Count == 0)
                        {
                            return null;
                        }
                        requestedType = (byte) NotificationSupportData.Notifications[0].NotificationType;   // CC:0071.03.04.11.007
                    }
                    else
                    {
                        if (NotificationSupportData.Notifications.Count == 0)
                        {
                            return null;
                        }
                        requestedType = (byte)NotificationSupportData.Notifications[0].NotificationType;    // CC:0071.03.04.12.002
                    }
                }
                requestedNotification = NotificationSupportData.Notifications.Find(type => (byte)type.NotificationType == requestedType);
                if (requestedNotification != null)
                {
                    rpt.v1AlarmType = 0;
                    rpt.v1AlarmLevel = 0;
                    rpt.notificationStatus = NotificationSupportData.IsPullNode ? (byte)0xFE : (byte)requestedNotification.NotificationStatus;
                    rpt.notificationType = requestedType;
                    if (NotificationSupportData.IsPullNode)
                    {
                        rpt.mevent = 0xFE;  // CC:0071.03.05.11.005 for Pull Nodes (0xFE = empty)
                    }
                    else
                    {
                        rpt.mevent = 0xFE;  // CC:0071.03.04.11.009 for unsupported events (Push Nodes)
                        if (requestedEvent != 0xFE) // Unknown event or UnsolicitedReport for status change
                        {
                            if (requestedNotification.EventSupportedList.Contains(requestedEvent))
                            {
                                rpt.mevent = requestedNotification.EventOrState;
                            }
                        }
                    }
                    rpt.properties1.eventParametersLength = 0;
                    if (requestedNotification.EventParameter != null)
                    {
                        rpt.properties1.eventParametersLength = (byte)requestedNotification.EventParameter.Length;
                        if (requestedNotification.EventParameter.Length > 0)
                        {
                            rpt.eventParameter = requestedNotification.EventParameter;
                        }
                    }
                    if (requestedNotification.SequenceNumberSupported)
                    {
                        rpt.properties1.sequence = 1;
                        rpt.sequenceNumber = requestedNotification.SequenceNumber;
                    }
                    else
                    {
                        rpt.properties1.sequence = 0;
                        rpt.sequenceNumber = ByteValue.Empty;   // omit sequenceNumber field if sequence flag is 0
                    }
                    return rpt;
                }
                return null; // CC:0071.03.04.11.005
            }
            return null;
        }
        private byte[] PrepareNotificationV3ReportCmd(byte[] command)
        {
            if (Version >= 3 && Version <= 8)
            {
                COMMAND_CLASS_NOTIFICATION_V3.NOTIFICATION_GET getCmd = command;
                return PrepareNotificationV3Report(getCmd.v1AlarmType, getCmd.notificationType, getCmd.mevent);
            }
            return null;
        }

        private byte[] PrepareAlarmV2OrNotificationSupportedReport()
        {
            if (Version >= 2 && Version <= 8)
            {
                COMMAND_CLASS_NOTIFICATION_V3.NOTIFICATION_SUPPORTED_REPORT rpt = new COMMAND_CLASS_NOTIFICATION_V3.NOTIFICATION_SUPPORTED_REPORT() { };
                List<byte> supportedNotificationTypes = new List<byte>();
                foreach (NotificationTypeData notif in NotificationSupportData.Notifications)
                {
                    supportedNotificationTypes.Add((byte) notif.NotificationType);
                }
                rpt.bitMask = CommandClassSupportUtils.CreateBitmaskFromList(supportedNotificationTypes, 0, true);
                rpt.properties1.numberOfBitMasks = (byte) rpt.bitMask.Count;
                rpt.properties1.v1Alarm = (byte) (NotificationSupportData.V1Alarm ? 0x01 : 0x00);
                return rpt;
            }
            return null;
        }

        private byte[] PrepareEventSupportedReport(NotificationTypeNames requestedTypeName)
        {
            if (Version >= 3 && Version <= 8)
            {
                COMMAND_CLASS_NOTIFICATION_V3.EVENT_SUPPORTED_REPORT rpt = new COMMAND_CLASS_NOTIFICATION_V3.EVENT_SUPPORTED_REPORT() { };
                NotificationTypeData requestedNotification = NotificationSupportData.Notifications.Find(type => type.NotificationType == requestedTypeName);
                if (requestedNotification != null)
                {
                    rpt.notificationType = (byte) requestedTypeName;
                    var advertisedEvents = new List<byte>(requestedNotification.EventSupportedList);
                    advertisedEvents.Remove(0x00); // Do not advertise special-purpose value 0x00 'Idle' (CC:0071.03.02.11.004)
                    advertisedEvents.Remove(0xFE); // Do not advertise special-purpose value 0xFE 'Unknown' (CC:0071.03.02.11.004)
                    rpt.bitMask = CommandClassSupportUtils.CreateBitmaskFromList(advertisedEvents, 0, false);
                    rpt.properties1.numberOfBitMasks = (byte) rpt.bitMask.Count;
                    return rpt;
                }
                rpt.notificationType = (byte) requestedTypeName;
                rpt.properties1.numberOfBitMasks = 0;
                rpt.bitMask = new List<byte> { };
                return rpt;
            }
            return null;
        }
        private byte[] PrepareEventSupportedReportCmd(byte[] command)
        {
            if (Version >= 3 && Version <= 8)
            {
                COMMAND_CLASS_NOTIFICATION_V3.EVENT_SUPPORTED_GET getCmd = command;
                NotificationTypeNames requestedTypeName = (NotificationTypeNames) (byte) getCmd.notificationType;
                return PrepareEventSupportedReport(requestedTypeName);
            }
            return null;
        }

        private SvResultNames HandleNotificationSet(NotificationTypeNames requestedNotificationTypeName, NotificationStatusNames newNotificationStatusName, NodeTag srcNode)
        {
            if (Version >= 1 && Version <= 8)
            {
                NotificationTypeData requestedNotification = NotificationSupportData.Notifications.Find(type => type.NotificationType == requestedNotificationTypeName);
                if (requestedNotification != null)
                {
                    if (NotificationSupportData.IsPullNode)
                    {
                        if (newNotificationStatusName == 0x00)
                        {
                            // CC:0071.05.11.009: 0x00 indicates that a persistent Notification for the specified
                            // Notification Type MUST be cleared in the receiving node's queue.
                            requestedNotification.NotificationStatus = newNotificationStatusName;
                            return SvResultNames.Successful;
                        }
                    }
                    else
                    {
                        if (newNotificationStatusName == NotificationStatusNames.UnsolicitedReportsDisabled
                         || newNotificationStatusName == NotificationStatusNames.UnsolicitedReportsEnabled)
                        {
                            if (requestedNotification.NotificationStatus != newNotificationStatusName)
                            {
                                requestedNotification.NotificationStatus = newNotificationStatusName;
                                if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                                {
                                    SendUnsolicitedReport(PrepareNotificationV3Report(0, (byte) requestedNotificationTypeName, 0xFE), srcNode);
                                }
                            }
                            return SvResultNames.Successful;
                        }
                    }
                }
            }
            return SvResultNames.Failed;
        }
        private SvResultNames HandleAlarmOrNotificationSetCmd(byte[] command, NodeTag srcNode)
        {
            if (Version >= 3 && Version <= 8)
            {
                COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_SET setCmd = command;
                return HandleNotificationSet((NotificationTypeNames) (byte) setCmd.notificationType, (NotificationStatusNames) (byte) setCmd.notificationStatus, srcNode);
            }
            else if (Version >= 1 && Version <= 2)
            {
                COMMAND_CLASS_ALARM_V2.ALARM_SET setCmd = command;
                return HandleNotificationSet((NotificationTypeNames)(byte)setCmd.zwaveAlarmStatus, (NotificationStatusNames)(byte)setCmd.zwaveAlarmStatus, srcNode);
            }
            return SvResultNames.Failed;
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, 
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != CommandClassId)
            {
                supervisionReport.status = (byte) SvStatusNames.NoSupport;
                return;
            }

            supervisionReport.status = (byte) SvStatusNames.Fail;  // FAIL as default
            switch (supervisionGet.encapsulatedCommand[1])
            {
                case COMMAND_CLASS_NOTIFICATION_V8.NOTIFICATION_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        supervisionReport.status = (byte)SvStatusNames.Success;
                    }
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }


        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is NotificationSupportData newData)
            {
                //if (NotificationSupportData.IsPullNode != newData.IsPullNode
                // || NotificationSupportData.V1Alarm != newData.V1Alarm)
                {
                    NotificationSupportData.IsPullNode = newData.IsPullNode;
                    NotificationSupportData.V1Alarm = newData.V1Alarm;
                }

                /*List<NotificationTypeData> notifsRemoved = NotificationSupportData.Notifications.FindAll(x => newData.Notifications.Find(y => y.NotificationType == x.NotificationType) == null);
                List<NotificationTypeData> notifsAdded   = newData.Notifications.FindAll(x => NotificationSupportData.Notifications.Find(y => y.NotificationType == x.NotificationType) == null);
                if (notifsAdded.Count > 0 || notifsRemoved.Count > 0)
                {
                    notifsRemoved.ForEach(x => NotificationSupportData.Notifications.Remove(x));
                    NotificationSupportData.Notifications.AddRange(notifsAdded);

                    if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        SendUnsolicitedReport(PrepareNotificationSupportedReport());
                    }
                }*/

                              
                bool isSupportChanged = false;

                List<byte[]> capabilityReports = new List<byte[]>();

                List<NotificationTypeData> notifsRemoved = NotificationSupportData.Notifications.FindAll(x => newData.Notifications.Find(y => y.NotificationType == x.NotificationType) == null);
                if (notifsRemoved.Count > 0)
                {
                    isSupportChanged = true;
                    notifsRemoved.ForEach(x => NotificationSupportData.Notifications.Remove(x));
                }

                foreach (NotificationTypeData newNotification in newData.Notifications)
                {
                    if (NotificationSupportData.Notifications.Find(x => x.NotificationType == newNotification.NotificationType) is NotificationTypeData currentNotification)
                    {
                        if (!CommandClassSupportUtils.ListElementsEqual(currentNotification.EventSupportedList, newNotification.EventSupportedList))
                        {
                            currentNotification.EventSupportedList = newNotification.EventSupportedList;
                            if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                            {
                                capabilityReports.Add(PrepareEventSupportedReport(newNotification.NotificationType));
                            }
                        }
                        // Internals
                        if (currentNotification.SequenceNumber != newNotification.SequenceNumber
                            || currentNotification.EventOrState != newNotification.EventOrState
                            || Enumerable.SequenceEqual(currentNotification.EventParameter, newNotification.EventParameter) == false
                            || currentNotification.SequenceNumberSupported != newNotification.SequenceNumberSupported)
                        {
                            currentNotification.SequenceNumber = newNotification.SequenceNumber;
                            currentNotification.EventOrState = newNotification.EventOrState;
                            currentNotification.EventParameter = newNotification.EventParameter;
                            currentNotification.SequenceNumberSupported = newNotification.SequenceNumberSupported;
                        }
                        if (currentNotification.NotificationStatus != newNotification.NotificationStatus)
                        {
                            HandleNotificationSet(currentNotification.NotificationType, newNotification.NotificationStatus, NodeTag.Empty);
                        }
                    }
                    else
                    {
                        isSupportChanged = true;
                        NotificationSupportData.Notifications.Add(newNotification);
                        if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                        {
                            capabilityReports.Add(PrepareEventSupportedReport(newNotification.NotificationType));
                        }
                    }

                    // Notification handles trigger flag within complete update bacause it triggers a report, not a notification
                    // Updates of event data is progressed no matter if this flag is set to true. This supports pull sensors, too.
                    if (newNotification.TriggerNotificationEvent)
                    {
                        SendUnsolicitedReport(PrepareNotificationV3Report(0, (byte)newNotification.NotificationType, newNotification.EventOrState), NodeTag.Empty);
                    }
                }

                if (isSupportChanged && ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                {
                    SendUnsolicitedReport(PrepareAlarmV2OrNotificationSupportedReport(), NodeTag.Empty);
                }
                foreach (byte[] r in capabilityReports)
                {
                    SendUnsolicitedReport(r, NodeTag.Empty);
                }
                /*
                        PrepareAlarmV1Report(byte alarmType);
                        PrepareAlarmV2Report(byte alarmType, byte zwaveAlarmType);
                        PrepareNotificationReport(byte v1AlarmType, byte requestedType, byte requestedEvent);
                    done PrepareNotificationSupportedReport(void);
                    done PrepareEventSupportedReport(NotificationTypeNames requestedTypeName);
                    done HandleNotificationSet(NotificationTypeNames requestedNotificationTypeName, NotificationStatusNames newNotificationStatusName)
                */


                // safety mechanism: make sure that trigger flag is still false
                NotificationSupportData.Notifications.ForEach(x => x.TriggerNotificationEvent = false);

                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }


    public class NotificationTypeData : INestedItem
    {
        public NotificationTypeNames NotificationType { get; set; }

        public NotificationStatusNames NotificationStatus { get; set; } = NotificationStatusNames.UnsolicitedReportsDisabled;

        #region Trigger Notification Event (EDE)
        [CustomDataParameterEventTrigger]
        public bool TriggerNotificationEvent { get; set; }
        public List<byte> EventSupportedList { get; set; } = new List<byte>();
        public byte EventOrState { get; set; } = 0x00;
        public byte[] EventParameter { get; set; } = new byte[] { };

        // Internals for Notification Report
        public bool SequenceNumberSupported { get; set; } = false;
        public byte SequenceNumber { get; set; } = 0x01;

        #endregion

        public NotificationTypeData(NotificationTypeNames notificationTypeName, NotificationStatusNames notificationStatusName, List<byte> eventsSupportedList)
        {
            NotificationType = notificationTypeName;
            NotificationStatus = notificationStatusName;
            EventSupportedList = new List<byte>();
            if (eventsSupportedList != null)
            {
                EventSupportedList = eventsSupportedList;
            }
        }

        // only used for EDE UI. see interface for usage notes.
        [JsonIgnore]
        public int NestedId
        {
            get => (int)NotificationType;
            set { }
        }

        public Type GetAvailableEventsEnumType()
        {
            switch (NotificationType)
            {
                case NotificationTypeNames.SmokeAlarm:
                    return typeof(NotificationSmokeAlarmEvents);
                case NotificationTypeNames.COAlarm:
                    return typeof(NotificationCOAlarmEvents);
                case NotificationTypeNames.CO2Alarm:
                    return typeof(NotificationCO2AlarmEvents);
                case NotificationTypeNames.HeatAlarm:
                    return typeof(NotificationHeatAlarmEvents);
                case NotificationTypeNames.WaterAlarm:
                    return typeof(NotificationWaterAlarmEvents);
                case NotificationTypeNames.AccessControl:
                    return typeof(NotificationAccessControlEvents);
                case NotificationTypeNames.HomeSecurity:
                    return typeof(NotificationHomeSecurityEvents);
                case NotificationTypeNames.PowerManagement:
                    return typeof(NotificationPowerManagementEvents);
                case NotificationTypeNames.System:
                    return typeof(NotificationSystemEvents);
                case NotificationTypeNames.EmergencyAlarm:
                    return typeof(NotificationEmergencyAlarmEvents);
                case NotificationTypeNames.Clock:
                    return typeof(NotificationClockEvents);
                case NotificationTypeNames.Appliance:
                    return typeof(NotificationApplianceEvents);
                case NotificationTypeNames.HomeHealth:
                    return typeof(NotificationHomeHealthEvents);
                case NotificationTypeNames.Siren:
                    return typeof(NotificationSirenEvents);
                case NotificationTypeNames.WaterValve:
                    return typeof(NotificationWaterValveEvents);
                case NotificationTypeNames.WeatherAlarm:
                    return typeof(NotificationWeatherAlarmEvents);
                case NotificationTypeNames.Irrigation:
                    return typeof(NotificationIrrigationEvents);
                case NotificationTypeNames.GasAlarm:
                    return typeof(NotificationGasAlarmEvents);
                case NotificationTypeNames.PestControl:
                    return typeof(NotificationPestControlEvents);
                case NotificationTypeNames.LightSensor:
                    return typeof(NotificationLightSensorEvents);
                case NotificationTypeNames.WaterQualityMonitoring:
                    return typeof(NotificationWaterQualityMonitoringEvents);
                case NotificationTypeNames.HomeMonitoring:
                    return typeof(NotificationHomeMonitoringEvents);
                default:
                    throw new Exception($"Unmapped Notification Type: {NotificationType}!");
            }
        }
    }

    public class NotificationSupportData : CommandClassSupportData, INodeItem
    {
        // Internals accessible from TestCases or EDE

        // Notification Report
        // Supported Notifications: Internal Default: none
        [NestedControl(1, "Notification Types")]
        public List<NotificationTypeData> Notifications { get; set; } = new List<NotificationTypeData>();
        // Pull node functionality is not fully implemented (queue is empty always)
        //[CustomDataParameter(4, "Is Pull Node", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config)]
        public bool IsPullNode { get; set; } = false;

        // Notification Supported Report
        // Supported Notifications Bitmask: Will be generated from NotificationSupportData.Notifications at runtime.
        [CustomDataParameter(1, "V1 Alarm", CommandClassesImpl.UiControl.CheckBox, CommandClassesImpl.InputType.Boolean, EditType.Config)]
        public bool V1Alarm { get; set; } = false;

        [JsonIgnore]
        public IEnumerable<INestedItem> ChildItems => Notifications;

        // Test Cases MAY use this method to create a notification in NotificationSupportData.Notifications
        public NotificationTypeData AddNotificationType(NotificationTypeNames notificationTypeName, NotificationStatusNames notificationStatusName, List<byte> eventsSupported)
        {
            byte notificationType = (byte) notificationTypeName;
            if (Notifications.Find(type => type.NotificationType == notificationTypeName) == null)
            {
                NotificationTypeData obj = new NotificationTypeData(notificationTypeName, notificationStatusName, eventsSupported);
                Notifications.Add(obj);
                return obj;
            }

            return null;
        }
        public NotificationTypeData AddNotificationType(byte notificationType, NotificationStatusNames notificationStatusName, List<byte> eventsSupported) {
            return AddNotificationType((NotificationTypeNames) notificationType, notificationStatusName, eventsSupported);
        }

        // Test Cases MAY use this method to change a notification in NotificationSupportData.Notifications
        public NotificationTypeData UpdateNotification(NotificationTypeNames notificationTypeName, NotificationStatusNames notificationStatusName, List<byte> eventsSupported)
        {
            byte notificationType = (byte) notificationTypeName;
            if (Notifications.Find(type => type.NotificationType == notificationTypeName) is NotificationTypeData notification)
            {
                Notifications.Remove(notification);
                NotificationTypeData obj = new NotificationTypeData(notificationTypeName, notificationStatusName, eventsSupported);
                Notifications.Add(obj);
                return obj;
            }
            return null;
        }
    }

    public enum NotificationTypeNames : byte
    {
        [Description("0x01 Smoke Alarm")]
        SmokeAlarm = 0x01,
        [Description("0x02 CO Alarm")]
        COAlarm = 0x02,
        [Description("0x03 CO2 Alarm")]
        CO2Alarm = 0x03,
        [Description("0x04 Heat Alarm")]
        HeatAlarm = 0x04,
        [Description("0x05 Water Alarm")]
        WaterAlarm = 0x05,
        [Description("0x06 Access Control")]
        AccessControl = 0x06,
        [Description("0x07 Home Security")]
        HomeSecurity = 0x07,
        [Description("0x08 Power Management")]
        PowerManagement = 0x08,
        [Description("0x09 System")]
        System = 0x09,
        [Description("0x0A Emergency Alarm")]
        EmergencyAlarm = 0x0A,
        [Description("0x0B Clock")]
        Clock = 0x0B,
        [Description("0x0C Appliance")]
        Appliance = 0x0C,
        [Description("0x0D Home Health")]
        HomeHealth = 0x0D,
        [Description("0x0E Siren")]
        Siren = 0x0E,
        [Description("0x0F Water Valve")]
        WaterValve = 0x0F,
        [Description("0x10 Weather Alarm")]
        WeatherAlarm = 0x10,
        [Description("0x11 Irrigation")]
        Irrigation = 0x11,
        [Description("0x12 Gas Alarm")]
        GasAlarm = 0x12,
        [Description("0x13 Pest Control")]
        PestControl = 0x13,
        [Description("0x14 Light Sensor")]
        LightSensor = 0x14,
        [Description("0x15 Water Quality Monitoring")]
        WaterQualityMonitoring = 0x15,
        [Description("0x16 Home Monitoring")]
        HomeMonitoring = 0x16,
    };

    public enum NotificationStatusNames : byte
    {
#if _PULLNODESUPPORT
        [Description("0x00 Report with notification from queue")]
        ReportWithNotificationFromQueue = 0x00,     // V2+ pull nodes only
        [Description("0x00 Report without notification from queue")]
        ReportWithoutNotificationFromQueue = 0xFE,  // V3+ pull nodes only
#endif
        [Description("0x00 Unsolicited Reports disabled")]
        UnsolicitedReportsDisabled = 0x00,          // V2+ push nodes only
        [Description("0xFF Unsolicited Reports enabled")]
        UnsolicitedReportsEnabled = 0xFF,           // V2+ push nodes only
    };

    #region Notification<Type>Events enums
    public enum NotificationSmokeAlarmEvents : byte
    {
        [Description("0x00 Idle")]
        StateIdle = 0x00,
        [Description("0x01 Smoke detected (location provided)")]
        SmokeDetectedWithLocation = 0x01,
        [Description("0x02 Smoke detected")]
        SmokeDetected = 0x02,
        [Description("0x03 Smoke alarm test")]
        SmokeAlarmTest = 0x03,
        [Description("0x06 Alarm silenced")]
        AlarmSilenced = 0x06,
        [Description("0x04 Replacement required")]
        ReplacementRequired = 0x04,
        [Description("0x05 Replacement required, End-of-life")]
        ReplacementRequiredEndOfLife = 0x05,
        [Description("0x07 Maintenance required, planned periodic inspection")]
        MaintenanceRequiredPlannedPeriodicInspection = 0x07,
        [Description("0x08 Maintenance required, dust in device")]
        MaintenanceRequiredDustInDevice = 0x08,
        [Description("0xFE Unknown event")]
        UnknownEvent = 0xFE
    }

    public enum NotificationCOAlarmEvents : byte
    {
        [Description("0x00 Idle")]
        StateIdle = 0x00,
        [Description("0x01 Carbon monoxide detected (location provided)")]
        CarbonMonoxideDetectedWithLocation = 0x01,
        [Description("0x02 Carbon monoxide detected")]
        CarbonMonoxideDetected = 0x02,
        [Description("0x03 Carbon monoxide test")]
        CarbonMonoxideTest = 0x03,
        [Description("0x04 Replacement required")]
        ReplacementRequired = 0x04,
        [Description("0x05 Replacement required End-of-life")]
        ReplacementRequiredEndOfLife = 0x05,
        [Description("0x06 Alarm silenced")]
        AlarmSilenced = 0x06,
        [Description("0x07 Maintenance required, planned periodic inspection")]
        MaintenanceRequiredPlannedPeriodicInspection = 0x07,
        [Description("0xFE Unknown event")]
        UnknownEvent = 0xFE
    }

    public enum NotificationCO2AlarmEvents : byte
    {
        [Description("0x00 Idle")]
        StateIdle = 0x00,
        [Description("0x01 Carbon dioxide detected (location provided)")]
        CarbonDioxideDetectedWithLocation = 0x01,
        [Description("0x02 Carbon dioxide detected")]
        CarbonDioxideDetected = 0x02,
        [Description("0x03 Carbon dioxide test")]
        CarbonDioxideTest = 0x03,
        [Description("0x04 Replacement required")]
        ReplacementRequired = 0x04,
        [Description("0x05 Replacement required, End-of-life")]
        ReplacementRequiredEndOfLife = 0x05,
        [Description("0x06 Alarm silenced")]
        AlarmSilenced = 0x06,
        [Description("0x07 Maintenance required, planned periodic inspection")]
        MaintenanceRequiredPlannedPeriodicInspection = 0x07,
        [Description("0xFE Unknown event")]
        UnknownEvent = 0xFE
    }

    public enum NotificationHeatAlarmEvents : byte
    {
        [Description("0x00 Idle")]
        StateIdle = 0x00,
        [Description("0x01 Overheat detected (location provided)")]
        OverheatDetectedWithLocation = 0x01,
        [Description("0x02 Overheat detected")]
        OverheatDetected = 0x02,
        [Description("0x05 Underheat detected (location provided)")]
        UnderHeatDetectedWithLocation = 0x05,
        [Description("0x06 Underheat detected")]
        UnderHeatDetected = 0x06,
        [Description("0x03 Rapid temperature rise (location detected)")]
        RapidTemperatureRiseWithLocation = 0x03,
        [Description("0x04 Rapid temperature rise")]
        RapidTemperatureRise = 0x04,
        [Description("0x0C Rapid temperature fall (location provided)")]
        RapidTemperatureFallWithLocation = 0x0C,
        [Description("0x0D Rapid temperature fall")]
        RapidTemperatureFall = 0x0D,
        [Description("0x07 Heat alarm test")]
        HeatAlarmTest = 0x07,
        [Description("0x09 Alarm silenced")]
        AlarmSilenced = 0x09,
        [Description("0x08 Replacement required, End-of-life")]
        ReplacementRequiredEndOfLife = 0x08,
        [Description("0x0A Mainenance required, dust in device")]
        MaintenanceRequiredDustInDevice = 0x0A,
        [Description("0x0B Maintenance required, planned periodic inspection")]
        MaintenanceRequiredPlannedPeriodicInspection = 0x0B,
        [Description("0xFE Unknown event")]
        UnknownEvent = 0xFE
    }

    public enum NotificationWaterAlarmEvents : byte
    {
        [Description("0x00 Idle")]
        StateIdle = 0x00,
        [Description("0x01 Water leak detected (location provided)")]
        WaterLeakDetectedWithLocation = 0x01,
        [Description("0x02 Water leak detected")]
        WaterLeakDetected = 0x02,
        [Description("0x03 Water level dropped (location provided)")]
        WaterLevelDroppedWithLocation = 0x03,
        [Description("0x04 Water level dropped")]
        WaterLevelDropped = 0x04,
        [Description("0x05 Replace water filter")]
        ReplaceWaterFilter = 0x05,
        [Description("0x06 Water flow alarm")]
        WaterFlowAlarm = 0x06,
        [Description("0x07 Water pressure alarm")]
        WaterPressureAlarm = 0x07,
        [Description("0x08 Water temperatur alarm")]
        WaterTemperatureAlarm = 0x08,
        [Description("0x09 Water level alarm")]
        WaterLevelAlarm = 0x09,
        [Description("0x0A Sump pump active")]
        SumpPumpActive = 0x0A,
        [Description("0x0B Sump pump failure")]
        SumpPumpFailure = 0x0B,
        [Description("0xFE Unknown event")]
        UnknownEvent = 0xFE
    }

    public enum NotificationAccessControlEvents : byte
    {
        [Description("0x00 Idle")]
        StateIdle = 0x00,
        [Description("0x01 Manual lock operation")]
        ManualLockOperation = 0x01,
        [Description("0x02 Manual unlock operation")]
        ManualUnlockOperation = 0x02,
        [Description("0x03 RF lock operation")]
        RfLockOperation = 0x03,
        [Description("0x04 RF unlock operation")]
        RfUnlockOperation = 0x04,
        [Description("0x05 Keypad lock operation")]
        KeypadLockOperation = 0x05,
        [Description("0x06 Keypad unlock operation")]
        KeypadUnlockOperation = 0x06,
        [Description("0x07 Manual not fully locked operation")]
        ManualNotFullyLockedOperation = 0x07,
        [Description("0x08 RF not fully locked operation")]
        RfNotFullyLockedOperation = 0x08,
        [Description("0x09 Auto lock locked operation")]
        AutoLockLockedOperation = 0x09,
        [Description("0x0A Auto lock not fully locked operation")]
        AutoLockNotFullyLockedOperation = 0x0A,
        [Description("0x0B Lock jammed")]
        LockJammed = 0x0B,
        [Description("0x0C All user codes deleted")]
        AllUserCodesDeleted = 0x0C,
        [Description("0x0D Single user code deleted")]
        SingleUserCodeDeleted = 0x0D,
        [Description("0x0E New user code added")]
        NewUserCodeAdded = 0x0E,
        [Description("0x0F New user code not added due to duplicate code")]
        NewUserCodeNotAddedDueToDuplicateCode = 0x0F,
        [Description("0x10 Keypad temporary disabled")]
        KeypadTemporaryDisabled = 0x10,
        [Description("0x11 Keypad busy")]
        KeypadBusy = 0x11,
        [Description("0x12 New program code entered")]
        NewProgramCodeEntered = 0x12,
        [Description("0x13 Manually enter user access code exceeds code limit")]
        ManuallyEnterUserAccesCodeExceedsCodeLimit = 0x13,
        [Description("0x14 Unlock by RF with invalid user code")]
        UnlockByRfWithInvalidUserCode = 0x14,
        [Description("0x15 Locked by RF with invalid user code")]
        LockedByRfWithInvalidUserCode = 0x15,
        [Description("0x16 Window / door is open")]
        WindowOrDoorIsOpen = 0x16,
        [Description("0x17 Window / door is closed")]
        WindowOrDoorIsClosed = 0x17,
        [Description("0x18 Window / door handle is open")]
        WindowOrDoorHandleIsOpen = 0x18,
        [Description("0x19 Window / door handle is closed")]
        WindowOrDoorHandleIsClosed = 0x19,
        [Description("0x20 Messaging User Code entered via keypad")]
        MessagingUserCodeEnteredViaKeypad = 0x20,
        [Description("0x21 Lock operation with user code")]
        LockOperationWithUserCode = 0x21,
        [Description("0x22 Unlock operation with user code")]
        UnlockOperationWithUserCode = 0x22,
        [Description("0x23 Credential lock close operation")]
        CredentialLockCloseOperation = 0x23,
        [Description("0x24 Credential unlock open operation")]
        CredentialUnlockOpenOperation = 0x24,
        // 0x25 ... 0x2E have been obsoleted.
        //[Description("0x25 All users deleted [OBSOLETE]")]
        //AllUsersDeleted = 0x25,
        //[Description("0x26 Multiple credentials deleted [OBSOLETE]")]
        //MultipleCredentialsDeleted = 0x26,
        //[Description("0x27 User added [OBSOLETE]")]
        //UserAdded = 0x27,
        //[Description("0x28 User modified [OBSOLETE]")]
        //UserModified = 0x28,
        //[Description("0x29 User deleted [OBSOLETE]")]
        //UserDeleted = 0x29,
        //[Description("0x2A User unchanged [OBSOLETE]")]
        //UserUnchanged = 0x2A,
        //[Description("0x2B Credential added [OBSOLETE]")]
        //CredentialAdded = 0x2B,
        //[Description("0x2C Credential modified [OBSOLETE]")]
        //CredentialModified = 0x2C,
        //[Description("0x2D Credential deleted [OBSOLETE]")]
        //CredentialDeleted = 0x2D,
        //[Description("0x2E Credential unchanged [OBSOLETE]")]
        //CredentialUnchanged = 0x2E,
        [Description("0x2F Valid credential access denied (User active state being set to occupied disabled)")]
        ValidCredentialAccessDeniedDueToUserActiveStateBeingSetToOccupiedDisabled = 0x2F,
        [Description("0x30 Valid credential access denied (User Schedule being inactive)")]
        ValidCredentialAccessDeniedDueToTheUserScheduleBeingInactive = 0x30,
        [Description("0x31 User access denied (Not enough credentials entered for rule)")]
        UserAccessDeniedDueToNotEnoughCredentialsEnteredForTheUserCredentialRule = 0x31,
        [Description("0x32 Invalid credential used")]
        InvalidCredentialUsedToAccessTheNode = 0x32,
        [Description("0x33 Non-access credential entered via local interface")]
        NonAccessCredentialEnteredViaLocalInterface = 0x33,
        // 0x34 .. 0x3F are reserved
        [Description("0x40 Barrier performing initialization process")]
        BarrierPerformingInitializationProcess = 0x40,
        [Description("0x41 Barrier operation force has been exceeded")]
        BarrierOperationForceHasBeenExceeded = 0x41,
        [Description("0x42 Barrier motor has exceed manufacturers operational time limit")]
        BarrierMotorHasExceededManufacturersOperationalTimeLimit = 0x42,
        [Description("0x43 Barrier operation has exceed physical mechanical limits")]
        BarrierOperationHasExceededPhysicalMechanicalLimits = 0x43,
        [Description("0x44 Barrier unable to perform requested operation due to UL requirements")]
        BarrierUnableToPerformRequestedOperationDueToULRequirements = 0x44,
        [Description("0x45 Barrier unattended operation has been disabled per UL requirements")]
        BarrierUnattendedOperationHasBeenDisabledPerULRequirements = 0x45,
        [Description("0x46 Barrier failed to perform requested operation (device malfunction)")]
        BarrierFailedToPerformRequestedOperation_DeviceMalfunction = 0x46,
        [Description("0x47 Barrier vacation mode")]
        BarrierVacationMode = 0x47,
        [Description("0x48 Barrier safety beam obstacle")]
        BarrierSafetyBeamObstacle = 0x48,
        [Description("0x49 Barrier sensor not detected (supervision error)")]
        BarrierSensorNotDetected_SupervisoryError = 0x49,
        [Description("0x4A Barrier sensor low battery warning")]
        BarrierSensorLowBatteryWarning = 0x4A,
        [Description("0x4B Barrier detected short in wall station wires")]
        BarrierDetectedShortInWallStationWires = 0x4B,
        [Description("0x4C Barrier associated with non Z-Wave remote control")]
        BarrierAssociatedWithNonZWaveRemoteControl = 0x4C,
        [Description("0xFE Unknown event")]
        UnknownEvent = 0xFE
    }

    public enum NotificationHomeSecurityEvents : byte
    {
        [Description("0x00 Idle")]
        StateIdle = 0x00,
        [Description("0x01 Intrusion detected (location provided)")]
        IntrusionWithLocation = 0x01,
        [Description("0x02 Intrusion detected")]
        Intrusion = 0x02,
        [Description("0x03 Tampering, product cover removed")]
        Tampering_ProductCoverRemoved = 0x03,
        [Description("0x04 Tampering, invalid code")]
        Tampering_InvalidCode = 0x04,
        [Description("0x05 Glass breakage (location provided)")]
        GlassBreakageWithLocation = 0x05,
        [Description("0x06 Glass breakage")]
        GlassBreakage = 0x06,
        [Description("0x07 Motion detection (location provided)")]
        MotionDetectionWithLocation = 0x07,
        [Description("0x08 Motion detection")]
        MotionDetection = 0x08,
        [Description("0x09 Tampering, product moved")]
        TamperingProductMoved = 0x09,
        [Description("0x0A Impact detected")]
        ImpactDetected = 0x0A,
        [Description("0x0B Magnetic field interference detected")]
        MagneticFieldInterferenceDetected = 0x0B,
        [Description("0x0C RF jamming detected")]
        RfJammingDetected = 0x0C,
        [Description("0xFE Unknown event")]
        UnknownEvent = 0xFE
    }

    public enum NotificationPowerManagementEvents : byte
    {
        [Description("0x00 Idle")]
        StateIdle = 0x00,
        [Description("0x01 Power applied")]
        PowerHasBeenApplied = 0x01,
        [Description("0x02 AC mains disconnected")]
        ACmainsDisconnected = 0x02,
        [Description("0x03 AC mains reconnected")]
        ACmainsReconnected = 0x03,
        [Description("0x04 Surge detected")]
        SurgeDetected = 0x04,
        [Description("0x05 Voltage drop / drift")]
        VoltageDropOrDrift = 0x05,
        [Description("0x06 Over-current detected")]
        OverCurrentDetected = 0x06,
        [Description("0x07 Over-voltage detected")]
        OverVoltageDetected = 0x07,
        [Description("0x08 Over-load detected")]
        OverloadDetected = 0x08,
        [Description("0x09 Load error")]
        LoadError = 0x09,
        [Description("0x0A Replace battery soon")]
        ReplaceBatterySoon = 0x0A,
        [Description("0x0B Replace battery now")]
        ReplaceBatteryNow = 0x0B,
        [Description("0x0C Battery is charging")]
        BatteryIsCharging = 0x0C,
        [Description("0x0D Battery is fully charged")]
        BatteryIsFullyCharged = 0x0D,
        [Description("0x0E Charge battery soon")]
        ChargeBatterySoon = 0x0E,
        [Description("0x01 Charge battery now")]
        ChargeBatteryNow = 0x0F,
        [Description("0x10 Backup battery is low")]
        BackupBatteryIsLow = 0x10,
        [Description("0x11 Battery fluid is low")]
        BatteryFluidIsLow = 0x11,
        [Description("0x12 Backup battery disconnected")]
        BackupBatteryDisconnected = 0x12,
        [Description("0x13 DC Jack Connected")]
        DCJackConnected = 0x13,
        [Description("0x14 DC Jack Disconnected")]
        DCJackDisconnected = 0x14,
        [Description("0xFE Unknown event")]
        UnknownEvent = 0xFE
    }

    public enum NotificationSystemEvents : byte
    {
        [Description("0x00 Idle")]
        StateIdle = 0x00,
        [Description("0x01 System hardware failure")]
        SystemHardwareFailure = 0x01,
        [Description("0x03 System hardware failure (code provided)")]
        SystemHardwareFailure_CodeProvided = 0x03,
        [Description("0x02 System software failure")]
        SystemSoftwareFailure = 0x02,
        [Description("0x04 System software failure (code provided)")]
        SystemSoftwareFailure_CodeProvided = 0x04,
        [Description("0x05 Heartbeat")]
        Heartbeat = 0x05,
        [Description("0x09 Tampering, product cover removed")]
        Tampering_ProductCoverRemoved = 0x06,
        [Description("0x07 Emergency shutoff")]
        EmergencyShutoff = 0x07,
        [Description("0x09 Digital input high state")]
        DigitalInputHighState = 0x09,
        [Description("0x0A Digital input low state")]
        DigitalInputLowState = 0x0A,
        [Description("0x0B Digital input open")]
        DigitalInputOpen = 0x0B,
        [Description("0xFE Unknown event")]
        UnknownEvent = 0xFE
    }

    public enum NotificationEmergencyAlarmEvents : byte
    {
        [Description("0x00 Idle")]
        StateIdle = 0x00,
        [Description("0x01 Contact police")]
        ContactPolice = 0x01,
        [Description("0x02 Contact fire service")]
        ContactFireService = 0x02,
        [Description("0x03 Contact medical service")]
        ContactMedicalService = 0x03,
        [Description("0x04 Panic alert")]
        PanicAlert = 0x04,
        [Description("0xFE Uunknown event")]
        UnknownEvent = 0xFE
    }

    public enum NotificationClockEvents : byte
    {
        [Description("0x00 Idle")]
        StateIdle = 0x00,
        [Description("0x01 Wake up alert")]
        WakeUpAlert = 0x01,
        [Description("0x02 Timer ended")]
        TimerEnded = 0x02,
        [Description("0x03 Time remaining")]
        TimeRemaining = 0x03,
        [Description("0xFE Unknown event")]
        UnknownEvent = 0xFE
    }

    public enum NotificationApplianceEvents : byte
    {
        [Description("0x00 Idle")]
        StateIdle = 0x00,
        [Description("0x01 Program started")]
        ProgramStarted = 0x01,
        [Description("0x02 Program in progress")]
        ProgramInProgress = 0x02,
        [Description("0x03 Program completed")]
        ProgramCompleted = 0x03,
        [Description("0x04 Replace main filter")]
        ReplaceMainFilter = 0x04,
        [Description("0x06 Supplying water")]
        SupplyingWater = 0x06,
        [Description("0x08 Boiling")]
        Boiling = 0x08,
        [Description("0x0A Washing")]
        Washing = 0x0A,
        [Description("0x0C Rinsing")]
        Rinsing = 0x0C,
        [Description("0x0E Draining")]
        Draining = 0x0E,
        [Description("0x10 Spinning")]
        Spinning = 0x10,
        [Description("0x12 Drying")]
        Drying = 0x12,
        [Description("0x05 Failure to set target temperture")]
        FailureToSetTargetTemperature = 0x05,
        [Description("0x07 Water supply failure")]
        WaterSupplyFailure = 0x07,
        [Description("0x09 Boiling failure")]
        BoilingFailure = 0x09,
        [Description("0x0B Washing failure")]
        WashingFailure = 0x0B,
        [Description("0x0D Rinsing failure")]
        RinsingFailure = 0x0D,
        [Description("0x0F Draining failure")]
        DrainingFailure = 0x0F,
        [Description("0x11 Spinning failure")]
        SpinningFailure = 0x11,
        [Description("0x13 Drying failure")]
        DryingFailure = 0x13,
        [Description("0x14 Fan failure")]
        FanFailure = 0x14,
        [Description("0x15 Compressor failure")]
        CompressorFailure = 0x15,
        [Description("0xFE Unknown event")]
        UnknownEvent = 0xFE
    }

    public enum NotificationHomeHealthEvents : byte
    {
        [Description("0x00 Idle")]
        StateIdle = 0x00,
        [Description("0x01 Leaving bed")]
        LeavingBed = 0x01,
        [Description("0x02 Sitting on bed")]
        SittingOnBed = 0x02,
        [Description("0x03 Lying on bed")]
        LyingOnBed = 0x03,
        [Description("0x05 Sitting on bed edge")]
        SittingOnBedEdge = 0x05,
        [Description("0x04 Posture changed")]
        PostureChanged = 0x04,
        [Description("0x06 Volatile organic compound level")]
        VolatileOrganicCompoundLevel = 0x06,
        [Description("0x07 Sleep apnea detected")]
        SleepApneaDetected = 0x07,
        [Description("0x08 Sleep stage 0 detected")]
        SleepStage0Detected_Dreaming_REM = 0x08,
        [Description("0x09 Sleep state 1 detetced")]
        SleepStage1Detected_LightSleep_nonREM1 = 0x09,
        [Description("0x0A Sleep stage 2 detected")]
        SleepStage2Detected_MediumSleep_nonREM2 = 0x0A,
        [Description("0x0B Sleep stage 3 detected")]
        SleepStage3Detected_DeepSleep_nonREM3 = 0x0B,
        [Description("0x0C Fall detected")]
        FallDetected = 0x0C,
        [Description("0xFE Unknown event")]
        UnknownEvent = 0xFE
    }

    public enum NotificationSirenEvents : byte
    {
        [Description("0x00 Idle")]
        StateIdle = 0x00,
        [Description("0x01 Siren active")]
        SirenActive = 0x01,
        [Description("0xFE Unknown event")]
        UnknownEvent = 0xFE
    }

    public enum NotificationWaterValveEvents : byte
    {
        [Description("0x00 Idle")]
        StateIdle = 0x00,
        [Description("0x01 Valve operation")]
        ValveOperation = 0x01,
        [Description("0x02 Main valve operation")]
        MainValveOperation = 0x02,
        [Description("0x03 Valve short circiut")]
        ValveShortCircuit = 0x03,
        [Description("0x04 Main valve short circuit")]
        MainValveShortCircuit = 0x04,
        [Description("0x05 Valve current alarm")]
        ValveCurrentAlarm = 0x05,
        [Description("0x06 Main valve current alarm")]
        MainValveCurrentAlarm = 0x06,
        [Description("0x07 Valve jammed")]
        ValveJammed = 0x07,
        [Description("0xFE Unknown event")]
        UnknownEvent = 0xFE
    }

    public enum NotificationWeatherAlarmEvents : byte
    {
        [Description("0x00 Idle")]
        StateIdle = 0x00,
        [Description("0x01 rain alarm")]
        RainAlarm = 0x01,
        [Description("0x02 Moisture alarm")]
        MoistureAlarm = 0x02,
        [Description("0x03 Freeze alarm")]
        FreezeAlarm = 0x03,
        [Description("0xFE Unknown event")]
        UnknownEvent = 0xFE
    }

    public enum NotificationIrrigationEvents : byte
    {
        [Description("0x00 Idle")]
        StateIdle = 0x00,
        [Description("0x01 Schedule started")]
        ScheduleStarted = 0x01,
        [Description("0x02 Schedule finished")]
        ScheduleFinished = 0x02,
        [Description("0x03 Valve table run started")]
        ValveTableRunStarted = 0x03,
        [Description("0x04 Valve table run finished")]
        ValveTableRunFinished = 0x04,
        [Description("0x05 Device is not configured")]
        DeviceIsNotConfigured = 0x05,
        [Description("0xFE Unknown event")]
        UnknownEvent = 0xFE
    }

    public enum NotificationGasAlarmEvents : byte
    {
        [Description("0x00 Idle")]
        StateIdle = 0x00,
        [Description("0x01 Combustible gas detected (location provided)")]
        CombustibleGasDetectedWithLocation = 0x01,
        [Description("0x02 Combustible gas detected")]
        CombustibleGasDetected = 0x02,
        [Description("0x03 Toxic gas detected (location provided)")]
        ToxicGasDetectedWithLocation = 0x03,
        [Description("0x04 Toxic gas detected")]
        ToxicGasDetected = 0x04,
        [Description("0x05 Gas alarm test")]
        GasAlarmTest = 0x05,
        [Description("0x06 Replacement required")]
        ReplacementRequired = 0x06,
        [Description("0xFE Unknown event")]
        UnknownEvent = 0xFE
    }

    public enum NotificationPestControlEvents : byte
    {
        [Description("0x00 Idle")]
        StateIdle = 0x00,
        [Description("0x01 Trap armed (location provided)")]
        TrapArmedWithLocation = 0x01,
        [Description("0x02 Trap armed")]
        TrapArmed = 0x02,
        [Description("0x03 Trap rearm required (location provided)")]
        TrapRearmRequiredWithLocation = 0x03,
        [Description("0x04 Trap rearm required")]
        TrapRearmRequired = 0x04,
        [Description("0x05 Pest detected (location provided)")]
        PestDetectedWithLocation = 0x05,
        [Description("0x06 Pest detected")]
        PestDetected = 0x06,
        [Description("0x07 Pest exterminated (location provided)")]
        PestExterminatedWithLocation = 0x07,
        [Description("0x08 Pest exterminated")]
        PestExterminated = 0x08,
        [Description("0xFE Unknown event")]
        UnknownEvent = 0xFE
    }

    public enum NotificationLightSensorEvents : byte
    {
        [Description("0x00 Idle")]
        StateIdle = 0x00,
        [Description("0x01 Light detected")]
        LightDetected = 0x01,
        [Description("0x02 Light color transition detected")]
        LightColorTransitionDetected = 0x02
    }

    public enum NotificationWaterQualityMonitoringEvents : byte
    {
        [Description("0x00 Idle")]
        StateIdle = 0x00,
        [Description("0x01 Chlorine alarm")]
        ChlorineAlarm = 0x01,
        [Description("0x02 Acidity (pH) alarm")]
        Acidity_pH_Alarm = 0x02,
        [Description("0x03 Water Oxidation alarm")]
        WaterOxidationAlarm = 0x03,
        [Description("0x04 Chlorine empty")]
        ChlorineEmpty = 0x04,
        [Description("0x05 Acidity (pH) empty")]
        Acidity_pH_Empty = 0x05,
        [Description("0x06 Waterflow measureing station shortage detected")]
        WaterflowMeasuringStationShortageDetected = 0x06,
        [Description("0x07 Waterflow clear water shortage detected")]
        WaterflowClearWaterShortageDetected = 0x07,
        [Description("0x08 Desinfection system error detected")]
        DisinfectionSystemErrorDetected = 0x08,
        [Description("0x09 Filter cleaning ongoing")]
        FilterCleaningOngoing = 0x09,
        [Description("0x0A heating operation ongoing")]
        HeatingOperationOngoing = 0x0A,
        [Description("0x0B Filter pump operation ongoing")]
        FilterPumpOperationOngoing = 0x0B,
        [Description("0x0C Freshwater operation ongoing")]
        FreshwaterOperationOngoing = 0x0C,
        [Description("0x0D Dry protection operation active")]
        DryProtectionOperationActive = 0x0D,
        [Description("0x0E Water tank is empty")]
        WaterTankIsEmpty = 0x0E,
        [Description("0x0F Water tank level is unknown")]
        WaterTankLevelIsUnknown = 0x0F,
        [Description("0x10 Water tank is full")]
        WaterTankIsFull = 0x10,
        [Description("0x11 Collective disorder")]
        CollectiveDisorder = 0x11
    }

    public enum NotificationHomeMonitoringEvents : byte
    {
        [Description("0x00 Idle")]
        StateIdle = 0x00,
        [Description("0x01 Home occupied (location provided)")]
        HomeOccupiedWithLocation = 0x01,
        [Description("0x02 Home occupied")]
        HomeOccupied = 0x02
    }
    #endregion // Notification<Type>Events enums
}
