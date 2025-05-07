using Utils.UI.Enums;
using ZWave.TextApplication;

namespace Zats.Services
{
    public class TerminalService : ServiceBase, ITerminalService
    {
        private DeviceManager DeviceManager { get; set; }
        public TerminalService(DeviceManager deviceManager)
        {
            DeviceManager = deviceManager;
            CommSettings = new CommSettings();
        }
        public CommSettings CommSettings { get; set; }
        public ServiceSettings Settings
        {
            get
            {
                return DeviceManager.Settings;
            }
            set
            {
                DeviceManager.Settings = value;
            }
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            DeviceManager.Log(text, logLevel);
        }

        public string Request(Alias deviceNumber, string data, int timeoutMs)
        {
            var device = DeviceManager.GetDeviceHostClient(deviceNumber).SClient as TextDevice;
            if (CommSettings.AppendNewLine)
            {
                data += '\r';
                if (CommSettings.WindowsStyleNewLine)
                {
                    data += '\n';
                }
            }
            var res = device.Request(data, timeoutMs);
            return res.ReceivedText;
        }

        public string Request(Alias deviceNumber, string data)
        {
            return Request(deviceNumber, data, 2000);
        }

        public void Send(Alias deviceNumber, string data)
        {
            var device = DeviceManager.GetDeviceHostClient(deviceNumber).SClient as TextDevice;
            device.Send(data);
        }

        public string RequestWithDelay(Alias deviceNumber, string data, int timeoutMs)
        {
            var device = DeviceManager.GetDeviceHostClient(deviceNumber).SClient as TextDevice;
            if (CommSettings.AppendNewLine)
            {
                data += '\r';
                if (CommSettings.WindowsStyleNewLine)
                {
                    data += '\n';
                }
            }
            var res = device.RequestWithDelay(data, timeoutMs);
            return res.ReceivedText;
        }
    }

    public class CommSettings
    {
        public bool AppendNewLine { get; set; }
        public bool WindowsStyleNewLine { get; set; }
    }
}
