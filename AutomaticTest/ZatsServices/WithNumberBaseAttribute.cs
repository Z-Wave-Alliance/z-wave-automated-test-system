using System;

namespace Zats.Services
{
    public class EmptyBaseAttribute : Attribute
    { }

    public class WithNumberBaseAttribute : EmptyBaseAttribute
    {
        public ushort Number { get; set; }

        public WithNumberBaseAttribute(ushort number)
        {
            Number = number;
        }
    }

    public class WithPercentBaseAttribute : EmptyBaseAttribute
    {
        public WithPercentBaseAttribute()
        {
        }
    }
}
