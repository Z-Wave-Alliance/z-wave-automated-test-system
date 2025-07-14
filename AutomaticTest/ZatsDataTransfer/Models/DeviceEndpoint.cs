// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;

namespace Zats.Services
{
    public class DeviceEndpoint : DeviceAttributes
    {
        public uint Sequence { get; set; }
        public UIntValue Id { get; set; }
        public ZcpLink IdLink { get; set; }
        public StringValue Type { get; set; }
        public ZcpLink TypeLink { get; set; }
        public StringValue DeviceType { get; set; }
        public ZcpLink DeviceTypeLink { get; set; }
        public Dictionary<StringValue, ZcpLink> OptionalCommandClasses { get; set; }

        public void WriteToConsole()
        {
            Console.WriteLine("Seq: " + Sequence);
            Console.WriteLine(" - Id:    " + Id.Value);
            Console.WriteLine(" - Type:  " + Type.Value);
            Console.WriteLine(" - DType: " + DeviceType.Value);
            //foreach (StringValue cc in OptionalCommandClasses)
            //{
            //    Console.WriteLine(" --- OCC: " + cc.Value);
            //}
        }
    }

    public class RootDevice : DeviceAttributes
    {
        public uint ZWavePlusVersion { get; set; }
        public StringValue DeviceType { get; set; }
        public ZcpLink DeviceTypeLink { get; set; }
        public List<StringValue> RoleTypes { get; set; }
        public ZcpLink RoleTypeLink { get; set; }
        public Dictionary<StringValue, ZcpLink> SecurityClasses { get; set; }
        public Dictionary<StringValue, ZcpLink> ControlledCommandClasses { get; set; }
        public Dictionary<StringValue, ZcpLink> SupportedCommandClasses { get; set; }

        public void WriteToConsole()
        {
            Console.WriteLine("Seq: 0");
            Console.WriteLine(" - Id:    Root");
            Console.WriteLine(" - Type:  Root");
            Console.WriteLine(" - DType: " + DeviceType.Value);
        }
    }
}
