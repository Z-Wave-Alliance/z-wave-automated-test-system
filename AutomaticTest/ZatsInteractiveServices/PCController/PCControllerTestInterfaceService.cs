// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Threading;
using Utils.UI.Enums;

namespace Zats.Services
{
    public class PCControllerTestInterfaceService : AutomationServiceBase, ITestInterfaceService
    {
        //private IEmulatedDeviceManager _emulatedDeviceManager;
        //private DebugDeviceManager _debugDeviceManager;
        private TestInterfaceService _testInterfaceService;

        public DeviceManager DeviceManager { get; }

        public PCControllerTestInterfaceService(/*IEmulatedDeviceManager emulatedDeviceManager,*/
            PCControllerDeviceManager pCControllerDeviceManager,
            DeviceManager deviceManager,
            /*DebugDeviceManager debugDeviceManager,*/ TestInterfaceService testInterfaceService) : base(pCControllerDeviceManager)
        {
            //_emulatedDeviceManager = emulatedDeviceManager;
            //_debugDeviceManager = debugDeviceManager;
            _testInterfaceService = testInterfaceService;
            DeviceManager = deviceManager;
        }

        public ServiceSettings Settings { get => _testInterfaceService.Settings; set => _testInterfaceService.Settings = value; }

        public HMIActions HMIActions => _testInterfaceService.HMIActions;

        public void ButtonSinglePress_UserInput(Alias deviceNumber, byte ButtonNumber)
        {
            _testInterfaceService.ButtonSinglePress_UserInput(deviceNumber, ButtonNumber);
        }

        public BasicResult.RemoveNode ExcludeNode(Alias controllerNumber, Alias testInterfaceNumber)
        {
            BasicResult.RemoveNode ret = null;
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            if (uiController != null)
            {
                var networkManagementView = FromMainMenuPage(uiController, PCControllerIdMap.NetworkManagementView.ID);
                var primaryMainWindow = uiController.Automation.NavigateTo(PCControllerIdMap.MainWindow.ID);
                networkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.RemoveBtn, null, WAIT_ELEMENT_READY_TIMEOUT_MS);
                networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.RemoveBtn).Click();

                primaryMainWindow.WaitForElementIsVisible(PCControllerIdMap.MainWindow.BusyView, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);
                Thread.Sleep(1000); // allow learn to begin
                SetLearnMode(testInterfaceNumber);

                primaryMainWindow.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null, 20000);

                ret = new PCControllerActionResult.RemoveNode(uiController);
            }
            else
            {
                ret = _testInterfaceService.ExcludeNode(controllerNumber, testInterfaceNumber);
            }
            return ret;
        }

        public byte[] GetDSK(Alias deviceNumber, int numberOfBytes)
        {
            return _testInterfaceService.GetDSK(deviceNumber, numberOfBytes);
        }

        public LibTypes GetLibrary(Alias endDevice)
        {
            return _testInterfaceService.GetLibrary(endDevice);
        }

        public BasicResult.IncludeNode IncludeNode(Alias controllerNumber, Alias testInterfaceNumber, Alias deviceDebugNumber = default(Alias))
        {
            BasicResult.IncludeNode ret = null;
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            if (uiController != null)
            {
                SecuritySchemes[] grantSchemes = null;
                if (DeviceManager.IncludeSettings.GrantedSchemes != null || !DeviceManager.IncludeSettings.CancelGrantedSchemes) // empty - all available by default.
                {
                    grantSchemes = DeviceManager.IncludeSettings.GrantedSchemes ?? new SecuritySchemes[0];
                }
                if (DeviceManager.IncludeSettings.JoinDSK == null)
                {
                    var dsk = DeviceManager.GetDSK(testInterfaceNumber, 2);
                    Thread.Sleep(1500);
                    DeviceManager.IncludeSettings.JoinDSK = dsk;
                }

                var sisUI = PCControllerDeviceManager.SisController.Value == 0 ? uiController : PCControllerDeviceManager.GetUIInstance(PCControllerDeviceManager.SisController);
                var sisnetworkManagementView = FromMainMenuPage(sisUI, PCControllerIdMap.NetworkManagementView.ID); //prepare sis ui if exists
                var sisNodesListObserver = sisUI.ActivateNodesListObserver((byte)testInterfaceNumber.Value);

                var networkManagementView = FromMainMenuPage(uiController, PCControllerIdMap.NetworkManagementView.ID);
                var primaryMainWindow = uiController.Automation.NavigateTo(PCControllerIdMap.MainWindow.ID);
                networkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.AddBtn, null, WAIT_ELEMENT_READY_TIMEOUT_MS);
                networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.AddBtn).Click();
                primaryMainWindow.WaitForElementIsVisible(PCControllerIdMap.MainWindow.BusyView, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);
                var inclusionToken = new PCControllerActionToken.IncludeNode(uiController, sisNodesListObserver, PCControllerDeviceManager, grantSchemes, 60000);

                Thread.Sleep(1500);
                SetLearnMode(testInterfaceNumber);

                var inclusionResult = inclusionToken.WaitCompletedSignal();
                primaryMainWindow.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                ret = inclusionResult;
            }
            else
            {
                ret = _testInterfaceService.IncludeNode(controllerNumber, testInterfaceNumber);
            }
            DeviceManager.IncludeSettings.Clear();
            return ret;
        }

        public int ProgramBootLoader(Alias deviceNumber, BootloaderPath regexFilter)
        {
            return _testInterfaceService.ProgramBootLoader(deviceNumber, regexFilter);
        }

        public BasicResult.IncludeNodeWithLearnMode ReplaceFailedNode(Alias controllerNumber, Node node, Alias testInterfaceNumber)
        {
            throw new NotImplementedException();
        }

        public void SetLearnMode(Alias deviceNumber)
        {
            _testInterfaceService.SetLearnMode(deviceNumber);
        }

        public void SoftReset(Alias deviceNumber)
        {
            var deviceHostClient = PCControllerDeviceManager.GetDeviceHostClient(deviceNumber);
            DeviceManager.ResetSample(deviceHostClient);
        }

        public void SoftReset(Alias[] deviceNumber)
        {
            foreach (var item in deviceNumber)
            {
                SoftReset(item);
            }
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            throw new NotImplementedException();
        }

        public void LoadDSK(Alias endDevice)
        {
            throw new NotImplementedException();
        }

        public BasicResult.AddNode AddNode(Alias primary, Alias doorLock, Alias deviceDebugNumber = default(Alias))
        {
            throw new NotImplementedException();
        }

        public BasicResult.RemoveNode ExcludeNode(Alias controllerNumber, Alias testInterfaceNumber, Modes mode)
        {
            throw new NotImplementedException();
        }

        public void SendNodeInfo(Alias deviceNumber, byte destNodeId, TransmitOptions txOptions)
        {
            throw new NotImplementedException();
        }
    }
}
