// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Utils;
using Utils.UI.Enums;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;

// ToDo: Supervision Report Status 'Working' for SetpointSet (optional)

namespace Zats.Services
{
    public class ThermostatSetpointSupport : CommandClassSupport
    {
        private ThermostatSetpointSupportData ThermostatSetpointSupportData { get; set; }

        //private readonly static string CommandClassName = "Thermostat Setpoint CC";
        //private string EndPointName = "";
        //private readonly int LogLevel;  // shortcut for SupportData.LogLevel
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public ThermostatSetpointSupport()
            : this(new ThermostatSetpointSupportData() { })
        { }

        public ThermostatSetpointSupport(ThermostatSetpointSupportData thermostatSetpointSupportData)
            : base(COMMAND_CLASS_THERMOSTAT_SETPOINT.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 3;
            ThermostatSetpointSupportData = thermostatSetpointSupportData;
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

                ResultForSv = SvResultNames.Undefined;
                if (isResponseAllowed && command != null && command.Length > 1 && ThermostatSetpointSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_THERMOSTAT_SETPOINT.THERMOSTAT_SETPOINT_GET.ID)
                    {
                        rpt = PrepareReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_THERMOSTAT_SETPOINT.THERMOSTAT_SETPOINT_SUPPORTED_GET.ID)
                    {
                        rpt = PrepareSupportedReport();
                    }
                    else if (command[1] == COMMAND_CLASS_THERMOSTAT_SETPOINT_V3.THERMOSTAT_SETPOINT_CAPABILITIES_GET.ID)
                    {
                        rpt = PrepareCapabilitiesReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_THERMOSTAT_SETPOINT.THERMOSTAT_SETPOINT_SET.ID)
                    {
                        ResultForSv = HandleSetpointSet(command);
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

        private byte[] PrepareReport(byte[] command)
        {
            COMMAND_CLASS_THERMOSTAT_SETPOINT.THERMOSTAT_SETPOINT_GET getCmd = command;
            COMMAND_CLASS_THERMOSTAT_SETPOINT.THERMOSTAT_SETPOINT_REPORT rpt = new COMMAND_CLASS_THERMOSTAT_SETPOINT.THERMOSTAT_SETPOINT_REPORT() { };
            byte requestedSetpointType = getCmd.properties1.setpointType;
            ThermostatSetpoint requestedSetpoint = ThermostatSetpointSupportData.Setpoints.Find(setpoint => setpoint.SetpointType == requestedSetpointType);
            if (requestedSetpoint != null && ValidateSetpointType(requestedSetpointType))
            {
                byte prec = Math.Max(requestedSetpoint.MinValuePrecision, requestedSetpoint.MaxValuePrecision);
                byte size = requestedSetpoint.MaxValueSize;
                if (ThermostatSetpointSupportData.DynamicSizeCalculation)
                    size = CommandClassSupportUtils.CalcByteFieldSize(requestedSetpoint.CurrentValue, prec);
                rpt.properties1.setpointType = requestedSetpointType;
                rpt.properties2.scale = requestedSetpoint.MaxValueScale;
                rpt.properties2.precision = prec;
                rpt.properties2.size = size;
                rpt.value = CommandClassSupportUtils.ValueToByteField(requestedSetpoint.CurrentValue, size, prec);
            }
            else
            {
                rpt.properties1.setpointType = (byte) ThermostatSetpointTypes.N_A;
                rpt.properties2.scale = 0;
                rpt.properties2.precision = 0;
                rpt.properties2.size = 1;
                rpt.value = CommandClassSupportUtils.ValueToByteField(0.0, 1, 0);
            }
            return rpt;
        }
        private byte[] PrepareSupportedReport()
        {
            if (Version >= 1)
            {
                COMMAND_CLASS_THERMOSTAT_SETPOINT.THERMOSTAT_SETPOINT_SUPPORTED_REPORT rpt = new COMMAND_CLASS_THERMOSTAT_SETPOINT.THERMOSTAT_SETPOINT_SUPPORTED_REPORT() { };
                rpt.bitMask = ThermostatSetpointSupportData.SupportedSetpointTypesBitmask;
                return rpt;
            }
            return null;
        }
        private byte[] PrepareCapabilitiesReport(byte[] command)
        {
            if (Version >= 3)
            {
                COMMAND_CLASS_THERMOSTAT_SETPOINT_V3.THERMOSTAT_SETPOINT_CAPABILITIES_GET getCmd = command;
                COMMAND_CLASS_THERMOSTAT_SETPOINT_V3.THERMOSTAT_SETPOINT_CAPABILITIES_REPORT rpt = new COMMAND_CLASS_THERMOSTAT_SETPOINT_V3.THERMOSTAT_SETPOINT_CAPABILITIES_REPORT() { };
                byte requestedSetpointType = getCmd.properties1.setpointType;
                ThermostatSetpoint requestedSetpoint = ThermostatSetpointSupportData.Setpoints.Find(setpoint => setpoint.SetpointType == requestedSetpointType);
                if (requestedSetpoint != null && ValidateSetpointType(requestedSetpointType))
                {
                    byte precMin = requestedSetpoint.MinValuePrecision;
                    byte sizeMin = requestedSetpoint.MinValueSize;
                    if (ThermostatSetpointSupportData.DynamicSizeCalculation)
                        sizeMin = CommandClassSupportUtils.CalcByteFieldSize(requestedSetpoint.MinValue, precMin);
                    rpt.properties1.setpointType = requestedSetpointType;
                    rpt.properties2.scale1 = requestedSetpoint.MinValueScale;
                    rpt.properties2.precision1 = precMin;
                    rpt.properties2.size1 = sizeMin;
                    rpt.minValue = CommandClassSupportUtils.ValueToByteField(requestedSetpoint.MinValue, sizeMin, precMin);
                    byte precMax = requestedSetpoint.MaxValuePrecision;
                    byte sizeMax = requestedSetpoint.MaxValueSize;
                    if (ThermostatSetpointSupportData.DynamicSizeCalculation)
                        sizeMax = CommandClassSupportUtils.CalcByteFieldSize(requestedSetpoint.MaxValue, precMax);
                    rpt.properties3.scale2 = requestedSetpoint.MaxValueScale;
                    rpt.properties3.precision2 = precMax;
                    rpt.properties3.size2 = sizeMax;
                    rpt.maxvalue = CommandClassSupportUtils.ValueToByteField(requestedSetpoint.MaxValue, sizeMax, precMax);
                }
                else
                {
                    rpt.properties1.setpointType = (byte) ThermostatSetpointTypes.N_A;
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

        private SvResultNames HandleSetpointSet(byte[] command)
        {
            COMMAND_CLASS_THERMOSTAT_SETPOINT.THERMOSTAT_SETPOINT_SET setCmd = command;
            byte requestedSetpointType = setCmd.properties1.setpointType;
            ThermostatSetpoint requestedSetpoint = ThermostatSetpointSupportData.Setpoints.Find(setpoint => setpoint.SetpointType == requestedSetpointType);
            if (requestedSetpoint != null && ValidateSetpointType(requestedSetpointType))
            {
                byte size = setCmd.properties2.size;
                byte precision = setCmd.properties2.precision;
                byte scale = setCmd.properties2.scale;
                if (setCmd.value.Count == size && (size == 1 || size == 2 || size == 4) && precision <= 7 && scale <= 1)
                {
                    requestedSetpoint.CurrentValue = CommandClassSupportUtils.ByteListToValue(setCmd.value, size, precision);
                    return SvResultNames.Successful;
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
            if (ResultForSv == SvResultNames.Undefined || supervisionGet.encapsulatedCommand[0] != Id)
            {
                supervisionReport.status = (byte) SvStatusNames.NoSupport;
                return;
            }

            supervisionReport.status = (byte) SvStatusNames.Fail;  // FAIL as default
            switch (supervisionGet.encapsulatedCommand[1])
            {
                case COMMAND_CLASS_THERMOSTAT_SETPOINT.THERMOSTAT_SETPOINT_SET.ID:
                    if (ResultForSv == SvResultNames.Successful)
                        supervisionReport.status = (byte) SvStatusNames.Success;
                    break;
            }
            ResultForSv = SvResultNames.Undefined;
        }
    }

    public class ThermostatSetpoint
    {
        // V1+ Report
        public byte SetpointType;
        public double CurrentValue = 0.0;
        // V3+ Capabilities Report
        public byte MinValueSize { get; set; } = 1;         // 0 = unknown; 1, 2, 4 = size
        public byte MinValuePrecision { get; set; } = 0;    //
        public byte MinValueScale { get; set; } = 0;        // 0 = Celsius; 1 = Fahrenheit
        public double MinValue = 0.0;
        public byte MaxValueSize { get; set; } = 1;         // 0 = unknown; 1, 2, 4 = size
        public byte MaxValuePrecision { get; set; } = 0;    //
        public byte MaxValueScale { get; set; } = 0;        // 0 = Celsius; 1 = Fahrenheit
        public double MaxValue = 0.0;

        public ThermostatSetpoint(byte setpointType, byte minPrecision, byte maxPrecision, byte minScale, byte maxScale, byte minSize, byte maxSize, double minValue, double maxValue, double currentValue)
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
    };

    public class ThermostatSetpointSupportData
    {
        // Internals accessible from TestCases
        public List<ThermostatSetpoint> Setpoints { get; set; } = new List<ThermostatSetpoint>();

        // This byte array represents the Bit Mask fields of the Thermostat Setpoint Supported Report Command.
        // It's length is dynamically, depending from the supported (added) Setpoint Types.
        // Use the ThermostatSetpointSupportData.AddSetpoint methods to create settype objects and update the Supported Setpoint Types Bit Mask fields.
        // The Value parameters are considered as measured in units of scale 0. Value can be provided as double or as byte list.
        public List<byte> SupportedSetpointTypesBitmask { get; } = new List<byte> { 0x00 }; // Bit Mask 1, nothing supported
        public bool DynamicSizeCalculation { get; set; } = false;

        public ThermostatSetpoint AddSetpoint(ThermostatSetpointTypes setpointTypeName, byte minPrecision, byte maxPrecision, byte minScale, byte maxScale, byte minSize, byte maxSize, double minValue, double maxValue, double currentValue)
        {
            byte setpointType = (byte)setpointTypeName;
            if (setpointType == 0x01 || setpointType == 0x02 || (setpointType >= 0x07 && setpointType <= 0x0F))
            {
                if (Setpoints.Find(setpoint => setpoint.SetpointType == setpointType) == null)
                {
                    ThermostatSetpoint obj = new ThermostatSetpoint(setpointType, minPrecision, maxPrecision, minScale, maxScale, minSize, maxSize, minValue, maxValue, currentValue);
                    Setpoints.Add(obj);
                    UpdateSetpointTypesBitmask(setpointType);
                    return obj;
                }
            }
            return null;
        }

        private void UpdateSetpointTypesBitmask(byte setpointType)
        {
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
                while (byteIndex >= SupportedSetpointTypesBitmask.Count)
                {
                    SupportedSetpointTypesBitmask.Add(0x00);
                }
                SupportedSetpointTypesBitmask[byteIndex] |= (byte)(1 << bit);
            }
        }
    }

    public enum ThermostatSetpointTypes : byte
    {
        N_A = 0x00,
        Heating = 0x01,
        Cooling = 0x02,
        Furnace = 0x07,
        DryAir = 0x08,
        MoistAir = 0x09,
        AutoChangeover = 0x0A,
        EnergySaveHeating = 0x0B,
        EnergysaveCooling = 0x0C,
        AwayHeating = 0x0D,
        AwayCooling = 0x0E,
        FullPower = 0x0F
    };
}
