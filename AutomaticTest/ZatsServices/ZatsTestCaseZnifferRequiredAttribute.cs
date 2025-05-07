using System;

namespace Zats.Services
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ZatsTestCaseZnifferRequiredAttribute : Attribute
    {
        public ZatsTestCaseZnifferRequiredAttribute() { }
    }
}
