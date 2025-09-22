// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Utils;
using Utils.UI.Enums;
using ZWave.BasicApplication.Operations;
using ZWave.Xml.Application;

namespace Zats.Services
{
    public class PCControllerService : AutomationServiceBase, IService
    {
        protected const int MAX_FRAME_SIZE = 30;
        protected const int INCLUSION_TIMEOUT_MS = 60000; // 60 sec
        protected DeviceManager DeviceManager { get; set; }
        protected Service BasicService { get; set; }
        protected AsyncService AsyncBasicService { get; set; }
        private PCControllerComplianceService _complianceService;
        public PCControllerService(PCControllerDeviceManager pccDeviceManager, DeviceManager deviceManager) : base(pccDeviceManager)
        {
            DeviceManager = deviceManager;
            BasicService = new Service(DeviceManager);
            AsyncBasicService = new AsyncService(DeviceManager);
            _complianceService = new PCControllerComplianceService(PCControllerDeviceManager); //- to avoid code duplication - better use same methods from PCControllerService
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            throw new NotImplementedException();
        }

        private bool SetupInclusionMode(UIInstance joiningUI, Modes addMode)
        {
            if (addMode != Modes.NodeAny && addMode != Modes.NodeOptionNetworkWide)
            {
                throw new InvalidOperationException("Unsupported inclusion mode");
            }
            bool ret = false;
            if (joiningUI.Library == LibTypes.Controller || joiningUI.Library == LibTypes.ControllerBridgeLib ||
                joiningUI.Library == LibTypes.ControllerPortableLib || joiningUI.Library == LibTypes.ControllerStaticLib)
            {
                using (var dlgWindow = joiningUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => joiningUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => joiningUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                            NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    var dialogView = dlgWindow.DialogView;
                    if (dialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS0Chk).GetCheckBoxStateAt())
                    {
                        dialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS0Chk).SetCheckBoxStateAt(false);
                    }
                    if (dialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2UnauthChk).GetCheckBoxStateAt())
                    {
                        dialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2UnauthChk).SetCheckBoxStateAt(false);
                    }
                    if (dialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2AuthChk).GetCheckBoxStateAt())
                    {
                        dialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2AuthChk).SetCheckBoxStateAt(false);
                    }
                    if (dialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2AccessChk).GetCheckBoxStateAt())
                    {
                        dialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2AccessChk).SetCheckBoxStateAt(false);
                    }
                    ret = dialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.JoinCSAChk).GetCheckBoxStateAt();
                }
            }
            return ret;
        }

        private BasicResult.SetLearnMode SetupDSKExchangeWithEndDevice(UIInstance includingUI, byte[] inputDsk, Alias nodeNumber, LearnModes learnMode, int timeoutMs)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            BasicToken.SetLearnMode learnRes = AsyncBasicService.SetLearnMode(nodeNumber, learnMode, timeoutMs);
            if (includingUI.Automation.WaitForDialogIsVisible(PCControllerIdMap.DialogWindow.ID, "Preparing inclusion", timeoutMs))
            {
                var dialogView = includingUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Preparing inclusion");
                SetGrantedSchemes(dialogView);

                var isInProc = includingUI.Automation.CheckVisible(PCControllerIdMap.MainWindow.BusyView);
                var isIncludingEnterDSKShown = includingUI.Automation.CheckVisible(PCControllerIdMap.DialogWindow.ID) ||
                                               includingUI.Automation.WaitForDialogIsVisible(PCControllerIdMap.DialogWindow.ID, "Enter DSK", isInProc ? timeoutMs - (int)stopwatch.ElapsedMilliseconds : 100);
                if (isIncludingEnterDSKShown)
                {
                    string dsk = string.Empty;
                    if (inputDsk.IsNullOrEmpty())
                    {
                        byte[] endDeviceDsk = DeviceManager.GetDSK(nodeNumber, 2);
                        dsk = ((endDeviceDsk[0] << 8) | endDeviceDsk[1]).ToString();
                    }
                    else
                    {
                        dsk = ((inputDsk[0] << 8) | inputDsk[1]).ToString();
                    }
                    includingUI.Automation.WaitForElementIsReady(PCControllerIdMap.UserInputDialog.InTb, null, 1000);
                    includingUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Enter DSK").NavigateTo(PCControllerIdMap.UserInputDialog.InTb).SetElementValue(dsk);
                    includingUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Enter DSK").NavigateTo(PCControllerIdMap.UserInputDialog.OkBtn).Click();
                }
            }
            stopwatch.Stop();
            return learnRes.WaitCompletedSignal();
        }


        private void SetupLearnModeWithCSA(UIInstance includingUI, UIInstance joiningUI, LearnModes modeLM)
        {
            SetLearnMode(joiningUI, modeLM);

            var isInProc = includingUI.Automation.CheckVisible(PCControllerIdMap.MainWindow.BusyView) && joiningUI.Automation.CheckVisible(PCControllerIdMap.MainWindow.BusyView);
            bool prepearingReady = includingUI.Automation.WaitForDialogIsVisible(PCControllerIdMap.DialogWindow.ID, "Preparing inclusion", isInProc ? WAIT_DLG_TIMEOUT_MS : 100);
            if (prepearingReady)
            {
                var dlgActiveKEX = includingUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Preparing inclusion");
                SetGrantedSchemes(dlgActiveKEX);

                string dsk = string.Empty;
                isInProc = includingUI.Automation.CheckVisible(PCControllerIdMap.MainWindow.BusyView) && joiningUI.Automation.CheckVisible(PCControllerIdMap.MainWindow.BusyView);
                var isJoiningDSKShown = includingUI.Automation.CheckVisible(PCControllerIdMap.DialogWindow.ID) ||
                                        includingUI.Automation.WaitForDialogIsVisible(PCControllerIdMap.DialogWindow.ID, "Client-side authentication", isInProc ? WAIT_DLG_TIMEOUT_MS : 100);
                if (isJoiningDSKShown)
                {
                    if (DeviceManager.IncludeSettings.JoinCSA != null && DeviceManager.IncludeSettings.JoinCSA.Length > 0)
                    {
                        StringBuilder csa = new StringBuilder();
                        csa.Append((DeviceManager.IncludeSettings.JoinCSA[0] << 8) | DeviceManager.IncludeSettings.JoinCSA[1]);
                        csa.Append(" ");
                        csa.Append((DeviceManager.IncludeSettings.JoinCSA[2] << 8) | DeviceManager.IncludeSettings.JoinCSA[3]);
                        dsk = csa.ToString();
                    }
                    else
                    {
                        var dlgDsk = includingUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Client-side authentication");
                        if (dlgDsk != null)
                        {
                            dsk = dlgDsk.NavigateTo(PCControllerIdMap.UserInputDialog.OutTb).GetElementValue();
                            dlgDsk.NavigateTo(PCControllerIdMap.UserInputDialog.OkBtn).Click();

                        }
                    }
                }
                joiningUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Enter DSK").NavigateTo(PCControllerIdMap.UserInputDialog.InTb).SetElementValue(dsk);
                joiningUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Enter DSK").NavigateTo(PCControllerIdMap.UserInputDialog.OkBtn).Click();
            }
        }

        public BasicResult.AddNodeWithLearnMode AddNode(Alias controllerNumber, Alias nodeNumber, Modes addMode, LearnModes learnMode, int timeoutMs)
        {
            var includingUI = PCControllerDeviceManager.GetUIInstance(controllerNumber);

            includingUI.ClearLog();
            var primaryNetworkManagementView = FromMainMenuPage(includingUI, PCControllerIdMap.NetworkManagementView.ID);
            var primaryMainWindow = includingUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.ID);

            bool nwi = addMode == Modes.NodeOptionNetworkWide;
            BasicResult.SetLearnMode learnRes = null;
            bool isEndDeviceAdded = false;
            BasicResult.AddNodeWithLearnMode ret = null;

            var stopwatch = new Stopwatch();
            var sisUI = PCControllerDeviceManager.SisController.Value == 0 ? includingUI : PCControllerDeviceManager.GetUIInstance(PCControllerDeviceManager.SisController);
            var joinInitId = MemoryGetId(nodeNumber).NodeId;
            var sisNodesListObserver = sisUI.ActivateNodesListObserver(joinInitId);
            var joiningUI = PCControllerDeviceManager.GetUIInstance(nodeNumber);
            if (joiningUI != null)
            {
                StartInclusion(includingUI, nwi, DeviceManager.IncludeSettings.SetupNodeLifelineSettings);
                var isCSAEnabled = SetupInclusionMode(joiningUI, addMode);
                joiningUI.ClearLog();
                FromMainMenuPage(joiningUI, PCControllerIdMap.NetworkManagementView.ID);
                var secondaryMainWindow = joiningUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.ID);
                var secondaryNodesListObserver = joiningUI.ActivateNodesListObserver();
                secondaryMainWindow.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                stopwatch.Start();
                if (isCSAEnabled)
                {
                    SetupLearnModeWithCSA(includingUI, joiningUI, learnMode);
                }
                else
                {
                    var timeoutLMLeft = stopwatch.ElapsedMilliseconds > timeoutMs ? 100 : (int)(timeoutMs - stopwatch.ElapsedMilliseconds);
                    $"AddNode timeoutLMLeft(1) = {timeoutLMLeft}ms"._DLOG();
                    var inputDsk = new byte[0];
                    if (new[] { LibTypes.EndDeviceLib, LibTypes.EndDeviceSysTestLib, LibTypes.EndDeviceLegacyRoutingLib }.Contains(joiningUI.Library))
                    {
                        inputDsk = PCControllerDeviceManager.GetDSK(nodeNumber, 2);
                    }
                    SetupLearnModeWithDSKExchange(sisUI, joiningUI, DeviceManager.IncludeSettings.JoinDSK ?? inputDsk, learnMode, timeoutLMLeft);
                }

                var timeoutLeft = stopwatch.ElapsedMilliseconds > timeoutMs ? 100 : (int)(timeoutMs - stopwatch.ElapsedMilliseconds);
                $"AddNode timeoutLMLeft(2) = {timeoutLeft}ms"._DLOG();
                var isCompleted = secondaryMainWindow.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null, timeoutLeft);
                if (!isCompleted)
                {
                    secondaryMainWindow.TryNavigateTo(PCControllerIdMap.MainWindow.BusyView)?.TryNavigateTo(PCControllerIdMap.MainWindow.AbortBtn)?.ClickIfEnabled();
                    secondaryMainWindow.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null, WAIT_ELEMENT_READY_TIMEOUT_MS);
                }
                secondaryMainWindow.WaitForElementIsReady(PCControllerIdMap.MainWindow.MainMenuBtn, null, WAIT_ELEMENT_READY_TIMEOUT_MS);
                learnRes = new PCControllerActionResult.LearnModeResult(joiningUI, secondaryNodesListObserver.GetIncludedNodeInfo()?.First());
                if (joiningUI.Automation.CheckVisible(PCControllerIdMap.DialogWindow.ID))
                {
                    joiningUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID).CloseWindow();
                }
            }
            else
            {
                isEndDeviceAdded = true;
                if (DeviceManager.IncludeSettings.JoinDSK == null)
                {
                    DeviceManager.IncludeSettings.JoinDSK = DeviceManager.GetDSK(nodeNumber, 2);
                }
                stopwatch.Start();
                StartInclusion(includingUI, nwi, DeviceManager.IncludeSettings.SetupNodeLifelineSettings);
                learnRes = SetupDSKExchangeWithEndDevice(sisUI, DeviceManager.IncludeSettings.JoinDSK, nodeNumber, learnMode, timeoutMs);
            }

            if (nwi)
            {
                primaryMainWindow.WaitForElementIsVisible(PCControllerIdMap.MainWindow.BusyView, null);
                primaryMainWindow.NavigateTo(PCControllerIdMap.MainWindow.BusyView).NavigateTo(PCControllerIdMap.MainWindow.AbortBtn).ClickIfEnabled();
            }
            else
            {
                var timeoutLeft = stopwatch.ElapsedMilliseconds > timeoutMs ? 100 : (int)(timeoutMs - stopwatch.ElapsedMilliseconds);
                $"AddNode timeoutLMLeft(3) = {timeoutLeft}ms"._DLOG();
                var isCompleted = primaryMainWindow.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null, timeoutLeft);
                if (!isCompleted)
                {
                    primaryMainWindow.TryNavigateTo(PCControllerIdMap.MainWindow.BusyView)?.TryNavigateTo(PCControllerIdMap.MainWindow.AbortBtn)?.ClickIfEnabled();
                    primaryMainWindow.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null, WAIT_ELEMENT_READY_TIMEOUT_MS);
                }
            }

            if (sisUI.Automation.CheckVisible(PCControllerIdMap.DialogWindow.ID))
            {
                sisUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID).CloseWindow();
            }

            primaryMainWindow.WaitForElementIsReady(PCControllerIdMap.MainWindow.MainMenuBtn, null, WAIT_ELEMENT_READY_TIMEOUT_MS);
            ret = new PCControllerActionResult.AddNodeWithLearnMode(includingUI, sisNodesListObserver.GetIncludedNodeInfo()?.Last(), isEndDeviceAdded, nwi, false); //x => x.Id == learnRes.NodeId
            ret.SetLearnMode = learnRes;
            DeviceManager.IncludeSettings.Clear();
            stopwatch.Stop();
            return ret;
        }

        private void StartInclusion(UIInstance includingUi, bool nwi, SetupNodeLifelineSettings setupNodeLifelineSettings)
        {
            var primaryNetworkManagementView = FromMainMenuPage(includingUi, PCControllerIdMap.NetworkManagementView.ID);
            if (nwi)
            {
                primaryNetworkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.NwiBtn, null);
                primaryNetworkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.NwiBtn).Click();
            }
            else if (setupNodeLifelineSettings != SetupNodeLifelineSettings.Default)
            {
                ApplyCustomAddNodeParameters(includingUi, setupNodeLifelineSettings);
            }
            else
            {
                primaryNetworkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.AddBtn, null);
                primaryNetworkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.AddBtn).Click();
            }
            includingUi.Automation.WaitForElementIsReady(PCControllerIdMap.MainWindow.BusyView, null, WAIT_ELEMENT_READY_TIMEOUT_MS);
        }

        private static void ApplyCustomAddNodeParameters(UIInstance includingUi, SetupNodeLifelineSettings setupNodeLifelineSettings)
        {
            includingUi.Automation.NavigateTo(PCControllerIdMap.NetworkManagementView.AddCustomBtn).Click();
            var isAddNodeCustomShown = includingUi.Automation.CheckVisible(PCControllerIdMap.DialogWindow.ID) ||
                                       includingUi.Automation.WaitForDialogIsVisible(PCControllerIdMap.DialogWindow.ID,
                                               PCControllerIdMap.AddNodeWithCustomSettingsView.CAPTION, WAIT_DLG_TIMEOUT_MS * 2);
            if (isAddNodeCustomShown)
            {
                var dlgActive = includingUi.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, PCControllerIdMap.AddNodeWithCustomSettingsView.CAPTION);
                if (dlgActive != null)
                {
                    try
                    {
                        var isSetAsSisAutomaticallyChk = PCControllerIdMap.AddNodeWithCustomSettingsView.IsSetAsSisAutomaticallyChk;
                        dlgActive.WaitForElementIsReady(isSetAsSisAutomaticallyChk, null);
                        dlgActive.NavigateTo(isSetAsSisAutomaticallyChk).TrySetCheckBoxStateAt(setupNodeLifelineSettings.HasFlag(SetupNodeLifelineSettings.IsSetAsSisAutomatically));
                        if (setupNodeLifelineSettings.HasFlag(SetupNodeLifelineSettings.IsSetAsSisAutomatically))
                        {
                            var isBasedOnZwpRoleTypeChk =
                                PCControllerIdMap.AddNodeWithCustomSettingsView.IsBasedOnZwpRoleTypeChk;
                            dlgActive.WaitForElementIsReady(isBasedOnZwpRoleTypeChk, null);
                            dlgActive.NavigateTo(isBasedOnZwpRoleTypeChk).TrySetCheckBoxStateAt(setupNodeLifelineSettings.HasFlag(SetupNodeLifelineSettings.IsBasedOnZwpRoleType));
                        }

                        var isDeleteReturnRouteChk = PCControllerIdMap.AddNodeWithCustomSettingsView.IsDeleteReturnRouteChk;
                        dlgActive.WaitForElementIsReady(isDeleteReturnRouteChk, null);
                        dlgActive.NavigateTo(isDeleteReturnRouteChk).TrySetCheckBoxStateAt(setupNodeLifelineSettings.HasFlag(SetupNodeLifelineSettings.IsDeleteReturnRoute));

                        var isAssignReturnRouteChk = PCControllerIdMap.AddNodeWithCustomSettingsView.IsAssignReturnRouteChk;
                        dlgActive.WaitForElementIsReady(isAssignReturnRouteChk, null);
                        dlgActive.NavigateTo(isAssignReturnRouteChk).TrySetCheckBoxStateAt(setupNodeLifelineSettings.HasFlag(SetupNodeLifelineSettings.IsAssignReturnRoute));

                        var isAssociationCreateChk = PCControllerIdMap.AddNodeWithCustomSettingsView.IsAssociationCreateChk;
                        dlgActive.WaitForElementIsReady(isAssociationCreateChk, null);
                        dlgActive.NavigateTo(isAssociationCreateChk).TrySetCheckBoxStateAt(setupNodeLifelineSettings.HasFlag(SetupNodeLifelineSettings.IsAssociationCreate));

                        var isMultichannelAssociationCreateChk = PCControllerIdMap.AddNodeWithCustomSettingsView.IsMultichannelAssociationCreateChk;
                        dlgActive.WaitForElementIsReady(isMultichannelAssociationCreateChk, null);
                        dlgActive.NavigateTo(isMultichannelAssociationCreateChk).TrySetCheckBoxStateAt(setupNodeLifelineSettings.HasFlag(SetupNodeLifelineSettings.IsMultichannelAssociationCreate));

                        var isWakeUpCapabilitiesChk = PCControllerIdMap.AddNodeWithCustomSettingsView.IsWakeUpCapabilitiesChk;
                        dlgActive.WaitForElementIsReady(isWakeUpCapabilitiesChk, null);
                        dlgActive.NavigateTo(isWakeUpCapabilitiesChk).TrySetCheckBoxStateAt(setupNodeLifelineSettings.HasFlag(SetupNodeLifelineSettings.IsWakeUpCapabilities));

                        var isWakeUpIntervalChk = PCControllerIdMap.AddNodeWithCustomSettingsView.IsWakeUpIntervalChk;
                        dlgActive.WaitForElementIsReady(isWakeUpIntervalChk, null);
                        dlgActive.NavigateTo(isWakeUpIntervalChk).TrySetCheckBoxStateAt(setupNodeLifelineSettings.HasFlag(SetupNodeLifelineSettings.IsWakeUpInterval));
                        includingUi.Automation.NavigateTo(PCControllerIdMap.AddNodeWithCustomSettingsView.OkBtn).Click();
                    }
                    catch (InvalidOperationException ioe)
                    {
                        $"{ioe.Message}"._DLOG();
                    }
                }
            }
        }

        public BasicResult.AddNodeWithLearnMode AddNode(Alias controllerNumber, Alias nodeNumber)
        {
            return AddNode(controllerNumber, nodeNumber, INCLUSION_TIMEOUT_MS);
        }

        public BasicResult.AddNodeWithLearnMode AddNode(Alias controllerNumber, Alias nodeNumber, int timeoutMs)
        {
            return AddNode(controllerNumber, nodeNumber, Modes.NodeAny, timeoutMs);
        }

        public BasicResult.AddNodeWithLearnMode AddNode(Alias controllerNumber, Alias nodeNumber, Modes addMode, int timeoutMs)
        {
            return AddNode(controllerNumber, nodeNumber, addMode, LearnModes.LearnModeClassic, timeoutMs);
        }

        public int AddNodes(Alias controllerNumber, int timeoutMs, Alias[] nodeNumbers)
        {
            throw new NotImplementedException();
        }

        public int AddNodes(Alias controllerNumber, Alias[] nodeNumbers)
        {
            throw new NotImplementedException();
        }

        public int AddNodesWithDSK(Alias controllerNumber, Alias[] nodeNumbers, int dsk)
        {
            throw new NotImplementedException();
        }

        public int AddNodesWithDSK(Alias controllerNumber, Alias[] nodeNumbers, byte[] dsk)
        {
            throw new NotImplementedException();
        }

        public int AddNodesWithDSK(Alias controllerNumber, int timeoutMs, Alias[] nodeNumbers, byte[] dsk)
        {
            throw new NotImplementedException();
        }

        public int AddNodesWithDSK(Alias controllerNumber, int timeoutMs, Alias[] nodeNumbers, int dsk)
        {
            throw new NotImplementedException();
        }

        public List<BasicResult.AddNodeWithLearnMode> AddNodesWithDSK(Alias controllerNumber, Alias[] nodeNumbers, Modes addMode, LearnModes learnMode, int dsk, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.AddNode AddNodeVirtual(Alias controllerNumber, Alias bridgeNumber, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.AddNode AddNodeVirtual(Alias controllerNumber, Alias bridgeNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.AddNodeWithLearnMode AddNodeWithDSK(Alias controllerNumber, Alias nodeNumber, Modes addMode, LearnModes learnMode, int dsk, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.AddNodeWithLearnMode AddNodeWithDSK(Alias controllerNumber, Alias nodeNumber, int dsk, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.AddNodeWithLearnMode AddNodeWithDSK(Alias controllerNumber, Alias nodeNumber, byte[] dsk)
        {
            throw new NotImplementedException();
        }

        public BasicResult.AddNodeWithLearnMode AddNodeWithDSK(Alias controllerNumber, Alias nodeNumber, int dsk)
        {
            throw new NotImplementedException();
        }

        public BasicResult.AddNodeWithLearnMode AddNodeWithDSK(Alias controllerNumber, Alias nodeNumber, Modes addMode, LearnModes learnMode, byte[] dsk, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.AddNodeWithLearnMode AddNodeWithDSK(Alias controllerNumber, Alias nodeNumber, byte[] dsk, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void ApplicationNodeInformation(Alias deviceNumber, params byte[] cmdClasses)
        {
            if (PCControllerDeviceManager.HasDevice(deviceNumber))
            {
                return new PCControllerActionResult.Void(true);
            }
            else
            {
                return BasicService.ApplicationNodeInformation(deviceNumber, cmdClasses);
            }
        }

        public BasicResult.Void ApplicationNodeInformation(Alias deviceNumber, DeviceOptions deviceOptions, byte generic, byte specific, params byte[] cmdClasses)
        {
            if (PCControllerDeviceManager.HasDevice(deviceNumber))
            {
                return new PCControllerActionResult.Void(true);
            }
            else
            {
                return BasicService.ApplicationNodeInformation(deviceNumber, deviceOptions, generic, specific, cmdClasses);
            }
        }

        public BasicResult.Void ApplicationNodeInformation(Alias deviceNumber, bool isListening, byte generic, byte specific, params byte[] nodeParameters)
        {
            if (PCControllerDeviceManager.HasDevice(deviceNumber))
            {
                return new PCControllerActionResult.Void(true);
            }
            else
            {
                return BasicService.ApplicationNodeInformation(deviceNumber, isListening, generic, specific, nodeParameters);
            }
        }

        public BasicResult.Void ApplicationNodeInformationCmdClasses(Alias deviceNumber, byte[] nodeParameters, byte[] unsecureNodeParameters, byte[] secureNodeParameters)
        {
            if (PCControllerDeviceManager.HasDevice(deviceNumber))
            {
                return new PCControllerActionResult.Void(true);
            }
            else
            {
                return BasicService.ApplicationNodeInformationCmdClasses(deviceNumber, nodeParameters, unsecureNodeParameters, secureNodeParameters);
            }
        }

        public BasicResult.Void ApplicationVirtualDeviceNodeInformation(Alias bridgeNumber, bool isListening, byte generic, byte specific, params byte[] cmdClasses)
        {
            if (PCControllerDeviceManager.HasDevice(bridgeNumber))
            {
                return new PCControllerActionResult.Void(true);
            }
            else
            {
                return BasicService.ApplicationVirtualDeviceNodeInformation(bridgeNumber, isListening, generic, specific, cmdClasses);
            }
        }

        public BasicResult.AreNodesNeighbours AreNodesNeighbours(Alias controllerNumber, Alias nodeNumber1, Alias nodeNumber2)
        {
            if (PCControllerDeviceManager.HasDevice(controllerNumber))
            {
                throw new NotImplementedException();
            }
            else
            {
                return BasicService.AreNodesNeighbours(controllerNumber, nodeNumber1, nodeNumber2);
            }
        }

        public BasicResult.AssignReturnRoute AssignPriorityReturnRoute(Alias controllerNumber, Alias srcNode, Alias destNode, Node priorityRoute0, Node priorityRoute1, Node priorityRoute2, Node priorityRoute3, byte routespeed)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void AssignPrioritySucReturnRoute(Alias controllerNumber, Alias nodeNumber, Node repeater0, Node repeater1, Node repeater2, Node repeater3, byte routespeed)
        {
            throw new NotImplementedException();
        }

        public BasicResult.AssignReturnRoute AssignReturnRoute(Alias controllerNumber, Alias srcNode, Alias destNode)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void AssignSucReturnRoute(Alias controllerNumber, Alias nodeNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.AddNode ControllerChange(Alias deviceNumber, ControllerChangeModes mode)
        {
            throw new NotImplementedException();
        }

        public BasicResult.AddNode CreateNewPrimary(Alias controllerNumber, Alias newPrimaryControllerNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.AddNode CreateNewPrimary(Alias controllerNumber, Alias newPrimaryControllerNumber, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void DeleteReturnRoute(Alias controllerNumber, Alias nodeNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void DeleteSucReturnRoute(Alias controllerNumber, Alias nodeNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void EnableSUC(Alias deviceNumber, bool isEnable, byte capabilities)
        {
            throw new NotImplementedException();
        }

        public BasicResult.RemoveNode ExcludeNode(Alias controllerNumber, Alias nodeNumber, Modes removeMode, LearnModes learnMode, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.RemoveNode ExcludeNode(Alias controllerNumber, Alias nodeNumber, Modes removeMode, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.RemoveNode ExcludeNode(Alias controllerNumber, Alias nodeNumber)
        {
            return RemoveNode(controllerNumber, nodeNumber);
        }

        public BasicResult.RemoveNode ExcludeNode(Alias controllerNumber, Alias nodeNumber, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.ExpectData ExpectData(Alias deviceNumber, byte[] expectedData)
        {
            throw new NotImplementedException();
        }

        public BasicResult.ExpectData ExpectData(Alias deviceNumber, byte[] expectedData, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void ExploreRequestExclusion(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void ExploreRequestInclusion(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.FirmwareUpdateNvmInit FirmwareUpdateNvmInit(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.FirmwareUpdateNvmSetNewImage FirmwareUpdateNvmSetNewImage(Alias deviceNumber, bool isSet)
        {
            throw new NotImplementedException();
        }

        public BasicResult.FirmwareUpdateNvmGetNewImage FirmwareUpdateNvmGetNewImage(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.FirmwareUpdateNvmUpdateCrc16 FirmwareUpdateNvmUpdateCrc16(Alias deviceNumber, int offset, ushort length, ushort seed)
        {
            throw new NotImplementedException();
        }

        public BasicResult.FirmwareUpdateNvmIsValidCrc16 FirmwareUpdateNvmIsValidCrc16(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.FirmwareUpdateNvmWrite FirmwareUpdateNvmWrite(Alias deviceNumber, int offset, ushort length, byte[] buffer)
        {
            throw new NotImplementedException();
        }

        public BasicResult.FirmwareUpdateNvmWrite FirmwareUpdateNvmWrite(Alias deviceNumber, ushort blockSize, string filePath)
        {
            throw new NotImplementedException();
        }

        public BasicResult.GetControllerCapabilities GetControllerCapabilities(Alias controllerNumber)
        {
            throw new NotImplementedException();
        }

        public string GetDevicePort(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.GetNeighborCount GetNeighborCount(Alias controllerNumber, Alias nodeNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.GetPriorityRoute GetPriorityRoute(Alias controllerNumber, Alias nodeNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.GetProtocolInfo GetProtocolInfo(Alias controllerNumber, Alias requestedDeviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.GetProtocolStatus GetProtocolStatus(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.GetRandomWord GetRandomWord(Alias deviceNumber, byte count)
        {
            throw new NotImplementedException();
        }

        public BasicResult.GetRoutingInfo GetRoutingInfo(Alias controllerNumber, Alias nodeNumber, byte removeBad, byte removeNonReps)
        {
            throw new NotImplementedException();
        }

        public BasicResult.GetSecurityKeys GetSecurityKeys(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.GetSecurityS2PublicDSK GetSecurityS2PublicDSK(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.GetSucNodeId GetSucNodeId(Alias controllerNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.GetTransmitCount GetTransmitCount(Alias installerNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.GetVersion GetVersion(Alias deviceNumber)
        {
            var uiInstance = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            var networkManagementView = FromMainMenuPage(uiInstance, PCControllerIdMap.NetworkManagementView.ID);

            var controllerInfoTree = networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.ControllerInfoTvw);

            var infoNode = controllerInfoTree.TreeViewGetFirstChild().TreeViewGetFirstChild().TreeViewGetNextSibling();
            string versionString = string.Empty;
            while (infoNode != null && !versionString.Contains("Z-Wave device firmware:"))
            {
                var textBox = infoNode.NavigateToClassNameProperty("TextBox");
                versionString = textBox.GetElementValue();
                infoNode = infoNode.TreeViewGetNextSibling();
            }
            var regex = new Regex(@"\d{1,2}[\.]{1}\d{1,2}"); // Regex for Number with dot
            var numsString = regex.Match(versionString);
            VersionResult res = new VersionResult();
            res.Version = numsString.ToString();

            return new PCControllerActionResult.GetVersion(res);
        }

        public BasicResult.GetVirtualNodes GetVirtualNodes(Alias bridgeNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.IncludeNodeWithLearnMode IncludeNode(Alias controllerNumber, Alias nodeNumber)
        {
            var res = AddNode(controllerNumber, nodeNumber);
            var ret = new PCControllerActionResult.IncludeNodeWithLearnMode(res);
            return ret;
        }

        public BasicResult.IncludeNodeWithLearnMode IncludeNode(Alias controllerNumber, Alias nodeNumber, int timeoutMs)
        {
            var res = AddNode(controllerNumber, nodeNumber, timeoutMs);
            var ret = new PCControllerActionResult.IncludeNodeWithLearnMode(res);
            return ret;
        }

        public BasicResult.IncludeNodeWithLearnMode IncludeNode(Alias controllerNumber, Alias nodeNumber, Modes addMode, int timeoutMs)
        {
            var res = AddNode(controllerNumber, nodeNumber, addMode, timeoutMs);
            var ret = new PCControllerActionResult.IncludeNodeWithLearnMode(res);
            return ret;
        }

        public BasicResult.IncludeNodeWithLearnMode IncludeNode(Alias controllerNumber, Alias nodeNumber, Modes addMode, LearnModes learnMode, int timeoutMs)
        {
            var res = AddNode(controllerNumber, nodeNumber, addMode, learnMode, timeoutMs);
            var ret = new PCControllerActionResult.IncludeNodeWithLearnMode(res);
            return ret;
        }

        public BasicResult.IsFailedNode IsFailedNode(Alias controllerNumber, Alias nodeNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.IsNodeWithinDirectRange IsNodeWithinDirectRange(Alias endDeviceNumber, Alias nodeNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.IsVirtualNode IsVirtualNode(Alias bridgeNumber, byte nodeId)
        {
            throw new NotImplementedException();
        }

        public BasicResult.MemoryGetBuffer MemoryGetBuffer(Alias deviceNumber, ushort offset, byte length)
        {
            throw new NotImplementedException();
        }

        public BasicResult.MemoryGetByte MemoryGetByte(Alias deviceNumber, ushort offset)
        {
            throw new NotImplementedException();
        }

        public BasicResult.MemoryGetId MemoryGetId(Alias deviceNumber)
        {
            var uiInstance = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (uiInstance == null)
            {
                return BasicService.MemoryGetId(deviceNumber);
            }

            var networkManagementView = FromMainMenuPage(uiInstance, PCControllerIdMap.NetworkManagementView.ID);
            MemoryGetIdResult res = new MemoryGetIdResult();
            var controllerInfoTree = networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.ControllerInfoTvw);
            var infoNode = controllerInfoTree.TreeViewGetFirstChild().TreeViewGetFirstChild().TreeViewGetNextSibling();
            string currentItem = string.Empty;
            while (infoNode != null)
            {
                var textBox = infoNode.NavigateToClassNameProperty("TextBox");
                currentItem = textBox.GetElementValue();

                if (currentItem.StartsWith("Id: "))
                {
                    string strval = currentItem.Replace("Id: ", "");
                    res.Node = new ZWave.Devices.NodeTag(Tools.GetByte(strval));
                }
                else if (currentItem.StartsWith("Home Id: "))
                {
                    string strval = currentItem.Replace("Home Id: ", "");
                    res.HomeId = Tools.GetBytes(strval);
                }

                infoNode = infoNode.TreeViewGetNextSibling();
            }
            return new PCControllerActionResult.MemoryGetId(res);
        }

        public BasicResult.Void MemoryPutBuffer(Alias deviceNumber, ushort offset, ushort length, byte[] data)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void MemoryPutByte(Alias deviceNumber, ushort offset, byte data)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void NvmBackupRestoreClose(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.NvmBackupRestoreOpen NvmBackupRestoreOpen(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.NvmBackupRestoreRead NvmBackupRestoreRead(Alias deviceNumber, byte length, int offset)
        {
            throw new NotImplementedException();
        }

        public BasicResult.NvmBackupRestoreWrite NvmBackupRestoreWrite(Alias deviceNumber, byte length, int offset, byte[] data)
        {
            throw new NotImplementedException();
        }

        public BasicResult.RandomByte RandomByte(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public bool Reconnect(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void RediscoveryNeeded(Alias endDeviceNumber, Alias nodeNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void RediscoveryNeededBroadcast(Alias endDeviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void RemoveFailedNodeId(Alias controllerNumber, Alias nodeNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.RemoveNode RemoveNode(Alias controllerNumber, Alias nodeNumber, int timeoutMs)
        {
            return RemoveNode(controllerNumber, nodeNumber, Modes.NodeAny, LearnModes.LearnModeClassic, timeoutMs);
        }

        public BasicResult.RemoveNode RemoveNode(Alias controllerNumber, Alias nodeNumber, Modes removeMode, LearnModes learnMode, int timeoutMs)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            uiController.ClearLog();
            var networkManagementView = FromMainMenuPage(uiController, PCControllerIdMap.NetworkManagementView.ID);
            var primaryMainWindow = uiController.Automation.NavigateTo(PCControllerIdMap.MainWindow.ID);
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(nodeNumber);

            var nwe = (removeMode == Modes.NodeOptionNetworkWide);
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

            if (nodeControllerUI != null)
            {
                nodeControllerUI.ClearLog();
                SetLearnMode(nodeControllerUI, learnMode);
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
            }
            else
            {
                Thread.Sleep(1000);
                BasicService.SetLearnMode(nodeNumber, LearnModes.LearnModeClassic, INCLUSION_TIMEOUT_MS);
            }

            BasicResult.RemoveNode ret = null;

            if (removeMode.HasFlag(Modes.NodeOptionNetworkWide))
            {
                primaryMainWindow.WaitForElementIsVisible(PCControllerIdMap.MainWindow.BusyView, null);
                primaryMainWindow.NavigateTo(PCControllerIdMap.MainWindow.BusyView).NavigateTo(PCControllerIdMap.MainWindow.AbortBtn).Click();
                primaryMainWindow.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                ret = new PCControllerActionResult.RemoveNodeNWE(uiController);
            }
            else
            {
                primaryMainWindow.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                ret = new PCControllerActionResult.RemoveNode(uiController);
            }
            return ret;
        }

        public BasicResult.RemoveNode RemoveNode(Alias controllerNumber, Alias nodeNumber)
        {
            return RemoveNode(controllerNumber, nodeNumber, Modes.NodeAny, LearnModes.LearnModeClassic, INCLUSION_TIMEOUT_MS);
        }

        public int RemoveNodes(Alias controllerNumber, int timeoutMs, params Alias[] nodeNumbers)
        {
            int ret = 0;
            if (nodeNumbers != null && nodeNumbers.Length > 0)
            {
                foreach (var item in nodeNumbers)
                {
                    var nodeRemoveRes = RemoveNode(controllerNumber, item, timeoutMs);
                    if (nodeRemoveRes)
                    {
                        ret++;
                    }
                }
            }
            return ret;
        }

        public int RemoveNodes(Alias controllerNumber, params Alias[] nodeNumbers)
        {
            return RemoveNodes(controllerNumber, INCLUSION_TIMEOUT_MS, nodeNumbers);
        }

        public BasicResult.Void ReplaceFailedNode(Alias controllerNumber, Alias nodeNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void ReplicationReceiveComplete(Alias controllerNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Transmit ReplicationSend(Alias controllerNumber, Alias nodeNumber, byte[] data, TransmitOptions txOptions)
        {
            throw new NotImplementedException();
        }

        public BasicResult.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, byte[] expectedData)
        {
            BasicResult.RequestData ret = null;
            var uiController = PCControllerDeviceManager.GetUIInstance(senderNumber);
            if (uiController != null)
            {
                uiController.ClearLog();
                var cmdClassesView = FromMainMenuPage(uiController, PCControllerIdMap.CommandClassesView.ID);
                cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.TbCommandToSend).SetElementValue(Utils.Tools.GetHex(data));

                var nid = MemoryGetId(receiverNumber).NodeId;
                uiController.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                    .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                    .NavigateToNameProperty(nid.ToString())
                    .Select();

                cmdClassesView.WaitForElementIsReady(PCControllerIdMap.CommandClassesView.BtnSend, null);
                cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.BtnSend).Click();
                Thread.Sleep(1000);

                ret = new PCControllerActionResult.RequestData(uiController, PCControllerDeviceManager.ZWaveDefinition, (byte)receiverNumber.Value, expectedData);
            }
            else
            {
                ret = BasicService.RequestData(senderNumber, receiverNumber, data, expectedData);
            }
            return ret;
        }

        public BasicResult.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, byte[] expectedData, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.RequestData RequestDataBridge(Alias senderNumber, Alias receiverNumber, Node srcNode, byte[] data, TransmitOptions txOptions, byte[] expectedData, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.RequestData RequestDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, byte[] expectedData, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.RequestData RequestDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, SecuritySchemes scheme, byte[] expectedData)
        {
            BasicResult.RequestData ret = null;
            var uiController = PCControllerDeviceManager.GetUIInstance(senderNumber);
            if (uiController != null)
            {
                var cmdClassesView = FromMainMenuPage(uiController, PCControllerIdMap.CommandClassesView.ID);
                cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.TbCommandToSend).SetElementValue(Tools.GetHex(data));
                uiController.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                    .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                    .NavigateToNameProperty(receiverNumber.Value.ToString())
                    .Select();

                switch (scheme)
                {
                    case SecuritySchemes.NONE:
                        cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.RbNonSercure).SelectRadioButtonItem();
                        break;
                    case SecuritySchemes.S0:
                    case SecuritySchemes.S2_ACCESS:
                    case SecuritySchemes.S2_AUTHENTICATED:
                    case SecuritySchemes.S2_UNAUTHENTICATED:
                        cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.RbSecure).SelectRadioButtonItem();
                        break;
                    case SecuritySchemes.S2_TEMP:
                    default:
                        cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.RbDefault).SelectRadioButtonItem();
                        break;
                }

                cmdClassesView.WaitForElementIsReady(PCControllerIdMap.CommandClassesView.BtnSend, null);
                cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.BtnSend).Click();
                Thread.Sleep(1000);

                ret = new PCControllerActionResult.RequestData(uiController, PCControllerDeviceManager.ZWaveDefinition, (byte)receiverNumber.Value, expectedData);
            }
            else
            {
                ret = BasicService.RequestData(senderNumber, receiverNumber, data, expectedData);
            }

            return ret;
        }

        public BasicResult.RequestData RequestDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2, byte[] expectedData, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.RequestNetworkUpdate RequestNetworkUpdate(Alias deviceNumber)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.MainMenuBtn).Click();
                nodeControllerUI.Automation.WaitForElementIsVisible(PCControllerIdMap.NetworkManagementView.ID,
                    () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainMenuView.NetworkManagementBtn).Click());
                var networkManagementView = nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.NetworkManagementView.ID);

                networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.UpdateBtn).ClickIfEnabled();
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
            }
            return new BasicResult.RequestNetworkUpdate();
        }

        public BasicResult.Void RequestNewRouteDestinations(Alias endDeviceNumber, byte[] destList)
        {
            throw new NotImplementedException();
        }

        public BasicResult.RequestNodeInfo RequestNodeInfo(Alias deviceNumber, Alias nodeNumber)
        {
            BasicResult.RequestNodeInfo ret = null;
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.MainMenuBtn).Click();
                nodeControllerUI.Automation.WaitForElementIsVisible(PCControllerIdMap.NetworkManagementView.ID,
                    () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainMenuView.NetworkManagementBtn).Click());
                var networkManagementView = nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.NetworkManagementView.ID);

                var nid = MemoryGetId(nodeNumber).NodeId;
                nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                    .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                    .NavigateToNameProperty(nid.ToString())
                    .Select();
                networkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.NodeInfoBtn, null);

                networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.NodeInfoBtn).Click();
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);

                ret = new PCControllerActionResult.RequestNodeInfo(nodeControllerUI, PCControllerDeviceManager.ZWaveDefinition);
            }
            return ret;
        }

        public BasicResult.GetNodeInfo GetNodeInfo(Alias deviceNumber, Alias nodeNumber)
        {
            BasicResult.GetNodeInfo ret = null;
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.MainMenuBtn).Click();
                nodeControllerUI.Automation.WaitForElementIsVisible(PCControllerIdMap.NetworkManagementView.ID,
                    () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainMenuView.NetworkManagementBtn).Click());
                var networkManagementView = nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.NetworkManagementView.ID);

                var nid = MemoryGetId(nodeNumber).NodeId;
                networkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.NodeInfoBtn,
                   () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                   .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                   .NavigateToNameProperty(nid.ToString())
                   .Select());

                networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.NodeInfoBtn).Click();
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);

                ret = new PCControllerActionResult.GetNodeInfo(nodeControllerUI, PCControllerDeviceManager.ZWaveDefinition);
            }
            return ret;
        }

        public BasicResult.RequestNodeNeighborUpdate RequestNodeNeighborUpdate(Alias controllerNumber, Alias nodeNumber)
        {
            BasicResult.RequestNodeNeighborUpdate ret = null;
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.MainMenuBtn).Click();
                nodeControllerUI.Automation.WaitForElementIsVisible(PCControllerIdMap.NetworkManagementView.ID,
                    () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainMenuView.NetworkManagementBtn).Click());
                var networkManagementView = nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.NetworkManagementView.ID);

                networkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.NeighborsUpdateBtn,
                   () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                   .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                   .NavigateToNameProperty(nodeNumber.Value.ToString())
                   .Select());

                networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.NeighborsUpdateBtn).Click();
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);

                ret = new PCControllerActionResult.RequestNodeNeighborUpdate(nodeControllerUI, PCControllerDeviceManager.ZWaveDefinition);
            }
            return ret;
        }

        public void Reset(Alias deviceNumber)
        {
            if (!PCControllerDeviceManager.HasDevice(deviceNumber))
            {
                BasicService.SetDefault(deviceNumber);
            }
            var uiInstance = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            var networkManagementView = FromMainMenuPage(uiInstance, PCControllerIdMap.NetworkManagementView.ID);
            networkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.ResetBtn, null);
            networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.ResetBtn).Click();
            //Reset SIS alias
            if (PCControllerDeviceManager.SisController.Value != 0 && PCControllerDeviceManager.SisController.Value == deviceNumber.Value)
            {
                PCControllerDeviceManager.SisController = new Alias(0);
            }
            networkManagementView.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null, WAIT_ELEMENT_READY_TIMEOUT_MS);
        }

        public BasicResult.Void ResetTransmitCount(Alias installerNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.RFPowerLevelGet RFPowerLevelGet(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void RFPowerlevelRediscoverySet(Alias deviceNumber, byte powerLevel)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void RFPowerLevelSet(Alias deviceNumber, byte powerLevel)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Transmit SendBroadCastNodeInfo(Alias deviceNumber, TransmitOptions txOptions)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            var networkManagementView = FromMainMenuPage(uiController, PCControllerIdMap.NetworkManagementView.ID);

            networkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.SendNodeInfoBtn, null, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);
            networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.SendNodeInfoBtn).Click();
            return new BasicResult.Transmit();
        }

        public BasicResult.SendData SendData(Alias senderNumber, Alias receiverNumber, byte[] data)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(senderNumber);
            var cmdClassesView = FromMainMenuPage(uiController, PCControllerIdMap.CommandClassesView.ID);

            PCControllerDeviceManager.ZWaveDefinition.ParseApplicationObject(data, out CommandClassValue[] parsedCCApplications);
            if (parsedCCApplications != null && parsedCCApplications.Length > 0)
            {
                var ccLatestVersion = parsedCCApplications.Max(x => x.CommandClassDefinition.Version);
                var ccApplication = parsedCCApplications.First(x => x.CommandClassDefinition.Version == ccLatestVersion);
                string className = ccApplication.CommandClassDefinition.Name + "_V" + ccLatestVersion;
                string operationName = ccApplication.CommandValue.CommandDefinition.Name;
                cmdClassesView.WaitForElementIsReady(PCControllerIdMap.CommandClassesView.RbDefault, null, WAIT_ELEMENT_READY_TIMEOUT_MS);
                cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.RbDefault).SelectRadioButtonItem();
                //bool b = ccApplication.CommandValue.ParamValues.Any(x => x.ParamDefinition.Type == zwParamType.BITMASK);
                if (data.Length < MAX_FRAME_SIZE /*&& !b*/)
                {
                    using (var dlgDsk = uiController.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Select Command",
                            () => uiController.Automation.NavigateTo(PCControllerIdMap.CommandClassesView.ShowSelectCommandBtn).Click(),
                            () =>
                            {
                                if (uiController.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Select Command").CheckVisible(PCControllerIdMap.SelectCommandView.OkBtn))
                                {
                                    uiController.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Select Command").NavigateTo(PCControllerIdMap.SelectCommandView.OkBtn).Click();
                                }
                                else
                                {
                                    uiController.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Select Command").CloseWindow();
                                }
                            }))
                    {
                        dlgDsk.DialogView.NavigateTo(PCControllerIdMap.SelectCommandView.AllCmdClassesChk).SetCheckBoxStateAt(true);
                        var commandClass = dlgDsk.DialogView.NavigateTo(PCControllerIdMap.SelectCommandView.ID)
                            .NavigateTo(PCControllerIdMap.SelectCommandView.SelectCommandClassesTvw)
                            .NavigateTo(className);
                        if (commandClass != null)
                        {
                            commandClass.SelectComboboxItem(operationName);
                        }
                    }

                    for (int i = 0; i < ccApplication.CommandValue.ParamValues.Count; i++)
                    {
                        var item = parsedCCApplications[VERSION].CommandValue.CommandDefinition.Param[i];
                        var paramValue = parsedCCApplications[VERSION].CommandValue.ParamValues[i];
                        uiController.Automation.WaitForElementIsReady(PCControllerIdMap.CommandClassesView.CommandClassesInfoTvw, null, WAIT_ELEMENT_READY_TIMEOUT_MS);
                        var TreeElementCCInfo = uiController.Automation.NavigateTo(PCControllerIdMap.CommandClassesView.CommandClassesInfoTvw);

                        if (item.Mode == ParamModes.Param)
                        {
                            if (paramValue.ParamDefinition.Type == zwParamType.CMD_CLASS_REF)
                            {
                                if (paramValue.ParamDefinition.SizeReference == "MSG_LENGTH")
                                {
                                    paramValue.TextValueList.Clear();
                                    paramValue.TextValueList.Add(paramValue.ByteValueList.ToHexString());
                                }
                                else
                                {
                                    _commandClass = PCControllerDeviceManager.ZWaveDefinition.CommandClasses.OrderByDescending(x => x.Version).
                                        FirstOrDefault(x => x.Key == Tools.GetHex(paramValue.ByteValueList[0], true));
                                    if (_commandClass != null)
                                    {
                                        paramValue.TextValueList.Clear();
                                        paramValue.TextValueList.Add(string.Format("{0} {1}", _commandClass.Name, _commandClass.Version));
                                    }
                                }
                            }
                            else if (paramValue.ParamDefinition.Type == zwParamType.CMD_REF)
                            {
                                paramValue.TextValueList.Clear();
                                var command = _commandClass.Command.Where(x => x.KeyId == paramValue.ByteValueList[0]).FirstOrDefault();
                                paramValue.TextValueList.Add(string.Format("{0}", command.ToString().ToUpper()));
                            }
                            else if (paramValue.ParamDefinition.Type == zwParamType.BITMASK && !string.IsNullOrWhiteSpace(paramValue.TextValue))
                            {
                                var treeNode = TreeElementCCInfo.NavigateTo(item.Name);
                                var wlkr = treeNode.TryNavigateToClassNameProperty("ListBox");
                                bool isFirst = true;
                                foreach (var tv in paramValue.TextValue.Split(','))
                                {
                                    string nameCondition = string.Concat(tv.Trim(' ').TakeWhile(x => Char.IsLetterOrDigit(x)));
                                    var bitmaskItem = wlkr.TryNavigateToNameProperty(nameCondition);
                                    if (bitmaskItem != null)
                                    {
                                        if (isFirst)
                                        {
                                            bitmaskItem.Select();
                                            isFirst = false;
                                        }
                                        else
                                        {
                                            bitmaskItem.AddToSelection();
                                        }
                                    }
                                    else
                                    {
                                        throw new InvalidOperationException("CC: No item found in list with name: " + nameCondition);
                                    }
                                }
                            }

                            //Set value in view main tree
                            if (paramValue.InnerValues != null)
                            {
                                ParamValue ParamValue = paramValue.InnerValues[VERSION];
                                SetParamsInTree(TreeElementCCInfo, ParamValue);
                            }
                            else if (paramValue.ByteValueList.Count > 0)
                            {
                                string paramName = item.Name;
                                string TextValue = paramValue.TextValue;
                                SetParamInTreeNode(TreeElementCCInfo, TextValue, paramName);
                            }
                        }
                        else if (item.Mode == ParamModes.VariantGroup)
                        {
                            string paramName = item.Name;
                            var TreeNode = TreeElementCCInfo.NavigateTo(paramName);
                            TreeNode.Select();
                            TreeNode.NavigateTo(string.Empty, "+").Click();

                            SetParamsInTree(TreeElementCCInfo, paramValue);
                        }
                        else if (item.Mode == ParamModes.Property)
                        {
                            SetParamsInTree(TreeElementCCInfo, paramValue);
                        }
                    }
                }
                else
                {
                    cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.TbCommandToSend).SetElementValue(Tools.GetHex(data));
                }
            }
            else
            {
                cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.TbCommandToSend).SetElementValue(Tools.GetHex(data));
            }
            var nid = MemoryGetId(receiverNumber).NodeId;
            uiController.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                .NavigateToNameProperty(nid.ToString())
                .Select();

            cmdClassesView.WaitForElementIsReady(PCControllerIdMap.CommandClassesView.BtnSend, null);
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.BtnSend).Click();

            return new PCControllerActionResult.SendData(uiController);
        }

        public BasicResult.SendData SendData(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions)
        {
            //throw new NotImplementedException();
            return SendData(senderNumber, receiverNumber, data);
        }

        public BasicResult.Void SendDataAbort(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.SendData SendDataBridge(Alias bridgeNumber, Node srcNode, Alias destNodeNumber, byte[] data, TransmitOptions txOptions)
        {
            throw new NotImplementedException();
        }

        public BasicResult.SendData SendDataBridge(Alias bridgeNumber, Node srcNode, Node destNode, byte[] data, TransmitOptions txOptions)
        {
            throw new NotImplementedException();
        }

        public BasicResult.SendData SendDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2)
        {
            throw new NotImplementedException();
        }

        public BasicResult.SendData SendDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, SecuritySchemes scheme)
        {
            throw new NotImplementedException();
        }

        public BasicResult.SendData SendDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, SecuritySchemes scheme)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(senderNumber);
            if (uiController != null)
            {
                var cmdClassesView = FromMainMenuPage(uiController, PCControllerIdMap.CommandClassesView.ID);
                cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.TbCommandToSend).SetElementValue(Utils.Tools.GetHex(data));

                switch (scheme)
                {
                    case SecuritySchemes.NONE:
                        cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.RbNonSercure).SelectRadioButtonItem();
                        break;
                    case SecuritySchemes.S0:
                    case SecuritySchemes.S2_ACCESS:
                    case SecuritySchemes.S2_AUTHENTICATED:
                    case SecuritySchemes.S2_UNAUTHENTICATED:
                        cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.RbSecure).SelectRadioButtonItem();
                        break;
                    case SecuritySchemes.S2_TEMP:
                    default:
                        cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.RbDefault).SelectRadioButtonItem();
                        break;
                }

                var nid = MemoryGetId(receiverNumber).NodeId;
                uiController.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                    .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                    .NavigateToNameProperty(nid.ToString())
                    .Select();

                cmdClassesView.WaitForElementIsReady(PCControllerIdMap.CommandClassesView.BtnSend, null);
                cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.BtnSend).Click();

                return new PCControllerActionResult.SendData(uiController);
            }
            else
            {
                var ret = BasicService.SendDataEx(senderNumber, receiverNumber, data, scheme);
                return ret;
            }
        }

        public BasicResult.Transmit SendDataMetaBridge(Alias bridgeNumber, Node srcNode, Alias destNodeNumber, byte[] data, TransmitOptions txOptions)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Transmit SendDataMulti(Alias senderNumber, Alias[] receiverNumbers, byte[] data, TransmitOptions txOptions)
        {
            var nids = new Node[receiverNumbers.Length];
            for (int i = 0; i < receiverNumbers.Length; i++)
            {
                nids[i] = MemoryGetId(receiverNumbers[i]).NodeId;
            }
            return _complianceService.SendDataMulti(senderNumber, nids, data, txOptions);
        }

        public BasicResult.Transmit SendDataMultiBridge(Alias bridgeNumber, Node srcNode, Alias[] destNodeNumbers, byte[] data, TransmitOptions txOptions)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Transmit SendNodeInfo(Alias deviceNumber, Alias receiverNumber, TransmitOptions txOptions)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Transmit SendNop(Alias senderNumber, Alias receiverNumber, TransmitOptions txOptions)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(senderNumber);
            var networkManagementView = FromMainMenuPage(uiController, PCControllerIdMap.NetworkManagementView.ID);
            var primaryMainWindow = uiController.Automation.NavigateTo(PCControllerIdMap.MainWindow.ID);

            uiController.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                .NavigateToNameProperty(receiverNumber.Value.ToString())
                .Select();

            uiController.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                .NavigateToNameProperty(receiverNumber.Value.ToString())
                .NavigateTo(PCControllerIdMap.NodesListView.EnableQOChk)
                .TrySetCheckBoxStateAt(true);

            networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.NopBtn).Click();
            primaryMainWindow.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);

            uiController.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                .NavigateToNameProperty(receiverNumber.Value.ToString())
                .NavigateTo(PCControllerIdMap.NodesListView.EnableQOChk)
                .TrySetCheckBoxStateAt(false);

            return new PCControllerActionResult.SendDataNop(uiController);
        }

        public BasicResult.Transmit EnqueueSendNop(Alias senderNumber, Alias receiverNumber, TransmitOptions txOptions)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(senderNumber);
            var networkManagementView = FromMainMenuPage(uiController, PCControllerIdMap.NetworkManagementView.ID);
            var primaryMainWindow = uiController.Automation.NavigateTo(PCControllerIdMap.MainWindow.ID);

            uiController.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                .NavigateToNameProperty(receiverNumber.Value.ToString())
                .Select();

            primaryMainWindow.NavigateTo(PCControllerIdMap.NetworkManagementView.NopBtn).Click();
            primaryMainWindow.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);

            return new PCControllerActionResult.SendDataNopEnqueue(uiController);
        }
        public BasicResult.Transmit SendVirtualDeviceNodeInformation(Alias bridgeNumber, Node srcNode, Alias destNodeNumber, TransmitOptions txOptions)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Transmit SendSucId(Alias controllerNumber, Alias nodeNumber, TransmitOptions txOptions)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Transmit SendTestFrame(Alias deviceNumber, Alias nodeNumber, byte powerLevel)
        {
            throw new NotImplementedException();
        }

        public BasicResult.SerialApiGetCapabilities SerialApiGetCapabilities(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.SerialApiGetInitData SerialApiGetInitData(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.SerialApiGetLRNodes SerialApiGetLRNodes(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void SerialApiTest(Alias deviceNumber, byte testCmd, ushort testDelay, byte testPayloadLength, ushort testCount, TransmitOptions txOptions, bool isStopOnErrors, params Alias[] nodeNumbers)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void SetDefault(Alias deviceNumber)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (uiController == null)
            {
                BasicService.SetDefault(deviceNumber);
            }
            else
            {
                var primaryMainWindow = uiController.Automation.NavigateTo(PCControllerIdMap.MainWindow.ID);
                var networkManagementView = FromMainMenuPage(uiController, PCControllerIdMap.NetworkManagementView.ID);
                networkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.ResetBtn, null);
                networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.ResetBtn).Click();
                //Reset SIS alias
                if (PCControllerDeviceManager.SisController.Value != 0 && PCControllerDeviceManager.SisController.Value == deviceNumber.Value)
                {
                    PCControllerDeviceManager.SisController = new Alias(0);
                }
                primaryMainWindow.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
            }
            return new BasicResult.Void();
        }

        public BasicResult.Void SetExtIntLevel(Alias deviceNumber, byte intSrc, bool triggerLevel)
        {
            throw new NotImplementedException();
        }

        public BasicResult.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode, bool isNonSecure, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode)
        {
            throw new NotImplementedException();
        }

        public BasicResult.SetPriorityRoute SetPriorityRoute(Alias controllerNumber, Alias nodeNumber, Node priority0, Node priority1, Node priority2, Node priority3, byte routespeed)
        {
            var nid = MemoryGetId(nodeNumber).NodeId;
            return _complianceService.SetPriorityRoute(controllerNumber, nid, priority0, priority1, priority2, priority3, routespeed);
        }

        public BasicResult.SetPriorityRoute SetPriorityRoute(Alias controllerNumber, Node node, Node repeater0, Node repeater1, Node repeater2, Node repeater3, byte routespeed)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void SetPromiscuousMode(Alias deviceNumber, bool state)
        {
            throw new NotImplementedException();
        }

        public BasicResult.ReturnValue SetRFReceiveMode(Alias deviceNumber, bool mode)
        {
            BasicResult.ReturnValue res = null;
            var deviceNumberUi = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (deviceNumberUi == null)
            {
                res = BasicService.SetRFReceiveMode(deviceNumber, mode);
            }
            else
            {
                throw new NotImplementedException();
            }
            return res;
        }

        public BasicResult.Void SetRoutingInfo(Alias controllerNumber, Alias nodeNumber, byte[] nodeMask)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void SetRoutingInfo(Alias controllerNumber, Node node, byte[] nodeMask)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void SetRoutingMAX(Alias deviceNumber, byte maxRouteTries)
        {
            throw new NotImplementedException();
        }

        public BasicResult.SetLearnMode SetVirtualDeviceLearnMode(Alias bridgeNumber, Node node, VirtualDeviceLearnModes mode)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void SetSleepMode(Alias deviceNumber, SleepModes mode)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void SetSleepMode(Alias deviceNumber, SleepModes mode, byte intEnable)
        {
            BasicResult.Void res = null;
            var deviceNumberUi = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (deviceNumberUi == null)
            {
                res = BasicService.SetSleepMode(deviceNumber, mode, intEnable);
            }
            else
            {
                throw new NotImplementedException();
            }
            return res;
        }

        public BasicResult.SetSucNodeId SetSUCNodeID(Alias controllerNumber, Alias controllerSucNumber)
        {
            BasicResult.SetSucNodeId ret = null;
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            var nid = MemoryGetId(controllerSucNumber).NodeId;
            if (nodeControllerUI != null)
            {

                var networkManagementView = FromMainMenuPage(nodeControllerUI, PCControllerIdMap.NetworkManagementView.ID);
                nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                   .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                   .NavigateToNameProperty(nid.ToString())
                   .Select();
                networkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.SetSisBtn, null, WAIT_ELEMENT_READY_TIMEOUT_MS);
                networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.SetSisBtn).Click();
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);

                ret = new PCControllerActionResult.SetSucNodeId(nodeControllerUI);
                if (ret)
                {
                    PCControllerDeviceManager.SisController = controllerSucNumber;
                }
            }
            return ret;
        }

        public BasicResult.SetSucNodeId SetSUCNodeID(Alias controllerNumber, Alias controllerSucNumber, bool isEnable, byte capabilities, bool isLowPoer)
        {
            return SetSUCNodeID(controllerNumber, controllerSucNumber);
        }

        public ServiceSettings Settings
        {
            get
            {
                return PCControllerDeviceManager.Settings;
            }
            set
            {
                PCControllerDeviceManager.Settings = value;
            }
        }

        public BasicResult.Void SetWutTimeout(Alias deviceNumber, byte timeoutSec)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void SetMaxInclusionRequestInterval(Alias deviceNumber, byte requestintervalSec)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void SoftReset(Alias deviceNumber)
        {

            var deviceHostClient = PCControllerDeviceManager.GetDeviceHostClient(deviceNumber);
            if (!DeviceManager.IsSerialApiController(deviceHostClient.Library))
            {
                DeviceManager.ResetSample(deviceHostClient);
            }
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            nodeControllerUI.Reconnect(deviceNumber.Value);
            return new BasicResult.Void();
        }

        public BasicResult.Void ResetSample(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void StoreHomeId(Alias installerNumber, byte[] homeId, byte nodeId)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void StoreNodeInfo(Alias installerNumber, byte nodeId, byte[] nodeInfo)
        {
            throw new NotImplementedException();
        }

        public BasicResult.TypeLibrary TypeLibrary(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void WatchDogDisable(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void WatchDogEnable(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void WatchDogKick(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void WatchDogStart(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void WatchDogStop(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.ReturnValue SetListenBeforeTalkThreshold(Alias deviceNumber, byte channel, byte threshold)
        {
            throw new NotImplementedException();
        }

        public BasicResult.IncludeNodeWithLearnMode IncludeNodeWithSettings(Alias controllerNumber, Alias nodeNumber)
        {
            throw new NotImplementedException();
        }
        private CommandClass _commandClass;

        // ParseApplicationObject can not define version of parsed clas and that's why we using first one the highest
        private const int VERSION = 0;
        private void SetParamsInTree(IZatsAutomation TreeElement, ParamValue paramValue)
        {
            for (int j = 0; j < paramValue.InnerValues.Count; j++)
            {
                string paramName = paramValue.InnerValues[j].ParamDefinition.Name;
                if (paramValue.InnerValues[j].ParamDefinition.Mode == ParamModes.VariantGroup)
                {
                    var treeNode = TreeElement.NavigateTo(PCControllerIdMap.CommandClassesView.CommandClassesInfoTvw).NavigateTo(paramName);
                    treeNode.Select();
                    treeNode.NavigateTo(string.Empty, "+").Click();

                    var ivs = paramValue.InnerValues[j];
                    foreach (var iv in ivs.InnerValues)
                    {
                        if (iv.InnerValues != null)
                        {
                            SetParamsInTree(treeNode, iv);
                        }
                        else
                        {
                            string TextValue = iv.TextValue;
                            SetParamInTreeNode(treeNode, TextValue, iv.ParamDefinition.Name);
                        }
                    }
                }
                else
                {
                    string TextValue = paramValue.InnerValues[j].TextValue;
                    SetParamInTreeNode(TreeElement, TextValue, paramName);
                }
            }
        }

        private void SetParamInTreeNode(IZatsAutomation TreeElement, string textValue, string paramName)
        {
            if (textValue != string.Empty)
            {
                var treeNode = TreeElement.NavigateTo(paramName);
                var checkBox = treeNode.TryNavigateToClassNameProperty("CheckBox");
                var comboBox = treeNode.TryNavigateToClassNameProperty("ComboBox");
                var textBox = treeNode.TryNavigateToClassNameProperty("TextBox");
                var textBox2 = treeNode.TryNavigateToIdProperty("tbValue");
                var listBox = treeNode.TryNavigateToClassNameProperty("ListBox");
                try
                {
                    if (listBox != null)
                    {
                        bool isFirst = true;
                        foreach (var tv in textValue.Split(','))
                        {
                            var cond = tv.TrimStart();
                            var idx = cond.IndexOf(" (KeyId");
                            if (idx > 0)
                            {
                                cond = cond.Remove(idx);
                            }
                            cond = cond.Replace(' ', '_');
                            var bitmaskItem = listBox.TryNavigateToNameProperty(cond);
                            if (bitmaskItem != null)
                            {
                                if (isFirst)
                                {
                                    bitmaskItem.Select();
                                    isFirst = false;
                                }
                                else
                                {
                                    bitmaskItem.AddToSelection();
                                }
                            }
                        }
                    }
                    if (checkBox != null)
                    {
                        bool state = Convert.ToBoolean(textValue);
                        checkBox.SetCheckBoxStateAt(state);
                    }
                    else if (comboBox != null)
                    {
                        comboBox.SelectComboboxItem(textValue);
                    }
                    else if (textBox != null)
                    {
                        var selectVal = textValue.Replace("command_class", "").Replace("_", " ").Replace("0x", "");
                        if (selectVal.Length % 2 != 0)
                        {
                            selectVal = "0" + selectVal;
                        }
                        //Set value in pror text box
                        if (textBox2 != null)
                        {
                            textBox2.SetElementValue(selectVal);
                        }
                        else
                        {
                            textBox.SetElementValue(selectVal);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.Message._DLOG("SetParamInTreeNode");
                }
            }
        }

        public BasicResult.Void ApplicationUserInput(Alias deviceNumber, byte[] userInputId, byte[] userInputLocal)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void ApplicationGetUserInput(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public void ProgramDeviceWithImageAndKeys(Alias deviceNumber, string Pu, string Pr, string ImagePath)
        {
            throw new NotImplementedException();
        }

        public bool UserActionRequired(string ActionDescription, int timeoutMilliseconds)
        {
            throw new NotImplementedException();
        }

        public bool UserActionRequired(string ActionDescription)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void WriteZW(Alias primary, string strData)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void WriteZW(Alias primary, byte[] data)
        {
            throw new NotImplementedException();
        }

        public BasicResult.ExpectZW RequestZW(Alias primary, string strData, string strFilter, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.ExpectZW RequestZW(Alias primary, byte[] data, string strFilter, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.MaxPayloadSize GetMaxPayloadSize(Alias controllerNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.MaxPayloadSize GetMaxLRPayloadSize(Alias controllerNumber)
        {
            throw new NotImplementedException();
        }

        public void SetLedState(Alias deviceNumber, byte ledMask)
        {
            throw new NotImplementedException();
        }

        public byte[] GetDsk(Alias deviceNumber, int NumBytes)
        {
            throw new NotImplementedException();
        }
        public byte[] GetDskFromEmbeded(Alias deviceNumber, int NumBytes)
        {
            return GetDsk(deviceNumber, NumBytes);
        }

        public BasicResult.Void SetMaxInclusionRequestIntervals(Alias deviceNumber, byte requestintervalSec)
        {
            throw new NotImplementedException();
        }

        public byte[] GetInclusionRequestHomeId(Alias deviceNumber)
        {
            var dsk = GetDsk(deviceNumber, 11);
            var homeId = new byte[4];
            homeId[0] = (byte)(dsk[8] | 0xC0);
            homeId[1] = dsk[9];
            homeId[2] = dsk[10];
            homeId[3] = (byte)(dsk[11] & 0xFE);
            return homeId;
        }

        public BasicResult.ReturnValue SetSecurityInclusionRequestedKeys(Alias deviceNumber, SecuritySchemes[] securitySchemes)
        {
            // Not supported for end devices.
            return null;
        }

        public BasicResult.Void PowerMgmtStayAwake(Alias deviceNumber, byte powerLockType, int powerTimeout)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void PowerMgmtCancel(Alias deviceNumber, byte powerLockType)
        {
            throw new NotImplementedException();
        }

        public BasicResult.ClearNetworkStats ClearNetworkStats(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.GetNetworkStats GetNetworkStats(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.GetTxTimer GetTxTimer(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void ClearTxTimers(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.FirmwareUpdateSendXModem FirmwareUpdateSendXModem(Alias primary, string fromFilePath)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void PowerMgmtStayAwake(Alias deviceNumber, byte powerLockType, int awakeTimeoutMs, int wakeupTimeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Transmit SendDataMultiEx(Alias senderNumber, Alias[] receiverNumbers, byte groupId, byte[] data, SecuritySchemes scheme)
        {
            throw new NotImplementedException();
        }

        public void BackUpNVM(Alias deviceNumber, string backUpPath)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Transmit SendBroadCastNodeInfo(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.ReturnValue SetSecurityInclusionRequestedAuthentication(Alias deviceNumber, SecurityAuthentications securityAuthentication)
        {
            throw new NotImplementedException();
        }

        public BasicResult.AddNodeWithLearnMode ControllerChange(Alias primaryControllerNumber, Alias newPrimaryControllerNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.AddNodeWithLearnMode ControllerChange(Alias primaryControllerNumber, Alias newPrimaryControllerNumber, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.RemoveNode RemoveNodeIdFromNetwork(Alias controllerNumber, Alias nodeNumber, Modes removeMode, LearnModes learnMode, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void DeleteFirstItemFromCommandQueue(Alias deviceNumber)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(deviceNumber);

            uiController.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
            using (var logDlg = uiController.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Commands Queue",
                    () => uiController.Automation.NavigateTo(PCControllerIdMap.MainWindow.CommandQueueBtn).Click(),
                    null)
                    )
            {
                var сommandQueueListView = logDlg.DialogView.NavigateTo(PCControllerIdMap.CommandQueueView.ItemsLv);
                int logRecordsCount = сommandQueueListView.GetGridRowCount();
                if (logRecordsCount > 0)
                {
                    var record = сommandQueueListView.GetValueFromGrid(0, 1);
                    сommandQueueListView.SelectFirstRowWithinGrid(1);

                    logDlg.DialogView.WaitForElementIsReady(PCControllerIdMap.CommandQueueView.DeleteBtn, null);
                    logDlg.DialogView.NavigateTo(PCControllerIdMap.CommandQueueView.DeleteBtn).Click();
                }
                else
                {
                    $"DeleteFirstItemFromCommandQueue - no records found"._DLOG();
                }
            }
            return new PCControllerActionResult.Void(true);
        }

        public BasicResult.Void ClearCommandQueue(Alias deviceNumber)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(deviceNumber);

            uiController.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
            using (var logDlg = uiController.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Commands Queue",
                    () => uiController.Automation.NavigateTo(PCControllerIdMap.MainWindow.CommandQueueBtn).Click(),
                    null)
                    )
            {
                logDlg.DialogView.WaitForElementIsReady(PCControllerIdMap.CommandQueueView.ClearBtn, null);
                logDlg.DialogView.NavigateTo(PCControllerIdMap.CommandQueueView.ClearBtn).Click();
            }
            return new PCControllerActionResult.Void(true);
        }

        public byte[] GetCsa(Alias controllerNumber, int numberOfBytes)
        {
            throw new NotImplementedException();
        }

        public bool Disconnect(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public bool Connect(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void SendDataForced(Alias nodeNumber, ChannelConfigs chConfig, TransmitRfOptions txRfOptions, byte[] homeId, byte srcNodeId, byte headerType, bool speedModified, bool ack, byte seqNo, byte[] destination, byte[] data)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void SendDataForced(Alias nodeNumber, ChannelConfigs chConfig, TransmitRfOptions txRfOptions, byte[] homeId, byte srcNodeId, byte headerType, bool speedModified, bool ack, byte seqNo, byte destination, byte[] data)
        {
            throw new NotImplementedException();
        }
        public BasicResult.ReturnValue SerialApiSetNodeIdBaseType(Alias primary, byte version)
        {
            throw new NotImplementedException();
        }

        public BasicResult.ReturnValue InitiateShutdown(Alias controllerNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void SetRfRegion(Alias dut, RfRegions rfRegion)
        {
            throw new NotImplementedException();
        }

        public BasicResult.GetRfRegion GetRfRegion(Alias dut)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void SetSecurityBypass(Alias deviceNumber, bool isBypass)
        {
            throw new NotImplementedException();
        }

        public BasicResult.DefaultTxPowerLevelGet GetDefaultTxPowerLevel(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }
        public BasicResult.DefaultTxPowerLevelGet GetDefaultTxPowerLevel16Bit(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void SetDefaultTxPowerLevel(Alias deviceNumber, sbyte normalTxPower, sbyte measured0dBmPower)
        {
            throw new NotImplementedException();
        }
        public BasicResult.Void SetDefaultTxPowerLevel16Bit(Alias deviceNumber, short normalTxPower, short measured0dBmPower)
        {
            throw new NotImplementedException();
        }

        public BasicResult.GetLRChannel GetLRChannel(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void SetLRChannel(Alias deviceNumber, LongRangeChannels channel)
        {
            throw new NotImplementedException();
        }

        public BasicResult.GetDcdcMode GetDcdcMode(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void SetDcdcMode(Alias deviceNumber, DcdcModes dcdcMode)
        {
            throw new NotImplementedException();
        }

        public BasicResult.GetBackgroundRssi GetBackgroundRSSI(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.GetRadioPTI GetRadioPTI(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void EnableRadioPTI(Alias deviceNumber, bool isEnabled)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void SetSupervisionDelay(Alias deviceNumber, ZWave.Enums.SupervisionReportStatuses status, int delay)
        {
            throw new NotImplementedException();
        }

        public string GetDskAsString(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void SetSupervisionDelay(Alias deviceNumber, SupervisionReportStatuses status, int delay)
        {
            throw new NotImplementedException();
        }
        public BasicResult.Void SetMaxLrTxPower(Alias controllerNumber, MaxLrTxPower value)
        {
            throw new NotImplementedException();
        }

        public BasicResult.GetMaxLrTxPower GetMaxLrTxPower(Alias controllerNumber)
        {
            throw new NotImplementedException();
        }

        public BasicResult.RequestNodeNeighborUpdate RequestNodeNeighborDiscovery(Alias controllerNumber, Alias nodeNumber, byte nodeType)
        {
            throw new NotImplementedException();
        }

        public bool NVMBackupWriteSession(Alias controller, IList<byte> data, int offset = 0, byte packetSize = 64)
        {
            throw new NotImplementedException();
        }

        public IList<byte> NVMBackupReadSession(Alias controller, int offset = 0, byte packetSize = 64, bool tillEOF = true)
        {
            throw new NotImplementedException();
        }
    }
}
