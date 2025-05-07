using System;
using System.Collections.Generic;
using System.Linq;
using Utils;

namespace Zats.Services
{
    public class PCControllerSecurityTestSettings : ISecurityTestSettings
    {
        public PCControllerDeviceManager PCControllerDeviceManager { get; private set; }

        public IIncludeSettings IncludeSettings
        {
            get
            {
                return PCControllerDeviceManager.IncludeSettings;
            }

            set
            {
                PCControllerDeviceManager.IncludeSettings = value;
            }
        }

        public PCControllerSecurityTestSettings(PCControllerDeviceManager pcControllerDeviceManager)
        {
            PCControllerDeviceManager = pcControllerDeviceManager;
        }

        #region ISecurityTestSettings Members

        public void ClearInclusionFramesS2(Alias deviceNumber)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ContainerTestsSchemeTab).
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.PageS2TestsSchemeTab).SelectTabPage();
                    if (dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2TestsSchemeChk).GetCheckBoxStateAt())
                    {
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameCommandChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameDelayChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameMulticastChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameBroadcastChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameEncryptedChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameNetworkKeyChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameTempKeyChk).SetCheckBoxStateAt(false);
                    }
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFramesClearBtn).ClickIfEnabled();
                }
            }
        }

        public void FreeSpanS2ForNode(Alias deviceNumber, Node node)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.MainMenuBtn).Click();
                nodeControllerUI.Automation.WaitForElementIsVisible(PCControllerIdMap.NetworkManagementView.ID,
                    () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainMenuView.NetworkManagementBtn).Click());
                var networkManagementView = nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.NetworkManagementView.ID);

                networkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.ResetSpanBtn,
                   () =>
                   nodeControllerUI.Automation
                   .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                   .NavigateToNameProperty(node.Id.ToString())
                   .Select(), AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);

                networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.ResetSpanBtn).Click();
            }
        }

        public byte[] GetControllerPublicKey(Alias deviceNumber)
        {
            var uiInstance = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            uiInstance.Automation.NavigateTo(PCControllerIdMap.MainWindow.MainMenuBtn).Click();
            uiInstance.Automation.WaitForElementIsVisible(PCControllerIdMap.NetworkManagementView.ID,
                () => uiInstance.Automation.NavigateTo(PCControllerIdMap.MainMenuView.NetworkManagementBtn).Click());
            var controllerInfoTree = uiInstance.Automation.NavigateTo(PCControllerIdMap.NetworkManagementView.ID).
                NavigateTo(PCControllerIdMap.NetworkManagementView.ControllerInfoTvw);

            var infoNode = controllerInfoTree.TreeViewGetFirstChild().TreeViewGetFirstChild().TreeViewGetNextSibling();
            string puKeyValue = string.Empty;
            while (infoNode != null && !puKeyValue.Contains("Pu"))
            {
                var textBox = infoNode.NavigateToClassNameProperty("TextBox");
                puKeyValue = textBox.GetElementValue();
                infoNode = infoNode.TreeViewGetNextSibling();
            }
            var puKey = Tools.GetBytes(puKeyValue);

            return puKey;
        }

        public byte[] GetNetworkKeyS2(Alias deviceNumber, SecuritySchemes scheme)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                string key = null;
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.CancelBtn).Click())
                        )
                {
                    switch (scheme)
                    {
                        case SecuritySchemes.NONE:
                            break;
                        case SecuritySchemes.S0:
                            if (dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS0Chk).GetCheckBoxStateAt())
                            {
                                key = dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS0Tb).GetElementValue();
                            }
                            else
                            {
                                key = dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.KeyS0Tb).GetElementValue();
                            }
                            break;
                        case SecuritySchemes.S2_ACCESS:
                            if (dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2AccessChk).GetCheckBoxStateAt())
                            {
                                key = dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2AccessTb).GetElementValue();
                            }
                            else
                            {
                                key = dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.KeyAccessTb).GetElementValue();
                            }
                            break;
                        case SecuritySchemes.S2_AUTHENTICATED:
                            if (dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2AuthChk).GetCheckBoxStateAt())
                            {
                                key = dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2AuthTb).GetElementValue();
                            }
                            else
                            {
                                key = dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.KeyAuthTb).GetElementValue();
                            }
                            break;
                        case SecuritySchemes.S2_UNAUTHENTICATED:
                            if (dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2UnauthChk).GetCheckBoxStateAt())
                            {
                                key = dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2UnauthTb).GetElementValue();
                            }
                            else
                            {
                                key = dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.KeyUnauthTb).GetElementValue();
                            }
                            break;
                        case SecuritySchemes.S2_TEMP:
                            //key = dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.KeyLastUsedTempTb).GetElementText();
                            break;
                        default:
                            break;
                    }
                }
                return key?.GetBytes();
            }
            return null;
        }

        public byte[] GetNetworkKeyS2Temp(Alias deviceNumber)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                string key = null;
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.CancelBtn).Click())
                        )
                {
                    key = dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.KeyLastUsedTempTb).GetElementValue();
                }
                return key.GetBytes();
            }
            return null;
        }

        public byte[] GetSpanS2ForNode(Alias deviceNumber, Node node)
        {
            throw new System.NotImplementedException();
        }

        public void NextSpanS2ForNode(Alias deviceNumber, Node node, int nextNonceCount)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.MainMenuBtn).Click();
                nodeControllerUI.Automation.WaitForElementIsVisible(PCControllerIdMap.NetworkManagementView.ID,
                    () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainMenuView.NetworkManagementBtn).Click());
                var networkManagementView = nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.NetworkManagementView.ID);

                nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                    .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                    .NavigateToNameProperty(node.Id.ToString())
                    .Select();
                networkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.NextSpanBtn, null);

                var nextSpanBtn = networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.NextSpanBtn);
                for (int i = 0; i < nextNonceCount; i++)
                {
                    if (nodeControllerUI.Automation.WaitForElementIsVisible(PCControllerIdMap.NetworkManagementView.NextSpanBtn, 1000))
                    {
                        nextSpanBtn.Click();
                    }
                }
            }
        }

        public void SetActiveNetworkKeyS2(Alias deviceNumber, SecuritySchemes scheme)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.MainMenuBtn).Click();
                nodeControllerUI.Automation.WaitForElementIsVisible(PCControllerIdMap.NetworkManagementView.ID,
                    () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainMenuView.NetworkManagementBtn).Click());
                var networkManagementView = nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.NetworkManagementView.ID);

                nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                    .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                    .NavigateToNameProperty(deviceNumber.Value.ToString())
                    .Select();

                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Select Security Scheme",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.NetworkManagementView.SecuritySchemeBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Select Security Scheme").
                        NavigateTo(PCControllerIdMap.UserInputDialog.OkBtn).Click())
                        )
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.UserInputDialog.InCmb).SelectComboboxItem(scheme.ToString());
                }
            }
        }

        public void SetActiveNetworkKeyS2Temp(Alias deviceNumber)
        {
            throw new System.NotImplementedException();
        }

        public void SetActiveSecurityScheme(Alias controllerNumber, Alias deviceNumber, SecuritySchemes scheme)
        {
            byte deviceId = (byte)deviceNumber.Value; // wrong, memory get id is needed
            SetActiveSecurityScheme(controllerNumber, deviceId, scheme);
        }

        public void SetInclusionFrameCommandS2(Alias deviceNumber, SecurityS2TestFrames testFrame, byte[] data)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ContainerTestsSchemeTab).
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.PageS2TestsSchemeTab).SelectTabPage();
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2TestsSchemeChk).SetCheckBoxStateAt(true);

                    string testFrameType = System.Enum.GetName(typeof(SecurityS2TestFrames), testFrame);
                    if (dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameTypeCmb).SelectComboboxItem(testFrameType))
                    {
                        if (data == null)
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameCommandChk).SetCheckBoxStateAt(false);
                        }
                        else
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameCommandChk).SetCheckBoxStateAt(true);
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameCommandValueTb).SetElementValue(Tools.GetHex(data));
                        }
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFramesSetAddBtn).ClickIfEnabled();
                    }
                    else
                    {
                        throw new System.NotImplementedException("Not supported SecurityS2TestFrame");
                    }
                }
            }
        }

        public void SetInclusionFrameDelayS2(Alias deviceNumber, SecurityS2TestFrames testFrame, int delay)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ContainerTestsSchemeTab).
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.PageS2TestsSchemeTab).SelectTabPage();
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2TestsSchemeChk).SetCheckBoxStateAt(true);

                    string testFrameType = System.Enum.GetName(typeof(SecurityS2TestFrames), testFrame);
                    if (dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameTypeCmb).SelectComboboxItem(testFrameType))
                    {
                        delay = delay > 999 ? delay / 1000 : 1;
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameDelayChk).SetCheckBoxStateAt(true);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameDelayValueTb).SetElementValue(delay.ToString());
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFramesSetAddBtn).ClickIfEnabled();
                    }
                    else
                    {
                        throw new System.NotImplementedException("Not supported SecurityS2TestFrame");
                    }
                }
            }
        }

        public void SetInclusionFrameEncryptedS2(Alias deviceNumber, SecurityS2TestFrames testFrame, bool? isEncrypted)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ContainerTestsSchemeTab).
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.PageS2TestsSchemeTab).SelectTabPage();
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2TestsSchemeChk).SetCheckBoxStateAt(true);

                    string testFrameType = System.Enum.GetName(typeof(SecurityS2TestFrames), testFrame);
                    if (dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameTypeCmb).SelectComboboxItem(testFrameType))
                    {
                        if (isEncrypted == null)
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameCommandChk).SetCheckBoxStateAt(false);
                        }
                        else
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameEncryptedChk).SetCheckBoxStateAt(true);
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameEncryptedValueChk).SetCheckBoxStateAt((bool)isEncrypted);
                        }
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFramesSetAddBtn).ClickIfEnabled();
                    }
                    else
                    {
                        throw new System.NotImplementedException("Not supported SecurityS2TestFrame");
                    }
                }
            }
        }

        public void SetInclusionFrameNetworkKeyS2(Alias deviceNumber, SecurityS2TestFrames testFrame, byte[] networkKey)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ContainerTestsSchemeTab).
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.PageS2TestsSchemeTab).SelectTabPage();
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2TestsSchemeChk).SetCheckBoxStateAt(true);

                    string testFrameType = System.Enum.GetName(typeof(SecurityS2TestFrames), testFrame);
                    if (dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameTypeCmb).SelectComboboxItem(testFrameType))
                    {
                        if (networkKey == null)
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameNetworkKeyChk).SetCheckBoxStateAt(false);
                        }
                        else
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameNetworkKeyChk).SetCheckBoxStateAt(true);
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameNetworkKeyValueTb).SetElementValue(Tools.GetHex(networkKey));
                        }
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFramesSetAddBtn).ClickIfEnabled();
                    }
                    else
                    {
                        throw new System.NotImplementedException("Not supported SecurityS2TestFrame");
                    }
                }
            }
        }

        public void SetInclusionFrameNetworkKeyS2Temp(Alias deviceNumber, SecurityS2TestFrames testFrame, byte[] networkKey)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ContainerTestsSchemeTab).
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.PageS2TestsSchemeTab).SelectTabPage();
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2TestsSchemeChk).SetCheckBoxStateAt(true);

                    string testFrameType = System.Enum.GetName(typeof(SecurityS2TestFrames), testFrame);
                    if (dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameTypeCmb).SelectComboboxItem(testFrameType))
                    {
                        if (networkKey == null)
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameTempKeyChk).SetCheckBoxStateAt(true);
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameTempKeyValueChk).SetCheckBoxStateAt(true);
                        }
                        else
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameTempKeyChk).SetCheckBoxStateAt(true);
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameTempKeyValueChk).SetCheckBoxStateAt(true);
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameNetworkKeyChk).SetCheckBoxStateAt(true);
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameNetworkKeyValueTb).SetElementValue(Tools.GetHex(networkKey));
                        }
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFramesSetAddBtn).ClickIfEnabled();
                    }
                    else
                    {
                        throw new System.NotImplementedException("Not supported SecurityS2TestFrame");
                    }
                }
            }
        }

        public void SetInclusionFrameNetworkKeyS2Temp(Alias deviceNumber, SecurityS2TestFrames testFrame)
        {
            SetInclusionFrameNetworkKeyS2Temp(deviceNumber, testFrame, null);
        }

        public void SetNetworkKeyS2(Alias deviceNumber, byte[] networkKey, SecuritySchemes activeNetworkKey)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    switch (activeNetworkKey)
                    {
                        case SecuritySchemes.NONE:
                            break;
                        case SecuritySchemes.S0:
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS0Chk).SetCheckBoxStateAt(true);
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS0Tb).SetElementValue(Tools.GetHex(networkKey));
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS0Chk).SetCheckBoxStateAt(true);
                            break;
                        case SecuritySchemes.S2_ACCESS:
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2AccessChk).SetCheckBoxStateAt(true);
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2AccessTb).SetElementValue(Tools.GetHex(networkKey));
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2AccessChk).SetCheckBoxStateAt(true);
                            break;
                        case SecuritySchemes.S2_AUTHENTICATED:
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2AuthChk).SetCheckBoxStateAt(true);
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2AuthTb).SetElementValue(Tools.GetHex(networkKey));
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2AuthChk).SetCheckBoxStateAt(true);
                            break;
                        case SecuritySchemes.S2_UNAUTHENTICATED:
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2UnauthChk).SetCheckBoxStateAt(true);
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2UnauthTb).SetElementValue(Tools.GetHex(networkKey));
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2UnauthChk).SetCheckBoxStateAt(true);
                            break;
                        case SecuritySchemes.S2_TEMP:
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public void SetNetworkKeyS2Temp(Alias deviceNumber, byte[] networkKey)
        {
            throw new System.NotImplementedException();
        }

        public void SetReceiverEntropyS2(Alias deviceNumber, byte[] receiverEntropy)
        {
            throw new System.NotImplementedException();
        }

        public void SetReceiverEntropyS2(Alias deviceNumber, byte[] receiverEntropy, int numOfTestReceiverEntropyUsage)
        {
            throw new System.NotImplementedException();
        }

        public void SetSecurityVersion(Alias deviceNumber, SecurityVersions securityVersions)
        {
            //throw new System.NotImplementedException();
        }

        public void SetSenderEntropyInputS2(Alias deviceNumber, byte[] senderEntropyInput)
        {
            throw new System.NotImplementedException();
        }

        public void SetSenderEntropyInputS2(Alias deviceNumber, byte[] senderEntropyInput, int numOfSenderEntropyInputUsage)
        {
            throw new System.NotImplementedException();
        }

        public void SetSpanS2(Alias deviceNumber, byte[] span)
        {
            SetSpanS2(deviceNumber, span, 1);
        }

        public void SetSpanS2(Alias deviceNumber, byte[] span, int numOfTestSpanUsage = 1)
        {
            SetS2ParametersOverride(deviceNumber, SecurityS2ParameterTypes.Span, span);
        }

        public void SetActiveSecurityScheme(Alias controllerNumber, Node node, SecuritySchemes scheme)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.MainMenuBtn).Click();
                nodeControllerUI.Automation.WaitForElementIsVisible(PCControllerIdMap.NetworkManagementView.ID,
                    () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainMenuView.NetworkManagementBtn).Click());
                var networkManagementView = nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.NetworkManagementView.ID);

                nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                    .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                    .NavigateToNameProperty(node.Id.ToString())
                    .Select();

                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Select Security Scheme",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.NetworkManagementView.SecuritySchemeBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Select Security Scheme").
                        NavigateTo(PCControllerIdMap.UserInputDialog.OkBtn).Click())
                        )
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.UserInputDialog.InCmb).SelectComboboxItem(scheme.ToString());
                }
            }
        }

        public void SetInclusionFrameMulticastS2(Alias deviceNumber, SecurityS2TestFrames testFrame, bool? isMulticast)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ContainerTestsSchemeTab).
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.PageS2TestsSchemeTab).SelectTabPage();
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2TestsSchemeChk).SetCheckBoxStateAt(true);

                    string testFrameType = System.Enum.GetName(typeof(SecurityS2TestFrames), testFrame);
                    if (dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameTypeCmb).SelectComboboxItem(testFrameType))
                    {
                        if (isMulticast == null)
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameMulticastChk).SetCheckBoxStateAt(false);
                        }
                        else
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameMulticastChk).SetCheckBoxStateAt(true);
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameMulticastValueChk).SetCheckBoxStateAt((bool)isMulticast);
                        }
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFramesSetAddBtn).ClickIfEnabled();
                    }
                    else
                    {
                        throw new System.NotImplementedException("Not supported SecurityS2TestFrame");
                    }
                }
            }
        }

        public void SetInclusionFrameBroadcastS2(Alias deviceNumber, SecurityS2TestFrames testFrame, bool? isBroadcast)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ContainerTestsSchemeTab).
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.PageS2TestsSchemeTab).SelectTabPage();
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2TestsSchemeChk).SetCheckBoxStateAt(true);

                    string testFrameType = System.Enum.GetName(typeof(SecurityS2TestFrames), testFrame);
                    if (dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameTypeCmb).SelectComboboxItem(testFrameType))
                    {
                        if (isBroadcast == null)
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameBroadcastChk).SetCheckBoxStateAt(false);
                        }
                        else
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameBroadcastChk).SetCheckBoxStateAt(true);
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFrameBroadcastValueChk).SetCheckBoxStateAt((bool)isBroadcast);
                        }
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFramesSetAddBtn).ClickIfEnabled();
                    }
                    else
                    {
                        throw new System.NotImplementedException("Not supported SecurityS2TestFrame");
                    }
                }
            }
        }

        public void SetS2MessageEncapsulationExtension(Alias deviceNumber, SecurityS2ExtensionTypes extensionType, SecurityS2MessageTypes messageType,
            bool? isEncrypted, byte[] value, int? numOfUsage, bool? isMoreToFollow, bool? isCritical, byte? extensionLength, bool isOverride)
        {
            SetS2MessageEncapsulationExtension(deviceNumber,
                new TestExtensionS2Settings(messageType, extensionType, SecurityS2TestExtensionsActions.Add)
                {
                    IsEncrypted = isEncrypted,
                    Value = value,
                    NumOfUsage = numOfUsage,
                    IsMoreToFollow = isMoreToFollow,
                    IsCritical = isCritical,
                    ExtensionLength = extensionLength
                },
                isOverride);
        }

        public void SetS2MessageEncapsulationExtension(Alias deviceNumber, TestExtensionS2Settings testExtensionSettings)
        {
            SetS2MessageEncapsulationExtension(deviceNumber, testExtensionSettings, false);
        }

        private void SetS2MessageEncapsulationExtension(Alias deviceNumber, TestExtensionS2Settings testExtensionSettings, bool isOverride)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ContainerTestsSchemeTab).
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.PageS2TestsSchemeTab).SelectTabPage();
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2TestsSchemeChk).SetCheckBoxStateAt(true);

                    string securityS2MessageType = Enum.GetName(typeof(SecurityS2MessageTypes), testExtensionSettings.MessageType);
                    string securityS2ExtensionType = Enum.GetName(typeof(SecurityS2ExtensionTypes), testExtensionSettings.ExtensionType);
                    string extensionS2AppliedActionType = Enum.GetName(typeof(SecurityS2TestExtensionsActions), testExtensionSettings.ExtensionAppliedAction);
                    if (dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionMessageTypeCmb).SelectComboboxItem(securityS2MessageType) &&
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionTypeCmb).SelectComboboxItem(securityS2ExtensionType) &&
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionAppliedActionCmb).SelectComboboxItem(extensionS2AppliedActionType))
                    {
                        if (testExtensionSettings.Value != null)
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionHasValueChk).SetCheckBoxStateAt(true);
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionValueTb).SetElementValue(Tools.GetHex(testExtensionSettings.Value));
                        }
                        else
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionHasValueChk).SetCheckBoxStateAt(false);
                        }
                        if (testExtensionSettings.IsEncrypted == null)
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionEncryptedKeyChk).SetCheckBoxStateAt(false);
                        }
                        else
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionEncryptedKeyChk).SetCheckBoxStateAt(true);
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionEncryptedValueChk).
                                SetCheckBoxStateAt((bool)testExtensionSettings.IsEncrypted);
                        }
                        if (testExtensionSettings.ExtensionLength == null)
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionLenghtKeyChk).SetCheckBoxStateAt(false);
                        }
                        else
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionLenghtKeyChk).SetCheckBoxStateAt(true);
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionLenghtValueTb).
                                SetElementValue(testExtensionSettings.ExtensionLength.ToString());
                        }
                        if (testExtensionSettings.IsMoreToFollow == null)
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionMoreToFollowKeyChk).SetCheckBoxStateAt(false);
                        }
                        else
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionMoreToFollowKeyChk).SetCheckBoxStateAt(true);
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionMoreToFollowValueChk).
                                SetCheckBoxStateAt((bool)testExtensionSettings.IsMoreToFollow);
                        }
                        if (testExtensionSettings.IsCritical == null)
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionCriticalKeyChk).SetCheckBoxStateAt(false);
                        }
                        else
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionCriticalKeyChk).SetCheckBoxStateAt(true);
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionCriticalValueChk).
                                SetCheckBoxStateAt((bool)testExtensionSettings.IsCritical);
                        }
                        if (testExtensionSettings.NumOfUsage == null)
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionNumberOfUsageKeyChk).SetCheckBoxStateAt(false);
                        }
                        else
                        {
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionNumberOfUsageKeyChk).SetCheckBoxStateAt(true);
                            dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionNumberOfUsageValueTb)
                                .SetElementValue(testExtensionSettings.NumOfUsage.ToString());
                        }
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2MessageExtensionsAddBtn).ClickIfEnabled();
                    }
                    else
                    {
                        throw new NotSupportedException("Not supported extension configuration value");
                    }
                }
                nodeControllerUI.Automation.WaitForElementIsReady(PCControllerIdMap.MainWindow.MainMenuBtn, null, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);
            }
        }

        public void ClearTestExtensionsS2(Alias deviceNumber)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ContainerTestsSchemeTab).
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.PageS2TestsSchemeTab).SelectTabPage();

                    if (dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2TestsSchemeChk).GetCheckBoxStateAt())
                    {
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionHasValueChk).SetCheckBoxStateAt(true);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionValueTb).SetElementValue(string.Empty);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionHasValueChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionEncryptedKeyChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionLenghtKeyChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionMoreToFollowKeyChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionCriticalKeyChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ExtensionNumberOfUsageKeyChk).SetCheckBoxStateAt(false);
                    }
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2MessageExtensionsClearBtn).ClickIfEnabled();
                }
            }

        }

        public void SetS2ParametersOverride(Alias deviceNumber, SecurityS2ParameterTypes securityS2ParameterType, byte[] value)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ContainerTestsSchemeTab).
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.PageS2TestsSchemeTab).SelectTabPage();
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2TestsSchemeChk).SetCheckBoxStateAt(true);
                    string paramType = System.Enum.GetName(typeof(SecurityS2ParameterTypes), securityS2ParameterType);
                    if (dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2ParameterTypeCmb).SelectComboboxItem(paramType))
                    {
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2ParameterValueTb).SetElementValue(Tools.GetHex(value));
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2ParameterSetAddBtn).ClickIfEnabled();
                    }
                    else
                    {
                        throw new System.NotImplementedException("Not supported SecurityS2ParameterTypes");
                    }
                }
            }
        }

        public void ClearS2ParametersOverride(Alias deviceNumber)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ContainerTestsSchemeTab).
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.PageS2TestsSchemeTab).SelectTabPage();
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2ParameterClearBtn).ClickIfEnabled();
                }
            }
        }

        public void ClearMPANTable(Alias deviceNumber)
        {
            IZatsAutomationDialog dlgWindow = null;
            NavigateToMpanTableConfigurationViewAndInvoke(deviceNumber, ref dlgWindow, () =>
            {
                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.LoadMPANsBtn).ClickIfEnabled();
                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.ClearBtn).ClickIfEnabled();
            });
        }

        public void RemoveMPANRecord(Alias deviceNumber, byte groupdId, Node owner)
        {
            IZatsAutomationDialog dlgWindow = null;
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            NavigateToMpanTableConfigurationViewAndInvoke(deviceNumber, ref dlgWindow, () =>
            {
                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.LoadMPANsBtn).ClickIfEnabled();

                Pair<int, string>[] columnAndCondition = new Pair<int, string>[2];
                columnAndCondition[0] = new Pair<int, string>(0, groupdId.ToString());
                columnAndCondition[1] = new Pair<int, string>(1, owner.Id.ToString());

                nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.ID).NavigateTo(PCControllerIdMap.MPANTableConfigurationView.MPANsLv)
                    .SelectValuesWithinGrid(columnAndCondition);

                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.RemoveBtn).ClickIfEnabled();
            });
        }

        public void AddOrUpdateMPANState(Alias deviceNumber, byte groupdId, Node owner, bool? isMOS, byte? seqNo, byte[] mpanState, Node[] nodes)
        {
            IZatsAutomationDialog dlgWindow = null;
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            NavigateToMpanTableConfigurationViewAndInvoke(deviceNumber, ref dlgWindow, () =>
            {
                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.LoadMPANsBtn).ClickIfEnabled();
                if (dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.MPANsLv).GetGridRowCount() > 0)
                {
                    Pair<int, string>[] GroupIdAndOwnerId = new Pair<int, string>[2];
                    GroupIdAndOwnerId[0] = new Pair<int, string>(0, groupdId.ToString());
                    GroupIdAndOwnerId[1] = new Pair<int, string>(1, owner.Id.ToString());

                    Pair<int, string>[] GroupId = new Pair<int, string>[1];
                    GroupId[0] = new Pair<int, string>(0, groupdId.ToString());

                    Pair<int, string>[] OwnerId = new Pair<int, string>[1];
                    OwnerId[0] = new Pair<int, string>(1, owner.Id.ToString());

                    bool IsBothInTable = dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.MPANsLv)
                        .IsValuesWithinGrid(GroupIdAndOwnerId);

                    bool IsGroupdIdInTable = dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.MPANsLv)
                        .IsValuesWithinGrid(GroupId);

                    bool IsOwnerIdInTable = dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.MPANsLv)
                        .IsValuesWithinGrid(OwnerId);

                    if (IsBothInTable)
                    {
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.MPANsLv)
                            .SelectValuesWithinGrid(GroupIdAndOwnerId);
                    }
                    else if (IsGroupdIdInTable)
                    {
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.OwnerIdTb).SetElementValue(owner.Id.ToString());
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.GroupIdTb).SetElementValue(groupdId.ToString());
                    }
                    else if (IsOwnerIdInTable)
                    {
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.OwnerIdTb).SetElementValue(owner.Id.ToString());
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.GroupIdTb).SetElementValue(groupdId.ToString());
                    }
                }
                else
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.GroupIdTb).SetElementValue(groupdId.ToString());
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.OwnerIdTb).SetElementValue(owner.Id.ToString());
                }
                if (isMOS != null)
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.MosChk).SetCheckBoxStateAt((bool)isMOS);
                }
                if (seqNo != null)
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.SequenceNumberTb).SetElementValue(seqNo.ToString());
                }
                if (nodes != null)
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.NodeIdsTb).SetElementValue(Tools.GetHex(nodes.Select(x => x.Id).ToArray()));
                }
                mpanState = mpanState ?? (new byte[16]);
                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.MPANStateTb).SetElementValue(Tools.GetHex(mpanState));
                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.AddUpdateBtn).ClickIfEnabled();
            });
        }

        public byte[] GetMPANState(Alias deviceNumber, Node[] nodes)
        {
            byte[] ret = null;
            IZatsAutomationDialog dlgWindow = null;
            NavigateToMpanTableConfigurationViewAndInvoke(deviceNumber, ref dlgWindow, () =>
            {
                if (nodes != null)
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.MPANsLv).
                        SelectRowWithinGrid(Tools.GetHex(nodes.OrderBy(x => x).Select(x => x.Id).ToArray()), 5 /*Node ids column*/);
                    var mpanState = dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.MPANStateTb).GetElementValue();
                    ret = Tools.GetBytes(mpanState);
                }
            });
            return ret;
        }

        public byte[] GetMPANState(Alias deviceNumber, Alias ownerDeviceNumber, byte groupdId)
        {
            byte[] ret = null;
            IZatsAutomationDialog dlgWindow = null;
            NavigateToMpanTableConfigurationViewAndInvoke(deviceNumber, ref dlgWindow, () =>
            {
                Pair<int, string>[] GroupIdAndOwnerId = new Pair<int, string>[2];
                GroupIdAndOwnerId[0] = new Pair<int, string>(0, groupdId.ToString());
                GroupIdAndOwnerId[1] = new Pair<int, string>(1, ownerDeviceNumber.Value.ToString());

                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.MPANsLv).SelectValuesWithinGrid(GroupIdAndOwnerId);
                var ownerIdString = dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.OwnerIdTb).GetElementValue();
                if (int.TryParse(ownerIdString, out int ownerId) &&
                    ownerDeviceNumber.Value == ownerId)
                {
                    var mpanState = dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.MPANStateTb).GetElementValue();
                    ret = Tools.GetBytes(mpanState);
                }
            });
            return ret;
        }

        public void NextMpanForGroupId(Alias deviceNumber, Alias ownerDeviceNumber, byte GroupId, int times)
        {
            IZatsAutomationDialog dlgWindow = null;
            NavigateToMpanTableConfigurationViewAndInvoke(deviceNumber, ref dlgWindow, () =>
            {
                Pair<int, string>[] GroupIdAndOwnerId = new Pair<int, string>[2];
                GroupIdAndOwnerId[0] = new Pair<int, string>(0, GroupId.ToString());
                GroupIdAndOwnerId[1] = new Pair<int, string>(1, ownerDeviceNumber.Value.ToString());
                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.MPANsLv).SelectValuesWithinGrid(GroupIdAndOwnerId);
                for (int i = 0; i < times; i++)
                {
                    dlgWindow.DialogView.WaitForElementIsReady(PCControllerIdMap.MPANTableConfigurationView.NextMPANBtn, null, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.NextMPANBtn).ClickIfEnabled();
                }
            });
        }

        public void NextMpanForGroupId(Alias deviceNumber, Alias ownerDeviceNumber, Node[] nodes, int times)
        {
            IZatsAutomationDialog dlgWindow = null;
            NavigateToMpanTableConfigurationViewAndInvoke(deviceNumber, ref dlgWindow, () =>
            {
                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.MPANsLv).
                    SelectRowWithinGrid(Tools.GetHex(nodes.OrderBy(x => x).Select(x => x.Id).ToArray()), 5 /*Node ids column*/);
                for (int i = 0; i < times; i++)
                {
                    dlgWindow.DialogView.WaitForElementIsReady(PCControllerIdMap.MPANTableConfigurationView.NextMPANBtn, null, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.NextMPANBtn).ClickIfEnabled();
                }
            });
        }

        public byte GetMpanGroupForNodeId(Alias deviceNumber, Node[] nodes)
        {
            byte ret = 0;
            IZatsAutomationDialog dlgWindow = null;
            NavigateToMpanTableConfigurationViewAndInvoke(deviceNumber, ref dlgWindow, () =>
            {
                if (nodes != null)
                {
                    var nids = Tools.GetHex(nodes.OrderBy(x => x).Select(x => x.Id).ToArray());
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.MPANsLv).
                        SelectRowWithinGrid(nids, 5);
                    var groupId = dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.GroupIdTb).GetElementValue();
                    ret = Tools.GetByte(groupId);
                }
            });
            return ret;
        }

        private void NavigateToMpanTableConfigurationViewAndInvoke(Alias deviceNumber, ref IZatsAutomationDialog dlgWindow, Action action)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                nodeControllerUI.Automation.WaitForElementIsReady(PCControllerIdMap.MainWindow.MainMenuBtn, null, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);
                nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.MainMenuBtn).Click();
                nodeControllerUI.Automation.WaitForElementIsVisible(PCControllerIdMap.NetworkManagementView.ID,
                    () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainMenuView.NetworkManagementBtn).Click());
                using (dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Mpan table configurations",
                        () => { nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.NetworkManagementView.MpanTableBtn).Click(); },
                        null))
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.LoadMPANsBtn).ClickIfEnabled();
                    dlgWindow.DialogView.WaitForElementIsReady(PCControllerIdMap.MPANTableConfigurationView.MPANsLv, null, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);
                    action.Invoke();
                }
                nodeControllerUI.Automation.WaitForElementIsReady(PCControllerIdMap.MainWindow.MainMenuBtn, null, AutomationServiceBase.WAIT_ELEMENT_READY_TIMEOUT_MS);
            }
        }

        public void SetSecurityTestReceiverNonceS0(Alias deviceNumber, byte[] nonceData)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ContainerTestsSchemeTab).
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.PageS0TestsSchemeTab).SelectTabPage();

                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS0TestsSchemeChk).SetCheckBoxStateAt(true);
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingControllerRb).SelectRadioButtonItem();
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidNonceSetKeyChk).SetCheckBoxStateAt(true);
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidNonceSetValueTb).SetElementValue(Tools.GetHex(nonceData));

                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingNodeRb).SelectRadioButtonItem();
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidNonceVerifyKeyChk).SetCheckBoxStateAt(true);
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidNonceVerifyValueTb).SetElementValue(Tools.GetHex(nonceData));
                }
            }
        }

        public void SetSecurityTestMACS0(Alias deviceNumber, byte[] code)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ContainerTestsSchemeTab).
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.PageS0TestsSchemeTab).SelectTabPage();

                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS0TestsSchemeChk).SetCheckBoxStateAt(true);
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingControllerRb).SelectRadioButtonItem();
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidMACSetKeyChk).SetCheckBoxStateAt(true);
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidMACSetValueTb).SetElementValue(Tools.GetHex(code));

                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingNodeRb).SelectRadioButtonItem();
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidMACVerifyKeyChk).SetCheckBoxStateAt(true);
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidMACVerifyValueTb).SetElementValue(Tools.GetHex(code));
                }
            }
        }

        public void SetSecurityTestNetworkKeySetS0(Alias deviceNumber, byte[] networkKey)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ContainerTestsSchemeTab).
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.PageS0TestsSchemeTab).SelectTabPage();

                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS0TestsSchemeChk).SetCheckBoxStateAt(true);

                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingControllerRb).SelectRadioButtonItem();
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidNetworkKeySetKeyChk).SetCheckBoxStateAt(true);
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidNetworkKeySetValueTb).SetElementValue(Tools.GetHex(networkKey));
                }
            }
        }

        public void SetSecurityTestNetworkKeyVerifyS0(Alias deviceNumber, byte[] networkKey)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ContainerTestsSchemeTab).
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.PageS0TestsSchemeTab).SelectTabPage();

                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS0TestsSchemeChk).SetCheckBoxStateAt(true);

                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingNodeRb).SelectRadioButtonItem();
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidNetworkKeyVerifyKeyChk).SetCheckBoxStateAt(true);
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidNetworkKeyVerifyValueTb).SetElementValue(Tools.GetHex(networkKey));
                }
            }
        }

        public void SetSecurityTestSchemeS0(Alias deviceNumber, byte scheme)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ContainerTestsSchemeTab).
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.PageS0TestsSchemeTab).SelectTabPage();

                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS0TestsSchemeChk).SetCheckBoxStateAt(true);

                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingControllerRb).SelectRadioButtonItem();
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0SendInvalidSchemeGetKeyChk).SetCheckBoxStateAt(true);
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0SendInvalidSchemeGetValueTb).SetElementValue(scheme.ToString());

                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingNodeRb).SelectRadioButtonItem();
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0SendInvalidSchemeReportKeyChk).SetCheckBoxStateAt(true);
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0SendInvalidSchemeReportValueTb).SetElementValue(scheme.ToString());
                }
            }
        }

        public void SetSecurityTestSchemeInInheritS0(Alias deviceNumber, byte scheme)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ContainerTestsSchemeTab).
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.PageS0TestsSchemeTab).SelectTabPage();

                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS0TestsSchemeChk).SetCheckBoxStateAt(true);
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingControllerRb).SelectRadioButtonItem();
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0SendInvalidSchemeInheritKeyChk).SetCheckBoxStateAt(true);
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0SendInvalidSchemeInheritValueTb).SetElementValue(scheme.ToString());
                }
            }
        }

        public void SetSecurityDelayS0(Alias deviceNumber, SecurityS0Delays delay, int timeoutMs)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ContainerTestsSchemeTab).
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.PageS0TestsSchemeTab).SelectTabPage();
                    if (dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS0TestsSchemeChk).GetCheckBoxStateAt())
                    {
                        if (timeoutMs > 999)
                            timeoutMs = timeoutMs / 1000;
                        switch (delay)
                        {
                            case SecurityS0Delays.NonceGet:
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingControllerRb).SelectRadioButtonItem();
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNonceGetKeyChk).SetCheckBoxStateAt(true);
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNonceGetValueTb).SetElementValue(timeoutMs.ToString());

                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingNodeRb).SelectRadioButtonItem();
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNonceGetInKeyChk).SetCheckBoxStateAt(true);
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNonceGetInValueTb).SetElementValue(timeoutMs.ToString());
                                break;
                            case SecurityS0Delays.NonceReport:
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingControllerRb).SelectRadioButtonItem();
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNonceReportKeyChk).SetCheckBoxStateAt(true);
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNonceReportValueTb).SetElementValue(timeoutMs.ToString());

                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingNodeRb).SelectRadioButtonItem();
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNonceReportInKeyChk).SetCheckBoxStateAt(true);
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNonceReportInValueTb).SetElementValue(timeoutMs.ToString());
                                break;
                            case SecurityS0Delays.Command:
                                //dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingNodeRb).SelectRadioButtonItem();
                                //1.4 Send Network Key Set with delay 20000msec after Security Nonce Report.
                                throw new NotImplementedException("undefined action for enum type SecurityS0Delays.Command");
                            case SecurityS0Delays.SchemeGet:
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingControllerRb).SelectRadioButtonItem();
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelaySchemeGetKeyChk).SetCheckBoxStateAt(true);
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelaySchemeGetValueTb).SetElementValue(timeoutMs.ToString());
                                break;
                            case SecurityS0Delays.SchemeReport:
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingNodeRb).SelectRadioButtonItem();
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelaySchemeReportKeyChk).SetCheckBoxStateAt(true);
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelaySchemeReportValueTb).SetElementValue(timeoutMs.ToString());
                                break;
                            case SecurityS0Delays.NetworkKeySet:
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingControllerRb).SelectRadioButtonItem();
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNetworkKeySetKeyChk).SetCheckBoxStateAt(true);
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNetworkKeySetValueTb).SetElementValue(timeoutMs.ToString());
                                break;
                            case SecurityS0Delays.NetworkKeyVerify:
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingNodeRb).SelectRadioButtonItem();
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNetworkKeyVerifyKeyChk).SetCheckBoxStateAt(true);
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNetworkKeyVerifyValueTb).SetElementValue(timeoutMs.ToString());
                                break;
                            case SecurityS0Delays.SchemeInherit:
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingControllerRb).SelectRadioButtonItem();
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelaySchemeInheritKeyChk).SetCheckBoxStateAt(true);
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelaySchemeInheritValueTb).SetElementValue(timeoutMs.ToString());
                                break;
                            case SecurityS0Delays.SchemeReportEnc:
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingNodeRb).SelectRadioButtonItem();
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayReportAfterSchemeInheritChk).SetCheckBoxStateAt(true);
                                dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayReportAfterSchemeInheritTb).SetElementValue(timeoutMs.ToString());
                                break;
                            default:
                                throw new System.NotImplementedException("undefined enum type SecurityS0Delays");
                        }
                    }
                }
            }
        }

        public void ClearSecurityTestOverridesS0(Alias deviceNumber)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ContainerTestsSchemeTab).
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.PageS0TestsSchemeTab).SelectTabPage();
                    if (dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS0TestsSchemeChk).GetCheckBoxStateAt())
                    {
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingControllerRb).SelectRadioButtonItem();

                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidNonceSetKeyChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidNonceSetValueTb).SetElementValue("");
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidMACSetKeyChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidMACSetValueTb).SetElementValue("");
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidNetworkKeySetKeyChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidNetworkKeySetValueTb).SetElementValue("");
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0SendInvalidSchemeGetKeyChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0SendInvalidSchemeGetValueTb).SetElementValue("");
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0SendInvalidSchemeInheritKeyChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0SendInvalidSchemeInheritValueTb).SetElementValue("");

                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingNodeRb).SelectRadioButtonItem();

                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidNonceVerifyKeyChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidNonceVerifyValueTb).SetElementValue("");
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidMACVerifyKeyChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidMACVerifyValueTb).SetElementValue("");
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidNetworkKeyVerifyKeyChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0InvalidNetworkKeyVerifyValueTb).SetElementValue("");
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0SendInvalidSchemeReportKeyChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0SendInvalidSchemeReportValueTb).SetElementValue("");
                    }
                }
            }
        }

        public void ClearSecurityDelaysS0(Alias deviceNumber)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ContainerTestsSchemeTab).
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.PageS0TestsSchemeTab).SelectTabPage();
                    if (dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS0TestsSchemeChk).GetCheckBoxStateAt())
                    {
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingControllerRb).SelectRadioButtonItem();

                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNetworkKeySetKeyChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNetworkKeySetValueTb).SetElementValue("0");
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNonceGetKeyChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNonceGetValueTb).SetElementValue("0");
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNonceReportKeyChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNonceReportValueTb).SetElementValue("0");
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelaySchemeGetKeyChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelaySchemeGetValueTb).SetElementValue("0");
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelaySchemeInheritKeyChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelaySchemeInheritValueTb).SetElementValue("0");

                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0IncludingNodeRb).SelectRadioButtonItem();

                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelaySchemeReportKeyChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelaySchemeReportValueTb).SetElementValue("0");
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNonceReportInKeyChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNonceReportInValueTb).SetElementValue("0");
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNonceGetInKeyChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNonceGetInValueTb).SetElementValue("0");
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNetworkKeyVerifyKeyChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayNetworkKeyVerifyValueTb).SetElementValue("0");
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayReportAfterSchemeInheritChk).SetCheckBoxStateAt(false);
                        dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S0DelayReportAfterSchemeInheritTb).SetElementValue("0");
                    }
                }
            }
        }

        public void DisableNonceReportS0(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public byte GetTxSequenceNumber(Alias deviceNumber, Node node)
        {
            throw new NotImplementedException();
        }

        public byte GetTxSequenceNumber(Alias deviceNumber, Alias PeerNode)
        {
            throw new NotImplementedException();
        }

        public void NetworkKeyChanged(Alias controllerNumber, Action<byte[], bool> storeNetworkKey)
        {


        }

        public byte UpdateTxSequenceNumber(Alias deviceNumber, Node node)
        {
            throw new NotImplementedException();
        }

        public byte[] GetLastSendDataBuffer(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public byte[] GetSpanContainerCtx(Alias deviceNumber, Node node)
        {
            throw new NotImplementedException();
        }

        public void SetSpanContainerCtx(Alias deviceNumber, Node node, byte[] ctx)
        {
            throw new NotImplementedException();
        }

        public void SetNodeIdsForMpanGroup(Alias deviceNumber, byte groupId, Node[] nodes)
        {

            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.MainMenuBtn).Click();
                nodeControllerUI.Automation.WaitForElementIsVisible(PCControllerIdMap.NetworkManagementView.ID,
                    () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainMenuView.NetworkManagementBtn).Click());

                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Mpan table configurations",
                        () => { nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.NetworkManagementView.MpanTableBtn).Click(); },
                        null))
                {
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.LoadMPANsBtn).ClickIfEnabled();

                    Pair<int, string>[] columnAndCondition = new Pair<int, string>[2];
                    columnAndCondition[0] = new Pair<int, string>(0, groupId.ToString());
                    columnAndCondition[1] = new Pair<int, string>(1, deviceNumber.Value.ToString());
                    nodeControllerUI.Automation.
                        NavigateTo(PCControllerIdMap.MPANTableConfigurationView.ID).
                        NavigateTo(PCControllerIdMap.MPANTableConfigurationView.MPANsLv)
                        .SelectValuesWithinGrid(columnAndCondition);

                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.NodeIdsTb).SetElementValue(Tools.GetHex(nodes.OrderBy(x => x).Select(x => x.Id).ToArray()));
                    var mpanState = dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.MPANStateTb).GetElementValue();
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.MPANStateTb).SetElementValue(""); // - avoid binding issue
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.MPANStateTb).SetElementValue(mpanState);
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.MPANTableConfigurationView.AddUpdateBtn).Click();
                }
            }
        }

        public void SetSpanContainerCtx(Alias deviceNumber, Node node, byte[] ctx, byte txSequenceNumber)
        {
            throw new NotImplementedException();
        }

        public void SetPredefinedNetworkKeys(Alias controllerNumber)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlg = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    var dlgWindow = dlg.DialogView;
                    if (dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS0Chk).GetCheckBoxStateAt())
                    {
                        dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS0Tb).
                            SetElementValue(Tools.GetHex(new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
                        dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS0Chk).SetCheckBoxStateAt(true);
                    }
                    if (dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2UnauthChk).GetCheckBoxStateAt())
                    {
                        dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2UnauthTb).
                            SetElementValue(Tools.GetHex(new byte[] { 0xC0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }));
                        dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2UnauthChk).SetCheckBoxStateAt(true);

                    }
                    if (dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2AuthChk).GetCheckBoxStateAt())
                    {
                        dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2AuthTb).
                            SetElementValue(Tools.GetHex(new byte[] { 0xC1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }));
                        dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2AuthChk).SetCheckBoxStateAt(true);
                    }
                    if (dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2AccessChk).GetCheckBoxStateAt())
                    {
                        dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2AccessTb).
                            SetElementValue(Tools.GetHex(new byte[] { 0xC2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }));
                        dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2AccessChk).SetCheckBoxStateAt(true);
                    }
                }
            }
        }

        public byte[] CalculateTempNetworkKeyS2(Alias deviceNumber, byte[] receiverPublicKey, bool isKeySender)
        {
            throw new NotImplementedException();
        }

        public void SetNetworkKeys(Alias deviceNumber, Dictionary<SecuritySchemes, byte[]> keys)
        {
            throw new NotImplementedException();
        }

        public void SetActiveSecurityScheme(Alias controllerNumber, Alias deviceNumber, SecuritySchemes scheme, bool isLRScheme)
        {
            throw new NotImplementedException();
        }

        public void SetActiveSecurityScheme(Alias controllerNumber, Node node, SecuritySchemes scheme, bool isLRScheme)
        {
            throw new NotImplementedException();
        }

        public byte[] GetNetworkKeyS2(Alias deviceNumber, SecuritySchemes scheme, bool isLRScheme)
        {
            throw new NotImplementedException();
        }

        public void SetNetworkKeyS2(Alias deviceNumber, byte[] networkKey, SecuritySchemes activeNetworkKey, bool isLRScheme)
        {
            throw new NotImplementedException();
        }

        public void SetInitialS2SequenceNumber(Alias zatsController, byte? sequenceNumber)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
