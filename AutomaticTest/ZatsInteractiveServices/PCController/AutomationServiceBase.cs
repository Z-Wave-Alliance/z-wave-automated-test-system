// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Linq;
using Utils;
using Zats.Services.Constants;

namespace Zats.Services
{
    public abstract class AutomationServiceBase : ServiceBase
    {
        public const int WAIT_DLG_TIMEOUT_MS = 60000;
        public const int WAIT_ELEMENT_READY_TIMEOUT_MS = 3000;

        protected PCControllerDeviceManager PCControllerDeviceManager { get; private set; }

        public AutomationServiceBase(PCControllerDeviceManager pccDeviceManager)
        {
            PCControllerDeviceManager = pccDeviceManager;
        }

        private string GetNavigateToPageBtnId(string viewAutomationId)
        {
            if (viewAutomationId == PCControllerIdMap.NetworkManagementView.ID)
            {
                return PCControllerIdMap.MainMenuView.NetworkManagementBtn;
            }
            else if (viewAutomationId == PCControllerIdMap.CommandClassesView.ID)
            {
                return PCControllerIdMap.MainMenuView.CommandClassesBtn;
            }
            else if (viewAutomationId == PCControllerIdMap.EncryptDecryptView.ID)
            {
                return PCControllerIdMap.MainMenuView.EncryptDecryptBtn;
            }
            else if (viewAutomationId == PCControllerIdMap.AssociationsView.ID)
            {
                return PCControllerIdMap.MainMenuView.AssociationsBtn;
            }
            else if (viewAutomationId == PCControllerIdMap.SetupRouteView.ID)
            {
                return PCControllerIdMap.MainMenuView.SetupRouteBtn;
            }
            else if (viewAutomationId == PCControllerIdMap.SmartStartView.ID)
            {
                return PCControllerIdMap.MainMenuView.SamrtStartBtn;
            }
            else
            {
                throw new NotImplementedException("GetNavigateToPageBtnId");
            }
        }

        protected IZatsAutomation FromMainMenuPage(UIInstance uiIstance, string pageAutomationId)
        {
            var primaryMainWindow = uiIstance.Automation.NavigateTo(PCControllerIdMap.MainWindow.ID);
            if (!primaryMainWindow.CheckVisible(pageAutomationId))
            {
                if (primaryMainWindow.CheckVisible(PCControllerIdMap.MainWindow.BusyView))
                {
                    primaryMainWindow.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null, WAIT_ELEMENT_READY_TIMEOUT_MS);
                }
                primaryMainWindow.WaitForElementIsReady(PCControllerIdMap.MainWindow.MainMenuBtn, null, WAIT_ELEMENT_READY_TIMEOUT_MS);
                primaryMainWindow.NavigateTo(PCControllerIdMap.MainWindow.MainMenuBtn).Click();
                var pageButtonId = GetNavigateToPageBtnId(pageAutomationId);
                primaryMainWindow.WaitForElementIsReady(pageButtonId, null, WAIT_ELEMENT_READY_TIMEOUT_MS);
                primaryMainWindow.WaitForElementIsVisible(pageAutomationId, () => primaryMainWindow.NavigateTo(pageButtonId).Click());
            }
            primaryMainWindow.WaitForElementIsReady(pageAutomationId, null, WAIT_ELEMENT_READY_TIMEOUT_MS);
            return primaryMainWindow.NavigateTo(pageAutomationId);
        }

        protected void SetLearnMode(UIInstance uiInstance, LearnModes mode)
        {
            var mainWnd = uiInstance.Automation.NavigateTo(PCControllerIdMap.MainWindow.ID);
            var networkManagementView = FromMainMenuPage(uiInstance, PCControllerIdMap.NetworkManagementView.ID);

            if (mode == LearnModes.LearnModeClassic)
            {
                networkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.ClassicLearnModeBtn, null, WAIT_ELEMENT_READY_TIMEOUT_MS);
                networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.ClassicLearnModeBtn).Click();
            }
            else
            {
                using (var dlgActive = mainWnd.OpenDialog(PCControllerIdMap.DialogWindow.ID, PCControllerIdMap.SelectLearnModeView.CAPTION,
                        () => networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.SelectLearnModeBtn).Click(), null, WAIT_DLG_TIMEOUT_MS * 2)
                        )
                {
                    string btnId = PCControllerIdMap.SelectLearnModeView.ClassicLMBtn;
                    switch (mode)
                    {
                        case LearnModes.LearnModeNWI:
                            btnId = PCControllerIdMap.SelectLearnModeView.NWILMBtn;
                            break;
                        case LearnModes.LearnModeNWE:
                            btnId = PCControllerIdMap.SelectLearnModeView.NWELMBtn;
                            break;
                        case LearnModes.LearnModeSmartStart:
                            btnId = PCControllerIdMap.SelectLearnModeView.SmartStartLMBtn;
                            break;
                        case LearnModes.LearnModeClassic:
                            btnId = PCControllerIdMap.SelectLearnModeView.ClassicLMBtn;
                            break;
                        case LearnModes.LearnModeNetworkMask:
                        case LearnModes.LearnModeDisable:
                        default:
                            throw new NotImplementedException("Unavailable option - Learn Mode");
                    }
                    dlgActive.DialogView.WaitForElementIsReady(btnId, null, WAIT_ELEMENT_READY_TIMEOUT_MS);
                    dlgActive.DialogView.NavigateTo(btnId).Click();
                }
            }
            mainWnd.WaitForElementIsVisible(PCControllerIdMap.MainWindow.BusyView, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);
        }

        /// <summary>
        /// Sete up KEX Set with granted Schemes, None - to disable All
        /// </summary>
        /// <param name="uiInstance">Joining Controller's UiInstance</param>
        /// <param name="grantSchemes">Null or CancelGrantedSchemes - Cancel KEX Set, 0 by dafault = enable all available, else rest listed</param>
        /// <param name="dlgPrepSolution">Dialog window of inclusion the controller</param>
        protected void SetGrantedSchemes(IZatsAutomation dlgPrepSolution)
        {
            if (PCControllerDeviceManager.IncludeSettings.CancelGrantedSchemes)
            {
                dlgPrepSolution.WaitForElementNotVisible(PCControllerIdMap.DialogWindow.ID,
                    () => dlgPrepSolution.NavigateTo(PCControllerIdMap.KexSetConfirmDialog.CancleBtn).ClickIfEnabled(), 2000);
            }
            else
            {
                var grantSchemes = PCControllerDeviceManager.IncludeSettings.GrantedSchemes;
                if (grantSchemes != null && grantSchemes.Length > 0)
                {
                    dlgPrepSolution.NavigateTo(PCControllerIdMap.KexSetConfirmDialog.S2AccessChk).SetCheckBoxStateAt(false);
                    dlgPrepSolution.NavigateTo(PCControllerIdMap.KexSetConfirmDialog.S2AuthenticatedChk).SetCheckBoxStateAt(false);
                    dlgPrepSolution.NavigateTo(PCControllerIdMap.KexSetConfirmDialog.S2UnauthenticatedChk).SetCheckBoxStateAt(false);
                    dlgPrepSolution.NavigateTo(PCControllerIdMap.KexSetConfirmDialog.S0Chk).SetCheckBoxStateAt(false);

                    if (!grantSchemes.Contains(SecuritySchemes.NONE))
                    {

                        if (grantSchemes.Contains(SecuritySchemes.S0))
                        {
                            dlgPrepSolution.NavigateTo(PCControllerIdMap.KexSetConfirmDialog.S0Chk).TrySetCheckBoxStateAt(true);
                        }
                        if (grantSchemes.Contains(SecuritySchemes.S2_ACCESS))
                        {
                            dlgPrepSolution.NavigateTo(PCControllerIdMap.KexSetConfirmDialog.S2AccessChk).TrySetCheckBoxStateAt(true);
                        }
                        if (grantSchemes.Contains(SecuritySchemes.S2_AUTHENTICATED))
                        {
                            dlgPrepSolution.NavigateTo(PCControllerIdMap.KexSetConfirmDialog.S2AuthenticatedChk).TrySetCheckBoxStateAt(true);
                        }
                        if (grantSchemes.Contains(SecuritySchemes.S2_UNAUTHENTICATED))
                        {
                            dlgPrepSolution.NavigateTo(PCControllerIdMap.KexSetConfirmDialog.S2UnauthenticatedChk).TrySetCheckBoxStateAt(true);
                        }
                    }
                }
                //dlgPrepSolution.NavigateTo(PCControllerIdMap.KexSetConfirmDialog.OkBtn).ClickIfEnabled();
                var r = dlgPrepSolution.WaitForElementNotVisible(PCControllerIdMap.DialogWindow.ID,
                    () => dlgPrepSolution.NavigateTo(PCControllerIdMap.KexSetConfirmDialog.OkBtn).ClickIfEnabled(), 2000);
            }
        }

        protected void SetupLearnModeWithDSKExchange(UIInstance includingUI, UIInstance joiningUI, byte[] inputDsk, LearnModes modeLM = LearnModes.LearnModeClassic, int timeout = 60000)
        {
            string joiningDSK = string.Empty;

            SetLearnMode(joiningUI, modeLM);
            timeout = timeout < 30000 ? 30000 : timeout;
            $"SetupLearnModeWithDSKExchange timeout = {timeout}ms"._DLOG();
            if (includingUI != joiningUI)
            {
                var isInProc = includingUI.Automation.CheckVisible(PCControllerIdMap.MainWindow.BusyView) && joiningUI.Automation.CheckVisible(PCControllerIdMap.MainWindow.BusyView);
                bool prepearingReady = includingUI.Automation.WaitForDialogIsVisible(PCControllerIdMap.DialogWindow.ID, "Preparing inclusion", isInProc ? timeout : InclusionS2TimeoutConstants.Joining.KexGet);
                if (prepearingReady)
                {
                    var dlgActiveKEX = includingUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Preparing inclusion");
                    SetGrantedSchemes(dlgActiveKEX);
                    isInProc = includingUI.Automation.CheckVisible(PCControllerIdMap.MainWindow.BusyView) && joiningUI.Automation.CheckVisible(PCControllerIdMap.MainWindow.BusyView);

                    if (joiningUI.Library == LibTypes.Controller || joiningUI.Library == LibTypes.ControllerBridgeLib ||
                        joiningUI.Library == LibTypes.ControllerPortableLib || joiningUI.Library == LibTypes.ControllerStaticLib)
                    {
                        var isJoiningDSKShown = joiningUI.Automation.CheckVisible(/*"Device specific key"*/PCControllerIdMap.DialogWindow.ID) ||
                                                joiningUI.Automation.WaitForDialogIsVisible(PCControllerIdMap.DialogWindow.ID, "Device specific key", isInProc ? timeout : 100);
                        if (isJoiningDSKShown)
                        {
                            var dlgJoiningDSK = joiningUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Device specific key");
                            if (dlgJoiningDSK != null)
                            {
                                joiningDSK = dlgJoiningDSK.NavigateTo(PCControllerIdMap.UserInputDialog.OutTb).GetElementValue();
                                dlgJoiningDSK.NavigateTo(PCControllerIdMap.UserInputDialog.OkBtn).Click();
                            }
                        }

                    }

                    isInProc = includingUI.Automation.CheckVisible(PCControllerIdMap.MainWindow.BusyView) && joiningUI.Automation.CheckVisible(PCControllerIdMap.MainWindow.BusyView);
                    var isIncludingEnterDSKShown = includingUI.Automation.CheckVisible(/*"Enter DSK"*/PCControllerIdMap.DialogWindow.ID) ||
                                                   includingUI.Automation.WaitForDialogIsVisible(PCControllerIdMap.DialogWindow.ID, "Enter DSK", isInProc ? timeout : WAIT_ELEMENT_READY_TIMEOUT_MS);
                    if (isIncludingEnterDSKShown)
                    {
                        var dlgEnterDSK = includingUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Enter DSK");
                        if (dlgEnterDSK != null)
                        {
                            if (PCControllerDeviceManager.IncludeSettings.CancelDSKInputDialog || inputDsk == null)
                            {
                                dlgEnterDSK.NavigateTo(PCControllerIdMap.UserInputDialog.CancelBtn).Click();
                            }
                            else
                            {
                                try
                                {
                                    string dsk = string.Empty;
                                    if (inputDsk.Length < 3)
                                    {
                                        if (inputDsk.Length == 2)
                                        {
                                            dsk = Tools.GetInt32(inputDsk).ToString();
                                        }
                                        else if (PCControllerDeviceManager.IncludeSettings.JoinDSK != null && PCControllerDeviceManager.IncludeSettings.JoinDSK.Length > 0)
                                        {
                                            dsk = ((PCControllerDeviceManager.IncludeSettings.JoinDSK[0] << 8) | PCControllerDeviceManager.IncludeSettings.JoinDSK[1]).ToString();
                                        }
                                        else
                                        {
                                            dsk = joiningDSK;
                                        }
                                    }
                                    else
                                    {
                                        dsk = ((inputDsk[0] << 8) | inputDsk[1]).ToString();
                                    }
                                    dlgEnterDSK.NavigateTo(PCControllerIdMap.UserInputDialog.InTb).SetElementValue(dsk);
                                    dlgEnterDSK.NavigateTo(PCControllerIdMap.UserInputDialog.OkBtn).Click();
                                }
                                catch (InvalidOperationException)
                                {
                                    // In case we do not need to specify DSK key. 
                                    // Or interupted by inclusion test frame
                                }
                            }
                        }
                    }
                }
            }
        }

    }
}
