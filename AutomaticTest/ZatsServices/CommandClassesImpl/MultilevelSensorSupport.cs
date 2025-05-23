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
    [CommandClassName("Multilevel Sensor Command Class")]
    [CommandClassId(COMMAND_CLASS_SENSOR_MULTILEVEL.ID)]
    [MinVersion(1)]
    [MaxVersion(11)]
    public class MultilevelSensorSupport : CommandClassSupport
    {
        public MultilevelSensorSupportData MultilevelSensorSupportData { get; set; }
        public override CommandClassSupportData CommandClassSupportData
        {
            get { return MultilevelSensorSupportData; }
            set { MultilevelSensorSupportData = (MultilevelSensorSupportData)value; }
        }

        public MultilevelSensorSupport()
            : this(new MultilevelSensorSupportData(new List<MultilevelSensorType>()) { })
        { }

        public MultilevelSensorSupport(MultilevelSensorSupportData multilevelSensorSupportData)
            : base(COMMAND_CLASS_SENSOR_MULTILEVEL.ID)
        {
            MultilevelSensorSupportData = multilevelSensorSupportData;
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

                if (isHandlingAllowed && command != null && command.Length > 1 && MultilevelSensorSupportData != null)
                {
                    byte[] rpt = null;
                    if (command[1] == COMMAND_CLASS_SENSOR_MULTILEVEL.SENSOR_MULTILEVEL_GET.ID && !IsSupervised())
                    {
                        rpt = PrepareSensorReportCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SENSOR_MULTILEVEL_V5.SENSOR_MULTILEVEL_SUPPORTED_GET_SCALE.ID && !IsSupervised())
                    {
                        rpt = PrepareSupportedScaleReportCmd(command);
                    }
                    else if (command[1] == COMMAND_CLASS_SENSOR_MULTILEVEL_V5.SENSOR_MULTILEVEL_SUPPORTED_GET_SENSOR.ID && !IsSupervised())
                    {
                        rpt = PrepareSupportedSensorReport();
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

        private byte[] PrepareSupportedScaleReport(MultilevelSensorType sensorType)
        {
            if (Version >= 5)
            {
                COMMAND_CLASS_SENSOR_MULTILEVEL_V5.SENSOR_MULTILEVEL_SUPPORTED_SCALE_REPORT rpt = new COMMAND_CLASS_SENSOR_MULTILEVEL_V5.SENSOR_MULTILEVEL_SUPPORTED_SCALE_REPORT() { };
                if (sensorType != null)
                {
                    rpt.sensorType = (byte)sensorType.NestedId;
                }
                rpt.properties1.scaleBitMask = GetScaleBitmask(sensorType);
                return rpt;
            }
            return null;
        }
        private byte[] PrepareSupportedScaleReportCmd(byte[] command)
        {
            COMMAND_CLASS_SENSOR_MULTILEVEL_V5.SENSOR_MULTILEVEL_SUPPORTED_GET_SCALE multilevelSensorScaleGetCmd = command;
            COMMAND_CLASS_SENSOR_MULTILEVEL_V5.SENSOR_MULTILEVEL_SUPPORTED_SCALE_REPORT rpt;
            MultilevelSensorType requestedSensor = MultilevelSensorSupportData.Sensors.Find(sensor => sensor.NestedId == multilevelSensorScaleGetCmd.sensorType);
            rpt = PrepareSupportedScaleReport(requestedSensor);
            if (requestedSensor == null && rpt != null)
            {
                rpt.sensorType = multilevelSensorScaleGetCmd.sensorType;
                rpt.properties1.scaleBitMask = 0x00;
            }
            return rpt;
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

        private byte[] PrepareSensorReportCmd(byte[] command)
        {
            if (MultilevelSensorSupportData.Sensors.Count() > 0)
            {
                if (Version < 5)
                {
                    MultilevelSensorType defaultSensor = MultilevelSensorSupportData.Sensors.FirstOrDefault();
                    if (defaultSensor != null &&
                        defaultSensor.MinVersion <= Version) // check that the requested (and configured) sensor is supported by the configured Multilevel Sensor CC Version
                    {
                        MultilevelSensorScale defaultScale = defaultSensor.Scales.FirstOrDefault();
                        if (defaultScale != null &&
                            defaultScale.MinVersion <= Version) // check that the default scale is supported by the configured Multilevel Sensor CC Version
                        {
                            return PrepareSensorReport(defaultSensor, defaultScale);
                        }
                    }
                }
                else
                {
                    COMMAND_CLASS_SENSOR_MULTILEVEL_V5.SENSOR_MULTILEVEL_GET multilevelSensorGetCmd = command;
                    MultilevelSensorType requestedSensor = MultilevelSensorSupportData.Sensors.Find(sensor => sensor.NestedId == multilevelSensorGetCmd.sensorType);

                    // if requested sensor type hasn't been found: return the default (the first registered) sensor
                    if (requestedSensor == null)
                    {
                        requestedSensor = MultilevelSensorSupportData.Sensors.FirstOrDefault();
                    }
                    if (requestedSensor != null && requestedSensor.MinVersion <= Version) // check that the requested (and configured) sensor is supported by the configured Multilevel Sensor CC Version
                    {
                        MultilevelSensorScale requestedScale = requestedSensor.Scales.Find(s => s.NestedId == multilevelSensorGetCmd.properties1.scale);
                        if (requestedScale == null)
                        {
                            requestedScale = requestedSensor.Scales.FirstOrDefault();
                        }

                        if (requestedScale != null && requestedScale.MinVersion <= Version) // check that the requested scale is supported by the configured Multilevel Sensor CC Version
                        {
                            return PrepareSensorReport(requestedSensor, requestedScale);
                        }
                    }
                }
            }
            return null;
        }

        private byte[] PrepareSensorReport(MultilevelSensorType sensorType, MultilevelSensorScale scale)
        {
            COMMAND_CLASS_SENSOR_MULTILEVEL.SENSOR_MULTILEVEL_REPORT rpt = new COMMAND_CLASS_SENSOR_MULTILEVEL.SENSOR_MULTILEVEL_REPORT();
           
            rpt.sensorType = (byte)sensorType.NestedId;
            rpt.properties1.precision = scale.Precision;
            rpt.properties1.scale = (byte)scale.NestedId;
            rpt.sensorValue = GetRawValue(sensorType, rpt.properties1.scale);
            rpt.properties1.size = Convert.ToByte(rpt.sensorValue.Count);

            return rpt;
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet, 
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            supervisionReport.status = (byte) SvStatusNames.NoSupport;
            return;
        }

        private byte GetScaleBitmask(MultilevelSensorType sensorType)
        {
            byte bitmask = 0x00;

            sensorType?.Scales?.ForEach(scale =>
            {
                if (scale.MinVersion <= Version)
                {
                    bitmask |= (byte)(0x01 << scale.NestedId);
                }
            });

            //if (sensorType != null)
            //{
            //    if (sensorType.Scales.Find(x => x.Id == 0x00) != null)
            //    {
            //        bitmask = Convert.ToByte(bitmask + 0x01);
            //    }
            //    if (sensorType.Scales.Find(x => x.Id == 0x01) != null)
            //    {
            //        bitmask = Convert.ToByte(bitmask + 0x02);
            //    }
            //    if (sensorType.Scales.Find(x => x.Id == 0x02) != null)
            //    {
            //        bitmask = Convert.ToByte(bitmask + 0x04);
            //    }
            //    if (sensorType.Scales.Find(x => x.Id == 0x03) != null)
            //    {
            //        bitmask = Convert.ToByte(bitmask + 0x08);
            //    }
            //}
            return bitmask;
        }

        public override void UpdateSupportData(CommandClassSupportData data)
        {
            if (data is MultilevelSensorSupportData updateData)
            {
                foreach (MultilevelSensorType sensor in MultilevelSensorSupportData.Sensors)
                {
                    if (updateData.Sensors.Find(x => x.NestedId == sensor.NestedId) is MultilevelSensorType updateSensor)
                    {
                        foreach (MultilevelSensorScale scale in sensor.Scales)
                        {
                            if (updateSensor.Scales.Find(s => s.NestedId == scale.NestedId) is MultilevelSensorScale updateScale)
                            {
                                if (updateScale.Precision != scale.Precision ||
                                    updateScale.Value != scale.Value)
                                {
                                    scale.Value = updateScale.Value;
                                    scale.Precision = updateScale.Precision;

                                    if (ReportTypes.HasFlag(ReportTypes.OnStateChange))
                                    {
                                        SendUnsolicitedReport(PrepareSensorReport(sensor, scale), NodeTag.Empty);
                                    }
                                }
                            }

                            List<MultilevelSensorScale> scalesRemoved = sensor.Scales.FindAll(x => updateSensor.Scales.Find(y => y.NestedId == x.NestedId) == null);
                            List<MultilevelSensorScale> scalesAdded = updateSensor.Scales.FindAll(x => sensor.Scales.Find(y => y.NestedId == x.NestedId) == null);

                            if (scalesAdded.Count > 0 || scalesRemoved.Count > 0)
                            {
                                scalesRemoved.ForEach(x => sensor.Scales.Remove(x));
                                sensor.Scales.AddRange(scalesAdded);

                                if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                                {
                                    SendUnsolicitedReport(PrepareSupportedScaleReport(sensor), NodeTag.Empty);
                                }
                            }
                        }
                    }
                }

                List<MultilevelSensorType> sensorsRemoved = MultilevelSensorSupportData.Sensors.FindAll(x => updateData.Sensors.Find(y => y.NestedId == x.NestedId) == null);
                List<MultilevelSensorType> sensorsAdded = updateData.Sensors.FindAll(x => MultilevelSensorSupportData.Sensors.Find(y => y.NestedId == x.NestedId) == null);

                if (sensorsAdded.Count > 0 || sensorsRemoved.Count > 0)
                {
                    sensorsRemoved.ForEach(x => MultilevelSensorSupportData.Sensors.Remove(x));
                    MultilevelSensorSupportData.Sensors.AddRange(sensorsAdded);

                    if (ReportTypes.HasFlag(ReportTypes.OnCapabilityChange))
                    {
                        SendUnsolicitedReport(PrepareSupportedSensorReport(), NodeTag.Empty);
                        // discussed with TKO:
                        //  - also report the supported scales of a new sensor type
                        //  - but do not report the value of each scale
                        //  - seems the spec doesn't contain any hint how to implement an End Device which that kind of capability change
                        sensorsAdded.ForEach(s => SendUnsolicitedReport(PrepareSupportedScaleReport(s), NodeTag.Empty));
                    
                    }
                }

                // inform (EDE) for any updates
                SupportDelegates?.DeviceDataUpdate?.Invoke(this);

            }
        }

        public static List<byte> GetRawValue(MultilevelSensorType sensorType, byte scaleId)
        {
            if (sensorType != null && sensorType.Scales.Find(s => s.NestedId == scaleId) is MultilevelSensorScale scale)
            {
                byte size = CommandClassSupportUtils.CalcByteFieldSize(scale.Value, scale.Precision); // handles overflow!
                return CommandClassSupportUtils.ValueToByteField(scale.Value, size, scale.Precision).ToList();
            }
            return null;
        }
    }

    public class MultilevelSensorSupportData : CommandClassSupportData
    {
        // Internals accessible from TestCases
        [NestedControl(2, "Firmware Versions")]
        public List<MultilevelSensorType> Sensors { get; set; } = null;
        //public List<Sensor> Sensors { get; set; } = new List<Sensor>();

        // V5+ Supported Report
        // This byte array represents the Bit Mask fields of the Multilevel Sensor Supported Sensor Report Command.
        // It's length is dynamically, depending from the supported (added) Sensor Types.
        // Use the MultilevelSensorSupportData.AddSensor methods to create sensor objects and update the Supported Sensor Types Bit Mask fields.
        // The Value parameter is considered as measured in units of scale 0. Value can be provided as double or as byte list.
        public List<byte> SupportedSensorTypesBitmask { get { return getSensorTypesBitmask(); } } 
        

        public MultilevelSensorSupportData()
        {

        }

        public MultilevelSensorSupportData(List<MultilevelSensorType> supportedSensorTypes)
        {
            Sensors = supportedSensorTypes;
        }

        private List<byte> getSensorTypesBitmask()
        {
            int byteIndex, bit;

            List<byte> supportedSensorTypesBitmask = new List<byte> { 0x00 };
            
            Sensors?.ForEach(sensor =>
            {
                byteIndex = ((sensor.NestedId - 1) / 8);
                bit = ((sensor.NestedId - 1) % 8);
                // extend bitmask to the lenght needed for this sensor type
                while (byteIndex >= supportedSensorTypesBitmask.Count)
                {
                    supportedSensorTypesBitmask.Add(0x00);
                }
                supportedSensorTypesBitmask[byteIndex] |= (byte) (0x01 << bit);
            });

            return supportedSensorTypesBitmask;
        }
    

    }
}
