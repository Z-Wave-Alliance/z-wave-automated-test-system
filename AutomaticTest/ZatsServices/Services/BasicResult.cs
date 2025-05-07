using ZWave.BasicApplication.Operations;
using System.Collections.Generic;
using System;
using static Zats.Services.BasicActionResult;
using ZWave;
using System.Linq;
using ZWave.Devices;
using System.Text;

namespace Zats.Services
{
    public partial class BasicResult
    {
        public class Void : _ActionResult
        {
        }

        public class BootstrapS2 : _ActionResult
        {
            public SecuritySchemes HighestScheme { get; set; }
        }

        public class ExpectZW : _ActionResult
        {
            public byte[] Data { get; set; }
        }

        public class Test
        {
            public int Total { get; set; }
            public int Fails { get; set; }

            public int SuccessPercentRate
            {
                get
                {
                    double k = Total - Fails;
                    return Total > 0 ? (int)(k * 100 / Total) : 0;
                }
            }

            public void Add(bool isSuccess)
            {
                Total++;
                if (!isSuccess)
                    Fails++;
            }

            internal void Reset()
            {
                Total = 0;
                Fails = 0;
            }
        }

        public class RequestData : SendData
        {
            public byte NodeId { get { return (byte)Node.Id; } }
            public Node Node { get; protected set; }
            public bool IsBroadcast { get; protected set; }
            public byte[] Command { get; protected set; }
            public sbyte RxRssi { get; protected set; }
            public SecuritySchemes RxSecurityScheme { get; protected set; }
            public SubstitutionInfo RxSubstituteInfo { get; set; }
        }

        public class ExpectData : _ActionResult
        {
            public ReceiveStatuses Options { get; set; }
            public byte NodeId { get { return (byte)Node.Id; } }
            public Node Node { get; protected set; }
            public byte DestNodeId { get { return (byte)DestNode.Id; } }
            public Node DestNode { get; protected set; }
            public bool IsBroadcast { get; protected set; }
            public byte[] Command { get; protected set; }
            public sbyte Rssi { get; protected set; }
            public SecuritySchemes SecurityScheme { get; protected set; }
            public SubstitutionInfo RxSubstituteInfo { get; set; }
            public List<ExtenssionInfo> Extensions { get; set; }
        }

        public class ListenControllerUpdate : _ActionResult
        {
            public IList<ApplicationControllerUpdate> ReceivedData { get; private set; }

            public ListenControllerUpdate(IList<ApplicationControllerUpdate> receivedData)
            {
                ReceivedData = receivedData;
            }
        }

        public class ListenData : _ActionResult
        {
            public IList<AchData> ReceivedData { get; private set; }

            public ListenData(IList<AchData> receivedData)
            {
                ReceivedData = receivedData;
            }
        }

        public class ExpectControllerUpdate : _ActionResult
        {
            public ControllerUpdateStatuses Status { get; set; }
            public ushort NodeId { get; set; }
            public byte[] Payload { get; set; }
            public ExpectControllerUpdate(ApplicationControllerUpdateResult value)
                : base(value)
            {
                Status = (ControllerUpdateStatuses)value.Status;
                NodeId = value.NodeId;
                Payload = value.Payload;
            }
        }

        public class AddNodeWithLearnMode : AddNode
        {
            public SetLearnMode SetLearnMode { get; set; }
        }

        public class IncludeNodeWithLearnMode : IncludeNode
        {
            public SetLearnMode SetLearnMode { get; set; }
        }

        public class IncludeNode : _ActionResult
        {
            public AddNode AddNodeResult { get; set; }
            public SendData SetWakeUpInterval { get; set; }
        }

        public class AddNode : _ActionResult
        {
            public bool IsEndDevice { get; protected set; }
            public byte NodeId { get; protected set; }
            public Node Node { get; protected set; }
            public byte Basic { get; protected set; }
            public byte Generic { get; protected set; }
            public byte Specific { get; protected set; }
            public byte[] SupportedCommandClasses { get; protected set; }
            public SubstituteStatuses SubstituteStatus { get; protected set; }
            public AddRemoveNodeStatuses AddRemoveNodeStatus { get; protected set; }
            public byte RoleType { get; set; }
            public SecuritySchemes[] SecuritySchemes { get; set; }
            public byte[] DSK { get; set; }
        }

        public class RemoveNode : _ActionResult
        {
            public byte NodeId { get; protected set; }
            public byte Basic { get; protected set; }
            public byte Generic { get; protected set; }
            public byte Specific { get; protected set; }
            public byte[] SupportedCommandClasses { get; protected set; }
        }

        public class SerialApiGetCapabilities : _ActionResult
        {
            public byte SerialApplicationVersion { get; set; }
            public byte SerialApplicationRevision { get; set; }
            public ushort ManufacturerId { get; set; }
            public ushort ManufacturerProductType { get; set; }
            public ushort ManufacturerProductId { get; set; }
            public SerialApiCommands[] SupportedSerialApiCommands { get; set; }
            public SerialApiGetCapabilities(SerialApiGetCapabilitiesResult value)
                : base(value)
            {
                SerialApplicationVersion = value.SerialApplicationVersion;
                SerialApplicationRevision = value.SerialApplicationRevision;
                ManufacturerId = value.ManufacturerId;
                ManufacturerProductType = value.ManufacturerProductType;
                ManufacturerProductId = value.ManufacturerProductId;
                if (value.SupportedSerialApiCommands != null)
                {
                    SupportedSerialApiCommands = value.SupportedSerialApiCommands.Select(x => (SerialApiCommands)x).ToArray();
                }
            }
        }

        public class NvmBackupRestoreOpen : _ActionResult
        {
            public NvmBackupRestoreStatuses Status { get; set; }
            public short NvmSize { get; set; }

            public NvmBackupRestoreOpen(NvmBackupRestoreOpenResult value)
                : base(value)
            {
                Status = (NvmBackupRestoreStatuses)value.Status;
                NvmSize = value.NvmSize;
            }
        }

        public class NvmBackupRestoreRead : _ActionResult
        {
            public NvmBackupRestoreStatuses Status { get; set; }
            public byte[] Data { get; set; }

            public NvmBackupRestoreRead(NvmBackupRestoreReadResult value)
                : base(value)
            {
                Status = (NvmBackupRestoreStatuses)value.Status;
                Data = value.Data;
            }
        }

        public class NvmBackupRestoreWrite : _ActionResult
        {
            public NvmBackupRestoreStatuses Status { get; set; }

            public NvmBackupRestoreWrite(NvmBackupRestoreWriteResult value)
                : base(value)
            {
                Status = (NvmBackupRestoreStatuses)value.Status;
            }
        }

        public class SetLearnMode : _ActionResult
        {
            public Node Node { get; protected set; }
            public byte NodeId { get; protected set; }
            public SubstituteStatuses SubstituteStatus { get; set; }
            public SecuritySchemes[] SecuritySchemes { get; set; }
            public LearnModeStatuses LearnModeStatus { get; set; }
        }

        public class Transmit : _ActionResult
        {
            public TransmitStatuses TransmitStatus { get; set; }
            public byte RouteTries { get; set; }
        }

        public class SendData : Transmit
        {
            public byte FuncId { get; set; }
            public bool HasTxTransmitReport { get; set; }
            public ushort TransmitTicks { get; set; }
            public byte RepeatersCount { get; set; }
            public sbyte[] RssiValuesIncoming { get; set; }
            public byte AckChannelNo { get; set; }
            public byte LastTxChannelNo { get; set; }
            public RoutingSchemes RouteScheme { get; set; }
            public byte[] Repeaters { get; set; }
            public byte RouteSpeed { get; set; }

            public byte LastFailedLinkFrom { get; set; }
            public byte LastFailedLinkTo { get; set; }
            public sbyte UsedTxpower { get; set; }
            public sbyte MeasuredNoiseFloor { get; set; }
            public sbyte AckDestinationUsedTxPower { get; set; }
            public sbyte DestinationAckMeasuredRSSI { get; set; }
            public sbyte DestinationckMeasuredNoiseFloor { get; set; }
            public SubstitutionInfo TxSubstituteInfo { get; set; }
        }

        public class MemoryGetId : _ActionResult
        {
            public Node Node { get; protected set; }
            public byte NodeId { get; protected set; }
            public byte[] HomeId { get; protected set; }
        }

        public class GetVersion : _ActionResult
        {
            public string Version { get; set; }
            public LibTypes Library { get; set; }
            public string VersionNumbers { get; set; }
            public GetVersion(VersionResult value)
                : base(value)
            {
                Version = value.Version;
                Library = (LibTypes)value.Library;
                VersionNumbers = value.VersionNumbers;
            }
        }

        public class GetSecurityKeys : _ActionResult
        {
            public byte SecurityKeysMask { get; set; }
            public SecuritySchemes[] SecuritySchemes { get; set; }
            public GetSecurityKeys(GetSecurityKeysResult value)
                : base(value)
            {
                SecurityKeysMask = value.SecurityKeysMask;
                if (value.SecuritySchemes != null)
                {
                    SecuritySchemes = value.SecuritySchemes.Select(x => (SecuritySchemes)x).ToArray();
                }
                else
                {
                    SecuritySchemes = new SecuritySchemes[0];
                }
            }
        }

        public class GetSecurityS2PublicDSK : _ActionResult
        {
            public byte[] DSK { get; set; }

            public GetSecurityS2PublicDSK(GetSecurityS2PublicDSKResult value)
            {
                if (value.DSK != null && value.DSK.Length == 16)
                {
                    DSK = new byte[value.DSK.Length];
                    value.DSK.CopyTo(DSK, 0);
                }
            }
        }

        public class GetControllerCapabilities : _ActionResult
        {
            //  IS_SECONDARY = 0x01,
            //  ON_OTHER_NETWORK = 0x02,
            //  NODEID_SERVER_PRESENT = 0x04,
            //  IS_REAL_PRIMARY = 0x08,
            //  IS_SUC = 0x10
            public byte ControllerCapability { get; protected set; }
            public GetControllerCapabilities(GetControllerCapabilitiesResult value)
                : base(value)
            {
                ControllerCapability = value.ControllerCapability;
            }

            public bool IsSecondary
            {
                get
                {
                    return (ControllerCapability & 0x01) > 0;
                }
            }

            public bool IsOnOtherNetwork
            {
                get
                {
                    return (ControllerCapability & 0x02) > 0;
                }
            }

            public bool IsNodeIdServerPresent
            {
                get
                {
                    return (ControllerCapability & 0x04) > 0;
                }
            }

            public bool IsRealPrimary
            {
                get
                {
                    return (ControllerCapability & 0x08) > 0;
                }
            }

            public bool IsSUC
            {
                get
                {
                    return (ControllerCapability & 0x10) > 0;
                }
            }
        }

        public class GetProtocolInfo : _ActionResult
        {
            public byte NodeId { get; protected set; }
            public byte Capability { get; protected set; }
            public byte Security { get; protected set; }
            public byte Basic { get; protected set; }
            public byte Generic { get; protected set; }
            public byte Specific { get; protected set; }
            public GetProtocolInfo(GetNodeProtocolInfoResult value)
                : base(value)
            {
                NodeId = (byte)value.Node.Id;
                Capability = value.NodeInfo.Capability;
                Security = value.NodeInfo.Security;
                Basic = value.NodeInfo.Basic;
                Generic = value.NodeInfo.Generic;
                Specific = value.NodeInfo.Specific;
            }
        }

        public class AreNodesNeighbours : _ActionResult
        {
            public bool RetValue { get; protected set; }
            public AreNodesNeighbours(ReturnValueResult value)
                : base(value)
            {
                RetValue = value.ByteValue > 0;
            }
        }

        public class GetNeighborCount : _ActionResult
        {
            public byte RetValue { get; protected set; }
            public GetNeighborCount(GetNeighborCountResult value)
                : base(value)
            {
                RetValue = value.RetValue;
            }
        }

        public class GetProtocolStatus : _ActionResult
        {
            public ProtocolStatuses ProtocolStatus { get; protected set; }
            public GetProtocolStatus(GetProtocolStatusResult value)
                : base(value)
            {
                ProtocolStatus = (ProtocolStatuses)value.ProtocolStatus;
            }
        }

        public class GetRandomWord : _ActionResult
        {
            public byte[] RetValue { get; protected set; }
            public GetRandomWord(GetRandomWordResult value)
                : base(value)
            {
                RetValue = value.RetValue;
            }
        }

        public class GetRoutingInfo : _ActionResult
        {
            public byte[] RoutingNodes { get; protected set; }
            public GetRoutingInfo(GetRoutingInfoResult value)
                : base(value)
            {
                if (value.RoutingNodes != null)
                {
                    RoutingNodes = value.RoutingNodes.Select(x => (byte)x.Id).ToArray();
                }
            }
        }

        public class GetVirtualNodes : _ActionResult
        {
            public byte[] NodeIds { get; protected set; }
            public GetVirtualNodes(GetVirtualNodesResult value)
                : base(value)
            {
                NodeIds = value.NodeIds.ToArray();
            }
        }

        public class IsFailedNode : _ActionResult
        {
            public bool RetValue { get; protected set; }
        }

        public class IsNodeWithinDirectRange : _ActionResult
        {
            public bool RetValue { get; protected set; }
            public IsNodeWithinDirectRange(IsNodeWithinDirectRangeResult value)
                : base(value)
            {
                RetValue = value.RetValue;
            }
        }

        public class IsVirtualNode : _ActionResult
        {
            public bool RetValue { get; protected set; }
            public IsVirtualNode(IsVirtualNodeResult value)
                : base(value)
            {
                RetValue = value.RetValue;
            }
        }

        public class MemoryGetBuffer : _ActionResult
        {
            public byte[] RetValue { get; protected set; }
            public MemoryGetBuffer(MemoryGetBufferResult value)
                : base(value)
            {
                RetValue = value.RetValue;
            }
        }

        public class MemoryGetByte : _ActionResult
        {
            public byte RetValue { get; protected set; }
            public MemoryGetByte(MemoryGetByteResult value)
                : base(value)
            {
                RetValue = value.RetValue;
            }
        }

        public class RandomByte : _ActionResult
        {
            public byte RetValue { get; protected set; }
            public RandomByte(RandomResult value)
                : base(value)
            {
                RetValue = value.RetValue;
            }
        }

        public class RequestNetworkUpdate : _ActionResult
        {
            public SucUpdateStatuses Status { get; protected set; }
        }

        public class RequestNodeInfo : _ActionResult
        {
            public byte NodeId { get; set; }
            public byte[] NodeInfo { get; set; }
            public byte Basic { get; set; }
            public byte Generic { get; set; }
            public byte Specific { get; set; }
            public byte[] CommandClasses { get; set; }
            public byte[] SecureCommandClasses { get; set; }
            public SecuritySchemes[] SecuritySchemes { get; set; }
        }

        public class GetNodeInfo : _ActionResult
        {
            public byte[] NodeInfo { get; protected set; }
            //public ZWave.BasicApplication.Tasks.NodeInfoResult NodeInfo { get; protected set; }
        }


        public class SerialApiGetInitData : _ActionResult
        {
            public byte SerialApiVersion { get; set; }
            public byte SerialApiCapability { get; set; }
            public ChipSeries ChipType { get; set; }
            public byte ChipRevision { get; set; }
            public byte[] IncludedNodes { get; set; }

            public SerialApiGetInitData(SerialApiGetInitDataResult value)
                : base(value)
            {
                SerialApiVersion = value.SerialApiVersion;
                SerialApiCapability = value.SerialApiCapability;
                ChipType = (ChipSeries)value.ChipType;
                ChipRevision = value.ChipRevision;
                if (value.IncludedNodes != null)
                {
                    IncludedNodes = value.IncludedNodes.Select(x => (byte)x.Id).ToArray();
                }
            }
        }

        public class SerialApiGetLRNodes : _ActionResult
        {
            public Node[] IncludedNodes { get; set; }

            public SerialApiGetLRNodes(SerialApiGetLRNodesResult value)
                : base(value)
            {
                IncludedNodes = value.IncludedNodes.Select(x => (Node)x).ToArray();
            }
        }

        public class RequestNodeNeighborUpdate : _ActionResult
        {
            public RequestNeighborUpdateStatuses NeighborUpdateStatus { get; protected set; }
        }

        public class RFPowerLevelGet : _ActionResult
        {
            public byte PowerLevel { get; protected set; }
            public RFPowerLevelGet(RFPowerLevelGetResult value)
                : base(value)
            {
                PowerLevel = value.PowerLevel;
            }
        }

        public class TypeLibrary : _ActionResult
        {
            public LibTypes Library { get; protected set; }
            public TypeLibrary(TypeLibraryResult value)
                : base(value)
            {
                Library = (LibTypes)value.Library;
            }
        }

        public class SetPriorityRoute : _ActionResult
        {
            public byte[] Repeaters { get; protected set; }
            public SetPriorityRoute(SetPriorityRouteResult value)
                : base(value)
            {
                Repeaters = value.PriorityRoute;
            }
        }

        public class AssignReturnRoute : _ActionResult
        {
            public byte RetStatus { get; protected set; }
            public AssignReturnRoute(AssignReturnRouteResult value)
                : base(value)
            {
                RetStatus = value.RetStatus;
            }
        }

        public class AssignSucReturnRoute : _ActionResult
        {
            public byte RetStatus { get; protected set; }
            public AssignSucReturnRoute(AssignSucReturnRouteResult value)
                : base(value)
            {
                RetStatus = value.RetStatus;
            }
        }

        public class GetPriorityRoute : _ActionResult
        {
            public Node NodeId { get; set; }
            public bool RetVal { get; set; }
            public byte[] Repeaters { get; set; }
            public byte RouteSpeed { get; set; }
            public GetPriorityRoute(GetPriorityRouteResult value)
                : base(value)
            {
                NodeId = value.NodeId;
                RetVal = value.RetVal;
                if (value.PriorityRoute != null)
                {
                    Repeaters = value.PriorityRoute.Select(x => (byte)x.Id).ToArray();
                }
                RouteSpeed = value.RouteSpeed;
            }
        }
        public class GetSucNodeId : _ActionResult
        {
            public byte SucNodeId { get; set; }
            public GetSucNodeId(GetSucNodeIdResult value)
                : base(value)
            {
                SucNodeId = (byte)value.SucNode.Id;
            }
        }

        public class GetTransmitCount : _ActionResult
        {
            public byte TxCounter { get; set; }
            public GetTransmitCount(GetTransmitCountResult value)
                : base(value)
            {
                TxCounter = value.TxCounter;
            }
        }

        public class SetSucNodeId : _ActionResult
        {
            public SetSucReturnValues RetVal { get; set; }
        }

        public class ExpectSmartStartAdd : _ActionResult
        {
            public AddNode AddNodeResult { get; set; }
        }

        public class AssociationGetGroups : _ActionResult
        {
            public AssociationGetGroups()
            {
                Groups = new List<AssociationGroup>();
            }
            public List<AssociationGroup> Groups { get; set; }
        }

        public class AssociationGetNodes : _ActionResult
        {
            public List<ushort> Nodes { get; set; }
        }

        public class FirmwareUpdateNvmInit : _ActionResult
        {
            public bool IsSupported { get; set; }
        }

        public class FirmwareUpdateNvmSetNewImage : _ActionResult
        {
            public bool IsSet { get; set; }
        }

        public class FirmwareUpdateNvmGetNewImage : _ActionResult
        {
            public bool IsNewImage { get; set; }
        }

        public class FirmwareUpdateNvmUpdateCrc16 : _ActionResult
        {
            public ushort Crc16 { get; set; }
        }

        public class FirmwareUpdateNvmIsValidCrc16 : _ActionResult
        {
            public bool IsValid { get; set; }
            public ushort Crc16 { get; set; }
        }

        public class FirmwareUpdateNvmWrite : _ActionResult
        {
            public bool IsOk { get; set; }
        }


        public class FirmwareUpdateSendXModem : _ActionResult
        {
            public bool IsOk { get; set; }
        }

        public class MaxPayloadSize : _ActionResult
        {
            public byte ReturnValue { get; set; }
        }

        public class ReturnValue : _ActionResult
        {
            public byte ByteValue { get; set; }
            public byte[] ByteArray { get; set; }
        }

        public class GetRfRegion : _ActionResult
        {
            public RfRegions RfRegion { get; set; }
        }

        public class ClearNetworkStats : _ActionResult
        {
            public byte RetValue { get; set; }
        }

        public class GetNetworkStats : _ActionResult
        {
            public ushort RFTxFrames { get; set; }
            public ushort RFTxLBTBackOffs { get; set; }
            public ushort RFRxFrames { get; set; }
            public ushort RFRxLRCErrors { get; set; }
            public ushort RFRxCRC16Errors { get; set; }
            public ushort RFRxForeignHomeID { get; set; }

            public string GetReport()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Network Statistics:");
                sb.AppendLine($"   RFTxFrames: {RFTxFrames}");
                sb.AppendLine($"   RFTxLBTBackOffs: {RFTxLBTBackOffs}");
                sb.AppendLine($"   RFRxFrames: {RFRxFrames}");
                sb.AppendLine($"   RFRxLRCErrors: {RFRxLRCErrors}");
                sb.AppendLine($"   RFRxCRC16Errors: {RFRxCRC16Errors}");
                sb.AppendLine($"   RFRxForeignHomeID: {RFRxForeignHomeID}");
                return sb.ToString();
            }
        }

        public class GetTxTimer : _ActionResult
        {
            public int TxTimeChannel0 { get; set; }
            public int TxTimeChannel1 { get; set; }
            public int TxTimeChannel2 { get; set; }
            public int TxTimeChannel3 { get; set; }
            public int TxTimeChannel4 { get; set; }
        }

        public class DefaultTxPowerLevelGet : _ActionResult
        {
            public short NormalTxPower { get; private set; }
            public short Measured0dBmPower { get; private set; }

            public DefaultTxPowerLevelGet(DefaultTxPowerLevelGetResult value)
                : base(value)
            {
                NormalTxPower = value.NormalTxPower;
                Measured0dBmPower = value.NormalTxPower;
            }
        }

        public class GetLRChannel : _ActionResult
        {
            public LongRangeChannels Channel { get; private set; }
            public GetLRChannel(GetLRChannelResult value)
                : base(value)
            {
                Channel = (LongRangeChannels)value.Channel;
            }
        }

        public class GetDcdcMode : _ActionResult
        {
            public DcdcModes DcdcMode { get; private set; }
            public GetDcdcMode(DcdcModeGetResult value)
                : base(value)
            {
                DcdcMode = (DcdcModes)value.DcdcMode;
            }
        }

        public class GetBackgroundRssi : _ActionResult
        {
            public byte[] BackgroundRSSILevels { get; set; }
        }


        public class GetRadioPTI : _ActionResult
        {
            public bool IsEnabled { get; protected set; }
            public GetRadioPTI(GetRadioPTIResult value)
                : base(value)
            {
                IsEnabled = value.IsEnabled;
            }
        }

        public class GetMaxLrTxPower : _ActionResult
        {
            public MaxLrTxPower MaxLrTxPower { get; protected set; }
            public GetMaxLrTxPower(GetMaxLrTxPowerResult value)
              : base(value)
            {
                MaxLrTxPower = (MaxLrTxPower)value.Value;
            }
        }
    }

    public class ApplicationControllerUpdate
    {
        public ApplicationControllerUpdate(ZWave.BasicApplication.Operations.ApplicationControllerUpdateResult acuData)
        {
            Status = (ControllerUpdateStatuses)acuData.Status;
            NodeId = acuData.NodeId;
            Payload = acuData.Payload;
        }

        public ControllerUpdateStatuses Status { get; set; }
        public ushort NodeId { get; set; }
        public byte[] Payload { get; set; }
    }

    public class AchData
    {
        public AchData(ZWave.BasicApplication.Operations.AchData achData)
        {
            CommandType = (byte)achData.CommandType;
            Options = (ReceiveStatuses)achData.Options;
            SrcNode = achData.SrcNode;
            DstNode = achData.DstNode;
            Command = achData.Command.ToArray();
            Rssi = achData.Rssi;
            SecurityScheme = (SecuritySchemes)achData.SecurityScheme;
            SubstituteIncomingFlags = (SubstituteIncomingFlags)achData.SubstituteIncomingFlags;
            TimeStamp = achData.TimeStamp;
        }

        public byte CommandType { get; }
        public ReceiveStatuses Options { get; }
        public Node SrcNode { get; }
        public Node DstNode { get; }
        public byte[] Command { get; }
        public sbyte Rssi { get; }
        public SecuritySchemes SecurityScheme { get; }
        public SubstituteIncomingFlags SubstituteIncomingFlags { get; }
        public DateTime TimeStamp { get; }
    }

    public class SubstitutionInfo
    {
        public SubstituteStatuses SubstituteStatus { get; set; }
        public static implicit operator SubstituteStatuses(SubstitutionInfo si)
        {
            return si.SubstituteStatus;
        }
    }

    public struct Node : IEquatable<Node>, IComparable<Node>, IComparable, ICloneable
    {
        public ushort Id;
        public byte EndPointId;
        public bool IsBitAddress;
        public string UnId;

        public Node(ushort node, byte endpoint, bool isBitAddress)
        {
            Id = node;
            EndPointId = endpoint;
            IsBitAddress = isBitAddress;
            UnId = null;
        }

        public Node(ushort node, byte endpoint)
        {
            Id = node;
            EndPointId = endpoint;
            IsBitAddress = false;
            UnId = null;
        }

        public Node(ushort nodeId)
        {
            Id = nodeId;
            EndPointId = 0;
            IsBitAddress = false;
            UnId = null;
        }

        public Node(string unId)
        {
            Id = 0;
            EndPointId = 0;
            IsBitAddress = false;
            UnId = null;
        }

        public Node Parent
        {
            get { return new Node(Id); }
        }

        public static Node FF = new Node(0xFF);
        public static Node Empty = new Node(0);

        public override int GetHashCode()
        {
            return (Id << 8) + EndPointId;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Node))
                return false;

            Node nodeId = (Node)obj;
            return Equals(nodeId);
        }

        public bool Equals(Node other)
        {
            return GetHashCode() == other.GetHashCode();
        }

        public static bool operator !=(Node nodeId1, Node nodeId2)
        {
            return !nodeId1.Equals(nodeId2);
        }

        public static bool operator ==(Node nodeId1, Node nodeId2)
        {
            return nodeId1.Equals(nodeId2);
        }

        public static implicit operator Node(byte nodeId)
        {
            return new Node(nodeId);
        }

        public static implicit operator Node(ushort nodeId)
        {
            return new Node(nodeId);
        }

        public static implicit operator Node(NodeTag node)
        {
            return new Node(node.Id, node.EndPointId, node.IsBitAddress);
        }

        public static implicit operator NodeTag(Node node)
        {
            return new NodeTag(node.Id, node.EndPointId, node.IsBitAddress);
        }

        public override string ToString()
        {
            if (EndPointId == 0)
            {
                return Id.ToString();
            }
            else
            {
                return Id.ToString() + "." + EndPointId.ToString();
            }
        }

        public int CompareTo(Node other)
        {
            if (Id < other.Id)
                return -1;
            else if (Id > other.Id)
                return 1;
            else if (EndPointId < other.EndPointId)
                return -1;
            else if (EndPointId > other.EndPointId)
                return 1;
            else
                return 0;
        }

        public int CompareTo(object obj)
        {
            if (!(obj is Node))
                return 0;

            Node nodeId = (Node)obj;
            return CompareTo(nodeId);
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
