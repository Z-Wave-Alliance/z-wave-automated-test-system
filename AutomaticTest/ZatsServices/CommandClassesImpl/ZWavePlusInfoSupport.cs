using System.ComponentModel;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.Devices;

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
                var scheme = (ZWave.Enums.SecuritySchemes)ReceivedAchData.SecurityScheme;
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
                roleType = (byte) ZWavePlusInfoSupportData.RoleType,
                nodeType = (byte) ZWavePlusInfoSupportData.NodeType,
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
        [XmlDataParameter(1, 0x5E, 0x02, new byte[] { 0x00 }, EditType.Config)]
        public byte ZWaveVersion { get; set; } = 0x02;

        [XmlDataParameter(1, 0x5E, 0x02, new byte[] { 0x01 }, EditType.Config)]
        public ZPIRoleTypes RoleType { get; set; } = ZPIRoleTypes.EndNodeAlwaysOn;

        [XmlDataParameter(1, 0x5E, 0x02, new byte[] { 0x02 }, EditType.Config)]
        public ZPINodeTypes NodeType { get; set; } = ZPINodeTypes.ZWavePlusNode;

        [XmlDataParameter(2, 0x5E, 0x02, new byte[] { 0x03 }, EditType.Config)]
        public byte[] InstallerIconType { get; set; } = { 0x07, 0x00 };

        [XmlDataParameter(2, 0x5E, 0x02, new byte[] { 0x04 }, EditType.Config)]
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
