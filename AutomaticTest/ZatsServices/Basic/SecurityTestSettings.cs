// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Devices;
using ZWave.Layers;
using ZWave.Security;
using System.Linq;
using System.Collections.Generic;
using ZWave.Devices;

namespace Zats.Services
{
    public class SecurityTestSettings : TestSettingsBase, ISecurityTestSettings
    {
        public IIncludeSettings IncludeSettings
        {
            get
            {
                return DeviceManager.IncludeSettings;
            }

            set
            {
                DeviceManager.IncludeSettings = value;
            }
        }

        public SecurityTestSettings(DeviceManager deviceManager)
            : base(deviceManager)
        {

        }

        public void SetSecurityVersion(Alias deviceNumber, SecurityVersions securityVersions)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            device.MemoryGetId();
            var sm = (SecurityManager)(device.SessionClient.GetSubstituteManager(typeof(SecurityManager)));
            device.Network.ResetSecuritySchemes();

            if (securityVersions.HasFlag(SecurityVersions.S0) && securityVersions.HasFlag(SecurityVersions.S2))
            {
                device.Network.SetSecuritySchemes(ZWave.Enums.SecuritySchemeSet.ALL);
            }
            else if (securityVersions.HasFlag(SecurityVersions.S0))
            {
                device.Network.SetSecuritySchemes(new[] { ZWave.Enums.SecuritySchemes.S0 });
            }
            else if (securityVersions.HasFlag(SecurityVersions.S2))
            {
                device.Network.SetSecuritySchemes(ZWave.Enums.SecuritySchemeSet.ALLS2);
            }
        }

        #region S2

        public byte[] GetNetworkKeyS2Temp(Alias deviceNumber)
        {
            byte[] ret = new byte[0];
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                ret = securityManager.SecurityManagerInfo.GetActualNetworkKeyS2Temp();
            }
            return ret;
        }

        public byte[] GetNetworkKeyS2(Alias deviceNumber, SecuritySchemes scheme)
        {
            return GetNetworkKeyS2(deviceNumber, scheme, false);
        }

        public byte[] GetNetworkKeyS2(Alias deviceNumber, SecuritySchemes scheme, bool isLRScheme)
        {
            byte[] ret = new byte[0];
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                ret = securityManager.SecurityManagerInfo.GetActualNetworkKey((ZWave.Enums.SecuritySchemes)scheme, isLRScheme);
            }
            return ret;
        }

        public byte[] GetControllerPublicKey(Alias deviceNumber)
        {
            byte[] ret = new byte[0];
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                ret = securityManager.SecurityManagerInfo.GetPublicKeyS2();
            }
            return ret;
        }

        public void SetNetworkKeyS2Temp(Alias deviceNumber, byte[] networkKey)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetNetworkKeyS2Temp(networkKey);
                securityManager.SecurityManagerInfo.ActivateNetworkKeyS2TempForNode(new InvariantPeerNodeId());
            }
        }

        public static byte GetNetworkKeysMask(IEnumerable<SecuritySchemes> requestSchemes)
        {
            return (byte)ZWave.BasicApplication.Security.SecurityManagerInfo.ConvertToNetworkKeyMask(requestSchemes
                .Select(x => (ZWave.Enums.SecuritySchemes)x).ToArray());
        }

        public void SetNetworkKeyS2(Alias deviceNumber, byte[] networkKey, SecuritySchemes activeNetworkKey)
        {
            SetNetworkKeyS2(deviceNumber, networkKey, activeNetworkKey, false);
        }

        public void SetNetworkKeyS2(Alias deviceNumber, byte[] networkKey, SecuritySchemes activeNetworkKey, bool isLRScheme)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestNetworkKey(networkKey, (ZWave.Enums.SecuritySchemes)activeNetworkKey, isLRScheme);
                if (activeNetworkKey == SecuritySchemes.S0)
                {
                    securityManager.SecurityManagerInfo.ActivateNetworkKeyS0();
                }
                else
                {
                    securityManager.SecurityManagerInfo.ActivateNetworkKeyS2ForNode(new InvariantPeerNodeId(), (ZWave.Enums.SecuritySchemes)activeNetworkKey, isLRScheme);
                }
            }
        }

        /// <summary>
        /// Include the network keys from a dictionary
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <param name="scheme"></param>
        public void SetNetworkKeys(Alias deviceNumber, Dictionary<SecuritySchemes, byte[]> keys)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            foreach (var keyEntry in keys)
            {
                if (sm != null && keyEntry.Key != SecuritySchemes.S2_TEMP)
                {
                    SecurityManager securityManager = (SecurityManager)sm;
                    securityManager.SecurityManagerInfo.SetTestNetworkKey(keyEntry.Value,
                        (ZWave.Enums.SecuritySchemes)keyEntry.Key, false);
                    if (keyEntry.Key == SecuritySchemes.S0)
                    {
                        securityManager.SecurityManagerInfo.ActivateNetworkKeyS0();
                    }
                    else
                    {
                        securityManager.SecurityManagerInfo.ActivateNetworkKeyS2ForNode(
                            new InvariantPeerNodeId(), (ZWave.Enums.SecuritySchemes)keyEntry.Key, false);
                    }
                }
            }
        }

        public void SetActiveNetworkKeyS2Temp(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.ActivateNetworkKeyS2TempForNode(new InvariantPeerNodeId());
            }
        }

        public void SetActiveNetworkKeyS2(Alias deviceNumber, SecuritySchemes scheme)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.ActivateNetworkKeyS2ForNode(new InvariantPeerNodeId(new NodeTag(0), new NodeTag(0xFF)), (ZWave.Enums.SecuritySchemes)scheme, false);
            }
        }

        public void SetActiveSecurityScheme(Alias controllerNumber, Alias deviceNumber, SecuritySchemes scheme)
        {
            SetActiveSecurityScheme(controllerNumber, deviceNumber, scheme, false);
        }

        public void SetActiveSecurityScheme(Alias controllerNumber, Alias deviceNumber, SecuritySchemes scheme, bool isLRScheme)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            SetActiveSecurityScheme(controllerNumber, device.Network.NodeTag, scheme, isLRScheme);
        }

        public void SetActiveSecurityScheme(Alias controllerNumber, Node node, SecuritySchemes scheme)
        {
            Device controller = DeviceManager.GetDevice(controllerNumber);
            bool isLRScheme = controller.Network.IsLongRangeEnabled(node) && controller.Network.IsLongRange(node);
            SetActiveSecurityScheme(controllerNumber, node, scheme, isLRScheme);
        }

        public void SetActiveSecurityScheme(Alias controllerNumber, Node node, SecuritySchemes scheme, bool isLRScheme)
        {
            Device controller = DeviceManager.GetDevice(controllerNumber);
            ISubstituteManager sm = controller.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                var supportedSchemes = controller.Network.GetSecuritySchemes(node);
                if (supportedSchemes == null)
                {
                    supportedSchemes = new[] { (ZWave.Enums.SecuritySchemes)scheme };
                }
                else
                {
                    supportedSchemes = supportedSchemes.Union(new[] { (ZWave.Enums.SecuritySchemes)scheme }).ToArray();
                }
                controller.Network.SetSecuritySchemes(node, supportedSchemes);
                controller.Network.SetCurrentSecurityScheme(node, (ZWave.Enums.SecuritySchemes)scheme);
                InvariantPeerNodeId peerNodeId = new InvariantPeerNodeId(controller.Network.NodeTag, node);
                SecurityManager securityManager = (SecurityManager)sm;
                if (scheme == SecuritySchemes.S2_ACCESS ||
                    scheme == SecuritySchemes.S2_AUTHENTICATED ||
                    scheme == SecuritySchemes.S2_UNAUTHENTICATED)
                {
                    securityManager.SecurityManagerInfo.ActivateNetworkKeyS2ForNode(peerNodeId, (ZWave.Enums.SecuritySchemes)scheme, isLRScheme);
                }
                else if (scheme == SecuritySchemes.S2_TEMP)
                {
                    securityManager.SecurityManagerInfo.ActivateNetworkKeyS2TempForNode(peerNodeId);
                }
            }
        }

        public void SetSpanS2(Alias deviceNumber, byte[] span)
        {
            SetSpanS2(deviceNumber, span, 0);
        }

        public void SetSpanS2(Alias deviceNumber, byte[] span, int numOfTestSpanUsage)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestSpanS2(span, numOfTestSpanUsage);
            }
        }

        public byte[] GetSpanContainerCtx(Alias deviceNumber, Node node)
        {
            byte[] ret = null;
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                ret = securityManager.SecurityManagerInfo.SpanTable.GetContainer(new InvariantPeerNodeId(device.Network.NodeTag, node)).Context.GetBytes();
            }
            return ret;
        }

        public void SetSpanContainerCtx(Alias deviceNumber, Node node, byte[] ctx, byte txSequenceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                var container = securityManager.SecurityManagerInfo.SpanTable.GetContainer(new InvariantPeerNodeId(device.Network.NodeTag, node));
                container.Context = new CTR_DRBG_CTX(ctx);
                container.TxSequenceNumber = txSequenceNumber;
            }
        }

        public void SetReceiverEntropyS2(Alias deviceNumber, byte[] receiverEntropy)
        {
            SetReceiverEntropyS2(deviceNumber, receiverEntropy, 0);
        }

        public void SetReceiverEntropyS2(Alias deviceNumber, byte[] receiverEntropy, int numOfTestReceiverEntropyUsage)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestReceiverEntropyS2(receiverEntropy, numOfTestReceiverEntropyUsage);
            }
        }

        public void SetSenderEntropyInputS2(Alias deviceNumber, byte[] senderEntropyInput)
        {
            SetSenderEntropyInputS2(deviceNumber, senderEntropyInput, 0);
        }

        public void SetSenderEntropyInputS2(Alias deviceNumber, byte[] senderEntropyInput, int numOfSenderEntropyInputUsage)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestSenderEntropyInputS2(senderEntropyInput, numOfSenderEntropyInputUsage); ;
            }
        }

        public void SetInclusionFrameCommandS2(Alias deviceNumber, SecurityS2TestFrames testFrame, byte[] data)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestFrameCommandS2(
                    (ZWave.BasicApplication.Security.SecurityS2TestFrames)((byte)testFrame), data);
            }
        }

        public void SetInclusionFrameDelayS2(Alias deviceNumber, SecurityS2TestFrames testFrame, int delay)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestFrameDelayS2(
                    (ZWave.BasicApplication.Security.SecurityS2TestFrames)((byte)testFrame), delay);
            }
        }

        public void SetInclusionFrameEncryptedS2(Alias deviceNumber, SecurityS2TestFrames testFrame, bool? isEncrypted)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestFrameEncryptedS2(
                    (ZWave.BasicApplication.Security.SecurityS2TestFrames)((byte)testFrame), isEncrypted);
            }
        }

        public void SetInclusionFrameMulticastS2(Alias deviceNumber, SecurityS2TestFrames testFrame, bool? isMulticast)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestFrameMulticastS2(
                    (ZWave.BasicApplication.Security.SecurityS2TestFrames)((byte)testFrame), isMulticast);
            }
        }

        public void SetInclusionFrameBroadcastS2(Alias deviceNumber, SecurityS2TestFrames testFrame, bool? isBroadcast)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestFrameBroadcastS2(
                    (ZWave.BasicApplication.Security.SecurityS2TestFrames)((byte)testFrame), isBroadcast);
            }
        }

        public void SetInclusionFrameNetworkKeyS2(Alias deviceNumber, SecurityS2TestFrames testFrame, byte[] networkKey)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestFrameNetworkKeyS2(
                    (ZWave.BasicApplication.Security.SecurityS2TestFrames)((byte)testFrame), networkKey, false);
            }
        }

        public void SetInclusionFrameNetworkKeyS2Temp(Alias deviceNumber, SecurityS2TestFrames testFrame)
        {
            SetInclusionFrameNetworkKeyS2Temp(deviceNumber, testFrame, null);
        }

        public void SetInclusionFrameNetworkKeyS2Temp(Alias deviceNumber, SecurityS2TestFrames testFrame, byte[] networkKey)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestFrameNetworkKeyS2(
                    (ZWave.BasicApplication.Security.SecurityS2TestFrames)((byte)testFrame), networkKey, true);
            }
        }

        public void ClearInclusionFramesS2(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.ClearTestFramesS2();
            }
        }

        public byte[] GetSpanS2ForNode(Alias deviceNumber, Node node)
        {
            byte[] ret = null;
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                InvariantPeerNodeId peerNodeId = new InvariantPeerNodeId(device.Network.NodeTag, node);
                var currentNonce = securityManager.SecurityManagerInfo.SpanTable.GetContainer(peerNodeId);
                if (currentNonce != null)
                {
                    if (currentNonce.SpanState == SpanStates.Span)
                    {
                        ret = new byte[currentNonce.Span.Length];
                        Array.Copy(currentNonce.Span, ret, currentNonce.Span.Length);
                    }
                }
            }
            return ret;
        }

        public byte[] GetMpanS2ForGrpId(Alias deviceNumber, byte[] nodeIds)
        {
            byte[] ret = null;
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                byte GroupId = securityManager.SecurityManagerInfo.MpanTable.FindGroup(nodeIds.Select(x => new ZWave.Devices.NodeTag(x)).ToArray());
                if (GroupId > 0)
                {
                    var currentMPAN = securityManager.SecurityManagerInfo.MpanTable.GetContainer(new NodeGroupId(device.Network.NodeTag, GroupId));
                    if (currentMPAN != null)
                    {
                        ret = new byte[currentMPAN.MpanState.Length];
                        Array.Copy(currentMPAN.MpanState, ret, currentMPAN.MpanState.Length);
                    }
                }
            }
            return ret;
        }

        public void NextSpanS2ForNode(Alias deviceNumber, Node node, int nextNonceCount)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                InvariantPeerNodeId peerNodeId = new InvariantPeerNodeId(device.Network.NodeTag, node);
                var spanContainer = securityManager.SecurityManagerInfo.SpanTable.GetContainer(peerNodeId);
                if (spanContainer != null)
                {
                    for (int i = 0; i < nextNonceCount; i++)
                    {
                        spanContainer.NextNonce();
                    }
                }
            }
        }

        public void FreeSpanS2ForNode(Alias deviceNumber, Node node)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                InvariantPeerNodeId peerNodeId = new InvariantPeerNodeId(device.Network.NodeTag, node);
                var currentNonce = securityManager.SecurityManagerInfo.SpanTable.GetContainer(peerNodeId);
                if (currentNonce != null)
                {
                    securityManager.SecurityManagerInfo.SpanTable.SetNonceFree(peerNodeId);
                }
            }
        }

        public void SetS2MessageEncapsulationExtension(Alias deviceNumber, SecurityS2ExtensionTypes extensionType, SecurityS2MessageTypes messageType,
            bool? isEncrypted, byte[] value, int? numOfUsage, bool? isMoreToFollow, bool? isCritical, byte? extensionLength, bool isOverride)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                if (isOverride)
                {
                    var msgTypes = new[] { SecurityS2MessageTypes.MulticastAll, SecurityS2MessageTypes.SinglecastAll };
                    var extTypes = new[]
                    {
                        SecurityS2ExtensionTypes.Mos,
                        SecurityS2ExtensionTypes.Mpan,
                        SecurityS2ExtensionTypes.MpanGrp,
                        SecurityS2ExtensionTypes.Span
                    };
                    foreach (var msgType in msgTypes)
                    {
                        foreach (var extType in extTypes)
                        {
                            securityManager.SecurityManagerInfo.AddTestExtensionS2(new TestExtensionS2Settings(msgType, extType, SecurityS2TestExtensionsActions.Delete).Settings);
                        }
                    }
                }
                var action = SecurityS2TestExtensionsActions.Add;
                securityManager.SecurityManagerInfo.AddTestExtensionS2(
                    new TestExtensionS2Settings(messageType, extensionType, action)
                    {
                        IsEncrypted = isEncrypted,
                        Value = value,
                        NumOfUsage = numOfUsage,
                        IsMoreToFollow = isMoreToFollow,
                        IsCritical = isCritical,
                        ExtensionLength = extensionLength
                    }.Settings);
            }
        }

        public void SetS2MessageEncapsulationExtension(Alias deviceNumber, TestExtensionS2Settings testExtensionSettings)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.AddTestExtensionS2(testExtensionSettings.Settings);
            }
        }

        public void ClearTestExtensionsS2(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.ClearTestExtensionsS2();
            }
        }

        public void SetS2ParametersOverride(Alias deviceNumber, SecurityS2ParameterTypes securityS2ParameterType, byte[] value)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                switch (securityS2ParameterType)
                {
                    case SecurityS2ParameterTypes.ReservedField:
                        securityManager.SecurityManagerInfo.SetTestReservedFieldS2(value[0]);
                        break;
                    case SecurityS2ParameterTypes.SecretKey:
                        securityManager.SecurityManagerInfo.SetTestSecretKeyS2(value);
                        break;
                    case SecurityS2ParameterTypes.Sender_EI:
                        securityManager.SecurityManagerInfo.SetTestSenderEntropyInputS2(value);
                        break;
                    case SecurityS2ParameterTypes.SequenceNo:
                        securityManager.SecurityManagerInfo.SetTestSequenceNumberS2(value[0]);
                        break;
                    case SecurityS2ParameterTypes.Span:
                        securityManager.SecurityManagerInfo.SetTestSpanS2(value);
                        break;
                    default:
                        //Add structure TestParametersS2 as TestExtensionsS2
                        break;
                }
            }
        }

        public void ClearS2ParametersOverride(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestReservedFieldS2(null);
                securityManager.SecurityManagerInfo.SetTestSecretKeyS2(null);
                securityManager.SecurityManagerInfo.SetTestSenderEntropyInputS2(null);
                securityManager.SecurityManagerInfo.SetTestSequenceNumberS2(null);
                securityManager.SecurityManagerInfo.SetTestSpanS2(null);
            }
        }

        public void ClearMPANTable(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.MpanTable.ClearMpanTable();
            }
        }

        public void RemoveMPANRecord(Alias deviceNumber, byte groupId, Node owner)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.MpanTable.RemoveRecord(new NodeGroupId(owner, groupId));
            }
        }

        public void AddOrUpdateMPANState(Alias deviceNumber, byte groupId, Node owner, bool? isMOS, byte? seqNo, byte[] mpanState, Node[] nodes)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                MpanContainer container = null;
                var nodeGroupId = new NodeGroupId(owner, groupId);

                if (!securityManager.SecurityManagerInfo.MpanTable.CheckMpanExists(nodeGroupId))
                {
                    container = securityManager.SecurityManagerInfo.MpanTable.AddOrReplace(nodeGroupId, 0x55, null, new byte[16]);
                }
                container = securityManager.SecurityManagerInfo.MpanTable.GetContainer(nodeGroupId);

                if (isMOS != null)
                {
                    container.SetMosState(isMOS.Value);
                }
                if (seqNo != null)
                {
                    container.SetSequenceNumber(seqNo.Value);
                }
                if (mpanState != null && mpanState.Length == 16)
                {
                    container.SetMpanState(mpanState);
                }
                if (nodes != null)
                {
                    container.SetReceiverGroupHandle(nodes.Select(x => (NodeTag)x).ToArray());
                }
            }
        }

        public byte[] GetMPANState(Alias deviceNumber, Node[] nodes)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            byte[] mpanState = new byte[16];
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                MpanContainer container = null;
                byte groupId = securityManager.SecurityManagerInfo.MpanTable.FindGroup(nodes.Select(x => (NodeTag)x).ToArray());
                var nodeGroupId = new NodeGroupId(device.Network.NodeTag, groupId);
                if (securityManager.SecurityManagerInfo.MpanTable.CheckMpanExists(nodeGroupId))
                {
                    container = securityManager.SecurityManagerInfo.MpanTable.GetContainer(nodeGroupId);
                }
                if (container != null)
                {
                    mpanState = container.MpanState;
                }
            }
            return mpanState;
        }

        private byte[] GetMPAN(Alias deviceNumber, Alias ownerDeviceNumber, byte groupId)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            Device ownerDevice = DeviceManager.GetDevice(ownerDeviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            byte[] mpan = new byte[16];
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                MpanContainer container = null;
                var nodeGroupId = new NodeGroupId(ownerDevice.Network.NodeTag, groupId);
                if (securityManager.SecurityManagerInfo.MpanTable.CheckMpanExists(nodeGroupId))
                {
                    container = securityManager.SecurityManagerInfo.MpanTable.GetContainer(nodeGroupId);
                }
                if (container != null)
                {
                    mpan = container.MpanState;
                }
            }
            return mpan;
        }

        public byte[] GetMPANState(Alias deviceNumber, Alias ownerDeviceNumber, byte groupId)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            Device ownerDevice = DeviceManager.GetDevice(ownerDeviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            byte[] mpanState = new byte[0];
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                MpanContainer container = null;
                var nodeGroupId = new NodeGroupId(ownerDevice.Network.NodeTag, groupId);
                if (securityManager.SecurityManagerInfo.MpanTable.CheckMpanExists(nodeGroupId))
                {
                    container = securityManager.SecurityManagerInfo.MpanTable.GetContainer(nodeGroupId);
                }
                if (container != null)
                {
                    mpanState = container.MpanState;
                }
            }
            return mpanState;
        }

        public void NextMpanForGroupId(Alias deviceNumber, Alias ownerDeviceNumber, byte groupId, int times)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                var mpan = new byte[16];
                for (int indi = 0; indi < times; indi++)
                {
                    var nodeGroupId = new NodeGroupId(device.Network.NodeTag, groupId);
                    if (securityManager.SecurityManagerInfo.MpanTable.CheckMpanExists(nodeGroupId))
                    {
                        securityManager.NextMPAN(nodeGroupId, mpan);
                    }
                }
            }
        }

        public void NextMpanForGroupId(Alias deviceNumber, Alias ownerDeviceNumber, Node[] nodes, int times)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                var mpan = new byte[16];
                byte groupId = securityManager.SecurityManagerInfo.MpanTable.FindGroup(nodes.Select(x => (NodeTag)x).ToArray());
                for (int indi = 0; indi < times; indi++)
                {
                    var nodeGroupId = new NodeGroupId(device.Network.NodeTag, groupId);
                    if (securityManager.SecurityManagerInfo.MpanTable.CheckMpanExists(nodeGroupId))
                    {
                        securityManager.NextMPAN(nodeGroupId, mpan);
                    }
                }
            }
        }

        public byte GetMpanGroupForNodeId(Alias deviceNumber, Node[] nodes)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                return securityManager.SecurityManagerInfo.MpanTable.FindGroup(nodes.Select(x => (NodeTag)x).ToArray());
            }
            return 0;
        }

        public void SetNodeIdsForMpanGroup(Alias deviceNumber, byte groupId, Node[] nodes)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                var container = securityManager.SecurityManagerInfo.MpanTable.GetContainer(new NodeGroupId(device.Network.NodeTag, groupId));
                if (container != null)
                {
                    container.SetReceiverGroupHandle(nodes.Select(x => (NodeTag)x).ToArray());
                }
            }
        }

        public byte GetTxSequenceNumber(Alias deviceNumber, Node node)
        {
            byte txSeqnumber = 0;
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                InvariantPeerNodeId peerNodeId = new InvariantPeerNodeId(device.Network.NodeTag, node);
                txSeqnumber = securityManager.SecurityManagerInfo.SpanTable.GetTxSequenceNumber(peerNodeId);
            }

            return txSeqnumber;
        }

        public byte GetTxSequenceNumber(Alias deviceNumber, Alias nodeNumber)
        {
            byte txSeqnumber = 0;
            Device device = DeviceManager.GetDevice(deviceNumber);
            Device peerDevice = DeviceManager.GetDevice(nodeNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                InvariantPeerNodeId peerNodeId = new InvariantPeerNodeId(device.Network.NodeTag, peerDevice.Network.NodeTag);
                txSeqnumber = securityManager.SecurityManagerInfo.SpanTable.GetTxSequenceNumber(peerNodeId);
            }
            return txSeqnumber;
        }

        public byte UpdateTxSequenceNumber(Alias deviceNumber, Node node)
        {
            byte txSeqnumber = 0;
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                InvariantPeerNodeId peerNodeId = new InvariantPeerNodeId(device.Network.NodeTag, node);
                txSeqnumber = securityManager.SecurityManagerInfo.SpanTable.UpdateTxSequenceNumber(peerNodeId);
            }
            return txSeqnumber;
        }

        public byte[] GetLastSendDataBuffer(Alias deviceNumber)
        {
            byte[] ret = null;
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                ret = securityManager.SecurityManagerInfo.LastSendDataBuffer;
            }
            return ret;
        }

        public void SetInclusionDSK(byte[] dskValue)
        {
        }

        public byte[] GetInclusionDSK()
        {
            throw new NotImplementedException();
        }

        public void SetInclusionKEX(SecuritySchemes grantedSchemes)
        {
            throw new NotImplementedException();
        }

        public void SetInclusionKEX(bool isGrantAll)
        {
            throw new NotImplementedException();
        }

        public SecuritySchemes GetInclusionKEX()
        {
            throw new NotImplementedException();
        }

        public void SetSecurityTestReceiverNonceS0(Alias deviceNumber, byte[] nonceData)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestReceiverNonceS0(nonceData);
            }
        }

        public void SetSecurityTestMACS0(Alias deviceNumber, byte[] code)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestMacS0(code);
            }
        }

        public void SetSecurityTestNetworkKeySetS0(Alias deviceNumber, byte[] networkKey)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestNetworkKeyS0InSet(networkKey);
            }
        }

        public void SetSecurityTestNetworkKeyVerifyS0(Alias deviceNumber, byte[] networkKey)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestNetworkKey(networkKey, ZWave.Enums.SecuritySchemes.S0, false);
                securityManager.SecurityManagerInfo.ActivateNetworkKeyS0();
            }
        }

        public void SetSecurityTestSchemeS0(Alias deviceNumber, byte scheme)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestSecuritySchemeS0(scheme);
            }
        }

        public void SetSecurityTestSchemeInInheritS0(Alias deviceNumber, byte scheme)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetTestSecuritySchemeInInheritS0(scheme);
            }
        }

        public void SetSecurityDelayS0(Alias deviceNumber, SecurityS0Delays delay, int timeoutMs)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetDelayS0((ZWave.BasicApplication.Security.SecurityS0Delays)((byte)delay), timeoutMs);
            }
        }

        public void ClearSecurityTestOverridesS0(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.ClearTestOverridesS0();
            }
        }

        public void ClearSecurityDelaysS0(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.ClearDelaysS0();
            }
        }

        public void DisableNonceReportS0(Alias deviceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.IsSenderNonceS0Disabled = true;
            }
        }

        public void NetworkKeyChanged(Alias controllerNumber, Action<byte[], bool> storeNetworkKey)
        {
            var device = DeviceManager.GetDevice(controllerNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.NetworkKeyS0Changed += (key, isTest) => storeNetworkKey(key, false);

                securityManager.SecurityManagerInfo.NetworkKeyS2Changed += (peer, key, scheme, istemp, isTest) =>
                {
                    storeNetworkKey(key, scheme == ZWave.Enums.SecuritySchemes.S2_TEMP);
                };
            }
        }

        public void SetPredefinedNetworkKeys(Alias controllerNumber)
        {
            SetNetworkKeyS2(controllerNumber, new byte[] { 0xC0 }.Concat(new byte[15]).ToArray(), SecuritySchemes.S2_UNAUTHENTICATED);
            SetNetworkKeyS2(controllerNumber, new byte[] { 0xC1 }.Concat(new byte[15]).ToArray(), SecuritySchemes.S2_AUTHENTICATED);
            SetNetworkKeyS2(controllerNumber, new byte[] { 0xC2 }.Concat(new byte[15]).ToArray(), SecuritySchemes.S2_ACCESS);
            SetNetworkKeyS2(controllerNumber, new byte[] { 0xC3 }.Concat(new byte[15]).ToArray(), SecuritySchemes.S2_AUTHENTICATED, true);
            SetNetworkKeyS2(controllerNumber, new byte[] { 0xC4 }.Concat(new byte[15]).ToArray(), SecuritySchemes.S2_ACCESS, true);
            SetNetworkKeyS2(controllerNumber, new byte[16].Select(x => (byte)0x01).ToArray(), SecuritySchemes.S0);

        }

        public byte[] CalculateTempNetworkKeyS2(Alias deviceNumber, byte[] receiverPublicKey, bool isKeySender)
        {
            byte[] ret = null;
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                ret = securityManager.SecurityManagerInfo.CalculateTempNetworkKeyS2(receiverPublicKey, isKeySender);
            }
            return ret;
        }

        public void SetInitialS2SequenceNumber(Alias deviceNumber, byte? sequenceNumber)
        {
            Device device = DeviceManager.GetDevice(deviceNumber);
            ISubstituteManager sm = device.SessionClient.GetSubstituteManager(typeof(SecurityManager));
            if (sm != null)
            {
                SecurityManager securityManager = (SecurityManager)sm;
                securityManager.SecurityManagerInfo.SetInitialS2SequenceNumber(sequenceNumber);
            }
        }

        #endregion
    }
}
