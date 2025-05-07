using System.Linq;
using System.Collections.Generic;
using ZWave;
using ZWave.BasicApplication.Operations;
using ZWave.BasicApplication.Tasks;
using System;

namespace Zats.Services
{
    public static class BasicActionResult
    {
        public class Void : BasicResult.Void
        {
            public Void(ActionResult value)
            {
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
            }

            public Void(bool isStateCompleted)
            {
                Init(isStateCompleted, null);
            }
        }

        public class ExpectZW : BasicResult.ExpectZW
        {
            public ExpectZW(ExpectZWResult value)
            {
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                Data = value.Data;
            }
        }

        public class MemoryGetId : BasicResult.MemoryGetId
        {
            public MemoryGetId(MemoryGetIdResult value)
            {
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                Node = value.Node;
                NodeId = (byte)value.Node.Id;
                HomeId = value.HomeId;
            }
        }

        public class AddNodeWithLearnMode : BasicResult.AddNodeWithLearnMode
        {
            public AddNodeWithLearnMode(AddRemoveNodeResult value, SetLearnModeResult learnResult)
            {
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                NodeId = (byte)value.Node.Id;
                Node = value.Node;
                Basic = value.Basic;
                Generic = value.Generic;
                Specific = value.Specific;
                SupportedCommandClasses = value.CommandClasses;
                SubstituteStatus = (SubstituteStatuses)value.SubstituteStatus;
                AddRemoveNodeStatus = (AddRemoveNodeStatuses)value.AddRemoveNodeStatus;
                IsEndDevice = value.IsEndDevice;
                RoleType = value.RoleType;
                if (value.SecuritySchemes != null)
                {
                    SecuritySchemes = value.SecuritySchemes.Select(x => (SecuritySchemes)x).ToArray();
                }
                else
                {
                    SecuritySchemes = new SecuritySchemes[0];
                }
                if (learnResult != null)
                    SetLearnMode = new SetLearnMode(learnResult);
            }
        }

        public class AddNode : BasicResult.AddNode
        {
            public AddNode(AddRemoveNodeResult value)
            {
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                NodeId = (byte)value.Node.Id;
                Node = value.Node;
                Basic = value.Basic;
                Generic = value.Generic;
                Specific = value.Specific;
                SupportedCommandClasses = value.CommandClasses;
                SubstituteStatus = (SubstituteStatuses)value.SubstituteStatus;
                DSK = value.DSK;
                AddRemoveNodeStatus = (AddRemoveNodeStatuses)value.AddRemoveNodeStatus;
                IsEndDevice = value.IsEndDevice;
                RoleType = value.RoleType;
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

        public class RemoveNode : BasicResult.RemoveNode
        {
            public RemoveNode(AddRemoveNodeResult value)
            {
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                NodeId = (byte)value.Node.Id;
                Basic = value.Basic;
                Generic = value.Generic;
                Specific = value.Specific;
                SupportedCommandClasses = value.CommandClasses;
            }
        }

        public class IncludeNode : BasicResult.IncludeNode
        {
            public IncludeNode(InclusionResult value)
            {
                if (value != null)
                {
                    Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                    AddNodeResult = new AddNode(value.AddRemoveNode);
                    SetWakeUpInterval = new SendData(value.SetWakeUpInterval);
                }
            }
        }

        public class Transmit : BasicResult.Transmit
        {
            public Transmit(TransmitResult value)
            {
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                TransmitStatus = (TransmitStatuses)value.TransmitStatus;
            }
        }

        public class SendData : BasicResult.SendData
        {
            public SendData(SendDataResult value)
            {
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                FuncId = value.FuncId;
                TransmitStatus = (TransmitStatuses)value.TransmitStatus;
                HasTxTransmitReport = value.HasTxTransmitReport;
                TransmitTicks = value.TransmitTicks;
                RepeatersCount = value.RepeatersCount;
                RssiValuesIncoming = value.RssiValuesIncoming;
                AckChannelNo = value.AckChannelNo;
                LastTxChannelNo = value.LastTxChannelNo;
                RouteScheme = (RoutingSchemes)value.RouteScheme;
                Repeaters = value.Repeaters;
                RouteSpeed = value.RouteSpeed;
                RouteTries = value.RouteTries;
                LastFailedLinkFrom = value.LastFailedLinkFrom;
                LastFailedLinkTo = value.LastFailedLinkTo;
                UsedTxpower = value.UsedTxpower;
                MeasuredNoiseFloor = value.MeasuredNoiseFloor;
                AckDestinationUsedTxPower = value.AckDestinationUsedTxPower;
                DestinationAckMeasuredRSSI = value.DestinationAckMeasuredRSSI;
                DestinationckMeasuredNoiseFloor = value.DestinationckMeasuredNoiseFloor;

                TxSubstituteInfo = new SubstitutionInfo() { SubstituteStatus = (SubstituteStatuses)value.TxSubstituteStatus };
            }
            public SendData(TransmitResult value)
            {
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                TransmitStatus = (TransmitStatuses)value.TransmitStatus;
            }
        }

        public class RequestData : BasicResult.RequestData
        {
            public RequestData(RequestDataResult value)
            {
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                TransmitStatus = (TransmitStatuses)value.TransmitStatus;
                HasTxTransmitReport = value.HasTxTransmitReport;
                TransmitTicks = value.TransmitTicks;
                RepeatersCount = value.RepeatersCount;
                RssiValuesIncoming = value.RssiValuesIncoming;
                AckChannelNo = value.AckChannelNo;
                LastTxChannelNo = value.LastTxChannelNo;
                RouteScheme = (RoutingSchemes)value.RouteScheme;
                Repeaters = value.Repeaters;
                RouteSpeed = value.RouteSpeed;
                RouteTries = value.RouteTries;
                LastFailedLinkFrom = value.LastFailedLinkFrom;
                LastFailedLinkTo = value.LastFailedLinkTo;
                UsedTxpower = value.UsedTxpower;
                MeasuredNoiseFloor = value.MeasuredNoiseFloor;
                AckDestinationUsedTxPower = value.AckDestinationUsedTxPower;
                DestinationAckMeasuredRSSI = value.DestinationAckMeasuredRSSI;
                DestinationckMeasuredNoiseFloor = value.DestinationckMeasuredNoiseFloor;
                TxSubstituteInfo = new SubstitutionInfo() { SubstituteStatus = (SubstituteStatuses)value.TxSubstituteStatus };

                Node = value.Node;
                IsBroadcast = value.IsBroadcast;
                Command = value.Command;
                RxRssi = value.RxRssi;
                RxSecurityScheme = (SecuritySchemes)value.RxSecurityScheme;
                RxSubstituteInfo = new SubstitutionInfo() { SubstituteStatus = (SubstituteStatuses)value.RxSubstituteStatus };
            }
        }

        public class IncludeNodeWithLearnMode : BasicResult.IncludeNodeWithLearnMode
        {
            public IncludeNodeWithLearnMode(InclusionResult value)
            {
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                AddNodeResult = new AddNode(value.AddRemoveNode);
            }

            public IncludeNodeWithLearnMode(InclusionResult value, SetLearnModeResult learnResult)
            {
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                AddNodeResult = new AddNode(value.AddRemoveNode);
                if (learnResult != null)
                {
                    SetLearnMode = new BasicActionResult.SetLearnMode(learnResult);
                }
            }
        }

        public class ExtenssionInfo : COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION.TVG1
        {
            public SecuritySchemes EncryptedWithScheme { get; set; }
        }

        public class ExpectData : BasicResult.ExpectData
        {
            public ExpectData(ExpectDataResult value)
            {
                Options = (ReceiveStatuses)value.Options;
                Node = value.SrcNode;
                DestNode = value.DestNode;
                Command = value.Command;
                Rssi = value.Rssi;
                SecurityScheme = (SecuritySchemes)value.SecurityScheme;
                RxSubstituteInfo = new SubstitutionInfo() { SubstituteStatus = (SubstituteStatuses)value.SubstituteStatus };
                if (value.Extensions != null)
                {
                    Extensions = value.Extensions.ExtensionsList.
                        Select(x => new ExtenssionInfo()
                        {
                            extensionLength = x.extensionLength.Value,
                            extension = x.extension,
                            properties1 = new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION.TVG1.Tproperties1()
                            {
                                critical = x.properties1.critical,
                                moreToFollow = x.properties1.moreToFollow,
                                type = x.properties1.type
                            },
                            EncryptedWithScheme = SecuritySchemes.NONE
                        }).ToList();
                    Extensions.AddRange(
                        value.Extensions.EncryptedExtensionsList.Select(x => new ExtenssionInfo()
                        {
                            extensionLength = x.extensionLength.Value,
                            extension = x.extension,
                            properties1 = new COMMAND_CLASS_SECURITY_2.SECURITY_2_MESSAGE_ENCAPSULATION.TVG1.Tproperties1()
                            {
                                critical = x.properties1.critical,
                                moreToFollow = x.properties1.moreToFollow,
                                type = x.properties1.type
                            },
                            EncryptedWithScheme = (SecuritySchemes)value.SecurityScheme
                        }));
                }
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
            }
        }

        public class RequestNetworkUpdate : BasicResult.RequestNetworkUpdate
        {
            public RequestNetworkUpdate(RequestNetworkUpdateResult value)
            {
                Status = (SucUpdateStatuses)value.Status;
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
            }
        }
        public class RequestNodeInfo : BasicResult.RequestNodeInfo
        {
            public RequestNodeInfo(RequestNodeInfoResult value)
            {
                NodeId = (byte)value.Node.Id;
                NodeInfo = value.NodeInfo;
                Basic = value.Basic;
                Generic = value.Generic;
                Specific = value.Specific;
                CommandClasses = value.CommandClasses;
                SecureCommandClasses = value.SecureCommandClasses;
                if (value.SecuritySchemes != null && value.SecuritySchemes.Length > 0)
                {
                    SecuritySchemes = value.SecuritySchemes.Select(x => (SecuritySchemes)x).ToArray();
                }
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
            }
        }

        public class GetNodeInfo : BasicResult.GetNodeInfo
        {
            public GetNodeInfo(NodeInfoResult value)
            {
                NodeInfo = value.RequestNodeInfo.NodeInfo;
            }
        }

        public class SetSucNodeId : BasicResult.SetSucNodeId
        {
            public SetSucNodeId(SetSucNodeIdResult value)
            {
                RetVal = (SetSucReturnValues)value.RetVal;
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
            }
        }

        public class IsFailedNode : BasicResult.IsFailedNode
        {
            public IsFailedNode(IsFailedNodeResult value)
            {
                RetValue = value.RetValue;
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
            }
        }

        public class SetLearnMode : BasicResult.SetLearnMode
        {
            public SetLearnMode(SetLearnModeResult value)
            {
                Node = value.Node;
                NodeId = (byte)value.Node.Id;
                SubstituteStatus = (SubstituteStatuses)value.SubstituteStatus;
                if (value.SecuritySchemes != null)
                {
                    SecuritySchemes = value.SecuritySchemes.Select(x => (SecuritySchemes)x).ToArray();
                }
                else
                {
                    SecuritySchemes = new SecuritySchemes[0];
                }
                LearnModeStatus = value.LearnModeStatus;
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
            }
        }

        public class RequestNodeNeighborUpdate : BasicResult.RequestNodeNeighborUpdate
        {
            public RequestNodeNeighborUpdate(RequestNodeNeighborUpdateResult value)
            {
                NeighborUpdateStatus = (RequestNeighborUpdateStatuses)value.NeighborUpdateStatus;
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
            }
        }

        public class ExpectSmartStartAdd : BasicResult.ExpectSmartStartAdd
        {
            public ExpectSmartStartAdd(InclusionResult value)
            {
                if (value != null)
                {
                    Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                    if (value.AddRemoveNode == null)
                    {
                        value.AddRemoveNode = new AddRemoveNodeResult();
                        value.SetWakeUpInterval = new SendDataResult();
                    }
                    AddNodeResult = new AddNode(value.AddRemoveNode);
                }
            }
        }

        public class AssociationGroupCollection : BasicResult.AssociationGetGroups
        {
            public AssociationGroupCollection(AssociationGroupsResult value)
            {
                if (value != null && value.Groups != null)
                {
                    Groups = new List<AssociationGroup>(value.Groups);
                    //DeviceId = result.DeviceId;
                    Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                }
            }
        }

        public class AssociationNodeCollection : BasicResult.AssociationGetNodes
        {
            public AssociationNodeCollection(AssociationNodesResult value)
            {
                if (value != null && value.Nodes != null)
                {
                    Nodes = value.Nodes;
                    Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                }
            }
        }

        public class AssociationGroupsResult : ActionResult
        {
            public AssociationGroupsResult()
            {
                Groups = new List<AssociationGroup>();
            }
            //public byte DeviceId { get; set; }
            public List<AssociationGroup> Groups { get; set; }

        }

        public class AssociationNodesResult : ActionResult
        {
            public AssociationNodesResult()
            {
                Nodes = new List<ushort>();
            }
            public List<ushort> Nodes { get; set; }
        }

        public class FirmwareUpdateNvmInit : BasicResult.FirmwareUpdateNvmInit
        {
            public FirmwareUpdateNvmInit(FirmwareUpdateNvmInitResult value)
            {
                if (value != null)
                {
                    IsSupported = value.IsSupported;
                    Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                }
            }
        }

        public class FirmwareUpdateNvmSetNewImage : BasicResult.FirmwareUpdateNvmSetNewImage
        {
            public FirmwareUpdateNvmSetNewImage(FirmwareUpdateNvmSetNewImageResult value)
            {
                if (value != null)
                {
                    IsSet = value.IsSet;
                    Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                }
            }
        }

        public class FirmwareUpdateNvmGetNewImage : BasicResult.FirmwareUpdateNvmGetNewImage
        {
            public FirmwareUpdateNvmGetNewImage(FirmwareUpdateNvmGetNewImageResult value)
            {
                if (value != null)
                {
                    IsNewImage = value.IsNewImage;
                    Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                }
            }
        }

        public class FirmwareUpdateNvmUpdateCrc16 : BasicResult.FirmwareUpdateNvmUpdateCrc16
        {
            public FirmwareUpdateNvmUpdateCrc16(FirmwareUpdateNvmUpdateCrc16Result value)
            {
                if (value != null)
                {
                    Crc16 = value.Crc16;
                    Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                }
            }
        }

        public class FirmwareUpdateNvmIsValidCrc16 : BasicResult.FirmwareUpdateNvmIsValidCrc16
        {
            public FirmwareUpdateNvmIsValidCrc16(FirmwareUpdateNvmIsValidCrc16Result value)
            {
                if (value != null)
                {
                    IsValid = value.IsValid;
                    Crc16 = value.Crc16;
                    Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                }
            }
        }

        public class FirmwareUpdateNvmWrite : BasicResult.FirmwareUpdateNvmWrite
        {
            public FirmwareUpdateNvmWrite(bool isOk)
            {
                IsOk = isOk;
                Init(isOk, null);
            }

            public FirmwareUpdateNvmWrite(FirmwareUpdateNvmWriteResult value)
            {
                if (value != null)
                {
                    IsOk = value.IsOk;
                    Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                }
            }
        }


        public class FirmwareUpdateSendXModem : BasicResult.FirmwareUpdateSendXModem
        {
            public FirmwareUpdateSendXModem(bool isOk)
            {
                IsOk = isOk;
                Init(isOk, null);
            }

            public FirmwareUpdateSendXModem(ActionResult value)
            {
                if (value != null)
                {
                    //IsOk = result.IsOk;
                    Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                }
            }
        }

        public class MaxPayloadSize : BasicResult.MaxPayloadSize
        {
            public MaxPayloadSize(GetMaxPayloadSizeResult value)
            {
                ReturnValue = value.MaxPayloadSize;
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
            }
        }

        public class ReturnValue : BasicResult.ReturnValue
        {
            public ReturnValue(ReturnValueResult value)
            {
                ByteValue = value.ByteValue;
                ByteArray = value.ByteArray;
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
            }
        }

        public class GetRfRegion : BasicResult.GetRfRegion
        {
            public GetRfRegion(RfRegionGetResult value)
            {
                RfRegion = value.RfRegion.ToRfRegion();
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
            }
        }

        internal class ClearNetworkStats : BasicResult.ClearNetworkStats
        {
            public ClearNetworkStats(ClearNetworkStatsResult value)
            {
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                RetValue = value.RetValue;
            }
        }

        internal class GetNetworkStats : BasicResult.GetNetworkStats
        {
            public GetNetworkStats(GetNetworkStatsResult res)
            {
                Init(res.IsStateCompleted, res.TraceLog);
                RFTxFrames = res.RFTxFrames;
                RFTxLBTBackOffs = res.RFTxLBTBackOffs;
                RFRxFrames = res.RFRxFrames;
                RFRxLRCErrors = res.RFRxLRCErrors;
                RFRxCRC16Errors = res.RFRxCRC16Errors;
                RFRxForeignHomeID = res.RFRxForeignHomeID;
            }
        }

        internal class GetTxTimer : BasicResult.GetTxTimer
        {
            public GetTxTimer(GetTxTimerResult value)
            {
                Init(value.IsStateCompleted, value.TraceLog);
                TxTimeChannel0 = value.TxTimeChannel0;
                TxTimeChannel1 = value.TxTimeChannel1;
                TxTimeChannel2 = value.TxTimeChannel2;
                TxTimeChannel3 = value.TxTimeChannel3;
                TxTimeChannel4 = value.TxTimeChannel4;
            }
        }

        public class BootstrapS2 : BasicResult.BootstrapS2
        {
            public BootstrapS2(ActionResult value, SecuritySchemes highestScheme)
            {
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
                HighestScheme = highestScheme;
            }

            public BootstrapS2(bool isStateCompleted, SecuritySchemes highestScheme)
            {
                Init(isStateCompleted, null);
                HighestScheme = highestScheme;
            }
        }

        public class GetBackgroundRssi : BasicResult.GetBackgroundRssi
        {
            public GetBackgroundRssi(GetBackgroundRssiResult value)
            {
                BackgroundRSSILevels = value.BackgroundRSSILevels;
                Init(value.IsStateCompleted, value.ElapsedMs, value.TraceLog);
            }
        }
    }
}
