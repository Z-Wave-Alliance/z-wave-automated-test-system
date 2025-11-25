// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Linq;
using System.Threading;
using Utils;
using Utils.UI.Enums;

namespace Zats.Services
{
    public class PCControllerComplianceService : AutomationServiceBase, IComplianceService
    {
        public ISendDataSettings SendDataSettings { get; set; }

        public PCControllerComplianceService(PCControllerDeviceManager pccDeviceManager) : base(pccDeviceManager)
        {
            SendDataSettings = new PCControllerSendDataSettings(PCControllerDeviceManager);
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void ApplicationVirtualDeviceNodeInformation(Alias bridgeNumber, Node endDeviceNode, bool isListening, byte generic, byte specific, byte[] cmdClasses)
        {
            throw new NotImplementedException();
        }

        public BasicResult.AssignReturnRoute AssignReturnRoute(Alias controllerNumber, Alias srcNode, Node destNode)
        {
            throw new NotImplementedException();
        }

        public BasicResult.AssignReturnRoute AssignReturnRoute(Alias controllerNumber, Node srcNode, Node destNode)
        {
            throw new NotImplementedException();
        }

        public byte FirmwareUpdateV2(Alias deviceNumber, Node nodeid, string filePath)
        {
            throw new NotImplementedException();
        }

        public byte FirmwareUpdateV3(Alias deviceNumber, Node nodeid, string filePath)
        {
            throw new NotImplementedException();
        }


        public BasicResult.IsFailedNode IsFailedNode(Alias controllerNumber, Node node)
        {
            BasicResult.IsFailedNode ret = null;
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            if (uiController != null)
            {
                uiController.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);
                uiController.Automation.NavigateTo(PCControllerIdMap.MainWindow.MainMenuBtn).Click();
                uiController.Automation.WaitForElementIsVisible(PCControllerIdMap.NetworkManagementView.ID,
                    () => uiController.Automation.NavigateTo(PCControllerIdMap.MainMenuView.NetworkManagementBtn).Click());
                var networkManagementView = uiController.Automation.NavigateTo(PCControllerIdMap.NetworkManagementView.ID);

                networkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.IsFailedBtn,
                   () => uiController.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                   .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                   .NavigateToNameProperty(node.Id.ToString())
                   .Select());

                networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.IsFailedBtn).ClickIfEnabled();
                uiController.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);

                ret = new PCControllerActionResult.IsFailedNode(uiController);
            }
            return ret;
        }

        public BasicResult.IncludeNodeWithLearnMode ReplaceFailedNode(Alias controllerNumber, Node node, Alias nodeNumber)
        {
            PCControllerActionResult.IncludeNodeWithLearnMode ret = null;
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            if (uiController != null)
            {

                var nodesListObserver = uiController.ActivateNodesListObserver((byte)node.Id);
                var networkManagementView = FromMainMenuPage(uiController, PCControllerIdMap.NetworkManagementView.ID);
                uiController.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                    .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                    .NavigateToNameProperty(node.Id.ToString())
                    .Select();
                networkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.ReplaceFailedBtn, null);
                networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.ReplaceFailedBtn).ClickIfEnabled();

                BasicResult.SetLearnMode learnRes = null;
                var joiningUI = PCControllerDeviceManager.GetUIInstance(nodeNumber);
                bool isEndDeviceAdded = false;
                if (joiningUI != null)
                {
                    var secondaryMainWindow = joiningUI.Automation.NavigateTo(PCControllerIdMap.MainWindow.ID);
                    secondaryMainWindow.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);

                    var sisUI = PCControllerDeviceManager.SisController.Value == 0 ?
                        uiController :
                        PCControllerDeviceManager.GetUIInstance(PCControllerDeviceManager.SisController);
                    SetupLearnModeWithDSKExchange(sisUI, joiningUI, new byte[0]);
                }
                else
                {
                    //isEndDeviceAdded = true;
                    //System.Threading.Thread.Sleep(1000);
                    //learnRes = BasicService.SetLearnMode(nodeNumber, LearnModes.LearnModeClassic, INCLUSION_TIMEOUT_MS);
                }
                uiController.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);

                var nodeInfo = nodesListObserver.GetIncludedNodeInfo()?.First();
                var replaceRes = new PCControllerActionResult.AddNodeWithLearnMode(uiController, nodeInfo, isEndDeviceAdded, false, true);
                replaceRes.SetLearnMode = learnRes;
                ret = new PCControllerActionResult.IncludeNodeWithLearnMode(replaceRes);
            }
            return ret;
        }

        public BasicResult.RemoveNode RemoveFailedNodeId(Alias controllerNumber, Node node)
        {
            PCControllerActionResult.RemoveNode ret = null;
            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            if (uiController != null)
            {
                var networkManagementView = FromMainMenuPage(uiController, PCControllerIdMap.NetworkManagementView.ID);
                uiController.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                    .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                    .NavigateToNameProperty(node.Id.ToString())
                    .Select();
                networkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.RemoveFailedBtn, null);
                networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.RemoveFailedBtn).ClickIfEnabled();

                uiController.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);

                ret = new PCControllerActionResult.RemoveNode(uiController);
            }
            return ret;
        }

        public BasicResult.RequestData RequestData(Alias senderNumber, Node receiverNode, byte[] data, TransmitOptions txOptions, byte[] expectedData, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.RequestData RequestData(Alias senderNumber, Node receiverNode, byte[] data, byte[] expectedData)
        {
            return RequestData(senderNumber, receiverNode, data, expectedData, 0);
        }

        public BasicResult.RequestData RequestData(Alias senderNumber, Node receiverNode, byte[] data, byte[] expectedData, int timeoutMs)
        {
            BasicResult.RequestData ret = null;
            var uiController = PCControllerDeviceManager.GetUIInstance(senderNumber);
            if (uiController != null)
            {
                uiController.ClearLog();
                var commandClassesView = FromMainMenuPage(uiController, PCControllerIdMap.CommandClassesView.ID);
                commandClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.TbCommandToSend).SetElementValue(Utils.Tools.GetHex(data));

                uiController.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                    .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                    .NavigateToNameProperty(receiverNode.Id.ToString())
                    .Select();
                commandClassesView.WaitForElementIsReady(PCControllerIdMap.CommandClassesView.BtnSend, null);
                commandClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.BtnSend).Click();
                var def2min = 2 * 60 * 1000;
                if (timeoutMs > def2min && timeoutMs < 1000)
                {
                    timeoutMs = 1000;
                }
                Thread.Sleep(timeoutMs);
                ret = new PCControllerActionResult.RequestData(uiController, PCControllerDeviceManager.ZWaveDefinition, (byte)receiverNode.Id, expectedData);
            }
            else
            {
                throw new NotImplementedException();
            }
            return ret;
        }

        public BasicResult.RequestData RequestDataEx(Alias senderNumber, Node receiverNode, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, byte[] expectedData, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.RequestData RequestDataEx(Alias senderNumber, Node receiverNode, byte[] data, SecuritySchemes scheme, byte[] expectedData)
        {
            BasicResult.RequestData ret = null;
            var uiController = PCControllerDeviceManager.GetUIInstance(senderNumber);
            if (uiController != null)
            {
                var cmdClassesView = FromMainMenuPage(uiController, PCControllerIdMap.CommandClassesView.ID);
                cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.TbCommandToSend).SetElementValue(Tools.GetHex(data));
                uiController.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                    .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                    .NavigateToNameProperty(receiverNode.Id.ToString())
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

                ret = new PCControllerActionResult.RequestData(uiController, PCControllerDeviceManager.ZWaveDefinition, (byte)receiverNode.Id, expectedData);
            }
            else
            {
                //ret = BasicService.RequestData(senderNumber, receiverNumber, data, expectedData);
            }

            return ret;
        }

        public BasicResult.RequestData RequestDataEx(Alias senderNumber, Node receiverNode, byte[] data, SecuritySchemes scheme, byte[] expectedData, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.RequestData RequestDataEx(Alias senderNumber, Node receiverNode, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2, byte[] expectedData, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public BasicResult.RequestNodeInfo RequestNodeInfo(Alias deviceNumber, Node node)
        {
            BasicResult.RequestNodeInfo ret = null;
            var nodeControllerUI = PCControllerDeviceManager.GetUIInstance(deviceNumber);
            if (nodeControllerUI != null)
            {
                var networkManagementView = FromMainMenuPage(nodeControllerUI, PCControllerIdMap.NetworkManagementView.ID);
                var dest = node.Id.ToString();
                if (node.EndPointId > 0)
                {
                    dest += $".{node.EndPointId}";
                }
                nodeControllerUI.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                 .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                 .NavigateToNameProperty(dest)
                 .Select();
                networkManagementView.WaitForElementIsReady(PCControllerIdMap.NetworkManagementView.NodeInfoBtn, null);

                networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.NodeInfoBtn).Click();
                nodeControllerUI.Automation.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);

                ret = new PCControllerActionResult.RequestNodeInfo(nodeControllerUI, PCControllerDeviceManager.ZWaveDefinition);
            }
            return ret;
        }

        public BasicResult.GetProtocolInfo GetProtocolInfo(Alias deviceNumber, Node node)
        {
            throw new NotImplementedException();
        }

        public BasicResult.SendData SendData(Alias senderNumber, Node receiverNode, byte[] cmdData, TransmitOptions txOptions)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(senderNumber);
            uiController.ClearLog();
            var commandClassesView = FromMainMenuPage(uiController, PCControllerIdMap.CommandClassesView.ID);

            commandClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.TbCommandToSend).SetElementValue(Utils.Tools.GetHex(cmdData));

            // Reset all previous radio buttons to default
            //commandClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.RbDefault).SelectRadioButtonItem();
            if (receiverNode.Id == 0xFF)
            {
                commandClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.RbBroadcast).SelectRadioButtonItem();
            }
            else
            {
                if (commandClassesView.CheckVisible(PCControllerIdMap.NodesListView.ID))
                {
                    commandClassesView.WaitForElementIsReady(PCControllerIdMap.NodesListView.ID, null, WAIT_ELEMENT_READY_TIMEOUT_MS);
                }
                uiController.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                    .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                    .NavigateToNameProperty(receiverNode.Id.ToString())
                    .Select();
            }
            commandClassesView.WaitForElementIsReady(PCControllerIdMap.CommandClassesView.BtnSend, null);
            commandClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.BtnSend).ClickIfEnabled();
            return new PCControllerActionResult.SendData(uiController);
        }

        public BasicResult.SendData SendData(Alias senderNumber, Node receiverNode, byte[] cmdData)
        {
            return SendData(senderNumber, receiverNode, cmdData, TransmitOptions.None);
        }

        public BasicResult.SendData SendDataBridge(Alias bridgeNumber, Node srcNode, Node receiverNode, byte[] data, TransmitOptions txOptions)
        {
            throw new NotImplementedException();
        }

        public BasicResult.SendData SendDataEx(Alias senderNumber, Node receiverNode, byte[] cmdData, SecuritySchemes scheme)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(senderNumber);
            var cmdClassesView = FromMainMenuPage(uiController, PCControllerIdMap.CommandClassesView.ID);
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.TbCommandToSend).SetElementValue(Utils.Tools.GetHex(cmdData));

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

            uiController.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                .NavigateToNameProperty(receiverNode.Id.ToString())
                .Select();

            cmdClassesView.WaitForElementIsReady(PCControllerIdMap.CommandClassesView.BtnSend, null);
            cmdClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.BtnSend).Click();

            return new PCControllerActionResult.SendData(uiController);
        }

        public BasicResult.SendData SendDataEx(Alias senderNumber, Node receiverNode, byte[] cmdData, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2)
        {
            throw new NotImplementedException();
        }

        public BasicResult.SendData SendDataEx(Alias senderNumber, Node receiverNode, byte[] cmdData, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Transmit SendNodeInfo(Alias deviceNumber, Node receiverNode, TransmitOptions txOptions)
        {
            throw new NotImplementedException();
        }

        public void SetActiveSecuritySchemeForNode(Alias deviceNumber, Node node, SecuritySchemes activeSecurityScheme)
        {
            throw new NotImplementedException();
        }

        public void SetSecurityForNode(Alias deviceNumber, Node node, bool isEnable)
        {
            throw new NotImplementedException();
        }

        public void SetSecurityVersionForBridge(Alias deviceNumber, byte VirtualNodeId, Node node, SecurityVersions securityVersion)
        {
            throw new NotImplementedException();
        }

        public void SetSecurityVersionForNode(Alias deviceNumber, Node node, SecurityVersions securityVersion)
        {
            throw new NotImplementedException();
        }

        public BasicResult.SetSucNodeId SetSUCNodeID(Alias controllerNumber, Node node, bool isEnable, byte capabilities, bool isLowPower)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Transmit SendDataMulti(Alias senderNumber, Node[] receiverNodeIds, byte[] data, TransmitOptions txOptions)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(senderNumber);
            uiController.ClearLog();

            var commandClassesView = FromMainMenuPage(uiController, PCControllerIdMap.CommandClassesView.ID);
            commandClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.TbCommandToSend).SetElementValue(Utils.Tools.GetHex(data));

            if (txOptions == TransmitOptions.None)
            {
                commandClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.SuppressMulticastFollowUpChk).SetCheckBoxStateAt(true);
            }
            else
            {
                commandClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.SuppressMulticastFollowUpChk).SetCheckBoxStateAt(false);
            }
            var nodelist = uiController.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID).NavigateTo(PCControllerIdMap.NodesListView.NodesLv);
            nodelist.NavigateToNameProperty(receiverNodeIds.First().ToString()).Select();
            if (receiverNodeIds.Length > 1)
            {
                foreach (var receiver in receiverNodeIds.Skip(1))
                {
                    nodelist.NavigateToNameProperty(receiver.ToString()).AddToSelection();
                }
            }
            else
            {
                commandClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.ForceMulticastChk).SetCheckBoxStateAt(true);
            }
            //alt+check selected : Doens't help
            nodelist.SelectValuesWithinGrid(receiverNodeIds.Select(x => x.Id.ToString()).ToArray(), 0);
            var selected = nodelist.GetSelectedValuesWithinGrid(0);
            if (selected.Length != receiverNodeIds.Length)
            {
                throw new InvalidOperationException("Failed Send Data Multi - select values = " + string.Join(", ", selected));
            }
            commandClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.BtnSend).Click();
            commandClassesView.WaitForElementIsReady(PCControllerIdMap.CommandClassesView.ForceMulticastChk, null);
            commandClassesView.NavigateTo(PCControllerIdMap.CommandClassesView.ForceMulticastChk).SetCheckBoxStateAt(false);
            return new PCControllerActionResult.SendData(uiController);
        }

        public byte FirmwareUpdateV5(Alias deviceNumber, Node nodeid, string filePath)
        {
            throw new NotImplementedException();
        }

        public byte FirmwareUpdateV5(Alias deviceNumber, Node nodeid, string filePath, byte activation)
        {
            throw new NotImplementedException();
        }

        public byte ActivteFirmwareUpdate(Alias deviceNumber, Node node, string filePath)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Transmit SendNop(Alias senderNumber, Node node, TransmitOptions txOptions)
        {
            var uiController = PCControllerDeviceManager.GetUIInstance(senderNumber);
            var networkManagementView = FromMainMenuPage(uiController, PCControllerIdMap.NetworkManagementView.ID);
            var primaryMainWindow = uiController.Automation.NavigateTo(PCControllerIdMap.MainWindow.ID);

            uiController.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID)
                .NavigateTo(PCControllerIdMap.NodesListView.NodesLv)
                .NavigateToNameProperty(node.Id.ToString())
                .Select();

            uiController.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID).NavigateTo(PCControllerIdMap.NodesListView.NodesLv).
                   GetRowWithinGrid(node.Id.ToString(), 0).NavigateTo(PCControllerIdMap.NodesListView.EnableQOChk).TrySetCheckBoxStateAt(true);

            networkManagementView.NavigateTo(PCControllerIdMap.NetworkManagementView.NopBtn).Click();
            primaryMainWindow.WaitForElementNotVisible(PCControllerIdMap.MainWindow.BusyView, null);

            uiController.Automation.NavigateTo(PCControllerIdMap.NodesListView.ID).NavigateTo(PCControllerIdMap.NodesListView.NodesLv).
                   GetRowWithinGrid(node.Id.ToString(), 0).NavigateTo(PCControllerIdMap.NodesListView.EnableQOChk).TrySetCheckBoxStateAt(false);

            return new PCControllerActionResult.SendDataNop(uiController);
        }

        public BasicResult.SetPriorityRoute SetPriorityRoute(Alias controllerNumber, Node nodeNumber, Node priority0, Node priority1, Node priority2, Node priority3, byte routespeed)
        {
            BasicResult.SetPriorityRoute res = null;

            var uiController = PCControllerDeviceManager.GetUIInstance(controllerNumber);
            if (uiController != null)
            {
                uiController.ClearLog();
                var setupRouteView = FromMainMenuPage(uiController, PCControllerIdMap.SetupRouteView.ID);
                setupRouteView.NavigateTo(PCControllerIdMap.SetupRouteView.RbGetSetPriorityRoute).SelectRadioButtonItem();
                setupRouteView.WaitForElementIsReady(PCControllerIdMap.SetupRouteView.TbPriorityRoute, null);
                setupRouteView.WaitForElementIsReady(PCControllerIdMap.SetupRouteView.CmbRouteSpeed, null);

                string repeaters = string.Join(" ", priority0.Id.ToString("00"), priority1.Id.ToString("00"), priority2.Id.ToString("00"), priority3.Id.ToString("00"));
                setupRouteView.NavigateTo(PCControllerIdMap.SetupRouteView.TbPriorityRoute).SetElementValue(repeaters);

                string speed = null;
                switch (routespeed)
                {
                    case 0:
                    case 1:
                        speed = "9.6 KBit/s";
                        break;
                    case 2:
                        speed = "40 KBit/s";
                        break;
                    case 3:
                        speed = "100 KBit/s";
                        break;
                    default:
                        break;
                }
                setupRouteView.NavigateTo(PCControllerIdMap.SetupRouteView.CmbRouteSpeed).SelectComboboxItem(speed);


                uiController.Automation.NavigateTo(PCControllerIdMap.SetupRouteView.LvDestinationsNodeList)
                    .NavigateToNameProperty(nodeNumber.Id.ToString())
                    .Select();

                setupRouteView.WaitForElementIsReady(PCControllerIdMap.SetupRouteView.BtnSetPriorityRoute, null);
                setupRouteView.NavigateTo(PCControllerIdMap.SetupRouteView.BtnSetPriorityRoute).Click();
                Thread.Sleep(1000);

                res = new PCControllerActionResult.SetPriorityRoute(uiController);
            }
            return res;
        }

        public BasicResult.Transmit SendDataMultiBridge(Alias bridgeNumber, Node srcNode, Node[] receiverNodeIds, byte[] data, TransmitOptions txOptions)
        {
            throw new NotImplementedException();
        }

        public byte FirmwareUpdateV3(Alias deviceNumber, Node node, string filePath, ushort fragmentSize)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Void SetRoutingInfo(Alias controllerNumber, Node node, byte[] nodeMask)
        {
            throw new NotImplementedException();
        }

        public BasicResult.SendData SendDataEx(Alias senderNumber, Node receiverNode, byte[] cmdData, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2, int Timeout)
        {
            throw new NotImplementedException();
        }

        public BasicResult.SendData SendDataEx(Alias senderNumber, Node receiverNode, byte[] cmdData, SecuritySchemes scheme, int Timeout)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Transmit SendDataMultiEx(Alias senderNumber, Node[] receiverNodeIds, byte groupId, byte[] data, SecuritySchemes scheme)
        {
            throw new NotImplementedException();
        }

        public byte FirmwareUpdateV5(Alias deviceNumber, Node nodeid, string filePath, ushort fragmentSize, byte activation)
        {
            throw new NotImplementedException();
        }

        public void SetActiveSecuritySchemeForNode(Alias deviceNumber, Node node, SecuritySchemes activeSecurityScheme, bool isLRScheme)
        {
            throw new NotImplementedException();
        }

        public BasicResult.Transmit SendTestFrame(Alias deviceNumber, Node nodeId, byte powerLevel)
        {
            throw new NotImplementedException();
        }

        public BasicResult.RequestNodeNeighborUpdate RequestNodeNeighborDiscovery(Alias controllerNumber, Node node, byte nodeType)
        {
            throw new NotImplementedException();
        }

        public BasicResult.GetPriorityRoute GetPriorityRoute(Alias controllerNumber, Node node)
        {
            throw new NotImplementedException();
        }

        public BasicResult.GetRoutingInfo GetRoutingInfo(Alias controllerNumber, Node node, byte removeBadLinks, byte removeNonRepeaters)
        {
            throw new NotImplementedException();
        }

        public BasicResult.RequestNodeNeighborUpdate RequestNodeNeighborUpdate(Alias controller, Node node)
        {
            throw new NotImplementedException();
        }
    }
}
