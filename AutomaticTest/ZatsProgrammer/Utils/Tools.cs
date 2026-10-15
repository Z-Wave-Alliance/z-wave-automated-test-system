// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.IO;
using System.Diagnostics;
using System.Linq;

namespace ZWave.Framework
{
    public class Tools
    {
        public static string ToBinString(byte[] val)
        {
            StringBuilder strData = new StringBuilder();
            foreach (byte b in val)
            {
                strData.AppendLine(ToBinString(b));
            }
            return strData.ToString();
        }

        public static string ToBinString(byte val)
        {
            StringBuilder strData = new StringBuilder();
            for (byte mask = 0x80; mask != 0; mask >>= 1)
            {
                strData.Append((val & mask) != 0 ? '1' : '0');
            }
            return strData.ToString();
        }

        /// <summary>
        /// Takes a byte array and converts it to a string of byte HEX values
        /// </summary>
        /// <param name="val">Array to convert</param>
        /// <returns></returns>
        public static string ToHexString(byte[] val)
        {

            StringBuilder strData = new StringBuilder();
            if (val != null)
            {
                for (int i = 0; i < val.Length; i++)
                {
                    strData.Append(GetHex(val[i]));
                }
            }
            return strData.ToString();
        }
        public static string ToHexString(byte[] val, string separator)
        {
            StringBuilder strData = new StringBuilder();
            for (int i = 0; i < val.Length; i++)
            {
                strData.Append(GetHex(val[i]) + separator);
            }
            return strData.ToString();
        }
        public static string ToHexString(List<byte> val, string separator)
        {
            StringBuilder strData = new StringBuilder();
            for (int i = 0; i < val.Count; i++)
            {
                strData.Append(GetHex(val[i]) + separator);
            }
            return strData.ToString();
        }

        public static string ToHexString(uint val)
        {
            string strData = "";
            strData += GetHex((byte)(val >> 24));
            strData += GetHex((byte)(val >> 16));
            strData += GetHex((byte)(val >> 8));
            strData += GetHex((byte)val);
            return strData;
        }

        public static string ToHexString(byte val)
        {
            return GetHex(val);
        }

        public static string ToHexString(ushort val)
        {
            string strData = "";
            strData += GetHex((byte)(val >> 8));
            strData += GetHex((byte)val);
            return strData;
        }
        public static byte[] FromHexString(string hex)
        {
            List<byte> list = new List<byte>();
            int i = 0;
            if (hex.Length % 2 != 0)
            {
                i = -1;
            }
            for (; i < hex.Length; i += 2)
            {
                try
                {
                    if (i == -1)
                    {
                        list.Add((byte)Int32.Parse(hex.Substring(0, 1), System.Globalization.NumberStyles.AllowHexSpecifier));
                    }
                    else
                    {
                        list.Add((byte)Int32.Parse(hex.Substring(i, 2), System.Globalization.NumberStyles.AllowHexSpecifier));
                    }
                }
                catch { }
            }
            return list.ToArray();
        }
        public static byte[] BlankArray(int length, byte blankValue)
        {
            byte[] retVal = new byte[length];
            for (int i = 0; i < length; i++)
                retVal[i] = blankValue;
            return retVal;
        }

        /// <summary>
        /// Gets the byte from string.
        /// </summary>
        /// <param name="str">The string value.</param>
        /// <returns></returns>
        public static byte GetByte(string str)
        {
            if (str.StartsWith("0x") || str.StartsWith("0X"))
                return Convert.ToByte(str, 16);
            else
                return Convert.ToByte(str);
        }

        public static byte[] GetBytes(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return new byte[0];

            byte[] buffer = new byte[(hex.Length / 2) + 1];
            int index = 0;
            bool isZeroFound = false;
            byte tmp = 0;
            bool isSecondPart = false;
            foreach (char item in hex)
            {
                if (isZeroFound)
                {
                    if (item == 'x' || item == 'X')
                    {
                        if (isSecondPart)
                        {
                            buffer[index++] = tmp;
                            isSecondPart = false;
                        }
                        isZeroFound = false;
                        continue;
                    }
                    else
                    {

                        //add 0
                        if (isSecondPart)
                        {
                            tmp = (byte)(tmp << 4);
                            buffer[index++] = tmp;
                            isSecondPart = false;
                        }
                        else
                        {
                            tmp = 0;
                            isSecondPart = true;
                        }
                    }
                    isZeroFound = false;
                }
                if (item == '0')
                {
                    isZeroFound = true;
                    continue;
                }
                else
                {
                    if (char.IsDigit(item) || item >= 'A' && item <= 'F' || item >= 'a' && item <= 'f')
                    {
                        if (isSecondPart)
                        {
                            tmp = (byte)(tmp << 4);
                            tmp += GetHexDigit(item);
                            buffer[index++] = tmp;
                            isSecondPart = false;
                        }
                        else
                        {
                            tmp = GetHexDigit(item);
                            isSecondPart = true;
                        }
                    }
                    else if (isSecondPart)
                    {
                        buffer[index++] = tmp;
                        isSecondPart = false;
                    }
                }
            }
            if (isZeroFound)
            {
                //add 0
                if (isSecondPart)
                {
                    tmp = (byte)(tmp << 4);
                    buffer[index++] = tmp;
                }
                else
                {
                    buffer[index++] = 0;
                }
            }
            else if (isSecondPart)
            {
                buffer[index++] = tmp;
            }
            byte[] ret = new byte[index];
            Array.Copy(buffer, 0, ret, 0, index);
            return ret;
        }

        /// <summary>
        /// Returns Int32 from byte array using big endian convention
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static int GetInt32(byte[] data)
        {
            int ret = 0;
            if (data != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (data.Length > i)
                    {
                        ret += data[data.Length - 1 - i] << (i * 8);
                    }
                }
            }
            return ret;
        }

        private static byte GetHexDigit(char item)
        {
            if (item > '9')
            {
                if (item > 'F')
                {
                    return (byte)(item - 'a' + 10);
                }
                else
                {
                    return (byte)(item - 'A' + 10);
                }
            }
            else
            {
                return (byte)(item - '0');
            }
        }

        /// <summary>
        /// Gets the datetime.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <returns></returns>
        public static DateTime getDateTime(string value)
        {
            return DateTime.Parse(value);
        }

        /// <summary>
        /// Gets the string from DateTime.
        /// </summary>
        /// <param name="value">The DateTime value.</param>
        /// <returns></returns>
        public static string getString(DateTime value)
        {
            return value.ToString();
        }

        public static StreamWriter logFileSW = null;
        public static StreamWriter errorFileSW = null;
        public static void CreateDebugExceptionFile(string prefix, string appDataPath, string userDataPath)
        {
            if (errorFileSW != null)
            {
                errorFileSW.Flush();
                errorFileSW.Close();
            }
            try
            {
                string str = prefix.ToUpper() + "_current_err_output.log";
                string prev_str = prefix.ToUpper() + "_prev_err_output.log";
                string path = "";
                if (!string.IsNullOrEmpty(userDataPath))
                {
                    path = userDataPath + "\\";
                }
                else if (!string.IsNullOrEmpty(appDataPath))
                {
                    path = appDataPath + "\\";
                }
                if (File.Exists(path + str))
                {

                    if (File.Exists(path + prev_str))
                    {
                        File.Delete(path + prev_str);
                    }
                    File.Move(path + str, path + prev_str);
                }
                errorFileSW = File.CreateText(path + str);
            }
            catch (Exception)
            {

            }
        }
        public static void CreateDebugLogFile(string prefix, string appDataPath, string userDataPath)
        {
            if (logFileSW != null)
            {
                logFileSW.Flush();
                logFileSW.Close();
            }
            try
            {
                string str = prefix.ToUpper() + "_current_output.log";
                string prev_str = prefix.ToUpper() + "_prev_output.log";
                string path = "";
                if (!string.IsNullOrEmpty(userDataPath))
                {
                    path = userDataPath + "\\";
                }
                else if (!string.IsNullOrEmpty(appDataPath))
                {
                    path = appDataPath + "\\";
                }
                if (File.Exists(path + str))
                {
                    if (File.Exists(path + prev_str))
                    {
                        File.Delete(path + prev_str);
                    }
                    File.Move(path + str, path + prev_str);
                }
                logFileSW = File.CreateText(path + str);
            }
            catch (Exception)
            {

            }
        }


        public static void CloseDebugLogFile()
        {
            if (logFileSW != null)
            {
                logFileSW.Flush();
                logFileSW.Close();
            }
        }
        public static void CloseDebugExceptionFile()
        {
            if (errorFileSW != null)
            {
                errorFileSW.Flush();
                errorFileSW.Close();
            }
        }

        public static void _writeDebugDiagnosticMessage(string msg)
        {
            if (logFileSW != null)
            {
                logFileSW.WriteLine(msg);
                logFileSW.Flush();
            }
            System.Diagnostics.Debug.WriteLine(msg);
        }

        public static void _writeDebugDiagnosticMessage(string msg, bool showTime, bool showCallingPoint)
        {
            _writeDebugDiagnosticMessage(msg, showTime, showCallingPoint, 1);
        }

        public static void _writeDebugDiagnosticMessage(string msg, bool showTime, bool showCallingPoint, int showCallingPointShifter)
        {
            string methodName = string.Empty;
            string timestr = string.Empty;
            if (showCallingPoint)
            {
                methodName = GetMethodName(showCallingPointShifter);
            }
            if (showTime)
            {
                timestr = CurrentDateTime.ToString("hh:mm:ss.ffff");
            }
            _writeDebugDiagnosticMessage(
                string.Format("{0} [{1}]: {2}",
                timestr,
                string.IsNullOrEmpty(methodName) ? string.Empty : string.Format("{0}", methodName),
                msg));
        }

        public static string GetMethodName(int showCallingPointShifter)
        {
            string methodName = "N/A";
            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
            if (st.FrameCount > (1 + showCallingPointShifter))
            {
                System.Diagnostics.StackFrame sf = st.GetFrame(1 + showCallingPointShifter);
                methodName = sf.GetMethod().Name;
            }
            return methodName;
        }

        public static void _writeDebugDiagnosticExceptionMessage(string msg)
        {
            if (errorFileSW != null)
            {
                errorFileSW.WriteLine(msg);
                errorFileSW.Flush();
            }
            System.Diagnostics.Debug.WriteLine(msg);
        }

        public static void _writeDebugDiagnosticExceptionMessage(Exception ex, int showCallingPointShifter)
        {
            string methodName = string.Empty;
            string timestr = string.Empty;
            if (true)
            {
                methodName = GetMethodName(showCallingPointShifter);
            }
            if (true)
            {
                timestr = CurrentDateTime.ToString("hh:mm:ss.ffff");
            }
            _writeDebugDiagnosticExceptionMessage(
                string.Format("{0} [{1}]: {2}",
                timestr,
                string.IsNullOrEmpty(methodName) ? string.Empty : string.Format("{0}", methodName),
                ex.Message + Environment.NewLine + ex.StackTrace));
        }
        private static Stopwatch timer = new Stopwatch();
        private static DateTime timerStartedDateTime = DateTime.Now;
        public static void StartHighPrecisionTimer()
        {
            timer.Reset();
            timerStartedDateTime = DateTime.Now;
            timer.Start();
        }
        public static void StopHighPrecisionTimer()
        {
            timer.Stop();
        }


        public static DateTime CurrentDateTime
        {
            get
            {
                return GetCurrentTime();
            }
        }

        //private static delegate DateTime GetCurrentDateTimeCaller();
        private static DateTime GetCurrentTime()
        {
            if (timer.IsRunning)
            {
                if (timer.ElapsedMilliseconds > 2000000000)
                {
                    StopHighPrecisionTimer();
                    StartHighPrecisionTimer();
                }
                return timerStartedDateTime.AddMilliseconds(timer.ElapsedMilliseconds);
            }
            else
            {
                return DateTime.Now;
            }
        }

        public static string MakeLegalMixCaseIdentifier(string inStr)
        {
            string result = "";
            string[] split = inStr.Split(" +ґ!\"#¤%&/()=?`@Ј$Ђ{[]}|,.;:-*'^Ё~<>".ToCharArray());
            for (int j = 0; j < split.Length; j++)
            {
                result += split[j];
                if (j < split.Length - 1 && split[j + 1].Length > 0)
                {
                    result += "_";
                }
            }
            if (result.Length == 0)
            {
                result = "_";
            }
            if (result.Length == 0)
            {
                result = "NONAME";
            }
            if (result.StartsWith("0") || result.StartsWith("1") || result.StartsWith("2") || result.StartsWith("3") || result.StartsWith("4") ||
                result.StartsWith("5") || result.StartsWith("6") || result.StartsWith("7") || result.StartsWith("8") || result.StartsWith("9"))
            {
                result = "m" + result;
            }
            return result;
        }


        public static byte GetBitsFromMask(byte mask)
        {
            byte ret = 0;
            if ((mask & 0x01) != 0)
                ret++;
            if ((mask & 0x02) != 0)
                ret++;
            if ((mask & 0x04) != 0)
                ret++;
            if ((mask & 0x08) != 0)
                ret++;
            if ((mask & 0x10) != 0)
                ret++;
            if ((mask & 0x20) != 0)
                ret++;
            if ((mask & 0x40) != 0)
                ret++;
            if ((mask & 0x80) != 0)
                ret++;
            return ret;
        }

        public static string GetHex(byte key, bool add_0x)
        {
            char[] chs;
            byte b;
            if (add_0x)
            {
                chs = new char[] { '0', 'x', '0', '0' };
                b = ((byte)(key >> 4));
                chs[2] = (char)(b > 9 ? b + 0x37 : b + 0x30);
                b = ((byte)(key & 0x0F));
                chs[3] = (char)(b > 9 ? b + 0x37 : b + 0x30);
            }
            else
            {
                chs = new char[2];
                b = ((byte)(key >> 4));
                chs[0] = (char)(b > 9 ? b + 0x37 : b + 0x30);
                b = ((byte)(key & 0x0F));
                chs[1] = (char)(b > 9 ? b + 0x37 : b + 0x30);
            }
            return new string(chs);
        }

        public static string GetHex(byte key)
        {
            char[] chs = new char[2];
            byte b;
            b = ((byte)(key >> 4));
            chs[0] = (char)(b > 9 ? b + 0x37 : b + 0x30);
            b = ((byte)(key & 0x0F));
            chs[1] = (char)(b > 9 ? b + 0x37 : b + 0x30);
            return new string(chs);
        }

        /// <summary>
        /// Example: 12 34 F4 55
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string GetHex(byte[] data, int startIndex, int length)
        {
            if (data == null || length == 0 || startIndex >= data.Length)
                return "";

            int actualLength = length;
            if (startIndex + actualLength > data.Length)
                actualLength = data.Length - startIndex;

            char[] chs = new char[actualLength * 3 - 1];
            byte b;
            bool isFirst = true;
            int index = 0;
            for (int i = startIndex; i < startIndex + actualLength; ++i)
            {
                if (isFirst)
                    isFirst = false;
                else
                    chs[index++] = ' ';

                b = ((byte)(data[i] >> 4));
                chs[index++] = (char)(b > 9 ? b + 0x37 : b + 0x30);

                b = ((byte)(data[i] & 0x0F));
                chs[index++] = (char)(b > 9 ? b + 0x37 : b + 0x30);
            }
            return new string(chs);
        }

        public static string GetHex(byte[] data)
        {
            if (data == null)
                return "";
            return GetHex(data, 0, data.Length);
        }

        public static string GetHex(IList<byte> data, int startIndex, int length)
        {
            if (data == null || length == 0 || startIndex >= data.Count)
                return "";

            int actualLength = length;
            if (startIndex + actualLength > data.Count)
                actualLength = data.Count - startIndex;

            char[] chs = new char[actualLength * 3 - 1];
            byte b;
            bool isFirst = true;
            int index = 0;
            for (int i = startIndex; i < startIndex + actualLength; ++i)
            {
                if (isFirst)
                    isFirst = false;
                else
                    chs[index++] = ' ';

                b = ((byte)(data[i] >> 4));
                chs[index++] = (char)(b > 9 ? b + 0x37 : b + 0x30);

                b = ((byte)(data[i] & 0x0F));
                chs[index++] = (char)(b > 9 ? b + 0x37 : b + 0x30);
            }
            return new string(chs);
        }

        public static string GetHex(IList<byte> data)
        {
            return GetHex(data, 0, data.Count);
        }

        /// <summary>
        /// Example: 1234FD55
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string GetHexShort(byte[] data)
        {
            if (data == null)
                return "";
            char[] chs = new char[data.Length * 2];
            byte b;
            for (int bx = 0, cx = 0; bx < data.Length; ++bx, ++cx)
            {
                b = ((byte)(data[bx] >> 4));
                chs[cx] = (char)(b > 9 ? b + 0x37 : b + 0x30);

                b = ((byte)(data[bx] & 0x0F));
                chs[++cx] = (char)(b > 9 ? b + 0x37 : b + 0x30);
            }
            return new string(chs);
        }

        public static string GetHexShortDefaultNullable(byte[] data)
        {
            if (data == null || data.Length == 0)
                return null;

            int sum = data.Sum(x => (int)x);
            if (sum > 0 && sum < data.Length * 0xFF)
            {
                return GetHexShort(data);

            }
            else
            {
                return null;
            }
        }

        public static byte GetMaskFromBits(byte bits, byte offsetInData)
        {
            byte tmp1 = (byte)(0xFF >> (8 - offsetInData - bits));
            byte tmp2 = (byte)(0xFF << offsetInData);
            byte mask = (byte)(tmp1 & tmp2);
            return mask;
        }

        public static List<byte[]> ArraySplit(byte[] data, int fragmentSize)
        {
            if (fragmentSize != 0)
            {
                List<byte[]> result = new List<byte[]>();
                if (data.Length <= fragmentSize)
                {
                    result.Add(data);
                }
                else
                {
                    for (int i = 0; i < data.Length; i += fragmentSize)
                    {
                        byte[] arr = new byte[fragmentSize];
                        if (i < (data.Length - fragmentSize))
                        {
                            Buffer.BlockCopy(data, i, arr, 0, fragmentSize);
                        }
                        else
                        {
                            arr = new byte[data.Length - i];
                            Buffer.BlockCopy(data, i, arr, 0, arr.Length);
                        }
                        result.Add(arr);
                    }
                }
                return result;
            }
            return null;
        }
    }
}