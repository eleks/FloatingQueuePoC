using System;
using System.Threading;

namespace FloatingQueue.Common.Common
{
    public enum ThreadState
    {
        NotStarted,
        Starting,
        Working,
        Stopped
    }

    public abstract class ThreadBase
    {
        private readonly Thread m_Thread;
        private ThreadState m_State;

        protected ThreadBase(string threadName = null)
        {
            m_Thread = new Thread(Run);

            if (threadName != null)
                m_Thread.Name = threadName;
        }

        public bool IsStopping { get; private set; }

        public bool IsAlive
        {
            get { return m_State != ThreadState.NotStarted && m_State != ThreadState.Stopped; }
        }

        protected Action<Exception> ThreadFailed { get; set; }

        public void Start(Action<Exception> threadFailedHandler)
        {
            if (threadFailedHandler != null)
                ThreadFailed += threadFailedHandler;

            StartCore();

            m_State = ThreadState.Starting;
            m_Thread.Start();
        }

        public void Stop()
        {
            IsStopping = true;
            Exception ex = null;
            try
            {
                StopCore();
            }
            catch (Exception e)
            {
                Logger.Instance.Debug("=== UNHANDLED THREAD STOPPING ERROR === \r\n=== {0} === ", e.Message);
                Logger.Instance.Debug("{0}\n{1}", e.Message, e.StackTrace);
                ex = e;
            }
            if (ex != null)
                OnThreadFailed(ex);
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
                RunCore();
            }
            catch (Exception e)
            {
                Logger.Instance.Debug("=== UNHANDLED THREAD RUNNING ERROR === \r\n=== {0} === ", e.Message);
                Logger.Instance.Debug("{0}\n{1}", e.Message, e.StackTrace);
                ex = e;
            }
            m_State = ThreadState.Stopped;

            if (ex != null)
                OnThreadFailed(ex);
        }

        protected void OnThreadFailed(Exception e)
        {
            var handler = ThreadFailed;
            if (handler != null)
            {
                try
                {
                    handler(e);
                }
                catch (Exception ex)
                {
                    Logger.Instance.Debug("=== THREAD FAILED HANDLER ERROR === \r\n=== {0} === ", e.Message);
                    Logger.Instance.Debug("{0}\n{1}", ex.Message, ex.StackTrace);
                }
            }
        }

        protected virtual void StartCore()
        {
        }

        protected abstract void RunCore();

        protected virtual void StopCore()
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
