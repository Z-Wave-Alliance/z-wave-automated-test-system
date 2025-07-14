// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using ZWave.TextApplication.Operations;

namespace Zats.Services
{
    public class DebugResult
    {
        public class Request : _ActionResult
        {
            public string ReceivedText { get; protected set; }
            public byte[] ReceivedData { get; protected set; }
        }

        public class Receive : _ActionResult
        {
            public string ReceivedText { get; set; }
            public byte[] ReceivedData { get; protected set; }
        }

        public class Void : _ActionResult
        {

        }
    }

    public class TextResult
    {
        public class Request : _ActionResult
        {
            public string ReceivedText { get; protected set; }
            public byte[] ReceivedData { get; protected set; }
            public float ReceivedValue { get; protected set; }
        }

        public class Receive : _ActionResult
        {
            public string ReceivedText { get; set; }
            public byte[] ReceivedData { get; protected set; }
        }

        public class Void : _ActionResult
        {

        }
    }
}
