namespace Zats.Services
{
    /// <summary>
    /// Supported Installation and Maintenance Extensions
    /// </summary>
    public enum IMAtypes
    {
        RouteChanged = 0,
        TransmissionTime = 0x01,
        LastWorkingRoute = 0x02,
        RSSI = 0x03,
        AckChannel = 0x04,
        TransmitChannel = 0x05,
        RoutingScheme = 0x06,
        RoutingAttempts = 0x07,
        LastFailedLink = 0x08
    }
}
