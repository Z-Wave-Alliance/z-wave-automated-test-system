// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;

namespace Zats.Services
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class TestInterfaceDeviceMappingAttribute : DeviceMappingAttribute
    {
        public TestInterfaceDeviceMappingAttribute(ushort number, ChipSeries chipSeries, DevKits devKit, LibTypes library)
            : base(number, chipSeries, devKit, library)
        {
            IsTestInterface = true;
        }

        public TestInterfaceDeviceMappingAttribute(ushort number, ChipSeries chipSeries, LibTypes library)
            : this(number, chipSeries, DevKits.Default, library) { }

        public TestInterfaceDeviceMappingAttribute(ushort number, DevKits devKit, LibTypes library)
           : this(number, ChipSeries.Default, devKit, library) { }

        public TestInterfaceDeviceMappingAttribute(ushort number, LibTypes library)
            : this(number, ChipSeries.Default, DevKits.Default, library) { }

        public TestInterfaceDeviceMappingAttribute(ushort number, string alias, LibTypes library)
              : this(number, ChipSeries.Default, DevKits.Default, library)
        {
            Alias = alias;
        }

        public TestInterfaceDeviceMappingAttribute(ushort number, string alias, bool isEmulated, LibTypes library)
            : this(number, ChipSeries.Default, DevKits.Default, library)
        {
            IsEmulated = isEmulated;
            Alias = alias;
        }

        public TestInterfaceDeviceMappingAttribute(ushort number, string alias)
              : this(number, ChipSeries.Default, DevKits.Default, LibTypes.Sample)
        {
            Alias = alias;
        }
    }
}
