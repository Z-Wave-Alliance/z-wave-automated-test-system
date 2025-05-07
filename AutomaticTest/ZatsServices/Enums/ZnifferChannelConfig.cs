namespace Zats.Services
{
    public enum ZnifferChannelConfig : byte
    {
        ClassicChannelsAndLRChannelA = ZWave.ZnifferApplication.Enums.ZnifferChannelConfig.ClassicChannelsAndLRChannelA,
        ClassicChannelsAndLRChannelB = ZWave.ZnifferApplication.Enums.ZnifferChannelConfig.ClassicChannelsAndLRChannelB,
        LRChannelsAAndBOnly = ZWave.ZnifferApplication.Enums.ZnifferChannelConfig.LRChannelsAAndBOnly,
    }
}
