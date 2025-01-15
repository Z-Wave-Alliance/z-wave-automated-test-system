// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using Utils;
using ZWave.Devices;
using System;
using ZWave;
using System.Linq;

namespace Zats.Services
{
    public class MultiChannelSupport : CommandClassSupport
    {
        private MultiChannelSupportData MultiChannelSupportData { get; set; }

        //private readonly static string CommandClassName = "Multi Channel CC";
        //private string EndPointName = "";
        //private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        //private SvResultNames ResultForSv = SvResultNames.Undefined;

        public List<IndividualEndPointEx> IndividualEndPoints { get; set; }
        public List<AggregatedEndPointEx> AggregatedEndPoints { get; set; }
        private int MaxEpsPerFindReport = 20;

        public MultiChannelSupport(List<IndividualEndPointEx> individualEndPoints, List<AggregatedEndPointEx> aggregatedEndPoints)
            : base(COMMAND_CLASS_MULTI_CHANNEL_V2.ID)
        {
            MinCCVersion = 3;
            MaxCCVersion = 4;
            //MultiChannelSupportData = multiChannelSupportData;
            MultiChannelSupportData = new MultiChannelSupportData() { };
            //LogLevel = MultiChannelSupportData.LogLevel;
            IndividualEndPoints = individualEndPoints;
            AggregatedEndPoints = aggregatedEndPoints;
        }

        protected override void OnHandledDelayed(DataReceivedUnit ou)
        {
            ou.SetNextActionItems();
            if (DstNode.EndPointId == ReceivedAchData.DstNode.EndPointId)
            {
                var sendDstNode = ReceivedAchData.DstNode;
                var sendSrcNode = ReceivedAchData.SrcNode;
                byte[] command = ReceivedAchData.Command;
                var scheme = (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme;
                bool isResponseAllowed = IsResponseAllowed(Network, command, scheme, Secure);

                if (isResponseAllowed && command != null && command.Length > 1 && MultiChannelSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_END_POINT_GET.ID)
                    {
                        rpt = PrepareEndpointReport(command);
                        /*if (Version < 3) return;

                        bool dynamic = false;
                        bool identical = true;
                        var cmd = (COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_END_POINT_GET)command;

                        IEnumerable<IndividualEndPointEx> dynamicEPs = IndividualEndPoints.Where(x => x.Dynamic == true);

                        if (dynamicEPs.Count() > 0)
                        {
                            dynamic = true;
                        }

                        if (IndividualEndPoints.Count() > 0)
                        {
                            IEnumerable<IndividualEndPointEx> identicalEPs = IndividualEndPoints.Where(x => x.GenericDeviceClass == IndividualEndPoints[0].GenericDeviceClass &&
                                                                                                            x.SpecificDeviceClass == IndividualEndPoints[0].SpecificDeviceClass);
                            if (identicalEPs.Count() < IndividualEndPoints.Count())
                            {
                                identical = false;
                            }
                        }

                        if (Version == 3)
                        {
                            var rpt = new COMMAND_CLASS_MULTI_CHANNEL_V3.MULTI_CHANNEL_END_POINT_REPORT();
                            rpt.properties1.dynamic = Convert.ToByte(dynamic);
                            rpt.properties1.identical = Convert.ToByte(identical);
                            rpt.properties2.endPoints = Convert.ToByte(IndividualEndPoints.Count());
                            var sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, scheme);
                            ou.SetNextActionItems(sendData);
                        }
                        else
                        {
                            var rpt = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_END_POINT_REPORT();
                            rpt.properties1.dynamic = Convert.ToByte(dynamic);
                            rpt.properties1.identical = Convert.ToByte(identical);
                            rpt.properties2.individualEndPoints = Convert.ToByte(IndividualEndPoints.Count());
                            rpt.properties3.aggregatedEndPoints = Convert.ToByte(AggregatedEndPoints.Count());
                            var sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, scheme);
                            ou.SetNextActionItems(sendData);
                        }*/
                    }
                    else if (command[1] == COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CAPABILITY_GET.ID)
                    {
                        rpt = PrepareCapabilitiesReport(command);
                        /*if (Version < 3) return;

                        var cmd = (COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CAPABILITY_GET)command;

                        IndividualEndPointEx ep = IndividualEndPoints.Where(x => x.EndPointId == cmd.properties1.endPoint).FirstOrDefault();
                        if (ep == null) return;

                        var rpt = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_CAPABILITY_REPORT();
                        rpt.genericDeviceClass = ep.GenericDeviceClass;
                        rpt.specificDeviceClass = ep.SpecificDeviceClass;
                        rpt.properties1.dynamic = Convert.ToByte(ep.Dynamic);
                        List<byte> commandClassList = ep.CommandClassSupportList.Where(x => x.Secure == false).Select(x => x.Id).ToList();

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
                        rpt.commandClass = commandClassList;
                        var sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, scheme);
                        ou.SetNextActionItems(sendData);*/
                    }
                    else if (command[1] == COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_END_POINT_FIND.ID)
                    {
                        if (Version < 3) return;

                        COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_END_POINT_FIND cmd = command;
                        List<IndividualEndPointEx> eps;
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
                    else if (command[1] == COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_AGGREGATED_MEMBERS_GET.ID)
                    {
                        rpt = PrepareAggregatedMembersReport(command);
                        /*if (Version < 4) return;

                        var cmd = (COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_AGGREGATED_MEMBERS_GET)command;

                        AggregatedEndPointEx aEp = AggregatedEndPoints.Where(x => x.EndPointId == cmd.properties1.aggregatedEndPoint).FirstOrDefault();
                        if (aEp == null) return;

                        var rpt = new COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_AGGREGATED_MEMBERS_REPORT();

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
                        var sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, scheme);
                        ou.SetNextActionItems(sendData);*/
                    }

                    if (rpt != null)
                    {
                        ApiOperation sendData = new SendDataExOperation(Network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command);
                }
            }
        }

        private byte[] PrepareEndpointReport(byte[] command)
        {
            if (Version >= 3)
            {
                bool dynamic = false;
                bool identical = true;
                COMMAND_CLASS_MULTI_CHANNEL_V4.MULTI_CHANNEL_END_POINT_GET cmd = command;

                IEnumerable<IndividualEndPointEx> dynamicEPs = IndividualEndPoints.Where(x => x.Dynamic == true);

                if (dynamicEPs.Count() > 0)
                {
                    dynamic = true;
                }

                if (IndividualEndPoints.Count() > 0)
                {
                    IEnumerable<IndividualEndPointEx> identicalEPs = IndividualEndPoints.Where(x => x.GenericDeviceClass == IndividualEndPoints[0].GenericDeviceClass &&
                                                                                                    x.SpecificDeviceClass == IndividualEndPoints[0].SpecificDeviceClass);
                    if (identicalEPs.Count() < IndividualEndPoints.Count())
                    {
                        identical = false;
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

                IndividualEndPointEx ep = IndividualEndPoints.Where(x => x.EndPointId == cmd.properties1.endPoint).FirstOrDefault();
                if (ep == null)
                {
                    if (AggregatedEndPoints == null)
                    {
                        return null;
                    }

                    AggregatedEndPointEx aggregatedEp = AggregatedEndPoints.Where(x => x.EndPointId == cmd.properties1.endPoint).FirstOrDefault();

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
                List<byte> commandClassList = ep.CommandClassSupportList.Where(x => x.Secure == false).Select(x => x.Id).ToList();

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

                AggregatedEndPointEx aEp = AggregatedEndPoints.Where(x => x.EndPointId == cmd.properties1.aggregatedEndPoint).FirstOrDefault();
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
    }

    public class MultiChannelSupportData
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
    }
    
    public class IndividualEndPointEx
    {
        public byte EndPointId { get; set; }
        public byte GenericDeviceClass { get; set; }
        public byte SpecificDeviceClass { get; set; }
        public bool Dynamic { get; set; }
        public bool Supervision { get; set; }
        public SecurityVersions SecurityVersions { get; set; }
        public List<CommandClassSupport> CommandClassSupportList { get; set; }
        
        public IndividualEndPointEx(byte endPointId, byte genericDeviceClass, byte specificDeviceClass, 
            bool dynamic, bool supervision, SecurityVersions securityVersions, List<CommandClassSupport> commandClassSupportList)
        {
            EndPointId = endPointId;
            GenericDeviceClass = genericDeviceClass;
            SpecificDeviceClass = specificDeviceClass;
            Dynamic = dynamic;
            Supervision = supervision;
            SecurityVersions = securityVersions;
            CommandClassSupportList = commandClassSupportList;
        }
    }

    public class AggregatedEndPointEx
    {
        public byte EndPointId { get; set; }

        public List<byte> Members { get; set; }

        public AggregatedEndPointEx(byte endPointId, List<byte> members)
        {
            EndPointId = endPointId;
            Members = members;
        }
    }
}
