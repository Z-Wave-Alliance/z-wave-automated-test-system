using Zats.Services;
using Zats.Services.MeshNetworkRacks;

namespace ZatsTestConsole
{
    public interface IManagersContainer
    {
        ProfileContext Context { get; }
        InstrumentsManager InstrumentsManager { get; }
        DebugDeviceManager DebugDeviceManager { get; }
        DeviceManager DeviceManager { get; }
        IEmulatedDeviceManager EmulatedDeviceManager { get; }
        PCControllerDeviceManager PCControllerDeviceManager { get; }
        ReportManager ReportManager { get; }
        ZipManager ZipManager { get; }
        UicManager UicManager { get; }
        MeshRouterManager MeshRouterManager { get; }
        ZnifferManager ZnifferManager { get; }
    }
}