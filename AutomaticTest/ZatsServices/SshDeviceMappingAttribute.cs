using System;

namespace Zats.Services
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class SshDeviceMappingAttribute : WithNumberBaseAttribute
    {
        public string Alias { get; set; }
        public SshDeviceMappingAttribute(ushort number, string alias) : base(number)
        {
            Alias = alias;
        }
    }
}
