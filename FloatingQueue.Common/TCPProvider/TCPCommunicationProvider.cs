using System;
using System.Collections.Generic;
using System.Linq;
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

        public ICommunicationObject CreateHost<T>(string displayName, string address)
        {
            Func<TCPServerBase> ctor;
            if (!m_HostMap.TryGetValue(typeof(T), out ctor))
                throw new Exception("Implementation unknown for type " + typeof(T).FullName);
            var res = ctor();
            res.Initialize(displayName, address);
            return res;
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
