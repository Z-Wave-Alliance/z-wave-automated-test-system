namespace Zats.Services.SampleApplication
{
    public interface IDoorLookKeypadProvider : ISampleApplicationProviderBase
    {
        void BatteryReport();
        bool? IsBoltLocked();
        bool? IsLatchOpen();
        void ToggleDoorHandle();
        void ToggleDoorHandle(byte duration);
        void ToggleKeyPad();
    }
}