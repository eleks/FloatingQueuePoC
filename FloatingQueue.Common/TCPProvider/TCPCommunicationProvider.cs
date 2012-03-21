using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.ServiceModel;
using System.Text;
using FloatingQueue.Common.TCP;

namespace FloatingQueue.Common.TCPProvider
{

    public class TCPCommunicationProvider : ICommunicationProvider
    {
        private readonly Dictionary<Type, Func<TCPClientBase>> m_ChannelMap = new Dictionary<Type, Func<TCPClientBase>>();
        private readonly Dictionary<Type, Func<TCPServerBase>> m_HostMap = new Dictionary<Type, Func<TCPServerBase>>();

        public T CreateChannel<T>(EndpointAddress endpointAddress)
        {
            Func<TCPClientBase> ctor;
            if (!m_ChannelMap.TryGetValue(typeof(T), out ctor))
                throw new Exception("Implementation unknown for type " + typeof(T).FullName);
            var res = ctor();
            res.Initialize(endpointAddress);
            return (T)(object) res;
        }

        public CommunicationObjectBase CreateHost<T>(string displayName, string address)
        {
            Func<TCPServerBase> ctor;
            if (!m_HostMap.TryGetValue(typeof(T), out ctor))
                throw new Exception("Implementation unknown for type " + typeof(T).FullName);
            var res = ctor();
            res.Initialize(displayName, address);
            return res;
        }

        public void OpenChannel<T>(T client)
        {
            var channel = GetCommunicationObject(client);
            channel.Open();
        }

        public void CloseChannel<T>(T client)
        {
            var channel = GetCommunicationObject(client);
            channel.Close();
        }

        private static CommunicationObjectBase GetCommunicationObject<T>(T client)
        {
            var channel = client as CommunicationObjectBase;
            if (channel == null)
                throw new ApplicationException("Client must implement TCPCommunicationObjectBase interface");
            return channel;
        }

        public void SafeNetworkCall(Action action)
        {
            try
            {
                action();
            }
            catch (IOException e)
            {
                throw new ConnectionErrorException(e);
            }
            catch (SocketException e)
            {
                throw new ConnectionErrorException(e);
            }
            catch (TimeoutException e)
            {
                throw new ConnectionErrorException(e);
            }
        }


        public void RegisterChannelImplementation<TIntf>(Func<TCPClientBase> ctor)
            where TIntf : class
        {
            m_ChannelMap.Add(typeof(TIntf), ctor);
        }

        public void RegisterHostImplementation<TIntf>(Func<TCPServerBase> ctor)
            where TIntf : class
        {
            m_HostMap.Add(typeof(TIntf), ctor);
        }
    }
}
