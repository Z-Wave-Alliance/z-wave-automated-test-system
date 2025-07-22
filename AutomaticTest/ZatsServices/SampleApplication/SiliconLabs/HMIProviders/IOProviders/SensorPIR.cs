using ZWave.BasicApplication.Devices;

namespace Zats.Services.SampleApplication.SiliconLabs.IOProviders
{
    class SensorPIR : SampleApplicationIOBase, ISensorPirProvider
    {
        public SensorPIR(DeviceHostClient deviceHostClient)
            : base(deviceHostClient)
        {
        }

        public void ActivatePirEvent()
        {
            if (_IsModernSeries)
            {
                if (_chipSeries == ChipSeries.RZ13_BRD4201B.ToString())
                {
                    IOHandler.HoldButton(0x03, 0x03);
                }
                else if (_chipSeries == ChipSeries.ZG13S_BRD4201D.ToString())
                {
                    IOHandler.HoldButton(0x03, 0x03);
                }
                else
                {
                    IOHandler.HoldButton(0x03, 0x05);
                }
            }
            else
            {
                IOHandler.ToggleButton(0x02);
            }
        }

        public void BatteryReport()
        {
            if (_IsModernSeries)
            {
                if (_chipSeries == ChipSeries.RZ13_BRD4201B.ToString())
                {
                    IOHandler.ToggleButton(0x04);
                }
                else if (_chipSeries == ChipSeries.ZG13S_BRD4201D.ToString())
                {
                    IOHandler.ToggleButton(0x04);
                }
                else
                {
                    IOHandler.ToggleButton(0x01);
                }
            }
            else
            {
                IOHandler.ToggleButton(0x05);
            }
        }

        public void Reset()
        {
            if (_chipSeries == ChipSeries.RZ13_BRD4201B.ToString())
            {
                IOHandler.HoldButton(0x03, 0x07);
            }
            else if (_chipSeries == ChipSeries.ZG13S_BRD4201D.ToString())
            {
                IOHandler.HoldButton(0x03, 0x07);
            }
            else
            {
                base.Reset();
            }
        }

        public void LearnMode()
        {
            if (_chipSeries == ChipSeries.RZ13_BRD4201B.ToString())
            {
                IOHandler.ToggleButton(0x03);
            }
            else if (_chipSeries == ChipSeries.ZG13S_BRD4201D.ToString())
            {
                IOHandler.ToggleButton(0x03);
            }
            else
            {
                base.LearnMode();
            }
        }
    }
}