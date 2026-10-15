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
using Newtonsoft.Json;
using ZWave.Xml.Application;
using static Zats.Services.ZipResult;
using System.Runtime.CompilerServices;
using ZatsServices_netcore.CommandClassesImpl.Definitions.Interfaces;

// ToDo: Supervision status 'Working' for Set Command ?

namespace Zats.Services
{
    [CommandClassName("Simple AV Control Command Class")]
    [CommandClassId(COMMAND_CLASS_SIMPLE_AV_CONTROL.ID)]
    [MinVersion(1)]
    [MaxVersion(4)]
    public class SimpleAvControlSupport : CommandClassSupport
    {
        private SimpleAvControlSupportData SimpleAvControlSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return SimpleAvControlSupportData; }
            set { SimpleAvControlSupportData = (SimpleAvControlSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        private readonly int BitmaskBytesPerReport = 31;

        public SimpleAvControlSupport()
            : this(new SimpleAvControlSupportData() { })
        { }

        public SimpleAvControlSupport(SimpleAvControlSupportData simpleAvControlSupportData)
            : base(COMMAND_CLASS_SIMPLE_AV_CONTROL.ID)
        {
            SimpleAvControlSupportData = simpleAvControlSupportData;
            SimpleAvControlSupportData.SupportClassObject = this;
        }

        public override void StartEmulation()
        {
            SimpleAvControlSupportData.SupportClassObject = this;
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
                if (isHandlingAllowed && command != null && command.Length > 1 && SimpleAvControlSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareReport();
                    }
                    else if (command[1] == COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SUPPORTED_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareSupportedReportCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SET.ID)
                    {
                        ResultForSv = HandleSetCommandCmd(command);
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

        private byte[] PrepareReport()
        {
            COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_REPORT rpt = new COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_REPORT();
            List<byte> supportedAvCommandsBitmask = CommandClassSupportUtils.CreateBitmaskFromList(SimpleAvControlSupportData.SupportedAvCommands.Select(x => x.Command).ToList(), 1);
            rpt.numberOfReports = (byte) (((supportedAvCommandsBitmask.Count - 1) / BitmaskBytesPerReport) + 1);
            return rpt;
        }
        private byte[] PrepareSupportedReport(byte requestedReport)
        {
            List<byte> supportedAvCommandsBitmask = CommandClassSupportUtils.CreateBitmaskFromList(SimpleAvControlSupportData.SupportedAvCommands.Select(x => x.Command).ToList(), 1, true);

            int numberOfReports = ((supportedAvCommandsBitmask.Count - 1) / BitmaskBytesPerReport) + 1;
            COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SUPPORTED_REPORT rpt = new COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SUPPORTED_REPORT();
            List<byte> bitmasks = new List<byte> { };
            rpt.reportNo = requestedReport;
            if (requestedReport >= 1 && requestedReport <= numberOfReports)
            {
                int index = (requestedReport - 1) * BitmaskBytesPerReport;
                while (index < supportedAvCommandsBitmask.Count && index < (requestedReport - 0) * BitmaskBytesPerReport)
                {
                    bitmasks.Add(supportedAvCommandsBitmask[index++]);
                }
            }
            if (bitmasks.Count == 0)
            {
                bitmasks.Add(0x00); // force Report if requestedReport is invalid
            }
            rpt.bitMask = bitmasks;
            return rpt;
        }
        private byte[] PrepareSupportedReportCmd(byte[] command)
        {
            COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SUPPORTED_GET getCmd = command;
            return PrepareSupportedReport(getCmd.reportNo);
        }

        public SvResultNames HandleSetCommand(SimpleAvKeyAttributeNames keyAttributes, List<ushort> avCommandsList, byte sequenceNumber, bool updateUI)
        {
            SvResultNames success = SvResultNames.Successful;

            if (sequenceNumber != SimpleAvControlSupportData.LastReceivedSequenceNumber)
            {
                SimpleAvControlSupportData.LastReceivedSequenceNumber = (byte)((SimpleAvControlSupportData.LastReceivedSequenceNumber + 1) % 256);
                if (keyAttributes == SimpleAvKeyAttributeNames.KeyDown
                 || keyAttributes == SimpleAvKeyAttributeNames.KeyUp
                 || keyAttributes == SimpleAvKeyAttributeNames.KeepAlive)
                {
                    SimpleAvControlSupportData.LastReceivedKeyAttribute = keyAttributes;
                    SimpleAvControlSupportData.LastReceivedAvCommands = string.Join(", ", avCommandsList);
                    foreach (ushort avCommand in avCommandsList)
                    {
                        if (SimpleAvControlSupportData.SupportedAvCommands.Find(x => x.Command == avCommand) != null)
                        {
                            CommandClassSupportUtils.ReportAddLog(SimpleAvControlSupportData.LogLevel,
                                        "{0} CC @ {1}: AV Command {2} = 0x{2:X2} accepted. Key Attributes: {3}",
                                        CommandClassShortName, EndPointName, avCommand, keyAttributes);
                            if (updateUI)
                            {
                                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
                            }
                        }
                        else
                        {
                            success = SvResultNames.Failed;
                            CommandClassSupportUtils.ReportAddLog(SimpleAvControlSupportData.LogLevel,
                                        "{0} CC @ {1}: AV Command {2} = 0x{2:X2} ignored (not supported). Key Attributes: {3}",
                                        CommandClassShortName, EndPointName, avCommand, keyAttributes);
                        }
                    }
                    return success;
                }
            }
            CommandClassSupportUtils.ReportAddLog(SimpleAvControlSupportData.LogLevel,
                        "{0} CC @ {1}: AV Command(s) ignored. Key Attributes {2} or Sequence Number {3} invalid.",
                        CommandClassShortName, EndPointName, (byte)keyAttributes, sequenceNumber);
            return SvResultNames.Failed;
        }
        public SvResultNames HandleSetCommandCmd(byte[] command)
        {
            COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SET setCmd = command;
            List<COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SET.TVG> avCommands = setCmd.vg;
            List<ushort> avCommandsList = new List<ushort>();

            foreach (COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SET.TVG avCommandVg in avCommands)
            {
                avCommandsList.Add((ushort)CommandClassSupportUtils.ByteFieldToUInt64(avCommandVg.command, 2));
            }
            return HandleSetCommand((SimpleAvKeyAttributeNames)setCmd.properties1.keyAttributes, avCommandsList, setCmd.sequenceNumber, true);
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
                case COMMAND_CLASS_SIMPLE_AV_CONTROL.SIMPLE_AV_CONTROL_SET.ID:
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
            if (data is SimpleAvControlSupportData newData)
            {
                bool isSupportedListChanged = !CommandClassSupportUtils.ListElementsEqual(SimpleAvControlSupportData.SupportedAvCommands, newData.SupportedAvCommands);

                SimpleAvControlSupportData.LastReceivedSequenceNumber = newData.LastReceivedSequenceNumber;
                if (isSupportedListChanged)
                {
                    SimpleAvControlSupportData.SupportedAvCommands = newData.SupportedAvCommands;
                    if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        SendUnsolicitedReport(PrepareReport(), NodeTag.Empty);
                        SendUnsolicitedReport(PrepareSupportedReport(1), NodeTag.Empty);
                        SendUnsolicitedReport(PrepareSupportedReport(2), NodeTag.Empty);
                    }
                }

                // TEST HandleSetCommand(SimpleAvKeyAttributeNames.KeyDown, new List<ushort> { 4, 125, 465 }, 118, false);

                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class SupportedAvCommand : INestedItem
    {
        public ushort Command { get; set;}

        // only used for EDE UI. see interface for useage notes.
        [JsonIgnore]
        public int NestedId
        {
            get => (int)Command;
            set { }
        }

        public SupportedAvCommand(ushort cmd)
        {
            Command = cmd;  
        }
    }

    public class SimpleAvControlSupportData : CommandClassSupportData, IBasicMapping, INodeItem
    {
        // Internals accessible from TestCases or EDE
        public byte LastReceivedSequenceNumber { get; set; } = 0;

        // This list contains all supported (added) AV commands.
        [NestedControl(1, "Supported AV Commands")]
        public List<SupportedAvCommand> SupportedAvCommands { get; set; } = new List<SupportedAvCommand>();

        // These values hold the content of the last received Simple AV Control Set Command.
        public SimpleAvKeyAttributeNames LastReceivedKeyAttribute { get; set; } = SimpleAvKeyAttributeNames.Undefined;
        public string LastReceivedAvCommands { get; set; } = "";

        [JsonIgnore]
        public IEnumerable<INestedItem> ChildItems => SupportedAvCommands;

        // The following methods are only used by some Test Cases.
        private readonly int[] HighestAvCommands = { 0, 363, 377, 383, 465 };
        public bool AddSupportedAvCommand(ushort avCommand)
        {
            //if (avCommand >= 1 && avCommand <= HighestAvCommands[SupportClassObject.Version]) // Version could not be available!
            if (avCommand >= 1 && avCommand <= HighestAvCommands[4])
            {
                if (SupportedAvCommands.Find(x => x.Command == avCommand) == null)
                {
                    SupportedAvCommands.Add(new SupportedAvCommand(avCommand));
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
                if (!AddSupportedAvCommand(avCommand))
                {
                    success = false;
                }
            }
            return success;
        }
        public bool RemoveSupportedAvCommand(ushort avCommand)
        {
            var cmd = SupportedAvCommands.Find(x => x.Command == avCommand);
            return cmd != null && SupportedAvCommands.Remove(cmd);
        }
        public bool RemoveSupportedAvCommands(List<ushort> avCommands)
        {
            bool success = true;
            foreach (ushort avCommand in avCommands)
            {
                if (!RemoveSupportedAvCommand(avCommand))
                {
                    success = false;
                }
            }
            return success;
        }

        // Basic CC Mapping
        [JsonIgnore]
        public SimpleAvControlSupport SupportClassObject;
        bool IBasicMapping.BasicSet(byte value, NodeTag srcNode)
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
                        avCommandsVg[0].command = new byte[] { (byte)(SupportedAvCommands[0].Command / 256), (byte)(SupportedAvCommands[0].Command % 256) };
                    }
                    if (value == 0x00)
                    {
                        avCommandsVg[0].command = new byte[] { (byte)(SupportedAvCommands[1].Command / 256), (byte)(SupportedAvCommands[1].Command % 256) };
                    }
                    setCmd.vg = avCommandsVg;
                    CommandClassSupportUtils.ReportAddLog(LogLevel, 
                                "{0} CC @ {1}: Basic Set with value 0x{2:X2} accepted.", 
                                SupportClassObject.CommandClassShortName, SupportClassObject.EndPointName, value);
                    return SupportClassObject.HandleSetCommandCmd(setCmd) == SvResultNames.Successful;
                }
            }
            CommandClassSupportUtils.ReportAddLog(LogLevel, 
                        "{0} CC @ {1}: Basic Set with value 0x{2:X2} ignored.", 
                        SupportClassObject.CommandClassShortName, SupportClassObject.EndPointName, value);
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
        [Description("0x00 Key down")]
        KeyDown = 0x00,
        [Description("0x01 Key up")]
        KeyUp = 0x01,
        [Description("0x02 Keep alive")]
        KeepAlive = 0x02,
        [Description("0xFF Undefined")]
        Undefined = 0xFF,
    };
}
