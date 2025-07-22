using ZWave.BasicApplication.Devices;

namespace Zats.Services.SampleApplication.SiliconLabs.IOProviders
{
    class SampleApplicationIOBase : ISampleApplicationProviderBase
    {
        private DevKits _devKit;
        /// <summary>
        /// Connected Device is Series1 (700s) or newer, has manipulator board connected
        /// </summary>
        protected bool _IsModernSeries;
        protected string _chipSeries;
        public readonly IIOHandler IOHandler;

        public SampleApplicationIOBase(DeviceHostClient deviceHostClient)
        {
            var device = (ITestInterfaceDevice)deviceHostClient.PClient;
            IOHandler = new ManipulatorIOHandler(device);
            _IsModernSeries = deviceHostClient.HasJName;
            _devKit = deviceHostClient.DevKit;
            _chipSeries = deviceHostClient.Host.ChipSeries;
        }

        public void LearnMode()
        {
            if (_IsModernSeries)
            {
                IOHandler.ToggleButton(0x02);
            }
            else
            {
                if (_devKit != DevKits.Default && _devKit < DevKits.v6_81)
                {
                    IOHandler.PressButton(0x01, 0x03);
                }
                else
                {
                    IOHandler.ToggleButton(0x01);
                }
            }
        }

        public void Reset()
        {
            if (_IsModernSeries)
            {
                IOHandler.HoldButton(0x02, 0x07);
            }
            else
            {
                IOHandler.HoldButton(0x01, 0x0C);
            }
        }
    }
}