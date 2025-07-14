// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zats.Services
{
    public interface IManualService
    {
        BasicResult.AddNode AddNode(Alias controllerNumber, int timeoutMs);
        BasicResult.RemoveNode RemoveNode(Alias controllerNumber, int timeoutMs);
    }
}
