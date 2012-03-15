using System;
using System.Collections.Generic;
using System.Linq;
using FloatingQueue.Server.Exceptions;

namespace FloatingQueue.Server.EventsLogic
{
    public class CriticalException : Exception
    {
        public CriticalException() {}
        public CriticalException(string message) : base(message) {}
    }

}

