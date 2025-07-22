namespace Zats.Services.SampleApplication
{
    public interface ILEDBulbProvider : ISampleApplicationProviderBase
    {
        void DimHold(int duration);
        void DimOnOff();
        bool? IsDimStatusOn();
        bool? IsStatusOn();
        void OnOff();
        void ToggleNotify();
    }
}