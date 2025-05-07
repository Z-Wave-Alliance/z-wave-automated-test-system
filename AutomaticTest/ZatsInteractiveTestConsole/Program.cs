using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                    ConsoleKeyInfo key = Console.ReadKey();
                    while (key.Key != ConsoleKey.Escape)
                    {
                        key = Console.ReadKey();
                    }
                }
                catch
                { }
            }
            return exitCode;
        }
    }
}
