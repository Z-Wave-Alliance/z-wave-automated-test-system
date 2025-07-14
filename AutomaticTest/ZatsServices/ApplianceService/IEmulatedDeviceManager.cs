// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System.Collections.Generic;

namespace Zats.Services
{
    public interface IEmulatedDeviceManager
    {
        bool ConnectDevices(Alias[] devicesNumbers);
        void FinalizeDevices();
        void InitDevices(List<UnknownDevice> required);
        void ResetConfiguredState();
        void SendRequest(Alias deviceAlias, string method, params object[] methodParams);
    }
}