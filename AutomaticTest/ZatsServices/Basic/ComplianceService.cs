// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
// SPDX-FileCopyrightText: 2025 Z-Wave Alliance https://z-wavealliance.org
using System.Collections.Generic;
using TransmitOptions = Zats.Services.TransmitOptions;
using System.Threading;
using Utils;
using ZWave;
using ZWave.BasicApplication.Devices;
using ZWave.BasicApplication.Operations;
using System.IO;
using ZWave.BasicApplication;
using Utils.UI.Enums;
using ZWave.CommandClasses;
using ZWave.BasicApplication.Tasks;
using System;
using ZWave.Security;
using System.Linq;
using ZWave.Devices;

namespace Zats.Services
{
    public class ComplianceService : ServiceBase, IComplianceService
    {
        private readonly TransmitOptions TX_OPTIONS = TransmitOptions.Ack | TransmitOptions.AutoRoute | TransmitOptions.Explore;
        private readonly TransmitSecurityOptions TX_SEC_OPTIONS = TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY;
        private readonly int REQUEST_TIMEOUT_MS = 10000;
        private DeviceManager DeviceManager { get; set; }

        public ISendDataSettings SendDataSettings
        {
            get
            {
                return DeviceManager.SendDataSettings;
            }
        }

        public ComplianceService(DeviceManager deviceManager)
        {
            DeviceManager = deviceManager;
        }

        protected override void Log(string text, LogLevels logLevel)
        {
            DeviceManager.Log(text, logLevel);
        }

        #region ICommandClassesService Members

        public BasicResult.Transmit SendNodeInfo(Alias deviceNumber, Node receiverNode, TransmitOptions txOptions)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            TransmitResult res = device.SendNodeInformation(receiverNode, (ZWave.Enums.TransmitOptions)txOptions);
            return new BasicActionResult.Transmit(res);
        }

        public BasicResult.Void ApplicationVirtualDeviceNodeInformation(Alias bridgeNumber, Node endDeviceNode, bool isListening, byte generic, byte specific, byte[] cmdClasses)
        {
            BridgeController bridge = (BridgeController)DeviceManager.GetDevice(bridgeNumber);
            ActionResult res = bridge.ApplicationVirtualDeviceNodeInformation(endDeviceNode, isListening, generic, specific, cmdClasses);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.SetSucNodeId SetSUCNodeID(Alias controllerNumber, Node node, bool isEnable, byte capabilities, bool isLowPower)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            SetSucNodeIdResult res = controller.SetSucNodeID(node, isEnable, isLowPower, capabilities);
            return new BasicActionResult.SetSucNodeId(res);
        }

        public BasicResult.SendData SendData(Alias senderNumber, Node receiverNode, byte[] cmdData, TransmitOptions txOptions)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);

            var data = cmdData;
            data = ApplySupervisionMultiChannelAndCRC16SettingsToEncapData(senderNumber, data);

            var substituteSettings = GetCommonSubstituteSettings(senderNumber);
            substituteSettings = ApplyMulticastSettingsToSubstSettings(senderNumber, substituteSettings);

            if (SendDataSettings.GetForceMultiCast(senderNumber))
            {
                var subTxOptions = (ZWave.Enums.TransmitOptions)txOptions;
                if (substituteSettings.SubstituteFlags.HasFlag(ZWave.Enums.SubstituteFlags.DenyFollowup))
                {
                    subTxOptions = subTxOptions & (~ZWave.Enums.TransmitOptions.TransmitOptionAcknowledge);
                }
                var result = sender.SendDataMulti(new NodeTag[] { receiverNode }, data, subTxOptions, substituteSettings);
                return new BasicActionResult.SendData(result);
            }
            else
            {
                var result = sender.SendData(receiverNode, data, (ZWave.Enums.TransmitOptions)txOptions, substituteSettings);
                return new BasicActionResult.SendData(result);
            }
        }

        /// <summary>
        /// Encapsulates data with Supervision Get encapsulation if enabled in ComplianceService.SendDataSettings.
        /// </summary>
        /// <returns>(Supervision-encapsulated) data</returns>
        private byte[] ApplySupervisionSettingsToEncapData(Alias senderNumber, byte[] data)
        {
            if (SendDataSettings.GetSupervisionGet(senderNumber) && data.Length > 1)
            {
                COMMAND_CLASS_SUPERVISION.SUPERVISION_GET encapData = new COMMAND_CLASS_SUPERVISION.SUPERVISION_GET();
                if (SendDataSettings.GetAutoIncrement(senderNumber))
                {
                    SendDataSettings.SetSessionID(senderNumber, (byte)(SendDataSettings.GetSessionID(senderNumber) + 1));
                }
                encapData.properties1.statusUpdates = SendDataSettings.GetStatusUpdates(senderNumber) ? (byte)1 : (byte)0;
                encapData.properties1.sessionId = SendDataSettings.GetSessionID(senderNumber);
                encapData.encapsulatedCommandLength = (byte)data.Length;
                encapData.encapsulatedCommand = new List<byte>(data);
                data = encapData;
            }
            return data;
        }

        /// <summary>
        /// Encapsulates data with Supervision and/or Multi Channel and/or CRC16 encapsulation each if enabled in ComplianceService.SendDataSettings.
        /// </summary>
        /// <returns>(encapsulated) data</returns>
        private byte[] ApplySupervisionMultiChannelAndCRC16SettingsToEncapData(Alias senderNumber, byte[] data)
        {
            data = ApplySupervisionSettingsToEncapData(senderNumber, data);

            if ((SendDataSettings.GetMultiChannel(senderNumber) || SendDataSettings.GetSrcEndPoint(senderNumber) != 0) && data.Length > 1)
            {
                COMMAND_CLASS_MULTI_CHANNEL_V3.MULTI_CHANNEL_CMD_ENCAP multiChannelCmd = new COMMAND_CLASS_MULTI_CHANNEL_V3.MULTI_CHANNEL_CMD_ENCAP();

                multiChannelCmd.commandClass = data[0];
                multiChannelCmd.command = data[1];
                multiChannelCmd.parameter = new List<byte>();
                for (int i = 2; i < data.Length; i++)
                {
                    multiChannelCmd.parameter.Add(data[i]);
                }
                multiChannelCmd.properties1.res = 0;
                multiChannelCmd.properties1.sourceEndPoint = (SendDataSettings.GetMultiChannel(senderNumber) ?
                    SendDataSettings.GetSrcEndPoint(senderNumber) : (byte)0);
                multiChannelCmd.properties2.bitAddress = Convert.ToByte(SendDataSettings.GetBitAdress(senderNumber));
                multiChannelCmd.properties2.destinationEndPoint = (SendDataSettings.GetMultiChannel(senderNumber) ?
                    SendDataSettings.GetDstEndPoint(senderNumber) : SendDataSettings.GetSrcEndPoint(senderNumber));
                data = multiChannelCmd;
            }

            if (SendDataSettings.GetCRC16(senderNumber) && data.Length > 1)
            {
                COMMAND_CLASS_CRC_16_ENCAP.CRC_16_ENCAP encapData = new COMMAND_CLASS_CRC_16_ENCAP.CRC_16_ENCAP();
                encapData.commandClass = data[0];
                encapData.command = data[1];
                encapData.data = new List<byte>();

                for (int i = 2; i < data.Length; i++)
                {
                    encapData.data.Add(data[i]);
                }

                encapData.checksum = new byte[] { 0, 0 };
                byte[] tmp = encapData;
                ushort crc = Utils.Tools.ZW_CreateCrc16(null, 0, tmp, (byte)(tmp.Length - 2));
                encapData.checksum = new[] { (byte)(crc >> 8), (byte)crc };
                data = encapData;
            }
            return data;
        }

        /// <summary>
        /// Creates common SubstituteSettings in accordance with ComplianceService.SendDataSettings.
        /// </summary>
        /// <returns>SubstituteSettings</returns>
        private SubstituteSettings GetCommonSubstituteSettings(Alias senderNumber)
        {
            ZWave.Enums.SubstituteFlags substituteFlags;
            if (SendDataSettings.GetSecure(senderNumber) == null)
            {
                substituteFlags = ZWave.Enums.SubstituteFlags.None;
            }
            else
            {
                substituteFlags = (bool)SendDataSettings.GetSecure(senderNumber) ?
                    ZWave.Enums.SubstituteFlags.UseSecurity : ZWave.Enums.SubstituteFlags.DenySecurity;
            }
            var substituteSettings = new SubstituteSettings();
            substituteSettings.S0MaxBytesPerFrameSize = SendDataSettings.GetMaxBytesPerFrameSize(senderNumber);
            substituteSettings.SubstituteFlags = substituteFlags;
            substituteSettings.IsSkipWaitingSendCallbackEnabled = SendDataSettings.GetSkipWaitingSendCallback(senderNumber);
            substituteSettings.CallbackWaitTimeoutMs = SendDataSettings.GetCallbackWaitTimeoutMs(senderNumber);
            return substituteSettings;
        }        

        /// <summary>
        /// Applies Multi Channel settings to SubstituteSettings in accordance with ComplianceService.SendDataSettings.
        /// </summary>
        /// <returns>SubstituteSettings</returns>
        private SubstituteSettings ApplyMultiChannelSettingsToSubstSettings(Alias senderNumber, SubstituteSettings substituteSettings)
        {
            if (substituteSettings == null)
            {
                substituteSettings = GetCommonSubstituteSettings(senderNumber);
            }

            if (SendDataSettings.GetMultiChannel(senderNumber))
            {
                substituteSettings.SubstituteFlags |= ZWave.Enums.SubstituteFlags.UseMultiChannel;
            }
            substituteSettings.IsBitAdress = SendDataSettings.GetBitAdress(senderNumber);
            substituteSettings.DstEndPoint = SendDataSettings.GetDstEndPoint(senderNumber);
            substituteSettings.SrcEndPoint = SendDataSettings.GetSrcEndPoint(senderNumber);
            return substituteSettings;
        }

        /// <summary>
        /// Applies CRC16 settings to SubstituteSettings in accordance with ComplianceService.SendDataSettings.
        /// </summary>
        /// <returns>SubstituteSettings</returns>
        private SubstituteSettings ApplyCRC16SettingsToSubstSettings(Alias senderNumber, SubstituteSettings substituteSettings)
        {
            if (substituteSettings == null)
            {
                substituteSettings = GetCommonSubstituteSettings(senderNumber);
            }

            if (SendDataSettings.GetCRC16(senderNumber))
            {
                substituteSettings.SubstituteFlags |= ZWave.Enums.SubstituteFlags.UseCrc16Encap;
            }
            return substituteSettings;
        }

        /// <summary>
        /// Applies Multicast settings to SubstituteSettings in accordance with ComplianceService.SendDataSettings.
        /// </summary>
        /// <returns>SubstituteSettings</returns>
        private SubstituteSettings ApplyMulticastSettingsToSubstSettings(Alias senderNumber, SubstituteSettings substituteSettings)
        {
            if (substituteSettings == null)
            {
                substituteSettings = GetCommonSubstituteSettings(senderNumber);
            }

            if (SendDataSettings.GetForceMultiCast(senderNumber) && SendDataSettings.GetSupressMulticastFollowUp(senderNumber))
            {
                substituteSettings.SubstituteFlags |= ZWave.Enums.SubstituteFlags.DenyFollowup;
            }
            return substituteSettings;
        }

        public BasicResult.SendData SendData(Alias senderNumber, Node receiverNode, byte[] cmdData)
        {
            return SendData(senderNumber, receiverNode, cmdData, TX_OPTIONS);
        }

        public BasicResult.SendData SendDataEx(Alias senderNumber, Node receiverNode, byte[] cmdData, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme)
        {
            return SendDataEx(senderNumber, receiverNode, cmdData, txOptions, txSecOptions, scheme, TransmitOptions2.NONE);
        }

        public BasicResult.SendData SendDataEx(Alias senderNumber, Node receiverNode, byte[] cmdData, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);

            var data = cmdData;
            data = ApplySupervisionMultiChannelAndCRC16SettingsToEncapData(senderNumber, data);

            var substituteSettings = GetCommonSubstituteSettings(senderNumber);
            substituteSettings = ApplyMulticastSettingsToSubstSettings(senderNumber, substituteSettings);

            if (SendDataSettings.GetForceMultiCast(senderNumber))
            {
                byte groupId = 0;

                TransmitResult res = sender.SendDataMultiEx(data, (ZWave.Enums.TransmitOptions)txOptions, substituteSettings, (ZWave.Enums.SecuritySchemes)scheme, groupId);
                if (!SendDataSettings.GetSupressMulticastFollowUp(senderNumber))
                {
                    var txSOptions = TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY | TransmitSecurityOptions.S2_TXOPTION_FIRST_SINGLECAST_FOLLOWUP | TransmitSecurityOptions.S2_TXOPTION_SINGLECAST_FOLLOWUP;

                    var svget = new COMMAND_CLASS_SUPERVISION.SUPERVISION_GET();
                    svget.encapsulatedCommand = data;
                    svget.encapsulatedCommandLength = (byte)data.Length;
                    svget.properties1.sessionId = ++supervisionSessionId;
                    RequestDataEx(
                        senderNumber,
                        receiverNode,
                        svget,
                        txOptions,
                        txSOptions,
                        scheme,
                        new COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT(),
                        2000);
                }
                return new BasicActionResult.SendData(res);
            }
            else
            {
                SendDataResult res = sender.SendDataEx(receiverNode, data, (ZWave.Enums.TransmitOptions)txOptions, substituteSettings, (ZWave.Enums.TransmitSecurityOptions)txSecOptions, (ZWave.Enums.SecuritySchemes)scheme, (ZWave.Enums.TransmitOptions2)txOptions2);
                return new BasicActionResult.SendData(res);
            }
        }

        public BasicResult.SendData SendDataEx(Alias senderNumber, Node receiverNode, byte[] cmdData, SecuritySchemes scheme)
        {
            return SendDataEx(senderNumber, receiverNode, cmdData, TX_OPTIONS, TX_SEC_OPTIONS, scheme);
        }

        public BasicResult.SendData SendDataEx(Alias senderNumber, Node receiverNode, byte[] cmdData, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2, int Timeout)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            SendDataResult res = sender.SendDataEx(receiverNode, cmdData, (ZWave.Enums.TransmitOptions)txOptions, (ZWave.Enums.TransmitSecurityOptions)txSecOptions, (ZWave.Enums.SecuritySchemes)scheme, (ZWave.Enums.TransmitOptions2)txOptions2, Timeout);
            return new BasicActionResult.SendData(res);
        }

        public BasicResult.SendData SendDataEx(Alias senderNumber, Node receiverNode, byte[] cmdData, SecuritySchemes scheme, int Timeout)
        {
            return SendDataEx(senderNumber, receiverNode, cmdData, TransmitOptions.Ack | TransmitOptions.AutoRoute | TransmitOptions.Explore
                , TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY, scheme, TransmitOptions2.NONE, Timeout);
        }

        public BasicResult.Transmit SendDataMulti(Alias senderNumber, Node[] receiverNodes, byte[] data, TransmitOptions txOptions)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            TransmitResult res = sender.SendDataMulti(receiverNodes.Select(x => (NodeTag)x).ToArray(), data, (ZWave.Enums.TransmitOptions)txOptions);
            return new BasicActionResult.Transmit(res);
        }

        private volatile byte supervisionSessionId = 0;
        public BasicResult.Transmit SendDataMultiEx(Alias senderNumber, Node[] receiverNodeIds, byte groupId, byte[] data, SecuritySchemes scheme)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            TransmitResult res = sender.SendDataMultiEx(data, (ZWave.Enums.TransmitOptions)TX_OPTIONS, (ZWave.Enums.SecuritySchemes)scheme, groupId);
            if (!SendDataSettings.GetSupressMulticastFollowUp(senderNumber))
            {
                var txSOptions = TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY | TransmitSecurityOptions.S2_TXOPTION_FIRST_SINGLECAST_FOLLOWUP | TransmitSecurityOptions.S2_TXOPTION_SINGLECAST_FOLLOWUP;
                foreach (var nodeId in receiverNodeIds)
                {
                    var svget = new COMMAND_CLASS_SUPERVISION.SUPERVISION_GET();
                    svget.encapsulatedCommand = data;
                    svget.encapsulatedCommandLength = (byte)data.Length;
                    svget.properties1.sessionId = ++supervisionSessionId;
                    RequestDataEx(
                        senderNumber,
                        nodeId,
                        svget,
                        TX_OPTIONS,
                        txSOptions,
                        scheme,
                        new COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT(),
                        2000);
                    txSOptions = TransmitSecurityOptions.S2_TXOPTION_VERIFY_DELIVERY | TransmitSecurityOptions.S2_TXOPTION_SINGLECAST_FOLLOWUP;
                }
            }
            return new BasicActionResult.Transmit(res);
        }

        public BasicResult.SendData SendDataBridge(Alias bridgeNumber, Node srcNode, Node receiverNode, byte[] data, TransmitOptions txOptions)
        {
            BridgeController bridge = (BridgeController)DeviceManager.GetDevice(bridgeNumber);
            SendDataResult res = bridge.SendDataBridge(srcNode, receiverNode, data, (ZWave.Enums.TransmitOptions)txOptions);
            return new BasicActionResult.SendData(res);
        }

        public BasicResult.Transmit SendDataMultiBridge(Alias bridgeNumber, Node srcNode, Node[] receiverNodeIds, byte[] data, TransmitOptions txOptions)
        {
            BridgeController bridge = (BridgeController)DeviceManager.GetDevice(bridgeNumber);
            TransmitResult res = bridge.SendDataMultiBridge(srcNode, receiverNodeIds.Select(x => (NodeTag)x).ToArray(), data, (ZWave.Enums.TransmitOptions)txOptions);
            return new BasicActionResult.Transmit(res);
        }

        public BasicResult.IsFailedNode IsFailedNode(Alias controllerNumber, Node node)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            IsFailedNodeResult res = controller.IsFailedNode(node);
            return new BasicActionResult.IsFailedNode(res);
        }

        public BasicResult.IncludeNodeWithLearnMode ReplaceFailedNode(Alias controllerNumber, Node node, Alias nodeNumber)
        {
            if (DeviceManager.IncludeSettings.JoinDSK == null)
            {
                DeviceManager.IncludeSettings.JoinDSK = DeviceManager.GetDSK(nodeNumber, 2);
            }
            DeviceManager.DisableSmartStart(controllerNumber);

            BasicResult.IncludeNodeWithLearnMode ret = null;
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            DeviceManager.ReportTestFramesAndExtensionOverrides(controllerNumber);

            var replaceToken = controller.ReplaceFailedNode(node, (x) => DeviceManager.EnableSmartStart(controllerNumber), 180000);
            controller.WaitFailedNodeStatusSignal(ZWave.BasicApplication.Enums.FailedNodeStatuses.NodeReplace, 180000);

            Device device = DeviceManager.GetDevice(nodeNumber);
            DeviceManager.DisableSmartStart(nodeNumber);
            Thread.Sleep(500);
            var learnRes = device.SetLearnMode(ZWave.Enums.LearnModes.LearnModeClassic, false, 30000);
            DeviceManager.EnableSmartStart(nodeNumber);
            var inclusionRes = (InclusionResult)replaceToken.WaitCompletedSignal();
            ret = new BasicActionResult.IncludeNodeWithLearnMode(inclusionRes, learnRes);
            if (ret.AddNodeResult.SubstituteStatus != SubstituteStatuses.None)
            {
                DeviceManager.LogNetworkKeys(controllerNumber, inclusionRes.AddRemoveNode.Node);
            }
            DeviceManager.EnableSmartStart(controllerNumber);
            DeviceManager.EnableSmartStart(nodeNumber);
            DeviceManager.IncludeSettings.Clear();
            return ret;
        }

        public BasicResult.RemoveNode RemoveFailedNodeId(Alias controllerNumber, Node node)
        {
            var ret = new BasicResult.RemoveNode();
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            DeviceManager.DisableSmartStart(controllerNumber);
            ActionResult res = controller.RemoveFailedNodeId(node);
            DeviceManager.EnableSmartStart(controllerNumber);
            ret.Init(res, res.TraceLog);
            return ret;
        }

        public BasicResult.RequestData RequestData(Alias senderNumber, Node receiverNode, byte[] data, byte[] expectedData)
        {
            return RequestData(senderNumber, receiverNode, data, expectedData, REQUEST_TIMEOUT_MS);
        }

        public BasicResult.RequestData RequestData(Alias senderNumber, Node receiverNode, byte[] data, byte[] expectedData, int timeoutMs)
        {
            return RequestData(senderNumber, receiverNode, data, TX_OPTIONS, expectedData, timeoutMs);
        }

        public BasicResult.RequestData RequestData(Alias senderNumber, Node receiverNode, byte[] sendData, TransmitOptions txOptions, byte[] expectedData, int timeoutMs)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            var data = sendData;
            data = ApplySupervisionSettingsToEncapData(senderNumber, data);

            var substituteSettings = GetCommonSubstituteSettings(senderNumber);
            substituteSettings = ApplyMultiChannelSettingsToSubstSettings(senderNumber, substituteSettings);
            substituteSettings = ApplyCRC16SettingsToSubstSettings(senderNumber, substituteSettings);

            RequestDataResult res = sender.RequestData(receiverNode, data, (ZWave.Enums.TransmitOptions)txOptions, substituteSettings, expectedData, timeoutMs);
            return new BasicActionResult.RequestData(res);
        }

        public BasicResult.RequestData RequestDataEx(Alias senderNumber, Node receiverNode, byte[] data, SecuritySchemes scheme, byte[] expectedData)
        {
            return RequestDataEx(senderNumber, receiverNode, data, scheme, expectedData, REQUEST_TIMEOUT_MS);
        }

        public BasicResult.RequestData RequestDataEx(Alias senderNumber, Node receiverNode, byte[] data, SecuritySchemes scheme, byte[] expectedData, int timeoutMs)
        {
            return RequestDataEx(senderNumber, receiverNode, data, TX_OPTIONS, TX_SEC_OPTIONS, scheme, expectedData, timeoutMs);
        }

        public BasicResult.RequestData RequestDataEx(Alias senderNumber, Node receiverNode, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, byte[] expectedData, int timeoutMs)
        {
            return RequestDataEx(senderNumber, receiverNode, data, txOptions, txSecOptions, scheme, TransmitOptions2.NONE, expectedData, timeoutMs);
        }

        public BasicResult.RequestData RequestDataEx(Alias senderNumber, Node receiverNode, byte[] sendData, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2, byte[] expectedData, int timeoutMs)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);

            var data = sendData;
            data = ApplySupervisionSettingsToEncapData(senderNumber, data);

            var substituteSettings = GetCommonSubstituteSettings(senderNumber);
            substituteSettings = ApplyMultiChannelSettingsToSubstSettings(senderNumber, substituteSettings);
            substituteSettings = ApplyCRC16SettingsToSubstSettings(senderNumber, substituteSettings);

            RequestDataResult res = sender.RequestDataEx(receiverNode, data, (ZWave.Enums.TransmitOptions)txOptions, substituteSettings, (ZWave.Enums.TransmitSecurityOptions)txSecOptions, (ZWave.Enums.SecuritySchemes)scheme, (ZWave.Enums.TransmitOptions2)txOptions2, expectedData, timeoutMs);
            return new BasicActionResult.RequestData(res);
        }

        public BasicResult.RequestNodeInfo RequestNodeInfo(Alias deviceNumber, Node node)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            RequestNodeInfoResult res = device.RequestNodeInfo(node);
            return new BasicActionResult.RequestNodeInfo(res);
        }

        public BasicResult.GetProtocolInfo GetProtocolInfo(Alias deviceNumber, Node node)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(deviceNumber);
            GetNodeProtocolInfoResult res = controller.GetProtocolInfo(node);
            return new BasicResult.GetProtocolInfo(res);
        }

        public BasicResult.AssignReturnRoute AssignReturnRoute(Alias controllerNumber, Alias srcNode, Node destNode)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            Device srcDevice = DeviceManager.GetDevice(srcNode);
            AssignReturnRouteResult res = controller.AssignReturnRoute(srcDevice.Network.NodeTag, destNode, out ActionToken token);
            return new BasicResult.AssignReturnRoute(res);
        }

        public void SetSecurityForNode(Alias deviceNumber, Node node, bool isEnable)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            if (isEnable)
            {
                device.Network.SetSecuritySchemes(node, new[] { ZWave.Enums.SecuritySchemes.S0 });
            }
            else
            {
                device.Network.ResetSecuritySchemes(node);
            }
        }

        public void SetSecurityVersionForNode(Alias deviceNumber, Node node, SecurityVersions securityVersion)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            device.MemoryGetId();
            device.Network.ResetSecuritySchemes(node);
            if (securityVersion.HasFlag(SecurityVersions.S0))
            {
                device.Network.SetSecuritySchemes(node, new[] { ZWave.Enums.SecuritySchemes.S0 });
            }
            if (securityVersion.HasFlag(SecurityVersions.S2))
            {
                device.Network.SetSecuritySchemes(node, ZWave.Enums.SecuritySchemeSet.ALLS2);
            }
        }

        public void SetSecurityVersionForBridge(Alias deviceNumber, byte VirtualNodeId, Node node, SecurityVersions securityVersion)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            device.MemoryGetId();
            device.Network.ResetSecuritySchemes(node);
            if (securityVersion.HasFlag(SecurityVersions.S0))
            {
                device.Network.SetSecuritySchemes(node, new[] { ZWave.Enums.SecuritySchemes.S0 });
            }
            if (securityVersion.HasFlag(SecurityVersions.S2))
            {
                device.Network.SetSecuritySchemes(node, ZWave.Enums.SecuritySchemeSet.ALLS2);
            }
        }

        // TODO review: duplicate of the ApplicationService.SecuritySettings.SetActiveSecurityScheme
        public void SetActiveSecuritySchemeForNode(Alias deviceNumber, Node node, SecuritySchemes activeSecurityScheme)
        {
            SetActiveSecuritySchemeForNode(deviceNumber, node, activeSecurityScheme, false);
        }

        // TODO review: duplicate of the ApplicationService.SecuritySettings.SetActiveSecurityScheme
        public void SetActiveSecuritySchemeForNode(Alias deviceNumber, Node node, SecuritySchemes activeSecurityScheme, bool isLRScheme)
        {
            Device controller = DeviceManager.GetDevice(deviceNumber);
            var sm = controller.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                var supportedSchemes = controller.Network.GetSecuritySchemes(node);
                if (supportedSchemes == null)
                {
                    supportedSchemes = new[] { (ZWave.Enums.SecuritySchemes)activeSecurityScheme };
                }
                else
                {
                    supportedSchemes = supportedSchemes.Union(new[] { (ZWave.Enums.SecuritySchemes)activeSecurityScheme }).ToArray();
                }
                controller.Network.SetSecuritySchemes(node, supportedSchemes);
                controller.Network.SetCurrentSecurityScheme(node, (ZWave.Enums.SecuritySchemes)activeSecurityScheme);
                InvariantPeerNodeId peerNodeId = new InvariantPeerNodeId(controller.Network.NodeTag, node);
                SecurityManager securityManager = (SecurityManager)sm;
                if (activeSecurityScheme == SecuritySchemes.S2_ACCESS ||
                    activeSecurityScheme == SecuritySchemes.S2_AUTHENTICATED ||
                    activeSecurityScheme == SecuritySchemes.S2_UNAUTHENTICATED)
                {
                    securityManager.SecurityManagerInfo.ActivateNetworkKeyS2ForNode(peerNodeId, (ZWave.Enums.SecuritySchemes)activeSecurityScheme, isLRScheme);
                }
                else if (activeSecurityScheme == SecuritySchemes.S2_TEMP)
                {
                    securityManager.SecurityManagerInfo.ActivateNetworkKeyS2TempForNode(peerNodeId);
                }
            }
        }

        public byte FirmwareUpdateV5(Alias deviceNumber, Node node, string filePath)
        {
            return FirmwareUpdateV5(deviceNumber, node, filePath, 0x00);
        }

        public byte FirmwareUpdateV5(Alias deviceNumber, Node node, string filePath, byte activation)
        {
            return FirmwareUpdateV5(deviceNumber, node, filePath, 0, activation);
        }

        public byte FirmwareUpdateV5(Alias deviceNumber, Node node, string filePath, ushort fragmentSize, byte activation)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(deviceNumber);
            byte ret = 0x00;
            AutoResetEvent completedFirmwareUpdateSignal = new AutoResetEvent(false);
            var statusToken = controller.ResponseData((a, b, c, d) =>
            {
                completedFirmwareUpdateSignal.Set();
                var repData = (COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_REPORT)d;
                ret = repData.status;
                return null;
            },
                (ZWave.Enums.TransmitOptions)TX_OPTIONS, new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_STATUS_REPORT());
            if (File.Exists(filePath))
            {
                byte[] firmwareData = null;
                try
                {
                    firmwareData = HexFileHelper.GetBytes(HexFileHelper.ReadIntelHexFile(filePath, 0xFF), 0xFF);
                }
                catch (FileFormatException)
                {
                }
                if (firmwareData == null)
                {
                    firmwareData = File.ReadAllBytes(filePath);
                }
                var firmwareChecksum = Tools.CalculateCrc16Array(firmwareData);
                byte[] frSize = new byte[] { (byte)(fragmentSize >> 8), (byte)fragmentSize };// default 40

                var requestRes = RequestData(deviceNumber, node,
                    new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_GET(),
                    new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_REPORT());
                if (requestRes.Command != null)
                {
                    var fwReport = (COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_REPORT)requestRes.Command;
                    if (fragmentSize == 0 || fragmentSize > Tools.GetInt32(fwReport.maxFragmentSize))
                    {
                        frSize = fwReport.maxFragmentSize;
                    }
                    List<byte[]> fwData = Tools.ArraySplit(firmwareData, Tools.GetInt32(frSize));
                    Log($"Number of FW fragments: {fwData.Count}, splitted by size: {Tools.GetInt32(frSize)}", LogLevels.Information);
                    aToken = null;
                    ret = InnerFirmwareUpdateV5(controller, node, fwData, firmwareChecksum, fwReport.firmware0Id, fwReport.manufacturerId, 0x00, frSize, activation, fwReport.hardwareVersion, out aToken);
                    if (ret == 0xFF)
                    {
                        completedFirmwareUpdateSignal.Reset();
                        completedFirmwareUpdateSignal.WaitOne(60*60*1000); // 60 min timeout
                    }
                    controller.Cancel(aToken);
                }
            }
            else
            {
                Log("File with firmware was not found", LogLevels.Error);
            }
            controller.Cancel(statusToken);
            return ret;
        }

        public byte FirmwareUpdateV2(Alias deviceNumber, Node node, string filePath)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(deviceNumber);
            byte ret = 0x00;
            AutoResetEvent completedFirmwareUpdateSignal = new AutoResetEvent(false);
            byte[] firmwareData, firmwareChecksum, firmwareId, manufacturerID;
            controller.ResponseData((a, b, c, d) =>
            {
                completedFirmwareUpdateSignal.Set();
                var repData = (COMMAND_CLASS_FIRMWARE_UPDATE_MD_V2.FIRMWARE_UPDATE_MD_REQUEST_REPORT)d;
                ret = repData.status;
                return null;
            },
                (ZWave.Enums.TransmitOptions)TX_OPTIONS, new COMMAND_CLASS_FIRMWARE_UPDATE_MD.FIRMWARE_UPDATE_MD_STATUS_REPORT());
            if (File.Exists(filePath))
            {
                firmwareData = HexFileHelper.GetBytes(HexFileHelper.ReadIntelHexFile(filePath, 0xFF), 0xFF);
                firmwareChecksum = Utils.Tools.CalculateCrc16Array(firmwareData);

                byte[] fragmentSize = new byte[] { 0x00, 0x28 };// default 40
                List<byte[]> fwData = Tools.ArraySplit(firmwareData, Tools.GetInt32(fragmentSize));

                var requestRes = RequestData(deviceNumber, node,
                    new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V2.FIRMWARE_MD_GET(),
                    new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V2.FIRMWARE_MD_REPORT());
                if (requestRes.Command != null)
                {
                    var fwReport = (COMMAND_CLASS_FIRMWARE_UPDATE_MD_V2.FIRMWARE_MD_REPORT)requestRes.Command;
                    firmwareId = fwReport.firmwareId;
                    manufacturerID = fwReport.manufacturerId;

                    ret = InnerFirmwareUpdateV2(controller, node, fwData, firmwareChecksum, firmwareId, manufacturerID, out aToken);
                    if (ret == 0xFF)
                    {
                        completedFirmwareUpdateSignal.Reset();
                        completedFirmwareUpdateSignal.WaitOne();
                    }
                    controller.Cancel(aToken);
                }
            }
            else
            {
                Log("File with firmware was not found", LogLevels.Error);
            }
            return ret;
        }
        ActionToken aToken = null;


        public byte FirmwareUpdateV3(Alias deviceNumber, Node node, string filePath)
        {
            return FirmwareUpdateV3(deviceNumber, node, filePath, 40);
        }
        public byte FirmwareUpdateV3(Alias deviceNumber, Node node, string filePath, ushort fragmentSize)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(deviceNumber);
            byte ret = 0x00;
            AutoResetEvent completedFirmwareUpdateSignal = new AutoResetEvent(false);
            byte[] firmwareData, firmwareChecksum, firmwareId, manufacturerID;
            controller.ResponseData((a, b, c, d) =>
            {
                completedFirmwareUpdateSignal.Set();
                var repData = (COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_REQUEST_REPORT)d;
                ret = repData.status;
                return null;
            },
                (ZWave.Enums.TransmitOptions)TX_OPTIONS, new COMMAND_CLASS_FIRMWARE_UPDATE_MD.FIRMWARE_UPDATE_MD_STATUS_REPORT());
            if (File.Exists(filePath))
            {
                firmwareData = HexFileHelper.GetBytes(HexFileHelper.ReadIntelHexFile(filePath, 0xFF), 0xFF);
                firmwareChecksum = Utils.Tools.CalculateCrc16Array(firmwareData);
                byte[] frSize = new byte[] { (byte)(fragmentSize >> 8), (byte)fragmentSize };// default 40


                var requestRes = RequestData(deviceNumber, node,
                    new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_MD_GET(),
                    new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_MD_REPORT());
                if (requestRes.Command != null)
                {
                    var fwReport = (COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_MD_REPORT)requestRes.Command;
                    firmwareId = fwReport.firmware0Id;
                    manufacturerID = fwReport.manufacturerId;
                    if (fragmentSize == 0 || fragmentSize > Tools.GetInt32(fwReport.maxFragmentSize))
                    {
                        frSize = fwReport.maxFragmentSize;
                    }
                    List<byte[]> fwData = Tools.ArraySplit(firmwareData, Tools.GetInt32(frSize));

                    //TODO:
                    //Index should be chosen from available. Now hard coded.
                    ret = InnerFirmwareUpdateV3(controller, node, fwData, firmwareChecksum, firmwareId, manufacturerID, 0, frSize, out aToken);
                    if (ret == 0xFF)
                    {
                        completedFirmwareUpdateSignal.Reset();
                        completedFirmwareUpdateSignal.WaitOne();
                    }
                    controller.Cancel(aToken);
                }
            }
            else
            {
                Log("File with firmware was not found", LogLevels.Error);
            }
            return ret;
        }

        public byte ActivteFirmwareUpdate(Alias deviceNumber, Node node, string filePath)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(deviceNumber);
            byte ret = 0x00;
            var fwuActivationSetData = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_ACTIVATION_SET();

            if (File.Exists(filePath))
            {
                byte[] firmwareData = null;
                try
                {
                    firmwareData = HexFileHelper.GetBytes(HexFileHelper.ReadIntelHexFile(filePath, 0xFF), 0xFF);
                }
                catch (FileFormatException)
                {
                }
                if (firmwareData == null)
                {
                    firmwareData = File.ReadAllBytes(filePath);
                }
                var firmwareChecksum = Tools.CalculateCrc16Array(firmwareData);

                var requestRes = RequestData(deviceNumber, node,
                    new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_GET(),
                    new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_REPORT());
                if (requestRes.Command != null)
                {
                    var fwReport = (COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_MD_REPORT)requestRes.Command;

                    fwuActivationSetData.manufacturerId = fwReport.manufacturerId;
                    fwuActivationSetData.firmwareId = fwReport.firmware0Id;
                    fwuActivationSetData.checksum = firmwareChecksum;
                    fwuActivationSetData.firmwareTarget = 0x00;
                    fwuActivationSetData.hardwareVersion = fwReport.hardwareVersion;
                }
                var fwuActivationReportData = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_ACTIVATION_STATUS_REPORT();

                //Start Activate
                var reqRes = RequestData(deviceNumber, node, fwuActivationSetData, fwuActivationReportData);
                if (reqRes && reqRes.Command != null)
                {
                    fwuActivationReportData = reqRes.Command;
                    ret = fwuActivationReportData.firmwareUpdateStatus;
                }
                else
                {
                    Log("FW Activation Report not received", LogLevels.Error);
                }
            }
            else
            {
                Log("File with firmware was not found", LogLevels.Error);
            }

            return ret;
        }

        private byte MDRequestGet(Node node, byte[] fwuMDRequestGetData, byte[] fwuMDRequestReportData, Controller controller)
        {
            byte ret = 0;
            RequestDataResult res = controller.RequestData(
                    node,
                    fwuMDRequestGetData,
                    (ZWave.Enums.TransmitOptions)TX_OPTIONS,
                    fwuMDRequestReportData, 3000);

            if (res == true && res.Command != null)
            {
                var repData = (COMMAND_CLASS_FIRMWARE_UPDATE_MD_V2.FIRMWARE_UPDATE_MD_REQUEST_REPORT)res.Command;
                ret = repData.status;
            }
            return ret;
        }

        private byte InnerFirmwareUpdateV2(Controller controller, Node node, List<byte[]> fwData, byte[] fwChecksum, byte[] fwId, byte[] mfId, out ActionToken token)
        {
            COMMAND_CLASS_FIRMWARE_UPDATE_MD_V2.FIRMWARE_UPDATE_MD_REQUEST_GET fwuMDRequestGetData = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V2.FIRMWARE_UPDATE_MD_REQUEST_GET();
            fwuMDRequestGetData.checksum = fwChecksum;
            fwuMDRequestGetData.firmwareId = fwId;
            fwuMDRequestGetData.manufacturerId = mfId;
            COMMAND_CLASS_FIRMWARE_UPDATE_MD_V2.FIRMWARE_UPDATE_MD_REQUEST_REPORT fwuMDRequestReportData = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V2.FIRMWARE_UPDATE_MD_REQUEST_REPORT();
            //Register ResponseData
            COMMAND_CLASS_FIRMWARE_UPDATE_MD.FIRMWARE_UPDATE_MD_GET fwuMDGetData = new COMMAND_CLASS_FIRMWARE_UPDATE_MD.FIRMWARE_UPDATE_MD_GET(); //response from the node

            token = controller.ResponseMultiData((a, b, c, x) =>
            {
                return FwuResponseDataHandler(fwData, x, out fwuMDGetData);
            }, (ZWave.Enums.TransmitOptions)TX_OPTIONS, fwuMDGetData);

            //Start Update
            return MDRequestGet(node, fwuMDRequestGetData, fwuMDRequestReportData, controller);
        }

        private byte InnerFirmwareUpdateV3(Controller controller, Node node, List<byte[]> fwData, byte[] fwChecksum, byte[] fwId, byte[] mfId, byte fwTarget, byte[] fragmentSize, out ActionToken token)
        {
            COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_REQUEST_GET fwuMDRequestGetData = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_REQUEST_GET();
            fwuMDRequestGetData.checksum = fwChecksum;
            fwuMDRequestGetData.firmwareId = fwId;
            fwuMDRequestGetData.manufacturerId = mfId;
            fwuMDRequestGetData.firmwareTarget = fwTarget;
            fwuMDRequestGetData.fragmentSize = fragmentSize;

            COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_REQUEST_REPORT fwuMDRequestReportData = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V3.FIRMWARE_UPDATE_MD_REQUEST_REPORT();
            //Register ResponseData
            COMMAND_CLASS_FIRMWARE_UPDATE_MD.FIRMWARE_UPDATE_MD_GET fwuMDGetData = new COMMAND_CLASS_FIRMWARE_UPDATE_MD.FIRMWARE_UPDATE_MD_GET(); //response from the node

            token = controller.ResponseMultiData((a, b, c, x) =>
            {
                return FwuResponseDataHandler(fwData, x, out fwuMDGetData);
            }, (ZWave.Enums.TransmitOptions)TX_OPTIONS, fwuMDGetData);

            //Start Update
            return MDRequestGet(node, fwuMDRequestGetData, fwuMDRequestReportData, controller);
        }

        private byte InnerFirmwareUpdateV5(Controller controller, Node node, List<byte[]> fwData, byte[] fwChecksum, byte[] fwId, byte[] mfId, byte fwTarget, byte[] fragmentSize, byte activation, byte hardwareVersion, out ActionToken token)
        {
            COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_GET fwuMDRequestGetData = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_GET();
            fwuMDRequestGetData.checksum = fwChecksum;
            fwuMDRequestGetData.firmwareId = fwId;
            fwuMDRequestGetData.manufacturerId = mfId;
            fwuMDRequestGetData.firmwareTarget = fwTarget;
            fwuMDRequestGetData.fragmentSize = fragmentSize;
            fwuMDRequestGetData.properties1 = activation;
            fwuMDRequestGetData.hardwareVersion = hardwareVersion;

            COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_REPORT fwuMDRequestReportData = new COMMAND_CLASS_FIRMWARE_UPDATE_MD_V5.FIRMWARE_UPDATE_MD_REQUEST_REPORT();
            //Register ResponseData
            COMMAND_CLASS_FIRMWARE_UPDATE_MD.FIRMWARE_UPDATE_MD_GET fwuMDGetData = new COMMAND_CLASS_FIRMWARE_UPDATE_MD.FIRMWARE_UPDATE_MD_GET(); //response from the node

            token = controller.ResponseMultiDataEx((a, b, c, x) =>
            {
                return FwuResponseDataHandler(fwData, x, out fwuMDGetData);
            }, (ZWave.Enums.TransmitOptions)TX_OPTIONS, true, fwuMDGetData);

            //Start Update
            return MDRequestGet(node, fwuMDRequestGetData, fwuMDRequestReportData, controller);
        }

        private List<byte[]> FwuResponseDataHandler(List<byte[]> fwData, byte[] x, out COMMAND_CLASS_FIRMWARE_UPDATE_MD.FIRMWARE_UPDATE_MD_GET fwuMDGetData)
        {
            fwuMDGetData = x;
            List<byte[]> fwuMDReportData = new List<byte[]>();
            short firmwareDataIndex = 0;
            for (byte i = 0; i < fwuMDGetData.numberOfReports; i++)
            {
                firmwareDataIndex = (short)(((fwuMDGetData.properties1.reportNumber1 << 8) + fwuMDGetData.reportNumber2) + i);
                if (firmwareDataIndex > 0 && fwData.Count > (firmwareDataIndex - 1))
                {
                    List<byte> dataToSend = new List<byte>(fwData[firmwareDataIndex - 1]);
                    dataToSend.Insert(0, COMMAND_CLASS_FIRMWARE_UPDATE_MD.ID);
                    dataToSend.Insert(1, COMMAND_CLASS_FIRMWARE_UPDATE_MD.FIRMWARE_UPDATE_MD_REPORT.ID);
                    if (fwData.Count <= firmwareDataIndex)
                    {
                        dataToSend.Insert(2, (byte)((firmwareDataIndex >> 8) | 0x80));
                    }
                    else
                    {
                        dataToSend.Insert(2, (byte)(firmwareDataIndex >> 8));
                    }

                    dataToSend.Insert(3, (byte)firmwareDataIndex);

                    ushort crc = Utils.Tools.CalculateCrc16(dataToSend);

                    dataToSend.Add((byte)(crc >> 8));
                    dataToSend.Add((byte)crc);

                    fwuMDReportData.Add(dataToSend.ToArray());
                }
            }
            return fwuMDReportData;
        }

        public BasicResult.SetPriorityRoute SetPriorityRoute(Alias controllerNumber, Node nodeNumber, Node priority0, Node priority1, Node priority2, Node priority3, byte routespeed)
        {
            Controller device = (Controller)DeviceManager.GetDevice(controllerNumber);
            SetPriorityRouteResult res = device.SetPriorityRoute(nodeNumber, priority0, priority1, priority2, priority3, routespeed);
            return new BasicResult.SetPriorityRoute(res);
        }

        public BasicResult.Transmit SendNop(Alias senderNumber, Node node, TransmitOptions txOptions)
        {
            Device sender = DeviceManager.GetDevice(senderNumber);
            TransmitResult res = sender.SendData(node, new byte[] { 0 }, (ZWave.Enums.TransmitOptions)txOptions);
            return new BasicActionResult.Transmit(res);
        }

        public BasicResult.Void SetRoutingInfo(Alias controllerNumber, Node node, byte[] nodeMask)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(controllerNumber);
            ActionResult res = controller.SetRoutingInfo(node, nodeMask);
            return new BasicActionResult.Void(res);
        }

        public BasicResult.Transmit SendTestFrame(Alias deviceNumber, Node nodeId, byte powerLevel)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            TransmitResult res = device.SendTestFrame(nodeId, powerLevel);
            return new BasicActionResult.Transmit(res);
        }

        public BasicResult.RequestNodeNeighborUpdate RequestNodeNeighborDiscovery(Alias deviceNumber, Node node, byte nodeType)
        {
            Controller controller = (Controller)DeviceManager.GetDevice(deviceNumber);
            RequestNodeNeighborUpdateResult res = controller.RequestNodeNeighborDiscovery(node, nodeType, 180000);
            return new BasicActionResult.RequestNodeNeighborUpdate(res);
        }

        #endregion
    }
}
