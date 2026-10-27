// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System;
using Utils;

namespace Zats.Services
{
    public class ProgrammingProviderBase
    {
        protected Action<bool, string> _logAction;
        protected string _appPath;
        protected IExternalToolRunner _toolRunner;
        public ProgrammingProviderBase(string appPath, Action<bool, string> logAction)
            : this(appPath, logAction, new ExternalToolRunner())
        {
        }

        /// <summary>
        /// Creates a provider that runs the programmer tool through the given runner instead of the local
        /// process runner, e.g. a fake returning canned tool output in a unit test.
        /// </summary>
        public ProgrammingProviderBase(string appPath, Action<bool, string> logAction, IExternalToolRunner toolRunner)
        {
            _logAction = logAction;
            _appPath = appPath;
            _toolRunner = toolRunner;
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

        /// <summary>
        /// Runs the programmer tool and logs the outcome. Returns the tool exit code, or -1 when the tool could not be
        /// started or was killed after <paramref name="timeoutMs"/>, see <see cref="IExternalToolRunner.Run"/>.
        /// </summary>
        protected int ProgrammerAction(string args, string message, int timeoutMs, out string output)
        {
            int ret = _toolRunner.Run(_appPath, args, timeoutMs, out output);
            LogProgrammerAction(ret, $"{message} args:{args}");
            if (ret != 0)
            {
                _logAction(false, output);
            }
            return ret;
        }
    }
}
