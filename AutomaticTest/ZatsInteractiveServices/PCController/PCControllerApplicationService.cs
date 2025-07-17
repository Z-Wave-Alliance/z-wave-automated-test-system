// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
// SPDX-FileCopyrightText: 2025 Z-Wave Alliance https://z-wavealliance.org
using System;
using System.Collections.Generic;
using Utils;
using Utils.UI.Enums;
using Zats.Services.PreconfiguredDevices;
using Zats.Services.QuirkDatabase;

namespace Zats.Services
{
    public class PCControllerApplicationService : AutomationServiceBase, IApplicationService
    {
        public Profile CurrentProfile { get; set; }
        public string Frequency { get; set; }
        private ApplicationService BasicApplicationService { get; set; }

        public Parameters Parameters
        {
            get { return PCControllerDeviceManager.Parameters; }
        }
        public IAssociations Associations { get; set; }
        public ISecurityS2 SecurityS2 { get; set; }
        public IFirmwareUpdateMD FirmwareUpdateMD { get; set; }
        private DeviceManager DeviceManager { get; set; }
        public PCControllerApplicationService(PCControllerDeviceManager pccDeviceManager, DeviceManager deviceManager) : base(pccDeviceManager)
        {
            BasicApplicationService = new ApplicationService(deviceManager);
            SecurityTestSettings = new PCControllerSecurityTestSettings(PCControllerDeviceManager);
            TransportServiceTestSettings = new PCControllerTransportServiceTestSettings(pccDeviceManager, deviceManager);
            Provisioning = new PCControllerProvisioningService(pccDeviceManager);
            Associations = new PCControllerAssociations(PCControllerDeviceManager);
            //FirmwareUpdate = new FirmwareUpdate(deviceManager);
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            throw new NotImplementedException();
        }

        public BasicToken.Void AddBasic(Alias deviceNumber)
        {
            if (PCControllerDeviceManager.HasDevice(deviceNumber))
            {
                // ("! - check if cmd in supported list")
                //throw new NotImplementedException();
                return null;
            }
            else
            {
                return BasicApplicationService.AddBasic(deviceNumber);
            }
        }

        public BasicToken.Void AddBasicEx(Alias deviceNumber)
        {
            if (PCControllerDeviceManager.HasDevice(deviceNumber))
            {
                // ("! - check if cmd in supported list")
                //throw new NotImplementedException();
                return null;
            }
            else
            {
                return BasicApplicationService.AddBasicEx(deviceNumber);
            }
        }

        public BasicToken.Void AddBasicEx(Alias deviceNumber, SecuritySchemes scheme)
        {
            throw new NotImplementedException();
        }

        public BasicToken.Void AddBasicSupport(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public void AddManufacturerSpecific(Alias deviceNumber, ushort manufacturerId, ushort productId, ushort productTypeId)
        {
            throw new NotImplementedException();
        }

        public void AddManufacturerSpecific_V2(Alias deviceNumber, ushort manufacturerId, ushort productId, ushort productTypeId, byte deviceIdType, byte deviceIdDataFormat, byte[] deviceId)
        {
            throw new NotImplementedException();
        }

        public BasicToken.Void AddMultiChannelSupport(Alias deviceNumber)
        {
            //("! - check if cmd in supported list")
            //throw new NotImplementedException();
            return null;
        }

        public BasicToken.Void AddResponse(Alias deviceNumber, byte[] cmd, byte[] expectedCmd)
        {
            BasicToken.Void ret = null;
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI == null)
            {
                ret = BasicApplicationService.AddResponse(deviceNumber, cmd, expectedCmd);
            }
            // else ("! - check if cmd in supported list")
            return ret;
        }

        public BasicToken.Void AddResponse(Alias deviceNumber, byte[] cmd, TransmitOptions txOptions, byte[] expectedCmd)
        {
            BasicToken.Void ret = null;
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI == null)
            {
                ret = BasicApplicationService.AddResponse(deviceNumber, cmd, txOptions, expectedCmd);
            }
            // else ("! - check if cmd in supported list")
            return ret;
        }

        public BasicToken.Void AddResponseEx(Alias deviceNumber, byte[] cmd, byte[] expectedCmd)
        {
            BasicToken.Void res = null;
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI == null)
            {
                res = BasicApplicationService.AddResponseEx(deviceNumber, cmd, expectedCmd);
            }
            return res;
            // else ("! - check if cmd in supported list")
        }

        public void AddResponseEx(Alias deviceNumber, byte[] cmd, TransmitOptions txOptions, byte[] expectedCmd)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI == null)
            {
                BasicApplicationService.AddResponseEx(deviceNumber, cmd, txOptions, expectedCmd);
            }
            // else ("! - check if cmd in supported list")
        }

        public void AddResponseEx(Alias deviceNumber, byte[] cmd, byte[] expectedCmd, int NumBytesToCompare)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI == null)
            {
                BasicApplicationService.AddResponseEx(deviceNumber, cmd, expectedCmd, NumBytesToCompare);
            }
        }

        public void AddSecurity(Alias deviceNumber, SecurityVersions securityVersions, params byte[] supportedCC)
        {
            AddOrModifySecurity(deviceNumber, securityVersions, false, supportedCC);
        }

        public void AddSecurity(Alias deviceNumber)
        {
            AddSecurity(deviceNumber, SecurityVersions.S0);
        }

        public void AddSecurity(Alias deviceNumber, SecurityVersions securityVersions)
        {
            AddSecurity(deviceNumber, securityVersions, null);
        }

        public void AddSecurity(Alias deviceNumber, params byte[] supportedCC)
        {
            throw new NotImplementedException();
        }

        public void AddSecurity(Alias deviceNumber, bool checkIfSupportSecurityClass, params byte[] supportedCC)
        {
            throw new NotImplementedException();
        }

        public void AddOrModifySecurity(Alias deviceNumber, SecurityVersions securityVersions, bool checkIfSupportSecurityClass, params byte[] supportedCC)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null &&
                (nodeControllerUI.Library == LibTypes.Controller || nodeControllerUI.Library == LibTypes.ControllerBridgeLib ||
                nodeControllerUI.Library == LibTypes.ControllerPortableLib || nodeControllerUI.Library == LibTypes.ControllerStaticLib))
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null, WAIT_ELEMENT_READY_TIMEOUT_MS);
                using (var dlg = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click(), WAIT_ELEMENT_READY_TIMEOUT_MS * 2)
                        )
                {
                    var dlgWindow = dlg.DialogView;
                    dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS0Chk).SetCheckBoxStateAt(false);
                    dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2UnauthChk).SetCheckBoxStateAt(false);
                    dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2AuthChk).SetCheckBoxStateAt(false);
                    dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2AccessChk).SetCheckBoxStateAt(false);

                    if (securityVersions != SecurityVersions.None)
                    {
                        if (securityVersions.HasFlag(SecurityVersions.S0))
                        {
                            dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS0Chk).SetCheckBoxStateAt(true);
                            dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS0Chk).SetCheckBoxStateAt(true);
                            dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS0Tb).SetElementValue(Tools.GetHex(new byte[16]));
                            dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS0Chk).SetCheckBoxStateAt(false);

                            dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ContainerTestsSchemeTab).
                                NavigateTo(PCControllerIdMap.SecuritySettingsDialog.PageS0TestsSchemeTab).SelectTabPage();
                            dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS0TestsSchemeChk).SetCheckBoxStateAt(false);
                        }
                        if (securityVersions.HasFlag(SecurityVersions.S2))
                        {
                            dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2UnauthChk).SetCheckBoxStateAt(true);
                            dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2AuthChk).SetCheckBoxStateAt(true);
                            dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2AccessChk).SetCheckBoxStateAt(true);
                            dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.JoinCSAChk).SetCheckBoxStateAt(false);
                            //unauth
                            dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2UnauthChk).SetCheckBoxStateAt(true);
                            dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2UnauthTb).SetElementValue(Tools.GetHex(new byte[16]));
                            dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2UnauthChk).SetCheckBoxStateAt(false);
                            //auth
                            dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2AuthChk).SetCheckBoxStateAt(true);
                            dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2AuthTb).SetElementValue(Tools.GetHex(new byte[16]));
                            dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2AuthChk).SetCheckBoxStateAt(false);
                            //access
                            dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2AccessChk).SetCheckBoxStateAt(true);
                            dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2AccessTb).SetElementValue(Tools.GetHex(new byte[16]));
                            dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS2AccessChk).SetCheckBoxStateAt(false);
                        }
                        dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ContainerTestsSchemeTab).
                           NavigateTo(PCControllerIdMap.SecuritySettingsDialog.PageS2TestsSchemeTab).SelectTabPage();
                        dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2TestsSchemeChk).SetCheckBoxStateAt(true);
                        dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2ParameterClearBtn).ClickIfEnabled();
                        dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2InclusionFramesClearBtn).ClickIfEnabled();
                        dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.S2MessageExtensionsClearBtn).ClickIfEnabled();
                        dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2TestsSchemeChk).SetCheckBoxStateAt(false);
                    }
                    if (string.IsNullOrEmpty(PCControllerDeviceManager.CurrentProfile.Reports))
                    {
                        dlg.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ChkSaveKeys).SetCheckBoxStateAt(false);
                        dlg.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.TbSavedKeysStoragePath).SetElementValue("");

                    }
                    else
                    {
                        dlg.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ChkSaveKeys).SetCheckBoxStateAt(true);
                        var path = System.IO.Path.Combine(PCControllerDeviceManager.CurrentProfile.Reports, "_SecurityKeys");
                        dlg.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.TbSavedKeysStoragePath).SetElementValue(path);
                    }

                }
            }
            else
            {
                //BasicApplicationService.AddSecurity(deviceNumber, securityVersions);
            }
        }

        public void AddCrc16(Alias deviceNumber)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI == null)
            {
                BasicApplicationService.AddCrc16(deviceNumber);
            }
        }

        public ApplicationToken.Supervision AddSupervision(Alias deviceNumber)
        {
            ApplicationToken.Supervision ret = new ApplicationToken.Supervision(null);
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI == null)
            {
                BasicApplicationService.AddSupervision(deviceNumber);
            }
            return ret;
        }

        public void AddMultiChannel(Alias deviceNumber)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI == null)
            {
                BasicApplicationService.AddMultiChannel(deviceNumber);
            }
        }

        public void AddTransportService(Alias deviceNumber, int maxFrameSize)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (uiController != null)
            {
                uiController.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlg = uiController.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, PCControllerIdMap.SendDataSettingsDialog.Title,
                        () => uiController.Automation.NavigateTo(PCControllerIdMap.MainWindow.SendDataSettingsBtn).Click(),
                        () => uiController.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, PCControllerIdMap.SendDataSettingsDialog.Title).
                        NavigateTo(PCControllerIdMap.SendDataSettingsDialog.OkBtn).Click())
                        )
                {
                    dlg.DialogView.NavigateTo(PCControllerIdMap.SendDataSettingsDialog.TransportServiceMaxSegmentSizeTb).SetElementValue(maxFrameSize.ToString());
                }
            }
        }

        public int AddTransportService(Alias deviceNumber)
        {
            int maxPayloadSize = 46;
            AddTransportService(deviceNumber, maxPayloadSize);
            return maxPayloadSize;
        }

        public void ClearSecurityDelays(Alias deviceNumber)
        {
            SecurityTestSettings.ClearSecurityDelaysS0(deviceNumber);
        }

        public void ClearSecurityTestOverrides(Alias deviceNumber)
        {
            SecurityTestSettings.ClearSecurityTestOverridesS0(deviceNumber);
        }

        public byte[] DecryptS0(byte[] receiverNonce, byte[] senderNonce, byte[] securityKey, byte[] message)
        {
            throw new NotImplementedException();
        }

        public byte[] EncryptS0(byte[] receiverNonce, byte[] senderNonce, byte[] securityKey, byte[] message)
        {
            throw new NotImplementedException();
        }

        public COMMAND_CLASS_SECURITY.SECURITY_MESSAGE_ENCAPSULATION EncryptS0MessageEncapsulation(byte property, byte[] command, Node senderNodeId, Node receiverNodeId, byte[] senderNonce, byte[] recevierNonce, byte[] networkKey)
        {
            throw new NotImplementedException();
        }

        public COMMAND_CLASS_SECURITY.SECURITY_MESSAGE_ENCAPSULATION_NONCE_GET EncryptS0MessageEncapsulationNonceGet(byte property, byte[] command, Node senderNodeId, Node receiverNodeId, byte[] senderNonce, byte[] recevierNonce, byte[] networkKey)
        {
            throw new NotImplementedException();
        }

        public byte[] GetSecurityNetworkKey(Alias deviceNumber)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                string key = null;
                using (var dlgWindow = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.CancelBtn).Click()))
                {
                    if (dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS0Chk).GetCheckBoxStateAt())
                    {
                        key = dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS0Tb).GetElementValue();
                    }
                    else
                    {
                        key = dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.KeyS0Tb).GetElementValue();
                    }
                }
                return key?.GetBytes();
            }
            return null;
        }

        public BasicResult.Test GetTrafficTestResult(Alias deviceNumber, Node node)
        {
            throw new NotImplementedException();
        }

        public void RemoveSecurity(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public void ResetSecurity(Alias deviceNumber)
        {
            // do nothing?
        }

        public void ResumeSecurity(Alias deviceNumber)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlg = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    var isPauseSecurityTgl = dlg.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableSecurityTgl);
                    isPauseSecurityTgl.SetCheckBoxStateAt(true);
                    isPauseSecurityTgl.SetCheckBoxStateAt(false);
                    bool validateState = isPauseSecurityTgl.GetCheckBoxStateAt() != false;
                    if (validateState)
                    {
                        throw new InvalidOperationException("Wrong state of ID_BTN_ENABLE_SECURITY");
                    }
                }
            }
        }

        public void ResumeTransportService(Alias deviceNumber, int maxFragmentSize)
        {
            throw new NotImplementedException();
        }

        public ISecurityTestSettings SecurityTestSettings { get; set; }

        public IProvisioningService Provisioning { get; private set; }

        public void SetSecurityDelay(Alias deviceNumber, SecurityS0Delays delay, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public void SetSecurityForNode(Alias deviceNumber, Alias nodeNumber, bool isEnable)
        {
            throw new NotImplementedException();
        }

        public void SetSecurityTestFragmentedPartDisabled(Alias deviceNumber, bool isFirstPartDisabled, bool isSecondPartDisabled)
        {
            throw new NotImplementedException();
        }

        public void SetSecurityTestMAC(Alias deviceNumber, byte[] code)
        {
            throw new NotImplementedException();
        }

        public void SetSecurityTestNetworkKey(Alias deviceNumber, byte[] networkKey)
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
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS0Chk).SetCheckBoxStateAt(true);
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS0Tb).SetElementValue(Tools.GetHex(networkKey));
                    dlgWindow.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OverrideS0Chk).SetCheckBoxStateAt(true);
                }
            }
        }

        public void SetSecurityTestPropertyByte(Alias deviceNumber, byte value)
        {
            throw new NotImplementedException();
        }

        public void SetSecurityTestPropertyByteSecondFrame(Alias deviceNumber, byte value)
        {
            throw new NotImplementedException();
        }

        public void SetSecurityTestReceiverNonce(Alias deviceNumber, byte[] nonceData)
        {
            throw new NotImplementedException();
        }

        public void SetSecurityTestReceiverNonceId(Alias deviceNumber, byte? nonceId)
        {
            throw new NotImplementedException();
        }

        public void SetSecurityTestScheme(Alias deviceNumber, byte scheme)
        {
            throw new NotImplementedException();
        }

        public void SetSecurityTestSchemeInGet(Alias deviceNumber, byte scheme)
        {
            throw new NotImplementedException();
        }

        public void SetSecurityTestSchemeInInherit(Alias deviceNumber, byte scheme)
        {
            throw new NotImplementedException();
        }

        public void SetSecurityTestSchemeInReport(Alias deviceNumber, byte scheme)
        {
            throw new NotImplementedException();
        }

        public void SetSecurityTestSchemeInReportEncrypted(Alias deviceNumber, byte scheme)
        {
            throw new NotImplementedException();
        }

        public void SetSecurityTestSenderNonce(Alias deviceNumber, byte[] nonceData)
        {
            throw new NotImplementedException();
        }

        public void SetSecurityVersionForNode(Alias deviceNumber, Alias nodeNumber, SecurityVersions securityVersion)
        {
            throw new NotImplementedException();
        }

        public void StartTrafficFromEndDeviceToNodeIdTest(Alias deviceNumber, Node node, byte[] cmd, byte[] expectedCmd, int intervalMs, int timeoutMs, SecuritySchemes securityScheme)
        {
            throw new NotImplementedException();
        }

        public void StartTrafficToNodeIdTest(Alias deviceNumber, Node node, byte[] cmd, byte[] expectedCmd, int intervalMs, int timeoutMs, bool isSetSpanFree)
        {
            throw new NotImplementedException();
        }

        public void StartTrafficToNodeIdTest(Alias deviceNumber, Node node, byte[] cmd, byte[] expectedCmd, int intervalMs, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public void StartTrafficToNodeIdTest(Alias deviceNumber, Node node, byte[] cmd, int intervalMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Test StopTrafficTest(Alias deviceNumber, Node node)
        {
            throw new NotImplementedException();
        }

        public void SuspendSecurity(Alias deviceNumber)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlg = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    var isPauseSecurityTgl = dlg.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableSecurityTgl);
                    isPauseSecurityTgl.SetCheckBoxStateAt(false);
                    isPauseSecurityTgl.SetCheckBoxStateAt(true);
                    bool validateState = isPauseSecurityTgl.GetCheckBoxStateAt() != true;
                    if (validateState)
                    {
                        throw new InvalidOperationException("Wrong state of ID_BTN_ENABLE_SECURITY");
                    }
                }
            }
        }

        public void SuspendTransportService(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public ITransportServiceTestSettings TransportServiceTestSettings { get; set; }

        public IZWaveClassesConverter ZWaveClasses => throw new NotImplementedException();

        public void EnableNetworkKey(Alias deviceNumber, SecuritySchemes securityScheme, bool isEnable)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
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
                    switch (securityScheme)
                    {
                        case SecuritySchemes.S0:
                            dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS0Chk).SetCheckBoxStateAt(isEnable);
                            break;
                        case SecuritySchemes.S2_ACCESS:
                            dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2AccessChk).SetCheckBoxStateAt(isEnable);
                            break;
                        case SecuritySchemes.S2_AUTHENTICATED:
                            dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2AuthChk).SetCheckBoxStateAt(isEnable);
                            break;
                        case SecuritySchemes.S2_UNAUTHENTICATED:
                            dlgWindow.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.EnableS2UnauthChk).SetCheckBoxStateAt(isEnable);
                            break;
                        case SecuritySchemes.NONE:
                        case SecuritySchemes.S2_TEMP:
                        default:
                            break;
                    }
                }
            }
        }

        public void EnableCSA(Alias deviceNumber, bool isEnable)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlg = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlg.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.JoinCSAChk).SetCheckBoxStateAt(isEnable);
                }
            }
        }

        public void SaveNetworkKeysToFile(Alias deviceNumber, string networkKeysFilePath)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlg = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlg.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.ChkSaveKeys).SetCheckBoxStateAt(true);
                    dlg.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.TbSavedKeysStoragePath).SetElementValue(networkKeysFilePath);
                }
            }
        }

        public void SaveSecuritySettingsToFile(Alias deviceNumber, string securitySettingsFilePath)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlg = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlg.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.BtnSaveSettings).Click();
                    nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null, WAIT_ELEMENT_READY_TIMEOUT_MS);

                    var saveAsDialog = nodeControllerUI.Automation.NavigateToNameProperty(PCControllerIdMap.FileDialog.SaveTitle);

                    saveAsDialog.NavigateToLocalizedControlTypeAndNameProperties("combo box", PCControllerIdMap.FileDialog.FileNameTb)
                        .SetElementValue(securitySettingsFilePath);
                    saveAsDialog.NavigateToLocalizedControlTypeAndNameProperties("Button", PCControllerIdMap.FileDialog.FileNameTb)
                        .SetElementValue(securitySettingsFilePath);
                    saveAsDialog.NavigateToLocalizedControlTypeAndNameProperties("Button", PCControllerIdMap.FileDialog.SaveBtn)
                        .Click();
                }
            }
        }

        public void OpenSecuritySettingsFromFile(Alias deviceNumber, string securitySettingsFilePath)
        {
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlg = nodeControllerUI.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, "Security Settings",
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.SecuritySettingsBtn).Click(),
                        () => nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, "Security Settings").
                        NavigateTo(PCControllerIdMap.SecuritySettingsDialog.OkBtn).Click())
                        )
                {
                    dlg.DialogView.NavigateTo(PCControllerIdMap.SecuritySettingsDialog.BtnLoadSettings).Click();
                    nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null, WAIT_ELEMENT_READY_TIMEOUT_MS);

                    var saveAsDialog = nodeControllerUI.Automation.NavigateToNameProperty(PCControllerIdMap.FileDialog.OpenTitle);
                    saveAsDialog.NavigateToLocalizedControlTypeAndNameProperties("combo box", PCControllerIdMap.FileDialog.FileNameTb)
                        .SetElementValue(securitySettingsFilePath);
                    saveAsDialog.NavigateToLocalizedControlTypeAndNameProperties("Button", PCControllerIdMap.FileDialog.OpenBtn)
                        .Click();
                }
            }
        }


        public byte[] DecryptS2(Alias deviceNumber, byte senderId, byte receiverId, byte[] homeId, byte sequenceNumber, byte[] receiverNonce, byte[] senderNonce, byte[] securityKey, int generationCount, bool isRealKey, byte[] fullMessageS2, out int currentGenerationCount)
        {
            currentGenerationCount = 0;

            var uiController = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            var encryptDecryptView = FromMainMenuPage(uiController, PCControllerIdMap.EncryptDecryptView.ID);

            if (isRealKey)
            {
                encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.NormalChk).TrySetCheckBoxStateAt(true);
            }
            else
            {
                encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.NormalChk).TrySetCheckBoxStateAt(false);
            }

            encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.HomeIdTb).SetElementValue(homeId.ToHexString());
            encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.SenderIdTb).SetElementValue(Convert.ToString(senderId));
            encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.ReceiverIdTb).SetElementValue(Convert.ToString(receiverId));
            encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.SequenceNumberTb).SetElementValue(Convert.ToString(sequenceNumber));

            encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.ReceiverNonceTb).SetElementValue(receiverNonce.ToHexString());
            encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.SenderNonceTb).SetElementValue(senderNonce.ToHexString());
            encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.SecurityKeyTb).SetElementValue(securityKey.ToHexString());

            encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.EncryptTb).SetElementValue(string.Empty);
            encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.DecryptTb).SetElementValue(string.Empty);
            encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.EncryptTb).SetElementValue(fullMessageS2.ToHexString());

            encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.DecryptBtn).Click();
            encryptDecryptView.WaitForElementIsReady(PCControllerIdMap.EncryptDecryptView.DecryptBtn, null);
            byte[] res = encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.DecryptTb).GetElementValue().ToByteArray();

            currentGenerationCount += 1;
            return res;
        }

        public byte[] EncryptS2(Alias deviceNumber, byte senderId, byte receiverId, byte[] homeId, byte sequenceNumber, byte[] receiverNonce, byte[] senderNonce, byte[] securityKey, byte[] textToEncrypt, int generationCount, bool isRealKey)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            var encryptDecryptView = FromMainMenuPage(uiController, PCControllerIdMap.EncryptDecryptView.ID);

            if (isRealKey)
            {
                encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.NormalChk).TrySetCheckBoxStateAt(true);
            }
            else
            {
                encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.NormalChk).TrySetCheckBoxStateAt(false);
            }

            encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.HomeIdTb).SetElementValue(homeId.ToHexString());
            encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.SenderIdTb).SetElementValue(Convert.ToString(senderId));
            encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.ReceiverIdTb).SetElementValue(Convert.ToString(receiverId));
            encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.SequenceNumberTb).SetElementValue(Convert.ToString(sequenceNumber));

            encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.ReceiverNonceTb).SetElementValue(receiverNonce.ToHexString());
            encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.SenderNonceTb).SetElementValue(senderNonce.ToHexString());
            encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.SecurityKeyTb).SetElementValue(securityKey.ToHexString());

            encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.DecryptTb).SetElementValue(string.Empty);
            encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.EncryptTb).SetElementValue(string.Empty);
            encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.DecryptTb).SetElementValue(textToEncrypt.ToHexString());


            encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.EncryptBtn).Click();
            encryptDecryptView.WaitForElementIsReady(PCControllerIdMap.EncryptDecryptView.EncryptBtn, null);

            byte[] res = encryptDecryptView.NavigateTo(PCControllerIdMap.EncryptDecryptView.EncryptTb).GetElementValue().ToByteArray();
            return res;
        }

        public void SetSecurityTestNetworkKeyTemp(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public void SetApplicationCmd(Alias deviceNumber, bool IsS0, bool IsS2s, LibTypes Library)
        {
            throw new NotImplementedException();
        }

        public void SetApplicationResponse(Alias deviceNumber, LibTypes Library, byte[] FullCmd)
        {
            throw new NotImplementedException();
        }

        public void EnableNetworkKey(Alias secondary, IEnumerable<SecuritySchemes> requestSchemes)
        {
            throw new NotImplementedException();
        }

        public void SetApplicationCmdNoResponse(Alias deviceNumber, bool IsS0, bool IsS2s, LibTypes Library)
        {
            throw new NotImplementedException();
        }

        public BootloaderPath GetOTABootloaderPath(Alias deviceNumber, DevKits devKit, params string[] regexFilter)
        {
            throw new NotImplementedException();
        }

        public BootloaderPath GetOTWBootloaderPath(Alias deviceNumber, DevKits devKit, params string[] regexFilter)
        {
            throw new NotImplementedException();
        }

        public List<BasicToken.ResponseData> AddSupportCc(Alias deviceNumber, byte CcId)
        {
            throw new NotImplementedException();
        }

        public List<BasicToken.ResponseData> SetupRootDevice(Alias deviceNumber, DeviceOptions deviceOptions, byte genericDeviceClass, byte specificDeviceClass,
            bool multiChannel, bool supervision, bool crc16, SecurityVersions securityVersions, bool transportService, IEnumerable<CommandClassSupport> commandClassSupportList, Configuration.ConfigurationItem configuration = null)
        {
            throw new NotImplementedException();
        }

        public List<BasicToken.ResponseData> SetupEndPoint(Alias deviceNumber, byte endPointId, byte genericDeviceClass, byte specificDeviceClass,
            bool supervision, SecurityVersions securityVersions, IEnumerable<CommandClassSupport> commandClassSupportList, AssociationSupportData rootAssociationSupportData = null)
        {
            throw new NotImplementedException();
        }

        public TasksAccessPoint SetupControllerDeviceConfigured(Alias deviceNumber, bool multiChannel, bool supervision, bool crc16, SecurityVersions securityVersions, bool transportService, bool overwriteConfig = false)
        {
            throw new NotImplementedException();
        }

        public TasksAccessPoint SetupControllerDeviceConfigured(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public TasksAccessPoint SetupControllerDeviceConfiguredAndWaitForDut(Alias deviceNumber, Node dutNodeId, RoleTypes roleType)
        {
            throw new NotImplementedException();
        }

        public TasksAccessPoint SetupControllerDeviceConfiguredAndWaitForDut(Alias deviceNumber, Node dutNodeId, RoleTypes roleType, byte? initS2SequenceNumber)
        {
            throw new NotImplementedException();
        }

        public void EnableCommandClassSupportForCallbacks(Alias deviceNumber, byte endPointId, CommandClassSupport commandClassSupport)
        {
            throw new NotImplementedException();
        }

        public void AddCommandCallbacks(byte commandClass, Dictionary<byte, CommandCallback> callbacks)
        {
            throw new NotImplementedException();
        }

        public void RemoveCommandCallbacks(byte commandClass)
        {
            throw new NotImplementedException();
        }

        public byte[] DecryptS0MessageEncapsulation(COMMAND_CLASS_SECURITY.SECURITY_MESSAGE_ENCAPSULATION frame, byte[] nonceBytes, byte[] networkKey)
        {
            throw new NotImplementedException();
        }

        public void AddSecurity(Alias deviceNumber, SecurityVersions securityVersions, LibTypes CommandClasses)
        {
            throw new NotImplementedException();
        }

        public void SetApplicationCmd(Alias deviceNumber, bool IsS0, bool IsS2s, LibTypes Library, byte[] wakeUpInterval)
        {
            throw new NotImplementedException();
        }

        public void SetApplicationResponse(Alias deviceNumber, LibTypes Library, byte[] FullCmd, byte[] wakeUpInterval)
        {
            throw new NotImplementedException();
        }

        public void SetProfileSecurityKeys(string S2Access, string S2Auth, string S2UnAuth)
        {
            throw new NotImplementedException();
        }

        public Profile GetProfile()
        {
            throw new NotImplementedException();
        }

        public void SetRoleType(Alias deviceNumber, RoleTypes roleType)
        {
            throw new NotImplementedException();
        }

        public byte[] GetProfileDsk()
        {
            throw new NotImplementedException();
        }

        public byte[] GetProfileNetworkKey(SecuritySchemes securityScheme)
        {
            throw new NotImplementedException();
        }

        public void SetProfileSecurityKeys(string S2Access, string S2Auth, string S2UnAuth, string S0Key)
        {
            throw new NotImplementedException();
        }

        public Dictionary<SecuritySchemes, byte[]> GetProfileNetworkKeys()
        {
            throw new NotImplementedException();
        }

        public void SetProfileSecurityKeys(string S2Access, string S2Auth, string S2UnAuth, string S0Key, string tempNetKey)
        {
            throw new NotImplementedException();
        }

        public void ModifyHomeId(Alias deviceNumber, byte[] homeId)
        {
            throw new NotImplementedException();
        }

        public byte[] GetSupportedCommandClasses(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public string GetFile(LibTypes library, ChipSeries chipSeries, DevKits devKit, params string[] regexFilters)
        {
            throw new NotImplementedException();
        }

        public string GetFile(LibTypes library, ChipSeries chipSeries, string frequency, DevKits devKit, params string[] regexFilters)
        {
            throw new NotImplementedException();
        }

        public string GetFile(Alias deviceNumber, DevKits devKit, params string[] regexFilters)
        {
            throw new NotImplementedException();
        }

        public string GetFile(Alias deviceNumber, string ending, DevKits devKit, params string[] regexFilters)
        {
            throw new NotImplementedException();
        }

        BasicToken.Void[] IApplicationService.SetApplicationCmd(Alias deviceNumber, bool IsS0, bool IsS2s, LibTypes Library)
        {
            if (PCControllerDeviceManager.HasDevice(deviceNumber))
            {
                BasicToken.Void[] voids = new BasicToken.Void[] { };
                return voids;
            }
            else
            {
                return BasicApplicationService.SetApplicationCmd(deviceNumber, IsS0, IsS2s, Library);
            }
        }

        BasicToken.Void[] IApplicationService.SetApplicationCmd(Alias deviceNumber, bool IsS0, bool IsS2, LibTypes Library, byte[] wakeUpInterval)
        {
            throw new NotImplementedException();
        }

        BasicToken.Void[] IApplicationService.SetApplicationCmd(Alias deviceNumber, bool IsS0, bool IsS2, LibTypes Library, byte zwavePlusVersion)
        {
            throw new NotImplementedException();
        }

        BasicToken.Void[] IApplicationService.SetApplicationCmd(Alias deviceNumber, bool IsS0, bool IsS2, LibTypes Library, byte[] wakeUpInterval, byte zwavePlusVersion)
        {
            throw new NotImplementedException();
        }

        BasicToken.Void[] IApplicationService.SetApplicationResponse(Alias deviceNumber, LibTypes Library, byte[] FullCmd)
        {
            throw new NotImplementedException();
        }

        BasicToken.Void[] IApplicationService.SetApplicationResponse(Alias deviceNumber, LibTypes Library, byte[] FullCmd, byte[] wakeUpInterval, byte zwavePlusVersion)
        {
            throw new NotImplementedException();
        }

        public BasicToken.Void[] SetApplicationCmdWithExtraClasses(Alias deviceNumber, bool IsS0, bool IsS2, LibTypes Library, byte[] extraClasses)
        {
            throw new NotImplementedException();
        }

        public BasicToken.Void AddMultiChannelSupport(Alias deviceNumber, byte[] EndPointCcs)
        {
            throw new NotImplementedException();
        }

        public void AddAssociationSupport(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }

        public void AddCommandClassSupport(Alias deviceNumber, byte[] ccIds)
        {
            throw new NotImplementedException();
        }

        public void AddCommandClassSupport(Alias deviceNumber, byte endPointId, CommandClassSupport ccSupport, bool updateVersionCC = true)
        {
            throw new NotImplementedException();
        }

        public void AddOrUpdateVersionCommandClassSupport(Alias deviceNumber, byte endPointId)
        {
            throw new NotImplementedException();
        }

        public BasicToken.Void[] SetApplicationResponsePreconfigured(Alias deviceNumber, IPreconfiguredDevice preconfiguredDevice)
        {
            throw new NotImplementedException();
        }
        public BasicToken.Void[] SetApplicationQuirk(Alias deviceNumber, bool isS0, bool isS2, QuirckLibrary library)
        {
            throw new NotImplementedException();
        }

        public BasicToken.Void[] SetApplicationQuirk(Alias deviceNumber, bool isS0, bool isS2, QuirckLibrary library, byte[] wakeUpInterval, byte zwavePlusVersion, byte[] extraClasses)
        {
            throw new NotImplementedException();
        }

        public BasicToken.Void[] SetApplicationQuirkResponse(Alias deviceNumber, QuirckLibrary library, byte[] fullCmd)
        {
            throw new NotImplementedException();
        }

        public BasicToken.Void[] SetApplicationQuirkResponse(Alias deviceNumber, QuirckLibrary library, byte[] fullCmd, byte[] wakeUpInterval, byte zWavePlusersion, int groupings = 1)
        {
            throw new NotImplementedException();
        }

        public void CancelAllRunningSupportCommands(Alias deviceNumber)
        {
            throw new NotImplementedException();
        }
    }
}