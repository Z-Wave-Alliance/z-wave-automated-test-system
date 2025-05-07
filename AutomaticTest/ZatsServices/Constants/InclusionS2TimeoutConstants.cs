namespace Zats.Services.Constants
{
    public static class InclusionS2TimeoutConstants
    {
        public static class Including
        {
            public static int KexReport => ZWave.Security.InclusionS2TimeoutConstants.Including.KexReport;
            public static int PublicKeyReport => ZWave.Security.InclusionS2TimeoutConstants.Including.PublicKeyReport;
            public static int NetworkKeyGet => ZWave.Security.InclusionS2TimeoutConstants.Including.NetworkKeyGet;
            public static int NetworkKeyVerify => ZWave.Security.InclusionS2TimeoutConstants.Including.NetworkKeyVerify;
            public static int TransferEnd => ZWave.Security.InclusionS2TimeoutConstants.Including.TransferEnd;
            public static int UserInputKeyList => ZWave.Security.InclusionS2TimeoutConstants.Including.UserInputKeyList;
            public static int UserInputDsk => ZWave.Security.InclusionS2TimeoutConstants.Including.UserInputDsk;
        }

        public static class Joining
        {
            public static int KexGet => ZWave.Security.InclusionS2TimeoutConstants.Joining.KexGet;
            public static int KexSet => ZWave.Security.InclusionS2TimeoutConstants.Joining.KexSet;
            public static int PublicKeyReport => ZWave.Security.InclusionS2TimeoutConstants.Joining.PublicKeyReport;
            public static int NetworkKeyReport => ZWave.Security.InclusionS2TimeoutConstants.Joining.NetworkKeyReport;
            public static int TransferEnd => ZWave.Security.InclusionS2TimeoutConstants.Joining.TransferEnd;
            public static int UserInputCsa => ZWave.Security.InclusionS2TimeoutConstants.Joining.UserInputCsa;
        }
    }
}
