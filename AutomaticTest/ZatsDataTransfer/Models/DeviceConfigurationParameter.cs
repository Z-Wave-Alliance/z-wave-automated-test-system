// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;

namespace Zats.Services
{
    public class DeviceConfigurationParameter : DeviceAttributes
    {
        public uint Sequence { get; set; }
        public UIntValue Number { get; set; }
        public StringValue Name { get; set; }
        public StringValue Info { get; set; }
        public StringValue Format { get; set; }
        public StringValue MinValue { get; set; }
        public StringValue MaxValue { get; set; }
        public StringValue DefaultValue { get; set; }
        public StringValue Size { get; set; }
        public BoolValue Readonly { get; set; }
        public BoolValue Advanced { get; set; }
        public BoolValue AlteringCapabilities { get; set; }

        public void WriteToConsole()
        {
            Console.WriteLine("Seq: " + Sequence);
            Console.WriteLine(" - Number: " + Number.Value);
            Console.WriteLine(" - Name:   " + Name.Value);
            Console.WriteLine(" - Info:   " + Info.Value);
            Console.WriteLine(" - Format: " + Format.Value);
            Console.WriteLine(" - MinValue:     " + MinValue.Value);
            Console.WriteLine(" - MaxValue:     " + MaxValue.Value);
            Console.WriteLine(" - DefaultValue: " + DefaultValue.Value);
            Console.WriteLine(" - Size:     " + Size.Value);
            Console.WriteLine(" - Readonly: " + Readonly.Value.ToString());
            Console.WriteLine(" - Advanced: " + Advanced.Value.ToString());
            Console.WriteLine(" - AlteringCapabilities: " + AlteringCapabilities.Value.ToString());
        }
    }
}
