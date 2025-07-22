namespace Zats.Services.SampleApplication
{
    public interface IMultilevelSensorProvider : ISampleApplicationProviderBase
    {
        void ActivateEvent();
        void BatteryReport();
        bool? IsStatusOn();
    }
}