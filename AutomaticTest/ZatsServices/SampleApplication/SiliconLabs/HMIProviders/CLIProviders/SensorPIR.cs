using System.Threading;
using ZWave.TextApplication;

namespace Zats.Services.SampleApplication.SiliconLabs.CLIProviders
{
    class SensorPIR : SampleApplicationCLIBase, ISensorPirProvider
    {
        public SensorPIR(TextDevice device) : base(device)
        { }

        public void ActivatePirEvent()
        {
            SendCommand("motion_detected activate", 1000);
            Thread.Sleep(20);
            SendCommand("motion_detected deactivate", 1000);
        }

        public void BatteryReport()
        {
            SendCommand("battery_report", 1000);
        }
    }
}