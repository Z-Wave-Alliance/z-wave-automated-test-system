using ZWave;

namespace Zats.Services
{
    public interface IFirmwareUpdateMD
    {
        BasicResult.Void EnableSupport(params Alias[] controlles);
        BasicResult.Void EnableSupport(byte version, params Alias[] controlles);
    }
}
