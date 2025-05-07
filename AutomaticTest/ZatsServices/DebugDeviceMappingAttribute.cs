using System;

namespace Zats.Services
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class DebugDeviceMappingAttribute : WithNumberBaseAttribute
    {
        public string Alias { get; set; }

        public DebugDeviceMappingAttribute(ushort number, string alias) : base(number)
        {
            Alias = alias;
        }
    }
}
