// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;

namespace Zats.Services
{
    public class EmptyBaseAttribute : Attribute
    { }

    public class WithNumberBaseAttribute : EmptyBaseAttribute
    {
        public ushort Number { get; set; }

        public WithNumberBaseAttribute(ushort number)
        {
            Number = number;
        }
    }

    public class WithPercentBaseAttribute : EmptyBaseAttribute
    {
        public WithPercentBaseAttribute()
        {
        }
    }
}
