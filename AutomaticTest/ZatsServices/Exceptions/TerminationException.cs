namespace Zats.Services
{
    public class TerminationException : ServiceException
    {
        public bool IsHidden { get; set; }
        public TestCaseResult Result { get; internal set; }

        protected TerminationException(TestCaseResult result, string message)
            : this(result, message, false)
        { }

        protected TerminationException(TestCaseResult result, string message, bool isHidden)
            : base(message)
        {
            Result = result;
            IsHidden = isHidden;
        }

        public static void Throw(TestCaseResult result, string exceptionMessage, params object[] args)
        {
            string message = ServiceBase.GetTestCaseCallInfo() + " " + exceptionMessage;
            if (args != null)
                message = string.Format(ServiceBase.GetTestCaseCallInfo() + " " + exceptionMessage, args);
            throw new TerminationException(result, message);
        }
    }
}
