using System;
using System.Runtime.Serialization;

namespace FloatingQueue.Server.Exceptions
{
    public class ServerInitializationException : Exception
    {
        public ServerInitializationException()
        {
        }

        public ServerInitializationException(string message) : base(message)
        {
        }

        public ServerInitializationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ServerInitializationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}