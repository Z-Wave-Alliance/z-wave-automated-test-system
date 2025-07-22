namespace Zats.Services.SampleApplication
{
    public interface ISensorPirProvider : ISampleApplicationProviderBase
    {
        void ActivatePirEvent();
        void BatteryReport();
    }
}