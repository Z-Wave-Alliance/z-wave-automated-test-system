// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Zats.Services
{
    public partial class TestCaseStaticController
    {
        [XmlIgnore]
        public bool ConnectedToZats { get; set; }
    
        public bool GetConnectionStateUi()
        {
            return ConnectedToZats || IsConnected;
        }
    }
}
