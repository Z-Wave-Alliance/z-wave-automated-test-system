using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zats.Services
{
    public interface ISecurityS2
    {
        BasicResult.BootstrapS2 Bootstrap(Alias controller, Node nodeId, byte[] dsk);
    }
}
