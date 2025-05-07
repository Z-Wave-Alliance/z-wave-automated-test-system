namespace Zats.Services
{
    public static class SpecificTypes
    {
        public const byte NOT_USED = 0x00;

        #region GENERIC_TYPE_STATIC_CONTROLLER
        public const byte PC_CONTROLLER = 0x01;
        public const byte SUBSYSTEM_CONTROLLER = 0x05;
        #endregion

        #region GENERIC_TYPE_GENERIC_CONTROLLER
        public const byte PORTABLE_REMOTE_CONTROLLER = 0x01;
        #endregion

        #region GENERIC_TYPE_ENTRY_CONTROL
        public const byte DOOR_LOCK = 0x01;
        public const byte SECURE_GATE = 0x06;
        public const byte SECURE_BARRIER_OPEN_ONLY = 0x08;
        public const byte SECURE_BARRIER_CLOSE_ONLY = 0x09;
        #endregion

        #region GENERIC_TYPE_AV_CONTROL_POINT
        public const byte SOUND_SWITCH = 0x01;
        #endregion

        #region GENERIC_TYPE_SWITCH_BINARY
        public const byte COLOR_TUNABLE_BINARY = 0x02;
        #endregion

        #region GENERIC_TYPE_SWITCH_MULTILEVEL
        public const byte COLOR_TUNABLE_MULTILEVEL = 0x02;
        public const byte CLASS_A_MOTOR_CONTROL = 0x05;
        public const byte CLASS_B_MOTOR_CONTROL = 0x06;
        public const byte CLASS_C_MOTOR_CONTROL = 0x07;
        #endregion

        #region GENERIC_TYPE_SENSOR_MULTILEVEL
        public const byte ROUTING_MULTILEVEL_SENSOR = 0x01;
        #endregion

        #region GENERIC_TYPE_THERMOSTAT
        public const byte THERMOSTAT_GENERAL_V2 = 0x06;
        #endregion

        #region GENERIC_TYPE_REPEATER_END_NODE
        public const byte IR_REPEATER = 0x03;
        #endregion

    }
}
