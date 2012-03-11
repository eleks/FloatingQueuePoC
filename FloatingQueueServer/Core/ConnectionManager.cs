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
    // todo: identify lost server by ServerId, not by Address
    public delegate void ConnectionLostHandler(string lostConnectionAddress);

    public interface IConnectionManager
    {
        void OpenOutcomingConnections();
        void CloseOutcomingConnections();
        bool TryReplicate(string aggregateId, int version, object e);
        event ConnectionLostHandler OnConnectionLoss; // todo MM: consider taking some actions when all connections are lost
    }

    public class ConnectionManager : IConnectionManager
    {
        // todo: consider mixing ProxyCollection and NodeCollection, as they represent similar information
        private readonly ProxyCollection m_Proxies = new ProxyCollection();
        private readonly object m_MonitoringLock = new object();
        private bool m_IsConnectionOpened = false;
        private bool m_MonitoringEnabled;
        private readonly object m_InitializationLock = new object();

        public const int MonitorWaitTime = 10000;

        public event ConnectionLostHandler OnConnectionLoss;

        // note MM: currently slaves don't open outer connections
        public void OpenOutcomingConnections()
        {
            lock (m_InitializationLock)
            {
                if (!m_IsConnectionOpened)
                {
                    // note MM: logic changed - slaves ping other slaves also (previously they pinged only master)
                    foreach (var node in Server.Configuration.Nodes.Siblings)
                    {
                        m_Proxies.OpenProxy(node.Address);
                        Server.Log.Info("Connected to \t{0}", node.Address);
                    }
                    StartMonitoringConnections();
                    OnConnectionLoss += m_Proxies.MarkAsDead;
                    m_IsConnectionOpened = true;
                }
            }
        }

        public void CloseOutcomingConnections()
        {
            m_Proxies.CloseAllProxies();
            StopMonitoringConnections();
            OnConnectionLoss -= m_Proxies.MarkAsDead;
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
                    FireConnectionLoss(proxy.Address);
                    Server.Log.Warn("Replication at {0} failed. Node is dead", proxy.Address);
                }
                catch (Exception ex)
                {
                    Server.Log.Warn("Cannot push.", ex);
                }
            }
            m_Proxies.RemoveDeadProxies();

            return replicas > 0;
        }

        private void StartMonitoringConnections()
        {
            lock (m_MonitoringLock)
            {
                m_MonitoringEnabled = true;
            }
            ThreadPool.QueueUserWorkItem(DoMonitoring);
        }
        private void StopMonitoringConnections()
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
                    IEnumerable<string> pingAddresses = 
                        (Server.Configuration.IsMaster
                       ? Server.Configuration.Nodes.Siblings
                       : Server.Configuration.Nodes.Where(n => n.IsMaster)) // slaves ping only master
                       .Select(n => n.Address).ToList();

                    Server.Log.Debug("Pinging other servers");
                    foreach (var proxy in m_Proxies.LiveProxies)
                    {
                        var result = proxy.Ping();

                        Server.Log.Debug("\t{0} - code {1}", proxy.Address, result.ResultCode);

                        if (result.ResultCode != 0)
                        {
                            FireConnectionLoss(proxy.Address);
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

        private void FireConnectionLoss(string address)
        {
            if (OnConnectionLoss != null)
                OnConnectionLoss(address);
        }

    }
}
