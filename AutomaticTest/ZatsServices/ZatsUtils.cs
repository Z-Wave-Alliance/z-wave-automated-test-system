// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;
using System.IO;
using Utils;

namespace Zats.Services
{
    /// <summary>
    /// Helper file for different utility methods to use in the test case
    /// </summary>
    public static class ZatsUtils
    {
        public static byte[] CalculateCrc16Array(IEnumerable<byte> data)
        {
            return Tools.CalculateCrc16Array(data);
        }

        public static IEnumerable<byte> ReadAllBytesFromHexFile(string filePath, byte emptyValue)
        {
            return HexFileHelper.GetBytes(HexFileHelper.ReadIntelHexFile(filePath, emptyValue), emptyValue);
        }

        public static IEnumerable<byte> ReadAllBytesFromBinFile(string filePath)
        {
            return File.ReadAllBytes(filePath);
        }
    }
}
