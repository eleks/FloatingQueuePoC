using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using FloatingQueue.ServiceProxy;
using FloatingQueue.ServiceProxy.GeneratedClient;

namespace FloatingQueue.Server.Core
{
    public interface IConnectionManager
    {
        void OpenOutcomingConnections();
        void CloseOutcomingConnections();
        bool TryReplicate(string aggregateId, int version, object e);
    }

    public class ConnectionManager : IConnectionManager
    {
        private readonly ProxyCollection m_Proxies = new ProxyCollection();

        private bool m_IsConnectionOpened = false;

        public void OpenOutcomingConnections()
        {
            foreach (var node in Server.Configuration.Nodes)
            {
                m_Proxies.OpenProxy(node.Address);
                Server.Log.Info("Connected to \t{0}", node.Address);
            }
            m_IsConnectionOpened = true;
        }

        public void CloseOutcomingConnections()
        {
            m_Proxies.CloseAllProxies();
            m_IsConnectionOpened = false;
        }

        public bool TryReplicate(string aggregateId, int version, object e)
        {
            if (!m_IsConnectionOpened)
            {
                OpenOutcomingConnections();
            }
            int replicas = 0;
            foreach (var proxy in m_Proxies.LiveProxies)
            {
                try
                {
                    proxy.Push(aggregateId, version, e);
                    replicas++;
                }
                catch (CommunicationException)
                {
                    m_Proxies.MarkAsDead(proxy);
                    Server.Log.Warn("node {0} is dead", proxy.Address);
                }
                catch (Exception ex)
                {
                    Server.Log.Warn("Cannot push.", ex);
                }
            }
            m_Proxies.RemoveDeadProxies();

            return replicas > 0;
        }

        private PingResult Ping(string address)
        {
            // todo: return failure in ping result instead of throwing
            var proxy = m_Proxies.LiveProxies.Where(p => address == p.Address).Single();
            return proxy.Ping();
        }
    }
}
