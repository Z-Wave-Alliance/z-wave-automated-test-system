// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils;
using System.Net;

namespace Zats.Services
{
    public static class ToStringExtensions
    {
        public static bool BytesEquals(this IPAddress source, IPAddress another)
        {
            bool ret = false;
            byte[] sourceBytes = source.GetAddressBytes();
            byte[] anotherBytes = another.GetAddressBytes();
            for (int i = 0; i < sourceBytes.Length; i++)
            {
                int diff = sourceBytes.Length - anotherBytes.Length;
                if ((i - diff) >= 0)
                {
                    ret = sourceBytes[i] == anotherBytes[i - diff];
                    if (!ret)
                        break;
                }
            }
            return ret;
        }

        public static int ToNumber(this byte[] source)
        {
            int ret = 0;
            if (source != null)
            {
                int length = source.Length;
                if (length < 4)
                {
                    for (int i = 0; i < length; i++)
                    {
                        ret = ret << 8;
                        ret += source[i];
                    }
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        ret = ret << 8;
                        ret += source[length - 4 + i];
                    }
                }
            }
            return ret;
        }

        public static byte[] ToByteArray(this int source, int length)
        {
            byte[] ret = new byte[length];
            int tmp = source;
            for (int i = 0; i < length; i++)
            {
                ret[length - 1 - i] = (byte)tmp;
                tmp = tmp >> 8;
            }
            return ret;
        }

        public static byte[] ToByteArray(this ushort source)
        {
            byte[] ret = new byte[2];
            int tmp = source;
            for (int i = 0; i < 2; i++)
            {
                ret[2 - 1 - i] = (byte)tmp;
                tmp = tmp >> 8;
            }
            return ret;
        }

        public static byte[] ToByteArray(this string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return null;
            }
            else
                return Tools.GetBytes(source);
        }

        public static byte[] GetCrc16Bytes(this IEnumerable<byte> source, int index, int length)
        {
            byte[] ret = null;
            if (source != null)
            {
                ret = Tools.CalculateCrc16Array(source, index, length);
            }
            return ret;
        }

        public static byte[] GetCrc16Bytes(this IEnumerable<byte> source)
        {
            byte[] ret = null;
            if (source != null)
            {
                ret = Tools.CalculateCrc16Array(source);
            }
            return ret;
        }

        public static string ToHexString(this byte[] source)
        {
            if (source == null)
            {
                return "<null>";
            }
            else
                return Tools.GetHex(source);
        }

        public static string ToHexString(this IList<byte> source)
        {
            if (source == null)
            {
                return "<null>";
            }
            else
                return Tools.GetHex(source);
        }

        public static bool StartsWith(this byte[] source, byte[] value)
        {
            if (source != null && value != null && source.Length >= value.Length)
            {
                bool ret = true;
                for (int i = 0; i < value.Length; i++)
                {
                    ret &= source[i] == value[i];
                    if (!ret)
                        break;
                }
                return ret;
            }
            else
                return false;
        }

        public static bool StartsWith(this IList<byte> source, byte[] value)
        {
            if (source != null && value != null && source.Count >= value.Length)
            {
                bool ret = true;
                for (int i = 0; i < value.Length; i++)
                {
                    ret &= source[i] == value[i];
                    if (!ret)
                        break;
                }
                return ret;
            }
            else
                return false;
        }

        public static bool Contains(this byte[] source, byte value)
        {
            if (source != null)
                return Array.IndexOf<byte>(source, value) >= 0;
            else
                return false;
        }

        public static bool Contains(this Node[] source, Node value)
        {
            if (source != null)
                return Array.IndexOf<Node>(source, value) >= 0;
            else
                return false;
        }

        public static bool Contains(this IList<byte> source, byte value)
        {
            if (source != null)
                return source.IndexOf(value) >= 0;
            else
                return false;
        }

        public static bool BelongsTo(this byte value, params byte[] array)
        {
            if (array != null)
                return Array.IndexOf<byte>(array, value) >= 0;
            else
                return false;
        }

        public static bool IsInRange(this byte value, byte start, byte end)
        {
            return value >= start && value <= end;
        }

        public static bool IsInRange(this int value, int start, int end)
        {
            return value >= start && value <= end;
        }
    }
}
