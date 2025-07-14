// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Zats.Services
{
    public class ServiceException : ApplicationException
    {
        public ServiceException(string message)
            : base(message)
        { }

        public ServiceException(string message, Exception innerException)
            : base(message, innerException)
        { }

        public static string GetMethodName(int showCallingPointShifter)
        {
            string methodName = "N/A";
            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
            if (st.FrameCount > (1 + showCallingPointShifter))
            {
                System.Diagnostics.StackFrame sf = st.GetFrame(1 + showCallingPointShifter);
                MethodBase mb = sf.GetMethod();
                methodName = String.Format("{0}.{1}", mb.DeclaringType.Name, mb.Name);
            }
            return methodName;
        }
    }
}
