using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZWave;
using System.Threading;
using ZWave.Exceptions;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Operations;
using Utils.UI.Enums;

namespace Zats.Services
{
    /// <summary>
    ///  Provides methods that required user manual interaction with device module.
    /// </summary>
    public class ManualService : ServiceBase, IManualService
    {
        private DeviceManager DeviceManager { get; set; }

        public ManualService(DeviceManager deviceManager)
        {
            DeviceManager = deviceManager;
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            DeviceManager.Log(text, logLevel);
        }

        public BasicResult.AddNode AddNode(Alias controllerNumber, int timeoutMs)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            DeviceManager.DisableSmartStart(controllerNumber);
            AddRemoveNodeResult res = controller.AddNodeToNetwork(ZWave.Enums.Modes.NodeAny, timeoutMs);
            DeviceManager.EnableSmartStart(controllerNumber);
            Thread.Sleep(2000); // end device finish learn mode later that ctrl
            return new BasicActionResult.AddNode(res);
        }

        public BasicResult.RemoveNode RemoveNode(Alias controllerNumber, int timeoutMs)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            DeviceManager.DisableSmartStart(controllerNumber);
            AddRemoveNodeResult res = controller.RemoveNodeFromNetwork(ZWave.Enums.Modes.NodeAny, timeoutMs);
            DeviceManager.EnableSmartStart(controllerNumber);
            return new BasicActionResult.RemoveNode(res);
        }
    }
}
