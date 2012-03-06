using System;
using System.Collections.Generic;
using System.ServiceModel;
using FloatingQueue.ServiceProxy.GeneratedClient;

namespace FloatingQueue.ServiceProxy
{
    public enum ConnectionMode { Auto, Manual };

    public class QueueServiceProxy : IDisposable
    {
        private QueueServiceClient m_Client = new QueueServiceClient();
        private readonly ConnectionMode m_ConnectionMode;
        private InvokeActionOverClientDelegate m_InvokeAction;

        public QueueServiceProxy(ConnectionMode connectionMode = ConnectionMode.Auto)
        {
            m_ConnectionMode = connectionMode;
            ChooseActionInvoker(m_ConnectionMode);
        }

        public ConnectionMode ConnectionMode
        {
            get { return m_ConnectionMode; }
        }

        public void Open()
        {
            EnsureOperationIsValid();
            m_Client.Open();
        }

        public void Close()
        {
            EnsureOperationIsValid();
            DoClose();
        }

        public void Dispose()
        {
            if (m_ConnectionMode == ConnectionMode.Manual)
                DoClose();
        }

        #region QueueService Wrapper Operations

        public void Push(string aggregateId, int version, object e)
        {
            m_InvokeAction(client => client.Push(aggregateId, version, e));
        }

        public bool TryGetNext(string aggregateId, int version, out object next)
        {
            // out parameters cannot be usedi inside anonymous methods
            // todo: try to come up without sticks
            switch (m_ConnectionMode)
            {
                case ConnectionMode.Auto:
                    return m_Client.TryGetNext(out next, aggregateId, version);
                case ConnectionMode.Manual:
                    try { return m_Client.TryGetNext(out next, aggregateId, version); }
                    finally { m_Client.Close(); }
            }
            next = null;
            return false;
        }

        public IEnumerable<object> GetAllNext(string aggregateId, int version)
        {
            IEnumerable<object> result = null;
            m_InvokeAction(client => result = client.GetAllNext(aggregateId, version));
            return result;
        }

        #endregion

        #region Private Methods

        private void EnsureOperationIsValid()
        {
            if (m_ConnectionMode == ConnectionMode.Auto)
                throw new InvalidOperationException("You cannot control the connection manually in Auto Mode");
        }

        private void DoClose()
        {
            if (m_Client != null)
            {
                if (m_Client.State == CommunicationState.Faulted)
                    m_Client.Abort();
                m_Client.Close();
                m_Client = new QueueServiceClient();
            }
        }

        private void ChooseActionInvoker(ConnectionMode connectionMode)
        {
            switch (connectionMode)
            {
                case ConnectionMode.Auto:
                    m_InvokeAction = SafeInvoke;
                    break;
                case ConnectionMode.Manual:
                    m_InvokeAction = UnsafeInvoke;
                    break;
                default:
                    throw new NotImplementedException("Only Auto and Manual mode are currently supported");
            }
        }

        private delegate void InvokeActionOverClientDelegate(Action<QueueServiceClient> operation);
        private void UnsafeInvoke(Action<QueueServiceClient> operation)
        {
            operation(m_Client);
        }
        private void SafeInvoke(Action<QueueServiceClient> operation)
        {
            try
            {
                operation(m_Client);
            }
            finally
            {
                DoClose();
            }
        }

        #endregion
    }


}
