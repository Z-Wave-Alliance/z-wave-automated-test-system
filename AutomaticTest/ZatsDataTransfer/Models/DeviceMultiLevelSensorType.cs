// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;

namespace Zats.Services
{
    public class DeviceMultiLevelSensorType : DeviceAttributes
    {
        public uint Sequence { get; set; }
        public StringValue Type { get; set; }
        public List<DeviceSensorScale> Scales { get; set; }

        public class DeviceSensorScale
        {
            public StringValue Scale { get; set; }
            public UIntValue Size { get; set; }
            public UIntValue Precision { get; set; }

            public DeviceSensorScale(StringValue scale, UIntValue size, UIntValue precision)
            {
                Scale = scale;
                Size = size;
                Precision = precision;
            }
        }

        public void WriteToConsole()
        {
            Console.WriteLine("Seq: " + Sequence);
            Console.WriteLine(" - Type:     " + Type.Value);
            foreach (DeviceSensorScale scale in Scales)
            {
                Console.WriteLine(" - Scale:    " + scale.Scale.Value);
                Console.WriteLine(" --- Size:      " + scale.Size.Value);
                Console.WriteLine(" --- Precision: " + scale.Precision.Value);
            }
        }
    }
}
