// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
// SPDX-FileCopyrightText: 2025 Z-Wave Alliance https://z-wavealliance.org
using System;
using System.Collections.Generic;
using System.Threading;
using ZWave;
using ZWave.BasicApplication.Devices;

namespace Zats.Services
{
    public class CommandClassesSupportHandlerModify : CommandClassesSupportHandler
    {
        public List<CommandClassSupport> GetDefaultCommandClassSupportsForCCIdsQuirk(List<byte> commandClassIds)
        {
            var ret = new List<CommandClassSupport>();

            foreach (var commandClassId in commandClassIds)
            {
                var ccSupport = CreateDefaultSupportCommandClassByIdQuirk(commandClassId);

                // Silently skip unsupported
                if (ccSupport != null)
                    ret.Add(ccSupport);
            }

            return ret;
        }

        /// <summary>
        /// /// Version and Multichannel are skipped!
        /// </summary>
        /// <param name="commandClassId"></param>
        /// <returns></returns>
        public CommandClassSupport CreateDefaultSupportCommandClassByIdQuirk(byte commandClassId)
        {
            switch (commandClassId)
            {
                case COMMAND_CLASS_VERSION.ID:
                    #region
                    return new VersionSupport() { Version = 2 };
                #endregion

                case COMMAND_CLASS_ASSOCIATION_GRP_INFO.ID:
                    #region
                    return new AssociationGroupInfoSupport(GetDummyFloodSensorAssociationSupportData()) { Version = 1 };
                #endregion

                case COMMAND_CLASS_ASSOCIATION_V2.ID:
                    #region
                    return new AssociationSupport(GetDummyFloodSensorAssociationSupportData()) { Version = 2 };
                #endregion

                case COMMAND_CLASS_SWITCH_BINARY_V2.ID:
                    #region
                    return new BinarySwitchSupport(new BinarySwitchSupportData()) { Version = 1 };
                #endregion

                case COMMAND_CLASS_CONFIGURATION_V4.ID:
                    #region
                    return new ConfigurationSupport(GetDummyConfigurationSupportData()) { Version = 1 };
                #endregion

                case COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.ID:
                    #region
                    AutoResetEvent _fwuMDCompleted = new AutoResetEvent(false);
                    return new FirmwareUpdateMdSupportBasicModify(() => { _fwuMDCompleted.Set(); }) { Version = 2 };
                #endregion

                case COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.ID:
                    #region
                    return new ManufacturerSpecificSupport(GetDummyFloodSensorManufacturerSpecificSupportData()) { Version = 2 };
                #endregion

                case COMMAND_CLASS_METER_V6.ID:
                    #region
                    return new MeterSupport(GetDummyMeterSupportData()) { Version = 4 };
                #endregion

                case COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID:
                    #region
                    return new ZWavePlusInfoSupport(GetDummyFloodSensorZWavePlusInfoSupportData()) { Version = 2 };
                #endregion

                case COMMAND_CLASS_DEVICE_RESET_LOCALLY.ID:
                    #region
                    return new DeviceResetLocallySupportModify(COMMAND_CLASS_DEVICE_RESET_LOCALLY.ID) { Version = 1 };
                #endregion

                case COMMAND_CLASS_POWERLEVEL.ID:
                    #region
                    return new PowerlevelSupportModify(COMMAND_CLASS_POWERLEVEL.ID) { Version = 1 };
                #endregion

                case COMMAND_CLASS_SWITCH_ALL.ID:
                    #region
                    return new SwitchAllSupportModify(COMMAND_CLASS_SWITCH_ALL.ID) { Version = 1 };
                #endregion

                case COMMAND_CLASS_PROTECTION.ID:
                    #region
                    return new ProtectionSupportModify(COMMAND_CLASS_PROTECTION.ID) { Version = 2 };
                #endregion

                case COMMAND_CLASS_SCENE_ACTUATOR_CONF.ID:
                    #region
                    return new SceneActuatorConfSupportModify(COMMAND_CLASS_SCENE_ACTUATOR_CONF.ID) { Version = 1 };
                #endregion

                case COMMAND_CLASS_SCENE_ACTIVATION.ID:
                    #region
                    return new SceneActivationSupportModify(COMMAND_CLASS_SCENE_ACTIVATION.ID) { Version = 1 };
                #endregion

                case COMMAND_CLASS_ALARM.ID:
                    #region
                    return new NotificationSupport( GetDummyNotificationSupportData() ) { Version = 5 };
                #endregion

                case COMMAND_CLASS_BASIC.ID:
                    #region
                    return new BasicSupport(GetDummyBasicSupportData(), GetDummyBasicSupportData().MappedSupportData) { Version = 1 };
                #endregion

                case COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V2.ID:
                    #region
                    return new MultiChannelAssociationSupport(GetDummyFloodSensorAssociationSupportData()) { Version = 2 };
                #endregion

                case COMMAND_CLASS_SENSOR_ALARM.ID:
                    #region
                    return new SensorAlarmSupportModify(GetDummySensorAlarmSupportData()) { Version = 1 }; ;
                #endregion

                case COMMAND_CLASS_WAKE_UP.ID:
                    #region
                    return new WakeUpSupport(GetDummyWakeUpSupportData()) { Version = 2 };
                #endregion

                case COMMAND_CLASS_APPLICATION_STATUS.ID:
                    #region
                    return new ApplicationStatusSupportModify(COMMAND_CLASS_APPLICATION_STATUS.ID) { Version = 1 };
                #endregion

                case COMMAND_CLASS_BATTERY.ID:
                    #region
                    return new BatterySupport(new BatterySupportData()) { Version = 1 };
                #endregion

                case COMMAND_CLASS_CRC_16_ENCAP.ID:
                    #region
                    return new Crc16EncapSupportModify(COMMAND_CLASS_CRC_16_ENCAP.ID) { Version = 1 };
                #endregion

                case COMMAND_CLASS_SENSOR_MULTILEVEL.ID:
                    #region
                    return new MultilevelSensorSupport(GetDummyMultilevelSensorSupportData()) { Version = 5 };
                #endregion

                default:
                    return null;
            }
        }

        public Action GetDummySetNewImageCompletedCallback()
        {
            return null;
        }

        public MultilevelSensorSupportData GetDummyMultilevelSensorSupportData()
        {
            var multilevelSensorSupportData = new MultilevelSensorSupportData();

            multilevelSensorSupportData.Sensors = new List<MultilevelSensorType>()
            { 
                new MultilevelSensorType("Air Temperature", 0x02, new List<MultilevelSensorScale>()
                { 
                    new MultilevelSensorScale("Degree Celsius", 0x00) { Value = 24.54 }
                })
            };

            return multilevelSensorSupportData;
        }

        public NotificationSupportData GetDummyNotificationSupportData()
        {
            var notificationSupportData = new NotificationSupportData()
            {
                V1Alarm = false
            };

            notificationSupportData.AddNotificationType(NotificationTypeNames.WaterAlarm, NotificationStatusNames.UnsolicitedReportsEnabled, new List<byte>() { 0xFE } );
            notificationSupportData.AddNotificationType(NotificationTypeNames.HomeSecurity, NotificationStatusNames.UnsolicitedReportsEnabled, new List<byte>() { 0x03 });

            return notificationSupportData;
        }

        private BasicSupportData GetDummyBasicSupportData()
        {
            var basicSupportData = new BasicSupportData()
            {

            };

            return basicSupportData;
        }
        private SensorAlarmSupportData GetDummySensorAlarmSupportData()
        {
            var sensorAlarmSupportData = new SensorAlarmSupportData();

            sensorAlarmSupportData.AddSensorAlarm(SensorAlarmTypes.GeneralPurposeAlarm, 0x00, new List<byte>() { 0x00, 0x00 });
            sensorAlarmSupportData.AddSensorAlarm(SensorAlarmTypes.WaterLeakAlarm, 0x00, new List<byte>() { 0x00, 0x00 });

            return sensorAlarmSupportData;
        }

        private WakeUpSupportData GetDummyWakeUpSupportData()
        {
            var wakeUpSupportData = new WakeUpSupportData();

            wakeUpSupportData.MinimumWakeUpInterval = 60;
            wakeUpSupportData.MaximumWakeUpInterval = 86400;
            wakeUpSupportData.DefaultWakeUpInterval = 21600;
            wakeUpSupportData.WakeUpIntervalStep = 1;

            wakeUpSupportData.WakeUpOnDemandSupport = false;

            return wakeUpSupportData;


        }

        private AssociationSupportData GetDummyFloodSensorAssociationSupportData()
        {
            var associationSupportData = new AssociationSupportData();

            associationSupportData.AssociationGroups.Add(new AssociationGroupEx(1, 0x01, "Lifeline", 0x01, new byte[] { 0x5A, 0x01, 0x71, 0x05, 0x31, 0x05, 0x80, 0x03 }));
            associationSupportData.AssociationGroups.Add(new AssociationGroupEx(2, 0x0A, "Flood Control", 0x01, new byte[] { 0x20, 0x01 }));
            associationSupportData.AssociationGroups.Add(new AssociationGroupEx(3, 0x0A, "Flood Alarm", 0x71, new byte[] { 0x71, 0x05 }));
            associationSupportData.AssociationGroups.Add(new AssociationGroupEx(4, 0x0A, "Tamper Alarm", 0x71, new byte[] { 0x71, 0x05 }));

            return associationSupportData;
        }

        private AssociationSupportData GetDummyAssociationSupportData()
        {
            var associationSupportData = new AssociationSupportData();

            associationSupportData.AssociationGroups.Add(new AssociationGroupEx(1, 5, "Lifeline", 0x0001, new byte[] {
                    COMMAND_CLASS_DEVICE_RESET_LOCALLY.ID, COMMAND_CLASS_DEVICE_RESET_LOCALLY.DEVICE_RESET_LOCALLY_NOTIFICATION.ID,
                    COMMAND_CLASS_SWITCH_BINARY_V2.ID, COMMAND_CLASS_SWITCH_BINARY_V2.SWITCH_BINARY_REPORT.ID, COMMAND_CLASS_NOTIFICATION_V3.ID, COMMAND_CLASS_METER.ID, 0x05, 0x02

                }));
            //associationSupportData.AssociationGroups.Add(new AssociationGroupEx(1, 5, "Lifeline", 0x0001, new byte[] {
            //    COMMAND_CLASS_DEVICE_RESET_LOCALLY.ID, COMMAND_CLASS_DEVICE_RESET_LOCALLY.DEVICE_RESET_LOCALLY_NOTIFICATION.ID,
            //    COMMAND_CLASS_SWITCH_BINARY.ID, COMMAND_CLASS_SWITCH_BINARY.SWITCH_BINARY_REPORT.ID,
            //    COMMAND_CLASS_INDICATOR.ID, COMMAND_CLASS_INDICATOR.INDICATOR_REPORT.ID}));

            //associationSupportData.AssociationGroups.Add(new AssociationGroupEx(2, 5, "Dummy Group 1", 0x0000, new byte[] {
            //    COMMAND_CLASS_ALARM.ID, COMMAND_CLASS_ALARM.ALARM_REPORT.ID }));

            //associationSupportData.AssociationGroups.Add(new AssociationGroupEx(3, 5, "Dummy Group 2", 0x0000, new byte[] {
            //     COMMAND_CLASS_ALARM.ID, COMMAND_CLASS_ALARM.ALARM_REPORT.ID }));
            return associationSupportData;
        }

        public SceneSupportData GetDummySceneSupportData()
        {
            var sceneSupportData = new SceneSupportData();
            return sceneSupportData;
        }

        private ConfigurationSupportData GetDummyConfigurationSupportData()
        {
            var configurationSupportData = new ConfigurationSupportData();
            configurationSupportData.Params.Add(new ConfigurationParam(1, 1, 0, 0, 100, 30, ConfigurationParameterFormatNames.UnsignedInt, "Param 1", ""));
            configurationSupportData.Params.Add(new ConfigurationParam(300, 1, 55, -20, 80, -10, ConfigurationParameterFormatNames.SignedInt, "Param 2", "signed negative", false, false, false));
            configurationSupportData.Params.Add(new ConfigurationParam(2, 1, 2, 0, 5, 2, ConfigurationParameterFormatNames.SignedInt, "Param 3", "readonly property set", true, false, false));
            return configurationSupportData;
        }

        private ManufacturerSpecificSupportData GetDummyManufacturerSpecificSupportData()
        {
            return new ManufacturerSpecificSupportData()
            {
                // Manufacturer Specific
                ManufacturerId = new byte[2] { 0x00, 0x00 }, // MFG_ID_ZWAVE
                ProductTypeId = new byte[2] { 0x00, 0xBB },
                ProductId = new byte[2] { 0x00, 0x01 },
                // Device Specific
                DeviceIdDataFormat1 = DeviceIdDataFormatNames.UTF8,
                DeviceIdDataFormat2 = DeviceIdDataFormatNames.UTF8,
                DefaultDeviceIdType = DeviceIdTypeNames.SerialNumber,
                SerialNumber = new byte[] { 0x32, 0x32, 0x32 },
                PseudoRandom = new byte[] { 0x31, 0x32, 0x33, 0x34 }
            };
        }

        private ManufacturerSpecificSupportData GetDummyFloodSensorManufacturerSpecificSupportData()
        {
            return new ManufacturerSpecificSupportData()
            {
                // Manufacturer Specific
                ManufacturerId = new byte[2] { 0x01, 0x0F }, // MFG_ID_ZWAVE
                ProductTypeId = new byte[2] { 0x0B, 0x01 },
                ProductId = new byte[2] { 0x10, 0x02 },
                // Device Specific
                DeviceIdDataFormat1 = DeviceIdDataFormatNames.UTF8,
                DeviceIdDataFormat2 = DeviceIdDataFormatNames.UTF8,
                DefaultDeviceIdType = DeviceIdTypeNames.SerialNumber,
                SerialNumber = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0xB0 },
                PseudoRandom = new byte[] { 0x31, 0x32, 0x33, 0x34 }
            };
        }

        private MeterSupportData GetDummyMeterSupportData()
        {
            var meterSupportData = new MeterSupportData()
            {
                MeterType = MeterTypeNames.Electric,
                MeterResetSupported = true,
                DefaultScale = 0x00,
                SupportedRateTypes = RateTypeNames.Import
            };

            meterSupportData.MeterValues = new List<MeterValue>
            {
                new MeterValue() { ScaleNumber = ScaleNames.ScalekWh, SizeConsuming = 2, PrecisionConsuming = 2, ValueConsuming = 1.23 },
                new MeterValue() { ScaleNumber = ScaleNames.ScaleW, SizeConsuming = 2, PrecisionConsuming = 1, ValueConsuming = 123.4 },
                new MeterValue() { ScaleNumber = ScaleNames.ScaleV, SizeConsuming = 2, PrecisionConsuming = 1, ValueConsuming = 231.2 },
                new MeterValue() { ScaleNumber = ScaleNames.ScalekVar, SizeConsuming = 2, PrecisionConsuming = 2, ValueConsuming = 43.21 }
            };

            return meterSupportData;
        }

        private ZWavePlusInfoSupportData GetDummyZWavePlusInfoSupportData()
        {
            var zWavePlusInfoSupportData = new ZWavePlusInfoSupportData()
            {
                ZWaveVersion = 0x02,
                // 0x00: ZWAVEPLUS_INFO_REPORT_ROLE_TYPE_CONTROLLER_CENTRAL_STATIC
                // 0x01: ZWAVEPLUS_INFO_REPORT_ROLE_TYPE_CONTROLLER_SUB_STATIC
                // 0x02: ZWAVEPLUS_INFO_REPORT_ROLE_TYPE_CONTROLLER_PORTABLE
                // 0x03: ZWAVEPLUS_INFO_REPORT_ROLE_TYPE_CONTROLLER_PORTABLE_REPORTING
                // 0x04: ZWAVEPLUS_INFO_REPORT_ROLE_TYPE_END_NODE_PORTABLE
                // 0x05: ZWAVEPLUS_INFO_REPORT_ROLE_TYPE_END_NODE_ALWAYS_ON
                // 0x06: ZWAVEPLUS_INFO_REPORT_ROLE_TYPE_END_NODE_SLEEPING_REPORTING
                // 0x07: ZWAVEPLUS_INFO_REPORT_ROLE_TYPE_END_NODE_SLEEPING_LISTENING
                RoleType = ZPIRoleTypes.EndNodeAlwaysOn,
                // 0x00: ZWAVEPLUS_INFO_REPORT_NODE_TYPE_ZWAVEPLUS_NODE
                // 0x01: ZWAVEPLUS_INFO_REPORT_NODE_TYPE_ZWAVEPLUS_FOR_IP_ROUTER
                // 0x02: ZWAVEPLUS_INFO_REPORT_NODE_TYPE_ZWAVEPLUS_FOR_IP_GATEWAY
                // 0x03: ZWAVEPLUS_INFO_REPORT_NODE_TYPE_ZWAVEPLUS_FOR_IP_CLIENT_IP_NODE
                // 0x04: ZWAVEPLUS_INFO_REPORT_NODE_TYPE_ZWAVEPLUS_FOR_IP_CLIENT_ZWAVE_NODE
                NodeType = ZPINodeTypes.ZWavePlusNode,
                InstallerIconType = new byte[] { 0x07, 0x02 },
                UserIconType = new byte[] { 0x07, 0x02 }
            };
            return zWavePlusInfoSupportData;
        }

        private ZWavePlusInfoSupportData GetDummyFloodSensorZWavePlusInfoSupportData()
        {
            var zWavePlusInfoSupportData = new ZWavePlusInfoSupportData()
            {
                ZWaveVersion = 0x01,
                // 0x00: ZWAVEPLUS_INFO_REPORT_ROLE_TYPE_CONTROLLER_CENTRAL_STATIC
                // 0x01: ZWAVEPLUS_INFO_REPORT_ROLE_TYPE_CONTROLLER_SUB_STATIC
                // 0x02: ZWAVEPLUS_INFO_REPORT_ROLE_TYPE_CONTROLLER_PORTABLE
                // 0x03: ZWAVEPLUS_INFO_REPORT_ROLE_TYPE_CONTROLLER_PORTABLE_REPORTING
                // 0x04: ZWAVEPLUS_INFO_REPORT_ROLE_TYPE_END_NODE_PORTABLE
                // 0x05: ZWAVEPLUS_INFO_REPORT_ROLE_TYPE_END_NODE_ALWAYS_ON
                // 0x06: ZWAVEPLUS_INFO_REPORT_ROLE_TYPE_END_NODE_SLEEPING_REPORTING
                // 0x07: ZWAVEPLUS_INFO_REPORT_ROLE_TYPE_END_NODE_SLEEPING_LISTENING
                RoleType = ZPIRoleTypes.EndNodeSleepingReporting,
                // 0x00: ZWAVEPLUS_INFO_REPORT_NODE_TYPE_ZWAVEPLUS_NODE
                // 0x01: ZWAVEPLUS_INFO_REPORT_NODE_TYPE_ZWAVEPLUS_FOR_IP_ROUTER
                // 0x02: ZWAVEPLUS_INFO_REPORT_NODE_TYPE_ZWAVEPLUS_FOR_IP_GATEWAY
                // 0x03: ZWAVEPLUS_INFO_REPORT_NODE_TYPE_ZWAVEPLUS_FOR_IP_CLIENT_IP_NODE
                // 0x04: ZWAVEPLUS_INFO_REPORT_NODE_TYPE_ZWAVEPLUS_FOR_IP_CLIENT_ZWAVE_NODE
                NodeType = ZPINodeTypes.ZWavePlusNode,
                InstallerIconType = new byte[] { 0x0C, 0x05 },
                UserIconType = new byte[] { 0x0C, 0x05 }
            };
            return zWavePlusInfoSupportData;
        }

    }
}
