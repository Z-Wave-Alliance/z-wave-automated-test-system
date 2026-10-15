// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.IO;
using System.Threading;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Collections;
using System.Text;
using ZWave.Framework;
using System.Collections.Generic;

namespace ZWave.Programmer.Classes
{
    /// <summary>
    /// Class used to work with Intel Hex files
    /// </summary>
    public class HexFileHelper
    {
        private enum RECORDS
        {
            DATA = 0x0,
            EOF = 0x1,
            EXTENDED_SEGMENT_ADDR = 0x02,
            START_SEGMENT_ADDR = 0x03,
            EXTENDED_LINEAR_ADDR = 0x04,
            START_LINEAR_ADDR = 0x05
        }
        public static byte[] GetBytes(SortedList<short, List<byte>> data, int length, byte emptyValue)
        {
            byte[] result = null;
            List<byte> r = new List<byte>();
            foreach (short key in data.Keys)
            {
                if (r.Count > 0)
                {
                    int nextSegmentAddress = key << 16;
                    if (r.Count < nextSegmentAddress)
                    {
                        r.AddRange(BlankArray(new byte[(nextSegmentAddress) - r.Count], emptyValue));
                    }
                }
                if (key < 0x0003)
                {
                    r.AddRange(data[key]);
                }
            }
            if (r.Count < length)
            {
                r.AddRange(BlankArray(new byte[(length) - r.Count], emptyValue));
            }
            if (r.Count > 0) result = r.ToArray();
            return result;
        }
        public static SortedList<short, List<byte>> ReadIntelHexFile(string fileName, byte emptyValue)
        {
            if (!File.Exists(fileName)) throw new FileNotFoundException();
            SortedList<short, List<byte>> result = new SortedList<short, List<byte>>();
            StreamReader sr = File.OpenText(fileName);
            ReadHex(emptyValue, result, sr);
            return result;
        }
        public static SortedList<short, List<byte>> ReadIntelHexData(string hexData, byte emptyValue)
        {
            SortedList<short, List<byte>> result = new SortedList<short, List<byte>>();
            StringReader sr = new StringReader(hexData);
            ReadHex(emptyValue, result, sr);
            return result;
        }

        private static void ReadHex(byte emptyValue, SortedList<short, List<byte>> result, StreamReader sr)
        {
            short currentSegmentKey = 0;
            string input = "";
            while ((input = sr.ReadLine()) != null)
            {
                if (!input.StartsWith(":"))
                {
                    sr.Close();
                    throw new Exception("Not Intel HEX file.");
                }
                byte[] hexRecord = Tools.FromHexString(input.Replace(":", ""));
                if (hexRecord.Length > 3)
                {
                    if (hexRecord[3] == (byte)RECORDS.DATA)
                    {
                        if (hexRecord.Length != hexRecord[0] + 5)
                            throw new Exception("Not Intel HEX file.");

                        if (result.Count == 0)
                        {
                            result.Add(0, new List<byte>());
                        }

                        ushort address = (ushort)((hexRecord[1] << 8) + hexRecord[2]);


                        if ((result[currentSegmentKey].Count - 1) < address)
                        {
                            result[currentSegmentKey].AddRange(BlankArray(new byte[(address + 1) - result[currentSegmentKey].Count], emptyValue));
                        }
                        if (result[currentSegmentKey].Count < (hexRecord[0] + address))
                        {
                            result[currentSegmentKey].AddRange(BlankArray(new byte[hexRecord[0] - 1], emptyValue));
                        }

                        for (int i = 0; i < hexRecord[0]; i++)
                        {
                            result[currentSegmentKey][address] = hexRecord[i + 4];
                            address++;
                        }
                    }
                    else if (hexRecord[3] == (byte)RECORDS.EOF)
                    {
                        //
                    }
                    else if (hexRecord[3] == (byte)RECORDS.EXTENDED_LINEAR_ADDR)
                    {
                        if (hexRecord.Length < 7)
                        {
                            sr.Close();
                            throw new Exception("Not Intel HEX file.");
                        }
                        currentSegmentKey = (short)((hexRecord[4] << 8) + hexRecord[5]);
                        result.Add(currentSegmentKey, new List<byte>());
                    }
                    else if (hexRecord[3] == (byte)RECORDS.EXTENDED_SEGMENT_ADDR)
                    {
                        if (hexRecord.Length < 7)
                        {
                            sr.Close();
                            throw new Exception("Not Intel HEX file.");
                        }
                    }
                    else
                    {
                        sr.Close();
                        throw new Exception("Unknown field found. Not Intel HEX file.");
                    }
                }
            }
            sr.Close();
        }
        private static void ReadHex(byte emptyValue, SortedList<short, List<byte>> result, StringReader sr)
        {
            short currentSegmentKey = 0;
            string input = "";
            while ((input = sr.ReadLine()) != null)
            {
                if (!input.StartsWith(":"))
                {
                    sr.Close();
                    throw new Exception("Not Intel HEX file.");
                }
                byte[] hexRecord = Tools.FromHexString(input.Replace(":", ""));
                if (hexRecord.Length > 3)
                {
                    if (hexRecord[3] == (byte)RECORDS.DATA)
                    {
                        if (hexRecord.Length != hexRecord[0] + 5)
                            throw new Exception("Not Intel HEX file.");

                        if (result.Count == 0)
                        {
                            result.Add(0, new List<byte>());
                        }

                        ushort address = (ushort)((hexRecord[1] << 8) + hexRecord[2]);


                        if ((result[currentSegmentKey].Count - 1) < address)
                        {
                            result[currentSegmentKey].AddRange(BlankArray(new byte[(address + 1) - result[currentSegmentKey].Count], emptyValue));
                        }
                        if (result[currentSegmentKey].Count < (hexRecord[0] + address))
                        {
                            result[currentSegmentKey].AddRange(BlankArray(new byte[hexRecord[0] - 1], emptyValue));
                        }

                        for (int i = 0; i < hexRecord[0]; i++)
                        {
                            result[currentSegmentKey][address] = hexRecord[i + 4];
                            address++;
                        }
                    }
                    else if (hexRecord[3] == (byte)RECORDS.EOF)
                    {
                        //
                    }
                    else if (hexRecord[3] == (byte)RECORDS.EXTENDED_LINEAR_ADDR)
                    {
                        if (hexRecord.Length < 7)
                        {
                            sr.Close();
                            throw new Exception("Not Intel HEX file.");
                        }
                        currentSegmentKey = (short)((hexRecord[4] << 8) + hexRecord[5]);
                        result.Add(currentSegmentKey, new List<byte>());
                    }
                    else if (hexRecord[3] == (byte)RECORDS.EXTENDED_SEGMENT_ADDR)
                    {
                        if (hexRecord.Length < 7)
                        {
                            sr.Close();
                            throw new Exception("Not Intel HEX file.");
                        }
                    }
                    else
                    {
                        sr.Close();
                        throw new Exception("Unknown field found. Not Intel HEX file.");
                    }
                }
            }
            sr.Close();
        }
        public static byte[] BlankArray(int length, byte emptyValue)
        {
            byte[] array = new byte[length];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = emptyValue;
            }
            return array;
        }
        public static byte[] BlankArray(byte[] array, byte emptyValue)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = emptyValue;
            }
            return array;
        }

        public static string WriteIntelHexFileStartingFromAddress(byte[] buffer, int startAddress, int endAddress, int recordLength, byte emptyValue, bool skipNoDataLines)
        {
            if (startAddress >= recordLength)
                throw new NotImplementedException("startAddress should be less than record length");
            StringBuilder sb = new StringBuilder();
            List<byte[]> sBuffer = new List<byte[]>();
            ushort seg_num = 0;
            if (recordLength == 0) recordLength = 0x10;
            for (int i = 0; i < buffer.Length; i += recordLength)
            {
                byte[] arr = new byte[recordLength];
                if (startAddress < recordLength && i == 0)
                {
                    arr = new byte[recordLength - startAddress];
                    Buffer.BlockCopy(buffer, startAddress, arr, 0, recordLength - startAddress);
                }
                else if (i < (buffer.Length - recordLength))
                {
                    Buffer.BlockCopy(buffer, i, arr, 0, recordLength);
                }
                else
                {
                    arr = new byte[buffer.Length - i];
                    Buffer.BlockCopy(buffer, i, arr, 0, arr.Length);
                }
                sBuffer.Add(arr);
            }
            if (!skipNoDataLines)
                sb.AppendLine(string.Format(":02000004{0}{1}", Tools.ToHexString(seg_num), Tools.ToHexString((byte)(0xFA - seg_num))));
            for (int i = 0; i < sBuffer.Count; i++)
            {
                byte chksum = (byte)sBuffer[i].Length;
                if (i == 0 && startAddress != 0)
                {
                    chksum += (byte)(startAddress & 0x00FF);
                }
                else
                {
                    chksum += (byte)((byte)(((ushort)(i * recordLength)) & 0x00FF) + (byte)(((ushort)(i * recordLength)) >> 8 & 0x00FF));
                }
                if ((i >> 12) > seg_num)
                {
                    seg_num = (ushort)(i >> 12);
                    sb.AppendLine(string.Format(":02000004{0}{1}", Tools.ToHexString(seg_num), Tools.ToHexString((byte)(0xFA - seg_num))));
                }
                if (i == 0 && startAddress != 0)
                {
                    sb.Append(string.Format(":{0}{1}00", Tools.ToHexString((byte)sBuffer[i].Length), Tools.ToHexString((ushort)(((startAddress) & 0x0000FFFF)))));
                }
                else
                {
                    sb.Append(string.Format(":{0}{1}00", Tools.ToHexString((byte)sBuffer[i].Length), Tools.ToHexString((ushort)((i * recordLength) & 0x0000FFFF))));
                }
                sb.Append(Tools.ToHexString(sBuffer[i], ""));
                for (int j = 0; j < sBuffer[i].Length; j++)
                {
                    chksum += sBuffer[i][j];
                }
                if (chksum != 0)
                {
                    sb.Append(Tools.ToHexString((byte)(0x100 - chksum)));
                }
                else
                {
                    sb.Append(Tools.ToHexString(0x00));
                }
                sb.Append(Environment.NewLine);
            }
            if (!skipNoDataLines)
                sb.AppendLine(":00000001FF");
            return sb.ToString();
        }

        public static string WriteIntelHexFile(byte[] buffer, int startAddress, int endAddress, int recordLength, byte emptyValue, bool skipNoDataLines)
        {
            StringBuilder sb = new StringBuilder();
            List<byte[]> sBuffer = new List<byte[]>();
            ushort seg_num = 0;
            if (recordLength == 0) recordLength = 0x10;
            //if (endAddress > buffer.Length)
            //{
            //    throw new ApplicationException("Unexpected end address.");
            //}
            //else
            //{
            for (int i = 0; i < buffer.Length; i += recordLength)
            {
                byte[] arr = new byte[recordLength];
                if (i < (buffer.Length - recordLength))
                {
                    Buffer.BlockCopy(buffer, i, arr, 0, recordLength);
                }
                else
                {
                    arr = new byte[buffer.Length - i];
                    Buffer.BlockCopy(buffer, i, arr, 0, arr.Length);
                }
                sBuffer.Add(arr);
            }
            if (!skipNoDataLines)
                sb.AppendLine(string.Format(":02000004{0}{1}", Tools.ToHexString(seg_num), Tools.ToHexString((byte)(0xFA - seg_num))));
            for (int i = 0; i < sBuffer.Count; i++)
            {
                byte chksum = (byte)sBuffer[i].Length;
                chksum += (byte)((byte)(((ushort)(i * recordLength)) & 0x00FF) + (byte)(((ushort)(i * recordLength)) >> 8 & 0x00FF));
                if ((i >> 12) > seg_num)
                {
                    seg_num = (ushort)(i >> 12);
                    sb.AppendLine(string.Format(":02000004{0}{1}", Tools.ToHexString(seg_num), Tools.ToHexString((byte)(0xFA - seg_num))));
                }
                if (startAddress > 0)
                {
                    sb.Append(string.Format(":{0}{1}00", Tools.ToHexString((byte)sBuffer[i].Length), Tools.ToHexString((ushort)(((startAddress + i) * recordLength) & 0x0000FFFF))));
                }
                else
                {
                    sb.Append(string.Format(":{0}{1}00", Tools.ToHexString((byte)sBuffer[i].Length), Tools.ToHexString((ushort)((i * recordLength) & 0x0000FFFF))));
                }
                sb.Append(Tools.ToHexString(sBuffer[i], ""));
                for (int j = 0; j < sBuffer[i].Length; j++)
                {
                    chksum += sBuffer[i][j];
                }
                if (chksum != 0)
                {
                    sb.Append(Tools.ToHexString((byte)(0x100 - chksum)));
                }
                else
                {
                    sb.Append(Tools.ToHexString(0x00));
                }
                sb.Append(Environment.NewLine);
            }
            if (!skipNoDataLines)
                sb.AppendLine(":00000001FF");
            //}
            #region old
            /*ushort seg_num = 0;
            byte chksum = 0;
            for (int i = startAddress / 16; i < ((endAddress - 1) / 16) + 1; i++)
            {
                if ((i >> 12) > seg_num)
                {
                    seg_num = (ushort)(i >> 12);
                    sb.AppendLine(string.Format(":02000004{0}{1}", Tools.ToHexString(seg_num), Tools.ToHexString((byte)(0xFA - seg_num))));
                }
                for (int j = 0; j < recordLength; j++)
                {
                    if (j % 16 == 0)
                    {
                        chksum = (byte)(0x10 + (byte)((((i * 16) + j)) & 0x00FF) + (byte)((((i * 16) + j)) >> 8 & 0x00FF));
                        sb.Append(string.Format(":10{0}00", Tools.ToHexString((ushort)(((i * 16) + j) & 0x0000FFFF))));
                        //fprintf(pOutFile,":10%04X00",((i*16)+j)&0x0000FFFF);
                    }
                    if ((buffer.Length - 1) > ((i * 16) + j))
                    {
                        chksum += buffer[(i * 16) + j];
                        sb.Append(Tools.ToHexString((byte)(buffer[(i * 16) + j] & 0xff)));
                    }
                    //fprintf(pOutFile,"%02X", mem[(i*16)+j]&0xff);
                    if (j % 16 == 15)
                    {
                        if (chksum != 0)
                        {
                            sb.Append(Tools.ToHexString((byte)(0x100 - chksum)) + Environment.NewLine);
                        }
                        //fprintf(pOutFile,"%02X\n",0x100-chksum);
                        else
                        {
                            sb.Append(Tools.ToHexString((byte)(0x00)) + Environment.NewLine);
                        }
                        //fprintf(pOutFile, "%02X\n", 0);
                    }
                }
            }

            sb.AppendLine(":00000001FF");*/
            #endregion
            return sb.ToString();
        }

        #region Old Code

        private struct flashMem
        {
            public byte length;
            public int adr;
            public byte type;
            public byte crc;
            public int sba;
            public int lba;
            public void SetAdr(byte msb, byte lsb)
            {
                adr = (msb << 8) | lsb;
            }
            public void SetLba(byte msb, byte lsb)
            {
                lba = (msb << 8) | lsb;
            }
            public void SetSba(byte msb, byte lsb)
            {
                sba = (msb << 8) | lsb;
            }

            public void SetType(byte typ)
            {
                type = typ;
            }

        }

        /// <summary>
        /// FILE_READ, FILE_WRITTEN, FILE_OPEN_ERROR, NOT_INTEL_HEX, ADDRESS_OUT_OF_RANGE, UNKNOWN_FIELD constants.
        /// </summary>
        internal const byte FILE_READ = 0,
          FILE_WRITTEN = 0,
          FILE_OPEN_ERROR = 1,
          NOT_INTEL_HEX = 2,
          ADDRESS_OUT_OF_RANGE = 3,
          UNKNOWN_FIELD = 4;
        /// <summary>
        /// COMPARE_OK, COMPARE_FAIL constants.
        /// </summary>
        internal const int COMPARE_OK = 0x0000, COMPARE_FAIL = 0xFFFF;
        ////////private enum RECORDS
        ////////{
        ////////    DATA = 0x0,
        ////////    EOF = 0x1,
        ////////    EXTENDED_SEGMENT_ADDR = 0x02,
        ////////    START_SEGMENT_ADDR = 0x03,
        ////////    EXTENDED_LINEAR_ADDR = 0x04,
        ////////    START_LINEAR_ADDR = 0x05
        ////////}
        /////////// <summary>
        /////////// Initializes a new instance of the <see cref="HexFileHelper"/> class.
        /////////// </summary>
        ////////public HexFileHelper()
        ////////{
        ////////    // 
        ////////    // TODO: Add constructor logic here
        ////////    //
        ////////}
        /////////// <summary>
        /////////// Creates the blank array.
        /////////// </summary>
        /////////// <param name="length">The length.</param>
        /////////// <param name="blankValue">The blank value.</param>
        /////////// <returns></returns>
        ////////static public byte[] BlankArray(int length, byte blankValue)
        ////////{
        ////////    byte[] retVal = new byte[length];
        ////////    for (int i = 0; i < length; i++)
        ////////        retVal[i] = blankValue;
        ////////    return retVal;
        ////////}

        /////////// <summary>
        /////////// Compares two byte arrays
        /////////// </summary>
        /////////// <param name="flash1">Array one to compare</param>
        /////////// <param name="flash2">Array two to compare</param>
        /////////// <param name="maxAdr">Max address to use</param>
        /////////// <returns>COMPARE_OK if succesfull</returns>
        ////////static public int Compare(byte[] flash1, byte[] flash2, int maxAdr)
        ////////{
        ////////    int i;
        ////////    for (i = 0; i < maxAdr; i++)
        ////////    {
        ////////        if (flash1[i] != flash2[i])
        ////////        {
        ////////            return COMPARE_FAIL;
        ////////        }
        ////////    }
        ////////    return COMPARE_OK;
        ////////}

        /////////// <summary>
        /////////// Converts data to Intel HEX format.
        /////////// </summary>
        /////////// <param name="startAdr">The start address.</param>
        /////////// <param name="data">The data.</param>
        /////////// <returns></returns>
        ////////static public string ToIntelHexData(int startAdr, byte[] data)
        ////////{
        ////////    int i = 0;
        ////////    byte recLen = (byte)data.Length;
        ////////    StringBuilder strData = new StringBuilder();
        ////////    byte crc;
        ////////    crc = recLen;
        ////////    while (i < data.Length)
        ////////    {
        ////////        crc += data[i];
        ////////        strData.Append(Tools.ToHexString(data[i++]));//flashData[count+i].ToString("X00");
        ////////    }
        ////////    crc += (byte)(startAdr >> 8);
        ////////    crc += (byte)(startAdr);
        ////////    crc = (byte)(~((int)crc) + 1);
        ////////    return (':' + Tools.ToHexString(recLen) + Tools.ToHexString((ushort)(startAdr)) +
        ////////            Tools.ToHexString((byte)RECORDS.DATA) + strData + Tools.ToHexString(crc));
        ////////}


        /////////// <summary>
        /////////// Converts data to Intel HEX format.
        /////////// </summary>
        /////////// <param name="startAdr">The start address.</param>
        /////////// <param name="data">The data.</param>
        /////////// <param name="skipValue">The skip value.</param>
        /////////// <returns></returns>
        ////////static public string ToIntelHexData(int startAdr, byte[] data, byte skipValue)
        ////////{
        ////////    bool blank = true;
        ////////    for (int i = 0; i < data.Length; i++)
        ////////    {
        ////////        if (data[i] != skipValue)
        ////////        {
        ////////            blank = false;
        ////////            break;
        ////////        }
        ////////    }
        ////////    if (!blank)
        ////////    {
        ////////        return ToIntelHexData(startAdr, data);
        ////////    }
        ////////    return "";
        ////////}

        /// <summary>
        /// Converts data in Intel HEX format to binary data.
        /// </summary>
        /// <param name="intelHex">The data in Intel HEX format.</param>
        /// <param name="defaultValue">The default value of byte (eraced value of memory).</param>
        /// <param name="minAdr">The lowest address to accept from Intel HEX data (byte from this address will be written to position 0 of output array, and so on).</param>
        /// <param name="maxAdr">The highest address to accept from Intel HEX data.</param>
        /// <returns>Array of bytes with binary data.</returns>
        static public byte[] FromIntelHexData(string intelHex, byte defaultValue, uint minAdr, uint maxAdr)
        {
            //TODO: implementation of this function should be replaced by solution without using temporary file.
            byte[] binData = null;
            try
            {
                string hexFileName = Path.GetTempFileName();
                StreamWriter sw = File.AppendText(hexFileName);
                sw.Write(intelHex);
                sw.Close();
                binData = BlankArray((int)(maxAdr - minAdr), defaultValue);
                if (ReadIntelHexFile(hexFileName, binData, (int)maxAdr, (int)minAdr) != HexFileHelper.FILE_READ)
                {
                    binData = null;
                }
                File.Delete(hexFileName);
            }
            catch
            {
            }
            return binData;
        }

        /////////// <summary>
        /////////// Writes the Intel HEX file.
        /////////// </summary>
        /////////// <param name="filename">The filename.</param>
        /////////// <param name="flashData">The flash data.</param>
        /////////// <returns></returns>
        ////////static public byte WriteIntelHexFile(string filename, byte[] flashData)
        ////////{
        ////////    return WriteIntelHexFile(filename, flashData, 0x10);
        ////////}
        /////////// <summary>
        /////////// Writes the Intel Hex file.
        /////////// </summary>
        /////////// <param name="filename">The filename.</param>
        /////////// <param name="flashData">The flash data.</param>
        /////////// <param name="recLen">The rec len.</param>
        /////////// <returns></returns>
        ////////static public byte WriteIntelHexFile(string filename, byte[] flashData, byte recLen)
        ////////{
        ////////    return WriteIntelHexFile(filename, flashData, 0, flashData.Length, recLen);
        ////////}

        /////////// <summary>
        /////////// Writes a file with a C# byte array containing the data from the array specified
        /////////// </summary>
        /////////// <param name="filename">file name to write</param>
        /////////// <param name="flashData">data to put in array</param>
        /////////// <param name="className">Name of the class.</param>
        /////////// <param name="arrayName">array name to give</param>
        /////////// <returns></returns>
        ////////static public byte WriteCSharpArray(string filename, byte[] flashData, string className, string arrayName)
        ////////{
        ////////    int i = flashData.Length;
        ////////    int length = 0;
        ////////    string strData = "";
        ////////    StreamWriter sr = File.CreateText(filename);
        ////////    while (i-- != 0)
        ////////    {
        ////////        if (flashData[i] != 0)
        ////////            break;
        ////////    }
        ////////    if (i > 0)
        ////////        length = i;
        ////////    i = 0;
        ////////    strData = "public class " + className + " \n{";
        ////////    sr.Write(strData);
        ////////    strData = "public byte[] " + arrayName + "={";
        ////////    sr.Write(strData);
        ////////    while (i < length)
        ////////    {
        ////////        strData = flashData[i++].ToString(Thread.CurrentThread.CurrentCulture) + ",";//ToIntelHexData(i+startAdr,data);
        ////////        if ((strData != null) && (strData.Length != 0))
        ////////        {
        ////////            sr.WriteLine(strData);
        ////////        }
        ////////    }
        ////////    strData = flashData[i++].ToString(Thread.CurrentThread.CurrentCulture) + "}";//ToIntelHexData(i+startAdr,data);
        ////////    if ((strData != null) && (strData.Length != 0))
        ////////        sr.Write(strData);
        ////////    else
        ////////        sr.WriteLine("}");
        ////////    sr.WriteLine(";}");
        ////////    sr.Close();
        ////////    return FILE_WRITTEN;

        ////////}
        /////////// <summary>
        /////////// Writes contents of flashData to an intel Hexfile
        /////////// </summary>
        /////////// <param name="filename">Name of file to create</param>
        /////////// <param name="flashData">All the data is written to intel hex file</param>
        /////////// <param name="startAdr">The offset to start the writing at</param>
        /////////// <param name="endAdr">The highest adress to write to</param>
        /////////// <param name="recTemp">Length of data records</param>
        /////////// <returns>
        /////////// FILE_OPEN_ERROR if file exists, FILE_WRITTEN if ok
        /////////// </returns>
        ////////static public byte WriteIntelHexFile(string filename, byte[] flashData, int startAdr, int endAdr, params byte[] recTemp)
        ////////{
        ////////    int i = 0;
        ////////    int count;
        ////////    StreamWriter sr = File.CreateText(filename);
        ////////    byte recLen = 0x10;
        ////////    string strData = "";
        ////////    if (recTemp.Length != 0)
        ////////    {
        ////////        recLen = recTemp[0];
        ////////    }
        ////////    while (i < endAdr)
        ////////    {
        ////////        strData = "";
        ////////        byte[] data = new byte[recLen];

        ////////        for (count = 0; count < recLen; count++)
        ////////        {
        ////////            if ((count + i) < (flashData.Length))
        ////////            {
        ////////                data[count] = flashData[count + i];
        ////////            }
        ////////            else
        ////////            {
        ////////                data[count] = 0;
        ////////            }
        ////////        }
        ////////        if (recTemp.Length < 2)
        ////////        {
        ////////            strData = ToIntelHexData(i + startAdr, data);
        ////////        }
        ////////        else
        ////////        {
        ////////            strData = ToIntelHexData(i + startAdr, data, recTemp[1]);
        ////////        }
        ////////        i += recLen;
        ////////        if ((strData != null) && (strData.Length != 0))
        ////////        {
        ////////            sr.WriteLine(strData);
        ////////        }
        ////////    }
        ////////    //Write EOF
        ////////    sr.WriteLine(":00000001FF");
        ////////    sr.Close();
        ////////    return FILE_WRITTEN;

        ////////}

        /////////// <summary>
        /////////// Writes the Intel HEX file.
        /////////// </summary>
        /////////// <param name="flashData">The flash data.</param>
        /////////// <param name="startAdr">The start adr.</param>
        /////////// <param name="endAdr">The end adr.</param>
        /////////// <param name="recTemp">The rec temp.</param>
        /////////// <returns></returns>
        ////////static public string WriteIntelHexFile(byte[] flashData, int startAdr, int endAdr, params byte[] recTemp)
        ////////{
        ////////    int i = 0;
        ////////    int count;
        ////////    StringWriter sr = new StringWriter();
        ////////    byte recLen = 0x10;
        ////////    string strData = "";
        ////////    int extLinearAddress = 0;
        ////////    if (recTemp.Length != 0)
        ////////    {
        ////////        recLen = recTemp[0];
        ////////    }
        ////////    while (i < endAdr)
        ////////    {
        ////////        strData = "";
        ////////        byte[] data = new byte[recLen];

        ////////        for (count = 0; count < recLen; count++)
        ////////        {
        ////////            if ((count + i) < (flashData.Length))
        ////////            {
        ////////                data[count] = flashData[count + i];
        ////////            }
        ////////            else
        ////////            {
        ////////                data[count] = 0;
        ////////            }
        ////////        }
        ////////        if (recTemp.Length <= 2)
        ////////        {
        ////////            if (((i + startAdr) / ushort.MaxValue) > 0 && extLinearAddress != ((i + startAdr) / ushort.MaxValue))
        ////////            {
        ////////                extLinearAddress = ((i + startAdr) / ushort.MaxValue);
        ////////                sr.WriteLine(":02000004" + GetExLinearAddressString((ushort)extLinearAddress) + GetExLinearAddressCrc((ushort)extLinearAddress));
        ////////                strData = ToIntelHexData(i + startAdr, data);
        ////////            }
        ////////            else
        ////////            {
        ////////                strData = ToIntelHexData(i + startAdr, data);


        ////////            }
        ////////        }
        ////////        else
        ////////        {
        ////////            strData = ToIntelHexData(i + startAdr, data, recTemp[1]);
        ////////        }
        ////////        i += recLen;
        ////////        if ((strData != null) && (strData.Length != 0))
        ////////        {
        ////////            sr.WriteLine(strData);
        ////////        }
        ////////    }
        ////////    //Write EOF
        ////////    sr.WriteLine(":00000001FF");
        ////////    sr.Close();
        ////////    return sr.ToString();

        ////////}

        ////////private static string GetExLinearAddressCrc(ushort extLinearAddress)
        ////////{
        ////////    return ((byte)(0x00 - (0x02 + 0x00 + 0x00 + 0x04 + ((byte)(extLinearAddress >> 8 & 0xFF)) + ((byte)(extLinearAddress & 0xFF))))).ToString("X2");
        ////////}

        ////////private static string GetExLinearAddressString(ushort extLinearAddress)
        ////////{
        ////////    return ((byte)(extLinearAddress >> 8 & 0xFF)).ToString("X2") + ((byte)(extLinearAddress & 0xFF)).ToString("X2");

        ////////}
        /////////// <summary>
        /////////// Converts string value to long value.
        /////////// </summary>
        /////////// <param name="inData">The in data.</param>
        /////////// <returns></returns>
        ////////static public long ToLong(string inData)
        ////////{
        ////////    if (inData != null)
        ////////    {
        ////////        return long.Parse(inData.Substring(0, 8), NumberStyles.AllowHexSpecifier, Thread.CurrentThread.CurrentCulture);
        ////////    }
        ////////    else
        ////////    {
        ////////        return 0;
        ////////    }
        ////////}
        /////////// <summary>
        /////////// Converts string value to byte array.
        /////////// </summary>
        /////////// <param name="inData">The in data.</param>
        /////////// <returns></returns>
        ////////static public byte[] ToByteArray(string inData)
        ////////{
        ////////    if (inData != null)
        ////////    {
        ////////        int n = 0;
        ////////        byte[] outArray = new byte[inData.Length / 2];
        ////////        for (int i = 0; i < outArray.Length; i++)
        ////////        {
        ////////            outArray[i] = byte.Parse(inData.Substring(n, 2), NumberStyles.AllowHexSpecifier, Thread.CurrentThread.CurrentCulture);
        ////////            n = n + 2;
        ////////        }
        ////////        return outArray;
        ////////    }
        ////////    else
        ////////    {
        ////////        return null;
        ////////    }
        ////////}
        /// <summary>
        /// Reads the contents of a Intel hext file and converts it to
        /// its binary data.
        /// </summary>
        /// <param name="inputfile">Filename to open</param>
        /// <param name="flashData">Array to place data in, index = address</param>
        /// <param name="maxAdr">The highest address to accept from file</param>
        /// <returns>FILE_READ if no error</returns>
        static public byte ReadIntelHexFile(string inputfile, byte[] flashData, int maxAdr)
        {
            return ReadIntelHexFile(inputfile, flashData, maxAdr, 0);
        }

        /// <summary>
        /// Reads the contents of a Intel hext file and converts it to
        /// its binary data.
        /// </summary>
        /// <param name="inputfile">Filename to open</param>
        /// <param name="flashData">Array to place data in, index = address</param>
        /// <param name="maxAdr">The highest address to accept from file</param>
        /// <param name="minAdr">The lowest address to accept from file (byte from this address will be written to position 0 of flashData array, and so on)</param>
        /// <returns>FILE_READ if no error</returns>
        static public byte ReadIntelHexFile(string inputfile, byte[] flashData, int maxAdr, int minAdr)
        {
            bool addData = true;
            flashMem flashInfo = new flashMem();
            if (!File.Exists(inputfile))
            {
                return FILE_OPEN_ERROR;
            }
            StreamReader sr = File.OpenText(inputfile);
            String input;
            String currentHex = "a";
            int i;
            /*Reset when we start on a new file*/
            flashInfo.SetLba(0, 0);
            flashInfo.SetSba(0, 0);
            int lineIndex = 0;
            while ((input = sr.ReadLine()) != null)
            {
                if (!addData) continue;
                if (!input.StartsWith(":"))
                    return NOT_INTEL_HEX;
                else
                {
                    i = 1;
                    currentHex = input.Substring(i, 2);
                    //length first
                    flashInfo.length = byte.Parse(currentHex, NumberStyles.AllowHexSpecifier, Thread.CurrentThread.CurrentCulture);
                    //adr msb
                    i += 2;
                    currentHex = input.Substring(i, 2);
                    byte msb = byte.Parse(currentHex, NumberStyles.AllowHexSpecifier, Thread.CurrentThread.CurrentCulture);
                    //adr lsb
                    i += 2;
                    currentHex = input.Substring(i, 2);
                    byte lsb = byte.Parse(currentHex, NumberStyles.AllowHexSpecifier, Thread.CurrentThread.CurrentCulture);
                    //LOAD OFFSET
                    flashInfo.SetAdr(msb, lsb);
                    i += 2;
                    currentHex = input.Substring(i, 2);
                    //RECTYP
                    flashInfo.type = byte.Parse(currentHex, NumberStyles.AllowHexSpecifier, Thread.CurrentThread.CurrentCulture);
                    i += 2;
                    //Handle remaining bytes by record type

                    if (flashInfo.type == (byte)RECORDS.DATA) //DATA_RECORD = 0
                    {
                        byte byteNo = 0;
                        if ((flashInfo.adr + flashInfo.sba + (flashInfo.lba << 16)) > maxAdr)
                            return ADDRESS_OUT_OF_RANGE;
                        else if ((flashInfo.adr + flashInfo.sba + (flashInfo.lba << 16)) < minAdr)
                            return ADDRESS_OUT_OF_RANGE;
                        else
                        {
                            for (byteNo = 0; byteNo < flashInfo.length; byteNo++)
                            {
                                currentHex = input.Substring(i, 2);
                                flashData[flashInfo.adr + byteNo - minAdr] = byte.Parse(currentHex, NumberStyles.AllowHexSpecifier, Thread.CurrentThread.CurrentCulture);
                                i += 2;
                            }
                        }
                        //Rest on line is crc
                        currentHex = input.Substring(i, 2);
                        flashInfo.crc = byte.Parse(currentHex, NumberStyles.AllowHexSpecifier, Thread.CurrentThread.CurrentCulture);
                    }
                    else if (flashInfo.type == (byte)RECORDS.EOF) //END_OF_FILE
                    {
                    }
                    else if (flashInfo.type == (byte)RECORDS.EXTENDED_SEGMENT_ADDR)
                    {
                        currentHex = input.Substring(i, 2);
                        msb = byte.Parse(currentHex, NumberStyles.AllowHexSpecifier, Thread.CurrentThread.CurrentCulture);
                        i += 2;
                        currentHex = input.Substring(i, 2);
                        lsb = byte.Parse(currentHex, NumberStyles.AllowHexSpecifier, Thread.CurrentThread.CurrentCulture);
                        i += 2;
                        flashInfo.SetSba(msb, lsb);
                    }
                    else if (flashInfo.type == (byte)RECORDS.EXTENDED_LINEAR_ADDR)
                    {

                        /* Extended linear Base Address are the 31 to 16 upper bit of the following
                                     * DATA record.*/
                        currentHex = input.Substring(i, 2);
                        msb = byte.Parse(currentHex, NumberStyles.AllowHexSpecifier, Thread.CurrentThread.CurrentCulture);
                        i += 2;
                        currentHex = input.Substring(i, 2);
                        lsb = byte.Parse(currentHex, NumberStyles.AllowHexSpecifier, Thread.CurrentThread.CurrentCulture);
                        i += 2;
                        flashInfo.SetLba(msb, lsb);
                        //if (lineIndex == 0 && lsb > 0)
                        //{
                        //    sr.Close();
                        //    byte[] fd = null;
                        //    byte res = ReadIntelHexFile500(inputfile, out fd);
                        //    for (int j = 0; j < flashData.Length; j++)
                        //    {
                        //        flashData[j] = fd[j];
                        //    }
                        //    return res;
                        //}
                        addData = flashInfo.lba == 0;
                    }
                    else
                    {
                        return UNKNOWN_FIELD;
                    }
                    lineIndex++;
                }
            }
            sr.Close();
            //System.Diagnostics.Debug.WriteLine("flash data1: " + Tools.ToHexString(flashData));
            return FILE_READ;
        }

        #endregion
    }
}
