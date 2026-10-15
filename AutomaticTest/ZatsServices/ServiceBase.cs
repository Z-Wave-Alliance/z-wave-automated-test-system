// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. <https://www.silabs.com>
using System.Diagnostics;
using Utils.UI.Enums;

namespace Zats.Services
{
    public abstract class ServiceBase
    {
        protected abstract void Log(string text, LogLevels logLevel);

        public static string GetTestCaseCallInfo()
        {
            string ret = null;
            StackTrace st = new StackTrace(true);
            StackFrame sfTC = null;
            StackFrame sfMTC = null;
            for (int i = 4; i < st.FrameCount; i++)
            {
                StackFrame sf = st.GetFrame(i);
                string fileName = sf.GetFileName();
                if (fileName != null && fileName.Contains("ZatsCodeTestCase"))
                {
                    sfTC = st.GetFrame(i - 1);
                    sfMTC = st.GetFrame(i - 2);
                    break;
                }
            }
            if (sfTC != null)
            {
                ret = string.Format("{0} [Ln {1}]",
                    sfMTC.GetMethod().Name,
                    //System.IO.Path.GetFileNameWithoutExtension(sfTC.GetFileName()),
                    sfTC.GetFileLineNumber());
            }
            return ret;
        }
    }

    public enum AssertStates
    {
        None,
        Completed,
        Failed,
        Cancelled
    }
}
