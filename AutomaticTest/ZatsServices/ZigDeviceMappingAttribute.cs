using System;

namespace Zats.Services
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class ZigDeviceMappingAttribute : WithNumberBaseAttribute
    {
        public string Alias { get; set; }
        public ZigDeviceMappingAttribute(ushort number, string alias) : base(number)
        {
            Alias = alias;
        }
    }
}
