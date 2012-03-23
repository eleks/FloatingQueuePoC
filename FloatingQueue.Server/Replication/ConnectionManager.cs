using System;
using System.IO;
using System.Net.Sockets;
using System.ServiceModel;
using System.Threading;
using FloatingQueue.Common;
using FloatingQueue.Common.Common;

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

        private readonly object m_ConnectionLock = new object();
        private bool m_IsConnectionOpened;
        private MonitoringThread m_Thread;

        public event ConnectionLostHandler OnConnectionLoss;

        public void OpenOutcomingConnections()
        {
            lock (m_ConnectionLock)
            {
                if (!m_IsConnectionOpened)
                {
                    Core.Server.Log.Debug("Opening connections...");

                    var nodes = Core.Server.Configuration.Nodes.Siblings;
                    foreach (var node in nodes)
                    {
                        node.Proxy.Open();
                        Core.Server.Log.Info("Connected to {0}", node.InternalAddress);
                    }
                    StartMonitoringConnections();
                    m_IsConnectionOpened = true;

                    Core.Server.Log.Debug("Opening connections done");
                }
            }
        }
        public void CloseOutcomingConnections()
        {
            lock (m_ConnectionLock)
            {
                StopMonitoringConnections();
                var nodes = Core.Server.Configuration.Nodes.Siblings;
                foreach (var node in nodes)
                {
                    node.Proxy.Close();
                }
                m_IsConnectionOpened = false;
            }
        }

        private void StartMonitoringConnections()
        {
            lock (m_ConnectionLock)
            {
                if (m_Thread != null)
                    throw new InvalidOperationException("Monitoring thread already started");
                //
                m_Thread = new MonitoringThread(this);
                m_Thread.Start(null);
            }
        }

        private void StopMonitoringConnections()
        {
            lock (m_ConnectionLock)
            {
                if (m_Thread != null)
                {
                    m_Thread.Stop();
                    m_Thread.Wait();
                    m_Thread = null;
                }
            }
        }

        private class MonitoringThread : ThreadBase
        {
            private readonly ConnectionManager m_Manager;

            public MonitoringThread(ConnectionManager manager)
                : base("ConnectionMonitoring")
            {
                m_Manager = manager;
            }

            protected override void RunCore()
            {
                while(!IsStopping)
                {
                    Core.Server.Log.Debug("Pinging other servers");
                    var nodes = Core.Server.Configuration.Nodes.Siblings;
                    foreach (var node in nodes)
                    {
                        try
                        {
                            node.Proxy.Ping();
                            Core.Server.Log.Debug("\t{0} - ping OK", node.InternalAddress);
                        }
                        catch (Exception ex)
                        {
                            Core.Server.Log.Debug("\t{0} - connection loss. Message {1}", node.InternalAddress, ex.Message);
                            m_Manager.FireConnectionLoss(node.ServerId);
                        }
                    }
                    Thread.Sleep(Core.Server.Configuration.PingTimeout);
                }
            }
        }

        private void FireConnectionLoss(int serverId)
        {
            var onConnectionLoss = OnConnectionLoss;
            if (onConnectionLoss != null)
                onConnectionLoss(serverId);
        }

        public bool TryReplicate(string aggregateId, int version, object e)
        {
            if (!m_IsConnectionOpened)
            {
                throw new InvalidOperationException("ConnectionManager is not open. Replication is not allowed");
                //OpenOutcomingConnections();
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
                catch (ConnectionErrorException ex)
                {
                    FireConnectionLoss(node.ServerId);
                    Core.Server.Log.Warn("Replication at {0} failed. (ConnectionErrorException: {1}) Node is dead", node.InternalAddress, ex.Message);
                }
                catch (Exception ex)
                {
                    Core.Server.Log.Warn("Cannot push.", ex);
                }
            }
            //}

            return replicas > 0;
        }


    }
}
