// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
// SPDX-FileCopyrightText: Z-Wave Alliance https://z-wavealliance.org
using System.Collections.Generic;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using Utils;
using ZWave.Devices;
using System;
using ZWave;
using System.Linq;
using Zats.Services.CommandClassesImpl.Definitions.Interfaces;

namespace Zats.Services
{
    [CommandClassName("Multi Channel Command Class")]
    [CommandClassId(COMMAND_CLASS_MULTI_CHANNEL_V2.ID)]
    [MinVersion(3)]
    [MaxVersion(4)]
    public class MultiChannelSupport : CommandClassSupport
    {
        private MultiChannelSupportData MultiChannelSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return MultiChannelSupportData; }
            set { MultiChannelSupportData = (MultiChannelSupportData)value; }
        }

        public List<IndividualEndPointData> IndividualEndPoints { get; set; }
        public List<AggregatedEndPointData> AggregatedEndPoints { get; set; }
        private int MaxEpsPerFindReport = 20;

        public MultiChannelSupport()
            : this(new List<IndividualEndPointData>() { }, new List<AggregatedEndPointData>() { })
        { }

        public MultiChannelSupport(List<IndividualEndPointData> individualEndPoints, List<AggregatedEndPointData> aggregatedEndPoints)
            : base(COMMAND_CLASS_MULTI_CHANNEL_V2.ID)
        {
            MultiChannelSupportData = new MultiChannelSupportData() { };
            IndividualEndPoints = individualEndPoints;
            AggregatedEndPoints = aggregatedEndPoints;
        }

        public MultiChannelSupport(List<IndividualEndPointEx> individualEndPoints, List<AggregatedEndPointData> aggregatedEndPoints)
            : base(COMMAND_CLASS_MULTI_CHANNEL_V2.ID)
        {
            MultiChannelSupportData = new MultiChannelSupportData() { };
            IndividualEndPoints = new List<IndividualEndPointData>();

            if (individualEndPoints != null)
            {
                foreach(IndividualEndPointEx epEx in individualEndPoints)
                {
                    IndividualEndPoints.Add(new IndividualEndPointData(epEx));
                }
            }
            AggregatedEndPoints = aggregatedEndPoints;
        }

        protected override void OnHandledDelayed(DataReceivedUnit ou)
        {
            ou.SetNextActionItems();

            if (MatchesDestinationEndPoint())
            {
                var sendDstNode = new NodeTag(ReceivedAchData.DstNode.Id, DstNode.EndPointId);
                var sendSrcNode = ReceivedAchData.SrcNode;
                byte[] command = ReceivedAchData.Command;
                var scheme = (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme;
                bool isHandlingAllowed = IsHandlingAllowed(Network, command, scheme, Secure);

                if (isHandlingAllowed && command != null && command.Length > 1 && MultiChannelSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_END_POINT_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareEndpointReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CAPABILITY_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareCapabilitiesReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_END_POINT_FIND.ID && !IsSupervised())
                    {
                        if (Version >= 3 && IsAnsweringAllowed())
                        {
                            COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_END_POINT_FIND cmd = command;
                            List<IndividualEndPointData> eps;
                            if (cmd.genericDeviceClass == 0xFF && cmd.specificDeviceClass == 0xFF)
                            {
                                eps = IndividualEndPoints;
                            }
                            else
                            {
                                eps = IndividualEndPoints.Where(x => x.GenericDeviceClass == cmd.genericDeviceClass &&
                                                           x.SpecificDeviceClass == cmd.specificDeviceClass).ToList();
                            }

                            List<SendDataExOperation> sendOperations = new List<SendDataExOperation>();

                            if (eps == null || eps.Count() == 0)
                            {
                                COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_END_POINT_FIND_REPORT rptF = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_END_POINT_FIND_REPORT
                                {
                                    genericDeviceClass = cmd.genericDeviceClass,
                                    specificDeviceClass = cmd.specificDeviceClass,
                                    reportsToFollow = 0,
                                    vg = new List<COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_END_POINT_FIND_REPORT.TVG>()
                                };
                                rptF.vg.Add(new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_END_POINT_FIND_REPORT.TVG
                                {
                                    properties1 = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_END_POINT_FIND_REPORT.TVG.Tproperties1
                                    {
                                        endPoint = 0
                                    }
                                });
                                sendOperations.Add(new SendDataExOperation(Network, sendDstNode, sendSrcNode, rptF, TxOptions, scheme));
                            }
                            else
                            {
                                int rptCount = 0;
                                int reportsToFollow = (eps.Count() - 1) / MaxEpsPerFindReport;

                                while (reportsToFollow >= 0)
                                {
                                    COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_END_POINT_FIND_REPORT rptF = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_END_POINT_FIND_REPORT
                                    {
                                        genericDeviceClass = cmd.genericDeviceClass,
                                        specificDeviceClass = cmd.specificDeviceClass,
                                        reportsToFollow = (byte) reportsToFollow,
                                        vg = new List<COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_END_POINT_FIND_REPORT.TVG>()
                                    };
                                    for (int i = 0; i < MaxEpsPerFindReport; i++)
                                    {
                                        if (eps.Count() > i + (MaxEpsPerFindReport * rptCount))
                                        {
                                            rptF.vg.Add(new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_END_POINT_FIND_REPORT.TVG
                                            {
                                                properties1 = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_END_POINT_FIND_REPORT.TVG.Tproperties1
                                                {
                                                    endPoint = eps[i + (MaxEpsPerFindReport * rptCount)].EndPointId
                                                }
                                            });
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    sendOperations.Add(new SendDataExOperation(Network, sendDstNode, sendSrcNode, rptF, TxOptions, scheme));
                                    rptCount++;
                                    reportsToFollow--;
                                }
                            }
                            ou.SetNextActionItems(sendOperations.ToArray());
                        }
                    }
                    else if (command[1] == COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_AGGREGATED_MEMBERS_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareAggregatedMembersReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP.ID)
                    {
                        rpt = PrepareCommandEncap(command, scheme);
                    }

                    if (rpt != null && IsAnsweringAllowed())
                    {
                        ApiOperation sendData = new SendDataExOperation(Network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command, sendDstNode.EndPointId);
                }
            }
        }

        private byte[] PrepareCommandEncap(byte[] encapsulatedCommand, ZWave.Enums.SecuritySchemes scheme)
        {
            byte[] data = null;
            List<byte> secureCCs = new List<byte>();
            var encapCmd = (COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP)encapsulatedCommand;
            var destEPId = encapCmd.properties2.destinationEndPoint;
            if (destEPId > 0 && destEPId <= IndividualEndPoints.Count)
            {
                var command = new List<byte>();
                command.Add(encapCmd.commandClass);
                command.Add(encapCmd.command);
                command.AddRange(encapCmd.parameter);

                IndividualEndPointData individualEndPointData = IndividualEndPoints.FirstOrDefault(x => x.EndPointId == destEPId);

                if (individualEndPointData != null)
                {
                    secureCCs = individualEndPointData.CommandClassDataList.Where(x => x.Secure == true && x.CommandClassId != COMMAND_CLASS_BASIC.ID).Select(x => x.CommandClassId).ToList();
                }

                if (encapCmd.commandClass == COMMAND_CLASS_SECURITY_2.ID &&
                    encapCmd.command == COMMAND_CLASS_SECURITY_2.SECURITY_2_COMMANDS_SUPPORTED_GET.ID)
                {
                    if (scheme != ZWave.Enums.SecuritySchemes.NONE)
                    {
                        List<byte> secureClasses = new List<byte>();
                        var rpt = new COMMAND_CLASS_SECURITY_2.SECURITY_2_COMMANDS_SUPPORTED_REPORT()
                        {
                            commandClass = secureCCs
                        };
                        data = EncapsulateData(rpt, destEPId, encapCmd.properties1.sourceEndPoint);
                    }
                }
                else if (encapCmd.commandClass == COMMAND_CLASS_SECURITY.ID &&
                    encapCmd.command == COMMAND_CLASS_SECURITY.SECURITY_COMMANDS_SUPPORTED_GET.ID)
                {
                    if (scheme != ZWave.Enums.SecuritySchemes.NONE)
                    {
                        ZWave.Enums.SecuritySchemes[] schemes = Network.GetSecuritySchemes();
                        if (schemes != null &&
                            schemes.Contains(ZWave.Enums.SecuritySchemes.S2_UNAUTHENTICATED) ||
                            schemes.Contains(ZWave.Enums.SecuritySchemes.S2_AUTHENTICATED) ||
                            schemes.Contains(ZWave.Enums.SecuritySchemes.S2_ACCESS))
                        {
                            secureCCs = new List<byte>();
                        }
                        var rpt = new COMMAND_CLASS_SECURITY.SECURITY_COMMANDS_SUPPORTED_REPORT()
                        {
                            commandClassSupport = secureCCs
                        };
                        data = EncapsulateData(rpt, destEPId, encapCmd.properties1.sourceEndPoint);
                    }
                }
                // Everything else is handled by the individual CommandClassSupport handle functions
            }
            return data;
        }

        private byte[] EncapsulateData(byte[] data, byte sourceEndPoint, byte destinationEndPoint)
        {
            COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP multiChannelCmd = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CMD_ENCAP();
            multiChannelCmd.commandClass = data[0];
            multiChannelCmd.command = data[1];
            multiChannelCmd.parameter = new List<byte>();
            for (int i = 2; i < data.Length; i++)
            {
                multiChannelCmd.parameter.Add(data[i]);
            }
            multiChannelCmd.properties1.res = 0;
            multiChannelCmd.properties1.sourceEndPoint = sourceEndPoint;
            multiChannelCmd.properties2.bitAddress = 0;
            multiChannelCmd.properties2.destinationEndPoint = destinationEndPoint;
            data = multiChannelCmd;
            return data;
        }

        private byte[] PrepareEndpointReport(byte[] command)
        {
            if (Version >= 3)
            {
                bool dynamic = false;
                bool identical = true;
                COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_END_POINT_GET cmd = command;

                IEnumerable<IndividualEndPointData> dynamicEPs = IndividualEndPoints.Where(x => x.Dynamic == true);

                if (dynamicEPs.Count() > 0)
                {
                    dynamic = true;
                }

                if (IndividualEndPoints.Count() > 0)
                {
                    IEnumerable<IndividualEndPointData> identicalEPs = IndividualEndPoints.Where(x => x.GenericDeviceClass == IndividualEndPoints[0].GenericDeviceClass &&
                                                                                                    x.SpecificDeviceClass == IndividualEndPoints[0].SpecificDeviceClass);
                    if (identicalEPs.Count() < IndividualEndPoints.Count())
                    {
                        identical = false;
                    }

                    foreach(IndividualEndPointData epData in IndividualEndPoints)
                    {
                        if (epData.EndPointId == 0) continue;
                        var DifferencesList = IndividualEndPoints[0].CommandClassDataList.Where(x => !epData.CommandClassDataList.Any(x1 => x1.CommandClassId == x.CommandClassId))
                            .Union(epData.CommandClassDataList.Where(x => !IndividualEndPoints[0].CommandClassDataList.Any(x1 => x1.CommandClassId == x.CommandClassId)));

                        if (DifferencesList != null && DifferencesList.Count() > 0)
                        {
                            identical = false;
                        }
                    }
                }

                if (Version == 3)
                {
                    COMMAND_CLASS_MULTI_CHANNEL_V3.MULTI_CHANNEL_END_POINT_REPORT rpt = new COMMAND_CLASS_MULTI_CHANNEL_V3.MULTI_CHANNEL_END_POINT_REPORT();
                    rpt.properties1.dynamic = Convert.ToByte(dynamic);
                    rpt.properties1.identical = Convert.ToByte(identical);
                    rpt.properties2.endPoints = Convert.ToByte(IndividualEndPoints.Count());
                    return rpt;
                }
                if (Version == 4)
                {
                    COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_END_POINT_REPORT rpt = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_END_POINT_REPORT();
                    rpt.properties1.dynamic = Convert.ToByte(dynamic);
                    rpt.properties1.identical = Convert.ToByte(identical);
                    rpt.properties2.individualEndPoints = Convert.ToByte(IndividualEndPoints.Count());
                    rpt.properties3.aggregatedEndPoints = Convert.ToByte(AggregatedEndPoints.Count());
                    return rpt;
                }
            }
            return null;
        }

        private byte[] PrepareCapabilitiesReport(byte[] command)
        {
            if (Version >= 3)
            {
                COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CAPABILITY_GET cmd = command;

                if (IndividualEndPoints == null)
                {
                    return null;
                }

                IndividualEndPointData ep = IndividualEndPoints.Where(x => x.EndPointId == cmd.properties1.endPoint).FirstOrDefault();
                if (ep == null)
                {
                    if (AggregatedEndPoints == null)
                    {
                        return null;
                    }

                    AggregatedEndPointData aggregatedEp = AggregatedEndPoints.Where(x => x.EndPointId == cmd.properties1.endPoint).FirstOrDefault();

                    if (aggregatedEp == null)
                    {
                        return null;
                    }

                    ep = IndividualEndPoints.Where(x => x.EndPointId == aggregatedEp.Members.FirstOrDefault()).FirstOrDefault();

                    if (ep == null)
                    {
                        return null;
                    }
                }

                COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CAPABILITY_REPORT rpt = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CAPABILITY_REPORT() { };
                rpt.genericDeviceClass = ep.GenericDeviceClass;
                rpt.specificDeviceClass = ep.SpecificDeviceClass;
                rpt.properties1.dynamic = Convert.ToByte(ep.Dynamic);

                List<byte> commandClassList;
                ZWave.Enums.SecuritySchemes[] schemes = Network.GetSecuritySchemes();
                if (schemes == null || schemes.Length == 0)
                {
                    commandClassList = ep.CommandClassDataList.Select(x => x.CommandClassId).ToList();
                }
                else
                {
                    commandClassList = ep.CommandClassDataList.Where(x => x.Secure == false).Select(x => x.CommandClassId).ToList();
                }

                if (ep.SecurityVersions.HasFlag(SecurityVersions.S2))
                {
                    commandClassList.Add(COMMAND_CLASS_SECURITY_2.ID);
                }
                if (ep.SecurityVersions.HasFlag(SecurityVersions.S0))
                {
                    commandClassList.Add(COMMAND_CLASS_SECURITY.ID);
                }
                if (ep.Supervision == true)
                {
                    commandClassList.Add(COMMAND_CLASS_SUPERVISION.ID);
                }
                rpt.properties1.endPoint = cmd.properties1.endPoint;
                rpt.commandClass = commandClassList;
                return rpt;
            }
            return null;
        }

        private byte[] PrepareAggregatedMembersReport(byte[] command)
        {
            if (Version == 4)
            {
                COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_AGGREGATED_MEMBERS_GET cmd = command;

                AggregatedEndPointData aEp = AggregatedEndPoints.Where(x => x.EndPointId == cmd.properties1.aggregatedEndPoint).FirstOrDefault();
                if (aEp == null)
                {
                    return null;
                }

                COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_AGGREGATED_MEMBERS_REPORT rpt = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_AGGREGATED_MEMBERS_REPORT();

                rpt.properties1.aggregatedEndPoint = cmd.properties1.aggregatedEndPoint;
                rpt.numberOfBitMasks = Convert.ToByte((aEp.Members.Max() + 8 - 1) / 8);

                rpt.aggregatedMembersBitMask = new List<byte>(new byte[rpt.numberOfBitMasks]);
                foreach (byte ep in aEp.Members)
                {
                    if (ep < 1 || ep > 127) continue;
                    int index = (ep - 1) / 8;
                    int pos = (ep - 1) % 8;

                    if (index < rpt.numberOfBitMasks)
                    {
                        rpt.aggregatedMembersBitMask[index] = (byte)(rpt.aggregatedMembersBitMask[index] | (byte)1 << pos);
                    }
                }
                return rpt;
            }
            return null;
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, 
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            supervisionReport.status = (byte) SvStatusNames.NoSupport;
            return;
        }


        public override void UpdateSupportData(CommandClassSupportData data)
        {
            // The MultiChannelSupportData class has no members, nothing to do.
            if (data is MultiChannelSupportData newData)
            {
            }
        }
    }

    public class MultiChannelSupportData : CommandClassSupportData
    {
        // Internals accessible from TestCases or EDE
    }

    public class IndividualEndPoint
    {
        public byte EndPointId { get; set; }
        public byte GenericDeviceClass { get; set; }
        public byte SpecificDeviceClass { get; set; }
        public bool Dynamic { get; set; }
        public bool Supervision { get; set; }
        public SecurityVersions SecurityVersions { get; set; }

        public IndividualEndPoint(byte endPointId, byte genericDeviceClass, byte specificDeviceClass,
            bool dynamic, bool supervision, SecurityVersions securityVersions)
        {
            EndPointId = endPointId;
            GenericDeviceClass = genericDeviceClass;
            SpecificDeviceClass = specificDeviceClass;
            Dynamic = dynamic;
            Supervision = supervision;
            SecurityVersions = securityVersions;
        }
    }

    // Contains list of basic CommandClassData objects for all supported CCs. Using this class for MultiChannelSupport and VersionSupport
    // instances prevent duplicated CommandClassSupport instances and problems with multiple timers objects.
    public class IndividualEndPointData : IndividualEndPoint
    {
        public IEnumerable<ICommandClass> CommandClassDataList { get; set; }

        public IndividualEndPointData(byte endPointId, byte genericDeviceClass, byte specificDeviceClass,
            bool dynamic, bool supervision, SecurityVersions securityVersions, List<CommandClassData> commandClassDataList)
            : base(endPointId, genericDeviceClass, specificDeviceClass, dynamic, supervision, securityVersions)
        {
            CommandClassDataList = commandClassDataList;
        }

        public IndividualEndPointData(IndividualEndPointEx endPointEx)
            : base(endPointEx.EndPointId, endPointEx.GenericDeviceClass, endPointEx.SpecificDeviceClass, 
                  endPointEx.Dynamic, endPointEx.Supervision, endPointEx.SecurityVersions)
        {

            if (endPointEx.CommandClassSupportList != null) 
            {
                CommandClassDataList = endPointEx.CommandClassSupportList;
            }
            else 
            {
                CommandClassDataList = new List<ICommandClass>();
            }
        }
    }

    // Contains CommandClassSupport instances. Used for building test devices.
    public class IndividualEndPointEx : IndividualEndPoint
    {
        public IEnumerable<CommandClassSupport> CommandClassSupportList { get; set; }

        public IndividualEndPointEx(byte endPointId, byte genericDeviceClass, byte specificDeviceClass,
            bool dynamic, bool supervision, SecurityVersions securityVersions, List<CommandClassSupport> commandClassSupportList)
            : base(endPointId, genericDeviceClass, specificDeviceClass, dynamic, supervision, securityVersions)
        {
            CommandClassSupportList = commandClassSupportList;
        }
    }

    // Wrapper class for basic Command Class data (everything that is needed in MultiChannelSupport and VersionSupport)
    public class CommandClassData : ICommandClass
    {
        public byte CommandClassId { get; set; }

        public byte Version { get; set; }

        public bool Secure { get; set; }

        public CommandClassData(byte commandClassId, byte version, bool secure)
        {
            CommandClassId = commandClassId;
            Version = version;
            Secure = secure;
        }
    }

    public class AggregatedEndPointData
    {
        public byte EndPointId { get; set; }

        public List<byte> Members { get; set; }

        public AggregatedEndPointData(byte endPointId, List<byte> members)
        {
            EndPointId = endPointId;
            Members = members;
        }
    }
}
