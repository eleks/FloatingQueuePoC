using System;

namespace FloatingQueue.Server.Exceptions
{
    public class BadConfigurationException : ApplicationException
    {
        public BadConfigurationException(string message) : base(message) {}
    }
}
