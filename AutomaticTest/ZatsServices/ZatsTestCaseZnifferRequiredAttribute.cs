// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;

namespace Zats.Services
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ZatsTestCaseZnifferRequiredAttribute : Attribute
    {
        public ZatsTestCaseZnifferRequiredAttribute() { }
    }
}
