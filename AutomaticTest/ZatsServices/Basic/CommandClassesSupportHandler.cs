// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
// SPDX-FileCopyrightText: Z-Wave Alliance https://z-wavealliance.org
using System;
using System.Collections.Generic;
using System.Threading;
using ZWave;
using ZWave.BasicApplication.Devices;
using ZWave.Devices;

namespace Zats.Services
{
    public class CommandClassesSupportHandler
    {
        public List<CommandClassSupport> GetSupportCommandClassesByLibrary(LibTypes library)
        {
            var ccIds = GetDefaultSupportedCCsForLibrary(library);
            return GetDefaultCommandClassSupportsForCCIds(ccIds);
        }

        public List<byte> GetDefaultSupportedCCsForLibrary(LibTypes library)
        {
            var ret = new List<byte>();
            switch (library)
            {
                case LibTypes.SwitchOnOff:
                    #region
                    ret.Add(COMMAND_CLASS_SWITCH_BINARY.ID);
                    ret.Add(COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.ID);
                    ret.Add(COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID);
                    #endregion
                    break;
                case LibTypes.MultilevelSwitch:
                    #region
                    ret.Add(COMMAND_CLASS_SWITCH_BINARY_V2.ID);
                    ret.Add(COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.ID);
                    ret.Add(COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID);
                    ret.Add(COMMAND_CLASS_SWITCH_MULTILEVEL.ID);
                    #endregion
                    break;
                case LibTypes.ControllerPortableLib:
                    #region
                    ret.Add(COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.ID);
                    ret.Add(COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID);
                    ret.Add(COMMAND_CLASS_WAKE_UP_V2.ID);
                    #endregion
                    break;
                case LibTypes.SensorPIR:
                    #region Region
                    ret.Add(COMMAND_CLASS_BATTERY_V3.ID);
                    ret.Add(COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID);
                    ret.Add(COMMAND_CLASS_NOTIFICATION_V3.ID);
                    ret.Add(COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.ID);
                    ret.Add(COMMAND_CLASS_WAKE_UP_V2.ID);
                    #endregion
                    break;
                case LibTypes.MultilevelSensor:
                    #region
                    ret.Add(COMMAND_CLASS_NOTIFICATION_V9.ID);
                    ret.Add(COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.ID);
                    ret.Add(COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID);
                    ret.Add(COMMAND_CLASS_WAKE_UP_V2.ID);
                    ret.Add(COMMAND_CLASS_SENSOR_MULTILEVEL_V11.ID);
                    #endregion
                    break;
                case LibTypes.DoorLockKeyPad:
                    #region
                    ret.Add(COMMAND_CLASS_DOOR_LOCK_V4.ID);
                    ret.Add(COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID);
                    ret.Add(COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.ID);
                    ret.Add(COMMAND_CLASS_DOOR_LOCK_V4.ID);
                    ret.Add(COMMAND_CLASS_BATTERY_V3.ID);
                    #endregion
                    break;
                case LibTypes.LEDBulb:
                    break;
                case LibTypes.PowerStrip:
                    break;
                case LibTypes.WallController:
                    #region
                    ret.Add(COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID);
                    ret.Add(COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.ID);
                    #endregion
                    break;
                case LibTypes.Controller:
                case LibTypes.ControllerBridgeLib:
                    #region
                    ret.Add(COMMAND_CLASS_ASSOCIATION.ID);
                    ret.Add(COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID);
                    ret.Add(COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.ID);
                    ret.Add(COMMAND_CLASS_VERSION_V3.ID);
                    #endregion
                    break;
                case LibTypes.Thermostat:
                    #region
                    ret.Add(COMMAND_CLASS_NOTIFICATION_V9.ID);
                    ret.Add(COMMAND_CLASS_SWITCH_BINARY.ID);
                    ret.Add(COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.ID);
                    ret.Add(COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID);
                    ret.Add(COMMAND_CLASS_THERMOSTAT_MODE.ID);
                    ret.Add(COMMAND_CLASS_THERMOSTAT_SETBACK.ID);
                    #endregion
                    break;
            }

            return ret;

        }

        /// <summary>
        /// Version and Multichannel are skipped!
        /// </summary>
        /// <param name="commandClassIds"></param>
        /// <returns></returns>
        public List<CommandClassSupport> GetDefaultCommandClassSupportsForCCIds(List<byte> commandClassIds)
        {
            var ret = new List<CommandClassSupport>();

            foreach (var commandClassId in commandClassIds)
            {
                var ccSupport = CreateDefaultSupportCommandClassById(commandClassId);

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
        public CommandClassSupport CreateDefaultSupportCommandClassById(byte commandClassId)
        {
            switch (commandClassId)
            {
                case COMMAND_CLASS_ANTITHEFT_V3.ID:
                    #region
                    return new AntiTheftSupport(new AntiTheftSupportData()) { Version = 3 };
                #endregion

                case COMMAND_CLASS_ANTITHEFT_UNLOCK.ID:
                    #region
                    return new AntiTheftUnlockSupport(new AntiTheftSupportData()) { Version = 1 };
                #endregion

                case COMMAND_CLASS_ASSOCIATION_GRP_INFO_V3.ID:
                    #region
                    return new AssociationGroupInfoSupport(GetDummyAssociationSupportData()) { Version = 3 };
                #endregion

                case COMMAND_CLASS_ASSOCIATION_V3.ID:
                    #region
                    return new AssociationSupport(GetDummyAssociationSupportData()) { Version = 3 };
                #endregion

                case COMMAND_CLASS_BARRIER_OPERATOR.ID:
                    #region
                    return new BarrierOperatorSupport(GetDummyBarrierOperatorSupportData()) { Version = 1 };
                #endregion

                case COMMAND_CLASS_BASIC_V2.ID:
                    #region
                    return new BasicSupport(new BasicSupportData(), new DummyGenericBasicMapping()) { Version = 2 };
                #endregion

                case COMMAND_CLASS_BATTERY_V3.ID:
                    #region
                    return new BatterySupport(new BatterySupportData() { BatteryLevel = 88 }) { Version = 3 };
                #endregion

                case COMMAND_CLASS_SWITCH_BINARY_V2.ID:
                    #region
                    return new BinarySwitchSupport(new BinarySwitchSupportData()) { Version = 2 };
                #endregion

                case COMMAND_CLASS_CENTRAL_SCENE_V3.ID:
                    #region
                    return new CentralSceneSupport(GetDummyCentralSceneSupportData()) { Version = 3 };
                #endregion

                case COMMAND_CLASS_SCENE_ACTUATOR_CONF.ID:
                    #region
                    return new SceneActuatorConfSupportModify(COMMAND_CLASS_SCENE_ACTUATOR_CONF.ID) { Version = 1 };
                    #endregion

                case COMMAND_CLASS_SWITCH_COLOR_V3.ID:
                    #region
                    return new ColorSwitchSupport(GetDummyColorSwitchSupportData()) { Version = 3 };
                #endregion

                case COMMAND_CLASS_CONFIGURATION_V4.ID:
                    #region
                    return new ConfigurationSupport(GetDummyConfigurationSupportData()) { Version = 4 };
                #endregion

                case COMMAND_CLASS_DOOR_LOCK_V4.ID:
                    #region
                    return new DoorLockSupport(GetDummyDoorLockSupportData()) { Version = 4 };
                #endregion

                case COMMAND_CLASS_ENTRY_CONTROL.ID:
                    #region
                    return new EntryControlSupport(new EntryControlSupportData()) { Version = 1 };
                #endregion

                case COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.ID:
                    #region
                    AutoResetEvent _fwuMDCompleted = new AutoResetEvent(false);
                    return new FirmwareUpdateMdSupportPCC(() => { _fwuMDCompleted.Set(); }) { Version = 5 };
                #endregion

                case COMMAND_CLASS_INDICATOR_V4.ID:
                    #region
                    return new IndicatorSupport(GetDummyIndicatorSupportData()) { Version = 4 };
                #endregion

                case COMMAND_CLASS_IR_REPEATER.ID:
                    #region
                    return new IrRepeaterSupport(GetDummyIrRepeaterSupportData()) { Version = 1 };
                #endregion

                case COMMAND_CLASS_MANUFACTURER_SPECIFIC_V2.ID:
                    #region
                    return new ManufacturerSpecificSupport(GetDummyManufacturerSpecificSupportData()) { Version = 2 };
                #endregion

                case COMMAND_CLASS_METER_V6.ID:
                    #region
                    return new MeterSupport(GetDummyMeterSupportData()) { Version = 6 };
                #endregion

                case COMMAND_CLASS_MULTI_CHANNEL_ASSOCIATION_V4.ID:
                    #region
                    return new MultiChannelAssociationSupport(GetDummyAssociationSupportData()) { Version = 4 };
                #endregion

                case COMMAND_CLASS_MULTI_CHANNEL_V2.ID:
                    #region
                    // Example, usually aggregated EPs are only allowed for some Meter and Multilevel Sensor DTs
                    //new AggregatedEndPointEx(4, new List<byte>() { 1, 2 }),
                    //return new MultiChannelSupport(GetDummyIndividualEndPoint(), new List<AggregatedEndPointEx>());
                    return null;
                #endregion

                case COMMAND_CLASS_SENSOR_MULTILEVEL_V11.ID:
                    #region
                    return new MultilevelSensorSupport(GetDummyMultilevelSensorSupportData()) { Version = 11 };
                #endregion

                case COMMAND_CLASS_SWITCH_MULTILEVEL_V4.ID:
                    #region
                    return new MultilevelSwitchSupport(new MultilevelSwitchSupportData()) { Version = 4 };
                #endregion

                case COMMAND_CLASS_NOTIFICATION_V8.ID:
                    #region
                    return new NotificationSupport(GetDummyNotificationSupportData()) { Version = 8 };
                #endregion

                case COMMAND_CLASS_SIMPLE_AV_CONTROL_V4.ID:
                    #region
                    return new SimpleAvControlSupport(GetDummySimpleAvControlSupportData()) { Version = 4 };
                #endregion

                case COMMAND_CLASS_SOUND_SWITCH_V2.ID:
                    #region
                    return new SoundSwitchSupport(new SoundSwitchSupportData()) { Version = 2 };
                #endregion

                case COMMAND_CLASS_THERMOSTAT_FAN_MODE_V5.ID:
                    #region
                    return new ThermostatFanModeSupport(GetDummyThermostatFanModeSupportData()) { Version = 5 };
                #endregion

                case COMMAND_CLASS_THERMOSTAT_FAN_STATE_V2.ID:
                    #region
                    return new ThermostatFanStateSupport(GetDummyThermostatFanStateSupportData()) { Version = 2 };
                #endregion

                case COMMAND_CLASS_THERMOSTAT_MODE_V3.ID:
                    #region
                    return new ThermostatModeSupport(GetDummyThermostatModeSupportData()) { Version = 3 };
                #endregion

                case COMMAND_CLASS_THERMOSTAT_OPERATING_STATE_V2.ID:
                    #region
                    return new ThermostatOperatingStateSupport(GetDummyThermostatOperatingStateSupportData()) { Version = 2 };
                #endregion

                case COMMAND_CLASS_THERMOSTAT_SETBACK.ID:
                    #region
                    return new ThermostatSetbackSupport(GetDummyThermostatSetbackSupportData()) { Version = 1 };
                #endregion

                case COMMAND_CLASS_THERMOSTAT_SETPOINT_V3.ID:
                    #region
                    return new ThermostatSetpointSupport(GetDummyThermostatSetpointSupportData()) { Version = 3 };
                #endregion

                case COMMAND_CLASS_USER_CODE_V2.ID:
                    #region
                    return new UserCodeSupport(GetDummyUserCodeSupportData()) { Version = 2 };
                #endregion

                case COMMAND_CLASS_VERSION.ID:
                    #region
                    return null;
                //return new VersionSupport(GetDummyVersionSupportData(), new List<CommandClassSupport>());
                #endregion

                case COMMAND_CLASS_WAKE_UP_V3.ID:
                    #region
                    return new WakeUpSupport(GetDummyWakeUpSupportData()) { Version = 3 };
                #endregion

                case COMMAND_CLASS_WINDOW_COVERING.ID:
                    #region
                    return new WindowCoveringSupport(GetDummyWindowCoveringSupportData(), new MultilevelSwitchSupportData()) { Version = 1 };
                #endregion

                case COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID:
                    #region
                    return new ZWavePlusInfoSupport(GetDummyZWavePlusInfoSupportData()) { Version = 2 };
                #endregion

                default:
                    return null;
            }
        }

        private AssociationSupportData GetDummyAssociationSupportData()
        {
            var associationSupportData = new AssociationSupportData();

            associationSupportData.AssociationGroups.Add(new AssociationGroupEx(1, 5, "Lifeline", 0x0001, new byte[] {
                    COMMAND_CLASS_DEVICE_RESET_LOCALLY.ID, COMMAND_CLASS_DEVICE_RESET_LOCALLY.DEVICE_RESET_LOCALLY_NOTIFICATION.ID,
                    COMMAND_CLASS_SWITCH_BINARY_V2.ID, COMMAND_CLASS_SWITCH_BINARY_V2.SWITCH_BINARY_REPORT.ID
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
        public AssociationSupportDataModify GetDummyAssociationSupportDataModify()
        {
            var associationSupportData = new AssociationSupportDataModify();

            associationSupportData.AssociationGroups.Add(new AssociationGroupExModify(1, 5, "Lifeline", 0x0001, new byte[] {
                    0x5B, 0x03, 0x5B, 0x06, 0x5A, 0x01, 0x87, 0x03
                }));
            associationSupportData.AssociationGroups.Add(new AssociationGroupExModify(2, 5, "Switch", 0x0002, new byte[] {
                COMMAND_CLASS_BASIC_V2.ID,COMMAND_CLASS_BASIC_V2.BASIC_SET.ID,COMMAND_CLASS_SWITCH_MULTILEVEL.ID,COMMAND_CLASS_SWITCH_MULTILEVEL.SWITCH_MULTILEVEL_START_LEVEL_CHANGE.ID,
                COMMAND_CLASS_SWITCH_MULTILEVEL.ID,COMMAND_CLASS_SWITCH_MULTILEVEL.SWITCH_MULTILEVEL_STOP_LEVEL_CHANGE.ID}));            

            associationSupportData.AssociationGroups.Add(new AssociationGroupExModify(3, 5, "Level", 0x0002, new byte[] {
                 COMMAND_CLASS_SWITCH_MULTILEVEL.ID,COMMAND_CLASS_SWITCH_MULTILEVEL.SWITCH_MULTILEVEL_START_LEVEL_CHANGE.ID,
                COMMAND_CLASS_SWITCH_MULTILEVEL.ID,COMMAND_CLASS_SWITCH_MULTILEVEL.SWITCH_MULTILEVEL_STOP_LEVEL_CHANGE.ID}));
            return associationSupportData;
        }

        public SceneSupportData GetDummySceneSupportData()
        {
            var sceneSupportData = new SceneSupportData();
            return sceneSupportData;
        }

        private BarrierOperatorSupportData GetDummyBarrierOperatorSupportData()
        {
            var barrierOperatorSupportData = new BarrierOperatorSupportData()
            {
                LogLevel = 0,
                TransitionTimeSec = 1,
                CurrentState = BarrierOperatorStateNames.Closed,
                TargetState = BarrierOperatorStateNames.Closed,
                SupportedSubsystemTypes = new List<BarrierOperatorSubsystem> {
                    new BarrierOperatorSubsystem(BarrierOperatorSubsystemTypes.AudibleNotification, BarrierOperatorSubsystemStateNames.Off),
                    new BarrierOperatorSubsystem(BarrierOperatorSubsystemTypes.VisualNotification, BarrierOperatorSubsystemStateNames.Off)
                },
            };
            return barrierOperatorSupportData;
        }

        private CentralSceneSupportData GetDummyCentralSceneSupportData()
        {
            var centralSceneSupportData = new CentralSceneSupportData()
            {
                LogLevel = 1,               // Internal default: 0
                Scenes = new List<SceneData>(),
                // V3+
                SlowRefreshIsSupported = true,
                // CC:005B.03.00.21.001: Slow Refresh MUST be enabled by default after inclusion, if supported. Will be done in constructor.
            };
            centralSceneSupportData.AddScene(1, 
                new List<SceneKeyAttribute> { 
                    (SceneKeyAttribute.KeyPressed1Time) 
                }
            );
            centralSceneSupportData.AddScene(2,
                new List<SceneKeyAttribute> { 
                    SceneKeyAttribute.KeyPressed1Time,
                    SceneKeyAttribute.KeyReleased,
                    SceneKeyAttribute.KeyHeldDown,
                    SceneKeyAttribute.KeyPressed2Time,
                    SceneKeyAttribute.KeyPressed3Time,
                    SceneKeyAttribute.KeyPressed4Time,
                    SceneKeyAttribute.KeyPressed5Time
                }
            );

            return centralSceneSupportData;
        }

        private ColorSwitchSupportData GetDummyColorSwitchSupportData()
        {
            return new ColorSwitchSupportData() {
                // For the encoding of duration values see Z-Wave Application Command Class Specification (0=instantly, 1-127=seconds, 128-254=minutes-127, 255=Factory Default Duration)
                FactoryDefaultSetDuration = 0,      // Default Duration for a Set command transition, used for all Color Component IDs. Internal default: 0 (instantly)
                FactoryDefaultLevelDuration = 32,   // Default Duration for a transition from 0% to 100% (255), used for all Color Component IDs. Internal default: 32 (32 seconds)
                // V1+
                // Supported Color Component IDs 0 to 7 are supported, 8 to 15 are reserved and MUST NOT be used
                SupportedColorComponents = new List<ColorComponent>
                {
                    new ColorComponent(ColorComponentIds.Red),
                    new ColorComponent(ColorComponentIds.Green),
                    new ColorComponent(ColorComponentIds.Blue),
                },
            };
        }

        private ConfigurationSupportData GetDummyConfigurationSupportData()
        {
            var configurationSupportData = new ConfigurationSupportData();
            configurationSupportData.Params.Add(new ConfigurationParam(1, 1, 0, 0, 100, 30, ConfigurationParameterFormatNames.UnsignedInt, "Param 1", ""));
            configurationSupportData.Params.Add(new ConfigurationParam(300, 1, 55, -20, 80, -10, ConfigurationParameterFormatNames.SignedInt, "Param 2", "signed negative", false, false, false));
            configurationSupportData.Params.Add(new ConfigurationParam(2, 1, 2, 0, 5, 2, ConfigurationParameterFormatNames.SignedInt, "Param 3", "readonly property set", true, false, false));
            return configurationSupportData;
        }

        private DoorLockSupportData GetDummyDoorLockSupportData()
        {
            return new DoorLockSupportData()
            {
                // Internals
                SimulatedDuration = 0,    // Simulated transition duration in seconds (0...255)
                                          // Initial Door Condition. Internal Default: 0x01 (Closed, Bolt Locked, Latch Open = Secured)
                                          // Bit 0: 0 = Door Open; 1 = Door Closed
                                          // Bit 1: 0 = Bolt Locked (out, extended)[=Door Secured]; 1 = Bolt Unlocked (in, retracted)
                                          // Bit 2: 0 = Latch Open (out, engaged, extended)[=Door Closed]; 1 = Latch Closed (in, disengaged, retracted)
                InitialDoorCondition = 0x01,
                // Initial Door Lock Mode. Internal Default: 0xFF (Secured)
                // ATTENTION: InitialCoorCondition and InitialDoorLockMode MUST match!
                InitialDoorLockMode = DoorLockModes.Secured,

                // Capabilities Report V4+
                // Internal Default: 0x06. Operation Types: Bit 0 = Reserved; Bit 1 = Constant Operation (0x01); Bit 2 = Timed Operation (0x02)
                SupportedOperationTypesList = new List<DoorLockOperationTypes>()
                {
                    DoorLockOperationTypes.ConstantOperation,
                    DoorLockOperationTypes.TimedOperation,
                },
                // Internal Default: all 7 defined modes. Door Lock Modes:
                // 0x00 = Door Unsecured; 0x01 = Door Unsecured with timeout;
                // 0x10 = Door Unsecured for inside  Door Handles; 0x11 = Door Unsecured for inside  Door Handles with timeout;
                // 0x20 = Door Unsecured for outside Door Handles; 0x21 = Door Unsecured for outside Door Handles with timeout;
                // 0xFF = Door Secured
                SupportedDoorLockModes = new List<DoorLockModes>()
                {
                    DoorLockModes.Unsecured, DoorLockModes.UnsecuredWithTimeout,
                    DoorLockModes.UnsecuredForInsideHandles, DoorLockModes.UnsecuredForInsideHandlesWithTimeout,
                    DoorLockModes.UnsecuredForOutsideHandles, DoorLockModes.UnsecuredForOutsideHandlesWithTimeout,
                    DoorLockModes.Secured
                },
                SupportedOutsideHandleModesBitMask = 0x0F,   // Outside Handles 1+2+3+4
                SupportedInsideHandleModesBitMask = 0x0F,    // Inside Handles 1+2+3+4
                SupportedDoorComponents = 0x06,             // Bit 0 = Door (not supported); Bit 1 = Bolt; Bit 2 = Latch
                AutoRelockSupport = true,
                HoldAndReleaseSupport = true,
                TwistAssistSupport = true,
                BlockToBlockSupport = true,
            };
        }

        private IndicatorSupportData GetDummyIndicatorSupportData()
        {
            var indicatorSupportData = new IndicatorSupportData();
            indicatorSupportData.AddIndicator((byte)IndicatorIdNames.NodeIdentify, 0x01, 0x00);
            indicatorSupportData.AddIndicator((byte)IndicatorIdNames.Busy, 0x01, 0x00);
            return indicatorSupportData;
        }

        private IrRepeaterSupportData GetDummyIrRepeaterSupportData()
        {
            return new IrRepeaterSupportData()
            {
                LearntCodeReadbackSupported = false,
                IrRepeatCommandSupported = false
            };
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
                SerialNumber = new byte[] { 0x32, 0x32, 0x32 },
                PseudoRandom = new byte[] { 0x31, 0x32, 0x33, 0x34 }
            };
        }

        private MeterSupportData GetDummyMeterSupportData()
        {
            var meterSupportData = new MeterSupportData() {
                MeterType = MeterTypeNames.Electric,

                // V2+ Supported Report
                MeterResetSupported = true,
                // V4+ Supported Report
                SupportedRateTypes = RateTypeNames.Import,
            };
            // Meter Values
            // Internal default values: Size=1, Precision=0, DefaultRateType=1, Values=0.0, DeltaTime=0
            meterSupportData.MeterValues.Add(new MeterValue {
                ScaleNumber = ScaleNames.ScalekWh,
                SizeConsuming = 2,
                PrecisionConsuming = 2,
                PrecisionProducing = 2,
                ValueConsuming = 1.23,       // Rate Type Import (0x01)
            });
            meterSupportData.MeterValues.Add(new MeterValue {
                ScaleNumber = ScaleNames.ScaleW,
                SizeConsuming = 2,
                PrecisionConsuming = 1,
                ValueConsuming = 123.4,      // Rate Type Import (0x01)
            });

            meterSupportData.MeterValues.Add(new MeterValue {
                ScaleNumber = ScaleNames.ScaleV,
                SizeConsuming = 2,
                PrecisionConsuming = 1,
                ValueConsuming = 231.2      // Rate Type Import (0x01)
            });

            meterSupportData.MeterValues.Add(new MeterValue {
                ScaleNumber = ScaleNames.ScalekVarh,
                SizeConsuming = 2,
                PrecisionConsuming = 2,
                ValueConsuming = 43.21,      // Rate Type Import (0x01)
            });

            return meterSupportData;
        }

        private List<IndividualEndPointEx> GetDummyIndividualEndPoint()
        {
            AssociationSupportData associationSupportDataEp1 = new AssociationSupportData()
            {
                AssociationGroups = new List<AssociationGroupEx> {
                    new AssociationGroupEx(1, 0, "Lifeline",   0x0001, new byte[] { 0x71, 0x05, 0x25, 0x03 }),
                    new AssociationGroupEx(2, 5, "alarm EP 1", 0x7108, new byte[] { 0x71, 0x05 })
                },
            };

            AssociationSupportData associationSupportDataEp2 = new AssociationSupportData()
            {
                AssociationGroups = new List<AssociationGroupEx> {
                    new AssociationGroupEx(1, 0, "Lifeline",   0x0001, new byte[] { 0x71, 0x05, 0x26, 0x03 }),
                    new AssociationGroupEx(2, 5, "alarm EP 2", 0x7108, new byte[] { 0x71, 0x05 })
                },
            };

            BinarySwitchSupportData mirroredBinarySwitchData = new BinarySwitchSupportData() { Duration = 0x0A, State = 0x00, TargetState = 0x00 };


            List<IndividualEndPointEx> individualEndPoints = new List<IndividualEndPointEx>()
            {
                new IndividualEndPointEx(1, (byte)GenericTypes.SWITCH_BINARY, (byte)SpecificTypes.NOT_USED, false, true, SecurityVersions.S2 | SecurityVersions.S0,
                    new List<CommandClassSupport>() {
                        new ZWavePlusInfoSupport()                                      { Version = 2, Secure = false },
                        new AssociationSupport(associationSupportDataEp1)               { Version = 2, Secure = true },
                        new AssociationGroupInfoSupport(associationSupportDataEp1)      { Version = 3, Secure = true },
                        new MultiChannelAssociationSupport(associationSupportDataEp1)   { Version = 3, Secure = true },
                        new BinarySwitchSupport(mirroredBinarySwitchData)               { Version = 2, Secure = true }
                    }
                ),
                new IndividualEndPointEx(2, (byte)GenericTypes.SWITCH_MULTILEVEL, (byte)SpecificTypes.NOT_USED, false, true, SecurityVersions.S2 | SecurityVersions.S0,
                    new List<CommandClassSupport>() {
                        new ZWavePlusInfoSupport()                                      { Version = 2, Secure = false },
                        new AssociationSupport(associationSupportDataEp2)               { Version = 2, Secure = true },
                        new AssociationGroupInfoSupport(associationSupportDataEp2)      { Version = 3, Secure = true },
                        new MultiChannelAssociationSupport(associationSupportDataEp2)   { Version = 3, Secure = true },
                        new MultilevelSwitchSupport()                                   { Version = 2, Secure = true }
                    }
                ),
            };
            return individualEndPoints;
        }

        private MultilevelSensorSupportData GetDummyMultilevelSensorSupportData()
        {
            var multilevelSensorSupportData = new MultilevelSensorSupportData(new List<MultilevelSensorType>()
            {
                new MultilevelSensorType("Air temperature", 0x01, new List<MultilevelSensorScale>() {
                    new MultilevelSensorScale("Celcius (C)", 0x00 ) { Precision = 2, Value = 20.14 },
                    new MultilevelSensorScale("Fahrenheit (F)", 0x01) { Precision = 2, Value = 68.25 },
                    new MultilevelSensorScale("Reserved (Unknown) scale", 0x02) { Precision = 2, Value = 88.88 },
                }),
                new MultilevelSensorType("Time", 0x21, new List<MultilevelSensorScale>() {
                    new MultilevelSensorScale("Second (s)", 0x00) { Precision = 0, Value = 42 }
                }),
                new MultilevelSensorType("Carbon monoxide", 0x28, new List<MultilevelSensorScale>() {
                    new MultilevelSensorScale("Mole per cubic meter (mol/m3)", 0x00) { Precision = 1, Value = 12.3 },
                    new MultilevelSensorScale("Parts/million (ppm)", 0x01) { Precision = 1, Value = 3.21 }
                })
            });
            return multilevelSensorSupportData;
        }

        private NotificationSupportData GetDummyNotificationSupportData()
        {
            var notificationSupportData = new NotificationSupportData();
            notificationSupportData.AddNotificationType(NotificationTypeNames.HeatAlarm, NotificationStatusNames.UnsolicitedReportsDisabled, new List<byte>() {
                (byte) NotificationHeatAlarmEvents.OverheatDetectedWithLocation,        // Alarm V2: Overheat (0x01)
                (byte) NotificationHeatAlarmEvents.UnderHeatDetected,                   // Alarm V2: Underheat (0x06)
                //0x02 | // Alarm V2: Overheat (0x01)
                //0x40   // Alarm V2: Underheat (0x06)
            });
            notificationSupportData.AddNotificationType(NotificationTypeNames.HomeHealth, NotificationStatusNames.UnsolicitedReportsDisabled, new List<byte>() {
                (byte) NotificationHomeHealthEvents.LyingOnBed,                         // Notification V4: Lying on bed (0x03) 
                (byte) NotificationHomeHealthEvents.PostureChanged,                     // Notification V4: Posture changed (0x04)
                //0x08 | // Notification V4: Lying on bed (0x03) 
                //0x10   // Notification V4: Posture changed (0x04)
            });
            notificationSupportData.AddNotificationType(NotificationTypeNames.System, NotificationStatusNames.UnsolicitedReportsDisabled, new List<byte>() {
                (byte) NotificationSystemEvents.SystemHardwareFailure,                  // Alarm V2: System hardware failure (0x01)
                (byte) NotificationSystemEvents.SystemSoftwareFailure,                  // Alarm V2: System software failure (0x02)
                (byte) NotificationSystemEvents.Tampering_ProductCoverRemoved,          // Notification V5: Tampering, product cover removed (0x06)
                (byte) NotificationSystemEvents.EmergencyShutoff,                       // Notification V7: Emergency shutoff (0x07)
                (byte) NotificationSystemEvents.DigitalInputOpen,                       // Notification V8: Digital intput open (0x0B)
                //0x02 | // Alarm V2: System hardware failure (0x01)
                //0x04 | // Alarm V2: System software failure (0x02)
                //0x40 | // Notification V5: Tampering, product cover removed (0x06)
                //0x80,  // Notification V7: Emergency shutoff (0x07)
                //0x08   // Notification V8: Digital intput open (0x0B)
            });
            notificationSupportData.AddNotificationType(NotificationTypeNames.Siren, NotificationStatusNames.UnsolicitedReportsDisabled, new List<byte>() {
                0x02  // Notification V6: Siren active (0x01)
            });
            notificationSupportData.AddNotificationType(NotificationTypeNames.HomeSecurity, NotificationStatusNames.UnsolicitedReportsEnabled, new List<byte>() {
                (byte)NotificationTypeNames.HomeSecurity
            });
            return notificationSupportData;
        }
        private SimpleAvControlSupportData GetDummySimpleAvControlSupportData()
        {
            var simpleAvControlSupportData = new SimpleAvControlSupportData();
            simpleAvControlSupportData.AddSupportedAvCommands(new List<ushort> { 3, 2, 1 });
            return simpleAvControlSupportData;
        }

        private ThermostatFanModeSupportData GetDummyThermostatFanModeSupportData(byte thermostatFanModeSupportVersion = 5)
        {
            // FanModesSupportedList MUST match Version!
            ThermostatFanModeSupportData thermostatFanModeSupportData = new ThermostatFanModeSupportData()
            {
                // Internals accessible from TestCases
                LogLevel = 1,   // Internal default: 0
                                // V1+ Supported Report
                                // V1+ Report
                FanModesSupportedList = new List<ThermostatFanModes>
                {
                    ThermostatFanModes.AutoLow,             // 0x00  V1+
                    ThermostatFanModes.Low,                 // 0x01  V1+
                    ThermostatFanModes.AutoHigh,            // 0x02  V1+
                    ThermostatFanModes.High,                // 0x03  V1+
                  //ThermostatFanModes.AutoMedium,          // 0x04  V2+
                  //ThermostatFanModes.Medium,              // 0x05  V2+
                  //ThermostatFanModes.Circulation,         // 0x06  V3+
                  //ThermostatFanModes.HumidityCirculation, // 0x07  V3+
                  //ThermostatFanModes.LeftAndRight,        // 0x08  V4+
                  //ThermostatFanModes.UpAndDown,           // 0x09  V4+
                  //ThermostatFanModes.Quiet,               // 0x0A  V4+
                  //ThermostatFanModes.ExternalCirculation, // 0x0B  V5+
                },
                // CurrentFanMode: Allowed modes from 0x00 to: 0x03 (V1); 0x05 (V2); 0x07 (V3); 0x0A (V4); 0x0B (V5)
                CurrentFanMode = ThermostatFanModes.AutoLow,
                // V2+ Set / V3+ Report
                Off = false,
            };
            if (thermostatFanModeSupportVersion >= 2)
            {
                thermostatFanModeSupportData.FanModesSupportedList.Add(ThermostatFanModes.AutoMedium);
                thermostatFanModeSupportData.FanModesSupportedList.Add(ThermostatFanModes.Medium);
            }
            if (thermostatFanModeSupportVersion >= 3)
            {
                thermostatFanModeSupportData.FanModesSupportedList.Add(ThermostatFanModes.Circulation);
                thermostatFanModeSupportData.FanModesSupportedList.Add(ThermostatFanModes.HumidityCirculation);
            }
            if (thermostatFanModeSupportVersion >= 4)
            {
                thermostatFanModeSupportData.FanModesSupportedList.Add(ThermostatFanModes.LeftAndRight);
                thermostatFanModeSupportData.FanModesSupportedList.Add(ThermostatFanModes.UpAndDown);
                thermostatFanModeSupportData.FanModesSupportedList.Add(ThermostatFanModes.Quiet);
            }
            if (thermostatFanModeSupportVersion >= 5)
            {
                thermostatFanModeSupportData.FanModesSupportedList.Add(ThermostatFanModes.ExternalCirculation);
            }

            return thermostatFanModeSupportData;
        }

        private ThermostatFanStateSupportData GetDummyThermostatFanStateSupportData()
        {
            ThermostatFanStateSupportData thermostatFanStateSupportData = new ThermostatFanStateSupportData()
            {
                // Internals accessible from TestCases
                LogLevel = 1,   // Internal default: 0
                                // V1+ Report
                                // CurrentFanState: Allowed states from 0 to: 2=RunningHigh (V1); 8=QuietCirculationMode (V2)
                                // Internal default: ThermostatFanStateNames.Off
                CurrentFanState = ThermostatFanStateNames.Off,
            };
            return thermostatFanStateSupportData;
        }

        private ThermostatModeSupportData GetDummyThermostatModeSupportData(byte thermostatModeSupportVersion = 3)
        {
            // ModesSupportedList MUST match Version!
            ThermostatModeSupportData thermostatModeSupportData = new ThermostatModeSupportData()
            {
                // Internals accessible from TestCases
                LogLevel = 1,   // Internal default: 0
                                // V1+ Supported Report
                ModesSupportedList = new List<ThermostatModes>
                {
                    ThermostatModes.OFF,                    // 0x00  V1+
                    //ThermostatModes.HEAT,                   // 0x01  V1+
                    ThermostatModes.COOL,                   // 0x02  V1+
                    ThermostatModes.AUTO,                   // 0x03  V1+
                    ThermostatModes.AUXILIARY,              // 0x04  V1+
                    ThermostatModes.RESUME_ON,              // 0x05  V1+
                    ThermostatModes.FAN,                    // 0x06  V1+
                    ThermostatModes.FURNACE,                // 0x07  V1+

                    ThermostatModes.DRY,                    // 0x08  V1+
                    ThermostatModes.MOIST,                  // 0x09  V1+
                    ThermostatModes.AUTO_CHANGEOVER,        // 0x0A  V1+
                  //ThermostatModes.ENERGY_HEAT,            // 0x0B  V2+
                  //ThermostatModes.ENERGY_COOL,            // 0x0C  V2+
                  //ThermostatModes.AWAY,                   // 0x0D  V2+
                    //ThermostatModes.RESERVED1               // 0x0E  not allowed
                  //ThermostatModes.FULL_POWER,             // 0x0F  V3+
                  //ThermostatModes.MANUFACTURER_SPECIFIC,  // 0x1F  V3+
                },
                //A                ModesSupportedBitmask = new List<byte> { 0xFD, 0xBF, 0x00, 0x80 },  // V3 with manufacturer specific, except mode 0x01 HEAT
                // V1+ Report
                // CurrentMode: Allowed modes: 0x00...0x0A (V1); 0x00...0x0D (V2); 0x00...0x0D, 0x0F, 0x1F (V3)
                CurrentMode = ThermostatModes.OFF,
                // V3 Set / Report
                ManufacturerData = new List<byte> { },
                // Basic Mapping: Value 0x00 maps to BasicSetOffMode and Value 0xFF maps to BasicSetOnMode.
                // Z-Wave Plus V1: BasicSetOffMode is interpreted as Energy Saving Mode and BasicSetOnMode is interpreted as Comfort Mode.
                // Z-Wave Plus V2: BasicSetOffMode and BasicSetOnMode will be set directly.
                // Allowed values for BasicSetOnMode  (ZWPv2): 0x01, 0x02, 0x03. Mode MUST be supported in ModesSupportedBitmask!
                // Allowed values for BasicSetOffMode (ZWPv2): 0x00 (V1+), 0x0B, 0x0C, 0x0D (V2+). Mode MUST be supported in ModesSupportedBitmask!
                BasicZWavePlusVersion = 2,              // Internal default: 2
                BasicSetOnMode = ThermostatModes.COOL, // Internal default: 0x01 (HEAT)
                BasicSetOffMode = ThermostatModes.OFF,  // Internal default: 0x00 (OFF)
            };
            if (thermostatModeSupportVersion >= 2)
            {
                thermostatModeSupportData.ModesSupportedList.Add(ThermostatModes.ENERGY_HEAT);
                thermostatModeSupportData.ModesSupportedList.Add(ThermostatModes.ENERGY_COOL);
                thermostatModeSupportData.ModesSupportedList.Add(ThermostatModes.AWAY);
            }
            if (thermostatModeSupportVersion >= 3)
            {
                thermostatModeSupportData.ModesSupportedList.Add(ThermostatModes.FULL_POWER);
                thermostatModeSupportData.ModesSupportedList.Add(ThermostatModes.MANUFACTURER_SPECIFIC);
            }

            return thermostatModeSupportData;
        }

        private ThermostatOperatingStateSupportData GetDummyThermostatOperatingStateSupportData(byte thermostatOperatingStateSupportVersion = 2)
        {
            ThermostatOperatingStateSupportData thermostatOperatingStateSupportData = new ThermostatOperatingStateSupportData() {
                // V1+ Report
                // Operating State - valid values: V1: 0x00 to 0x06; V2: 0x00 to 0x0B; 0x00 = IDLE
                OperatingState = ThermostatOperatingStateNames.Idle,
                // V2+ Supported Report
            };
            // V2+ Supported Report
            thermostatOperatingStateSupportData.OperatingStateLogs.Add(new OperatingStateLog { 
                OperatingState = ThermostatOperatingStateNames.Cooling, UsageTodayHours = 11 });
            thermostatOperatingStateSupportData.OperatingStateLogs.Add(new OperatingStateLog { 
                OperatingState = ThermostatOperatingStateNames.Heating, UsageTodayHours = 12 });
            thermostatOperatingStateSupportData.OperatingStateLogs.Add(new OperatingStateLog { 
                OperatingState = ThermostatOperatingStateNames.FanOnly, UsageTodayHours = 13 });
            thermostatOperatingStateSupportData.OperatingStateLogs.Add(new OperatingStateLog { 
                OperatingState = ThermostatOperatingStateNames.PendingHeat, UsageTodayHours = 14 });
            thermostatOperatingStateSupportData.OperatingStateLogs.Add(new OperatingStateLog { 
                OperatingState = ThermostatOperatingStateNames.PendingCool, UsageTodayHours = 15 });
            thermostatOperatingStateSupportData.OperatingStateLogs.Add(new OperatingStateLog { 
                OperatingState = ThermostatOperatingStateNames.VentEconomizer, UsageTodayHours = 16 });
            thermostatOperatingStateSupportData.OperatingStateLogs.Add(new OperatingStateLog { 
                OperatingState = ThermostatOperatingStateNames.AuxHeating, UsageTodayHours = 17 });
            thermostatOperatingStateSupportData.OperatingStateLogs.Add(new OperatingStateLog { 
                OperatingState = ThermostatOperatingStateNames.Heating2ndStage, UsageTodayHours = 18 });
            thermostatOperatingStateSupportData.OperatingStateLogs.Add(new OperatingStateLog { 
                OperatingState = ThermostatOperatingStateNames.Cooling2ndStage, UsageTodayHours = 19 });
            thermostatOperatingStateSupportData.OperatingStateLogs.Add(new OperatingStateLog { 
                OperatingState = ThermostatOperatingStateNames.AuxHeat2ndStage, UsageTodayHours = 20 });
            thermostatOperatingStateSupportData.OperatingStateLogs.Add(new OperatingStateLog { 
                OperatingState = ThermostatOperatingStateNames.AuxHeat3rdStage, UsageTodayHours = 21 });

            return thermostatOperatingStateSupportData;
        }

        private ThermostatSetbackSupportData GetDummyThermostatSetbackSupportData()
        {
            ThermostatSetbackSupportData thermostatSetbackSupportData = new ThermostatSetbackSupportData()
            {
                // Internals accessible from TestCases
                LogLevel = 1,   // Internal default: 0
                                // Setback Type: 0x00 = No override; 0x01 = Temporary override; 0x02 = Permanent override; 0x03 = Reserved
                SetbackType = SetbackTypeNames.NoOverride,
                // Setback State: 0x80...0xFF & 0x00... 0x78 = setback; 0x79 = Frost protection;
                //                0x7A = Energy Saving Mode; 0x7B...0x7E = Reserved; 0x7F = Unused State
                SetbackState = 0x00,
            };
            return thermostatSetbackSupportData;
        }

        private ThermostatSetpointSupportData GetDummyThermostatSetpointSupportData()
        {
            byte thermostatSetpointSupportVersion = 3;
            ThermostatSetpointSupportData thermostatSetpointSupportData = new ThermostatSetpointSupportData()
            {
                // DynamicSizeCalculation: if true, for each reported value a matching size is calculated, min and max size are ignored
                DynamicSizeCalculation = false,     // Internal default: false
            };
            //                                                                           precs  scales  sizes   min     max   curr

            var currentValue = CommandClassSupportUtils.ByteListToValue(new List<byte>() { 0x0A }, 1, 0);
            thermostatSetpointSupportData.AddSetpoint(ThermostatSetpointTypes.Heating, 0, 0, 0, 0, 1, 1, 32.0, 999.9, currentValue);

            thermostatSetpointSupportData.AddSetpoint(ThermostatSetpointTypes.Cooling, 1, 1, 0, 0, 4, 4, 32.0, 999.9, 128.0);
            if (thermostatSetpointSupportVersion >= 2)
            {
                thermostatSetpointSupportData.AddSetpoint(ThermostatSetpointTypes.AwayHeating, 2, 2, 0, 0, 2, 2, 9.9, 199.9, -2.0);
            }
            if (thermostatSetpointSupportVersion >= 3)
            {
                thermostatSetpointSupportData.AddSetpoint(ThermostatSetpointTypes.FullPower, 3, 3, 0, 0, 2, 2, 20.0, 99.9, -32.0);
            }

            return thermostatSetpointSupportData;
        }

        private UserCodeSupportData GetDummyUserCodeSupportData()
        {
            UserCodeSupportData userCodeSupportData = new UserCodeSupportData() {
                // The implementation IGNORES a Master Code already set to a User ID (CC:0063.02.0E.11.007)
                AdminCodeSup = true,
                AdminCodeDeactivationSup = true,
                // Internal Default: 0x17. Bit 0 = Available; Bit 1 = Enabled/Grant Access; Bit 2 = Disabled; Bit 3 = Messaging (not implemented); Bit 4 = Passage Mode
                SupportedUserIdStatusListV2 = new List<UserCodeSupportedUserIdStatusV2>
                {
                    UserCodeSupportedUserIdStatusV2.Available,
                    UserCodeSupportedUserIdStatusV2.Enabled,
                    UserCodeSupportedUserIdStatusV2.Disabled,
                    UserCodeSupportedUserIdStatusV2.PassageMode,
                },
                // Internal Default: 0x07. Bit 0 = Normal mode; Bit 1 = Vacation mode; Bit 2 = Privacy Mode; Bit 3 = Locked Out Mode (not implemented)
                SupportedKeypadModesList = new List<UserCodeKeypadMode>
                {
                    UserCodeKeypadMode.Normal,
                    UserCodeKeypadMode.Vacation,
                    UserCodeKeypadMode.Privacy,
                },
                // SupportedKeysList contains all supported ASCII codes. 
                // Internal default: { 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39 } = Keys 0...9
                SupportedKeysList = new byte[] { 0x01, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39 },
                UserCodeChecksumSup = true,
                MultipleUserCodeReportSup = true,
                MultipleUserCodeSetSup = true,
            };

            for (int i = 0; i < 258; i++) {
                userCodeSupportData.Users.Add(new UserCodeUser((ushort)i, UserCodeUserIdStatusV1.Available, new byte[] { 0, 0, 0, 0 }));
            }

            return userCodeSupportData;
        }

        public VersionSupportData GetDummyVersionSupportData()
        {
            var versionSupportData = new VersionSupportData() {
                // Default values for the Version Reports
                // Version Report (V1+)
                // ZWaveLibraryType = (byte)Device.Library,     // this field is filled internally
                // HardwareVersion = Network.HardwareVersion,   // this field is filled internally
                // FirmwareVersions.Count MUST match NumberOfFirmwareTargets (2 bytes per target) !
                FirmwareVersions = new List<TargetFirmwareVersion>()
                {
                    new TargetFirmwareVersion(0x01, 0xAA)
                },
                // Version Capabilities Report (V3)
                CapVersion = 1,
                CapCommandClass = 1,
                CapZWSoftware = 1,
                // Version Z-Wave Software Report (V3)
                // ZWaveProtocol*Version and Application*Version are partially used in V1..V2 Reports
                SDKVersion = new byte[3] { 0x07, 0x00, 0x00 },
                ZWaveApplicationFrameworkAPIVersion = new byte[3] { 0x00, 0x00, 0x02 },
                ZWaveApplicationFrameworkBuildNumber = new byte[2] { 0x00, 0x04 },
                HostInterfaceVersion = new byte[3] { 0x00, 0x00, 0x03 },
                HostInterfaceBuildNumber = new byte[2] { 0x00, 0x01 },
                // ZWaveProtocolVersion: Byte1 = V1..V2 Z-Wave Protocol Version; Byte2 = V1..V2 Z-Wave Protocol Sub Version
                ZWaveProtocolVersion = new byte[3] { 0x06, 0x51, 0x00 },
                ZWaveProtocolBuildNumber = new byte[2] { 0x00, 0xAA },
                // ApplicationVersion: Byte1 = V1 ApplicationVersion resp. V2 Firmware0Version; Byte2: V1 ApplicationSubVersion resp. V2 Firmware0SubVersion
                ApplicationVersion = new byte[3] { 0x01, 0x03, 0x00 },
                ApplicationBuildNumber = new byte[2] { 0x00, 0xBB }
            };
            return versionSupportData;
        }

        private WakeUpSupportData GetDummyWakeUpSupportData()
        {
            WakeUpSupportData wakeUpSupportData = new WakeUpSupportData()
            {
                // ! For WakeUp CTTv2 scripts set ZWavePlusInfoSupportData.RoleType to ZPIRoleTypes.EndNodeSleepingReporting (0x06 = SLEEPING_REPORTING) above
                // Internals
                LogLevel = 1,                   // Internal default: 0
                                                //SimulateWakeUp = true,          // false = off, true = on (timer simulation, log only). Internal default: false
                                                //SendWakeUpNotification = true,  // True: requires SimulateWakeUp==true to send notification. Internal default: false
                                                // V1+
                WakeUpInterval = 10,            // Wake Up Interval: 0 or 1...16777215 seconds; internal default: 60
                WakeUpDestinationNodeId = 0x01, // Internal default: 0x01
                                                // V2+
                MinimumWakeUpInterval = 5,      // Internal default: 5
                MaximumWakeUpInterval = 1000,   // Internal default: 100000
                DefaultWakeUpInterval = 30,     // Internal default: 60
                WakeUpIntervalStep = 1,         // Internal default: 1
                                                // V3+
                WakeUpOnDemandSupport = false,  // Internal default: false, because this feature is not yet supported
            };
            return wakeUpSupportData;
        }

        private WindowCoveringSupportData GetDummyWindowCoveringSupportData()
        {
            var windowCoveringSupportData = new WindowCoveringSupportData()
            {
                FactoryDefaultSetDuration = 4,
                FactoryDefaultLevelDuration = 16,
                BasicZWavePlusVersion = 2,
                SpecificDeviceClass = WindowCoveringSpecificDeviceClass.PositionAndEndpointAware,
            };

            windowCoveringSupportData.AddParameter(WindowCoveringParameterNames.OutRightPosition, (byte)WindowCoveringPositionNames.Closed);
            windowCoveringSupportData.AddParameter(WindowCoveringParameterNames.OutBottomPosition, (byte)WindowCoveringPositionNames.Closed);
            return windowCoveringSupportData;
        }

        private ZWavePlusInfoSupportData GetDummyZWavePlusInfoSupportData()
        {
            var zWavePlusInfoSupportData = new ZWavePlusInfoSupportData()
            {
                ZWaveVersion = 0x02,
                RoleType = RoleTypes.END_NODE_ALWAYS_ON,
                NodeType = ZWave.Enums.NodeTypes.ZWAVEPLUS_NODE,
                InstallerIconType = new byte[] { 0x07, 0x02 },
                UserIconType = new byte[] { 0x07, 0x02 }
            };
            return zWavePlusInfoSupportData;
        }
        private class DummyGenericBasicMapping : IBasicMapping
        {
            byte state = 0x00;

            private bool enabled = true;
            public bool Enabled { get { return enabled; } set { enabled = value; } }

            public bool BasicSet(byte value, NodeTag srcNode)
            {
                state = value;
                return true;
            }

            public COMMAND_CLASS_BASIC_V2.BASIC_REPORT BasicV2Get()
            {
                if (Enabled == false)
                {
                    return null;
                }

                return new COMMAND_CLASS_BASIC_V2.BASIC_REPORT()
                {
                    currentValue = state,
                    targetValue = state,
                    duration = 0
                };
            }
        }
    }

    #region Helper classes

    public class ActionTokenForDeviceContainer
    {
        public ActionToken ActionToken { get; set; }
        public Device Device { get; set; }
        public Type ActionType { get; set; }
        public CommandClassSupport CommandClassSupport { get; set; }
    }


    #endregion
}
