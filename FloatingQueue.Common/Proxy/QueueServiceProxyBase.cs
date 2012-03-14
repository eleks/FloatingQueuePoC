using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace FloatingQueue.Common.Proxy
{
    public abstract class QueueServiceProxyBase : IQueueService, IDisposable
    {
        protected EndpointAddress EndpointAddress;
        private readonly Binding m_Binding = new NetTcpBinding();

        private IQueueService m_Client;
        protected IQueueService Client
        {
            get { return m_Client ?? (m_Client = CreateClient()); }
        }

        protected ICommunicationObject Channel
        {
            get
            {
                var channel = Client as ICommunicationObject;
                if (channel == null)
                    throw new ApplicationException("Client must implement ICommunicationObject interface");
                return channel;
            }
        }

        protected virtual IQueueService CreateClient()
        {
            return ChannelFactory<IQueueService>.CreateChannel(m_Binding, EndpointAddress);
        }

        public virtual void Push(string aggregateId, int version, object e)
        {
            Client.Push(aggregateId, version, e);
        }

        public virtual bool TryGetNext(string aggregateId, int version, out object next)
        {
            return Client.TryGetNext(aggregateId, version, out next);
        }

        public virtual IEnumerable<object> GetAllNext(string aggregateId, int version)
        {
            return Client.GetAllNext(aggregateId, version);
        }

        public virtual PingResult Ping(PingParams pingParams)
        {
            // todo create enumeration for fault reasons
            try
            {
                return Client.Ping(pingParams);
            }
            catch (CommunicationException)
            {
                return new PingResult() { ErrorCode = 1 };
            }
            catch (TimeoutException)
            {
                return new PingResult() { ErrorCode = 2 };
            }
        }

        public void Dispose()
        {
            DoClose();
        }

        protected void DoClose()
        {
            if (m_Client == null)
                return;

            bool abort = false;
            try
            {
                if (Channel.State == CommunicationState.Faulted)
                    abort = true;
                else
                    Channel.Close();
            }
            catch (CommunicationException)
            {
                abort = true;
            }
            catch (TimeoutException)
            {
                abort = true;
            }
            if (abort)
                Channel.Abort();
            m_Client = null;
        }
    }
}
