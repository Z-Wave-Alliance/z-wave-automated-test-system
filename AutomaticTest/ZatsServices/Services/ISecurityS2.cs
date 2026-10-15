// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
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
