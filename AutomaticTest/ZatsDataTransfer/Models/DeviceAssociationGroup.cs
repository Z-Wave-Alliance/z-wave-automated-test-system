// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;

namespace Zats.Services
{
    public class DeviceAssociationGroup : DeviceAttributes
    {
        public uint Sequence { get; set; }
        public StringValue Name { get; set; }
        public StringValue Profile { get; set; }
        public bool HasMultiChannelEndpoint { get; set; }
        public UIntValue MultiChannelEndpointId { get; set; }
        public UIntValue GroupingId { get; set; }
        public UIntValue MaxNodes { get; set; }

        public StringValue CommandClassesAndCommand { get; set; }

        public void WriteToConsole()
        {
            Console.WriteLine("Seq: " + Sequence);
            Console.WriteLine(" - Name:   " + Name.Value);
            Console.WriteLine(" - GroupingId:   " + GroupingId.Value);
            Console.WriteLine(" - MaxNodes:     " + MaxNodes.Value);
            Console.WriteLine(" - Profile:      " + Profile.Value);
            Console.WriteLine(" - CommandClassesAndCommand: " + CommandClassesAndCommand.Value);
            Console.WriteLine(" - HasMultiChannelEndpoint: " + HasMultiChannelEndpoint.ToString());
            if (HasMultiChannelEndpoint)
            {
                Console.WriteLine(" - MultiChannelEndpointId: " + MultiChannelEndpointId.Value);
            }
        }
    }
}
