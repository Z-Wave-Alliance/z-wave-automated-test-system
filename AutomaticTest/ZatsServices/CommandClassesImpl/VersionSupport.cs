using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Enums;
using Utils;
using ZWave.Devices;
using ZWave.BasicApplication.Devices;
using System.Collections.Generic;
using System.Linq;
using ZWave;
using ZWave.BasicApplication;
using System;
using ZatsServices_netcore.CommandClassesImpl.Definitions.Interfaces;
using Newtonsoft.Json;
using Zats.Services.CommandClassesImpl.Definitions.Interfaces;

namespace Zats.Services
{
    [CommandClassName("Version Command Class")]
    [CommandClassId(COMMAND_CLASS_VERSION.ID)]
    [MinVersion(1)]
    [MaxVersion(3)]
    public class VersionSupport : CommandClassSupport
    {
        private VersionSupportData VersionSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return VersionSupportData; }
            set { VersionSupportData = (VersionSupportData)value; }
        }

        public IEnumerable<ICommandClass> SupportedRootCCs { get; set; }
        public List<IndividualEndPointData> IndividualEndPoints { get; set; }

        public VersionSupport()
            : this (new VersionSupportData() { }, new List<CommandClassData>(), new List<IndividualEndPointData>())
        { }

        public VersionSupport(List<CommandClassData> supportedRootCCData, 
                              List<IndividualEndPointData> individualEndPointData = null)
            : this(new VersionSupportData() { }, supportedRootCCData, individualEndPointData)
        { }

        public VersionSupport(List<CommandClassSupport> supportedRootCCs,
                              List<IndividualEndPointEx> individualEndPoints = null)
            : base(COMMAND_CLASS_VERSION.ID)
        {
            VersionSupportData = new VersionSupportData();
            SupportedRootCCs = supportedRootCCs;
            IndividualEndPoints = GetEpData(individualEndPoints);
        }

        public VersionSupport(VersionSupportData versionSupportData,
                              List<CommandClassSupport> supportedRootCCs,
                              List<IndividualEndPointEx> individualEndPoints = null)
            : base(COMMAND_CLASS_VERSION.ID)
        {
            VersionSupportData = versionSupportData;
            SupportedRootCCs = supportedRootCCs;
            IndividualEndPoints = GetEpData(individualEndPoints);
        }

        public VersionSupport(VersionSupportData versionSupportData, 
                              List<CommandClassData> supportedRootCCs, 
                              List<IndividualEndPointData> individualEndPoints = null)
            : base(COMMAND_CLASS_VERSION.ID)
        {
            VersionSupportData = versionSupportData;
            SupportedRootCCs = supportedRootCCs;
            IndividualEndPoints = individualEndPoints;
        }

        private static List<IndividualEndPointData> GetEpData(List<IndividualEndPointEx> individualEndPoints)
        {
            if (individualEndPoints != null)
            {
                List<IndividualEndPointData> individualEndPointData = new List<IndividualEndPointData>();
                foreach (IndividualEndPointEx epEx in individualEndPoints)
                {
                    individualEndPointData.Add(new IndividualEndPointData(epEx));
                }
                return individualEndPointData;
            }
            else
            {
                return null;
            }
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

                if (isHandlingAllowed && command != null && command.Length > 1 && VersionSupportData != null && SupportedRootCCs != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_VERSION.VERSION_GET.ID)
                    {
                        rpt = PrepareVersionReport();
                    }
                    else if (command[1] == COMMAND_CLASS_VERSION_V3.VERSION_CAPABILITIES_GET.ID)
                    {
                        rpt = PrepareCapabilitiesReport();
                    }
                    else if (command[1] == COMMAND_CLASS_VERSION_V3.VERSION_ZWAVE_SOFTWARE_GET.ID)
                    {
                        rpt = PrepareSoftwareReport();
                    }
                    else if (command[1] == COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_GET.ID)
                    {
                        rpt = PrepareCommandClassReportCmd(command);
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

        private byte[] PrepareVersionReport()
        {
            if (Version == 1)
            {
                COMMAND_CLASS_VERSION.VERSION_REPORT rpt = new COMMAND_CLASS_VERSION.VERSION_REPORT()
                {
                    zWaveLibraryType = (byte) Network.Library,
                    applicationVersion = VersionSupportData.Firmware0Version,
                    applicationSubVersion = VersionSupportData.Firmware0SubVersion,
                    zWaveProtocolVersion = VersionSupportData.ZWaveProtocolVersionV1,
                    zWaveProtocolSubVersion = VersionSupportData.ZWaveProtocolSubVersionV1
                };
                return rpt;
            }
            else
            {
                COMMAND_CLASS_VERSION_V2.VERSION_REPORT rpt = new COMMAND_CLASS_VERSION_V2.VERSION_REPORT()
                {
                    zWaveLibraryType = (byte)Network.Library,
                    firmware0Version = VersionSupportData.Firmware0Version,
                    firmware0SubVersion = VersionSupportData.Firmware0SubVersion,
                    zWaveProtocolVersion = VersionSupportData.ZWaveProtocolVersionV1,
                    zWaveProtocolSubVersion = VersionSupportData.ZWaveProtocolSubVersionV1,
                    hardwareVersion = VersionSupportData.HardwareVersion,
                    numberOfFirmwareTargets = (byte)(VersionSupportData.FirmwareVersions != null ? VersionSupportData.FirmwareVersions.Count : 0),
                };
                for (int i = 0; i < rpt.numberOfFirmwareTargets; i++)
                {
                    COMMAND_CLASS_VERSION_V2.VERSION_REPORT.TVG vg = new COMMAND_CLASS_VERSION_V2.VERSION_REPORT.TVG() { };
                    if (i < VersionSupportData.FirmwareVersions.Count)
                    {
                        vg.firmwareVersion = VersionSupportData.FirmwareVersions[i].FirmwareVersion;
                        vg.firmwareSubVersion = VersionSupportData.FirmwareVersions[i].FirmwareSubVersion;
                    }
                    else
                    {
                        vg.firmwareVersion = 0xFF;
                        vg.firmwareSubVersion = 0xFF;
                    }
                    rpt.vg.Add(vg);
                }
                return rpt;
            }
        }

        private byte[] PrepareCapabilitiesReport()
        {
            if (Version >= 3)
            {
                COMMAND_CLASS_VERSION_V3.VERSION_CAPABILITIES_REPORT rpt = new COMMAND_CLASS_VERSION_V3.VERSION_CAPABILITIES_REPORT() { };
                rpt.properties1.version = VersionSupportData.CapVersion;
                rpt.properties1.commandClass = VersionSupportData.CapCommandClass;
                rpt.properties1.zWaveSoftware = VersionSupportData.CapZWSoftware;
                return rpt;
            }
            return null;
        }

        private byte[] PrepareSoftwareReport()
        {
            if (Version >= 3)
            {
                COMMAND_CLASS_VERSION_V3.VERSION_ZWAVE_SOFTWARE_REPORT rpt = new COMMAND_CLASS_VERSION_V3.VERSION_ZWAVE_SOFTWARE_REPORT()
                {
                    sdkVersion = VersionSupportData.SDKVersion,
                    applicationFrameworkApiVersion = VersionSupportData.ZWaveApplicationFrameworkAPIVersion,
                    applicationFrameworkBuildNumber = VersionSupportData.ZWaveApplicationFrameworkBuildNumber,
                    hostInterfaceVersion = VersionSupportData.HostInterfaceVersion,
                    hostInterfaceBuildNumber = VersionSupportData.HostInterfaceBuildNumber,
                    zWaveProtocolVersion = VersionSupportData.ZWaveProtocolVersion,
                    zWaveProtocolBuildNumber = VersionSupportData.ZWaveProtocolBuildNumber,
                    applicationVersion = VersionSupportData.ApplicationVersion,
                    applicationBuildNumber = VersionSupportData.ApplicationBuildNumber
                };
                return rpt;
            }
            return null;
        }

        private byte[] PrepareCommandClassReportCmd(byte[] command)
        {
            COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_GET versionCCGetCmd = command;
            COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_REPORT rpt = new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_REPORT() { };

            rpt.requestedCommandClass = versionCCGetCmd.requestedCommandClass;

            // The report for these Command Classes is created anywhere else in ZATS...
            if (versionCCGetCmd.requestedCommandClass == COMMAND_CLASS_SUPERVISION.ID)
            {
                //rpt.commandClassVersion = 1;
                return null;
            }
            else if (versionCCGetCmd.requestedCommandClass == COMMAND_CLASS_CRC_16_ENCAP.ID)
            {
                //rpt.commandClassVersion = 0;
                return null;
            }
            else if (versionCCGetCmd.requestedCommandClass == COMMAND_CLASS_SECURITY.ID)
            {
                //rpt.commandClassVersion = 1;
                return null;
            }
            else if (versionCCGetCmd.requestedCommandClass == COMMAND_CLASS_SECURITY_2.ID)
            {
                //rpt.commandClassVersion = 1;
                return null;
            }
            else if (versionCCGetCmd.requestedCommandClass == COMMAND_CLASS_TRANSPORT_SERVICE.ID)
            {
                //rpt.commandClassVersion = 2;
                return null;
            }
            else
            {
                ICommandClass supportedCC = SupportedRootCCs.ToList().Find(x => x.CommandClassId == versionCCGetCmd.requestedCommandClass);

                if (supportedCC == null && IndividualEndPoints != null)
                {
                    foreach (IndividualEndPointData ep in IndividualEndPoints)
                    {
                        supportedCC = ep.CommandClassDataList.ToList().Find(x => x.CommandClassId == versionCCGetCmd.requestedCommandClass);
                        if (supportedCC != null)
                        {
                            break;
                        }
                    }
                }

                if (supportedCC != null)
                {
                    rpt.commandClassVersion = supportedCC.Version;
                }
                else
                {
                    rpt.commandClassVersion = 0x00;
                }
                return rpt;
            }
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, 
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            supervisionReport.status = (byte) SvStatusNames.NoSupport;
            return;
        }


        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is VersionSupportData newData)
            {
                bool identicalFirmwareTargets = newData.FirmwareVersions.Count == VersionSupportData.FirmwareVersions.Count;
                if (identicalFirmwareTargets)
                {
                    for (int i = 0; i < newData.FirmwareVersions.Count; i++)
                    {
                        if (VersionSupportData.FirmwareVersions.ElementAt(i).FirmwareVersion != newData.FirmwareVersions.ElementAt(i).FirmwareVersion
                         || VersionSupportData.FirmwareVersions.ElementAt(i).FirmwareSubVersion != newData.FirmwareVersions.ElementAt(i).FirmwareSubVersion)
                        {
                            identicalFirmwareTargets = false;
                            break;
                        }
                    }
                }
                if (VersionSupportData.ApplicationVersion != newData.ApplicationVersion
                 || VersionSupportData.ApplicationBuildNumber != newData.ApplicationBuildNumber
                 || VersionSupportData.ZWaveProtocolVersion != newData.ZWaveProtocolVersion
                 || VersionSupportData.ZWaveProtocolBuildNumber != newData.ZWaveProtocolBuildNumber
                 || VersionSupportData.HardwareVersion != newData.HardwareVersion
                 || VersionSupportData.FirmwareVersions.Count != newData.FirmwareVersions.Count
                 || !identicalFirmwareTargets)
                {
                    VersionSupportData.ApplicationVersion = newData.ApplicationVersion;
                    VersionSupportData.ApplicationBuildNumber = newData.ApplicationBuildNumber;
                    VersionSupportData.ZWaveProtocolVersion = newData.ZWaveProtocolVersion;
                    VersionSupportData.ZWaveProtocolBuildNumber = newData.ZWaveProtocolBuildNumber;
                    VersionSupportData.HardwareVersion = newData.HardwareVersion;
                    VersionSupportData.FirmwareVersions = newData.FirmwareVersions;
                    if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        SendUnsolicitedReport(PrepareVersionReport(), NodeTag.Empty);
                    }
                }

                if (VersionSupportData.CapVersion != newData.CapVersion
                 || VersionSupportData.CapCommandClass != newData.CapCommandClass
                 || VersionSupportData.CapZWSoftware != newData.CapZWSoftware)
                {
                    VersionSupportData.CapVersion = newData.CapVersion;
                    VersionSupportData.CapCommandClass = newData.CapCommandClass;
                    VersionSupportData.CapZWSoftware = newData.CapZWSoftware;
                    if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        SendUnsolicitedReport(PrepareCapabilitiesReport(), NodeTag.Empty);
                    }
                }

                if (VersionSupportData.SDKVersion != newData.SDKVersion
                 || VersionSupportData.ZWaveApplicationFrameworkAPIVersion != newData.ZWaveApplicationFrameworkAPIVersion
                 || VersionSupportData.ZWaveApplicationFrameworkBuildNumber != newData.ZWaveApplicationFrameworkBuildNumber
                 || VersionSupportData.HostInterfaceVersion != newData.HostInterfaceVersion
                 || VersionSupportData.HostInterfaceBuildNumber != newData.HostInterfaceBuildNumber)
                {
                    VersionSupportData.SDKVersion = newData.SDKVersion;
                    VersionSupportData.ZWaveApplicationFrameworkAPIVersion = newData.ZWaveApplicationFrameworkAPIVersion;
                    VersionSupportData.ZWaveApplicationFrameworkBuildNumber = newData.ZWaveApplicationFrameworkBuildNumber;
                    VersionSupportData.HostInterfaceVersion = newData.HostInterfaceVersion;
                    VersionSupportData.HostInterfaceBuildNumber = newData.HostInterfaceBuildNumber;
                    if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        SendUnsolicitedReport(PrepareSoftwareReport(), NodeTag.Empty);
                    }
                }
                // PrepareCommandClassReport is available via Z-Wave only, because elements are not part of VersionSupportData

                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class TargetFirmwareVersion : INestedItem
    {
        public int NestedId { get; set; }
        
        [CustomDataParameter(1, "Version", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Int_0_255, EditType.Config)]
        public byte FirmwareVersion { get; set; }
        
        [CustomDataParameter(1, "Subversion", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Int_0_255, EditType.Config)]
        public byte FirmwareSubVersion { get; set; }
        public TargetFirmwareVersion(byte firmwareVersion, byte firmwareSubVersion)
        {
            FirmwareVersion = firmwareVersion;
            FirmwareSubVersion = firmwareSubVersion;
        }
    }

    public class VersionSupportData : CommandClassSupportData, INodeItem
    {
        // V1
        [ParameterSeparator(1, "Version Report")]
        [XmlDataParameter(1, COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_VERSION.VERSION_REPORT.ID, new byte[] { 0x00 }, EditType.Readonly)]
        public byte ZWaveLibraryType { get; set; } = 0x03;            // = (byte)Network.Library (hardcoded)

        [XmlDataParameter(1, COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_VERSION.VERSION_REPORT.ID, new byte[] { 0x01 }, EditType.Config )]
        public byte ZWaveProtocolVersionV1 { get; set; }

        [XmlDataParameter(1, COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_VERSION.VERSION_REPORT.ID, new byte[] { 0x02 }, EditType.Config)]
        public byte ZWaveProtocolSubVersionV1 { get; set; }

        [XmlDataParameter(1, COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_VERSION.VERSION_REPORT.ID, new byte[] { 0x03 }, EditType.Config)]
        public byte Firmware0Version { get; set; }            // see ApplicationVersion member V3

        [XmlDataParameter(1, COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_VERSION.VERSION_REPORT.ID, new byte[] { 0x04 }, EditType.Config)]
        public byte Firmware0SubVersion { get; set; }         // see ApplicationVersion member V3

        // V2
        [XmlDataParameter(2, COMMAND_CLASS_VERSION.ID, COMMAND_CLASS_VERSION.VERSION_REPORT.ID, new byte[] { 0x05 }, EditType.Config)]
        public byte HardwareVersion { get; set; }            // = Network.HardwareVersion (hardcoded)

        [NestedControl(2, "Firmware Versions")]
        public List<TargetFirmwareVersion> FirmwareVersions { get; set; } = new List<TargetFirmwareVersion>();


        // V3 Capabilities Report
        [ParameterSeparator(3, "Version Capabilities Report")]
        [XmlDataParameter(3, COMMAND_CLASS_VERSION_V3.ID, COMMAND_CLASS_VERSION_V3.VERSION_CAPABILITIES_REPORT.ID, new byte[] { 0x00, 0x00 }, EditType.Config)]
        public byte CapVersion { get; set; } = 1;       // 1 = true

        [XmlDataParameter(3, COMMAND_CLASS_VERSION_V3.ID, COMMAND_CLASS_VERSION_V3.VERSION_CAPABILITIES_REPORT.ID, new byte[] { 0x00, 0x01}, EditType.Config)]
        public byte CapCommandClass { get; set; } = 1;  // 1 = true

        [XmlDataParameter(3, COMMAND_CLASS_VERSION_V3.ID, COMMAND_CLASS_VERSION_V3.VERSION_CAPABILITIES_REPORT.ID, new byte[] { 0x00, 0x02 }, EditType.Config)]
        public byte CapZWSoftware { get; set; } = 1;    // 1 = true

        // V3 Software Report
        [ParameterSeparator(3, "Version Z-Wave Software Report")]
        [XmlDataParameter(3, COMMAND_CLASS_VERSION_V3.ID, COMMAND_CLASS_VERSION_V3.VERSION_ZWAVE_SOFTWARE_REPORT.ID, new byte[] { 0x00 }, EditType.Config)]
        public byte[] SDKVersion { get; set; } = { 0x07, 0x00, 0x00 };

        [XmlDataParameter(3, COMMAND_CLASS_VERSION_V3.ID, COMMAND_CLASS_VERSION_V3.VERSION_ZWAVE_SOFTWARE_REPORT.ID, new byte[] { 0x01 }, EditType.Config)]
        public byte[] ZWaveApplicationFrameworkAPIVersion { get; set; } = { 0x00, 0x00, 0x00 };

        [XmlDataParameter(3, COMMAND_CLASS_VERSION_V3.ID, COMMAND_CLASS_VERSION_V3.VERSION_ZWAVE_SOFTWARE_REPORT.ID, new byte[] { 0x02 }, EditType.Config)]
        public byte[] ZWaveApplicationFrameworkBuildNumber { get; set; } = { 0x00, 0x00 };

        [XmlDataParameter(3, COMMAND_CLASS_VERSION_V3.ID, COMMAND_CLASS_VERSION_V3.VERSION_ZWAVE_SOFTWARE_REPORT.ID, new byte[] { 0x03 }, EditType.Config)]
        public byte[] HostInterfaceVersion { get; set; } = { 0x00, 0x00, 0x00 };

        [XmlDataParameter(3, COMMAND_CLASS_VERSION_V3.ID, COMMAND_CLASS_VERSION_V3.VERSION_ZWAVE_SOFTWARE_REPORT.ID, new byte[] { 0x04 }, EditType.Config)]
        public byte[] HostInterfaceBuildNumber { get; set; } = { 0x00, 0x00 };

        // ZWaveProtocolVersion: Byte1 = V1..V2 Z-Wave Protocol Version; Byte2 = V1..V2 Z-Wave Protocol Sub Version
        [XmlDataParameter(3, COMMAND_CLASS_VERSION_V3.ID, COMMAND_CLASS_VERSION_V3.VERSION_ZWAVE_SOFTWARE_REPORT.ID, new byte[] { 0x05 }, EditType.Config)]
        public byte[] ZWaveProtocolVersion { get; set; } = { 0x07, 0x00, 0x00 };

        [XmlDataParameter(3, COMMAND_CLASS_VERSION_V3.ID, COMMAND_CLASS_VERSION_V3.VERSION_ZWAVE_SOFTWARE_REPORT.ID, new byte[] { 0x06 }, EditType.Config)]
        public byte[] ZWaveProtocolBuildNumber { get; set; } = { 0x00, 0xFF };

        // ApplicationVersion : Byte1 = V1 ApplicationVersion resp. V2 Firmware0Version; Byte2: V1 ApplicationSubVersion resp. V2 Firmware0SubVersion
        [XmlDataParameter(3, COMMAND_CLASS_VERSION_V3.ID, COMMAND_CLASS_VERSION_V3.VERSION_ZWAVE_SOFTWARE_REPORT.ID, new byte[] { 0x07 }, EditType.Config)]
        public byte[] ApplicationVersion { get; set; } = { 0x01, 0x00, 0x00 };

        [XmlDataParameter(3, COMMAND_CLASS_VERSION_V3.ID, COMMAND_CLASS_VERSION_V3.VERSION_ZWAVE_SOFTWARE_REPORT.ID, new byte[] { 0x08 }, EditType.Config)]
        public byte[] ApplicationBuildNumber { get; set; } = { 0x00, 0xFF };

        [JsonIgnore]
        public IEnumerable<INestedItem> ChildItems => FirmwareVersions;
    }
}
