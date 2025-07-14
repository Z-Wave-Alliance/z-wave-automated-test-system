// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services
{
    public interface IUserInteractive
    {
        IMessageBox MessageBox { get; set; }
        IManualService ManualService { get; set; }
    }
}
