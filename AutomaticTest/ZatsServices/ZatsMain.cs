// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;

namespace Zats.Services
{
    public class ZatsMain
    {
        public List<Type> Tests { get; set; }
        public ZatsMain()
        {
            Tests = new List<Type>();
        }
    }
}
