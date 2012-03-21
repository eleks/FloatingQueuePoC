using System;
using FloatingQueue.Common.Common;
using FloatingQueue.Server.Core;

namespace FloatingQueue.Tests.Server
{
    public class TestLogger : ILogger
    {
        public void Debug(string message, Exception exception)
        {
            Console.WriteLine("D: " + message);
            Console.WriteLine(exception);
        }

        public void Debug(string format, params object[] args)
        {
            Console.WriteLine("D: " + format, args);
        }

        public void Info(string message, Exception exception)
        {
            Console.WriteLine("I: " + message);
            Console.WriteLine(exception);
        }

        public void Info(string format, params object[] args)
        {
            Console.WriteLine("I: " + format, args);
        }

        public void Warn(string message, Exception exception)
        {
            Console.WriteLine("W: " + message);
            Console.WriteLine(exception);
        }

        public void Warn(string format, params object[] args)
        {
            Console.WriteLine("W: " + format, args);
        }

        public void Error(string message, Exception exception)
        {
            Console.WriteLine("E: " + message);
            Console.WriteLine(exception);
        }

        public void Error(string format, params object[] args)
        {
            Console.WriteLine("E: " + format, args);
        }

        public void Fatal(string message, Exception exception)
        {
            Console.WriteLine("F: " + message);
            Console.WriteLine(exception);
        }

        public void Fatal(string format, params object[] args)
        {
            Console.WriteLine("F: " + format, args);
        }
    }
}
