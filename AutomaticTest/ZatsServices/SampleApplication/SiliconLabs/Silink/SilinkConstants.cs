namespace Zats.Services.SampleApplication
{
    internal enum SilinkButtons
    {
        BTN_0 = 0,
        BTN_1 = 1
    }

    internal enum SilinkButtonPressTypes
    {
        Short = 100,
        Medium = 601,
        Long = 1701
    }

    internal static class ButtonCommands
    {
        internal static readonly string Press = "press";
        internal static readonly string Release = "release";
        internal static readonly string Read = "read";
        internal static readonly string Enable = "enable";
        internal static readonly string Disable = "disable";
    }

}
