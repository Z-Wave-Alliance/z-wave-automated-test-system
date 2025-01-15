// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using ZWave;
using ZWave.TextApplication.Operations;

namespace Zats.Services
{
    public class DebugToken
    {
        public class Receive : _ActionToken
        {
            public Receive(ActionToken token)
                : base(token)
            { }

            public DebugResult.Receive WaitCompletedSignal()
            {
                var res = (ReceiveResult)Token.WaitCompletedSignal();
                return new DebugResult.Receive { ReceivedText = res.ReceivedText };
            }
        }

        public class Void : _ActionToken
        {
            public Void(ActionToken token)
                : base(token)
            { }

            public DebugResult.Void WaitCompletedSignal()
            {
                var res = (ReceiveResult)Token.WaitCompletedSignal();
                return new DebugResult.Void { };
            }
        }
    }
}
