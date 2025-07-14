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

// ToDo: Supervision status 'Working' for Set Command ?

namespace Zats.Services
{
    public class SimpleAvControlSupport : CommandClassSupport
    {
        private SimpleAvControlSupportData SimpleAVSupportData { get; set; }

        public readonly string CommandClassName = "Simple AV Control CC";
        public string EndPointName = "";
        private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private readonly int BitmaskBytesPerReport = 31;

        public SimpleAvControlSupport()
            : this(new SimpleAvControlSupportData() { })
        { }

        public SimpleAvControlSupport(SimpleAvControlSupportData simpleAVSupportData)
            : base(COMMAND_CLASS_SIMPLE_AV_CONTROL.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 4;
            SimpleAVSupportData = simpleAVSupportData;
            SimpleAVSupportData.SupportClassObject = this;
            LogLevel = SimpleAVSupportData.LogLevel;
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
                if (isResponseAllowed && command != null && command.Length > 1 && SimpleAVSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_GET.ID)
                    {
                        rpt = PrepareReport();
                    }
                    else if (command[1] == COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SUPPORTED_GET.ID)
                    {
                        rpt = PrepareSupportedReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SET.ID)
                    {
                        ResultForSv = HandleSetCommand(command);
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

        private byte[] PrepareReport()
        {
            COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_REPORT rpt = new COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_REPORT() { };
            rpt.numberOfReports = (byte) (((SimpleAVSupportData.SupportedAvCommandsBitmask.Count - 1) / BitmaskBytesPerReport) + 1);
            return rpt;
        }
        private byte[] PrepareSupportedReport(byte[] command)
        {
            COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SUPPORTED_GET getCmd = command;
            int numberOfReports = ((SimpleAVSupportData.SupportedAvCommandsBitmask.Count - 1) / BitmaskBytesPerReport) + 1;
            byte requestedReport = getCmd.reportNo;
            if (requestedReport >= 1 && requestedReport <= numberOfReports)
            {
                COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SUPPORTED_REPORT rpt = new COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SUPPORTED_REPORT() { };
                int index = (requestedReport - 1) * BitmaskBytesPerReport;
                List<byte> bitmasks = new List<byte> { };
                while (index < SimpleAVSupportData.SupportedAvCommandsBitmask.Count && index < (requestedReport - 0) * BitmaskBytesPerReport)
                {
                    bitmasks.Add(SimpleAVSupportData.SupportedAvCommandsBitmask[index++]);
                }
                rpt.reportNo = requestedReport;
                rpt.bitMask = bitmasks;
                return rpt;
            }
            return null;
        }

        public SvResultNames HandleSetCommand(byte[] command)
        {
            COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SET setCmd = command;
            SimpleAvKeyAttributeNames keyAttributes = (SimpleAvKeyAttributeNames) setCmd.properties1.keyAttributes;
            List<COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SET.TVG> avCommands = setCmd.vg;
            SvResultNames success = SvResultNames.Successful;

            if (setCmd.sequenceNumber != SimpleAVSupportData.LastReceivedSequenceNumber)
            {
                SimpleAVSupportData.LastReceivedSequenceNumber = (byte) ((SimpleAVSupportData.LastReceivedSequenceNumber + 1) % 256);
                if (keyAttributes == SimpleAvKeyAttributeNames.KeyDown
                 || keyAttributes == SimpleAvKeyAttributeNames.KeyUp
                 || keyAttributes == SimpleAvKeyAttributeNames.KeepAlive)
                {
                    foreach (COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SET.TVG avCommandVg in avCommands)
                    {
                        ushort avCommand = (ushort) CommandClassSupportUtils.ByteFieldToUInt64(avCommandVg.command, 2);
                        if (SimpleAVSupportData.SupportedAvCommands.Contains(avCommand))
                        {
                            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: AV Command {2} = 0x{2:X2} accepted. Key Attributes: {3}", 
                                        CommandClassName, EndPointName, avCommand, keyAttributes);
                        }
                        else
                        {
                            success = SvResultNames.Failed;
                            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: AV Command {2} = 0x{2:X2} ignored (not supported). Key Attributes: {3}",
                                        CommandClassName, EndPointName, avCommand, keyAttributes);
                        }
                    }
                    return success;
                }
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: AV Command(s) ignored. Key Attributes {2} or Sequence Number {3} invalid.",
                        CommandClassName, EndPointName, (byte) keyAttributes, setCmd.sequenceNumber);
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
                case COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }
    }

    public class SimpleAvControlSupportData : IBasicMapping
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
        public byte LastReceivedSequenceNumber { get; set; } = 0;

        // This byte list contains all supported (added) AV commands.
        public List<ushort> SupportedAvCommands { get; set; } = new List<ushort> { };

        // This byte list represents the Bit Mask fields of the Simple AV Control Supported Report Command.
        // List length is dynamically, depending from the supported (added) AV commands.
        // Use the SimpleAVSupportData.AddSupportedAvCommand methods to add AV commands and update the Supported Commands Bit Mask fields.
        public List<byte> SupportedAvCommandsBitmask { get; set; } = new List<byte> { };


        private readonly int HighestAvCommandsV4 = 465;
        //private readonly int[] HighestAvCommands = { 0, 363, 377, 383, 465 };
        public bool AddSupportedAvCommand(ushort avCommand)
        {
            //if (avCommand >= 1 && avCommand <= HighestAvCommands[SupportClassObject.Version]) // Version could not be available!
            if (avCommand >= 1 && avCommand <= HighestAvCommandsV4)
            {
                if (!SupportedAvCommands.Contains(avCommand))
                {
                    SupportedAvCommands.Add(avCommand);
                    UpdateSupportedAvCommandsBitmask(avCommand);
                    return true;
                }
            }
            return false;
        }
        public bool AddSupportedAvCommands(List<ushort> avCommands)
        {
            bool success = true;
            foreach (ushort avCommand in avCommands)
            {
                if (AddSupportedAvCommand(avCommand) == false)
                {
                    success = false;
                }
            }
            return success;
        }
        private void UpdateSupportedAvCommandsBitmask(ushort avCommand)
        {
            byte byteIndex, bit;
            //if (avCommand >= 1 && avCommand <= HighestAvCommands[SupportClassObject.Version]) // Version could not be available
            if (avCommand >= 1 && avCommand <= HighestAvCommandsV4)
            {
                byteIndex = (byte) ((avCommand - 1) / 8);    // Bit 0 represents command #1
                bit = (byte) ((avCommand - 1) % 8);
                while (byteIndex >= SupportedAvCommandsBitmask.Count)
                {
                    SupportedAvCommandsBitmask.Add(0x00);
                }
                SupportedAvCommandsBitmask[byteIndex] |= (byte)(1 << bit);
            }
        }

        // Basic CC Mapping
        public SimpleAvControlSupport SupportClassObject;
        bool IBasicMapping.BasicSet(byte value)
        {
            if (SupportClassObject.Version <= 4)
            {
                if (SupportedAvCommands.Count > 2 && (value == 0x00 || value == 0xFF))
                {
                    COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SET setCmd = new COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SET() { };
                    setCmd.sequenceNumber = (byte)((LastReceivedSequenceNumber + 1) % 256);
                    setCmd.properties1.keyAttributes = (byte)SimpleAvKeyAttributeNames.KeyDown;
                    setCmd.reserved2 = new byte[2] { 0x00, 0x00 };
                    List<COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SET.TVG> avCommandsVg = new List<COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SET.TVG>() { };
                    avCommandsVg.Add(new COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SET.TVG());
                    if (value == 0xFF)
                    {
                        avCommandsVg[0].command = new byte[] { (byte)(SupportedAvCommands[0] / 256), (byte)(SupportedAvCommands[0] % 256) };
                    }
                    if (value == 0x00)
                    {
                        avCommandsVg[0].command = new byte[] { (byte)(SupportedAvCommands[1] / 256), (byte)(SupportedAvCommands[1] % 256) };
                    }
                    setCmd.vg = avCommandsVg;
                    CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Basic Set with value 0x{2:X2} accepted.", 
                                SupportClassObject.CommandClassName, SupportClassObject.EndPointName, value);
                    return SupportClassObject.HandleSetCommand(setCmd) == SvResultNames.Successful;
                }
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, "{0}{1}: Basic Set with value 0x{2:X2} ignored.", 
                        SupportClassObject.CommandClassName, SupportClassObject.EndPointName, value);
            return false;
        }
        COMMAND_CLASS_BASIC_V2.BASIC_REPORT IBasicMapping.BasicV2Get()
        {
            COMMAND_CLASS_BASIC_V2.BASIC_REPORT rpt = new COMMAND_CLASS_BASIC_V2.BASIC_REPORT() { };
            rpt.currentValue = 0x00;
            rpt.targetValue = 0;
            rpt.duration = 0;
            return rpt;
        }
    }
    public enum SimpleAvKeyAttributeNames : byte
    {
        KeyDown = 0x00,
        KeyUp = 0x01,
        KeepAlive = 0x02,
    };
}
