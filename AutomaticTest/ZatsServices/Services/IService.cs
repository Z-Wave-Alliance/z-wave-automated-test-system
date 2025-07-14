// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using ZWave.Enums;

namespace Zats.Services
{
    public interface IService
    {
        /// <summary>
        /// Start inclusion proccess, with default parameters
        /// </summary>
        /// <param name="controllerNumber">Inclusion controller</param>
        /// <param name="nodeNumber">Joining node</param>
        /// <returns>Inclusion result</returns>
        BasicResult.AddNodeWithLearnMode AddNode(Alias controllerNumber, Alias nodeNumber);
        /// <summary>
        /// Start inclusion proccess, with default parameters
        /// </summary>
        /// <param name="controllerNumber">Inclusion controller</param>
        /// <param name="nodeNumber">Joining node</param>
        /// <returns>Inclusion result</returns>
        BasicResult.AddNodeWithLearnMode AddNode(Alias controllerNumber, Alias nodeNumber, int timeoutMs);
        BasicResult.Void SetMaxLrTxPower(Alias controllerNumber, MaxLrTxPower value);
        BasicResult.GetMaxLrTxPower GetMaxLrTxPower(Alias controllerNumber);
        /// <summary>
        /// Get Max Payload Size
        /// </summary>
        /// <param name="controllerNumber"></param>
        /// <returns>Max Payload Size for Serial API quired</returns>
        BasicResult.MaxPayloadSize GetMaxPayloadSize(Alias controllerNumber);
        BasicResult.MaxPayloadSize GetMaxLRPayloadSize(Alias controllerNumber);
        BasicResult.Void WriteZW(Alias primary, string strData);
        BasicResult.Void SendDataForced(Alias nodeNumber, ChannelConfigs chConfig, TransmitRfOptions txRfOptions, byte[] homeId, byte srcNodeId, byte headerType, bool speedModified, bool ack, byte seqNo, byte[] destination, byte[] data);
        BasicResult.Void SendDataForced(Alias nodeNumber, ChannelConfigs chConfig, TransmitRfOptions txRfOptions, byte[] homeId, byte srcNodeId, byte headerType, bool speedModified, bool ack, byte seqNo, byte destination, byte[] data);
        // BasicResult.Void SendDataForced(Alias nodeNumber, ChannelConfigs chConfig, TransmitRfOptions txRfOptions, byte[] homeId, byte[] srcNodeId, byte headerType, bool speedModified, bool ack, byte seqNo, byte[] destination, byte[] data);
        BasicResult.ReturnValue InitiateShutdown(Alias controllerNumber);
        BasicResult.Void WriteZW(Alias primary, byte[] data);
        BasicResult.ExpectZW RequestZW(Alias primary, string strData, string strFilter, int timeoutMs);
        BasicResult.ExpectZW RequestZW(Alias primary, byte[] data, string strFilter, int timeoutMs);
        BasicResult.ReturnValue SerialApiSetNodeIdBaseType(Alias primary, byte version);
        BasicResult.AddNodeWithLearnMode AddNode(Alias controllerNumber, Alias nodeNumber, Modes addMode, int timeoutMs);
        BasicResult.AddNodeWithLearnMode AddNode(Alias controllerNumber, Alias nodeNumber, Modes addMode, LearnModes learnMode, int timeoutMs);
        int AddNodes(Alias controllerNumber, int timeoutMs, Alias[] nodeNumbers);
        int AddNodes(Alias controllerNumber, Alias[] nodeNumbers);
        int AddNodesWithDSK(Alias controllerNumber, Alias[] nodeNumbers, int dsk);
        int AddNodesWithDSK(Alias controllerNumber, Alias[] nodeNumbers, byte[] dsk);
        int AddNodesWithDSK(Alias controllerNumber, int timeoutMs, Alias[] nodeNumbers, byte[] dsk);
        int AddNodesWithDSK(Alias controllerNumber, int timeoutMs, Alias[] nodeNumbers, int dsk);
        System.Collections.Generic.List<BasicResult.AddNodeWithLearnMode> AddNodesWithDSK(Alias controllerNumber, Alias[] nodeNumbers, Modes addMode, LearnModes learnMode, int dsk, int timeoutMs);
        BasicResult.AddNode AddNodeVirtual(Alias controllerNumber, Alias bridgeNumber, int timeoutMs);
        BasicResult.AddNode AddNodeVirtual(Alias controllerNumber, Alias bridgeNumber);
        BasicResult.AddNodeWithLearnMode AddNodeWithDSK(Alias controllerNumber, Alias nodeNumber, Modes addMode, LearnModes learnMode, int dsk, int timeoutMs);
        BasicResult.AddNodeWithLearnMode AddNodeWithDSK(Alias controllerNumber, Alias nodeNumber, int dsk, int timeoutMs);
        BasicResult.AddNodeWithLearnMode AddNodeWithDSK(Alias controllerNumber, Alias nodeNumber, byte[] dsk);
        BasicResult.AddNodeWithLearnMode AddNodeWithDSK(Alias controllerNumber, Alias nodeNumber, int dsk);
        BasicResult.AddNodeWithLearnMode AddNodeWithDSK(Alias controllerNumber, Alias nodeNumber, Modes addMode, LearnModes learnMode, byte[] dsk, int timeoutMs);
        BasicResult.AddNodeWithLearnMode AddNodeWithDSK(Alias controllerNumber, Alias nodeNumber, byte[] dsk, int timeoutMs);
        BasicResult.Void ApplicationNodeInformation(Alias deviceNumber, params byte[] cmdClasses);
        BasicResult.Void ApplicationNodeInformation(Alias deviceNumber, DeviceOptions deviceOptions, byte generic, byte specific, params byte[] cmdClasses);
        BasicResult.Void ApplicationNodeInformation(Alias deviceNumber, bool isListening, byte generic, byte specific, params byte[] nodeParameters);
        BasicResult.Void SetRfRegion(Alias dut, RfRegions rfRegion);
        BasicResult.ReturnValue SetSecurityInclusionRequestedKeys(Alias deviceNumber, SecuritySchemes[] securitySchemes);
        BasicResult.GetRfRegion GetRfRegion(Alias dut);
        BasicResult.ReturnValue SetSecurityInclusionRequestedAuthentication(Alias deviceNumber, SecurityAuthentications securityAuthentication);
        BasicResult.Void ApplicationNodeInformationCmdClasses(Alias deviceNumber, byte[] nodeParameters, byte[] unsecureNodeParameters, byte[] secureNodeParameters);
        BasicResult.Void ApplicationVirtualDeviceNodeInformation(Alias bridgeNumber, bool isListening, byte generic, byte specific, params byte[] cmdClasses);
        BasicResult.AreNodesNeighbours AreNodesNeighbours(Alias controllerNumber, Alias nodeNumber1, Alias nodeNumber2);
        BasicResult.AssignReturnRoute AssignPriorityReturnRoute(Alias controllerNumber, Alias srcNode, Alias destNode, Node priorityRoute0, Node priorityRoute1, Node priorityRoute2, Node priorityRoute3, byte routespeed);
        BasicResult.Void AssignPrioritySucReturnRoute(Alias controllerNumber, Alias nodeNumber, Node repeater0, Node repeater1, Node repeater2, Node repeater3, byte routespeed);
        BasicResult.AssignReturnRoute AssignReturnRoute(Alias controllerNumber, Alias srcNode, Alias destNode);
        BasicResult.Void AssignSucReturnRoute(Alias controllerNumber, Alias nodeNumber);
        BasicResult.AddNode ControllerChange(Alias deviceNumber, ControllerChangeModes mode);
        BasicResult.AddNodeWithLearnMode ControllerChange(Alias primaryControllerNumber, Alias newPrimaryControllerNumber);
        BasicResult.AddNodeWithLearnMode ControllerChange(Alias primaryControllerNumber, Alias newPrimaryControllerNumber, int timeoutMs);
        BasicResult.AddNode CreateNewPrimary(Alias controllerNumber, Alias newPrimaryControllerNumber);
        BasicResult.AddNode CreateNewPrimary(Alias controllerNumber, Alias newPrimaryControllerNumber, int timeoutMs);
        BasicResult.Void DeleteReturnRoute(Alias controllerNumber, Alias nodeNumber);
        BasicResult.Void DeleteSucReturnRoute(Alias controllerNumber, Alias nodeNumber);
        BasicResult.Void EnableSUC(Alias deviceNumber, bool isEnable, byte capabilities);
        BasicResult.RemoveNode ExcludeNode(Alias controllerNumber, Alias nodeNumber, Modes removeMode, LearnModes learnMode, int timeoutMs);
        BasicResult.RemoveNode ExcludeNode(Alias controllerNumber, Alias nodeNumber, Modes removeMode, int timeoutMs);
        BasicResult.RemoveNode ExcludeNode(Alias controllerNumber, Alias nodeNumber);
        BasicResult.RemoveNode ExcludeNode(Alias controllerNumber, Alias nodeNumber, int timeoutMs);
        BasicResult.ExpectData ExpectData(Alias deviceNumber, byte[] expectedData);
        BasicResult.ExpectData ExpectData(Alias deviceNumber, byte[] expectedData, int timeoutMs);
        BasicResult.Void ExploreRequestExclusion(Alias deviceNumber);
        BasicResult.Void ExploreRequestInclusion(Alias deviceNumber);
        BasicResult.FirmwareUpdateNvmInit FirmwareUpdateNvmInit(Alias deviceNumber);
        BasicResult.FirmwareUpdateNvmSetNewImage FirmwareUpdateNvmSetNewImage(Alias deviceNumber, bool isSet);
        BasicResult.FirmwareUpdateNvmGetNewImage FirmwareUpdateNvmGetNewImage(Alias deviceNumber);
        BasicResult.FirmwareUpdateNvmUpdateCrc16 FirmwareUpdateNvmUpdateCrc16(Alias deviceNumber, int offset, ushort length, ushort seed);
        BasicResult.FirmwareUpdateNvmIsValidCrc16 FirmwareUpdateNvmIsValidCrc16(Alias deviceNumber);
        BasicResult.FirmwareUpdateNvmWrite FirmwareUpdateNvmWrite(Alias deviceNumber, int offset, ushort length, byte[] buffer);
        BasicResult.FirmwareUpdateNvmWrite FirmwareUpdateNvmWrite(Alias deviceNumber, ushort blockSize, string filePath);
        BasicResult.FirmwareUpdateSendXModem FirmwareUpdateSendXModem(Alias primary, string fwFileName);
        BasicResult.GetControllerCapabilities GetControllerCapabilities(Alias controllerNumber);
        string GetDevicePort(Alias deviceNumber);
        BasicResult.GetNeighborCount GetNeighborCount(Alias controllerNumber, Alias nodeNumber);
        BasicResult.GetPriorityRoute GetPriorityRoute(Alias controllerNumber, Alias nodeNumber);
        BasicResult.GetProtocolInfo GetProtocolInfo(Alias controllerNumber, Alias requestedDeviceNumber);
        BasicResult.GetProtocolStatus GetProtocolStatus(Alias deviceNumber);
        BasicResult.GetRandomWord GetRandomWord(Alias deviceNumber, byte count);
        BasicResult.GetRoutingInfo GetRoutingInfo(Alias controllerNumber, Alias nodeNumber, byte removeBad, byte removeNonReps);
        BasicResult.GetSecurityKeys GetSecurityKeys(Alias deviceNumber);
        BasicResult.GetSecurityS2PublicDSK GetSecurityS2PublicDSK(Alias deviceNumber);
        BasicResult.GetSucNodeId GetSucNodeId(Alias controllerNumber);
        BasicResult.GetTransmitCount GetTransmitCount(Alias installerNumber);
        BasicResult.GetVersion GetVersion(Alias deviceNumber);
        BasicResult.GetVirtualNodes GetVirtualNodes(Alias bridgeNumber);
        BasicResult.IncludeNodeWithLearnMode IncludeNode(Alias controllerNumber, Alias nodeNumber);
        BasicResult.IncludeNodeWithLearnMode IncludeNode(Alias controllerNumber, Alias nodeNumber, int timeoutMs);
        BasicResult.IncludeNodeWithLearnMode IncludeNode(Alias controllerNumber, Alias nodeNumber, Modes addMode, int timeoutMs);
        BasicResult.IncludeNodeWithLearnMode IncludeNode(Alias controllerNumber, Alias nodeNumber, Modes addMode, LearnModes learnMode, int timeoutMs);
        BasicResult.IsFailedNode IsFailedNode(Alias controllerNumber, Alias nodeNumber);
        BasicResult.IsNodeWithinDirectRange IsNodeWithinDirectRange(Alias endDeviceNumber, Alias nodeNumber);
        BasicResult.IsVirtualNode IsVirtualNode(Alias bridgeNumber, byte nodeId);
        BasicResult.MemoryGetBuffer MemoryGetBuffer(Alias deviceNumber, ushort offset, byte length);
        BasicResult.MemoryGetByte MemoryGetByte(Alias deviceNumber, ushort offset);
        BasicResult.MemoryGetId MemoryGetId(Alias deviceNumber);
        BasicResult.Void MemoryPutBuffer(Alias deviceNumber, ushort offset, ushort length, byte[] data);
        BasicResult.Void MemoryPutByte(Alias deviceNumber, ushort offset, byte data);
        BasicResult.Void NvmBackupRestoreClose(Alias deviceNumber);
        BasicResult.NvmBackupRestoreOpen NvmBackupRestoreOpen(Alias deviceNumber);
        BasicResult.NvmBackupRestoreRead NvmBackupRestoreRead(Alias deviceNumber, byte length, int offset);
        BasicResult.NvmBackupRestoreWrite NvmBackupRestoreWrite(Alias deviceNumber, byte length, int offset, byte[] data);
        BasicResult.RandomByte RandomByte(Alias deviceNumber);
        bool Reconnect(Alias deviceNumber);
        BasicResult.Void RediscoveryNeeded(Alias endDeviceNumber, Alias nodeNumber);
        BasicResult.Void RediscoveryNeededBroadcast(Alias endDeviceNumber);
        BasicResult.Void RemoveFailedNodeId(Alias controllerNumber, Alias nodeNumber);
        BasicResult.RemoveNode RemoveNode(Alias controllerNumber, Alias nodeNumber, int timeoutMs);
        BasicResult.RemoveNode RemoveNode(Alias controllerNumber, Alias nodeNumber, Modes removeMode, LearnModes learnMode, int timeoutMs);
        BasicResult.RemoveNode RemoveNode(Alias controllerNumber, Alias nodeNumber);
        BasicResult.RemoveNode RemoveNodeIdFromNetwork(Alias controllerNumber, Alias nodeNumber, Modes removeMode, LearnModes learnMode, int timeoutMs);
        int RemoveNodes(Alias controllerNumber, int timeoutMs, params Alias[] nodeNumbers);
        int RemoveNodes(Alias controllerNumber, params Alias[] nodeNumbers);
        //BasicResult.Void ReplaceFailedNode(Alias controllerNumber, Alias nodeNumber);
        BasicResult.Void ReplicationReceiveComplete(Alias controllerNumber);
        BasicResult.Transmit ReplicationSend(Alias controllerNumber, Alias nodeNumber, byte[] data, TransmitOptions txOptions);
        BasicResult.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, byte[] expectedData);
        BasicResult.RequestData RequestData(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, byte[] expectedData, int timeoutMs);
        BasicResult.RequestData RequestDataBridge(Alias senderNumber, Alias receiverNumber, Node srcNode, byte[] data, TransmitOptions txOptions, byte[] expectedData, int timeoutMs);
        BasicResult.RequestData RequestDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, byte[] expectedData, int timeoutMs);
        BasicResult.RequestData RequestDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, SecuritySchemes scheme, byte[] expectedData);
        BasicResult.RequestData RequestDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2, byte[] expectedData, int timeoutMs);
        BasicResult.RequestNetworkUpdate RequestNetworkUpdate(Alias deviceNumber);
        BasicResult.Void RequestNewRouteDestinations(Alias endDeviceNumber, byte[] destList);
        BasicResult.RequestNodeInfo RequestNodeInfo(Alias deviceNumber, Alias nodeNumber);
        BasicResult.GetNodeInfo GetNodeInfo(Alias deviceNumber, Alias nodeNumber);
        BasicResult.RequestNodeNeighborUpdate RequestNodeNeighborUpdate(Alias controllerNumber, Alias nodeNumber);

        /// <summary>
        /// If calling the API with nodeType 0x00, and 0x01, it will work as a normal neighbor update.
        /// Otherwise (0x02):
        /// When the API is called with the node-id of the listening node and the nodes to discovery are FLIRS nodes
        /// 1. Controller will send find_nodes_in_range for each FLIRS node in the network where the listening node is the only node in the bitmask
        /// 2. The Controller will create node-id bitmask for each FLIRS node that can reach the listening node
        /// 3. The Controller will update the routing info for the listening node
        /// </summary>
        /// <param name="controllerNumber">Serial API Controller Host</param>
        /// <param name="nodeNumber">Node Id</param>
        /// <param name="nodeType">0- non listening, 1 - listening, 2 - FLiRS</param>
        BasicResult.RequestNodeNeighborUpdate RequestNodeNeighborDiscovery(Alias controllerNumber, Alias nodeNumber, byte nodeType);
        void Reset(Alias deviceNumber);
        BasicResult.Void ResetTransmitCount(Alias installerNumber);
        BasicResult.RFPowerLevelGet RFPowerLevelGet(Alias deviceNumber);
        BasicResult.Void RFPowerlevelRediscoverySet(Alias deviceNumber, byte powerLevel);
        BasicResult.Void RFPowerLevelSet(Alias deviceNumber, byte powerLevel);
        BasicResult.Transmit SendBroadCastNodeInfo(Alias deviceNumber);
        BasicResult.Transmit SendBroadCastNodeInfo(Alias deviceNumber, TransmitOptions txOptions);
        BasicResult.SendData SendData(Alias senderNumber, Alias receiverNumber, byte[] data);
        BasicResult.SendData SendData(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions);
        BasicResult.Void SendDataAbort(Alias deviceNumber);
        BasicResult.SendData SendDataBridge(Alias bridgeNumber, Node srcNode, Alias destNodeNumber, byte[] data, TransmitOptions txOptions);
        BasicResult.SendData SendDataBridge(Alias bridgeNumber, Node srcNode, Node destNode, byte[] data, TransmitOptions txOptions);
        BasicResult.SendData SendDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2);
        BasicResult.SendData SendDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, TransmitOptions txOptions, SecuritySchemes scheme);
        BasicResult.SendData SendDataEx(Alias senderNumber, Alias receiverNumber, byte[] data, SecuritySchemes scheme);
        BasicResult.Transmit SendDataMultiEx(Alias senderNumber, Alias[] receiverNumbers, byte groupId, byte[] data, SecuritySchemes scheme);
        BasicResult.Transmit SendDataMetaBridge(Alias bridgeNumber, Node srcNode, Alias destNodeNumber, byte[] data, TransmitOptions txOptions);
        BasicResult.Transmit SendDataMulti(Alias senderNumber, Alias[] receiverNumbers, byte[] data, TransmitOptions txOptions);
        BasicResult.Transmit SendDataMultiBridge(Alias bridgeNumber, Node srcNode, Alias[] destNodeNumbers, byte[] data, TransmitOptions txOptions);
        BasicResult.Transmit SendNodeInfo(Alias deviceNumber, Alias receiverNumber, TransmitOptions txOptions);
        BasicResult.Transmit SendNop(Alias senderNumber, Alias receiverNumber, TransmitOptions txOptions);
        BasicResult.Transmit EnqueueSendNop(Alias senderNumber, Alias receiverNumber, TransmitOptions txOptions);
        BasicResult.Transmit SendVirtualDeviceNodeInformation(Alias bridgeNumber, Node srcNode, Alias destNodeNumber, TransmitOptions txOptions);
        BasicResult.Transmit SendSucId(Alias controllerNumber, Alias nodeNumber, TransmitOptions txOptions);
        BasicResult.Transmit SendTestFrame(Alias deviceNumber, Alias nodeNumber, byte powerLevel);
        BasicResult.SerialApiGetCapabilities SerialApiGetCapabilities(Alias deviceNumber);
        BasicResult.SerialApiGetInitData SerialApiGetInitData(Alias deviceNumber);
        BasicResult.SerialApiGetLRNodes SerialApiGetLRNodes(Alias primary);
        BasicResult.Void SerialApiTest(Alias deviceNumber, byte testCmd, ushort testDelay, byte testPayloadLength, ushort testCount, TransmitOptions txOptions, bool isStopOnErrors, params Alias[] nodeNumbers);
        BasicResult.Void SetDefault(Alias deviceNumber);
        BasicResult.Void SetExtIntLevel(Alias deviceNumber, byte intSrc, bool triggerLevel);
        BasicResult.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode, int timeoutMs);
        BasicResult.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode, bool isNonSecure, int timeoutMs);
        BasicResult.SetLearnMode SetLearnMode(Alias deviceNumber, LearnModes mode);
        BasicResult.SetPriorityRoute SetPriorityRoute(Alias controllerNumber, Alias nodeNumber, Node repeater0, Node repeater1, Node repeater2, Node repeater3, byte routespeed);
        BasicResult.SetPriorityRoute SetPriorityRoute(Alias controllerNumber, Node node, Node repeater0, Node repeater1, Node repeater2, Node repeater3, byte routespeed);
        BasicResult.Void SetPromiscuousMode(Alias deviceNumber, bool state);
        BasicResult.ReturnValue SetRFReceiveMode(Alias deviceNumber, bool mode);
        BasicResult.Void SetRoutingInfo(Alias controllerNumber, Alias nodeNumber, byte[] nodeMask);
        BasicResult.Void SetRoutingInfo(Alias controllerNumber, Node node, byte[] nodeMask);
        BasicResult.Void SetRoutingMAX(Alias deviceNumber, byte maxRouteTries);
        BasicResult.SetLearnMode SetVirtualDeviceLearnMode(Alias bridgeNumber, Node node, VirtualDeviceLearnModes mode);
        BasicResult.Void SetSleepMode(Alias deviceNumber, SleepModes mode);
        BasicResult.Void SetSleepMode(Alias deviceNumber, SleepModes mode, byte intEnable);
        BasicResult.Void PowerMgmtStayAwake(Alias deviceNumber, byte powerLockType, int powerTimeout);
        BasicResult.Void PowerMgmtStayAwake(Alias deviceNumber, byte powerLockType, int awakeTimeoutMs, int wakeupTimeoutMs);
        BasicResult.Void PowerMgmtCancel(Alias deviceNumber, byte powerLockType);
        BasicResult.ClearNetworkStats ClearNetworkStats(Alias deviceNumber);
        BasicResult.GetNetworkStats GetNetworkStats(Alias deviceNumber);
        BasicResult.GetTxTimer GetTxTimer(Alias deviceNumber);
        BasicResult.Void ClearTxTimers(Alias deviceNumber);

        /// <summary>
        /// Configure a static/bridge controller to be a SIS node
        /// </summary>
        /// <param name="controllerNumber"></param>
        /// <param name="controllerSucNumber"></param>
        /// <returns></returns>
        BasicResult.SetSucNodeId SetSUCNodeID(Alias controllerNumber, Alias controllerSucNumber);
        /// <summary>
        /// Configure a static/bridge controller be a SUC/SIS node or not
        /// </summary>
        /// <param name="controllerNumber"></param>
        /// <param name="controllerSucNumber"></param>
        /// <param name="isEnable">Want the static controller to be a SUC/SIS node</param>
        /// <param name="capabilities">Enable the node ID server functionality to become a SIS</param>
        /// <param name="isLowPower">Want to send the frame with low transmission power</param>
        /// <returns></returns>
        BasicResult.SetSucNodeId SetSUCNodeID(Alias controllerNumber, Alias controllerSucNumber, bool isEnable, byte capabilities, bool isLowPower);
        ServiceSettings Settings { get; set; }
        BasicResult.Void SetWutTimeout(Alias deviceNumber, byte timeoutSec);
        BasicResult.Void SetMaxInclusionRequestInterval(Alias deviceNumber, byte requestintervalSec);
        BasicResult.Void SoftReset(Alias deviceNumber);
        BasicResult.Void StoreHomeId(Alias installerNumber, byte[] homeId, byte nodeId);
        BasicResult.Void StoreNodeInfo(Alias installerNumber, byte nodeId, byte[] nodeInfo);
        BasicResult.TypeLibrary TypeLibrary(Alias deviceNumber);
        BasicResult.Void WatchDogDisable(Alias deviceNumber);
        BasicResult.Void WatchDogEnable(Alias deviceNumber);
        BasicResult.Void WatchDogKick(Alias deviceNumber);
        BasicResult.Void WatchDogStart(Alias deviceNumber);
        BasicResult.Void WatchDogStop(Alias deviceNumber);
        BasicResult.ReturnValue SetListenBeforeTalkThreshold(Alias deviceNumber, byte channel, byte threshold);
        BasicResult.Void ApplicationUserInput(Alias deviceNumber, byte[] userInputId, byte[] userInputLocal);
        BasicResult.Void ApplicationGetUserInput(Alias deviceNumber);
        bool UserActionRequired(string ActionDescription, int timeoutMilliseconds);
        bool UserActionRequired(string ActionDescription);
        void SetLedState(Alias deviceNumber, byte ledMask);
        byte[] GetDskFromEmbeded(Alias deviceNumber, int NumBytes);
        byte[] GetDsk(Alias deviceNumber, int NumBytes);
        BasicResult.Void SetMaxInclusionRequestIntervals(Alias deviceNumber, byte requestintervalSec);
        byte[] GetInclusionRequestHomeId(Alias deviceNumber);
        void BackUpNVM(Alias deviceNumber, string backUpPath);

        /// <summary>
        /// UI specific command for deleting one item from PC Controller commands queue
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <returns></returns>
        BasicResult.Void DeleteFirstItemFromCommandQueue(Alias deviceNumber);

        /// <summary>
        /// UI specific command for clearing commands queue
        /// </summary>
        /// <param name="deviceNumber"></param>
        /// <returns></returns>
        BasicResult.Void ClearCommandQueue(Alias deviceNumber);

        byte[] GetCsa(Alias controllerNumber, int numberOfBytes);
        bool Disconnect(Alias deviceNumber);
        bool Connect(Alias deviceNumber);
        BasicResult.Void SetSecurityBypass(Alias deviceNumber, bool isBypass);
        BasicResult.DefaultTxPowerLevelGet GetDefaultTxPowerLevel(Alias deviceNumber);
        BasicResult.DefaultTxPowerLevelGet GetDefaultTxPowerLevel16Bit(Alias deviceNumber);
        BasicResult.Void SetDefaultTxPowerLevel(Alias deviceNumber, sbyte normalTxPower, sbyte measured0dBmPower);
        BasicResult.Void SetDefaultTxPowerLevel16Bit(Alias deviceNumber, short normalTxPower, short measured0dBmPower);
        BasicResult.GetLRChannel GetLRChannel(Alias deviceNumber);
        BasicResult.Void SetLRChannel(Alias deviceNumber, LongRangeChannels channel);
        BasicResult.GetDcdcMode GetDcdcMode(Alias deviceNumber);
        BasicResult.Void SetDcdcMode(Alias deviceNumber, DcdcModes dcdcMode);
        BasicResult.GetBackgroundRssi GetBackgroundRSSI(Alias deviceNumber);
        
        /// <summary>
        /// Get current PTI support state on a required device.
        /// </summary>
        /// <param name="deviceNumber">Device alias to call Serial API operation on it.</param>
        /// <returns>Current PTI state: true if enabled, otherwise - false.</returns>
        BasicResult.GetRadioPTI GetRadioPTI(Alias deviceNumber);

        /// <summary>
        /// Enables or Disables Radio PTI support on required device.
        /// </summary>
        /// <param name="deviceNumber">Device alias to call Serial API operation on it.</param>
        /// <param name="isEnabled">true to enable PTI support; otherwise, false.</param>
        /// <returns>Called operation returns only completed state.</returns>
        BasicResult.Void EnableRadioPTI(Alias deviceNumber, bool isEnabled);

        BasicResult.Void SetSupervisionDelay(Alias deviceNumber, SupervisionReportStatuses status, int delay);
        string GetDskAsString(Alias deviceNumber);
    }
}
