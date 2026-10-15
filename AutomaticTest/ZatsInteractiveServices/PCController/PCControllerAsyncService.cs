// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Linq;
using Utils.UI.Enums;

namespace Zats.Services
{
    public class PCControllerAsyncService : AutomationServiceBase, IAsyncService
    {
        private readonly int REQUEST_TIMEOUT_MS = 10000;
        private const int INCLUSION_TIMEOUT_MS = 65000; // 65 sec
        private DeviceManager DeviceManager { get; set; }
        private AsyncService BasicAsyncService { get; set; }

        public PCControllerAsyncService(PCControllerDeviceManager pccDeviceManager, DeviceManager deviceManager) : base(pccDeviceManager)
        {
            DeviceManager = deviceManager;
            BasicAsyncService = new AsyncService(DeviceManager);
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            throw new NotImplementedException();
        }

        public BasicToken.AddNode AddNode(Alias controllerNumber, Modes mode, int timeoutMs)
        {
            if (PCControllerDeviceManager.HasDevice(controllerNumber))
            {
                var uiInstance = PCControllerDeviceManager.GetUIInstance(controllerNumber);
                var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
                var networkManagementView = FromMainMenuPage(uiController, PCControllerIdMap.NetworkManagementView.ID);
                var nodesListObserver = uiInstance.ActivateNodesListObserver();

                bool nwi = mode == (Modes.NodeOptionNetworkWide);
                if (nwi)
                {
                    networkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.NwiBtn, null);
                    networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.NwiBtn).Click();
                }
                else
                {
                    networkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.AddBtn, null);
                    networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.AddBtn).Click();
                }

                uiController.Automation.WaitForElementIsVisible(PCControllerIdMap.MainWindow.BusyView, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);
                SecuritySchemes[] grantSchemes = null;
                if (DeviceManager.IncludeSettings.GrantedSchemes != null ||
                    !DeviceManager.IncludeSettings.CancelGrantedSchemes)
                {
                    //empty - all available by default
                    grantSchemes = DeviceManager.IncludeSettings.GrantedSchemes;
                }
                //byte[] dsk = null;
                //if ((DeviceManager.IncludeSettings.JoinDSK != null &&
                //    DeviceManager.IncludeSettings.JoinDSK.Length > 0) ||
                //    !DeviceManager.IncludeSettings.CancelDSKInputDialog)
                //{
                //    dsk = DeviceManager.IncludeSettings.JoinDSK;
                //}

                var res = new PCControllerActionToken.AddNode(uiInstance, nodesListObserver, PCControllerDeviceManager, grantSchemes, timeoutMs);
                PCControllerDeviceManager.IncludeSettings.Clear();
                return res;
            }
            else
            {
                return BasicAsyncService.AddNode(controllerNumber, mode, timeoutMs);
            }
        }

        public BasicToken.AddNode AddNode(Alias controllerNumber, Modes mode)
        {
            return AddNode(controllerNumber, mode, INCLUSION_TIMEOUT_MS);
        }

        public BasicToken.AddNode AddNode(Alias controllerNumber)
        {
            return AddNode(controllerNumber, Modes.NodeAny);
        }

        public BasicToken.AddNode AddNodeWithDSK(Alias controllerNumber, Modes mode, int dsk)
        {
            return AddNodeWithDSK(controllerNumber, mode, dsk, INCLUSION_TIMEOUT_MS);
        }

        public BasicToken.AddNode AddNodeWithDSK(Alias controllerNumber, int dsk)
        {
            return AddNodeWithDSK(controllerNumber, Modes.NodeAny, dsk);
        }

        public BasicToken.AddNode AddNodeWithDSK(Alias controllerNumber, byte[] dsk)
        {
            return AddNodeWithDSK(controllerNumber, Modes.NodeAny, dsk);
        }

        public BasicToken.AddNode AddNodeWithDSK(Alias controllerNumber, Modes mode, byte[] dsk)
        {
            return AddNodeWithDSK(controllerNumber, mode, dsk, INCLUSION_TIMEOUT_MS);
        }

        public BasicToken.AddNode AddNodeWithDSK(Alias controllerNumber, Modes mode, int dsk, int timeoutMs)
        {
            return AddNodeWithDSK(controllerNumber, mode, new byte[] { (byte)(dsk >> 8), (byte)(dsk & byte.MaxValue) }, timeoutMs);
        }

        public BasicToken.AddNode AddNodeWithDSK(Alias controllerNumber, Modes mode, byte[] dsk, int timeoutMs)
        {
            return AddNodeWithDSK(controllerNumber, mode, dsk, new SecuritySchemes[] { }, timeoutMs);
        }

        public BasicToken.AddNode AddNodeWithDSK(Alias controllerNumber, Modes mode, int dsk, SecuritySchemes[] grantSchemes, int timeoutMs)
        {
            return AddNodeWithDSK(controllerNumber, mode, new byte[] { (byte)(dsk >> 8), (byte)(dsk & byte.MaxValue) }, grantSchemes, timeoutMs);
        }

        public BasicToken.AddNode AddNodeWithDSK(Alias controllerNumber, Modes mode, byte[] dsk, SecuritySchemes[] grantSchemes, int timeoutMs)
        {
            DeviceManager.IncludeSettings.GrantedSchemes = grantSchemes;
            DeviceManager.IncludeSettings.JoinDSK = dsk;
            if (PCControllerDeviceManager.HasDevice(controllerNumber))
            {
                return AddNode(controllerNumber, mode, timeoutMs);
            }
            else
            {
                return BasicAsyncService.AddNodeWithDSK(controllerNumber, mode, dsk, timeoutMs);
            }
        }

        public void CancelToken(Alias deviceNumber, _ActionToken token)
        {
            var deviceUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            var primaryMainWindow = deviceUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.ID);
            if (primaryMainWindow.CheckVisible(PCControllerIdMap.MainWindow.BusyView))
            {
                primaryMainWindow.NavigateTo(PCControllerIdMap.MainWindow.BusyView).NavigateTo(PCControllerIdMap.MainWindow.AbortBtn).ClickIfEnabled();
            }
            //bool operationResult = primaryMainWindow.NavigateTo(PCControllerIdMap.MainWindow.BusyView).CheckVisible(PCControllerIdMap.MainWindow.AbortBtn);
            bool isNotVisible = primaryMainWindow.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null, WAIT_ELEMENT_READY_TIMEOUT_MS);
            if (!isNotVisible)
            {
                throw new InvalidOperationException("Operation do not canceled!");
            }
        }

        public BasicToken.AddNode ControllerChange(Alias controllerNumber, ControllerChangeModes mode, int timeoutMs)
        {
            var uiInstance = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            var networkManagementView = FromMainMenuPage(uiController, PCControllerIdMap.NetworkManagementView.ID);
            var nodesListObserver = uiInstance.ActivateNodesListObserver();

            networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.ShiftBtn).ClickIfEnabled();

            uiController.Automation.WaitForElementIsVisible(PCControllerIdMap.MainWindow.BusyView, 2000);
            SecuritySchemes[] grantSchemes = null;
            if (DeviceManager.IncludeSettings.GrantedSchemes != null ||
                !DeviceManager.IncludeSettings.CancelGrantedSchemes)
            {
                //empty - all available by default
                grantSchemes = DeviceManager.IncludeSettings.GrantedSchemes;
            }
            //byte[] dsk = null;
            //if ((DeviceManager.IncludeSettings.JoinDSK != null &&
            //    DeviceManager.IncludeSettings.JoinDSK.Length > 0) ||
            //    !DeviceManager.IncludeSettings.CancelDSKInputDialog)
            //{
            //    dsk = DeviceManager.IncludeSettings.JoinDSK;
            //}
            //DeviceManager.IncludeSettings.Clear();
            var res = new PCControllerActionToken.AddNode(uiInstance, nodesListObserver, PCControllerDeviceManager, grantSchemes, timeoutMs);
            PCControllerDeviceManager.IncludeSettings.Clear();
            return res;
        }

        public BasicToken.ExpectControllerUpdate ExpectControllerUpdate(Alias controllerNumber, ControllerUpdateStatuses updateStatus, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicToken.ExpectData ExpectData(Alias deviceNumber, byte[] expectedData)
        {
            return ExpectData(deviceNumber, expectedData, REQUEST_TIMEOUT_MS);
        }

        public BasicToken.ExpectData ExpectData(Alias deviceNumber, byte[] expectedData, int timeoutMs)
        {
            return ExpectData(deviceNumber, expectedData, 2, timeoutMs);
        }

        public BasicToken.ExpectData ExpectData(Alias deviceNumber, byte[] expectedData, int numberOfBytesToCompare, int timeoutMs)
        {
            var uiInstance = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            uiInstance.ClearLog();
            return new PCControllerActionToken.ExpectData(uiInstance, PCControllerDeviceManager.ZWaveDefinition, expectedData, numberOfBytesToCompare, timeoutMs);
        }

        public BasicToken.ListenControllerUpdate ListenControllerUpdate(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicToken.ListenData ListenData(Alias deviceNumber)
        {
            return ListenData(deviceNumber, null);
        }

        public BasicToken.ListenData ListenData(Alias deviceNumber, byte[] compareData)
        {
            var uiInstance = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            uiInstance.ClearLog();
            return new PCControllerActionToken.ListenData(uiInstance, PCControllerDeviceManager.ZWaveDefinition, compareData);
        }

        public BasicToken.RemoveNode RemoveNode(Alias controllerNumber)
        {
            return RemoveNode(controllerNumber, Modes.NodeAny, INCLUSION_TIMEOUT_MS);
        }

        public BasicToken.RemoveNode RemoveNode(Alias controllerNumber, Modes mode)
        {
            return RemoveNode(controllerNumber, mode, INCLUSION_TIMEOUT_MS);
        }

        public BasicToken.RemoveNode RemoveNode(Alias controllerNumber, Modes mode, int timeoutMs)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            var networkManagementView = FromMainMenuPage(uiController, PCControllerIdMap.NetworkManagementView.ID);

            var nwe = (mode == Modes.NodeOptionNetworkWide);
            if (nwe)
            {
                networkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.NweBtn, null);
                networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.NweBtn).Click();
            }
            else
            {
                networkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.RemoveBtn, null);
                networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.RemoveBtn).Click();
            }
            return new PCControllerActionToken.RemoveNode(uiController, timeoutMs);
        }

        public BasicToken.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, byte[] expectedData, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicToken.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, byte[] expectedData)
        {
            throw new NotImplementedException();
        }

        public BasicToken.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, byte[] expectedData, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicToken.RequestData RequestDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, SecuritySchemes scheme, byte[] expectedData)
        {
            throw new NotImplementedException();
        }

        public BasicToken.RequestData RequestDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2, byte[] expectedData, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicToken.RequestData RequestDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, SecuritySchemes scheme, byte[] expectedData, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicToken.RequestData RequestDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, byte[] expectedData, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicToken.RequestNetworkUpdate RequestNetworkUpdate(Alias senderNumber)
        {
            throw new NotImplementedException();
        }

        public BasicToken.RequestNodeInfo RequestNodeInfo(Alias deviceNumber, Node node)
        {
            throw new NotImplementedException();
        }

        public BasicToken.RequestNodeNeighborUpdate RequestNodeNeighborUpdate(Alias controllerNumber, Alias nodeNumber)
        {
            throw new NotImplementedException();
        }

        public BasicToken.SendData SendData(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions)
        {
            throw new NotImplementedException();
        }

        public BasicToken.SendData SendData(Alias senderNumber, Alias receiverNumber, byte[] data)
        {
            throw new NotImplementedException();
        }

        public BasicToken.SendData SendData(Alias senderNumber, Node receiver, byte[] data, TransmitOptions txOptions)
        {
            throw new NotImplementedException();
        }

        public BasicToken.SendData SendDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2)
        {
            throw new NotImplementedException();
        }

        public BasicToken.SendData SendDataEx(Alias senderNumber, Node receiver, byte[] data, SecuritySchemes scheme)
        {
            throw new NotImplementedException();
        }

        public BasicToken.SendData SendDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, SecuritySchemes scheme)
        {
            throw new NotImplementedException();
        }

        public BasicToken.SendData SendDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme)
        {
            throw new NotImplementedException();
        }

        public BasicToken.SendData SendDataEx(Alias senderNumber, Node receiver, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2)
        {
            throw new NotImplementedException();
        }

        public BasicToken.SendData SendDataMultiEx(Alias senderNumber, byte[] data, SecuritySchemes scheme, byte groupId)
        {
            throw new NotImplementedException();
        }

        public BasicToken.SendData SendDataMultiEx(Alias senderNumber, byte[] data, TransmitOptions txOptions, SecuritySchemes scheme, byte groupId)
        {
            throw new NotImplementedException();
        }

        public BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode)
        {
            var uiInstance = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            INodesListObserver nodesListObserver = null;
            if (PCControllerDeviceManager.HasDevice(deviceNumber))
            {
                var uiController = PCControllerDeviceManager.GetUIInstance(deviceNumber);
                var networkManagementView = FromMainMenuPage(uiController, PCControllerIdMap.NetworkManagementView.ID);
                nodesListObserver = uiController.ActivateNodesListObserver();
                var primaryMainWindow = uiController.Automation.NavigateTo(PCControllerIdMap.MainWindow.ID);

                // some specific case to abort learn mode!
                if (mode == LearnModes.LearnModeDisable)
                {
                    if (uiController.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null, WAIT_ELEMENT_READY_TIMEOUT_MS))
                    {
                        uiInstance.Automation.TryNavigateTo(PCControllerIdMap.MainWindow.BusyView)?.TryNavigateTo(PCControllerIdMap.MainWindow.AbortBtn)?.ClickIfEnabled();
                        uiInstance.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null, WAIT_ELEMENT_READY_TIMEOUT_MS);
                    }
                }
                else if (mode == LearnModes.LearnModeClassic)
                {
                    networkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.ClassicLearnModeBtn, null);
                    networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.ClassicLearnModeBtn).Click();
                }
                else
                {
                    using (var dlgActive = primaryMainWindow.OpenDialog(PCControllerIdMap.DialogWindow.ID, PCControllerIdMap.SelectLearnModeView.CAPTION,
                            () =>
                            {
                                networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.SelectLearnModeBtn).Click();
                            },
                            null))
                    {
                        string btnId = PCControllerIdMap.SelectLearnModeView.ClassicLMBtn;
                        if (mode.HasFlag(LearnModes.LearnModeNWI))
                        {
                            btnId = PCControllerIdMap.SelectLearnModeView.NWILMBtn;
                        }
                        else if (mode.HasFlag(LearnModes.LearnModeNWE))
                        {
                            btnId = PCControllerIdMap.SelectLearnModeView.NWELMBtn;
                        }
                        else if (mode.HasFlag(LearnModes.LearnModeSmartStart))
                        {
                            btnId = PCControllerIdMap.SelectLearnModeView.SmartStartLMBtn;
                        }

                        dlgActive.DialogView.WaitForElementIsReady(btnId, null);
                        dlgActive.DialogView.NavigateTo(btnId).ClickIfEnabled();

                    }
                }
                return new PCControllerActionToken.SetLearnMode(uiInstance, nodesListObserver);
            }
            else
            {
                return BasicAsyncService.SetLearnMode(deviceNumber);
            }
        }

        public BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber)
        {
            return SetLearnMode(deviceNumber, LearnModes.LearnModeClassic);
        }

        public BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode, int timeoutMs)
        {
            return SetLearnMode(deviceNumber, mode);
        }

        public BasicToken.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode, bool isNonSecure, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public bool WaitAnyNodeStatus(Alias deviceNumber, int timeoutMs, params NodeStatuses[] statuses)
        {
            throw new NotImplementedException();
        }

        public bool WaitAssignStatus(Alias deviceNumber, int timeoutMs, AssignStatuses status)
        {
            throw new NotImplementedException();
        }

        public bool WaitCompletedSignal(_ActionToken token)
        {
            throw new NotImplementedException();
        }

        public bool WaitNodeStatus(Alias deviceNumber, int timeoutMs, NodeStatuses status)
        {
            throw new NotImplementedException();
        }

        public BasicToken.SendData SendDataMulti(Alias senderNumber, Node[] nodes, byte[] data, SubstituteFlags Flags)
        {
            throw new NotImplementedException();
        }

        public BasicToken.ExpectSmartStartAdd ExpectSmartStartAdd(Alias controllerNumber, int timeoutMs)
        {
            if (PCControllerDeviceManager.HasDevice(controllerNumber))
            {
                var uiInstance = PCControllerDeviceManager.GetUIInstance(controllerNumber);
                var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
                var networkManagementView = FromMainMenuPage(uiController, PCControllerIdMap.NetworkManagementView.ID);
                var nodesListObserver = uiInstance.ActivateNodesListObserver();

                //prepare token for result
                var res = new PCControllerActionToken.ExpectSmartStartAdd(uiInstance, nodesListObserver, timeoutMs);
                return res;
            }
            throw new NotImplementedException();
        }

        public BasicToken.Void SetRFReceiveMode(Alias DeviceNumber, bool mode)
        {
            throw new NotImplementedException();
        }

        public BasicToken.ExpectZW ExpectZW(Alias deviceNumber, string filter, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicToken.ExpectZW ExpectZW(Alias deviceNumber, IEnumerable<string> filter, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicToken.ExpectZW ExpectZW(Alias deviceNumber, ByteData[] filter, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicToken.ExpectZW ExpectZW(Alias deviceNumber, IEnumerable<ByteData[]> filter, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicToken.Includenode IncludeNode(Alias controllerNumber, byte[] dks, Modes addMode, int timeoutMs, SetupNodeLifelineSettings setupNodeLifelineSettings = SetupNodeLifelineSettings.Default)
        {
            throw new NotImplementedException();
        }

        public BasicToken.ExpectData ExpectData(Alias deviceNumber, Node srcNode, byte[] expectedData, int timeoutMs)
        {
            BasicToken.ExpectData result = null;
            var uiController = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            uiController.ClearLog();
            if (uiController != null)
            {
                result = new PCControllerActionToken.ExpectData(uiController, PCControllerDeviceManager.ZWaveDefinition, expectedData, 2, timeoutMs);
            }
            else
            {
                result = BasicAsyncService.ExpectData(deviceNumber, srcNode, expectedData, timeoutMs);
            }

            return result;
        }

        public BasicToken.ExpectData ExpectData(Alias deviceNumber, Node srcNode, byte[] expectedData, SecurityS2ExtensionTypes[] securityS2ExtensionType, int timeoutMs)
        {
            BasicToken.ExpectData result = null;
            var uiController = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            uiController.ClearLog();
            if (uiController != null)
            {
                if (securityS2ExtensionType.Contains(SecurityS2ExtensionTypes.Mpan))
                {
                    throw new NotImplementedException("Log doesn't contain encypted extensions information.");
                }
                result = new PCControllerActionToken.ExpectDataWithS2Extension(uiController, PCControllerDeviceManager.ZWaveDefinition, securityS2ExtensionType, timeoutMs);
            }
            else
            {
                result = BasicAsyncService.ExpectData(deviceNumber, srcNode, null, securityS2ExtensionType, timeoutMs);
            }
            return result;
        }

        public BasicToken.AddNode ReplaceFailedNode(Alias controllerNumber, Node node, byte[] dsk, Modes addMode, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicToken.AddNode AddNode(Alias controllerNumber, Modes mode, bool isModeStopEnabled, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicToken.AddNode ControllerChange(Alias controllerNumber, ControllerChangeModes mode, bool isModeStopEnabled, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicToken.RemoveNode RemoveNodeIdFromNetwork(Alias controllerNumber, Node nodeNumber, Modes mode, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicToken.Void NoiseData(Alias alias, Node node, byte[] data, TransmitOptions txOptions, Action<BasicResult.SendData> sendCallback, int interval, int timeout)
        {
            throw new NotImplementedException();
        }

        public BasicToken.Void NoiseData(Alias alias, Node[] nodes, byte[] data, TransmitOptions txOptions, Action<BasicResult.SendData> sendCallback, int interval, int timeout)
        {
            throw new NotImplementedException();
        }

        public BasicToken.Void NoiseData(Alias alias, Node node, byte[] data, TransmitOptions txOptions, byte[] expectData, Action<BasicResult.RequestData> requestCallback, int interval, int timeout)
        {
            throw new NotImplementedException();
        }

        public BasicToken.Void NoiseData(Alias alias, Node[] nodes, byte[] data, TransmitOptions txOptions, byte[] expectData, Action<BasicResult.RequestData> requestCallback, int interval, int timeout)
        {
            throw new NotImplementedException();
        }

        public BasicToken.Void SetRFReceiveMode(Alias DeviceNumber, bool mode, bool isNodelay)
        {
            throw new NotImplementedException();
        }

        public BasicToken.SetLearnMode WaitForLearnMode(Alias deviceNumber, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicToken.Void NoiseDataEx(Alias deviceNumber, Node[] nodes, byte[] data, TransmitOptions txOptions, byte[] expectData, Action<BasicResult.RequestData> requestCallback, int interval, int timeout, SecuritySchemes secSch, bool isPoisson)
        {
            throw new NotImplementedException();
        }
    }
}
