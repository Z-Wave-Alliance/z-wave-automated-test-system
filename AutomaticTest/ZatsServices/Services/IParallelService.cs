using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zats.Services
{
    public interface IParallelService
    {
        void GetVersion(params Alias[] deviceNumbers);
        void MemoryGetId(params Alias[] deviceNumbers);
        void Reset(params Alias[] deviceNumbers);
        void SerialApiGetCapabilities(params Alias[] deviceNumbers);
        void SerialApiGetInitData(Alias[] deviceNumbers);
        void SetDefault(params Alias[] deviceNumbers);
        void SetDefault(IEnumerable<Alias> deviceNumbers);
        void SetRFReceiveMode(bool mode, params Alias[] deviceNumbers);
        void SoftReset(params Alias[] deviceNumbers);
    }
}
