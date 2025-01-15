// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;

namespace ZatsTestConsole
{
    class Program
    {
        static int Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");

            int exitCode = new TestRunner().RunCodeTest();
            if (!TestRunner._isQuit)
            {
                try
                {
                    ConsoleKeyInfo key = Console.ReadKey();
                    while (key.Key != ConsoleKey.Escape)
                    {
                        key = Console.ReadKey();
                    }
                }
                catch
                { }
            }
            Console.WriteLine($"Console Exit.");
            return exitCode;
        }
    }
}
