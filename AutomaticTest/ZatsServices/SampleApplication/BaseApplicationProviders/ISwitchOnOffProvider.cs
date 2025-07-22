namespace Zats.Services.SampleApplication
{
    public interface ISwitchOnOffProvider : ISampleApplicationProviderBase
    {
        void HoldKey01(byte duration);
        bool? IsStatusOn();
        void OnOff();
    }
}