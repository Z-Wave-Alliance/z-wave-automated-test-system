// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
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
using Zats.Services.CommandClassesImpl;

// ToDo: Create unsolicited Entry Control Notification Commands is not yet supported

namespace Zats.Services
{
    [CommandClassName("Entry Control Command Class")]
    [CommandClassId(COMMAND_CLASS_ENTRY_CONTROL.ID)]
    [MinVersion(1)]
    [MaxVersion(1)]
    public class EntryControlSupport : CommandClassSupport
    {
        private EntryControlSupportData EntryControlSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return EntryControlSupportData; }
            set { EntryControlSupportData = (EntryControlSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public EntryControlSupport()
            : this(new EntryControlSupportData() { })
        { }

        public EntryControlSupport(EntryControlSupportData entryControlSupportData)
            : base(COMMAND_CLASS_ENTRY_CONTROL.ID)
        {
            EntryControlSupportData = entryControlSupportData;
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

                ResultForSv = SvResultNames.Undefined;
                if (isHandlingAllowed && command != null && command.Length > 1 && EntryControlSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_ENTRY_CONTROL.ENTRY_CONTROL_KEY_SUPPORTED_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareKeySupportedReport();
                    }
                    else if (command[1] == COMMAND_CLASS_ENTRY_CONTROL.ENTRY_CONTROL_EVENT_SUPPORTED_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareEventSupportedReport();
                    }
                    else if (command[1] == COMMAND_CLASS_ENTRY_CONTROL.ENTRY_CONTROL_CONFIGURATION_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareConfigurationReport();
                    }
                    else if (command[1] == COMMAND_CLASS_ENTRY_CONTROL.ENTRY_CONTROL_CONFIGURATION_SET.ID)
                    {
                        ResultForSv = HandleConfigurationSetCmd(command, sendSrcNode);
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

        private SvResultNames HandleConfigurationSet(byte keyCacheSize, byte keyCacheTimeout, bool uiUpdate, NodeTag srcNode)
        {
            if (keyCacheSize >= EntryControlSupportData.KeyCachedSizeMinimum && keyCacheSize <= EntryControlSupportData.KeyCachedSizeMaximum)
            {
                EntryControlSupportData.KeyCacheSize = keyCacheSize;
                CommandClassSupportUtils.ReportAddLog(EntryControlSupportData.LogLevel, "{0} CC @ {1}: KeyCacheSize set to {2}.",
                            CommandClassShortName, EndPointName, EntryControlSupportData.KeyCacheSize);
            }
            if (keyCacheTimeout >= EntryControlSupportData.KeyCachedTimeoutMinimum && keyCacheTimeout <= EntryControlSupportData.KeyCachedTimeoutMaximum)
            {
                EntryControlSupportData.KeyCacheTimeout = keyCacheTimeout;
                CommandClassSupportUtils.ReportAddLog(EntryControlSupportData.LogLevel, "{0} CC @ {1}: KeyCacheTimeout set to {2}.",
                            CommandClassShortName, EndPointName, EntryControlSupportData.KeyCacheTimeout);
            }
            if (keyCacheSize >= EntryControlSupportData.KeyCachedSizeMinimum && keyCacheSize <= EntryControlSupportData.KeyCachedSizeMaximum
             && keyCacheTimeout >= EntryControlSupportData.KeyCachedTimeoutMinimum && keyCacheTimeout <= EntryControlSupportData.KeyCachedTimeoutMaximum)
            {
                if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                {
                    SendUnsolicitedReport(PrepareConfigurationReport(), srcNode);
                }
                if (uiUpdate)
                {
                    SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
                }
                return SvResultNames.Successful;
            }
            CommandClassSupportUtils.ReportAddLog(EntryControlSupportData.LogLevel, "{0} CC @ {1}: KeyCacheSize {2} and/or KeyCacheTimeout {3} ignored.",
                        CommandClassShortName, EndPointName, keyCacheSize, keyCacheTimeout);
            return SvResultNames.Failed;
        }

        private SvResultNames HandleConfigurationSetCmd(byte[] command, NodeTag srcNode)
        {
            COMMAND_CLASS_ENTRY_CONTROL.ENTRY_CONTROL_CONFIGURATION_SET setCmd = command;
            return HandleConfigurationSet(setCmd.keyCacheSize, setCmd.keyCacheTimeout, true, srcNode);
        }


        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, 
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != CommandClassId)
            {
                supervisionReport.status = (byte) SvStatusNames.NoSupport;
                return;
            }

            supervisionReport.status = (byte) SvStatusNames.Fail;  // FAIL as default
            switch (supervisionGet.encapsulatedCommand[1])
            {
                case COMMAND_CLASS_ENTRY_CONTROL.ENTRY_CONTROL_CONFIGURATION_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                    {
                        supervisionReport.status = (byte)SvStatusNames.Success;
                    }
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }


        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is EntryControlSupportData newData)
            {
                if (newData.TriggerEntryControlNotification == true)  // UI-Button 'Entry Control Notification', ignore all other data newData members
                {
                    COMMAND_CLASS_ENTRY_CONTROL.ENTRY_CONTROL_NOTIFICATION rpt = new COMMAND_CLASS_ENTRY_CONTROL.ENTRY_CONTROL_NOTIFICATION() { };
                    rpt.sequenceNumber = newData.SequenceNumber;
                    rpt.eventType = (byte)newData.EventType;
                    rpt.properties1.dataType = (byte)newData.DataType;
                    rpt.eventData = newData.EventData;
                    rpt.eventDataLength = (byte)(newData.EventData != null ? newData.EventData.Length : 0);

                    EntryControlSupportData.SequenceNumber = newData.SequenceNumber;
                    EntryControlSupportData.EventType = newData.EventType;
                    EntryControlSupportData.DataType = newData.DataType;
                    EntryControlSupportData.EventData = newData.EventData;

                    CommandClassSupportUtils.ReportAddLog(EntryControlSupportData.LogLevel,
                                "{0} CC @ {1}: Entry Control Notification to (triggered) prepared",
                                CommandClassShortName, EndPointName);
                    SendUnsolicitedReport(rpt, NodeTag.Empty);
                    CommandClassSupportUtils.ReportAddLog(EntryControlSupportData.LogLevel,
                                "{0} CC @ {1}: Entry Control Notification to (triggered) sent",
                                CommandClassShortName, EndPointName);

                }
                else
                {
                    // Always update Entry Notification parameters when normal Apply button is pressed.
                    // This ensures modified Notification parameters don't get lost on Apply.
                    EntryControlSupportData.SequenceNumber = newData.SequenceNumber;
                    EntryControlSupportData.EventType = newData.EventType;
                    EntryControlSupportData.DataType = newData.DataType;
                    EntryControlSupportData.EventData = newData.EventData;

                    bool identicalCacheOperation = true;
                    if (newData.KeyCacheSize != EntryControlSupportData.KeyCacheSize
                     || newData.KeyCacheTimeout != EntryControlSupportData.KeyCacheTimeout)
                    {
                        identicalCacheOperation = false;
                    }

                    bool identicalCacheConfiguration = true;
                    if (EntryControlSupportData.KeyCachedSizeMinimum != newData.KeyCachedSizeMinimum
                     || EntryControlSupportData.KeyCachedSizeMaximum != newData.KeyCachedSizeMaximum
                     || EntryControlSupportData.KeyCachedTimeoutMinimum != newData.KeyCachedTimeoutMinimum
                     || EntryControlSupportData.KeyCachedTimeoutMaximum != newData.KeyCachedTimeoutMaximum)
                    {
                        identicalCacheConfiguration = false;
                    }

                    bool identicalSupportedKeys = CommandClassSupportUtils.ListElementsEqual(EntryControlSupportData.SupportedKeysList, newData.SupportedKeysList);

                    bool identicalSupportedDataTypes = CommandClassSupportUtils.ListElementsEqual(EntryControlSupportData.SupportedDataTypesList, newData.SupportedDataTypesList);

                    bool identicalSupportedEventTypes = CommandClassSupportUtils.ListElementsEqual(EntryControlSupportData.SupportedEventTypesList, newData.SupportedEventTypesList);

                    if (!identicalCacheOperation)
                    {
                        HandleConfigurationSet(newData.KeyCacheSize, newData.KeyCacheTimeout, false, NodeTag.Empty);
                    }

                    if (!identicalSupportedKeys)
                    {
                        EntryControlSupportData.SupportedKeysList = newData.SupportedKeysList;
                        if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                        {
                            SendUnsolicitedReport(PrepareKeySupportedReport(), NodeTag.Empty);
                        }
                    }

                    if (!identicalCacheConfiguration
                     || !identicalSupportedDataTypes
                     || !identicalSupportedEventTypes)
                    {
                        EntryControlSupportData.SupportedDataTypesList = newData.SupportedDataTypesList;
                        EntryControlSupportData.SupportedEventTypesList = newData.SupportedEventTypesList;
                        EntryControlSupportData.KeyCachedSizeMinimum = newData.KeyCachedSizeMinimum;
                        EntryControlSupportData.KeyCachedSizeMaximum = newData.KeyCachedSizeMaximum;
                        EntryControlSupportData.KeyCachedTimeoutMinimum = newData.KeyCachedTimeoutMinimum;
                        EntryControlSupportData.KeyCachedTimeoutMaximum = newData.KeyCachedTimeoutMaximum;
                        if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                        {
                            SendUnsolicitedReport(PrepareEventSupportedReport(), NodeTag.Empty);
                        }
                    }
                }

                // safety mechanism: make sure that trigger flag is still false
                EntryControlSupportData.TriggerEntryControlNotification = false;

                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class EntryControlSupportData : CommandClassSupportData
    {
        // Configuration Report
        [ParameterSeparator(1, "Entry Control Configuration Report")]
        [XmlDataParameter(1, 0x6F, 0x06, new byte[] { 0x00 }, EditType.Config | EditType.Edit)]
        public byte KeyCacheSize { get; set; } = 4;

        [XmlDataParameter(1, 0x6F, 0x06, new byte[] { 0x01 }, EditType.Config | EditType.Edit)]
        public byte KeyCacheTimeout { get; set; } = 2;

        [ParameterSeparator(1, "Entry Control Notification")]
        //[XmlDataParameter(1, 0x6F, 0x01, new byte[] { 0x00 }, EditType.View | EditType.Edit)]
        [CustomDataParameter(1, "Sequence Number", UiControl.TextBox, InputType.Int_0_255, EditType.View | EditType.Edit)]
        public byte SequenceNumber { get; set; } = 0;

        //[XmlDataParameter(1, 0x6F, 0x01, new byte[] { 0x01, 0x00 }, EditType.View | EditType.Edit)]
        [CustomDataParameter(1, "Data Type", UiControl.ListComboBox, InputType.Enum, EditType.View | EditType.Edit, typeof(EntryControlDataTypeNames))]
        public EntryControlDataTypeNames DataType { get; set; } = EntryControlDataTypeNames.ASCII;

        //[XmlDataParameter(1, 0x6F, 0x01, new byte[] { 0x02 }, EditType.View | EditType.Edit)]
        [CustomDataParameter(1, "Event Type", UiControl.ListComboBox, InputType.Enum, EditType.View | EditType.Edit, typeof(EntryControlEventTypeNames))]
        public EntryControlEventTypeNames EventType { get; set; } = EntryControlEventTypeNames.Enter;

        [CustomDataParameter(1, "Event Data (hex)", UiControl.TextBox, InputType.HexByteArray, EditType.View | EditType.Edit)]
        public byte[] EventData { get; set; } = new byte[] { };

        [CustomDataParameterEventTrigger]
        [CustomDataParameter(1, "Trigger Entry Control Notification", UiControl.Button, InputType.Boolean, EditType.View | EditType.Edit)]
        public bool TriggerEntryControlNotification { get; set; } = false;    // Set this to true to trigger one manual EntryControlNotification (EDE only)

        // Supported Keys Report
        // SupportedKeysList contains all supported ASCII codes.
        // Internal Default: none.
        // Usage: .SupportedKeysList = new List<byte> { 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39 };
        [ParameterSeparator(1, "Entry Control Key Supported Report")]
        [CustomDataParameter(1, "Supported ASCII code list (hex)", UiControl.TextBox, InputType.HexByteArray, EditType.Config)]
        public byte[] SupportedKeysList { get; set; } = new byte[] { };

        // Supported Event Types Report
        // Data Types: 0x00 = NA; 0x01 = RAW; 0x02 = ASCII; 0x03 = MD5.
        // Internal Default: None
        // Usage: .SupportedDataTypesList = new List<EntryControlDataTypeNames> { EntryControlDataTypeNames.NA, ... };
        [ParameterSeparator(1, "Entry Control Event Supported Report")]
        [XmlDataParameter(1, 0x6F, 0x05, new byte[] { 0x01 }, EditType.Config)]
        public List<EntryControlDataTypeNames> SupportedDataTypesList { get; set; } = new List<EntryControlDataTypeNames>();

        // Event Types: Support allowed from 0x00 to 0x18 (= all defined).
        // Internal Default: none
        // Usage: .SupportedEventTypesList = new List<EntryControlEventTypeNames> { EntryControlEventTypeNames.Caching, ... };
        // Possible values:
        // EntryControlEventTypeNames.Caching,         // 0x00
        // EntryControlEventTypeNames.CachedKeys,      // 0x01
        // EntryControlEventTypeNames.Enter,           // 0x02
        // EntryControlEventTypeNames.DisarmAll,       // 0x03
        // EntryControlEventTypeNames.ArmAll,          // 0x04
        // EntryControlEventTypeNames.ArmAway,         // 0x05
        // EntryControlEventTypeNames.ArmHome,         // 0x06
        // EntryControlEventTypeNames.ExitDelay,       // 0x07
        // EntryControlEventTypeNames.Arm1,            // 0x08
        // EntryControlEventTypeNames.Arm2,            // 0x09
        // EntryControlEventTypeNames.Arm3,            // 0x0A
        // EntryControlEventTypeNames.Arm4,            // 0x0B
        // EntryControlEventTypeNames.Arm5,            // 0x0C
        // EntryControlEventTypeNames.Arm6,            // 0x0D
        // EntryControlEventTypeNames.Rfid,            // 0x0E
        // EntryControlEventTypeNames.Bell,            // 0x0F
        // EntryControlEventTypeNames.Fire,            // 0x10
        // EntryControlEventTypeNames.Police,          // 0x11
        // EntryControlEventTypeNames.AlertPanic,      // 0x12
        // EntryControlEventTypeNames.AlertMedical,    // 0x13
        // EntryControlEventTypeNames.GateOpen,        // 0x14
        // EntryControlEventTypeNames.GateClose,       // 0x15
        // EntryControlEventTypeNames.Lock,            // 0x16
        // EntryControlEventTypeNames.Unlock,          // 0x17
        // EntryControlEventTypeNames.Test,            // 0x18
        [XmlDataParameter(1, 0x6F, 0x05, new byte[] { 0x03 }, EditType.Config)]
        public List<EntryControlEventTypeNames> SupportedEventTypesList { get; set; } = new List<EntryControlEventTypeNames>();

        [XmlDataParameter(1, 0x6F, 0x05, new byte[] { 0x04 }, EditType.Config)]
        public byte KeyCachedSizeMinimum { get; set; } = 1;

        [XmlDataParameter(1, 0x6F, 0x05, new byte[] { 0x05 }, EditType.Config)]
        public byte KeyCachedSizeMaximum { get; set; } = 32;

        [XmlDataParameter(1, 0x6F, 0x05, new byte[] { 0x06 }, EditType.Config)]
        public byte KeyCachedTimeoutMinimum { get; set; } = 1;

        [XmlDataParameter(1, 0x6F, 0x05, new byte[] { 0x07 }, EditType.Config)]
        public byte KeyCachedTimeoutMaximum { get; set; } = 10;
    }

    public enum EntryControlDataTypeNames : byte
    {
        [Description("0x00 NA")]
        NA = 0,
        [Description("0x01 RAW")]
        RAW = 1,
        [Description("0x02 ASCII")]
        ASCII = 2,
        [Description("0x03 MD5")]
        MD5 = 3,
    };
    public enum EntryControlEventTypeNames : byte
    {
        [Description("0x00 Caching")]
        Caching = 0x00,
        [Description("0x01 Cached keys")]
        CachedKeys = 0x01,
        [Description("0x02 Enter")]
        Enter = 0x02,
        [Description("0x03 Disarm all")]
        DisarmAll = 0x03,
        [Description("0x04 Arm all")]
        ArmAll = 0x04,
        [Description("0x05 Arm away")]
        ArmAway = 0x05,
        [Description("0x06 Arm home")]
        ArmHome = 0x06,
        [Description("0x07 Exit delay")]
        ExitDelay = 0x07,
        [Description("0x08 Arm 1")]
        Arm1 = 0x08,
        [Description("0x09 Arm 2")]
        Arm2 = 0x09,
        [Description("0x0A Arm 3")]
        Arm3 = 0x0A,
        [Description("0x0B Arm 4")]
        Arm4 = 0x0B,
        [Description("0x0C Arm 5")]
        Arm5 = 0x0C,
        [Description("0x0D Arm 6")]
        Arm6 = 0x0D,
        [Description("0x0E RFID")]
        Rfid = 0x0E,
        [Description("0x0F Bell")]
        Bell = 0x0F,
        [Description("0x10 Fire")]
        Fire = 0x10,
        [Description("0x11 Police")]
        Police = 0x11,
        [Description("0x12 Alert panic")]
        AlertPanic = 0x12,
        [Description("0x13 Alert medical")]
        AlertMedical = 0x13,
        [Description("0x14 Gate open")]
        GateOpen = 0x14,
        [Description("0x15 Gate close")]
        GateClose = 0x15,
        [Description("0x16 Lock")]
        Lock = 0x16,
        [Description("0x17 Unlock")]
        Unlock = 0x17,
        [Description("0x18 Test")]
        Test = 0x18,
    }
}
