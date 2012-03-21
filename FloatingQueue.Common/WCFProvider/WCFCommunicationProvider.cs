using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using FloatingQueue.Common.TCP;

namespace FloatingQueue.Common.WCF
{
    public class WCFCommunicationProvider : ICommunicationProvider
    {
        private readonly Binding m_Binding = new NetTcpBinding();

        public T CreateChannel<T>(EndpointAddress endpointAddress)
        {
            return ChannelFactory<T>.CreateChannel(m_Binding, endpointAddress);
        }

        public CommunicationObjectBase CreateHost<T>(string displayName, string address)
        {
            var serviceType = typeof(T);
            var serviceUri = new Uri(address);

            var host = new ServiceHost(serviceType, serviceUri) {CloseTimeout = TimeSpan.FromMilliseconds(1000)};
            return new WCFCommunicationObjectWrapper(host);
        }

        public void OpenChannel<T>(T client)
        {
            var channel = GetCommunicationObject(client);
            channel.Open();
        }

        public void CloseChannel<T>(T client)
        {
            var channel = GetCommunicationObject(client);
            bool abort = false;
            try
            {
                if (channel.State == CommunicationState.Faulted)
                    abort = true;
                else
                    channel.Close();
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
                channel.Abort();
        }

        private static ICommunicationObject GetCommunicationObject<T>(T client)
        {
            var channel = client as ICommunicationObject;
            if (channel == null)
                throw new ApplicationException("Client must implement ICommunicationObject interface");
            return channel;
        }

        public void SafeNetworkCall(Action action)
        {
            try
            {
                action();
            }
            catch (CommunicationException e)
            {
                throw new ConnectionErrorException(e);
            }
            catch (TimeoutException e)
            {
                throw new ConnectionErrorException(e);
            }
        }

        private class WCFCommunicationObjectWrapper : CommunicationObjectBase
        {
            private readonly ICommunicationObject m_CommunicationObject;

            public WCFCommunicationObjectWrapper(ICommunicationObject communicationObject)
            {
                m_CommunicationObject = communicationObject;
            }

            public override void Open()
            {
                m_CommunicationObject.Open();
            }

            public override void Close()
            {
                m_CommunicationObject.Close();
            }
        }
    }
}
