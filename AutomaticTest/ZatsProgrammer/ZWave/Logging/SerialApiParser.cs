// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Text;
using ZWave.Enums;
using ZWave.Framework;

namespace ZWave.Logging
{
    public static class SerialApiParser
    {
        internal static string GetApplicationString(byte applicationByte, byte[] payload, Type enumType)
        {
            if (payload != null && payload.Length == 1 && payload[0] == applicationByte)
            {
                HeaderTypes type = (HeaderTypes)applicationByte;
                switch (type)
                {
                    case HeaderTypes.Acknowledge:
                        return "06-Ack";
                    case HeaderTypes.NotAcknowledged:
                        return "15-Nak";
                    case HeaderTypes.Can:
                        return "18-Can";
                    default:
                        return type.ToString();
                }
            }
            else
            {
                string ret = Tools.GetHex(applicationByte);
                try
                {
                    if (enumType != null)
                    {
                        ret = string.Format("{0:X2}-{1}", applicationByte, Enum.GetName(enumType, applicationByte));
                    }
                }
                catch (ArgumentNullException)
                {
                }
                catch (ArgumentException)
                {
                }
                return ret;
            }
        }
    }
}
