using System;
using ZWave.TextApplication;

namespace Zats.Services.SampleApplication.SiliconLabs.SilinkProviders
{
    class MultilevelSensor : SampleApplicationSilinkBase, IMultilevelSensorProvider
    {
        public MultilevelSensor(TextDevice devBoardDevice) : base(devBoardDevice)
        { }

        public void ActivateEvent()
        {
            _silinkAdminIOHandler.HoldButton((byte)SilinkButtons.BTN_0, 0);
        }

        public void BatteryReport()
        {
            _silinkAdminIOHandler.ToggleButton((byte)SilinkButtons.BTN_0);
        }

        public bool? IsStatusOn()
        {
            // TODO: Implement this method
            throw new NotImplementedException();
        }
    }
}