// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zats.Services.ZWaveClasses
{
    public class Dummy
    { }
}

namespace Zats.Services
{
    public partial class COMMAND_CLASS_NETWORK_MANAGEMENT_PROXY
    {
        public partial class NODE_INFO_CACHED_REPORT
        {
            private byte[] securityScheme0Mark = new byte[] { 0xF1, 0x00 };
            private byte controlMark = 0xEF;
            private IList<byte> _nonSecureCommandClass = new List<byte>();
            private IList<byte> _securityScheme0CommandClass = new List<byte>();
            public IList<byte> nonSecureCommandClass
            {
                get
                {
                    CalculateCommandClasses();
                    return _nonSecureCommandClass;
                }
            }

            public IList<byte> securityScheme0CommandClass
            {
                get
                {
                    CalculateCommandClasses();
                    return _securityScheme0CommandClass;
                }
            }

            private void CalculateCommandClasses()
            {
                _nonSecureCommandClass.Clear();
                _securityScheme0CommandClass.Clear();
                int index = 0;
                if (commandClass != null)
                {
                    var data = commandClass.ToArray();
                    while (data.Length - 0 > index && (data.Length - 2 < index || data[index + 0] != securityScheme0Mark[0] || data[index + 1] != securityScheme0Mark[1]))
                    {
                        var tmp = (data.Length > index) ? data[index++] : (byte)0x00;
                        if (tmp != controlMark)
                        {
                            _nonSecureCommandClass.Add(tmp);
                        }
                    }
                    index++; //Marker
                    index++; //Marker
                    while (data.Length - 0 > index)
                    {
                        var tmp = (data.Length > index) ? data[index++] : (byte)0x00;
                        if (tmp != controlMark)
                        {
                            _securityScheme0CommandClass.Add(tmp);
                        }
                    }
                }
            }
        }
    }
}
