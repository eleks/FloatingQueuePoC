using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FloatingQueue.Server.Exceptions
{
    public class IncompatibleDataException : Exception
    {
        public IncompatibleDataException(string message) : base(message) {}
    }
}
