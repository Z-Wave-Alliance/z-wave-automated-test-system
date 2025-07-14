// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;

namespace Zats.Services
{
    public class DeviceMeterType : DeviceAttributes
    {
        public uint Sequence { get; set; }
        public StringValue Type { get; set; }
        public StringValue RateType { get; set; }
        public BoolValue Reset { get; set; }
        public List<DeviceMeterScale> Scales { get; set; }

        public class DeviceMeterScale
        {
            public StringValue Scale { get; set; }
            public UIntValue Size { get; set; }
            public UIntValue Precision { get; set; }

            public DeviceMeterScale(StringValue scale, UIntValue size, UIntValue precision)
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
            Console.WriteLine(" - RateType: " + RateType.Value);
            Console.WriteLine(" - Reset:    " + Reset.Value.ToString());
            foreach (DeviceMeterScale scale in Scales)
            {
                Console.WriteLine(" - Scale:    " + scale.Scale.Value);
                Console.WriteLine(" --- Size:      " + scale.Size.Value);
                Console.WriteLine(" --- Precision: " + scale.Precision.Value);
            }
        }
    }
}
