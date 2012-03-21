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
            SetNewAddress(address);
        }

        public EndpointAddress EndpointAddress { get; private set; }
        public string Address { get; private set; }

        public void SetNewAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
                throw new ArgumentNullException("address");

            if (m_Client != null)
                throw new InvalidOperationException("Connection must be closed before address changing");
            EndpointAddress = new EndpointAddress(address);
            Address = address;
        }

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

        public void CloseClient()
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
