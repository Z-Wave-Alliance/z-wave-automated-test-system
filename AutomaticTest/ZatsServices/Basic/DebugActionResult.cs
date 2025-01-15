// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZWave.TextApplication.Operations;

namespace Zats.Services
{
    public static class DebugActionResult
    {
        public class Request : DebugResult.Request
        {
            public Request(RequestResult value)
            {
                Init(value.IsStateCompleted, value.TraceLog);
                ReceivedText = value.ReceivedText;
                ReceivedData = value.ReceivedData;
            }
        }
    }
    public static class TextActionResult
    {
        public class Request : TextResult.Request
        {
            public Request(RequestResult value)
            {
                Init(value.IsStateCompleted, value.TraceLog);
                ReceivedText = value.ReceivedText;
                ReceivedData = value.ReceivedData;
                ReceivedValue = value.ReceivedValue;
        }
        }
    }
}
