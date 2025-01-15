// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;

namespace Zats.Services
{
    public abstract class DeviceAttributes
    {
        public interface Value
        {
            string Key { get; set; }
            List<string> ItemNumbers { get; set; }
            List<int> ItemIds { get; set; }
        }

        public class StringValue : Value
        {
            public string Key { get; set; }
            public string Value { get; set; }
            public List<string> ItemNumbers { get; set; }
            public List<int> ItemIds { get; set; }
        }

        public class BoolValue : Value
        {
            public string Key { get; set; }
            public bool Value { get; set; }
            public List<string> ItemNumbers { get; set; }
            public List<int> ItemIds { get; set; }
        }

        public class UIntValue : Value
        {
            public string Key { get; set; }
            public uint Value { get; set; }
            public List<string> ItemNumbers { get; set; }
            public List<int> ItemIds { get; set; }
        }
    }
}
