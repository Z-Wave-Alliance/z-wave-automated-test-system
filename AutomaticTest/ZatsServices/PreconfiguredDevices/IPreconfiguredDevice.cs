// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;

namespace Zats.Services.PreconfiguredDevices
{
    public interface IPreconfiguredDevice
    {
        List<byte> StandardCc { get; set; }
        List<byte> SecuredCc { get; set; }
        List<byte> SpecificCc { get; set; }
        List<byte> FullCc { get; set; }
        LibTypes Library { get; set; }
        GenericTypes GenericType { get; set; }
        byte SpecificType { get; set; }
        ZWave.Enums.DeviceOptions DeviceOptions { get; set; }
        List<CommandClassSupport> SupportedModifyCommandClasses { get; set; }
    }
}
