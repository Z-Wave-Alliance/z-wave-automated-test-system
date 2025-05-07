using System;

namespace Zats.Services
{
    /// <summary>
    /// MetaData options for provisioning list
    /// </summary>
    [Flags]
    public enum PLMetaData
    {
        CriticalFlag = ZWave.Enums.PLMetaData.CriticalFlag,
        ProductType = ZWave.Enums.PLMetaData.ProductType,
        ProductId = ZWave.Enums.PLMetaData.ProductId,
        MaxInclusionRequestInterval = ZWave.Enums.PLMetaData.MaxInclusionRequestInterval,
        UUID16 = ZWave.Enums.PLMetaData.UUID16,
        Name = ZWave.Enums.PLMetaData.Name,
        Location = ZWave.Enums.PLMetaData.Location,
        InclusionStatus = ZWave.Enums.PLMetaData.InclusionStatus,
        AdvancedJoining = ZWave.Enums.PLMetaData.AdvancedJoining,
        BootstrappingMode = ZWave.Enums.PLMetaData.BootstrappingMode,
        NetworkStatus = ZWave.Enums.PLMetaData.NetworkStatus,
    }

    public enum PListStatus
    {
        Pending = ZWave.Enums.PListStatus.Pending,
        Included = ZWave.Enums.PListStatus.Included,
        Passive = ZWave.Enums.PListStatus.Passive,
        Ignore = ZWave.Enums.PListStatus.Ignore,
    }

    public enum PLBootstrappingModeOpt
    {
        Security2 = ZWave.Enums.PLBootstrappingModeOpt.Security2,
        SmartStart = ZWave.Enums.PLBootstrappingModeOpt.SmartStart,
    }
}
