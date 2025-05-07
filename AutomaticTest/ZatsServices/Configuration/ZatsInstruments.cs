using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zats.Services
{
    public class ZatsInstruments
    {
        public ZatsInstrument[] Instruments { get; set; }
    }

    public class ZatsInstrument
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public InstrumentTypes Type { get; set; }
        public string Alias { get; set; }
        public string SName { get; set; }
        public string SType { get; set; }
    }
}
