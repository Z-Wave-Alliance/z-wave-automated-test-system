using System;

namespace Zats.Services
{
    public enum AddRemoveNodeStatuses
    {
        None = ZWave.BasicApplication.Enums.AddRemoveNodeStatuses.None,
        Added = ZWave.BasicApplication.Enums.AddRemoveNodeStatuses.Added,
        Replicated = ZWave.BasicApplication.Enums.AddRemoveNodeStatuses.Replicated,
        Replaced = ZWave.BasicApplication.Enums.AddRemoveNodeStatuses.Replaced
    }
}
