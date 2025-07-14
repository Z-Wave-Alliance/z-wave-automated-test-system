// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;

namespace Zats.Services
{
    public interface IZatsAutomationDialog : IDisposable
    {
        IZatsAutomation DialogView { get; }
    }
}
