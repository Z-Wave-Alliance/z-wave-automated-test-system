// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using Utils.UI.Enums;

namespace Zats.Services
{
    public class PCControllerSendDataSettings : AutomationServiceBase, ISendDataSettings
    {
        public PCControllerSendDataSettings(PCControllerDeviceManager pccDeviceManager) : base(pccDeviceManager)
        {
        }

        public void SetCRC16(Alias controllerNumber, bool enabled)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            var cmdClassesView = FromMainMenuPage(uiController, PCControllerIdMap.CommandClassesView.ID);
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.CRC16Chk).SetCheckBoxStateAt(true);
        }

        public void SetSupressMulticastFollowUp(Alias controllerNumber, bool enabled)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            var cmdClassesView = FromMainMenuPage(uiController, PCControllerIdMap.CommandClassesView.ID);
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.SuppressMulticastFollowUpChk).SetCheckBoxStateAt(true);
        }

        public void SetForceMultiCast(Alias controllerNumber, bool enabled)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            var cmdClassesView = FromMainMenuPage(uiController, PCControllerIdMap.CommandClassesView.ID);
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.ForceMulticastChk).SetCheckBoxStateAt(true);
        }

        public void SetSupervisionGet(Alias controllerNumber, bool enabled)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            var cmdClassesView = FromMainMenuPage(uiController, PCControllerIdMap.CommandClassesView.ID);
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.SupervisionGetChk).SetCheckBoxStateAt(true);
        }

        public void SetStatusUpdates(Alias controllerNumber, bool enabled)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            var cmdClassesView = FromMainMenuPage(uiController, PCControllerIdMap.CommandClassesView.ID);
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.StatusUpdatesChk).SetCheckBoxStateAt(true);
        }

        public void SetSessionID(Alias controllerNumber, byte value)
        {
            SetAutoIncrement(controllerNumber, false);
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            var cmdClassesView = FromMainMenuPage(uiController, PCControllerIdMap.CommandClassesView.ID);
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.TbSessionID).SetElementValue(Utils.Tools.GetHex(value));
        }

        public void SetAutoIncrement(Alias controllerNumber, bool enabled)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            var cmdClassesView = FromMainMenuPage(uiController, PCControllerIdMap.CommandClassesView.ID);
            if (cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.AutoIncrementChk).GetCheckBoxStateAt() != enabled)
            {
                cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.AutoIncrementChk).SetCheckBoxStateAt(enabled);
            }
        }

        public void SetMultiChannel(Alias controllerNumber, bool enabled)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            var cmdClassesView = FromMainMenuPage(uiController, PCControllerIdMap.CommandClassesView.ID);
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.MultiChannelChk).SetCheckBoxStateAt(true);
        }

        public void SetSrcEndPoint(Alias controllerNumber, byte enabled)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            var cmdClassesView = FromMainMenuPage(uiController, PCControllerIdMap.CommandClassesView.ID);
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.TbSrcEndPoint).SetElementValue(enabled.ToString());
        }

        public void SetDstEndPoint(Alias controllerNumber, byte enabled)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            var cmdClassesView = FromMainMenuPage(uiController, PCControllerIdMap.CommandClassesView.ID);
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.TbDestEndPoint).SetElementValue(enabled.ToString());
        }

        public void SetBitAdress(Alias controllerNumber, bool enabled)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            var cmdClassesView = FromMainMenuPage(uiController, PCControllerIdMap.CommandClassesView.ID);
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.BitAddressChk).SetCheckBoxStateAt(true);
        }

        public void SetSecure(Alias controllerNumber, bool? enabled)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            var cmdClassesView = FromMainMenuPage(uiController, PCControllerIdMap.CommandClassesView.ID);
            if (enabled == true)
            {
                cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.RbSecure).SelectRadioButtonItem();
            }
            else if (enabled == false)
            {
                cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.RbNonSercure).SelectRadioButtonItem();
            }
            else
            {
                cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.RbDefault).SelectRadioButtonItem();
            }
        }
        public void Clear(Alias controllerNumber)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            var cmdClassesView = FromMainMenuPage(uiController, PCControllerIdMap.CommandClassesView.ID);

            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.SupervisionGetChk).TrySetCheckBoxStateAt(true);
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.AutoIncrementChk).TrySetCheckBoxStateAt(false);
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.TbSessionID).SetElementValue(Utils.Tools.GetHex(new byte()));
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.StatusUpdatesChk).TrySetCheckBoxStateAt(false);
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.SupervisionGetChk).TrySetCheckBoxStateAt(false);

            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.MultiChannelChk).TrySetCheckBoxStateAt(true);
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.TbSrcEndPoint).SetElementValue(Utils.Tools.GetHex(new byte()));
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.TbDestEndPoint).SetElementValue(Utils.Tools.GetHex(new byte()));
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.MultiChannelChk).TrySetCheckBoxStateAt(false);

            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.BitAddressChk).TrySetCheckBoxStateAt(false);
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.CRC16Chk).TrySetCheckBoxStateAt(false);
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.SuppressMulticastFollowUpChk).TrySetCheckBoxStateAt(false);
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.ForceMulticastChk).TrySetCheckBoxStateAt(false);
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.SupervisionGetChk).TrySetCheckBoxStateAt(false);
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.MultiChannelChk).TrySetCheckBoxStateAt(false);

            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.RbDefault).SelectRadioButtonItem();
        }

        public void SetTransportServiceMaxSize(Alias controllerNumber, int value)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            if (uiController != null)
            {
                uiController.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlg = uiController.Automation.OpenDialog(PCControllerIdMap.DialogWindow.ID, PCControllerIdMap.SendDataSettingsDialog.Title,
                        () => uiController.Automation.NavigateTo(PCControllerIdMap.MainWindow.SendDataSettingsBtn).Click(),
                        () => uiController.Automation.NavigateTo(PCControllerIdMap.DialogWindow.ID, PCControllerIdMap.SendDataSettingsDialog.Title).
                        NavigateTo(PCControllerIdMap.SendDataSettingsDialog.OkBtn).Click())
                        )
                {
                    dlg.DialogView.NavigateTo(PCControllerIdMap.SendDataSettingsDialog.TransportServiceMaxSegmentSizeTb).SetElementValue(value.ToString());
                }
            }
        }

        public void SetMaxBytesPerFrameSize(Alias controllerNumber, int value)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            if (uiController != null)
            {
                uiController.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlg = uiController.Automation.OpenDialog(PCControllerIdMap.SendDataSettingsDialog.ID, PCControllerIdMap.SendDataSettingsDialog.Title,
                        () => uiController.Automation.NavigateTo(PCControllerIdMap.MainWindow.SendDataSettingsBtn).Click(),
                        () => uiController.Automation.NavigateTo(PCControllerIdMap.SendDataSettingsDialog.ID, PCControllerIdMap.SendDataSettingsDialog.Title).
                        NavigateTo(PCControllerIdMap.SendDataSettingsDialog.OkBtn).Click())
                        )
                {
                    dlg.DialogView.NavigateTo(PCControllerIdMap.SendDataSettingsDialog.S0MaxBytesPerFrameSizeTb).SetElementValue(value.ToString());
                }
            }
        }

        protected override void Log(string text, LogLevels logLevel)
        {
        }

        public void SetSkipWaitingSendCallback(Alias controllerNumber, bool enabled)
        {
            throw new System.NotImplementedException();
        }

        public void SetResponseDelayMs(Alias controllerNumber, int value)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            if (uiController != null)
            {
                uiController.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlg = uiController.Automation.OpenDialog(PCControllerIdMap.SendDataSettingsDialog.ID, PCControllerIdMap.SendDataSettingsDialog.Title,
                        () => uiController.Automation.NavigateTo(PCControllerIdMap.MainWindow.SendDataSettingsBtn).Click(),
                        () => uiController.Automation.NavigateTo(PCControllerIdMap.SendDataSettingsDialog.ID, PCControllerIdMap.SendDataSettingsDialog.Title).
                        NavigateTo(PCControllerIdMap.SendDataSettingsDialog.OkBtn).Click())
                        )
                {
                    dlg.DialogView.NavigateTo(PCControllerIdMap.SendDataSettingsDialog.DelayResponseMsTb).SetElementValue(value.ToString());
                }
            }
        }

        public bool GetCRC16(Alias controllerNumber)
        {
            throw new System.NotImplementedException();
        }

        public bool GetSupressMulticastFollowUp(Alias controllerNumber)
        {
            throw new System.NotImplementedException();
        }

        public bool GetForceMultiCast(Alias controllerNumber)
        {
            throw new System.NotImplementedException();
        }

        public bool GetSkipWaitingSendCallback(Alias controllerNumber)
        {
            throw new System.NotImplementedException();
        }

        public bool GetSupervisionGet(Alias controllerNumber)
        {
            throw new System.NotImplementedException();
        }

        public bool GetStatusUpdates(Alias controllerNumber)
        {
            throw new System.NotImplementedException();
        }

        public byte GetSessionID(Alias controllerNumber)
        {
            throw new System.NotImplementedException();
        }

        public bool GetAutoIncrement(Alias controllerNumber)
        {
            throw new System.NotImplementedException();
        }

        public bool GetMultiChannel(Alias controllerNumber)
        {
            throw new System.NotImplementedException();
        }

        public byte GetSrcEndPoint(Alias controllerNumber)
        {
            throw new System.NotImplementedException();
        }

        public byte GetDstEndPoint(Alias controllerNumber)
        {
            throw new System.NotImplementedException();
        }

        public bool GetBitAdress(Alias controllerNumber)
        {
            throw new System.NotImplementedException();
        }

        public bool? GetSecure(Alias controllerNumber)
        {
            throw new System.NotImplementedException();
        }

        public int GetTransportServiceMaxSize(Alias controllerNumber)
        {
            throw new System.NotImplementedException();
        }

        public int GetMaxBytesPerFrameSize(Alias controllerNumber)
        {
            throw new System.NotImplementedException();
        }

        public int GetResponseDelayMs(Alias controllerNumber)
        {
            throw new System.NotImplementedException();
        }

        public void SetCallbackWaitTimeout(Alias deviceNumber, int timeoutMs)
        {
            throw new System.NotImplementedException();
        }

        public int GetCallbackWaitTimeoutMs(Alias senderNumber)
        {
            throw new System.NotImplementedException();
        }

        public void SetRequestTimeoutMs(Alias controllerNumber, int value)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            if (uiController != null)
            {
                uiController.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                using (var dlg = uiController.Automation.OpenDialog(PCControllerIdMap.SendDataSettingsDialog.ID, PCControllerIdMap.SendDataSettingsDialog.Title,
                        () => uiController.Automation.NavigateTo(PCControllerIdMap.MainWindow.SendDataSettingsBtn).Click(),
                        () => uiController.Automation.NavigateTo(PCControllerIdMap.SendDataSettingsDialog.ID, PCControllerIdMap.SendDataSettingsDialog.Title).
                        NavigateTo(PCControllerIdMap.SendDataSettingsDialog.OkBtn).Click())
                        )
                {
                    dlg.DialogView.NavigateTo(PCControllerIdMap.SendDataSettingsDialog.RequestsTimeoutMsTb).SetElementValue(value.ToString());
                }
            }
        }

        public int GetRequestTimeoutMs(Alias controllerNumber)
        {
            throw new System.NotImplementedException();
        }
    }
}
