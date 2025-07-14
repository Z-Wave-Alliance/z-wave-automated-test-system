// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
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

namespace Zats.Services
{
   public class VersionSupport : CommandClassSupport
    {
        private VersionSupportData VersionSupportData { get; set; }

        //private readonly static string CommandClassName = "Version CC";
        //private string EndPointName = "";
        //private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        private List<CommandClassSupport> SupportedRootCCs { get; set; }
        private List<IndividualEndPointEx> IndividualEndPoints { get; set; }

        public VersionSupport(List<CommandClassSupport> supportedRootCCs, 
                              List<IndividualEndPointEx> individualEndPoints = null)
            : this(new VersionSupportData() { }, supportedRootCCs, individualEndPoints)
        { }

        public VersionSupport(VersionSupportData versionSupportData, 
                              List<CommandClassSupport> supportedRootCCs, 
                              List<IndividualEndPointEx> individualEndPoints = null)
            : base(COMMAND_CLASS_VERSION.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 3;
            VersionSupportData = versionSupportData;
            SupportedRootCCs = supportedRootCCs;
            IndividualEndPoints = individualEndPoints;
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

                if (isResponseAllowed && command != null && command.Length > 1 && VersionSupportData != null && SupportedRootCCs != null)
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
                        rpt = PrepareCommandClassReport(command);
                    }

                    if (rpt != null)
                    {
                        ApiOperation sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }

                    CommandCallback(command);
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
                    applicationVersion = VersionSupportData.ApplicationVersion[0],
                    applicationSubVersion = VersionSupportData.ApplicationVersion[1],
                    zWaveProtocolVersion = VersionSupportData.ZWaveProtocolVersion[0],
                    zWaveProtocolSubVersion = VersionSupportData.ZWaveProtocolVersion[1]
                };
                return rpt;
            }
            else
            {
                COMMAND_CLASS_VERSION_V2.VERSION_REPORT rpt = new COMMAND_CLASS_VERSION_V2.VERSION_REPORT()
                {
                    zWaveLibraryType = (byte) Network.Library,
                    firmware0Version = VersionSupportData.ApplicationVersion[0],
                    firmware0SubVersion = VersionSupportData.ApplicationVersion[1],
                    zWaveProtocolVersion = VersionSupportData.ZWaveProtocolVersion[0],
                    zWaveProtocolSubVersion = VersionSupportData.ZWaveProtocolVersion[1],
                    hardwareVersion = Network.HardwareVersion,
                    numberOfFirmwareTargets = VersionSupportData.NumberOfFirmwareTargets,
                };
                for (int i = 0; i < rpt.numberOfFirmwareTargets; i++)
                {
                    COMMAND_CLASS_VERSION_V2.VERSION_REPORT.TVG vg = new COMMAND_CLASS_VERSION_V2.VERSION_REPORT.TVG() { };
                    if (i < VersionSupportData.FirmwareVersions.Count)
                    {
                        vg.firmwareVersion = VersionSupportData.FirmwareVersions[i].firmwareVersion;
                        vg.firmwareSubVersion = VersionSupportData.FirmwareVersions[i].firmwareSubVersion;
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

        private byte[] PrepareCommandClassReport(byte[] command)
        {
            COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_GET versionCCGetCmd = command;
            COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_REPORT rpt = new COMMAND_CLASS_VERSION.VERSION_COMMAND_CLASS_REPORT() { };

            rpt.requestedCommandClass = versionCCGetCmd.requestedCommandClass;

            // The report for these Command Classes is created anywhere else...
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
                //CommandClassSupport supportedCC = SupportedRootCCs.Where(x => x.Id == versionCCGetCmd.requestedCommandClass).FirstOrDefault();
                CommandClassSupport supportedCC = SupportedRootCCs.Find(x => x.Id == versionCCGetCmd.requestedCommandClass);

                if (supportedCC == null && IndividualEndPoints != null)
                {
                    foreach (IndividualEndPointEx ep in IndividualEndPoints)
                    {
                        //supportedCC = ep.CommandClassSupportList.Where(x => x.Id == versionCCGetCmd.requestedCommandClass).FirstOrDefault();
                        supportedCC = ep.CommandClassSupportList.Find(x => x.Id == versionCCGetCmd.requestedCommandClass);
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

    }

    public class VersionSupportData
    {
        // V1
        //public byte ZWaveLibraryType { get; set; }            // = (byte)Network.Library (hardcoded)
        //public byte ZWaveProtocolVersion { get; set; }        // see ZWaveProtocolVersion member V3
        //public byte ZWaveProtocolSubVersion { get; set; }     // see ZWaveProtocolVersion member V3
        //public byte Firmware0Version { get; set; }            // see ApplicationVersion member V3
        //public byte Firmware0SubVersion { get; set; }         // see ApplicationVersion member V3
        // V2
        // public byte HardwareVersion { get; set; }            // = Network.HardwareVersion (hardcoded)
        public byte NumberOfFirmwareTargets { get; set; } = 0x00;
        // Version1 SubVersion1 ... VersionN SubVersionN matching NumberOfFirmwareTargets
        public List<COMMAND_CLASS_VERSION_V2.VERSION_REPORT.TVG> FirmwareVersions { get; set; } = new List<COMMAND_CLASS_VERSION_V2.VERSION_REPORT.TVG>() { };
        // V3 Capabilities Report
        public byte CapVersion { get; set; } = 1;
        public byte CapCommandClass { get; set; } = 1;
        public byte CapZWSoftware { get; set; } = 1;

        // V3 Software Report
        public byte[] SDKVersion { get; set; } = { 0x07, 0x00, 0x00 };
        public byte[] ZWaveApplicationFrameworkAPIVersion { get; set; } = { 0x00, 0x00, 0x00 };
        public byte[] ZWaveApplicationFrameworkBuildNumber { get; set; } = { 0x00, 0x00 };
        public byte[] HostInterfaceVersion { get; set; } = { 0x00, 0x00, 0x00 };
        public byte[] HostInterfaceBuildNumber { get; set; } = { 0x00, 0x00 };

        // ZWaveProtocolVersion: Byte1 = V1..V2 Z-Wave Protocol Version; Byte2 = V1..V2 Z-Wave Protocol Sub Version
        public byte[] ZWaveProtocolVersion { get; set; } = { 0x06, 0x51, 0x00 };
        public byte[] ZWaveProtocolBuildNumber { get; set; } = { 0x00, 0xFF };

        // ApplicationVersion : Byte1 = V1 ApplicationVersion resp. V2 Firmware0Version; Byte2: V1 ApplicationSubVersion resp. V2 Firmware0SubVersion
        public byte[] ApplicationVersion { get; set; } = { 0x01, 0x03, 0x00 };
        public byte[] ApplicationBuildNumber { get; set; } = { 0x00, 0xFF };
    }
}
