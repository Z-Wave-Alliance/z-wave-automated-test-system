using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zats.Services
{
    [Flags]
    public enum TransmitSecurityOptions
    {
        S2_TXOPTION_VERIFY_DELIVERY = ZWave.Enums.TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY,
        S2_TXOPTION_SINGLECAST_FOLLOWUP = ZWave.Enums.TransmitSecurityOptions.S2_TXOPTION_SINGLECAST_FOLLOWUP,
        S2_TXOPTION_FIRST_SINGLECAST_FOLLOWUP = ZWave.Enums.TransmitSecurityOptions.S2_TXOPTION_FIRST_SINGLECAST_FOLLOWUP
    }
}
