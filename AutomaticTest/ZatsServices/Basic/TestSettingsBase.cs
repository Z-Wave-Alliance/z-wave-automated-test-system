namespace Zats.Services
{
    public abstract class TestSettingsBase
    {
        public DeviceManager DeviceManager { get; set; }

        public TestSettingsBase(DeviceManager deviceManager)
        {
            DeviceManager = deviceManager;
        }
    }
}
