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
            output = string.Empty;
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
                try
                {
                    process.Kill();
                    // Ensure process exits after kill, but do not block indefinitely
                    if (!process.WaitForExit(5000))
                    {
                        LogProgrammerAction(false, $"{message} args:{args} - Process failed to exit after kill.");
                    }
                }
                catch (InvalidOperationException)
                {
                    LogProgrammerAction(false, $"{message} args:{args} - Process termination issue.");
                }
            }
            
            try
            {
                output = tmp.Result;
            }
            catch (AggregateException)
            {
                LogProgrammerAction(false, $"{message} args:{args} - Read output result");
            }

            int ret = process.HasExited ? process.ExitCode : -1;
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
