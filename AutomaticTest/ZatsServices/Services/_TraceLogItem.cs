// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using ZWave;

namespace Zats.Services
{
    public class _TraceLogItem
    {
        public DateTime TimeStamp { get; set; }
        public byte[] Data { get; set; }
        public byte Flags { get; set; }
        public bool IsOutcome
        {
            get { return (Flags & 0x01) > 0; }
            set
            {
                Flags &= 0xFF - 0x01;
                if (value)
                    Flags += 0x01;
            }
        }

        public _TraceLogItem(TraceLogItem item)
            : this(item.TimeStamp, item.Data, item.IsOutcome)
        {
        }

        public _TraceLogItem(DateTime dt, byte[] data, bool isOutcome)
        {
            TimeStamp = dt;
            Data = data;
            IsOutcome = isOutcome;
        }
    }
}
