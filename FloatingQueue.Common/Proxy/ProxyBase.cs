using System;
using System.ServiceModel;

namespace FloatingQueue.Common.Proxy
{
    public abstract class ProxyBase<T> :  IDisposable 
        where T : class
    {
        private T m_Client;

        protected ProxyBase(string address)
        {
            if(String.IsNullOrEmpty(address))
            {
                throw new ArgumentNullException(address);
            }

            EndpointAddress = new EndpointAddress(address);
        }

        public EndpointAddress EndpointAddress { get; private set; }

        protected T Client
        {
            get { return m_Client ?? (m_Client = CreateClient()); }
        }

        private T CreateClient()
        {
            return CommunicationProvider.Instance.CreateChannel<T>(EndpointAddress);
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

        public void Dispose()
        {
            CloseClient();
        }

        protected void OpenClient()
        {
            Channel.Open();
        }

        protected void CloseClient()
        {
            if (m_Client != null)
            {
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
}
