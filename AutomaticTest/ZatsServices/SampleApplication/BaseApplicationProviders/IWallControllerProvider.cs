namespace Zats.Services.SampleApplication
{
    public interface IWallControllerProvider : ISampleApplicationProviderBase
    {
        void HoldKey01(byte duration);
        void HoldKey02(byte duration);
        void HoldKey03(byte duration);
        void Key01();
        void Key02();
        void Key03();
    }
}