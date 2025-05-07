using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zats.Services
{
    public abstract partial class ZatsCodeInteractiveTestCase : ZatsCodeTestCase, IUserInteractive
    {
        public IMessageBox MessageBox { get; set; }
        public IManualService ManualService { get; set; }
    }
}
