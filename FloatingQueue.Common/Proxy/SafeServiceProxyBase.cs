using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FloatingQueue.Common.Proxy
{
    public abstract class SafeServiceProxyBase<TProxy, TIntf>: IDisposable
        where TProxy : ProxyBase<TIntf>
        where TIntf : class
    {
        protected readonly TProxy Proxy;
        //
        private readonly bool m_KeepConnectionOpened;
        private readonly int m_MaxRetry;
        //
        private bool m_NoRetryMode;

        protected SafeServiceProxyBase(TProxy proxy, bool keepConnection, bool connectNow, int maxRetry)
        {
            Proxy = proxy;
            m_KeepConnectionOpened = keepConnection;
            m_MaxRetry = maxRetry;
            if (connectNow)
                ConnectOnInit();
        }

        private void ConnectOnInit()
        {
            ReConnect();
        }

        public void Dispose()
        {
            Proxy.CloseClient();
        }

        protected void SafeCall(Action action)
        {
            if (m_NoRetryMode)
            {
                SafeNetworkOperation(action); // do the operation
            }
            else
            {
                int retry = 0;
                while (retry < m_MaxRetry)
                {
                    try
                    {
                        if (retry > 0)
                        {
                            ReConnect();
                        }
                        SafeNetworkOperation(action); // try do operation
                        break; // no exceptions - success
                    }
                    catch (ConnectionErrorException)
                    {
                        if (retry == m_MaxRetry - 1)
                            throw;
                    }
                    retry++;
                }
            }
        }

        protected void SafeNetworkOperation(Action action)
        {
            try
            {
                CommunicationProvider.Instance.SafeNetworkCall(action);
            }
            finally
            {
                if (!m_KeepConnectionOpened)
                    Proxy.CloseClient();
            }
        }

        protected void ExecuteInNoRetryMode(Action action)
        {
            var prevNoRetryMode = m_NoRetryMode;
            m_NoRetryMode = true;
            try
            {
                action();
            }
            finally
            {
                m_NoRetryMode = prevNoRetryMode;
            }
        }

        protected abstract void ReConnect();
    }

}
