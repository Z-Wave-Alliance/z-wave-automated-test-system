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

            // Skip this wait when stdin is redirected (e.g. spawned by CTT with piped
            // stdio): no Escape keystroke will ever arrive over the pipe, so waiting
            // here would block the process from exiting indefinitely.
            if (!TestRunner._isQuit && !Console.IsInputRedirected)
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
