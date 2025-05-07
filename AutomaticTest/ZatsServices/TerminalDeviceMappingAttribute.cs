using System;

namespace Zats.Services
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class TerminalDeviceMappingAttribute : DeviceMappingAttribute
    {
        public TerminalDeviceMappingAttribute(ushort number, ChipSeries chipSeries, DevKits devKit, LibTypes library)
            : base(number, chipSeries, devKit, library) { }

        public TerminalDeviceMappingAttribute(ushort number, ChipSeries chipSeries, LibTypes library)
            : this(number, chipSeries, DevKits.Default, library) { }

        public TerminalDeviceMappingAttribute(ushort number, DevKits devKit, LibTypes library)
           : this(number, ChipSeries.Default, devKit, library) { }

        public TerminalDeviceMappingAttribute(ushort number, LibTypes library)
            : this(number, ChipSeries.Default, DevKits.Default, library) { }

        public TerminalDeviceMappingAttribute(ushort number, string alias, LibTypes library)
              : this(number, ChipSeries.Default, DevKits.Default, library)
        {
            Alias = alias;
        }

        public TerminalDeviceMappingAttribute(ushort number, string alias, bool isEmulated, LibTypes library)
            : this(number, ChipSeries.Default, DevKits.Default, library)
        {
            IsEmulated = isEmulated;
            Alias = alias;
        }

        public TerminalDeviceMappingAttribute(ushort number, string alias)
              : this(number, ChipSeries.Default, DevKits.Default, LibTypes.Sample)
        {
            Alias = alias;
        }
    }
}
