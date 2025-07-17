// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
// SPDX-FileCopyrightText: 2025 Z-Wave Alliance https://z-wavealliance.org
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using System.Collections.Generic;
using ZWave.Devices;



namespace Zats.Services
{
    /// <summary>
    /// Generic Command Class support to support unknown CC's or known CC's freely
    /// Supervision is handled with SUCCESS if a Command Callback has been registered using
    /// ApplicationService.AddCommandCallbacks() method
    /// </summary>

    public class SensorAlarmSupportModify : CommandClassSupport
    {
        private SvResultNames ResultForSv = SvResultNames.Undefined;

        public const int SensorsTypesCount = 0x07; // Known Notification Type area is 0x01 to 0x16

        private SensorAlarmSupportData SensorAlarmSupportData { get; set; }

        public SensorAlarmSupportModify(SensorAlarmSupportData sensorAlarmSupportData)
            : base(COMMAND_CLASS_SENSOR_ALARM.ID)
        {
            MinCCVersion = 1;
            MaxCCVersion = 3;
            SensorAlarmSupportData = sensorAlarmSupportData;
        }

        public override void SupervisionCallback(COMMAND_CLASS_SUPERVISION.SUPERVISION_GET supervisionGet,
                                                 COMMAND_CLASS_SUPERVISION.SUPERVISION_REPORT supervisionReport)
        {
            supervisionReport.status = (byte)SvStatusNames.NoSupport;
            return;
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

                if (isHandlingAllowed && command != null && command.Length > 1 && SensorAlarmSupportData != null)
                {
                    if (command[1] == COMMAND_CLASS_SENSOR_ALARM.SENSOR_ALARM_SUPPORTED_GET.ID)
                    {
                        ApiOperation sendData;
                        COMMAND_CLASS_SENSOR_ALARM.SENSOR_ALARM_SUPPORTED_REPORT rpt;

                        rpt = new COMMAND_CLASS_SENSOR_ALARM.SENSOR_ALARM_SUPPORTED_REPORT()
                        {
                            bitMask = SensorAlarmSupportData.BitMask,
                            numberOfBitMasks = (ByteValue)SensorAlarmSupportData.BitMask.Count
                        };

                        sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);
                    }
                    if(command[1] == COMMAND_CLASS_SENSOR_ALARM.SENSOR_ALARM_GET.ID)
                    {
                        ApiOperation sendData;
                        COMMAND_CLASS_SENSOR_ALARM.SENSOR_ALARM_REPORT rpt;
                        COMMAND_CLASS_SENSOR_ALARM.SENSOR_ALARM_GET getCmd = command;


                        rpt = new COMMAND_CLASS_SENSOR_ALARM.SENSOR_ALARM_REPORT()
                        {
                            sensorType = getCmd.sensorType,
                            sensorState = 0x00,
                            seconds = new byte[] { 0 }
                        };
                        sendData = new SendDataExOperation(_network, sendDstNode, sendSrcNode, rpt, TxOptions, TxSecOptions, scheme, TxOptions2);
                        ou.SetNextActionItems(sendData);

                    }

                    CommandCallback(command, sendDstNode.EndPointId);
                }
            }
        }

        // CC doesn't use internal data which inherits from CommandClassSupportData, just satisfy abstract members
        public override CommandClassSupportData CommandClassSupportData {
            get { return null; }
            set {; }
        }

        public override void UpdateSupportData(CommandClassSupportData data) {

        }

    }

    public class SensorAlarm
    {
        public byte SensorType;
        public byte SensorState;
        public double Seconds;

        public SensorAlarm(SensorAlarmTypes sensorType, byte sensorState, double seconds)
        {
            SensorType = (byte)sensorType;
            SensorState = sensorState;
            Seconds = seconds;
        }
    }

    public class SensorAlarmSupportData
    {
        public List<SensorAlarm> Sensors { get; set; } = new List<SensorAlarm>();
        public List<byte> BitMask { get; } = new List<byte> { 0x00 };
        public ByteValue NumberOfBitMasks { get; set; } 

        public SensorAlarm AddSensorAlarm(SensorAlarmTypes sensorAlarmType, byte sensorState, List<byte> seconds)
        {
            byte sensorType = (byte)sensorAlarmType;
            if(Sensors.Find(sensor => sensor.SensorType == sensorType) == null)
            {
                double sec = CommandClassSupportUtils.ByteListToValue(seconds, (byte)seconds.Count, 0x02);
                SensorAlarm obj = new SensorAlarm(sensorAlarmType, sensorState, sec);
                Sensors.Add(obj);
                UpdateSensorAlarmTypesBitmask(sensorType);
                return obj;
            }
            return null;
        }
        private void UpdateSensorAlarmTypesBitmask(byte sensorType)
        {
            byte byteIndex, bit;
            if (sensorType >= 0x00 && sensorType < SensorAlarmSupportModify.SensorsTypesCount)
            {
                byteIndex = (byte)(sensorType / 8);
                bit = (byte)(sensorType % 8);
                while (byteIndex >= BitMask.Count)
                {
                    BitMask.Add(0x00);
                }
                BitMask[byteIndex] |= (byte)(1 << bit);
            }
        }

    }

    public enum SensorAlarmTypes : byte
    {
        GeneralPurposeAlarm = 0x00,
        SmokeAlarm = 0x01,
        COAlarm = 0x02,
        CO2Alarm = 0x03,
        HeatAlarm = 0x04,
        WaterLeakAlarm = 0x05,
        ReturnFirstAlarmOnSupportedList = 0xFF
    }

    

}
