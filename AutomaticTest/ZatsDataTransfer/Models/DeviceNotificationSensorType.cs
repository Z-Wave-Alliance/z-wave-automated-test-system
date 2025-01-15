// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;

namespace Zats.Services
{
    public class DeviceNotificationType : DeviceAttributes
    {
        public uint Sequence { get; set; }
        public StringValue Type { get; set; }
        public List<StringValue> Events { get; set; }

        public void WriteToConsole()
        {
            Console.WriteLine("Seq: " + Sequence);
            Console.WriteLine(" - Type:     " + Type.Value);
            foreach (StringValue sensorEvent in Events)
            {
                Console.WriteLine(" --- Event:    " + sensorEvent.Value);
            }
        }
    }
}
