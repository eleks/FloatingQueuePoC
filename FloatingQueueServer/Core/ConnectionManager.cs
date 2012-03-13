using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;

namespace FloatingQueue.Server.Core
{
    // todo: identify lost server by ServerId, not by Address
    public delegate void ConnectionLostHandler(int lostServerId);

    public interface IConnectionManager
    {
        void OpenOutcomingConnections();
        void CloseOutcomingConnections();
        bool TryReplicate(string aggregateId, int version, object e);
        event ConnectionLostHandler OnConnectionLoss; // todo MM: consider taking some actions when all connections are lost
    }

    public class ConnectionManager : IConnectionManager
    {
        private readonly object m_MonitoringLock = new object();
        private bool m_IsConnectionOpened = false;
        private bool m_MonitoringEnabled;
        private readonly object m_InitializationLock = new object();

        public const int MonitorWaitTime = 10000;

        public event ConnectionLostHandler OnConnectionLoss;

        public void OpenOutcomingConnections()
        {
            lock (m_InitializationLock)
            {
                if (!m_IsConnectionOpened)
                {
                    foreach (var node in Server.Configuration.Nodes.Siblings)
                    {
                        node.Proxy.Open();
                        Server.Log.Info("Connected to \t{0}", node.Address);
                    }
                    StartMonitoringConnections();
                    OnConnectionLoss += Server.Configuration.Nodes.RemoveDeadNode;
                    m_IsConnectionOpened = true;
                }
            }
        }
        public void CloseOutcomingConnections()
        {
            foreach (var node in Server.Configuration.Nodes.Siblings)
            {
                node.Proxy.Close();
            }
            StopMonitoringConnections();
            OnConnectionLoss -= Server.Configuration.Nodes.RemoveDeadNode;
            m_IsConnectionOpened = false;
        }

        public bool TryReplicate(string aggregateId, int version, object e)
        {
            if (!m_IsConnectionOpened)
            {
                OpenOutcomingConnections();
            }
            int replicas = 0;
            foreach (var node in Server.Configuration.Nodes.Siblings)
            {
                try
                {
                    node.Proxy.Push(aggregateId, version, e);
                    replicas++;
                }
                catch (CommunicationException)
                {
                    FireConnectionLoss(node.ServerId);
                    Server.Log.Warn("Replication at {0} failed. Node is dead", node.Address);
                }
                catch (Exception ex)
                {
                    Server.Log.Warn("Cannot push.", ex);
                }
            }

            // todo MM: Server.Configuration.Nodes.RemoveDeadProxies();

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
                    Server.Log.Debug("Pinging other servers");
                    foreach (var node in Server.Configuration.Nodes.Siblings)
                    {
                        var result = node.Proxy.Ping();

                        Server.Log.Debug("\t{0} - code {1}", node.Address, result.ResultCode);

                        if (result.ResultCode != 0)
                        {
                            FireConnectionLoss(node.ServerId);
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

        private void FireConnectionLoss(int serverId)
        {
            if (OnConnectionLoss != null)
                OnConnectionLoss(serverId);
        }

    }
}
