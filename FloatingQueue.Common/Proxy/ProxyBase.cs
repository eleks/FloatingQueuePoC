using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

namespace FloatingQueue.Common.Proxy
{
    public abstract class ProxyBase<T> :  IDisposable
        where T : class
    {
        protected EndpointAddress EndpointAddress;
        private readonly Binding m_Binding = new NetTcpBinding();

        private T m_Client;
        protected T Client
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

        protected virtual T CreateClient()
        {
            return ChannelFactory<T>.CreateChannel(m_Binding, EndpointAddress);
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
