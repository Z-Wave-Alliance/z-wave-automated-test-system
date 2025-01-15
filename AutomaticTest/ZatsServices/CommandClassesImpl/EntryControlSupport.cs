// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Utils;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;
using Utils.UI.Enums;

// ToDo: Create unsolicited Entry Control Notification Commands is not yet supported

namespace Zats.Services
{
    public class EntryControlSupport : CommandClassSupport
    {
        private EntryControlSupportData EntryControlSupportData { get; set; }

        private readonly static string CommandClassName = "Entry Control CC";
        private string EndPointName = "";
        private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public EntryControlSupport()
            : this(new EntryControlSupportData() { })
        { }

        public EntryControlSupport(EntryControlSupportData entryControlSupportData)
            : base(COMMAND_CLASS_ENTRY_CONTROL.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 1;
            EntryControlSupportData = entryControlSupportData;
            LogLevel = EntryControlSupportData.LogLevel;
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

                EndPointName = ReceivedAchData.DstNode.EndPointId == 0 ? " @ Root" : string.Format(" @ EP {0}", ReceivedAchData.DstNode.EndPointId);
                ResultForSv = SvResultNames.Undefined;
                if (isResponseAllowed && command != null && command.Length > 1 && EntryControlSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_ENTRY_CONTROL.ENTRY_CONTROL_KEY_SUPPORTED_GET.ID)
                    {
                        rpt = PrepareKeySupportedReport();
                    }
                    else if (command[1] == COMMAND_CLASS_ENTRY_CONTROL.ENTRY_CONTROL_EVENT_SUPPORTED_GET.ID)
                    {
                        rpt = PrepareEventSupportedReport();
                    }
                    else if (command[1] == COMMAND_CLASS_ENTRY_CONTROL.ENTRY_CONTROL_CONFIGURATION_GET.ID)
                    {
                        rpt = PrepareConfigurationReport();
                    }
                    else if (command[1] == COMMAND_CLASS_ENTRY_CONTROL.ENTRY_CONTROL_CONFIGURATION_SET.ID)
                    {
                        ResultForSv = HandleConfigurationSet(command);
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

        private byte[] PrepareKeySupportedReport()
        {
            COMMAND_CLASS_ENTRY_CONTROL.ENTRY_CONTROL_KEY_SUPPORTED_REPORT rpt = new COMMAND_CLASS_ENTRY_CONTROL.ENTRY_CONTROL_KEY_SUPPORTED_REPORT() { };

            rpt.keySupportedBitMask = CommandClassSupportUtils.CreateBitmaskFromList(EntryControlSupportData.SupportedKeysList, 0); ;
            rpt.keySupportedBitMaskLength = (byte) rpt.keySupportedBitMask.Count;
            return rpt;
        }

        private byte[] PrepareEventSupportedReport()
        {
            COMMAND_CLASS_ENTRY_CONTROL.ENTRY_CONTROL_EVENT_SUPPORTED_REPORT rpt = new COMMAND_CLASS_ENTRY_CONTROL.ENTRY_CONTROL_EVENT_SUPPORTED_REPORT() { };
            rpt.dataTypeSupportedBitMask = CommandClassSupportUtils.CreateBitmaskFromList(EntryControlSupportData.SupportedDataTypesList, 0);
            rpt.properties1.dataTypeSupportedBitMaskLength = (byte) rpt.dataTypeSupportedBitMask.Count;
            rpt.eventTypeSupportedBitMask = CommandClassSupportUtils.CreateBitmaskFromList(EntryControlSupportData.SupportedEventTypesList, 0);
            rpt.properties2.eventSupportedBitMaskLength = (byte) rpt.eventTypeSupportedBitMask.Count;
            rpt.keyCachedSizeSupportedMinimum = EntryControlSupportData.KeyCachedSizeMinimum;
            rpt.keyCachedSizeSupportedMaximum = EntryControlSupportData.KeyCachedSizeMaximum;
            rpt.keyCachedTimeoutSupportedMinimum = EntryControlSupportData.KeyCachedTimeoutMinimum;
            rpt.keyCachedTimeoutSupportedMaximum = EntryControlSupportData.KeyCachedTimeoutMaximum;
            return rpt;
        }

        private byte[] PrepareConfigurationReport()
        {
            COMMAND_CLASS_ENTRY_CONTROL.ENTRY_CONTROL_CONFIGURATION_REPORT rpt = new COMMAND_CLASS_ENTRY_CONTROL.ENTRY_CONTROL_CONFIGURATION_REPORT() { };
            rpt.keyCacheSize = EntryControlSupportData.KeyCacheSize;
            rpt.keyCacheTimeout = EntryControlSupportData.KeyCacheTimeout;
            return rpt;
        }

        private SvResultNames HandleConfigurationSet(byte[] command)
        {
            COMMAND_CLASS_ENTRY_CONTROL.ENTRY_CONTROL_CONFIGURATION_SET setCmd = command;

            if (setCmd.keyCacheSize >= EntryControlSupportData.KeyCachedSizeMinimum && setCmd.keyCacheSize <= EntryControlSupportData.KeyCachedSizeMaximum)
            {
                EntryControlSupportData.KeyCacheSize = setCmd.keyCacheSize;
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: KeyCacheSize set to {2}.",
                            CommandClassName, EndPointName, EntryControlSupportData.KeyCacheSize);
            }
            if (setCmd.keyCacheTimeout >= EntryControlSupportData.KeyCachedTimeoutMinimum && setCmd.keyCacheTimeout <= EntryControlSupportData.KeyCachedTimeoutMaximum)
            {
                EntryControlSupportData.KeyCacheTimeout = setCmd.keyCacheTimeout;
                CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: KeyCacheTimeout set to {2}.",
                            CommandClassName, EndPointName, EntryControlSupportData.KeyCacheTimeout);
            }
            if (setCmd.keyCacheSize >= EntryControlSupportData.KeyCachedSizeMinimum && setCmd.keyCacheSize <= EntryControlSupportData.KeyCachedSizeMaximum
             && setCmd.keyCacheTimeout >= EntryControlSupportData.KeyCachedTimeoutMinimum && setCmd.keyCacheTimeout <= EntryControlSupportData.KeyCachedTimeoutMaximum)
            { 
                return SvResultNames.Successful;
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: KeyCacheSize {2} and/or KeyCacheTimeout {3} ignored.",
                        CommandClassName, EndPointName, setCmd.keyCacheSize, setCmd.keyCacheTimeout);
            return SvResultNames.Failed;
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, 
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != Id)
            {
                supervisionReport.status = (byte) SvStatusNames.NoSupport;
                return;
            }

            supervisionReport.status = (byte) SvStatusNames.Fail;  // FAIL as default
            switch (supervisionGet.encapsulatedCommand[1])
            {
                case COMMAND_CLASS_ENTRY_CONTROL.ENTRY_CONTROL_CONFIGURATION_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }
    }

    public class EntryControlSupportData
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
        // Configuration Report
        public byte KeyCacheSize { get; set; } = 4;
        public byte KeyCacheTimeout { get; set; } = 2;

        // Supported Keys Report
        // SupportedKeysList contains all supported ASCII codes. Default: 0...9
        public List<byte> SupportedKeysList { get; set; } = new List<byte> { 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39 };

        // Supported Event Types Report
        // Data Types: 0x00 = NA; 0x01 = RAW; 0x02 = ASCII; 0x03 = MD5
        public List<EntryControlDataTypeNames> SupportedDataTypesList { get; set; } = new List<EntryControlDataTypeNames> 
        {
            EntryControlDataTypeNames.NA,
            EntryControlDataTypeNames.RAW,
            EntryControlDataTypeNames.ASCII,
            EntryControlDataTypeNames.MD5
        };

        // Event Types: supported from 0x00 to 0x18 (= all defined)
        public List<EntryControlEventTypeNames> SupportedEventTypesList { get; set; } = new List<EntryControlEventTypeNames> 
        {
            EntryControlEventTypeNames.Caching,         // 0x00
            EntryControlEventTypeNames.CachedKeys,      // 0x01
            EntryControlEventTypeNames.Enter,           // 0x02
            EntryControlEventTypeNames.DisarmAll,       // 0x03
            EntryControlEventTypeNames.ArmAll,          // 0x04
            EntryControlEventTypeNames.ArmAway,         // 0x05
            EntryControlEventTypeNames.ArmHome,         // 0x06
            EntryControlEventTypeNames.ExitDelay,       // 0x07
            EntryControlEventTypeNames.Arm1,            // 0x08
            EntryControlEventTypeNames.Arm2,            // 0x09
            EntryControlEventTypeNames.Arm3,            // 0x0A
            EntryControlEventTypeNames.Arm4,            // 0x0B
            EntryControlEventTypeNames.Arm5,            // 0x0C
            EntryControlEventTypeNames.Arm6,            // 0x0D
            EntryControlEventTypeNames.Rfid,            // 0x0E
            EntryControlEventTypeNames.Bell,            // 0x0F
            EntryControlEventTypeNames.Fire,            // 0x10
            EntryControlEventTypeNames.Police,          // 0x11
            EntryControlEventTypeNames.AlertPanic,      // 0x12
            EntryControlEventTypeNames.AlertMedical,    // 0x13
            EntryControlEventTypeNames.GateOpen,        // 0x14
            EntryControlEventTypeNames.GateClose,       // 0x15
            EntryControlEventTypeNames.Lock,            // 0x16
            EntryControlEventTypeNames.Unlock,          // 0x17
            EntryControlEventTypeNames.Test,            // 0x18
        };

        public byte KeyCachedSizeMinimum { get; set; } = 1;
        public byte KeyCachedSizeMaximum { get; set; } = 32;
        public byte KeyCachedTimeoutMinimum { get; set; } = 1;
        public byte KeyCachedTimeoutMaximum { get; set; } = 10;
    }

    public enum EntryControlDataTypeNames : byte
    {
        NA = 0,
        RAW = 1,
        ASCII = 2,
        MD5 = 3,
    };
    public enum EntryControlEventTypeNames : byte
    {
        Caching = 0x00,
        CachedKeys = 0x01,
        Enter = 0x02,
        DisarmAll = 0x03,
        ArmAll = 0x04,
        ArmAway = 0x05,
        ArmHome = 0x06,
        ExitDelay = 0x07,
        Arm1 = 0x08,
        Arm2 = 0x09,
        Arm3 = 0x0A,
        Arm4 = 0x0B,
        Arm5 = 0x0C,
        Arm6 = 0x0D,
        Rfid = 0x0E,
        Bell = 0x0F,
        Fire = 0x10,
        Police = 0x11,
        AlertPanic = 0x12,
        AlertMedical = 0x13,
        GateOpen = 0x14,
        GateClose = 0x15,
        Lock = 0x16,
        Unlock = 0x17,
        Test = 0x18,
    }
}
