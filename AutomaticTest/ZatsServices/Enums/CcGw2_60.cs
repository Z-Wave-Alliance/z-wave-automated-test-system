using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZWave.Enums
{
    public struct CmdClass
    {
        public byte CcId;
        public byte Version;
        public bool SecureOnly;
        public bool LanNif;
        public bool PanGwNif;
        public bool AppendedPanNodeNif;
    }
}
