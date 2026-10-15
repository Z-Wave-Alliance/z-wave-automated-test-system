// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using ZWave.Programmer.Controllers;

namespace ZWave.Programmer
{
    class Program
    {
        static int Main(string[] args)
        {
            ControllerManager.Instance.Start(args);
            return ControllerManager.ApplicationExitCode;
        }
    }
}
