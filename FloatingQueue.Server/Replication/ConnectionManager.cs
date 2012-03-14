using System;
using System.ServiceModel;
using System.Threading;
using FloatingQueue.Common;

namespace FloatingQueue.Server.Replication
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
        private bool m_IsConnectionOpened = false;
        private bool m_MonitoringEnabled = false;

        private readonly object m_InitializationLock = new object();
        private readonly object m_MonitoringLock = new object();

        public const int MonitorWaitTime = 10000;

        public event ConnectionLostHandler OnConnectionLoss;

        public void OpenOutcomingConnections()
        {
            lock (m_InitializationLock)
            {
                if (!m_IsConnectionOpened)
                {
                    foreach (var node in Core.Server.Configuration.Nodes.SyncedSiblings)
                    {
                        node.Proxy.Open();
                        Core.Server.Log.Info("Connected to \t{0}", node.Address);
                    }
                    StartMonitoringConnections();
                    m_IsConnectionOpened = true;
                }
            }
        }
        public void CloseOutcomingConnections()
        {
            foreach (var node in Core.Server.Configuration.Nodes.SyncedSiblings)
            {
                node.Proxy.Close();
            }
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
            foreach (var node in Core.Server.Configuration.Nodes.SyncedSiblings)
            {
                try
                {
                    node.Proxy.Push(aggregateId, version, e);
                    replicas++;
                }
                catch (CommunicationException)
                {
                    FireConnectionLoss(node.ServerId);
                    Core.Server.Log.Warn("Replication at {0} failed. Node is dead", node.Address);
                }
                catch (Exception ex)
                {
                    Core.Server.Log.Warn("Cannot push.", ex);
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
                    Core.Server.Log.Debug("Pinging other servers");
                    foreach (var node in Core.Server.Configuration.Nodes.SyncedSiblings)
                    {
                        var result = node.Proxy.Ping(PingHelper.CheckConnectionPingParams);

                        Core.Server.Log.Debug("\t{0} - code {1}", node.Address, result.ErrorCode);

                        if (result.ErrorCode != 0)
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
