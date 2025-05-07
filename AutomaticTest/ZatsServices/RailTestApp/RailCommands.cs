namespace Zats.Services
{
    /// <summary> RAIL Test app commands map. </summary>
    public static class RailCommands
    {
        //System:
        public const string ERROR_CODE = "errorCode";
        public const string DEVICE_READY = ">";
        public const string MAX_ARG = "Maximum number of arguments exceeded";
        //Commands:
        public const string RESET = "reset";
        public const string RX = "rx";
        public const string SET_ZWAVE_MODE = "setZWaveMode";
        public const string SET_TX_LENGTH = "setTxLength";
        public const string SET_TX_PAYLOAD = "setTxPayload";
        public const string SET_ZWAVE_REGION = "setZWaveRegion";
        public const string SET_POWER = "setPower";
        public const string SET_CHANNEL = "setChannel";
        public const string SET_TX_TONE = "setTxTone";
        public const string SET_TX_DELAY = "setTxDelay";
        public const string TX = "tx";

        //configTxOptions

        public const string CONFIG_RX_CHANNEL_HOPPING = "configRXChannelHopping";
        public const string ENABLE_RX_CHANNEL_HOPPING = "enableRxChannelHopping";
    }
}
