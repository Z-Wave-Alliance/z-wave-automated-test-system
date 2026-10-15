// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using Utils;
using Utils.UI.Enums;
using ZatsServices_netcore.CommandClassesImpl.Definitions.Interfaces;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;
using ZWave.Xml.Application;

// ToDo: Supervision Report Status 'Working' for SetpointSet (optional)

namespace Zats.Services
{
    [CommandClassName("Thermostat Setpoint Command Class")]
    [CommandClassId(COMMAND_CLASS_THERMOSTAT_SETPOINT.ID)]
    [MinVersion(1)]
    [MaxVersion(3)]
    public class ThermostatSetpointSupport : CommandClassSupport
    {
        private ThermostatSetpointSupportData ThermostatSetpointSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return ThermostatSetpointSupportData; }
            set { ThermostatSetpointSupportData = (ThermostatSetpointSupportData)value; }
        }

        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public ThermostatSetpointSupport()
            : this(new ThermostatSetpointSupportData() { })
        { }

        public ThermostatSetpointSupport(ThermostatSetpointSupportData thermostatSetpointSupportData)
            : base(COMMAND_CLASS_THERMOSTAT_SETPOINT.ID)
        {
            ThermostatSetpointSupportData = thermostatSetpointSupportData;
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
                if (isHandlingAllowed && command != null && command.Length > 1 && ThermostatSetpointSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_THERMOSTAT_SETPOINT.THERMOSTAT_SETPOINT_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareReportCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_THERMOSTAT_SETPOINT.THERMOSTAT_SETPOINT_SUPPORTED_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareSupportedReport();
                    }
                    else if (command[1] == COMMAND_CLASS_THERMOSTAT_SETPOINT_V3.THERMOSTAT_SETPOINT_CAPABILITIES_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareCapabilitiesReportCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_THERMOSTAT_SETPOINT.THERMOSTAT_SETPOINT_SET.ID)
                    {
                        ResultForSv = HandleSetpointSetCmd(command, sendSrcNode);
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

        private byte[] PrepareReport(byte requestedSetpointType)
        {
            COMMAND_CLASS_THERMOSTAT_SETPOINT.THERMOSTAT_SETPOINT_REPORT rpt = new COMMAND_CLASS_THERMOSTAT_SETPOINT.THERMOSTAT_SETPOINT_REPORT() { };
            ThermostatSetpoint requestedSetpoint = ThermostatSetpointSupportData.Setpoints.Find(setpoint => (byte)setpoint.SetpointType == requestedSetpointType);
            if (requestedSetpoint != null && ValidateSetpointType(requestedSetpointType))
            {
                byte prec = requestedSetpoint.CurrentValuePrecision; //Math.Max(requestedSetpoint.MinValuePrecision, requestedSetpoint.MaxValuePrecision);
                byte size = requestedSetpoint.CurrentValueSize;
                if (ThermostatSetpointSupportData.DynamicSizeCalculation)
                {
                    size = CommandClassSupportUtils.CalcByteFieldSize(requestedSetpoint.CurrentValue, prec);
                }
                rpt.properties1.setpointType = requestedSetpointType;
                rpt.properties2.scale = (byte)requestedSetpoint.MaxValueScale;
                rpt.properties2.precision = prec;
                rpt.properties2.size = size;
                rpt.value = CommandClassSupportUtils.ValueToByteField(requestedSetpoint.CurrentValue, size, prec);
            }
            else
            {
                rpt.properties1.setpointType = (byte)ThermostatSetpointTypes.N_A;
                rpt.properties2.scale = 0;
                rpt.properties2.precision = 0;
                rpt.properties2.size = 1;
                rpt.value = CommandClassSupportUtils.ValueToByteField(0.0, 1, 0);
            }
            return rpt;

        }
        private byte[] PrepareReportCmd(byte[] command)
        {
            COMMAND_CLASS_THERMOSTAT_SETPOINT.THERMOSTAT_SETPOINT_GET getCmd = command;
            return PrepareReport(getCmd.properties1.setpointType);
        }
        private byte[] PrepareSupportedReport()
        {
            if (Version >= 1)
            {
                COMMAND_CLASS_THERMOSTAT_SETPOINT.THERMOSTAT_SETPOINT_SUPPORTED_REPORT rpt = new COMMAND_CLASS_THERMOSTAT_SETPOINT.THERMOSTAT_SETPOINT_SUPPORTED_REPORT() { };
                rpt.bitMask = ThermostatSetpointSupportData.CreateSupportedSetpointTypesBitmask();
                return rpt;
            }
            return null;
        }
        private byte[] PrepareCapabilitiesReport(byte requestedSetpointType)
        {
            if (Version >= 3)
            {
                COMMAND_CLASS_THERMOSTAT_SETPOINT_V3.THERMOSTAT_SETPOINT_CAPABILITIES_REPORT rpt = new COMMAND_CLASS_THERMOSTAT_SETPOINT_V3.THERMOSTAT_SETPOINT_CAPABILITIES_REPORT() { };
                ThermostatSetpoint requestedSetpoint = ThermostatSetpointSupportData.Setpoints.Find(setpoint => (byte)setpoint.SetpointType == requestedSetpointType);
                if (requestedSetpoint != null && ValidateSetpointType(requestedSetpointType))
                {
                    byte precMin = requestedSetpoint.MinValuePrecision;
                    byte sizeMin = requestedSetpoint.MinValueSize;
                    if (ThermostatSetpointSupportData.DynamicSizeCalculation)
                    {
                        sizeMin = CommandClassSupportUtils.CalcByteFieldSize(requestedSetpoint.MinValue, precMin);
                    }
                    rpt.properties1.setpointType = requestedSetpointType;
                    rpt.properties2.scale1 = (byte)requestedSetpoint.MinValueScale;
                    rpt.properties2.precision1 = precMin;
                    rpt.properties2.size1 = sizeMin;
                    rpt.minValue = CommandClassSupportUtils.ValueToByteField(requestedSetpoint.MinValue, sizeMin, precMin);
                    byte precMax = requestedSetpoint.MaxValuePrecision;
                    byte sizeMax = requestedSetpoint.MaxValueSize;
                    if (ThermostatSetpointSupportData.DynamicSizeCalculation)
                    {
                        sizeMax = CommandClassSupportUtils.CalcByteFieldSize(requestedSetpoint.MaxValue, precMax);
                    }
                    rpt.properties3.scale2 = (byte)requestedSetpoint.MaxValueScale;
                    rpt.properties3.precision2 = precMax;
                    rpt.properties3.size2 = sizeMax;
                    rpt.maxvalue = CommandClassSupportUtils.ValueToByteField(requestedSetpoint.MaxValue, sizeMax, precMax);
                }
                else
                {
                    rpt.properties1.setpointType = (byte)ThermostatSetpointTypes.N_A;
                    rpt.properties2.scale1 = 0;
                    rpt.properties2.precision1 = 0;
                    rpt.properties2.size1 = 1;
                    rpt.minValue = CommandClassSupportUtils.ValueToByteField(0.0, 1, 0);
                    rpt.properties3.scale2 = 0;
                    rpt.properties3.precision2 = 0;
                    rpt.properties3.size2 = 1;
                    rpt.maxvalue = CommandClassSupportUtils.ValueToByteField(0.0, 1, 0);
                }
                return rpt;
            }
            return null;
        }
        private byte[] PrepareCapabilitiesReportCmd(byte[] command)
        {
            if (Version >= 3)
            {
                COMMAND_CLASS_THERMOSTAT_SETPOINT_V3.THERMOSTAT_SETPOINT_CAPABILITIES_GET getCmd = command;
                return PrepareCapabilitiesReport(getCmd.properties1.setpointType);
            }
            return null;
        }

        private SvResultNames HandleSetpointSet(byte requestedSetpointType, double newValue, bool updateUI, NodeTag srcNode)
        {
            ThermostatSetpoint requestedSetpoint = ThermostatSetpointSupportData.Setpoints.Find(setpoint => (byte)setpoint.SetpointType == requestedSetpointType);
            if (requestedSetpoint != null && ValidateSetpointType(requestedSetpointType))
            {
                requestedSetpoint.CurrentValue = newValue;
                if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                {
                    SendUnsolicitedReport(PrepareReport(requestedSetpointType), srcNode);
                }
                if (updateUI)
                {
                    SupportDelegates?.DeviceDataUpdate?.Invoke(this); // inform (EDE) about current state
                }
                return SvResultNames.Successful;
            }
            return SvResultNames.Failed;
        }
        private SvResultNames HandleSetpointSetCmd(byte[] command, NodeTag srcNode)
        {
            COMMAND_CLASS_THERMOSTAT_SETPOINT.THERMOSTAT_SETPOINT_SET setCmd = command;
            byte requestedSetpointType = setCmd.properties1.setpointType;
            ThermostatSetpoint requestedSetpoint = ThermostatSetpointSupportData.Setpoints.Find(setpoint => (byte)setpoint.SetpointType == requestedSetpointType);
            if (requestedSetpoint != null && ValidateSetpointType(requestedSetpointType))
            {
                byte size = setCmd.properties2.size;
                byte precision = setCmd.properties2.precision;
                byte scale = setCmd.properties2.scale;
                if (setCmd.value.Count == size && (size == 1 || size == 2 || size == 4) && precision <= 7 && scale <= 1)
                {
                    return HandleSetpointSet(requestedSetpointType, CommandClassSupportUtils.ByteListToValue(setCmd.value, size, precision), true, srcNode);
                }
            }
            return SvResultNames.Failed;
        }

        public bool ValidateSetpointType(byte setpointType)
        {
            if (setpointType == 0x01 || setpointType == 0x02 || (setpointType >= 0x07 && setpointType <= 0x0F))
            {
                if ((Version == 1 && setpointType <= 0x0A)
                 || (Version == 2 && setpointType <= 0x0D)
                 || (Version == 3 && setpointType <= 0x0F))
                {
                    return true;
                }
            }
            return false;
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet,
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != CommandClassId)
            {
                supervisionReport.status = (byte)SvStatusNames.NoSupport;
                return;
            }

            supervisionReport.status = (byte)SvStatusNames.Fail;  // FAIL as default
            switch (supervisionGet.encapsulatedCommand[1])
            {
                case COMMAND_CLASS_THERMOSTAT_SETPOINT.THERMOSTAT_SETPOINT_SET.ID:
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
            if (data is ThermostatSetpointSupportData newData)
            {
                bool isSupportChanged = false;

                List<byte[]> capabilityReports = new List<byte[]>();

                List<ThermostatSetpoint> setpointsRemoved = ThermostatSetpointSupportData.Setpoints.FindAll(x => newData.Setpoints.Find(y => y.SetpointType == x.SetpointType) == null);
                //List<ThermostatSetpoint> setpointsAdded   = newData.Setpoints.FindAll(x => ThermostatSetpointSupportData.Setpoints.Find(y => y.SetpointType == x.SetpointType) == null);
                if (setpointsRemoved.Count > 0 /*|| setpointsAdded.Count > 0*/)
                {
                    isSupportChanged = true;
                    setpointsRemoved.ForEach(x => ThermostatSetpointSupportData.Setpoints.Remove(x));
                }

                ThermostatSetpointSupportData.DynamicSizeCalculation = newData.DynamicSizeCalculation;

                foreach (ThermostatSetpoint newSetpoint in newData.Setpoints)
                {
                    if (ThermostatSetpointSupportData.Setpoints.Find(x => x.SetpointType == newSetpoint.SetpointType) is ThermostatSetpoint currentSetpoint)
                    {
                        if (currentSetpoint.MinValueSize != newSetpoint.MinValueSize
                         || currentSetpoint.MinValuePrecision != newSetpoint.MinValuePrecision
                         || currentSetpoint.MinValueScale != newSetpoint.MinValueScale
                         || currentSetpoint.MinValue != newSetpoint.MinValue
                         || currentSetpoint.MaxValueSize != newSetpoint.MaxValueSize
                         || currentSetpoint.MaxValuePrecision != newSetpoint.MaxValuePrecision
                         || currentSetpoint.MaxValueScale != newSetpoint.MaxValueScale
                         || currentSetpoint.MaxValue != newSetpoint.MaxValue)
                        {
                            currentSetpoint.MinValueSize = newSetpoint.MinValueSize;
                            currentSetpoint.MinValuePrecision = newSetpoint.MinValuePrecision;
                            currentSetpoint.MinValueScale = newSetpoint.MinValueScale;
                            currentSetpoint.MinValue = newSetpoint.MinValue;
                            currentSetpoint.MaxValueSize = newSetpoint.MaxValueSize;
                            currentSetpoint.MaxValuePrecision = newSetpoint.MaxValuePrecision;
                            currentSetpoint.MaxValueScale = newSetpoint.MaxValueScale;
                            currentSetpoint.MaxValue = newSetpoint.MaxValue;
                            if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                            {
                                capabilityReports.Add(PrepareCapabilitiesReport((byte)currentSetpoint.SetpointType));
                            }
                        }
                        if (currentSetpoint.CurrentValue != newSetpoint.CurrentValue)
                        {
                            HandleSetpointSet((byte)newSetpoint.SetpointType, newSetpoint.CurrentValue, false, NodeTag.Empty);
                        }
                    }
                    else
                    {
                        isSupportChanged = true;
                        ThermostatSetpointSupportData.Setpoints.Add(newSetpoint);
                        if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                        {
                            capabilityReports.Add(PrepareCapabilitiesReport((byte)newSetpoint.SetpointType));
                        }
                    }
                }

                if (isSupportChanged && ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                {
                    SendUnsolicitedReport(PrepareSupportedReport(), NodeTag.Empty);
                    foreach (byte[] r in capabilityReports)
                    {
                        SendUnsolicitedReport(r, NodeTag.Empty);
                    }
                }

                SupportDelegates?.DeviceDataUpdate?.Invoke(this); // always inform (EDE) about current state
            }
        }
    }

    public class ThermostatSetpoint : INestedItem
    {
        // V1+ Report
        public ThermostatSetpointTypes SetpointType { get; set; }

        [ParameterSeparator(1, "Current Value")]
        [CustomDataParameter(1, "Value", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Double, EditType.Config | EditType.Edit)]
        public double CurrentValue { get; set; } = 0.0;

        [CustomDataParameter(1, "Size", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Int_0_9, EditType.Config | EditType.Edit)]
        public byte CurrentValueSize { get; set; } = 1;

        [CustomDataParameter(1, "Precision", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Int_0_7, EditType.Config | EditType.Edit)]
        public byte CurrentValuePrecision { get; set; } = 0;

        [CustomDataParameter(1, "Scale", CommandClassesImpl.UiControl.ListComboBox, CommandClassesImpl.InputType.Enum, EditType.Config, ItemsSource = typeof(ThermostatSetpointScales))]
        public ThermostatSetpointScales CurrentValueScale { get; set; } = ThermostatSetpointScales.Celcius;


        // V3+ Capabilities Report
        [ParameterSeparator(3, "Minimum Value")]
        [CustomDataParameter(3, "Value", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Double, EditType.Config | EditType.Edit)]
        public double MinValue { get; set; } = 0.0;

        [CustomDataParameter(3, "Size", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Int_0_9, EditType.Config | EditType.Edit)]
        public byte MinValueSize { get; set; } = 1;         // 0 = unknown; 1, 2, 4 = size

        [CustomDataParameter(3, "Precision", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Int_0_7, EditType.Config | EditType.Edit)]
        public byte MinValuePrecision { get; set; } = 0;    //

        [CustomDataParameter(3, "Scale", CommandClassesImpl.UiControl.ListComboBox, CommandClassesImpl.InputType.Enum, EditType.Config, ItemsSource = typeof(ThermostatSetpointScales))]
        public ThermostatSetpointScales MinValueScale { get; set; } = 0;        // 0 = Celsius; 1 = Fahrenheit

        [ParameterSeparator(3, "Maximum Value")]
        [CustomDataParameter(3, "Value", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Double, EditType.Config | EditType.Edit)]
        public double MaxValue { get; set; } = 0.0;

        [CustomDataParameter(3, "Size", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Int_0_9, EditType.Config | EditType.Edit)]
        public byte MaxValueSize { get; set; } = 1;         // 0 = unknown; 1, 2, 4 = size

        [CustomDataParameter(3, "Precision", CommandClassesImpl.UiControl.TextBox, CommandClassesImpl.InputType.Int_0_7, EditType.Config | EditType.Edit)]
        public byte MaxValuePrecision { get; set; } = 0;    //

        [CustomDataParameter(3, "Scale", CommandClassesImpl.UiControl.ListComboBox, CommandClassesImpl.InputType.Enum, EditType.Config, ItemsSource = typeof(ThermostatSetpointScales))]
        public ThermostatSetpointScales MaxValueScale { get; set; } = 0;        // 0 = Celsius; 1 = Fahrenheit
        public int NestedId //{ get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        {
            get => (byte)SetpointType;
            set { }
        }

        public ThermostatSetpoint()
        {

        }
        public ThermostatSetpoint(ThermostatSetpointTypes setpointType, byte minPrecision, byte maxPrecision, ThermostatSetpointScales minScale, ThermostatSetpointScales maxScale, byte minSize, byte maxSize, double minValue, double maxValue, double currentValue)
        {
            SetpointType = setpointType;
            CurrentValue = currentValue;
            MinValueSize = minSize;
            MinValuePrecision = minPrecision;
            MinValueScale = minScale;
            MinValue = minValue;
            MaxValueSize = maxSize;
            MaxValuePrecision = maxPrecision;
            MaxValueScale = maxScale;
            MaxValue = maxValue;
        }

        public static List<ThermostatSetpointTypes> GetAllSupportedSetpointTypes()
        {
            return Enum.GetValues(typeof(ThermostatSetpointTypes)).Cast<ThermostatSetpointTypes>().ToList();
        }
    }
    public class ThermostatSetpointSupportData : CommandClassSupportData, INodeItem
    {
        // Internals accessible from TestCases or EDE

        [JsonIgnore]
        public IEnumerable<INestedItem> ChildItems => Setpoints;

        [NestedControl(1, "Setpoints")]
        public List<ThermostatSetpoint> Setpoints { get; set; } = new List<ThermostatSetpoint>();
        public bool DynamicSizeCalculation { get; set; } = false;

        public ThermostatSetpoint AddSetpoint(ThermostatSetpointTypes setpointTypeName, byte minPrecision, byte maxPrecision, ThermostatSetpointScales minScale, ThermostatSetpointScales maxScale, byte minSize, byte maxSize, double minValue, double maxValue, double currentValue)
        {
            byte setpointType = (byte)setpointTypeName;
            if (setpointType == 0x01 || setpointType == 0x02 || (setpointType >= 0x07 && setpointType <= 0x0F))
            {
                if (Setpoints.Find(setpoint => (byte)setpoint.SetpointType == setpointType) == null)
                {
                    ThermostatSetpoint obj = new ThermostatSetpoint((ThermostatSetpointTypes)setpointType, minPrecision, maxPrecision, minScale, maxScale, minSize, maxSize, minValue, maxValue, currentValue);
                    Setpoints.Add(obj);
                    return obj;
                }
            }
            return null;
        }

        internal List<byte> CreateSupportedSetpointTypesBitmask()
        {
            List<byte> supportedSetpointTypesBitmask = new List<byte>();
            foreach (ThermostatSetpoint setpoint in Setpoints)
            {
                byte setpointType = (byte)setpoint.SetpointType;
                if (setpointType == 0x01 || setpointType == 0x02 || (setpointType >= 0x07 && setpointType <= 0x0F))
                {
                    byte byteIndex, bit;
                    if (setpointType == 0x01 || setpointType == 0x02)
                    {
                        byteIndex = 0;
                        bit = setpointType;
                    }
                    else
                    {
                        byteIndex = (byte)((setpointType - 4) / 8); // map 0x07 to Byte 1 Bit 3 and so on
                        bit = (byte)((setpointType - 4) % 8);
                    }
                    while (byteIndex >= supportedSetpointTypesBitmask.Count)
                    {
                        supportedSetpointTypesBitmask.Add(0x00);
                    }
                    supportedSetpointTypesBitmask[byteIndex] |= (byte) (1 << bit);
                }
            }
            return supportedSetpointTypesBitmask;
        }
    }

    public enum ThermostatSetpointTypes : byte
    {
        [Description("0x00 N/A")]
        N_A = 0x00,
        [Description("0x01 Heating")]
        Heating = 0x01,
        [Description("0x02 Cooling")]
        Cooling = 0x02,
        [Description("0x07 Furnace")]
        Furnace = 0x07,
        [Description("0x08 Dry air")]
        DryAir = 0x08,
        [Description("0x09 Moist air")]
        MoistAir = 0x09,
        [Description("0x0A Auto changeover")]
        AutoChangeover = 0x0A,
        [Description("0x0B Energy save heating")]
        EnergySaveHeating = 0x0B,
        [Description("0x0C Energy save cooling ")]
        EnergysaveCooling = 0x0C,
        [Description("0x0D Away heating")]
        AwayHeating = 0x0D,
        [Description("0x0E Away cooling")]
        AwayCooling = 0x0E,
        [Description("0x0F Full power")]
        FullPower = 0x0F
    };

    public enum ThermostatSetpointScales : byte
    {
        [Description("0x01 Celcius")]
        Celcius = 0x00,
        [Description("0x01 Fahrenheit")]
        Fahrenheit = 0x01
    }
}
