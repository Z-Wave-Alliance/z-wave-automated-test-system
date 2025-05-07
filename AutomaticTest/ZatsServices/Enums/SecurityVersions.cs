using System;

namespace Zats.Services
{
    /// <summary>
    /// Security Versions:
    /// </summary>
    [Flags]
    public enum SecurityVersions
    {
        None = 0x00,
        S0 = 0x01,
        S2 = 0x02
    }
}
