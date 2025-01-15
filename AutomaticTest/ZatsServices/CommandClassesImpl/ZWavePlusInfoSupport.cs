// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.Devices;

namespace Zats.Services
{
    public class ZWavePlusInfoSupport : CommandClassSupport
    {
        private ZWavePlusInfoSupportData ZWavePlusInfoSupportData { get; set; }

        //private readonly string CommandClassName = "Z-Wave Plus Info CC";
        //private string EndPointName = "";
        //private readonly int LogLevel;  // shortcut for SupportData.LogLevel

        public ZWavePlusInfoSupport()
            : this(new ZWavePlusInfoSupportData() { })
        { }

        public ZWavePlusInfoSupport(ZWavePlusInfoSupportData zWavePlusInfoSupportData)
            : base(COMMAND_CLASS_ZWAVEPLUS_INFO.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 2;
            ZWavePlusInfoSupportData = zWavePlusInfoSupportData;
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

                if (isResponseAllowed && command != null && command.Length > 1 && ZWavePlusInfoSupportData != null)
                {
                    if (command[1] == COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_GET.ID)
                    {
                        ApiOperation sendData;
                        COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_REPORT rpt;
                        
                        rpt = new COMMAND_CLASS_ZWAVEPLUS_INFO_V2.ZWAVEPLUS_INFO_REPORT()
                        {
                            zWaveVersion = ZWavePlusInfoSupportData.ZWaveVersion,
                            roleType = (byte) ZWavePlusInfoSupportData.RoleType,
                            nodeType = (byte) ZWavePlusInfoSupportData.NodeType,
                            installerIconType = ZWavePlusInfoSupportData.InstallerIconType,
                            userIconType = ZWavePlusInfoSupportData.UserIconType
                        };

                        sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command);
                }
            }
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, 
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            supervisionReport.status = (byte) SvStatusNames.NoSupport;
            return;
        }
    }

    public class ZWavePlusInfoSupportData
    {
        public byte ZWaveVersion { get; set; } = 0x02;
        public ZPIRoleTypes RoleType { get; set; } = ZPIRoleTypes.EndNodeAlwaysOn;
        public ZPINodeTypes NodeType { get; set; } = ZPINodeTypes.ZWavePlusNode;
        public byte[] InstallerIconType { get; set; } = { 0x07, 0x00 };
        public byte[] UserIconType { get; set; } = { 0x07, 0x00 };
    }

    public enum ZPIRoleTypes : byte
    {
        ControllerCentralStatic     = 0x00,
        ControllerSubStatic         = 0x01,
        ControllerPortable          = 0x02,
        ControllerPortableReporting = 0x03,
        EndNodePortable             = 0x04,
        EndNodeAlwaysOn             = 0x05,
        EndNodeSleepingReporting    = 0x06,
        EndNodeSleepingListening    = 0x07,
    }
    public enum ZPINodeTypes : byte
    {
        ZWavePlusNode                 = 0x00,
        ZWavePlusForIpRouter          = 0x01,
        ZWavePlusForIpGateway         = 0x02,
        ZWavePlusForIpClientIpNode    = 0x03,
        ZWavePlusForIpClientZWaveNode = 0x04,
    }
}
