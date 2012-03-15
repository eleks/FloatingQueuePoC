using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FloatingQueue.Server.Exceptions
{
    public class BusinessLogicException : Exception
    {
        public BusinessLogicException(string message) : base(message) {}
    }
}
