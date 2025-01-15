// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Utils;
using Utils.UI.Enums;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;

// ToDo: Supervision Report Status 'Working' for NotificationSet (optional)

namespace Zats.Services
{
    public class NotificationSupport : CommandClassSupport
    {
        private NotificationSupportData NotificationSupportData { get; set; }

        //private readonly static string CommandClassName = "Notification CC";
        //private string EndPointName = "";
        //private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public const int NotificationTypesCount = 0x17; // Known Notification Type area is 0x01 to 0x16
        public const int EventStatesCount = 0x4C;       // Known Event Type area is 0x00 to 0x4C
        public const int HighestV2AlarmType = 0x0B;

        public NotificationSupport()
            : this(new NotificationSupportData() { })
        { }

        public NotificationSupport(NotificationSupportData notificationSupportData)
            : base(COMMAND_CLASS_NOTIFICATION_V3.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 8;
            NotificationSupportData = notificationSupportData;
            //LogLevel = NotificationSupportData.LogLevel;
        }

        protected override void OnHandledDelayed(DataReceivedUnit ou)
        {
            ou.SetNextActionItems();
            if (DstNode.EndPointId == ReceivedAchData.DstNode.EndPointId)
            {
                var sendDstNode = ReceivedAchData.DstNode;
                var sendSrcNode = ReceivedAchData.SrcNode;
                byte[] command = ReceivedAchData.Command;
                var scheme = (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme;
                bool isResponseAllowed = IsResponseAllowed(Network, command, scheme, Secure);

                ResultForSv = SvResultNames.Undefined;
                if (isResponseAllowed && command != null && command.Length > 1 && NotificationSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_ALARM.ALARM_GET.ID && command.Length == 3)
                    {
                        rpt = PrepareAlarmV1Report(command);
                    }
                    else if (command[1] == COMMAND_CLASS_ALARM_V2.ALARM_GET.ID && command.Length == 4)
                    {
                        rpt = PrepareAlarmV2Report(command);
                    }

                    else if (command[1] == COMMAND_CLASS_NOTIFICATION_V3.NOTIFICATION_GET.ID && command.Length == 5)
                    {
                        rpt = PrepareNotificationReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_NOTIFICATION_V3.NOTIFICATION_SUPPORTED_GET.ID)
                    {
                        rpt = PrepareNotificationSupportedReport();
                    }
                    else if (command[1] == COMMAND_CLASS_NOTIFICATION_V3.EVENT_SUPPORTED_GET.ID)
                    {
                        rpt = PrepareEventSupportedReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_NOTIFICATION_V3.NOTIFICATION_SET.ID)
                    {
                        ResultForSv = HandleNotificationSet(command);
                    }
                    else if (command[1] == COMMAND_CLASS_ALARM_V2.ALARM_SET.ID)
                    {
                        ResultForSv = HandleAlarmSet(command);
                    }

                    if (rpt != null)
                    {
                        ApiOperation sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command);
                }
            }
        }

        private byte[] PrepareAlarmV1Report(byte[] command)
        {

            if (Version >= 1 && Version <= 8)
            {
                COMMAND_CLASS_ALARM.ALARM_GET getCmd = command;
                COMMAND_CLASS_ALARM.ALARM_REPORT rpt = new COMMAND_CLASS_ALARM.ALARM_REPORT() { };
                if (!NotificationSupportData.V1Alarm)
                {
                    rpt.alarmType = 0;
                    rpt.alarmLevel = 0;
                }
                else
                {
                    rpt.alarmType = getCmd.alarmType;
                    rpt.alarmLevel = 0;
                }
                return rpt;
            }
            return null;
        }

        private byte[] PrepareAlarmV2Report(byte[] command)
        {
            if (Version >= 2 && Version <= 8)
            {
                COMMAND_CLASS_ALARM_V2.ALARM_GET getCmd = command;
                COMMAND_CLASS_ALARM_V2.ALARM_REPORT rpt = new COMMAND_CLASS_ALARM_V2.ALARM_REPORT() { };
                byte alarmType = getCmd.alarmType;
                byte zwaveAlarmType = getCmd.zwaveAlarmType;
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
                    requestedNotification = NotificationSupportData.Notifications.Find(type => type.NotificationType <= HighestV2AlarmType);
                }
                else
                {
                    requestedNotification = NotificationSupportData.Notifications.Find(type => type.NotificationType == zwaveAlarmType);
                }
                if (requestedNotification != null)
                {
                    // Table 90 (SDS13781 2020B), Push node responses to V2 Alarm Get (example)
                    rpt.alarmType = 0;
                    rpt.alarmLevel = 0;
                    rpt.zwaveAlarmStatus = requestedNotification.NotificationStatus;    // 0x00 or 0xFF (unsolicited OFF or ON)
                    rpt.zwaveAlarmType = requestedNotification.NotificationType;
                    rpt.zwaveAlarmEvent = 0xFE;                                         // Idle state
                    rpt.zensorNetSourceNodeId = 0;
                    rpt.numberOfEventParameters = 0;
                    if (NotificationSupportData.IsPullNode)
                    {
                        rpt.zwaveAlarmStatus = 0x00;                                    // 0x00: Simulate empty queue (CC:0071.04.00.21.006)
                        rpt.zwaveAlarmEvent = 0xFE;                                     // Idle state
                    }
                    return rpt;
                }
            }
            return null;
        }

        private byte[] PrepareNotificationReport(byte[] command)
        {
            if (Version >= 3 && Version <= 8)
            {
                COMMAND_CLASS_NOTIFICATION_V3.NOTIFICATION_GET getCmd = command;
                COMMAND_CLASS_NOTIFICATION_V3.NOTIFICATION_REPORT rpt = new COMMAND_CLASS_NOTIFICATION_V3.NOTIFICATION_REPORT() { };
                byte v1AlarmType = getCmd.v1AlarmType;
                byte requestedType = getCmd.notificationType;
                byte requestedEvent = getCmd.mevent;
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
                    for (requestedType = 1; requestedType < NotificationSupport.NotificationTypesCount; requestedType++)
                    {
                        if ((NotificationSupportData.NotificationSupportedBitmask[requestedType / 8] & (1 << (requestedType % 8))) != 0)
                            break;
                    }
                }
                requestedNotification = NotificationSupportData.Notifications.Find(type => type.NotificationType == requestedType);
                if (requestedNotification != null)
                {
                    rpt.v1AlarmType = 0;
                    rpt.v1AlarmLevel = 0;
                    rpt.notificationStatus = NotificationSupportData.IsPullNode ? (byte) 0xFE : requestedNotification.NotificationStatus;
                    rpt.notificationType = requestedType;
                    if (NotificationSupportData.IsPullNode)
                    {
                        rpt.mevent = 0xFE;  // CC:0071.03.05.11.005 for Pull Nodes (0xFE = empty)
                    }
                    else
                    {
                        rpt.mevent = 0xFE;  // CC:0071.03.04.11.009 for unsupported events (Push Nodes)
                        if ((requestedEvent / 8) < requestedNotification.EventSupportedBitmask.Count)
                        {
                            if ((requestedNotification.EventSupportedBitmask[requestedEvent / 8] & (1 << (requestedEvent % 8))) != 0)
                            {
                                rpt.mevent = requestedNotification.EventState;
                            }
                        }
                    }
                    rpt.properties1.eventParametersLength = 0;
                    if (requestedNotification.SequenceNumber != 0)
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

        private byte[] PrepareNotificationSupportedReport()
        {
            if (Version >= 1 && Version <= 8)
            {
                COMMAND_CLASS_NOTIFICATION_V3.NOTIFICATION_SUPPORTED_REPORT rpt = new COMMAND_CLASS_NOTIFICATION_V3.NOTIFICATION_SUPPORTED_REPORT() { };
                rpt.properties1.numberOfBitMasks = (byte) NotificationSupportData.NotificationSupportedBitmask.Count;
                rpt.properties1.v1Alarm = (byte) (NotificationSupportData.V1Alarm ? 0x01 : 0x00);
                rpt.bitMask = NotificationSupportData.NotificationSupportedBitmask;
                return rpt;
            }
            return null;
        }

        private byte[] PrepareEventSupportedReport(byte[] command)
        {
            if (Version >= 3 && Version <= 8)
            {
                COMMAND_CLASS_NOTIFICATION_V3.EVENT_SUPPORTED_GET getCmd = command;
                COMMAND_CLASS_NOTIFICATION_V3.EVENT_SUPPORTED_REPORT rpt = new COMMAND_CLASS_NOTIFICATION_V3.EVENT_SUPPORTED_REPORT() { };
                byte requestedType = getCmd.notificationType;
                NotificationTypeData requestedNotification = NotificationSupportData.Notifications.Find(type => type.NotificationType == requestedType);
                if (requestedNotification != null)
                {
                    rpt.notificationType = requestedType;
                    rpt.properties1.numberOfBitMasks = (byte)requestedNotification.EventSupportedBitmask.Count;
                    rpt.bitMask = requestedNotification.EventSupportedBitmask;
                    return rpt;
                }
                rpt.notificationType = requestedType;
                rpt.properties1.numberOfBitMasks = 0;
                rpt.bitMask = new List<byte> { };
                return rpt;
            }
            return null;
        }

        private SvResultNames HandleNotificationSet(byte[] command)
        {
            if (Version >= 3 && Version <= 8)
            {
                COMMAND_CLASS_NOTIFICATION_V3.NOTIFICATION_SET setCmd = command;
                byte requestedType = setCmd.notificationType;
                NotificationTypeData requestedNotification = NotificationSupportData.Notifications.Find(type => type.NotificationType == requestedType);
                byte newNotificationStatus = setCmd.notificationStatus;
                if (requestedNotification != null)
                {
                    if (NotificationSupportData.IsPullNode == false)
                    {
                        if (newNotificationStatus == 0x00 || newNotificationStatus == 0xFF)
                        {
                            requestedNotification.NotificationStatus = newNotificationStatus;
                            return SvResultNames.Successful;
                        }
                    }
                    else
                    {
                        if (newNotificationStatus == 0x00)
                        {
                            requestedNotification.NotificationStatus = newNotificationStatus;
                            return SvResultNames.Successful;
                        }
                    }
                }
            }
            return SvResultNames.Failed;
        }

        private SvResultNames HandleAlarmSet(byte[] command)
        {
            if (Version >= 1 && Version <= 2)
            {
                COMMAND_CLASS_ALARM_V2.ALARM_SET setCmd = command;
                byte requestedType = setCmd.zwaveAlarmType;
                NotificationTypeData requestedNotification = NotificationSupportData.Notifications.Find(type => type.NotificationType == requestedType);
                byte newNotificationStatus = setCmd.zwaveAlarmStatus;
                if (requestedNotification != null)
                {
                    if (NotificationSupportData.IsPullNode == false)
                    {
                        if (newNotificationStatus == 0x00 || newNotificationStatus == 0xFF)
                        {
                            requestedNotification.NotificationStatus = newNotificationStatus;
                            return SvResultNames.Successful;
                        }
                    }
                    else
                    {
                        if (newNotificationStatus == 0x00)
                        {
                            requestedNotification.NotificationStatus = newNotificationStatus;
                            return SvResultNames.Successful;
                        }
                    }
                }
            }
            return SvResultNames.Failed;
        }


        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, 
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != Id)
            {
                supervisionReport.status = (byte) SvStatusNames.NoSupport;
                return;
            }

            supervisionReport.status = (byte) SvStatusNames.Fail;  // FAIL as default
            switch (supervisionGet.encapsulatedCommand[1])
            {
                case COMMAND_CLASS_NOTIFICATION_V3.NOTIFICATION_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }
    }

    public class NotificationTypeData
    {
        public byte NotificationType;
        public byte NotificationStatus = 0x00;
        public List<byte> EventSupportedBitmask { get; set; } = new List<byte> { };
        // Internals
        public byte SequenceNumber { get; set; } = 0x00;
        public byte EventState { get; set; } = 0x00;

        public NotificationTypeData(byte notificationType, byte notificationStatus, List<byte> eventsSupported)
        {
            NotificationType = notificationType;
            NotificationStatus = notificationStatus;
            EventSupportedBitmask = new List<byte> { };
            foreach (byte eventType in eventsSupported)
            {
                byte byteIndex, bit;
                byteIndex = (byte) (eventType / 8);
                bit = (byte) (eventType % 8);
                while (byteIndex >= EventSupportedBitmask.Count)
                {
                    EventSupportedBitmask.Add(0x00);
                }
                EventSupportedBitmask[byteIndex] |= (byte) (1 << bit);
            }
        }
    }

    public class NotificationSupportData
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
        public List<NotificationTypeData> Notifications { get; set; } = new List<NotificationTypeData>();
        public bool IsPullNode { get; set; } = false;

        // Notification Supported Report
        public List<byte> NotificationSupportedBitmask { get; set; } = new List<byte> { 0x00 };
        public bool V1Alarm { get; set; } = false;

        public NotificationTypeData AddNotificationType(NotificationTypeNames notificationTypeName, byte notificationStatus, List<byte> eventsSupported)
        {
            byte notificationType = (byte)notificationTypeName;
            if (notificationType >= 0x01 && notificationType < NotificationSupport.NotificationTypesCount)
            {
                if (Notifications.Find(type => type.NotificationType == notificationType) == null)
                {
                    NotificationTypeData obj = new NotificationTypeData(notificationType, notificationStatus, eventsSupported);
                    Notifications.Add(obj);
                    UpdateNotificationSupportedBitmask(notificationType);
                    return obj;
                }
            }
            return null;
        }
        private void UpdateNotificationSupportedBitmask(byte notificationType)
        {
            byte byteIndex, bit;
            if (notificationType >= 0x01 && notificationType < NotificationSupport.NotificationTypesCount)
            {
                byteIndex = (byte) (notificationType / 8);
                bit = (byte) (notificationType % 8);
                while (byteIndex >= NotificationSupportedBitmask.Count)
                {
                    NotificationSupportedBitmask.Add(0x00);
                }
                NotificationSupportedBitmask[byteIndex] |= (byte) (1 << bit);
            }
        }
    }

    public enum NotificationTypeNames : byte
    {
        SmokeAlarm = 0x01,
        COAlarm = 0x02,
        CO2Alarm = 0x03,
        HeatAlarm = 0x04,
        WaterAlarm = 0x05,
        AccessControl = 0x06,
        HomeSecurity = 0x07,
        PowerManagement = 0x08,
        System = 0x09,
        EmergencyAlarm = 0x0A,
        Clock = 0x0B,
        Appliance = 0x0C,
        HomeHealth = 0x0D,
        Siren = 0x0E,
        WaterValve = 0x0F,
        WeatherAlarm = 0x10,
        Irrigation = 0x11,
        GasAlarm = 0x12,
        PestControl = 0x13,
        LightSensor = 0x14,
        WaterQualityMonitoring = 0x15,
        HomeMonitoring = 0x16,
    };

    public enum NotificationSmokeAlarmEvents : byte
    {
        StateIdle = 0x00,
        SmokeDetectedWithLocation = 0x01,
        SmokeDetected = 0x02,
        SmokeAlarmTest = 0x03,
        AlarmSilenced = 0x06,
        ReplacementRequired = 0x04,
        ReplacementRequiredEndOfLife = 0x05,
        MaintenanceRequiredPlannedPeriodicInspection = 0x07,
        MaintenanceRequiredDustInDevice = 0x08, 
        UnknownEvent = 0xFE
    }

    public enum NotificationCOAlarmEvents : byte
    {
        StateIdle = 0x00,
        CarbonMonoxideDetectedWithLocation = 0x01,
        CarbonMonoxideDetected = 0x02,
        CarbonMonoxideTest = 0x03,
        ReplacementRequired = 0x04,
        ReplacementRequiredEndOfLife = 0x05,
        AlarmSilenced = 0x06,
        MaintenanceRequiredPlannedPeriodicInspection = 0x07,
        UnknownEvent = 0xFE
    }

    public enum NotificationCO2AlarmEvents : byte
    {
        StateIdle = 0x00,
        CarbonDioxideDetectedWithLocation = 0x01,
        CarbonDioxideDetected = 0x02,
        CarbonDioxideTest = 0x03,
        ReplacementRequired = 0x04,
        ReplacementRequiredEndOfLife = 0x05,
        AlarmSilenced = 0x06,
        MaintenanceRequiredPlannedPeriodicInspection = 0x07,
        UnknownEvent = 0xFE
    }

    public enum NotificationHeatAlarmEvents : byte
    {
        StateIdle = 0x00,
        OverheatDetectedWithLocation = 0x01,
        OverheatDetected = 0x02,
        UnderHeatDetectedWithLocation = 0x05,
        UnderHeatDetected = 0x06,
        RapidTemperatureRiseWithLocation = 0x03,
        RapidTemperatureRise = 0x04,
        RapidTemperatureFallWithLocation = 0x0C,
        RapidTemperatureFall = 0x0D,
        HeatAlarmTest = 0x07,
        AlarmSilenced = 0x09,
        ReplacementRequiredEndOfLife = 0x08,
        MaintenanceRequiredDustInDevice = 0x0A,
        MaintenanceRequiredPlannedPeriodicInspection = 0x0B,
        UnknownEvent = 0xFE
    }

    public enum NotificationWaterAlarmEvents : byte
    {
        StateIdle = 0x00,
        WaterLeakDetectedWithLocation = 0x01,
        WaterLeakDetected = 0x02,
        WaterLevelDroppedWithLocation = 0x03,
        WaterLevelDropped = 0x04,
        ReplaceWaterFilter = 0x05,
        WaterFlowAlarm = 0x06,
        WaterPressureAlarm = 0x07,
        WaterTemperatureAlarm = 0x08, 
        WaterLevelAlarm = 0x09,
        SumpPumpActive = 0x0A,
        SumpPumpFailure = 0x0B,
        UnknownEvent = 0xFE
    }

    public enum NotificationAccessControlEvents : byte
    {
        StateIdle = 0x00, 
        ManualLockOperation = 0x01,
        ManualUnlockOperation = 0x02,
        RfLockOperation = 0x03,
        RfUnlockOperation = 0x04,
        KeypadLockOperation = 0x05,
        KeypadUnlockOperation = 0x06,
        ManualNotFullyLockedOperation = 0x07,
        RfNotFullyLockedOperation = 0x08,
        AutoLockLockedOperation = 0x09,
        AutoLockNotFullyLockedOperation = 0x0A,
        LockJammed = 0x0B,
        AllUserCodesDeleted = 0x0C,
        SingleUserCodeDeleted = 0x0D,
        NewUserCodeAdded = 0x0E,
        NewUserCodeNotAddedDueToDuplicateCode = 0x0F,
        KeypadTemporaryDisabled = 0x10,
        KeypadBusy = 0x11,
        NewProgramCodeEntered = 0x12,
        ManuallyEnterUserAccesCcodeExceedsCodeLimit = 0x13,
        UnlockByRfWithInvalidUserCode = 0x14,
        LockedByRfWithInvalidUserCode = 0x15,
        WindowOrDoorIsOpen = 0x16,
        WindowOrDoorIsClosed = 0x17,
        WindowOrDoorHandleIsOpen = 0x18,
        WindowOrDoorHandleIsClosed = 0x19,
        MessagingUserCodeEnteredViaKeypad = 0x20,
        BarrierPerformingInitializationProcess = 0x40,
        BarrierOperationForceHasBeenExceeded = 0x41,
        BarrierMotorHasExceededManufacturersOperationalTimeLimit = 0x42,
        BarrierOperationHasExceededPhysicalMechanicalLimits = 0x43,
        BarrierUnableToPerformRequestedOperationDueToULRequirements = 0x44,
        BarrierUnattendedOperationHasBeenDisabledPerULRequirements = 0x45,
        BarrierFailedToPerformRequestedOperation_DeviceMalfunction = 0x46,
        BarrierVacationMode = 0x47,
        BarrierSafetyBeamObstacle = 0x48,
        BarrierSensorNotDetected_SupervisoryError = 0x49,
        BarrierSensorLowBatteryWarning = 0x4A,
        BarrierDetectedShortInWallStationWires = 0x4B,
        BarrierAssociatedWithNonZWaveRemoteControl = 0x4C,
        UnknownEvent = 0xFE
    }

    public enum NotificationHomeSecurityEvents : byte
    {
        StateIdle = 0x00,
        IntrusionWithLocation = 0x01,
        Intrusion = 0x02,
        Tampering_ProductCoverRemoved = 0x03,
        Tampering_InvalidCode = 0x04,
        GlassBreakageWithLocation = 0x05,
        GlassBreakage = 0x06,
        MotionDetectionWithLocation = 0x07,
        MotionDetection = 0x08,
        TamperingProductMoved = 0x09,
        ImpactDetected = 0x0A,
        MagneticFieldInterferenceDetected = 0x0B,
        RfJammingDetected = 0x0C,
        UnknownEvent = 0xFE
    }

    public enum NotificationPowerManagementEvents : byte
    {
        StateIdle = 0x00,
        PowerHasBeenApplied = 0x01,
        ACmainsDisconnected = 0x02,
        ACmainsReconnected = 0x03,
        SurgeDetected = 0x04,
        VoltageDropOrDrift = 0x05,
        OverCurrentDetected = 0x06,
        OverVoltageDetected = 0x07,
        OverloadDetected = 0x08,
        LoadError = 0x09,
        ReplaceBatterySoon = 0x0A,
        ReplaceBatteryNow = 0x0B,
        BatteryFluidIsLow = 0x11,
        BatteryIsCharging = 0x0C,
        BatteryIsFullyCharged = 0x0D,
        ChargeBatterySoon = 0x0E,
        ChargeBatteryNow = 0x0F,
        BackupBatteryIsLow = 0x10,
        BackupBatteryDisconnected = 0x12,
        UnknownEvent = 0xFE
    }

    public enum NotificationSystemEvents : byte
    {
        StateIdle = 0x00,
        SystemHardwareFailure = 0x01,
        SystemHardwareFailure_CodeProvided = 0x03,
        SystemSoftwareFailure = 0x02,
        SystemSoftwareFailure_CodeProvided = 0x04,
        Heartbeat = 0x05,
        Tampering_ProductCoverRemoved = 0x06,
        EmergencyShutoff = 0x07,
        DigitalInputHighState = 0x09,
        DigitalInputLowState = 0x0A,
        DigitalInputOpen = 0x0B,
        UnknownEvent = 0xFE
    }

    public enum NotificationEmergencyAlarmEvents : byte
    {
        StateIdle = 0x00,
        ContactPolice = 0x01,
        ContactFireService = 0x02,
        ContactMedicalService = 0x03,
        PanicAlert = 0x04,
        UnknownEvent = 0xFE
    }

    public enum NotificationClockEvents : byte
    {
        StateIdle = 0x00,
        WakeUpAlert = 0x01,
        TimerEnded = 0x02,
        TimeRemaining = 0x03,
        UnknownEvent = 0xFE
    }

    public enum NotificationApplianceEvents : byte
    {
        StateIdle = 0x00,
        ProgramStarted = 0x01,
        ProgramInProgress = 0x02,
        ProgramCompleted = 0x03,
        ReplaceMainFilter = 0x04,
        SupplyingWater = 0x06,
        Boiling = 0x08,
        Washing = 0x0A,
        Rinsing = 0x0C,
        Draining = 0x0E,
        Spinning = 0x10,
        Drying = 0x12,
        FailureToSetTargetTemperature = 0x05,
        WaterSupplyFailure = 0x07,
        BoilingFailure = 0x09,
        WashingFailure = 0x0B,
        RinsingFailure = 0x0D,
        DrainingFailure = 0x0F,
        SpinningFailure = 0x11,
        DryingFailure = 0x13,
        FanFailure = 0x14,
        CompressorFailure = 0x15,
        UnknownEvent = 0xFE
    }

    public enum NotificationHomeHealthEvents : byte
    {
        StateIdle = 0x00,
        LeavingBed = 0x01,
        SittingOnBed = 0x02,
        LyingOnBed = 0x03,
        SittingOnBedEdge = 0x05,
        PostureChanged = 0x04,
        VolatileOrganicCompoundLevel = 0x06,
        SleepApneaDetected = 0x07,
        SleepStage0Detected_Dreaming_REM = 0x08,
        SleepStage1Detected_LightSleep_nonREM1 = 0x09,
        SleepStage2Detected_MediumSleep_nonREM2 = 0x0A,
        SleepStage3Detected_DeepSleep_nonREM3 = 0x0B,
        FallDetected = 0x0C,
        UnknownEvent = 0xFE
    }

    public enum NotificationSirenEvents : byte
    {
        StateIdle = 0x00,
        SirenActive = 0x01,
        UnknownEvent = 0xFE
    }

    public enum NotificationWaterValveEvents : byte
    {
        StateIdle = 0x00,
        ValveOperation = 0x01,
        MainValveOperation = 0x02,
        ValveShortCircuit = 0x03,
        MainValveShortCircuit = 0x04,
        ValveCurrentAlarm = 0x05,
        MainValveCurrentAlarm = 0x06,
        UnknownEvent = 0xFE
    }

    public enum NotificationWeatherAlarmEvents : byte
    {
        StateIdle = 0x00,
        RainAlarm = 0x01,
        MoistureAlarm = 0x02,
        FreezeAlarm = 0x03,
        UnknownEvent = 0xFE
    }

    public enum NotificationIrrigationEvents : byte
    {
        StateIdle = 0x00,
        ScheduleStarted = 0x01,
        ScheduleFinished = 0x02,
        ValveTableRunStarted = 0x03,
        ValveTableRunFinished = 0x04,
        DeviceIsNotConfigured = 0x05,
        UnknownEvent = 0xFE
    }

    public enum NotificationGasAlarmEvents : byte
    {
        StateIdle = 0x00,
        CombustibleGasDetectedWithLocation = 0x01,
        CombustibleGasDetected = 0x02,
        ToxicGasDetectedWithLocation = 0x03,
        ToxicGasDetected = 0x04,
        GasAlarmTest = 0x05,
        ReplacementRequired = 0x06,
        UnknownEvent = 0xFE
    }

    public enum NotificationPestControlEvents : byte
    {
        StateIdle = 0x00,
        TrapArmedWithLocation = 0x01,
        TrapArmed = 0x02,
        TrapRearmRequiredWithLocation = 0x03,
        TrapRearmRequired = 0x04,
        PestDetectedWithLocation = 0x05,
        PestDetected = 0x06,
        PestExterminatedWithLocation = 0x07,
        PestExterminated = 0x08,
        UnknownEvent = 0xFE
    }

    public enum NotificationLightSensorEvents : byte
    {
        StateIdle = 0x00, 
        LightDetected = 0x01,
        LightColorTransitionDetected = 0x02
    }

    public enum NotificationWaterQualityMonitoringEvents : byte
    {
        StateIdle = 0x00,
        ChlorineAlarm = 0x01,
        Acidity_pH_Alarm = 0x02,
        WaterOxidationAlarm = 0x03,
        ChlorineEmpty  = 0x04,
        Acidity_pH_Empty  = 0x05,
        WaterflowMeasuringStationShortageDetected = 0x06,
        WaterflowClearWaterShortageDetected = 0x07,
        DisinfectionSystemErrorDetected = 0x08,
        FilterCleaningOngoing = 0x09,
        HeatingOperationOngoing = 0x0A,
        FilterPumpOperationOngoing = 0x0B,
        FreshwaterOperationOngoing = 0x0C,
        DryProtectionOperationActive = 0x0D,
        WaterTankIsEmpty = 0x0E,
        WaterTankLevelIsUnknown = 0x0F,
        WaterTankIsFull = 0x10,
        CollectiveDisorder = 0x11
    }

    public enum NotificationHomeMonitoringEvents : byte
    {
        StateIdle = 0x00,
        HomeOccupiedWithLocation = 0x01,
        HomeOccupied = 0x02
    }


}
