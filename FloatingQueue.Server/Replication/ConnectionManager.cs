using System;
using System.ServiceModel;
using System.Threading;

namespace FloatingQueue.Server.Replication
{
    public delegate void ConnectionLostHandler(int lostServerId);

    public interface IConnectionManager
    {
        void OpenOutcomingConnections();
        void CloseOutcomingConnections();
        bool TryReplicate(string aggregateId, int version, object e);
        event ConnectionLostHandler OnConnectionLoss; // todo MM: switch to readonly mode when all connections are lost
    }

    public class ConnectionManager : IConnectionManager
    {
        private bool m_IsConnectionOpened = false;
        private bool m_MonitoringEnabled = false;

        private readonly object m_InitializationLock = new object();
        private readonly object m_ConnectionLock = new object();

        public event ConnectionLostHandler OnConnectionLoss;

        public void OpenOutcomingConnections()
        {
            lock (m_InitializationLock)
            {
                if (!m_IsConnectionOpened)
                {
                    Core.Server.Log.Debug("Opening connections...");

                    foreach (var node in Core.Server.Configuration.Nodes.Siblings)
                    {
                        node.Proxy.Open();
                        Core.Server.Log.Info("Connected to {0}", node.InternalAddress);
                    }
                    StartMonitoringConnections();
                    m_IsConnectionOpened = true;

                    Core.Server.Log.Debug("Finished opening connections");
                }
            }
        }
        public void CloseOutcomingConnections()
        {
            lock (m_ConnectionLock)
            {
                foreach (var node in Core.Server.Configuration.Nodes.Siblings)
                {
                    node.Proxy.Close();
                }
                StopMonitoringConnections();
                m_IsConnectionOpened = false;
            }
        }

        public bool TryReplicate(string aggregateId, int version, object e)
        {
            if (!m_IsConnectionOpened)
            {
                OpenOutcomingConnections();
            }
            int replicas = 0;
            //lock (m_ConnectionLock) //note MM: this lock would avoid firing connection loss twice, but in cost of performance.
            //{
            foreach (var node in Core.Server.Configuration.Nodes.Siblings)
            {
                try
                {
                    node.Proxy.Push(aggregateId, version, e);
                    replicas++;
                }
                //catch (ObjectDisposedException) //todo
                //{
                //}
                catch (CommunicationException)
                {
                    FireConnectionLoss(node.ServerId);
                    Core.Server.Log.Warn("Replication at {0} failed. Node is dead", node.InternalAddress);
                }
                catch (Exception ex)
                {
                    Core.Server.Log.Warn("Cannot push.", ex);
                }
            }
            //}

            return replicas > 0;
        }

        // todo MM: consider pinging both addresses - public and internal
        private void StartMonitoringConnections()
        {
            lock (m_ConnectionLock)
            {
                m_MonitoringEnabled = true;
            }
            ThreadPool.QueueUserWorkItem(DoMonitoring);
        }
        private void StopMonitoringConnections()
        {
            m_MonitoringEnabled = false;
        }

        private bool m_IsMonitoring = false;
        private void DoMonitoring(object state)
        {
            if (m_IsMonitoring)
                return;
            m_IsMonitoring = true;
            try
            {
                bool stop = false;
                while (!stop)
                {
                    //if (Core.Server.Configuration.Nodes.Siblings.Count == 0)
                    //    Core.Server.Log.Info("No other servers to ping...");
                    //else
                        Core.Server.Log.Debug("Pinging other servers");
                    //lock (m_ConnectionLock)
                    //{
                    foreach (var node in Core.Server.Configuration.Nodes.Siblings)
                    {
                        var resultCode = node.Proxy.Ping();

                        Core.Server.Log.Debug("\t{0} - code {1}", node.InternalAddress, resultCode);

                        if (resultCode != 0)
                        {
                            FireConnectionLoss(node.ServerId);
                        }
                    }
                    //}

                    Thread.Sleep(Core.Server.Configuration.PingTimeout);

                    lock (m_ConnectionLock)
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
