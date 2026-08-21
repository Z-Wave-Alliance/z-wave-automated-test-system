// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zats.Services;

namespace ZatsTestConsole
{
    class Program 
    {
        static int Main(string[] args)
        {
            int exitCode = new TestRunner().RunCodeTest();
            if (!TestRunner._isQuit)
            {
                try
                {
                    StdinDispatcher.Shared.WaitForCharAsync(c => c == (char)ConsoleKey.Escape).GetAwaiter().GetResult();
                }
                catch
                { }
            }
            return exitCode;
        }
    }
}
