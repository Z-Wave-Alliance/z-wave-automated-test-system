using System;

namespace Zats.Services
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class MeshNetworkRouterMappingAttribute : WithNumberBaseAttribute
    {
        public string Alias { get; set; }

        public MeshNetworkRouterMappingAttribute(ushort number, string alias) : base(number)
        {
            Alias = alias;
        }
    }
}
