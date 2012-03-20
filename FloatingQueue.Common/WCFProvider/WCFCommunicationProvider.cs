﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

namespace FloatingQueue.Common.WCF
{
    public class WCFCommunicationProvider : ICommunicationProvider
    {
        private readonly Binding m_Binding = new NetTcpBinding();

        public T CreateChannel<T>(EndpointAddress endpointAddress)
        {
            return ChannelFactory<T>.CreateChannel(m_Binding, endpointAddress);
        }

        public ICommunicationObject CreateHost<T>(string displayName, string address)
        {
            var serviceType = typeof(T);
            var serviceUri = new Uri(address);

            var host = new ServiceHost(serviceType, serviceUri) {CloseTimeout = TimeSpan.FromMilliseconds(1000)};
            return host;
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
    }
}
