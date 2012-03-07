using System;
using System.Collections.Generic;
using System.ServiceModel;
using FloatingQueue.ServiceProxy.GeneratedClient;

namespace FloatingQueue.ServiceProxy
{
    public abstract class QueueServiceProxy : IDisposable
    {
        private QueueServiceClient m_Client;
        protected QueueServiceClient Client
        {
            get { return m_Client ?? (m_Client = CreateClientCore()); }
        }

        public void Dispose()
        {
            DoClose();
        }

        public abstract void Push(string aggregateId, int version, object e);

        public abstract bool TryGetNext(string aggregateId, int version, out object next);

        public abstract IEnumerable<object> GetAllNext(string aggregateId, int version);

        protected void DoClose()
        {
            if (m_Client != null)
            {
                if (m_Client.State == CommunicationState.Faulted)
                    m_Client.Abort();
                else
                    m_Client.Close();
                m_Client = null;
            }
        }

        protected void CreateClient()
        {
            m_Client = CreateClientCore();
        }

        protected virtual QueueServiceClient CreateClientCore()
        {
            return new QueueServiceClient();
        }

    }
}
