using System;

namespace Zats.Services
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class ZipDeviceMappingAttribute : WithNumberBaseAttribute
    {
        public string Alias { get; set; }
        public ZipDeviceMappingAttribute(ushort number, string alias) : base(number)
        {
            Alias = alias;
        }
    }
}
