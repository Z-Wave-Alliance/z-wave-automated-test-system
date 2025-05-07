namespace Zats.Services.Constants
{
    public static class TestInterfaceConstants
    {

        public static class ButtonActionTemplates
        {
            public static byte[] PressButton(byte buttonNumber, byte times)
            {
                return GetButtonCommand(buttonNumber, ButtonOperations.Press, times);
            }

            public static byte[] ToggleButton(byte buttonNumber)
            {
                return PressButton(buttonNumber, 0x01);
            }

            public static byte[] HoldButton(byte buttonNumber, byte duration)
            {
                return GetButtonCommand(buttonNumber, ButtonOperations.Hold, duration);
            }
            private static byte[] GetButtonCommand(byte buttonNumber, byte pressType, byte durationOrTimes)
            {
                return new[] { ButtonOperations.CommandId, buttonNumber, pressType, durationOrTimes };
            }
        }

        public static class ButtonOperations
        {
            public const byte CommandId = (byte)ProgrammerCommandTypes.ButtonPress;
            public const byte Press = 0x01;
            public const byte Hold = 0x02;
            public const int DefaultTimeoutMs = 3000;
        }
    }

    public enum ProgrammerCommandTypes
    {
        Ack = 0x06,
        StoreHomeId = 0x13,
        GetSoftwareVersion = 0x26,
        ProgEnable = 0x40,
        ReadSignatureByte = 0x47,
        ProgDisable = 0x48,
        ToggleEeprom = 0x51,
        ButtonPress = 0x73,
        GetLedState = 0x74,
        ReadNvr = 0xFE,
    }
}
