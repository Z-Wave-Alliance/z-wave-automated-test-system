// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;

namespace Zats.Services
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class RailDeviceMappingAttribute : DeviceMappingAttribute
    {
        private RailDeviceMappingAttribute(ushort number, ChipSeries chipSeries, DevKits devKit, LibTypes library)
            : base(number, chipSeries, devKit, library) { }

        public RailDeviceMappingAttribute(ushort number, string alias)
              : this(number, ChipSeries.Default, DevKits.Default, LibTypes.RailTest)
        {
            Alias = alias;
            InterfaceType = InterfaceTypes.COM; 
        }
    }
}
