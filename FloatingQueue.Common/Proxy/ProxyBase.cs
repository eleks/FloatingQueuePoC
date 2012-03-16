using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace FloatingQueue.Common.Proxy
{
    public abstract class ProxyBase<T> :  IDisposable
        where T : class
    {
        protected EndpointAddress EndpointAddress;

        private T m_Client;
        protected T Client
        {
            get { return m_Client ?? (m_Client = CreateClient()); }
        }

        private ICommunicationObject Channel
        {
            get
            {
                var channel = Client as ICommunicationObject;
                if (channel == null)
                    throw new ApplicationException("Client must implement ICommunicationObject interface");
                return channel;
            }
        }

        protected virtual T CreateClient()
        {
            return CommunicationProvider.Instance.CreateChannel<T>(EndpointAddress);
        }

        public void Dispose()
        {
            DoClose();
        }

        protected void DoOpen()
        {
            Channel.Open();
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
