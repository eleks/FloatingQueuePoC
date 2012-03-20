using System;
using log4net;
using log4net.Config;

namespace FloatingQueue.Common.Common
{
    public interface ILogger
    {
        void Debug(string message, Exception exception);
        void Debug(string format, params object[] args);

        void Info(string message, Exception exception);
        void Info(string format, params object[] args);

        void Warn(string message, Exception exception);
        void Warn(string format, params object[] args);

        void Error(string message, Exception exception);
        void Error(string format, params object[] args);

        void Fatal(string message, Exception exception);
        void Fatal(string format, params object[] args);
    }

    public class Logger : ILogger
    {
        private readonly ILog m_Log = LogManager.GetLogger(typeof(Logger));

        private static Logger ms_Instance;

        public static Logger Instance
        {
            get
            {
                return ms_Instance ?? (ms_Instance = new Logger());
            }
        }

        protected Logger()
        {
            XmlConfigurator.Configure();
        }

        public void Debug(string message, Exception exception)
        {
            m_Log.Debug(message, exception);
        }

        public void Debug(string format, params object[] args)
        {
            m_Log.DebugFormat(format, args);
        }

        public void Info(string message, Exception exception)
        {
            m_Log.Info(message, exception);
        }

        public void Info(string format, params object[] args)
        {
            m_Log.InfoFormat(format, args);
        }

        public void Warn(string message, Exception exception)
        {
            m_Log.Warn(message, exception);
        }

        public void Warn(string format, params object[] args)
        {
            m_Log.WarnFormat(format, args);
        }

        public void Error(string message, Exception exception)
        {
            m_Log.Error(message, exception);
        }

        public void Error(string format, params object[] args)
        {
            m_Log.ErrorFormat(format, args);
        }

        public void Fatal(string message, Exception exception)
        {
            m_Log.Fatal(message, exception);
        }

        public void Fatal(string format, params object[] args)
        {
            m_Log.FatalFormat(format, args);
        }
    }
}
