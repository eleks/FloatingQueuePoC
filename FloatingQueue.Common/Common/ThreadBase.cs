using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FloatingQueue.Common.Common
{
    public static class DbgLogger
    {
        public static void WriteLine(string format, params object[] args)
        {
            Console.Out.WriteLine(format, args);
        }

        public static void LogException(Exception e)
        {
            while (e != null)
            {
                Console.Out.WriteLine("Exception: " + e.Message);
                Console.Out.WriteLine(e.StackTrace);
                e = e.InnerException;
            }
        }
    }


    public abstract class ThreadBase
    {
        public enum ThreadState { NotStarted, Starting, Working, Stopped };

        private readonly Thread m_Thread;
        private ThreadState m_State;

        protected ThreadBase(string threadName = null)
        {
            m_Thread = new Thread(Run);
            if (threadName != null)
                m_Thread.Name = threadName;
        }

        public void Start(Action<Exception> onThreadFailed)
        {
            if (onThreadFailed != null)
                OnThreadFailed += onThreadFailed;
            DoStart();
            m_State = ThreadState.Starting;
            m_Thread.Start();
        }

        public void Stop()
        {
            IsStopping = true;
            Exception ex = null;
            try
            {
                DoStop();
            }
            catch (Exception e)
            {
                DbgLogger.WriteLine("=== UNHANDLED THREAD STOPPING ERROR === \r\n=== {0} === ", e.Message);
                DbgLogger.LogException(e);
                ex = e;
            }
            if (ex != null)
                DoThreadFailed(ex);
        }

        public bool IsStopping { get; private set; }

        public bool IsAlive
        {
            get { return m_State != ThreadState.NotStarted && m_State != ThreadState.Stopped; }
        }

        public void Wait()
        {
            if (IsAlive)
                m_Thread.Join();
        }

        private void Run()
        {
            Exception ex = null;
            m_State = ThreadState.Working;
            try
            {
                DoRun();
            }
            catch (Exception e)
            {
                DbgLogger.WriteLine("=== UNHANDLED THREAD RUNNING ERROR === \r\n=== {0} === ", e.Message);
                DbgLogger.LogException(e);
                ex = e;
            }
            m_State = ThreadState.Stopped;
            if (ex != null)
                DoThreadFailed(ex);
        }

        protected Action<Exception> OnThreadFailed { get; set; }

        protected void DoThreadFailed(Exception e)
        {
            Action<Exception> onThreadFailed = OnThreadFailed;
            if (onThreadFailed == null)
                return;

            try
            {
                onThreadFailed(e);
            }
            catch (Exception ex)
            {
                DbgLogger.WriteLine("=== THREAD FAILED HANDLER ERROR === \r\n=== {0} === ", e.Message);
                DbgLogger.LogException(ex);
            }
        }

        protected virtual void DoStart()
        {
        }

        protected abstract void DoRun();

        protected virtual void DoStop()
        {
        }

        protected void Sleep(TimeSpan ts)
        {
            var ms = ts.TotalMilliseconds;
            Sleep(ms < int.MaxValue ? (int)ms : int.MaxValue);
        }

        protected void Sleep(int mSec)
        {
            const int delta = 1000;
            while (!IsStopping && mSec > 0)
            {
                int sleep = delta;
                mSec -= delta;
                if (mSec < 0)
                {
                    sleep = mSec + delta;
                    mSec = 0;
                }
                Thread.Sleep(sleep);
            }
        }
    }
}
