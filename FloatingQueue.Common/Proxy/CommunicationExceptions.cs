﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FloatingQueue.Common.Proxy
{
    public abstract class FloatingQueueExceptionBase : Exception
    {
        protected FloatingQueueExceptionBase(string message)
            : base(message)
        {
        }
    }

    public abstract class ServerErrorExceptionBase : FloatingQueueExceptionBase
    {
        protected ServerErrorExceptionBase(string message) : base(message)
        {
        }
    }

    public class ServerInternalException : ServerErrorExceptionBase
    {
        public const int CODE = 500;

        public ServerInternalException(string message)
            : base(message)
        {
        }
    }

    public class ServerInvalidArgumentException : ServerErrorExceptionBase
    {
        public const int CODE = 501;

        public ServerInvalidArgumentException(string message)
            : base(message)
        {
        }
    }

    public class ServerStreamAlreadyChangedException : ServerErrorExceptionBase
    {
        public const int CODE = 400;

        public ServerStreamAlreadyChangedException(string message)
            : base(message)
        {
        }
    }


    public abstract class CommunicationErrorExceptionBase : FloatingQueueExceptionBase
    {
        protected CommunicationErrorExceptionBase(string message) : base(message)
        {
        }
    }

    public class ServerUnavailableException : CommunicationErrorExceptionBase
    {
        public ServerUnavailableException(string message) : base(message)
        {
        }
    }

    public class ServerIsReadonlyException : CommunicationErrorExceptionBase
    {
        public ServerIsReadonlyException(string message)
            : base(message)
        {
        }
    }

    public class InvalidProtocolException : CommunicationErrorExceptionBase
    {
        public const int CODE = 502;

        public InvalidProtocolException(string message) : base(message)
        {
        }
    }
}
