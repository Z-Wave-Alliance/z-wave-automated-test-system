// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
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
