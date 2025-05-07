namespace Zats.Services
{
    /// <summary>
    /// Channel Speed.
    /// Uses as a parameter for 'setchannel' command.
    /// Desc: INS14283 Table 1.
    /// </summary>
    public enum RailChannels
    {
        /// <summary>100 kBit/s GFSK</summary>
        Ch0 = 0,
        /// <summary>40 kBit/s FSK</summary>
        Ch1 = 1,
        /// <summary>9.6 kBit/s FSK</summary>
        Ch2 = 2,
        /// <summary>
        /// 100 kBit/s OQPSK
        /// * Channel 3 is only available for regions 11 and 12, the Z-Wave Long Range *
        /// </summary>
        Ch3 = 3,
    }
}
