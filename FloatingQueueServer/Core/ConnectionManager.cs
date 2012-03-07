using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using FloatingQueue.ServiceProxy;
using FloatingQueue.ServiceProxy.GeneratedClient;

namespace FloatingQueue.Server.Core
{
    public delegate void ConnectionLostHandler(string lostConnectionAddress);

    public interface IConnectionManager
    {
        void OpenOutcomingConnections();
        void CloseOutcomingConnections();
        bool TryReplicate(string aggregateId, int version, object e);
        event ConnectionLostHandler OnConnectionLoss; // fires in another thread
    }

    public class ConnectionManager : IConnectionManager
    {
        private readonly ProxyCollection m_Proxies = new ProxyCollection();
        private readonly object m_MonitoringLock = new object();
        private bool m_IsConnectionOpened = false;
        private bool m_MonitoringEnabled;
        private readonly object m_InitializationLock = new object();

        public const int MonitorWaitTime = 5000;

        public event ConnectionLostHandler OnConnectionLoss;

        // note MM: currently slaves don't open outer connections
        public void OpenOutcomingConnections()
        {
            lock (m_InitializationLock)
            {
                if (!m_IsConnectionOpened)
                {
                    foreach (var node in Server.Configuration.Nodes)
                    {
                        m_Proxies.OpenProxy(node.Address);
                        Server.Log.Info("Connected to \t{0}", node.Address);
                    }
                    StartMonitoringConnections();
                    m_IsConnectionOpened = true;
                }
            }
        }

        public void CloseOutcomingConnections()
        {
            m_Proxies.CloseAllProxies();
            StopMonitoringConnections();
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

        public void StartMonitoringConnections()
        {
            lock (m_MonitoringLock)
            {
                m_MonitoringEnabled = true;
            }
            ThreadPool.QueueUserWorkItem(DoMonitoring);
        }
        public void StopMonitoringConnections()
        {
            lock (m_MonitoringLock)
            {
                m_MonitoringEnabled = false;
            }
        }

        private bool m_IsMonitoring = false;
        private void DoMonitoring(object state)
        {
            // note MM: currently monitoring thread is stopped automatically when process is stopped, but this is a proper way to stop it without stopping process
            if (m_IsMonitoring)
                return;
            m_IsMonitoring = true;
            try
            {
                bool stop = false;
                while (!stop)
                {
                    IEnumerable<string> pingAddresses = (Server.Configuration.IsMaster
                                                             ? Server.Configuration.Nodes
                                                             : Server.Configuration.Nodes.Where(n => n.IsMaster))
                                                             // slaves ping only master
                                                             .Select(n => n.Address).ToList();

                    Server.Log.Info("Pinging other servers");
                    foreach (var address in pingAddresses)
                    {
                        var result = PingNode(address);

                        Server.Log.Info("\t{0} - code {1}", address, result.ResultCode);

                        if (result.ResultCode != 0)
                        {
                            if (OnConnectionLoss != null)
                                OnConnectionLoss(address);
                        }
                    }
                    Thread.Sleep(MonitorWaitTime);
                    lock (m_MonitoringLock)
                    {
                        stop = !m_MonitoringEnabled;
                    }
                }
            }
            finally
            {
                m_IsMonitoring = false;
            }
        }

        private PingResult PingNode(string address)
        {
            lock (m_MonitoringLock)
            {
                var proxy = m_Proxies.LiveProxies.ToList().Where(p => address == p.Address);
                if (proxy.Count() != 1)
                    return new PingResult() {ResultCode = 3};
                return proxy.Single().Ping();
            }

        }
    }
}
