
using ZWave.TextApplication;

namespace Zats.Services.SampleApplication.SiliconLabs.SilinkProviders
{
    class SensorPIR : SampleApplicationSilinkBase, ISensorPirProvider
    {
        public SensorPIR(TextDevice devBoardDevice) : base(devBoardDevice)
        { }

        public void ActivatePirEvent()
        {
            _silinkAdminIOHandler.HoldButton((byte)SilinkButtons.BTN_0, 0);
        }

        public void BatteryReport()
        {
            _silinkAdminIOHandler.ToggleButton((byte)SilinkButtons.BTN_0);
        }
    }
}