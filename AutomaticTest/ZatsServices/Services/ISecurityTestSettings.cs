// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;

namespace Zats.Services
{
    public interface ISecurityTestSettings
    {
        IIncludeSettings IncludeSettings { get; set; }
        void ClearInclusionFramesS2(Alias deviceNumber);
        void FreeSpanS2ForNode(Alias deviceNumber, Node node);
        byte[] GetControllerPublicKey(Alias deviceNumber);
        byte[] GetNetworkKeyS2(Alias deviceNumber, SecuritySchemes scheme);
        byte[] GetNetworkKeyS2(Alias deviceNumber, SecuritySchemes scheme, bool isLRScheme);
        byte[] GetNetworkKeyS2Temp(Alias deviceNumber);
        byte[] GetSpanS2ForNode(Alias deviceNumber, Node node);
        void NextSpanS2ForNode(Alias deviceNumber, Node node, int nextNonceCount);
        [System.Obsolete("Use SetActiveSecurityScheme with specified device")]
        void SetActiveNetworkKeyS2(Alias deviceNumber, SecuritySchemes scheme);
        void SetActiveNetworkKeyS2Temp(Alias deviceNumber);
        void SetActiveSecurityScheme(Alias controllerNumber, Alias deviceNumber, SecuritySchemes scheme);
        void SetInitialS2SequenceNumber(Alias zatsController, byte? sequenceNumber);
        void SetActiveSecurityScheme(Alias controllerNumber, Alias deviceNumber, SecuritySchemes scheme, bool isLRScheme);
        void SetActiveSecurityScheme(Alias controllerNumber, Node node, SecuritySchemes scheme);
        void SetActiveSecurityScheme(Alias controllerNumber, Node node, SecuritySchemes scheme, bool isLRScheme);
        void SetPredefinedNetworkKeys(Alias controllerNumber);
        void SetInclusionFrameCommandS2(Alias deviceNumber, SecurityS2TestFrames testFrame, byte[] data);

        /// <summary>
        /// Enable test schema and set delay for specified inclusion S2 frame
        /// </summary>
        /// <param name="deviceNumber">Zats controller alias</param>
        /// <param name="testFrame">Inclusion S2 frame</param>
        /// <param name="delay">In milliseconds</param>
        void SetInclusionFrameDelayS2(Alias deviceNumber, SecurityS2TestFrames testFrame, int delay);
        void SetInclusionFrameEncryptedS2(Alias deviceNumber, SecurityS2TestFrames testFrame, bool? isEncrypted);
        void SetInclusionFrameMulticastS2(Alias deviceNumber, SecurityS2TestFrames testFrame, bool? isMulticast);
        void SetInclusionFrameBroadcastS2(Alias deviceNumber, SecurityS2TestFrames testFrame, bool? isBroadcast);
        void SetInclusionFrameNetworkKeyS2(Alias deviceNumber, SecurityS2TestFrames testFrame, byte[] networkKey);
        void SetInclusionFrameNetworkKeyS2Temp(Alias deviceNumber, SecurityS2TestFrames testFrame, byte[] networkKey);
        void SetInclusionFrameNetworkKeyS2Temp(Alias deviceNumber, SecurityS2TestFrames testFrame);
        void SetNetworkKeyS2(Alias deviceNumber, byte[] networkKey, SecuritySchemes activeNetworkKey);
        void SetNetworkKeyS2(Alias deviceNumber, byte[] networkKey, SecuritySchemes activeNetworkKey, bool isLRScheme);
        void SetNetworkKeyS2Temp(Alias deviceNumber, byte[] networkKey);
        void SetReceiverEntropyS2(Alias deviceNumber, byte[] receiverEntropy);
        void SetReceiverEntropyS2(Alias deviceNumber, byte[] receiverEntropy, int numOfTestReceiverEntropyUsage);
        void SetSecurityVersion(Alias deviceNumber, SecurityVersions securityVersions);
        void SetSenderEntropyInputS2(Alias deviceNumber, byte[] senderEntropyInput);
        void SetSenderEntropyInputS2(Alias deviceNumber, byte[] senderEntropyInput, int numOfSenderEntropyInputUsage);
        void SetSpanS2(Alias deviceNumber, byte[] span);
        void SetSpanS2(Alias deviceNumber, byte[] span, int numOfTestSpanUsage);
        byte[] GetSpanContainerCtx(Alias deviceNumber, Node node);
        void SetSpanContainerCtx(Alias deviceNumber, Node node, byte[] ctx, byte txSequenceNumber);
        void SetS2MessageEncapsulationExtension(Alias deviceNumber, SecurityS2ExtensionTypes extensionType, SecurityS2MessageTypes messageType, bool? isEncrypted, byte[] value, int? numOfUsage,
            bool? isMoreToFollow, bool? isCritical, byte? extensionLength, bool isOverride);
        void SetS2MessageEncapsulationExtension(Alias deviceNumber, TestExtensionS2Settings testExtensionSettings);
        void ClearTestExtensionsS2(Alias deviceNumber);
        void SetS2ParametersOverride(Alias deviceNumber, SecurityS2ParameterTypes securityS2ParameterType, byte[] value);
        void ClearS2ParametersOverride(Alias deviceNumber);
        void ClearMPANTable(Alias deviceNumber);
        void RemoveMPANRecord(Alias deviceNumber, byte groupdId, Node owner);
        void AddOrUpdateMPANState(Alias deviceNumber, byte groupdId, Node owner, bool? isMOS, byte? seqNo, byte[] mpanState, Node[] nodeIds);
        byte[] GetMPANState(Alias deviceNumber, Node[] nodeIds);
        byte[] GetMPANState(Alias deviceNumber, Alias ownerDeviceNumber, byte groupdId);
        void NextMpanForGroupId(Alias deviceNumber, Alias ownerDeviceNumber, byte GroupId, int times);
        void NextMpanForGroupId(Alias deviceNumber, Alias ownerDeviceNumber, Node[] nodeIds, int times);
        byte GetMpanGroupForNodeId(Alias deviceNumber, Node[] nodeIds);
        void SetNodeIdsForMpanGroup(Alias deviceNumber, byte groupId, Node[] nodeIds);
        void SetSecurityTestReceiverNonceS0(Alias deviceNumber, byte[] nonceData);
        void DisableNonceReportS0(Alias deviceNumber);
        byte[] CalculateTempNetworkKeyS2(Alias deviceNumber, byte[] receiverPublicKey, bool isKeySender);
        void SetSecurityTestMACS0(Alias deviceNumber, byte[] code);
        void SetSecurityTestNetworkKeySetS0(Alias deviceNumber, byte[] networkKey);
        void SetSecurityTestNetworkKeyVerifyS0(Alias deviceNumber, byte[] networkKey);
        void SetSecurityTestSchemeS0(Alias deviceNumber, byte scheme);
        void SetSecurityTestSchemeInInheritS0(Alias deviceNumber, byte scheme);
        void SetSecurityDelayS0(Alias deviceNumber, SecurityS0Delays delay, int timeoutMs);
        void ClearSecurityTestOverridesS0(Alias deviceNumber);
        void ClearSecurityDelaysS0(Alias deviceNumber);
        byte GetTxSequenceNumber(Alias deviceNumber, Node node);
        byte GetTxSequenceNumber(Alias deviceNumber, Alias nodeNumber);
        byte UpdateTxSequenceNumber(Alias deviceNumber, Node node);
        void NetworkKeyChanged(Alias controllerNumber, Action<byte[], bool> storeNetworkKey);
        byte[] GetLastSendDataBuffer(Alias deviceNumber);
        void SetNetworkKeys(Alias deviceNumber, Dictionary<SecuritySchemes, byte[]> keys);
    }
}
