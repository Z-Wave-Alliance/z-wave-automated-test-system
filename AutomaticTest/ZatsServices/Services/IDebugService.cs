// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;

namespace Zats.Services
{
    public interface IDebugService
    {
        //BasicResult.AddNode AddDUT(Alias Primary, Alias debugDeviceNumber, Modes mode);
        //ProductPlusAppIds InitDUT(Alias debugDeviceNumber);
        //BasicResult.RemoveNode RemoveDUT(Alias Primary, Alias debugDeviceNumber, Modes mode);
        DebugResult.Request Request(Alias deviceNumber, dCommand data, int timeoutMs);
        DebugResult.Request Request(Alias deviceNumber, string data, int timeoutMs);
        DebugResult.Request Request(Alias deviceNumber, dChannel channel, dCommand data, string parameter, int timeoutMs);
        DebugResult.Request Request(Alias deviceNumber, dChannel channel, dCommand data, int timeoutMs);
        //void Send(Alias deviceNumber, dChannel channel, dCommand data);
        void Send(Alias deviceNumber, string data);
        DebugToken.Receive Receive(Alias deviceNumber, int timeOut);
        void CancelToken(Alias deviceNumber, _ActionToken token);
        DebugToken.Void Listen(Alias deviceNumber, Action<string> listenCallback);
        ServiceSettings Settings { get; set; }
    }
}
