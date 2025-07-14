// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Diagnostics;

namespace Zats.Services
{
    public class ProgrammingProviderBase
    {
        protected Action<bool, string> _logAction;
        protected string _appPath;
        public ProgrammingProviderBase(string appPath, Action<bool, string> logAction)
        {
            _logAction = logAction;
            _appPath = appPath;
        }

        private ProgrammingProviderBase()
        {
        }

        protected void LogProgrammerAction(int exitCode, string message)
        {
            LogProgrammerAction(exitCode == 0, message);
        }

        protected void LogProgrammerAction(bool isOk, string message)
        {
            _logAction(isOk, message);
        }

        protected int ProgrammerAction(string args, string message, int timeoutMs)
        {
            return ProgrammerAction(args, message, timeoutMs, out string output);
        }

        protected int ProgrammerAction(string args, string message, int timeoutMs, out string output)
        {
            var process = new Process();
            process.StartInfo.FileName = _appPath;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            var tmp = process.StandardOutput.ReadToEndAsync();
            var res = process.WaitForExit(timeoutMs);
            if (!res)
            {
                process.Kill();
            }
            output = tmp.Result;
            
            int ret = process.ExitCode;
            process.Close();
            LogProgrammerAction(ret, $"{message} args:{args}");
            if (ret != 0)
            {
                _logAction(false, output);
            }
            return ret;
        }
    }
}
