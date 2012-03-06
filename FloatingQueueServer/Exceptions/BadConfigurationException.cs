using System;

namespace FloatingQueueServer
{
    public class BadConfigurationException : ApplicationException
    {
        public BadConfigurationException(string message) : base(message) {}
    }
}
