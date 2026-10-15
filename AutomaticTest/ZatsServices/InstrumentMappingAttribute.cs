// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;

namespace Zats.Services
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class InstrumentMappingAttribute : WithNumberBaseAttribute
    {
        public InstrumentTypes Type { get; set; }
        public string Alias { get; set; }

        public InstrumentMappingAttribute(ushort number, InstrumentTypes type) : base(number)
        {
            Type = type;
        }
    }
}
