
namespace Zats.Services
{
    public interface IComplianceService
    {
        ISendDataSettings SendDataSettings { get; }
        BasicResult.Void ApplicationVirtualDeviceNodeInformation(Alias bridgeNumber, Node endDeviceNode, bool isListening, byte generic, byte specific, byte[] cmdClasses);
        BasicResult.AssignReturnRoute AssignReturnRoute(Alias controllerNumber, Alias srcNode, Node destNode);
        byte FirmwareUpdateV2(Alias deviceNumber, Node nodeid, string filePath);
        byte FirmwareUpdateV3(Alias deviceNumber, Node nodeid, string filePath);
        byte FirmwareUpdateV3(Alias deviceNumber, Node nodeid, string filePath, ushort fragmentSize);
        byte FirmwareUpdateV5(Alias deviceNumber, Node nodeid, string filePath);
        byte FirmwareUpdateV5(Alias deviceNumber, Node nodeid, string filePath, byte activation);
        byte FirmwareUpdateV5(Alias deviceNumber, Node nodeid, string filePath, ushort fragmentSize, byte activation);
        BasicResult.IsFailedNode IsFailedNode(Alias controllerNumber, Node node);
        BasicResult.IncludeNodeWithLearnMode ReplaceFailedNode(Alias controllerNumber, Node node, Alias joiningDeivceNumber);
        BasicResult.Transmit SendTestFrame(Alias deviceNumber, Node nodeId, byte powerLevel);
        BasicResult.RemoveNode RemoveFailedNodeId(Alias controllerNumber, Node node);
        BasicResult.RequestData RequestData(Alias senderNumber, Node receiverNode, byte[] data, TransmitOptions txOptions, byte[] expectedData, int timeoutMs);
        BasicResult.RequestData RequestData(Alias senderNumber, Node receiverNode, byte[] data, byte[] expectedData);
        BasicResult.RequestData RequestData(Alias senderNumber, Node receiverNode, byte[] data, byte[] expectedData, int timeoutMs);
        BasicResult.RequestData RequestDataEx(Alias senderNumber, Node receiverNode, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, byte[] expectedData, int timeoutMs);
        BasicResult.RequestData RequestDataEx(Alias senderNumber, Node receiverNode, byte[] data, SecuritySchemes scheme, byte[] expectedData);
        BasicResult.RequestData RequestDataEx(Alias senderNumber, Node receiverNode, byte[] data, SecuritySchemes scheme, byte[] expectedData, int timeoutMs);
        BasicResult.RequestData RequestDataEx(Alias senderNumber, Node receiverNode, byte[] data, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2, byte[] expectedData, int timeoutMs);
        BasicResult.RequestNodeInfo RequestNodeInfo(Alias deviceNumber, Node node);
        BasicResult.GetProtocolInfo GetProtocolInfo(Alias deviceNumber, Node node);
        BasicResult.SendData SendData(Alias senderNumber, Node receiverNode, byte[] cmdData, TransmitOptions txOptions);
        BasicResult.SendData SendData(Alias senderNumber, Node receiverNode, byte[] cmdData);
        BasicResult.SendData SendDataBridge(Alias bridgeNumber, Node srcNode, Node receiverNode, byte[] data, TransmitOptions txOptions);
        BasicResult.SendData SendDataEx(Alias senderNumber, Node receiverNode, byte[] cmdData, SecuritySchemes scheme);
        BasicResult.SendData SendDataEx(Alias senderNumber, Node receiverNode, byte[] cmdData, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2);
        BasicResult.SendData SendDataEx(Alias senderNumber, Node receiverNode, byte[] cmdData, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme);
        BasicResult.SendData SendDataEx(Alias senderNumber, Node receiverNode, byte[] cmdData, SecuritySchemes scheme, int Timeout);
        BasicResult.SendData SendDataEx(Alias senderNumber, Node receiverNode, byte[] cmdData, TransmitOptions txOptions, TransmitSecurityOptions txSecOptions, SecuritySchemes scheme, TransmitOptions2 txOptions2, int Timeout);
        BasicResult.Transmit SendNodeInfo(Alias deviceNumber, Node receiverNode, TransmitOptions txOptions);
        void SetActiveSecuritySchemeForNode(Alias deviceNumber, Node node, SecuritySchemes activeSecurityScheme);
        void SetActiveSecuritySchemeForNode(Alias deviceNumber, Node node, SecuritySchemes activeSecurityScheme, bool isLRScheme);
        void SetSecurityForNode(Alias deviceNumber, Node node, bool isEnable);
        void SetSecurityVersionForBridge(Alias deviceNumber, byte VirtualNodeId, Node node, SecurityVersions securityVersion);
        void SetSecurityVersionForNode(Alias deviceNumber, Node node, SecurityVersions securityVersion);
        BasicResult.SetSucNodeId SetSUCNodeID(Alias controllerNumber, Node node, bool isEnable, byte capabilities, bool isLowPower);
        BasicResult.Transmit SendDataMulti(Alias senderNumber, Node[] receiverNodeIds, byte[] data, TransmitOptions txOptions);
        BasicResult.Transmit SendDataMultiEx(Alias senderNumber, Node[] receiverNodeIds, byte groupId, byte[] data, SecuritySchemes scheme);
        BasicResult.Transmit SendDataMultiBridge(Alias bridgeNumber, Node srcNode, Node[] receiverNodeIds, byte[] data, TransmitOptions txOptions);
        BasicResult.SetPriorityRoute SetPriorityRoute(Alias controllerNumber, Node node, Node priority0, Node priority1, Node priority2, Node priority3, byte routespeed);
        BasicResult.Transmit SendNop(Alias senderNumber, Node node, TransmitOptions txOptions);
        BasicResult.Void SetRoutingInfo(Alias controllerNumber, Node node, byte[] nodeMask);
        byte ActivteFirmwareUpdate(Alias deviceNumber, Node node, string filePath);
        BasicResult.RequestNodeNeighborUpdate RequestNodeNeighborDiscovery(Alias controllerNumber, Node node, byte nodeType);
    }
}
