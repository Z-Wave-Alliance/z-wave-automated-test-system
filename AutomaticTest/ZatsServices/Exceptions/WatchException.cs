using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zats.Services
{
    public class WatchException : ServiceException
    {
        protected WatchException(string message)
            : base(message)
        { }
    }
}
