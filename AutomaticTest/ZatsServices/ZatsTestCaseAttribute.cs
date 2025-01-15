// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;

namespace Zats.Services
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ZatsTestCaseAttribute : Attribute
    {
        public ZatsTestCaseAttribute(string description, params Tags[] tags)
        {
            Description = description;
            Tags = tags;
        }
        public string Description { get; set; }
        public Tags[] Tags { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class ZatsTestCaseOptionsAttribute : Attribute
    {
        public ZatsTestCaseOptionsAttribute(ZWaveSet parameter, bool isEnabled)
        {
            Parameter = parameter;
            IsEnabled = isEnabled;
        }

        public ZatsTestCaseOptionsAttribute(ZWaveSet parameter, bool isEnabled, Alias[] numbers)
        {
            Parameter = parameter;
            IsEnabled = isEnabled;
            Numbers = numbers;
        }

        public ZWaveSet Parameter { get; set; }
        public bool IsEnabled { get; set; }
        public Alias[] Numbers { get; set; }
    }

    public enum ZWaveSet
    {
        SmartStart,
        NodeIdBase2
    }
}
