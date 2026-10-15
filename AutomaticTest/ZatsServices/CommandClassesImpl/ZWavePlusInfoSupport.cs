// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
using System.ComponentModel;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;

namespace Zats.Services
{
    [CommandClassName("Z-Wave Plus Info Command Class")]
    [CommandClassId(COMMAND_CLASS_ZWAVEPLUS_INFO.ID)]
    [MinVersion(1)]
    [MaxVersion(2)]
    public class ZWavePlusInfoSupport : CommandClassSupport
    {
        private ZWavePlusInfoSupportData ZWavePlusInfoSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return ZWavePlusInfoSupportData; }
            set { ZWavePlusInfoSupportData = (ZWavePlusInfoSupportData)value; }
        }

        public ZWavePlusInfoSupport()
            : this(new ZWavePlusInfoSupportData() { })
        { }

        public ZWavePlusInfoSupport(ZWavePlusInfoSupportData zWavePlusInfoSupportData)
            : base(COMMAND_CLASS_ZWAVEPLUS_INFO.ID)
        {
            ZWavePlusInfoSupportData = zWavePlusInfoSupportData;
        }

        protected override void OnHandledDelayed(DataReceivedUnit ou)
        {
            ou.SetNextActionItems();

            if (MatchesDestinationEndPoint())
            {
                var sendDstNode = new NodeTag(ReceivedAchData.DstNode.Id, DstNode.EndPointId);
                var sendSrcNode = ReceivedAchData.SrcNode;
                byte[] command = ReceivedAchData.Command;
                ZWave.Enums.SecuritySchemes scheme = ReceivedAchData.SecurityScheme;
                bool isHandlingAllowed = IsHandlingAllowed(Network, command, scheme, Secure);

                if (isHandlingAllowed && command != null && command.Length > 1 && ZWavePlusInfoSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareZWavePlusInfoReport();
                    }
                    if (rpt != null && IsAnsweringAllowed())
                    {
                        ApiOperation sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command, sendDstNode.EndPointId);
                }
            }
        }

        private byte[] PrepareZWavePlusInfoReport()
        {
            COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_REPORT rpt;

            rpt = new COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_REPORT()
            {
                zWaveVersion = ZWavePlusInfoSupportData.ZWaveVersion,
                roleType = (byte)ZWavePlusInfoSupportData.RoleType,
                nodeType = (byte)ZWavePlusInfoSupportData.NodeType,
                installerIconType = ZWavePlusInfoSupportData.InstallerIconType,
                userIconType = ZWavePlusInfoSupportData.UserIconType
            };
            return rpt;
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, 
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            supervisionReport.status = (byte) SvStatusNames.NoSupport;
            return;
        }


        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is ZWavePlusInfoSupportData newData)
            {
                if (ZWavePlusInfoSupportData.ZWaveVersion != newData.ZWaveVersion
                 || ZWavePlusInfoSupportData.RoleType != newData.RoleType
                 || ZWavePlusInfoSupportData.NodeType != newData.NodeType
                 || ZWavePlusInfoSupportData.InstallerIconType[0] != newData.InstallerIconType[0]
                 || ZWavePlusInfoSupportData.InstallerIconType[1] != newData.InstallerIconType[1]
                 || ZWavePlusInfoSupportData.UserIconType[0] != newData.UserIconType[0]
                 || ZWavePlusInfoSupportData.UserIconType[1] != newData.UserIconType[1])
                {
                    ZWavePlusInfoSupportData.ZWaveVersion = newData.ZWaveVersion;
                    ZWavePlusInfoSupportData.RoleType = newData.RoleType;
                    ZWavePlusInfoSupportData.NodeType = newData.NodeType;
                    ZWavePlusInfoSupportData.InstallerIconType = newData.InstallerIconType;
                    ZWavePlusInfoSupportData.UserIconType = newData.UserIconType;
                    if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        SendUnsolicitedReport(PrepareZWavePlusInfoReport(), NodeTag.Empty);
                    }
                }
                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class ZWavePlusInfoSupportData : CommandClassSupportData
    {
        [ParameterSeparator(1, "Z-Wave Plus Info Report")]
        [XmlDataParameter(1, COMMAND_CLASS_ZWAVEPLUS_INFO.ID, COMMAND_CLASS_ZWAVEPLUS_INFO.ZWAVEPLUS_INFO_REPORT.ID, new byte[] { 0x00 }, EditType.Config)]
        public byte ZWaveVersion { get; set; } = 0x02;

        [XmlDataParameter(1, COMMAND_CLASS_ZWAVEPLUS_INFO.ID, COMMAND_CLASS_ZWAVEPLUS_INFO.ZWAVEPLUS_INFO_REPORT.ID, new byte[] { 0x01 }, EditType.Config)]
        public RoleTypes RoleType { get; set; } = RoleTypes.END_NODE_ALWAYS_ON;

        [XmlDataParameter(1, 1, COMMAND_CLASS_ZWAVEPLUS_INFO.ID, COMMAND_CLASS_ZWAVEPLUS_INFO.ZWAVEPLUS_INFO_REPORT.ID, new byte[] { 0x02 }, EditType.Config)]
        public ZPIV1NodeTypes V1NodeType { get; set; } = ZPIV1NodeTypes.ZWAVEPLUS_NODE;

        [XmlDataParameter(2, COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID, COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_REPORT.ID, new byte[] { 0x02 }, EditType.Config)]
        public NodeTypes NodeType { get; set; } = NodeTypes.ZWAVEPLUS_NODE;

        [XmlDataParameter(2, COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID, COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_REPORT.ID, new byte[] { 0x03 }, EditType.Config)]
        public byte[] InstallerIconType { get; set; } = { 0x07, 0x00 };

        [XmlDataParameter(2, COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ID, COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_REPORT.ID, new byte[] { 0x04 }, EditType.Config)]
        public byte[] UserIconType { get; set; } = { 0x07, 0x00 };
    }

    public enum ZPIV1NodeTypes : byte
    {
        ZWAVEPLUS_NODE                      = NodeTypes.ZWAVEPLUS_NODE, // 0x00
        ZWAVEPLUS_FOR_IP_ROUTER             = 0x01, // obsolete in V2
        ZWAVEPLUS_FOR_IP_GATEWAY            = NodeTypes.ZWAVEPLUS_FOR_IP_GATEWAY, // 0x02
        ZWAVEPLUS_FOR_IP_CLIENT_IP_NODE     = 0x03, // obsolete in V2
        ZWAVEPLUS_FOR_IP_CLIENT_ZWAVE_NODE  = 0x04, // obsolete in V2
    }
}
