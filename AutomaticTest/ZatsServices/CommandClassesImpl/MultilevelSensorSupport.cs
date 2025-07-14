// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;

namespace Zats.Services
{
    public class MultilevelSensorSupport : CommandClassSupport
    {
        public MultilevelSensorSupportData MultilevelSensorSupportData { get; set; }

        //private readonly string CommandClassName = "Multilevel Sensor CC";
        //private string EndPointName = "";
        //private readonly int LogLevel;  // shortcut for SupportData.LogLevel

        public MultilevelSensorSupport()
            : this(new MultilevelSensorSupportData() { })
        { }

        public MultilevelSensorSupport(MultilevelSensorSupportData multilevelSensorSupportData)
            : base(COMMAND_CLASS_SENSOR_MULTILEVEL.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 11;
            MultilevelSensorSupportData = multilevelSensorSupportData;
            //LogLevel = MultilevelSensorSupportData.LogLevel;
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

                if (isResponseAllowed && command != null && command.Length > 1 && MultilevelSensorSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_SENSOR_MULTILEVEL.SENSOR_MULTILEVEL_GET.ID)
                    {
                        rpt = PrepareSensorReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SENSOR_MULTILEVEL_V5.SENSOR_MULTILEVEL_SUPPORTED_GET_SCALE.ID)
                    {
                        rpt = PrepareSupportedScaleReport(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SENSOR_MULTILEVEL_V5.SENSOR_MULTILEVEL_SUPPORTED_GET_SENSOR.ID)
                    {
                        rpt = PrepareSupportedSensorReport();
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

        private byte[] PrepareSupportedScaleReport(byte[] command)
        {
            if (Version >= 5)
            {
                COMMAND_CLASS_SENSOR_MULTILEVEL_V5.SENSOR_MULTILEVEL_SUPPORTED_GET_SCALE multilevelSensorScaleGetCmd = command;
                Sensor requestedSensor = MultilevelSensorSupportData.Sensors.Find(sensor => sensor.SensorType == multilevelSensorScaleGetCmd.sensorType);
                COMMAND_CLASS_SENSOR_MULTILEVEL_V5.SENSOR_MULTILEVEL_SUPPORTED_SCALE_REPORT rpt = new COMMAND_CLASS_SENSOR_MULTILEVEL_V5.SENSOR_MULTILEVEL_SUPPORTED_SCALE_REPORT() { };

                rpt.sensorType = multilevelSensorScaleGetCmd.sensorType;
                rpt.properties1.scaleBitMask = 0x00;
                if (requestedSensor != null)
                {
                    rpt.properties1.scaleBitMask = requestedSensor.GetScalesBitMask();
                }
                return rpt;
            }
            return null;
        }

        private byte[] PrepareSupportedSensorReport()
        {
            if (Version >= 5)
            {
                COMMAND_CLASS_SENSOR_MULTILEVEL_V5.SENSOR_MULTILEVEL_SUPPORTED_SENSOR_REPORT rpt = new COMMAND_CLASS_SENSOR_MULTILEVEL_V5.SENSOR_MULTILEVEL_SUPPORTED_SENSOR_REPORT()
                {
                    bitMask = MultilevelSensorSupportData.SupportedSensorTypesBitmask
                };
                return rpt;
            }
            return null;
        }

        private byte[] PrepareSensorReport(byte[] command)
        {
            COMMAND_CLASS_SENSOR_MULTILEVEL.SENSOR_MULTILEVEL_REPORT rpt = new COMMAND_CLASS_SENSOR_MULTILEVEL.SENSOR_MULTILEVEL_REPORT() { };

            if (Version < 5)
            {
                Sensor requestedSensor = MultilevelSensorSupportData.Sensors[0];
                rpt.sensorType = requestedSensor.SensorType;
                rpt.properties1.precision = requestedSensor.Precision;
                rpt.properties1.scale = requestedSensor.Scales[0];
                rpt.sensorValue = requestedSensor.GetRawValue(rpt.properties1.scale);
                rpt.properties1.size = Convert.ToByte(rpt.sensorValue.Count);
            }
            else
            {
                COMMAND_CLASS_SENSOR_MULTILEVEL_V5.SENSOR_MULTILEVEL_GET multilevelSensorGetCmd = command;
                Sensor requestedSensor = MultilevelSensorSupportData.Sensors.Find(sensor => sensor.SensorType == multilevelSensorGetCmd.sensorType);
                if (requestedSensor == null)
                {
                    requestedSensor = MultilevelSensorSupportData.Sensors[0];
                }

                rpt.sensorType = requestedSensor.SensorType;
                rpt.properties1.precision = requestedSensor.Precision;
                rpt.properties1.scale = requestedSensor.Scales[0];
                if (requestedSensor.IsScaleSupported(multilevelSensorGetCmd.properties1.scale))
                {
                    rpt.properties1.scale = multilevelSensorGetCmd.properties1.scale;
                }
                rpt.sensorValue = requestedSensor.GetRawValue(rpt.properties1.scale);
                rpt.properties1.size = Convert.ToByte(rpt.sensorValue.Count);

                /*if (requestedSensor.IsScaleSupported(multilevelSensorGetCmd.properties1.scale))
                {
                    COMMAND_CLASS_SENSOR_MULTILEVEL.SENSOR_MULTILEVEL_REPORT rpt = new COMMAND_CLASS_SENSOR_MULTILEVEL.SENSOR_MULTILEVEL_REPORT()
                    {
                        sensorType = requestedSensor.SensorType,
                        properties1 = new COMMAND_CLASS_SENSOR_MULTILEVEL.SENSOR_MULTILEVEL_REPORT.Tproperties1()
                        {
                            precision = requestedSensor.Precision,
                            scale = multilevelSensorGetCmd.properties1.scale,
                            size = Convert.ToByte(requestedSensor.Value.Count)
                        },
                        sensorValue = requestedSensor.GetValue(multilevelSensorGetCmd.properties1.scale)
                    };
                    return rpt;
                }
                else
                {
                    COMMAND_CLASS_SENSOR_MULTILEVEL.SENSOR_MULTILEVEL_REPORT rpt = new COMMAND_CLASS_SENSOR_MULTILEVEL.SENSOR_MULTILEVEL_REPORT()
                    {
                        sensorType = requestedSensor.SensorType,
                        properties1 = new COMMAND_CLASS_SENSOR_MULTILEVEL.SENSOR_MULTILEVEL_REPORT.Tproperties1()
                        {
                            precision = requestedSensor.Precision,
                            scale = requestedSensor.Scales[0],
                            size = Convert.ToByte(requestedSensor.Value.Count)
                        },
                        sensorValue = requestedSensor.Value
                    };
                    return rpt;
                }*/
            }
            return rpt;
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, 
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            supervisionReport.status = (byte) SvStatusNames.NoSupport;
            return;
        }
    }

    public delegate double GetValueCallback(Sensor sensor, byte scale);

    public class Sensor
    {
        public byte SensorType;
        public byte Precision;
        public byte[] Scales;
        public byte ScaleBitmask;
        private GetValueCallback getValue;
        public double Value;

        public Sensor(MultilevelSensorTypes sensorType, byte precision, byte[] scales, GetValueCallback getValueCallback, double value)
        {
            SensorType = (byte) sensorType;
            Precision = precision;
            Scales = scales;
            getValue = getValueCallback;
            Value = value;
        }

        public double GetValue(byte scale)
        {
            if (getValue != null && IsScaleSupported(scale))
            {
                return getValue.Invoke(this, scale);
            }

            return Value;
        }

        public List<byte> GetRawValue(byte scale)
        {
            if (getValue != null && IsScaleSupported(scale))
            {
                double value = getValue.Invoke(this, scale);
                byte size = CommandClassSupportUtils.CalcByteFieldSize(value, Precision); // handles overflow!
                return CommandClassSupportUtils.ValueToByteField(value, size, Precision).ToList();
            }
            return null;
        }
        public bool IsScaleSupported(byte scale)
        {
            return (Scales.Contains(scale));
        }
        public byte GetScalesBitMask()
        {
            byte bitmask = 0x00;

            if (Scales.Contains(0x00))
            {
                bitmask = Convert.ToByte(bitmask + 0x01);
            }
            if (Scales.Contains(0x01))
            {
                bitmask = Convert.ToByte(bitmask + 0x02);
            }
            if (Scales.Contains(0x02))
            {
                bitmask = Convert.ToByte(bitmask + 0x04);
            }
            if (Scales.Contains(0x03))
            {
                bitmask = Convert.ToByte(bitmask + 0x08);
            }
            return bitmask;
        }
        private bool IsTemperatureSensorType(byte sensorType)
        {
            if (sensorType == 0x01 || sensorType == 0x0B || sensorType == 0x17 || sensorType == 0x18 || sensorType == 0x22
             || sensorType == 0x3E || sensorType == 0x3F || sensorType == 0x40 || sensorType == 0x41 || sensorType == 0x4A
             || sensorType == 0x4B || sensorType == 0x4C || sensorType == 0x4D || sensorType == 0x50)
                return true;
            return false;
        }
    }

    public class MultilevelSensorSupportData
    {
        // Internals accessible from TestCases
        public int LogLevel { get; set; } = 0;
        public List<Sensor> Sensors { get; set; } = new List<Sensor>();

        // V5+ Supported Report
        // This byte array represents the Bit Mask fields of the Multilevel Sensor Supported Sensor Report Command.
        // It's length is dynamically, depending from the supported (added) Sensor Types.
        // Use the MultilevelSensorSupportData.AddSensor methods to create sensor objects and update the Supported Sensor Types Bit Mask fields.
        // The Value parameter is considered as measured in units of scale 0. Value can be provided as double or as byte list.
        public List<byte> SupportedSensorTypesBitmask { get; } = new List<byte> { 0x00 }; // Bit Mask 1, nothing supported

        public Sensor AddSensor(MultilevelSensorTypes sensorTypeName, byte precision, byte[] scales, GetValueCallback getValueCallback, double value)
        {
            byte sensorType = (byte)sensorTypeName;
            if (Sensors.Find(sensor => sensor.SensorType == sensorType) == null)
            {
                Sensor obj = new Sensor(sensorTypeName, precision, scales, getValueCallback, value);
                Sensors.Add(obj);
                UpdateSensorTypesBitmask(sensorType);
                return obj;
            }
            return null;
        }
        public Sensor AddSensor(MultilevelSensorTypes sensorTypeName, byte precision, byte[] scales, GetValueCallback getValueCallback, List<byte> value)
        {
            byte sensorType = (byte)sensorTypeName;
            if (Sensors.Find(sensor => sensor.SensorType == sensorType) == null)
            {
                double val = CommandClassSupportUtils.ByteListToValue(value, (byte)value.Count, precision);
                Sensor obj = new Sensor(sensorTypeName, precision, scales, getValueCallback, val);
                Sensors.Add(obj);
                UpdateSensorTypesBitmask(sensorType);
                return obj;
            }
            return null;
        }
        public Sensor AddSensor(MultilevelSensorTypes sensorTypeName, byte precision, byte[] scales, double value)
        {
            byte sensorType = (byte)sensorTypeName;
            if (Sensors.Find(sensor => sensor.SensorType == sensorType) == null)
            {
                Sensor obj = new Sensor(sensorTypeName, precision, scales, MultilevelSensorDefaultGetValueCallbacks.GetDefaultValue, value);
                Sensors.Add(obj);
                UpdateSensorTypesBitmask(sensorType);
                return obj;
            }
            return null;
        }
        public Sensor AddSensor(MultilevelSensorTypes sensorTypeName, byte precision, byte[] scales, List<byte> value)
        {
            byte sensorType = (byte)sensorTypeName;
            if (Sensors.Find(sensor => sensor.SensorType == sensorType) == null)
            {
                double val = CommandClassSupportUtils.ByteListToValue(value, (byte)value.Count, precision);
                Sensor obj = new Sensor(sensorTypeName, precision, scales, MultilevelSensorDefaultGetValueCallbacks.GetDefaultValue, val);
                Sensors.Add(obj);
                UpdateSensorTypesBitmask(sensorType);
                return obj;
            }
            return null;
        }

        private void UpdateSensorTypesBitmask(byte sensorType)
        {
            byte byteIndex, bit;
            if (sensorType > 0)
            {
                byteIndex = (byte) ((sensorType - 1) / 8);
                bit = (byte) ((sensorType - 1) % 8);
                while (byteIndex >= SupportedSensorTypesBitmask.Count)
                {
                    SupportedSensorTypesBitmask.Add(0x00);
                }
                SupportedSensorTypesBitmask[byteIndex] |= (byte) (1 << bit);
            }
        }
    }

    public enum MultilevelSensorTypes : byte
    {
        AirTemperature = 0x01,
        GeneralPurpose = 0x02, // deprecated by V11
        Illuminance = 0x03,
        Power = 0x04,
        Humidity = 0x05,
        Velocity = 0x06,
        Direction = 0x07,
        AtmosphericPressure = 0x08,
        BarometricPressure = 0x09,
        SolarRadiation = 0x0A,
        DewPoint = 0x0B,
        RainRate = 0x0C,
        TideLevel = 0x0D,
        Weight = 0x0E,
        Voltage = 0x0F,
        Current = 0x10,
        CarbonDioxide = 0x11,
        AirFlow = 0x12,
        TankCapacity = 0x13,
        Distance = 0x14,
        AnglePosition = 0x15, // deprecated by V8
        Rotation = 0x16,
        WaterTemparature = 0x17,
        SoilTemparature = 0x18,
        SeismicIntensity = 0x19,
        SeismicMagnitude = 0x1A,
        Ultraviolet = 0x1B,
        ElectricalResisivity  = 0x1C,
        ElectricalConductivity = 0x1D, 
        Loudness = 0x1E,
        Moisture = 0x1F, 
        Frequency = 0x20, 
        Time = 0x21, 
        TargetTemperature = 0x22, 
        ParticulateMatter = 0x23,
        Formaldehyde = 0x24,
        RadonConcentration = 0x25, 
        MethaneDensity = 0x26, 
        VolatileOrganicCompoundLevel = 0x27, 
        CarbonMonoxide = 0x28,
        SoilHumidity = 0x29,
        SoilReactivity = 0x2A,
        SoilSanity = 0x2B,
        HeartRate = 0x2C,
        BloodPressure = 0x2D, 
        MuscleMass = 0x2E,
        FatMass = 0x2F,
        BoneMass = 0x30, 
        TotalBodyWater = 0x31,
        BasicMetabolicRate = 0x32,
        BodyMassIndex = 0x33,
        AccelerationX_Axis = 0x34,
        AccelerationY_Axis = 0x35, 
        AccelerationZ_Axis = 0x36,
        SmokeDensity = 0x37, 
        WaterFlow = 0x38, 
        WaterPressure = 0x39, 
        ParticulateMatter10 = 0x3B, 
        RespiatoryRate = 0x3C,
        RelativeModulationLevel = 0x3D, 
        BoilerWaterTemperature = 0x3E, 
        DomesticHotWaterTemperature = 0x3F, 
        OutsideTemperature = 0x40, 
        ExhaustTemperature = 0x41, 
        WaterChlorineLevel = 0x42, 
        WaterAcidity = 0x43, 
        WaterOxidationReductionPotential = 0x44, 
        HeartRateLF_HF_Ratio = 0x45, 
        MotionDirection = 0x46, 
        AppliedForceOnSensor = 0x47, 
        ReturnAirTemperature = 0x48,
        SupplyAirTemperature = 0x49, 
        CondenserCoilTemperature = 0x4A, 
        EvaporatorCoilTemperature = 0x4B,
        LiquidLineTemperature = 0x4C,
        DischargeLineTemperature = 0x4D,
        Suction = 0x4E,
        Discharge = 0x4F,
        DefrostTemperature = 0x50,
        Ozone = 0x51,
        SulfurDioxide = 0x52,
        NitrogenDioxide = 0x53,
        Ammonia = 0x54,
        Lead = 0x55,
        ParticulateMatter1 = 0x56
    }

    public static class MultilevelSensorDefaultGetValueCallbacks
    {
        public static double TemperatureGetValue(Sensor sensor, byte scale)
        {
            if (sensor != null)
            {
                if (scale == 1)
                {
                    return sensor.Value * 9 / 5 + 32;
                }
                else
                {
                    return sensor.Value;
                }
            }
            return 0;
        }

        public static double GetDefaultValue(Sensor sensor, byte scale)
        {
            if (sensor != null)
            {
                return sensor.Value;
            }
            return 0;
        }
    }
}
