// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
// SPDX-FileCopyrightText: Z-Wave Alliance https://z-wavealliance.org
#define CC_SUPPORT_LOGGING
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Utils;
using Utils.UI.Enums;
using ZWave;
using ZWave.BasicApplication;
using ZWave.BasicApplication.Operations;
using ZWave.CommandClasses;
using ZWave.Devices;
using ZWave.Enums;
using System.Collections;
using System.Collections.Concurrent;
using System.Text;
using System.Security.Cryptography;


namespace Zats.Services
{
    public class CommandClassSupportUtils
    {
        public static List<byte> GetDestinationEndPoints(NodeTag destinationNode)
        {
            if (destinationNode.IsBitAddress)
            {
                return GetDestinationEndPointsFromMCBitAddress(destinationNode.EndPointId);
            }

            return new List<byte> { destinationNode.EndPointId };
        }

        private static List<byte> GetDestinationEndPointsFromMCBitAddress(byte destinationEPBitAddress)
        {
            if (destinationEPBitAddress == 0x00)
            {
                return new List<byte> { 0x00 };
            }

            var dstEPs = new List<byte>();

            for (int bit = 0; bit < 7; bit++) // 7 bit only
            {
                if ((destinationEPBitAddress & (1 << bit)) != 0)
                {
                    dstEPs.Add((byte)(bit + 1));
                }
            }

            return dstEPs;
        }

        public static SvResultNames CombineSupervisionResults(SvResultNames previousResult, SvResultNames newResult)
        {
            if (previousResult == SvResultNames.Failed)
            {
                // Supervision FAIL supersedes
                return SvResultNames.Failed;
            }

            return newResult;
        }

        // Unsolicited Frames: common timer for all supported Command Classes
        // Usage: Workflow is created and initialized with 'TickerInterval' in AssociationSupport.
        //        LogLevel should be set in the Test Case initialization before configuring supported Command Classes.
        public static int TickerIntervalMs = 1000;
        public static int LogLevel = 0;

        private static Random randomGenerator = new Random();

        private static readonly ConcurrentBag<UnsolicitedReport> PendingReports = new ConcurrentBag<UnsolicitedReport> { };
        private class UnsolicitedReport
        {
            public byte[] Report;
            public List<AssociationGroupEx.NodeIdEx> NodeIdList;
            public ZWave.Enums.TransmitOptions TxOptions;
            public ZWave.Enums.SecuritySchemes Scheme;
        }
        public static void AddUnsolicitedFrame(byte[] rpt, byte[] nodeIdByteList, ZWave.Enums.TransmitOptions txOptions, byte scheme)
        {
            if (rpt != null && nodeIdByteList != null)
            {
                // Create pseudo node list
                List<AssociationGroupEx.NodeIdEx> nodeIdList = new List<AssociationGroupEx.NodeIdEx>();
                foreach (byte nodeId in nodeIdByteList)
                {
                    nodeIdList.Add(new AssociationGroupEx.NodeIdEx(nodeId, 0, false));
                }
                PendingReports.Add(new UnsolicitedReport() { Report = rpt, NodeIdList = nodeIdList, TxOptions = txOptions, Scheme = (ZWave.Enums.SecuritySchemes)scheme });
            }
        }
        public static void AddUnsolicitedFrame(byte[] rpt, byte groupId, ZWave.Enums.TransmitOptions txOptions, byte scheme)
        {
            if (rpt != null && groupId > 0)
            {
                AssociationGroupEx group = AssociationSupportData.SupportDataObject.AssociationGroups.Find(grp => grp.GroupId == groupId);
                PendingReports.Add(new UnsolicitedReport() { Report = rpt, NodeIdList = group.AssociatedNodeIds, TxOptions = txOptions, Scheme = (ZWave.Enums.SecuritySchemes)scheme });
            }
        }

        public static void OnTickerIntervalElapsed(TimeElapsedUnit ou)
        {
            // ReportAddLog(LogLevel, "{0}: TimeInterval elapsed, {1} reports pending", "CommandClassSupportUtils", PendingReports.Count);
            ou.SetNextActionItems();
            while (PendingReports.TryTake(out UnsolicitedReport rpt))
            {
                foreach (AssociationGroupEx.NodeIdEx node in rpt.NodeIdList)
                {
                    ReportAddLog(LogLevel, "{0}: Sending report ({1}: {2:X2} {3:X2}) to node {4} (0x{4:X2})",
                                "CommandClassSupportUtils", (CommandClassIdNames)rpt.Report[0], rpt.Report[0], rpt.Report[1], node.NodeId);
                    // TODO NETWORK
                    var sendData = new SendDataExOperation(new NetworkViewPoint(), new Node(node.NodeId), rpt.Report, rpt.TxOptions, rpt.Scheme);
                    ou.AddNextActionItems(sendData);
                }
            }
        }

        public static byte CalcByteFieldSize(double value, byte precision)
        {
            long l = Convert.ToInt64(value * Math.Pow(10, precision));
            if (l >= -128 && l <= 127)
                return 1;
            if (l >= -32768 && l <= 32767)
                return 2;
            return 4;
        }

        public static byte[] ValueToByteField(double value, byte size, byte precision)
        {
            byte[] valueBytes = new byte[size];
            long l = 0;
            if ((size == 1 || size == 2 || size == 4) && precision <= 7)
            {
                l = Convert.ToInt64(value * Math.Pow(10, precision));
                switch (size)
                {
                    case 1:
                        l = Math.Min(127, l);
                        l = Math.Max(-128, l);
                        valueBytes[0] = (byte)l;
                        break;
                    case 2:
                        l = Math.Min(32767, l);
                        l = Math.Max(-32768, l);
                        valueBytes = BitConverter.GetBytes((short)l);
                        break;
                    case 4:
                        l = Math.Min(0x7FFFFFFF, l);
                        l = Math.Max(-0x80000000, l);
                        valueBytes = BitConverter.GetBytes((int)l);
                        break;
                }
            }
            Array.Reverse(valueBytes);
            //ReportAddLog(1, "ValueToByteField: value={0}, size={1}, prec={2}, l={3}, bytes={4}", value, size, precision, l, BitConverter.ToString(valueBytes));
            return valueBytes;
        }

        public static double ByteListToValue(IList<byte> bytesList, byte size, byte precision)
        {
            double value = 0.0;
            if (bytesList != null)
            {
                var bytes = new List<byte>(bytesList).ToArray();
                if (bytes.Length == size && (size == 1 || size == 2 || size == 4) && precision <= 7)
                {
                    long l = (sbyte)bytes[0];
                    if (size >= 2)
                    {
                        l = (l << 8) + bytes[1];
                    }
                    if (size == 4)
                    {
                        l = (l << 16) + (bytes[2] << 8) + bytes[3];
                    }
                    value = Convert.ToDouble(l) / Math.Pow(10, precision);
                }
            }
            //ReportAddLog(1, "ByteFieldToValue: bytes={0}, size={1}, prec={2}, value={3}", BitConverter.ToString(bytes), size, precision, value);
            return value;
        }
        public static ulong ByteFieldToUInt64(byte[] bytes, byte size)
        {
            ulong value = 0;
            if (bytes != null)
            {
                if (bytes.Length == size && size >= 1 && size <= 4)
                {
                    for (int i = 0; i < size; i++)
                    {
                        value = (value << 8) + bytes[i];
                    }
                }
            }
            //ReportAddLog(1, "ByteFieldToUInt64: bytes={0}, size={1}, value={2}", BitConverter.ToString(bytes), size, value);
            return value;
        }
        public static long ByteFieldToInt64(byte[] bytes, byte size)
        {
            long value = 0;
            if (bytes != null)
            {
                if (bytes.Length == size && size >= 1 && size <= 4)
                {
                    if ((bytes[0] & 0x80) != 0x00)
                        value = -1;
                    for (int i = 0; i < size; i++)
                    {
                        value = (value << 8) + bytes[i];
                    }
                }
            }
            //ReportAddLog(1, "ByteFieldToInt64: bytes={0}, size={1}, value={2}", BitConverter.ToString(bytes), size, value);
            return value;
        }
        public static byte[] UInt64ToByteField(ulong value, byte size)
        {
            byte[] valueBytes = new byte[size];
            byte[] valueLong = BitConverter.GetBytes(value);

            for (int i = 0; i < size; i++)
            {
                valueBytes[i] = valueLong[size - 1 - i];
            }
            //ReportAddLog(1, "UInt64ToByteField: value={0}, size={1}, valueLong={2}, bytes={3}", value, size, BitConverter.ToString(valueLong), BitConverter.ToString(valueBytes));
            return valueBytes;
        }

        public static byte[] Int64ToByteField(long value, byte size)
        {
            byte[] valueBytes = new byte[size];
            byte[] valueLong = BitConverter.GetBytes(value);

            for (int i = 0; i < size; i++)
            {
                valueBytes[i] = valueLong[size - 1 - i];
            }
            //ReportAddLog(1, "Int64ToByteField: value={0}, size={1}, valueLong={2}, bytes={3}", value, size, BitConverter.ToString(valueLong), BitConverter.ToString(valueBytes));
            return valueBytes;
        }

        /// <summary>
        /// Validates a number from a byte list. The number is formatted as described in Unsigned/Signed field encoding (two’s complement representation) of SDS13781.
        /// </summary>
        /// <param name="bytesList">ZWave field with the number</param>
        /// <param name="size">Size of the ZWave field</param>
        /// <param name="precision">Precision of the number</param>
        /// <returns>true: valid; false: invalid.</returns>
        public static bool ValidateByteListValue(IList<byte> bytesList, byte size, byte precision)
        {
            if (bytesList != null)
            {
                double v = CommandClassSupportUtils.ByteListToValue(bytesList, size, precision);
                IList<byte> vl = CommandClassSupportUtils.ValueToByteField(v, size, precision);
                if (bytesList.SequenceEqual(vl))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Decodes a duration field to seconds. The duration is formatted as described 'Actuator Command Class Duration Set/Report encoding' (Command Class Specification).
        /// </summary>
        /// <param name="durationField">Duration field.</param>
        /// <returns>Duration in seconds.</returns>
        public static uint DurationToSeconds(byte durationField)
        {
            if (durationField >= 0x80 && durationField < 0xFF)
            {
                return (uint)(durationField - 0x7F) * 60;
            }
            return durationField;
        }
        /// <summary>
        /// Codes seconds into a duration field. The duration is formatted as described 'Actuator Command Class Duration Set/Report encoding' (Command Class Specification).
        /// </summary>
        /// <param name="seconds">Seconds.</param>
        /// <returns>Duration field.</returns>
        public static byte SecondsToDuration(uint seconds)
        {
            if (seconds >= 128)
            {
                return (byte)((seconds / 60) + 0x7F);
            }
            return (byte)seconds;
        }

        /// <summary>
        /// Decodes a duration field to a string. The duration is formatted as described 'Actuator Command Class Duration Set/Report encoding' (Command Class Specification).
        /// </summary>
        /// <param name="durationField">Duration field.</param>
        /// <returns>Proper string.</returns>
        public static string DurationToString(byte durationField)
        {
            if (durationField == 0)
            {
                return "instantly (0x00)";
            }
            else if (durationField >= 0x01 && durationField <= 0x7F)
            {
                return durationField.ToString() + " seconds";
            }
            else if (durationField >= 0x80 && durationField <= 0xFE)
            {
                return (durationField - 0x7F).ToString() + " minutes";
            }
            else
            {
                return "factory default (0xFF)";
            }

        }

        public static string ByteArrayToString(List<byte> bytes)
        {
            string byteList = "";

            if (bytes == null || bytes.Count == 0)
            {
                return "none";
            }
            for (int i = 0; i < bytes.Count; i++)
            {
                byteList += ((i > 0) ? ", 0x" : "0x") + bytes[i].ToString("X2");
            }

            return byteList;
        }

        /*public static List<byte> TestEnumToByteList()
        {
            List<IrRepeaterLearningStatusNames> names = new List<IrRepeaterLearningStatusNames>() { IrRepeaterLearningStatusNames.FailBufferOverflow };
            names.Add(IrRepeaterLearningStatusNames.FailTimeout);
            List<byte> b = CommandClassSupportUtils.CreateBitmaskFromList(names.Cast<byte>().ToList(), 0);
            return b;
        }*/

        /// <summary>
        /// Creates a Z-Wave bitmask from a single byte.
        /// </summary>
        /// <param name="bit0Value">Declares the list value, which represents Bit 0 of 1st bitmask byte. 0 = Bit 0 represents value 0x00; 1 = Bit 0 represents value 0x01</param>
        /// <returns>Bitmask as byte list, ready for usage in frames with dynamic mask length.</returns>
        public static List<byte> CreateBitmaskFromList(byte b, int bit0Value)
        {
            return CreateBitmaskFromList(new List<byte>() { b }, bit0Value);
        }

        /// <summary>
        /// Creates a Z-Wave bitmask from a list of 'supported' values. This list may be empty.
        /// </summary>
        /// <param name="list">List of a scalar type (byte, enum, int, ...)</param>
        /// <param name="bit0Value">Declares the list value, which represents Bit 0 of 1st bitmask byte. 0 = Bit 0 represents value 0x00; 1 = Bit 0 represents value 0x01</param>
        /// <param name="forceFirstByte">Force non-empty result list, if source list is empty.</param>
        /// <returns>Bitmask as byte list, ready for usage in frames with dynamic mask length.</returns>
        public static List<byte> CreateBitmaskFromList(IList list, int bit0Value, bool forceFirstByte = false)
        {
            List<byte> bitmask = new List<byte>();

            if (list != null)
            {
                foreach (var elementVar in list)
                {
                    int element = Convert.ToInt32(elementVar);
                    if (element >= bit0Value)
                    {
                        int byteIndex, bit;
                        byteIndex = (element - bit0Value) / 8;
                        bit = (element - bit0Value) % 8;
                        while (byteIndex >= bitmask.Count)
                        {
                            bitmask.Add(0x00);
                        }
                        bitmask[byteIndex] |= (byte)(1 << bit);
                    }
                }
                if (forceFirstByte && bitmask.Count == 0)
                {
                    bitmask.Add(0x00);
                }
            }
            return bitmask;
        }

        /// <summary>
        /// Compares two lists without considerung the order of their elements. The listed objects should implement IEquatable.
        /// https://stackoverflow.com/a/53791956
        /// </summary>
        /// <param name="listN">lists to compare.</param>
        /// <returns>
        /// Returns true if all list elements of list1 found in list2 AND vice versa. The order of the elements is not considered.
        /// </returns>
        // Test Code
        //List<byte> b1 = new List<byte> { 0x01, 0x02, 0x03 };
        //List<byte> b2 = new List<byte> { 0x03, 0x02, 0x01, 0x04 };
        //IEnumerable<byte> x1 = b1.Except(b2);
        //IEnumerable<byte> x2 = b2.Except(b1);
        //if (x1.Any() || x2.Any())
        //  b1.Add(0x04);
        public static bool ListElementsEqual<T>(IEnumerable<T> list1, IEnumerable<T> list2)
            //where T : IEquatable<T>   // where clause is disabled to make enums working
        {
            if (list1 == null || list2 == null)
            {
                return false;
            }
            if (list1.Count() != list2.Count())
            {
                return false;
            }
            if (list1.Except(list2).Any())
            {
                return false;
            }
            if (list2.Except(list1).Any())
            { 
                return false;
            }
            return true;
        }

#if ReportTimerStateOldstyle
        /// <summary>
        /// Creates a log entry for some timer states, if logging is enabled (oldstyle, deprecated).
        /// </summary>
        /// <param name="logLevel">0: Log is disabled; 1: Log is enabled.</param>
        /// <param name="timerName">Name of the timer.</param>
        /// <param name="timerState">Current state of the timer (int).</param>
        /// <param name="timerValue">Current timer value, should match the timerState and the log text.</param>
        public static void ReportTimerState(int logLevel, string timerName, int timerState, uint timerValue)
        {
            if (logLevel > 0)
            {
                switch (timerState)
                {
                    case 1: ReportAddLog(logLevel, "{0} started: {1} seconds.", timerName, timerValue); break;
                    case 2: ReportAddLog(logLevel, "{0} expired after {1} seconds.", timerName, timerValue); break;
                    case 3: ReportAddLog(logLevel, "{0} cancelled. Elapsed time: {1} seconds.", timerName, timerValue); break;
                    case 4: ReportAddLog(logLevel, "{0} reset.", timerName); break;
                    case 5: ReportAddLog(logLevel, "{0} cancelled. Remaining time: {1} seconds.", timerName, timerValue); break;
                    case 6: ReportAddLog(logLevel, "{0} running. Remaining time: {1} seconds.", timerName, timerValue); break;
                    case 7: ReportAddLog(logLevel, "{0} running. Remaining time: {1} milliseconds.", timerName, timerValue); break;
                }
            }
        }
#endif

        /// <summary>
        /// Creates a log entry for some timer states, if logging is enabled.
        /// </summary>
        /// <param name="logLevel">0: Log is disabled; 1: Log is enabled.</param>
        /// <param name="timerName">Name of the timer.</param>
        /// <param name="timerState">Current state of the timer (enum).</param>
        /// <param name="timerValue">Current timer value, should match the timerState and the log text.</param>
        public static void ReportTimerState(int logLevel, string timerName, ReportTimerStateNames timerState, long timerValue)
        {
            if (logLevel > 0)
            {
                switch (timerState)
                {
                    case ReportTimerStateNames.Started: ReportAddLog(logLevel, "{0} started: {1} seconds", timerName, timerValue); break;
                    case ReportTimerStateNames.ExpiredSec: ReportAddLog(logLevel, "{0} expired after {1} seconds.", timerName, timerValue); break;
                    case ReportTimerStateNames.CancelledElapsedSec: ReportAddLog(logLevel, "{0} cancelled. Elapsed time: {1} seconds.", timerName, timerValue); break;
                    case ReportTimerStateNames.Reset: ReportAddLog(logLevel, "{0} reset.", timerName); break;
                    case ReportTimerStateNames.CancelledRemainSec: ReportAddLog(logLevel, "{0} cancelled. Remaining time: {1} seconds.", timerName, timerValue); break;
                    case ReportTimerStateNames.RunRemainSec: ReportAddLog(logLevel, "{0} is running. Remaining time: {1} seconds.", timerName, timerValue); break;
                    case ReportTimerStateNames.RunRemainMs: ReportAddLog(logLevel, "{0} is running. Remaining time: {1} milliseconds.", timerName, timerValue); break;
                }
            }
        }

        /// <summary>
        /// Creates a common log entry, if logging is enabled.
        /// </summary>
        /// <param name="logLevel">0: Log is disabled; 1: Log is enabled.</param>
        /// <param name="formatString">Format string for the log entry.</param>
        /// <param name="values">Optional values/objects for the format string.</param>
        public static void ReportAddLog(int logLevel, string formatString, params object[] values)
        {
#if CC_SUPPORT_LOGGING
            if (logLevel > 0)
                ReportService.AddLog(String.Format(formatString, values), LogLevels.Information);
#endif // CC_SUPPORT_LOGGING
        }

        /// <summary>
        /// Returns the hardware level in percent for a given Z-Wave value
        /// </summary>
        /// <param name="value">Multilevel Switch Z-Wave value</param>
        /// <returns>Human readable string representing the hardware level</returns>
        public static string GetMultilevelSwitchHardwareLevel(byte value)
        {
            float val = value;

            if (value == 0xFF)
            {
                return "recent non-zero value";
            }
            else if (value > 0x63)
            {
                return "not defined";
            }
            else
            {
                return (val / 99.0f * 100.0f).ToString("F0") + "%";
            }
        }

        /// <summary>
        /// Get the name of a Notification Type by its given byte value
        /// </summary>
        /// <param name="notificationTypeValue">Notification Type value as byte</param>
        /// <returns>Human readable string of the given Notification Type</returns>
        public static string GetNotificationTypeName(NotificationTypeNames notificationType) {
            string notificationTypeName = "Unknown";

            if (typeof(NotificationTypeNames).IsEnumDefined(notificationType)) {
                notificationTypeName = (notificationType).ToString();
            }

            return notificationTypeName + " (0x" + ((byte)notificationType).ToString("X2") + ")"; ;
        }

        /// <summary>
        /// Get the name of a Notification Type by its given byte value
        /// </summary>
        /// <param name="notificationTypeValue">Notification Type value as byte</param>
        /// <returns>Human readable string of the given Notification Type</returns>
        public static string GetNotificationTypeName(byte notificationTypeValue) {
            string notificationTypeName = "Unknown";

            if (typeof(NotificationTypeNames).IsEnumDefined(notificationTypeValue)) {
                notificationTypeName = ((NotificationTypeNames)notificationTypeValue).ToString();
            }

            return notificationTypeName + " (0x" + notificationTypeValue.ToString("X2") + ")"; ;
        }

        /// <summary>
        /// Return the name of an event given by the byte value according to a given Notification Type
        /// </summary>
        /// <param name="notificationType">The notification type the event type byte is based on</param>
        /// <param name="eventType">The event type id as byte.</param>
        /// <returns>Human readable string for given Notification type / event type</returns>
        public static string GetNotificationEventName(NotificationTypeNames notificationType, byte eventType)
        {
            string eventName = string.Empty;

            switch (notificationType)
            {
                case NotificationTypeNames.SmokeAlarm:
                    eventName = ((NotificationSmokeAlarmEvents)eventType).ToString();
                    break;
                case NotificationTypeNames.COAlarm:
                    eventName = ((NotificationCOAlarmEvents)eventType).ToString();
                    break;
                case NotificationTypeNames.CO2Alarm:
                    eventName = ((NotificationCO2AlarmEvents)eventType).ToString();
                    break;
                case NotificationTypeNames.HeatAlarm:
                    eventName = ((NotificationHeatAlarmEvents)eventType).ToString();
                    break;
                case NotificationTypeNames.WaterAlarm:
                    eventName = ((NotificationWaterAlarmEvents)eventType).ToString();
                    break;
                case NotificationTypeNames.AccessControl:
                    eventName = ((NotificationAccessControlEvents)eventType).ToString();
                    break;
                case NotificationTypeNames.HomeSecurity:
                    eventName = ((NotificationHomeSecurityEvents)eventType).ToString();
                    break;
                case NotificationTypeNames.PowerManagement:
                    eventName = ((NotificationPowerManagementEvents)eventType).ToString();
                    break;
                case NotificationTypeNames.System:
                    eventName = ((NotificationSystemEvents)eventType).ToString();
                    break;
                case NotificationTypeNames.EmergencyAlarm:
                    eventName = ((NotificationEmergencyAlarmEvents)eventType).ToString();
                    break;
                case NotificationTypeNames.Clock:
                    eventName = ((NotificationClockEvents)eventType).ToString();
                    break;
                case NotificationTypeNames.Appliance:
                    eventName = ((NotificationApplianceEvents)eventType).ToString();
                    break;
                case NotificationTypeNames.HomeHealth:
                    eventName = ((NotificationHomeHealthEvents)eventType).ToString();
                    break;
                case NotificationTypeNames.Siren:
                    eventName = ((NotificationSirenEvents)eventType).ToString();
                    break;
                case NotificationTypeNames.WaterValve:
                    eventName = ((NotificationWaterValveEvents)eventType).ToString();
                    break;
                case NotificationTypeNames.WeatherAlarm:
                    eventName = ((NotificationWeatherAlarmEvents)eventType).ToString();
                    break;
                case NotificationTypeNames.Irrigation:
                    eventName = ((NotificationIrrigationEvents)eventType).ToString();
                    break;
                case NotificationTypeNames.GasAlarm:
                    eventName = ((NotificationGasAlarmEvents)eventType).ToString();
                    break;
                case NotificationTypeNames.PestControl:
                    eventName = ((NotificationPestControlEvents)eventType).ToString();
                    break;
                case NotificationTypeNames.LightSensor:
                    eventName = ((NotificationLightSensorEvents)eventType).ToString();
                    break;
                case NotificationTypeNames.WaterQualityMonitoring:
                    eventName = ((NotificationWaterQualityMonitoringEvents)eventType).ToString();
                    break;
                case NotificationTypeNames.HomeMonitoring:
                    eventName = ((NotificationHomeMonitoringEvents)eventType).ToString();
                    break;
            };

            return eventName + " (0x" + eventType.ToString("X2") + ")";
        }

        public static byte[] GetRandomBytes(int count)
        {
            var ret = new byte[count];

            for (int i = 0; i < count; i++)
            {
                ret[i] = GetRandomByte();
            }
            return ret;
        }

        public static byte GetRandomByte()
        {
            return (byte)randomGenerator.Next(0, 256);
        }

        /// <summary>
        /// Get a random PIN code in range e.g. 0000 ... 9999999999 in ASCII representation.
        /// </summary>
        /// <returns>ASCII bytes</returns>
        public static byte[] GetRandomPinCode(byte minLength = 4, byte maxLength = 10)
        {
            return GetUniqueUserCode(null, minLength, maxLength);
        }

        /// <summary>
        /// Get a unique User Code in ASCII representation considering existing User Codes and the supported keys.
        /// </summary>
        /// <returns>ASCII bytes</returns>
        public static byte[] GetUniqueUserCode(UserCodeSupportData suppData, byte minLength = 4, byte maxLength = 10, uint maxRetries = 1000)
        {
            byte[] supportedKeys = suppData?.SupportedKeysList ?? new byte[] { 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39 };

            var codeLenght = randomGenerator.Next(minLength, maxLength + 1);

            byte[] newCode = new byte[codeLenght];

            do
            {
                for (int i = 0; i < codeLenght; i++)
                {
                    newCode[i] = supportedKeys[randomGenerator.Next(0, supportedKeys.Length)];
                }

                if (suppData == null || suppData.Users == null || suppData.Users.Count == 0
                    || (suppData.Users.Any(user => user.UserCode.SequenceEqual(newCode)) == false && suppData.AdminCode.SequenceEqual(newCode) == false) )

                {
                    return newCode;
                }

            } while (maxRetries-- > 0);

            return new byte[minLength];

        }

        public static bool IsCredentialTypeBiometric(CredentialType credType)
        {
            switch (credType)
            {
                case CredentialType.EyeBiometric:
                case CredentialType.FaceBiometric:
                case CredentialType.FingerBiometric:
                case CredentialType.HandBiometric:
                case CredentialType.UnspecBiometric:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns the CRC16 calculated value
        /// </summary>
        /// <param name="data">Byte value</param>
        /// <returns>Returns the CRC16 calculated value</returns>
        public static ushort CalculateCRC16(byte[] data)
        {
            return Tools.CalculateCrc16(data?.ToArray() ?? new byte[0]);
        }

        /// <summary>
        /// Returns the hardware level for a given Z-Wave value
        /// </summary>
        /// <param name="parameter">Window Covering Parameter</param>
        /// <param name="value">Window Covering Z-Wave value</param>
        /// <returns>Human readable string representing the hardware level</returns>
        public static string GetWindowCoveringHardwareLevel(WindowCoveringParameterNames parameter, byte value)
        {
            // Examples:
            //
            // Parameter = 'InBottomPosition'
            // Z-Wave value =   0 (0x00) –> hardware level = 'closed'
            // Z-Wave value =   1 (0x01) –> hardware level = '1%'
            // Z-Wave value =  49 (0x31) –> hardware level = '49%'
            // Z-Wave value =  50 (0x32) –> hardware level = '51%'
            // Z-Wave value =  51 (0x33) –> hardware level = '52%'
            // Z-Wave value =  98 (0x62) –> hardware level = '99%'
            // Z-Wave value =  99 (0x63) –> hardware level = 'open'
            // Z-Wave value = 100 (0x64) –> hardware level = 'not available'
            //
            // Parameter = 'InBottomMovement'
            // For all Z-Wave values –> hardware level = 'not available'
            //
            // Parameter = 'HorizontalSlatsAnlgePosition'
            // Z-Wave value =   0 (0x00) –> hardware level = 'closed; up inside'
            // Z-Wave value =   1 (0x01) –> hardware level = '99% up inside'
            // Z-Wave value =  49 (0x31) –> hardware level = '1% up inside'
            // Z-Wave value =  50 (0x32) –> hardware level = 'open'
            // Z-Wave value =  51 (0x33) –> hardware level = '1% down inside'
            // Z-Wave value =  98 (0x62) –> hardware level = '99% down inside'
            // Z-Wave value =  99 (0x63) –> hardware level = 'closed; down inside'
            // Z-Wave value = 100 (0x64) –> hardware level = 'not available'
            //
            // Parameter = 'VerticalSlatsAnlgePosition'
            // Z-Wave value =   0 (0x00) –> hardware level = 'closed; to the right inside'
            // Z-Wave value =   1 (0x01) –> hardware level = '99% to the right inside'
            // Z-Wave value =  49 (0x31) –> hardware level = '1% to the right inside'
            // Z-Wave value =  50 (0x32) –> hardware level = 'open'
            // Z-Wave value =  51 (0x33) –> hardware level = '1% to the left inside'
            // Z-Wave value =  98 (0x62) –> hardware level = '99% to the left inside'
            // Z-Wave value =  99 (0x63) –> hardware level = 'closed; to the left inside'
            // Z-Wave value = 100 (0x64) –> hardware level = 'not available'

            string hardwareLevel = "not available";

            if (parameter == WindowCoveringParameterNames.OutLeftPosition || parameter == WindowCoveringParameterNames.OutRightPosition
                || parameter == WindowCoveringParameterNames.InLeftPosition || parameter == WindowCoveringParameterNames.InRightPosition
                || parameter == WindowCoveringParameterNames.InRightLeftPosition
                || parameter == WindowCoveringParameterNames.OutBottomPosition || parameter == WindowCoveringParameterNames.OutTopPosition
                || parameter == WindowCoveringParameterNames.InBottomPosition || parameter == WindowCoveringParameterNames.InTopPosition
                || parameter == WindowCoveringParameterNames.InTopBottomPosition)
            {
                if (value == 0x00)
                {
                    hardwareLevel = "closed";
                }
                else if (value > 0x00 && value < 0x63)
                {
                    float val = (float)value / 99.0f * 100.0f;

                    hardwareLevel = val.ToString("F0") + "%";
                }
                else if (value == 0x63)
                {
                    hardwareLevel = "open";
                }
                // else no valid value
            }
            else if (parameter == WindowCoveringParameterNames.VerticalSlatsAnlgePosition
                || parameter == WindowCoveringParameterNames.HorizontalSlatsAnlgePosition)
            {
                if (value == 0x00) // closed (to one side)
                {
                    if (parameter == WindowCoveringParameterNames.VerticalSlatsAnlgePosition)
                    {
                        hardwareLevel = "closed; to the right inside";
                    }
                    else
                    {
                        hardwareLevel = "closed; up inside";
                    }
                }
                else if (value > 0x00 && value < 0x32)
                {
                    float val = 100 - (101 + ((value - 50) / 49.0f * 100.0f));

                    hardwareLevel = val.ToString("F0") + "%";

                    if (parameter == WindowCoveringParameterNames.VerticalSlatsAnlgePosition)
                    {
                        hardwareLevel = hardwareLevel + " to the right inside";
                    }
                    else
                    {
                        hardwareLevel = hardwareLevel + " up inside";
                    }
                }
                else if (value == 0x32)
                {
                    hardwareLevel = "open";
                }
                else if (value > 0x32 && value < 0x63)
                {
                    float val = 101 + ((value - 99) / 48.0f * 100.0f);

                    hardwareLevel = val.ToString("F0") + "%";

                    if (parameter == WindowCoveringParameterNames.VerticalSlatsAnlgePosition)
                    {
                        hardwareLevel = hardwareLevel + " to the left inside";
                    }
                    else
                    {
                        hardwareLevel = hardwareLevel + " down inside";
                    }
                }
                else if (value == 0x63) // closed to the other side
                {
                    if (parameter == WindowCoveringParameterNames.VerticalSlatsAnlgePosition)
                    {
                        hardwareLevel = "closed; to the left inside";
                    }
                    else
                    {
                        hardwareLevel = "closed; down inside";
                    }
                }
                // else no valid value
            }
            // else no exact value available 

            return hardwareLevel;
        }
    }
    public enum ReportTimerStateNames : int
    {
        Started = 1,
        ExpiredSec = 2,
        CancelledElapsedSec = 3,
        Reset = 4,
        CancelledRemainSec = 5,
        RunRemainSec = 6,
        RunRemainMs = 7,
    };

    public enum SvStatusNames : byte
    {
        NoSupport = 0x00,
        Working = 0x01,
        Fail = 0x02,
        Success = 0xFF,
    };
    public enum SvResultNames : int
    {
        Undefined = -1,
        Failed = 0,
        Successful = 1,
    };

    public enum CommandClassIdNames : byte
    {
        // Notifications already implemented
        IrRepeaterCC = COMMAND_CLASS_IR_REPEATER.ID,                // 0xA0
        WakeUpCC = COMMAND_CLASS_WAKE_UP.ID,                        // 0x84
        // Notifications not yet implemented
        NotificationCC = COMMAND_CLASS_NOTIFICATION_V3.ID,          // 0x71
        BatteryCC = COMMAND_CLASS_BATTERY.ID,                       // 0x80
        CentralScene = COMMAND_CLASS_CENTRAL_SCENE.ID,              // 0x5B
        DeviceResetLocally = COMMAND_CLASS_DEVICE_RESET_LOCALLY.ID, // 0x5A
        EntryControl = COMMAND_CLASS_ENTRY_CONTROL.ID,              // 0x6F
    }
}
