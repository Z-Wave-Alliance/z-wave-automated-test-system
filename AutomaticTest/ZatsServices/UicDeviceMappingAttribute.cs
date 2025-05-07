using System;

namespace Zats.Services
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class UicDeviceMappingAttribute : WithNumberBaseAttribute
    {
        public string Alias { get; set; }
        public UicDeviceMappingAttribute(ushort number, string alias) : base(number)
        {
            Alias = alias;
        }
    }
}
 