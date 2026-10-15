// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
// SPDX-FileCopyrightText: Z-Wave Alliance <https://z-wavealliance.org>

//#define WAIT_FOR_DEBUGGER

using System;
using System.Diagnostics;
using System.Threading;
using Zats.Services;

namespace ZatsTestConsole
{
    class Program
    {
        private static bool _useStartStopTags;

        static int Main(string[] args)
        {
            _useStartStopTags = ZatsUtils.ParseArguments(Environment.GetCommandLineArgs()).UseStartStopTags;

            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");

#if WAIT_FOR_DEBUGGER
            Console.WriteLine();
            if (_useStartStopTags)
            {
                Console.WriteLine(ConsoleCustomOutput.ZATS_START_TAG);
            }
            Console.WriteLine("Warning: Waiting for debugger to attach ...");
            if (_useStartStopTags)
            {
                Console.WriteLine(ConsoleCustomOutput.ZATS_END_TAG);
            }
            while (!Debugger.IsAttached)
            {
                Thread.Sleep(100);
            }
            if (_useStartStopTags)
            {
                Console.WriteLine(ConsoleCustomOutput.ZATS_START_TAG);
            }
            Console.WriteLine("Debugger attached");
            Console.WriteLine();
            if (_useStartStopTags)
            {
                Console.WriteLine(ConsoleCustomOutput.ZATS_END_TAG);
            }
#endif

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
            Console.WriteLine();
            if (_useStartStopTags)
            {
                Console.WriteLine(ConsoleCustomOutput.ZATS_START_TAG);
            }
            Console.WriteLine($"ZATS Test Console Exit.");
            if (_useStartStopTags)
            {
                Console.WriteLine(ConsoleCustomOutput.ZATS_END_TAG);
            }
            return exitCode;
        }
    }
}
