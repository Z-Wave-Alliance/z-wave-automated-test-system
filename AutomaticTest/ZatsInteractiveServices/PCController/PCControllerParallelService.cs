using System;
using System.Linq;
using Utils.UI.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zats.Services
{
    public class PCControllerParallelService : AutomationServiceBase, IParallelService
    {
        private ParallelService BasicParallelService { get; set; }
        private Service BasicService { get; set; }

        public PCControllerParallelService(PCControllerDeviceManager pccDeviceManager, DeviceManager deviceManager, ParallelService parallelService) : base(pccDeviceManager)
        {
            BasicParallelService = parallelService;
            BasicService = new Service(deviceManager);
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            throw new NotImplementedException();
        }

        public void GetVersion(params Alias[] deviceNumbers)
        {
            throw new NotImplementedException();
        }

        public void MemoryGetId(params Alias[] deviceNumbers)
        {
            var endDevices = deviceNumbers.Where(number => !PCControllerDeviceManager.HasDevice(number)).ToArray();
            if (endDevices != null && endDevices.Count() > 0)
            {
                BasicParallelService.MemoryGetId(endDevices);
            }
        }

        public void Reset(params Alias[] deviceNumbers)
        {
            var controllers = deviceNumbers.Where(number => PCControllerDeviceManager.HasDevice(number)).ToArray();
            var endDevices = deviceNumbers.Where(number => !PCControllerDeviceManager.HasDevice(number)).ToArray();
            SetDefault(controllers);
            SoftReset(controllers);
            MemoryGetId(controllers);
            if (endDevices != null && endDevices.Count() > 0)
            {
                BasicParallelService.Reset(endDevices);
            }
        }

        public void SerialApiGetCapabilities(params Alias[] deviceNumbers)
        {
            throw new NotImplementedException();
        }

        public void SerialApiGetInitData(Alias[] deviceNumbers)
        {
            throw new NotImplementedException();
        }

        public void SetDefault(IEnumerable<Alias> deviceNumbers)
        {
            var endDevices = deviceNumbers.Where(number => !PCControllerDeviceManager.HasDevice(number)).ToArray();
            if (endDevices != null && endDevices.Count() > 0)
            {
                BasicParallelService.SetDefault(endDevices);
            }
            var ctrlsNumbers = deviceNumbers.Intersect(PCControllerDeviceManager.GetDeviceKeys());
            foreach (var ctrlNo in ctrlsNumbers)
            {
                var uiInstance = PCControllerDeviceManager.GetUIInstance(ctrlNo);
                var networkManagementView = FromMainMenuPage(uiInstance, PCControllerIdMap.NetworkManagementView.ID);
                networkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.ResetBtn, null);
                networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.ResetBtn).Click();
                //Reset SIS alias
                if (PCControllerDeviceManager.SisController.Value != 0 && PCControllerDeviceManager.SisController == ctrlNo)
                {
                    PCControllerDeviceManager.SisController = new Alias(0);
                }
                uiInstance.Automation.WaitForElementIsReady(PCControllerIdMap.MainWindow.MainMenuBtn, null, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);
            }
        }

        public void SetDefault(params Alias[] deviceNumbers)
        {
            SetDefault((IEnumerable<Alias>)deviceNumbers);
        }

        public void SetRFReceiveMode(bool mode, params Alias[] deviceNumbers)
        {
            var pcDevices = PCControllerDeviceManager.GetDeviceKeys();
            var ctrlsNumbers = deviceNumbers.Where(x => pcDevices.Any(y => y == x));
            foreach (var ctrlNo in ctrlsNumbers)
            {
                BasicResult.ReturnValue res = null;
                var deviceNumberUi = PCControllerDeviceManager.GetUIInstance(ctrlNo);
                if (deviceNumberUi == null)
                {
                    res = BasicService.SetRFReceiveMode(ctrlNo, mode);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        public void SoftReset(params Alias[] deviceNumbers)
        {
            var endDevices = deviceNumbers.Where(number => !PCControllerDeviceManager.HasDevice(number)).ToArray();
            if (endDevices != null && endDevices.Count() > 0)
            {
                BasicParallelService.SoftReset(endDevices);
            }
        }
    }
}
