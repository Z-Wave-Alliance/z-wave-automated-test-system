// SPDX-License-Identifier: BSD-3-Clause
// SPDX-FileCopyrightText: Silicon Laboratories Inc. https://www.silabs.com
namespace Zats.Services
{
    public class AssertionException : ServiceException
    {
        public bool IsHidden { get; set; }
        protected AssertionException(string message)
            : this(message, false)
        { }

        protected AssertionException(string message, bool isHidden)
            : base(message)
        {
            IsHidden = isHidden;
        }

        public static void Throw()
        {
            string message = ServiceBase.GetTestCaseCallInfo();
            throw new AssertionException(message);
        }

        public static void Throw(string exceptionMessage, params object[] args)
        {
            string message = ServiceBase.GetTestCaseCallInfo() + " " + exceptionMessage;
            if (args != null)
                message = string.Format(ServiceBase.GetTestCaseCallInfo() + " " + exceptionMessage, args);
            throw new AssertionException(message);
        }

        internal static void Throw(string exceptionMessage)
        {
            string message = ServiceBase.GetTestCaseCallInfo() + " " + exceptionMessage;
            throw new AssertionException(message);
        }

        internal static void Throw(string exceptionMessage, bool isHidden)
        {
            string message = ServiceBase.GetTestCaseCallInfo() + " " + exceptionMessage;
            throw new AssertionException(message, isHidden);
        }
    }
}
