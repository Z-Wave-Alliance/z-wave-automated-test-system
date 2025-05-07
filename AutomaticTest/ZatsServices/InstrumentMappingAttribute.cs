using System;

namespace Zats.Services
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class InstrumentMappingAttribute : WithNumberBaseAttribute
    {
        public InstrumentTypes Type { get; set; }
        public string Alias { get; set; }

        public InstrumentMappingAttribute(ushort number, InstrumentTypes type) : base(number)
        {
            Type = type;
        }
    }
}
