using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FloatingQueue.ServiceProxy;

namespace FloatingQueue.Server.Core
{
    public interface IConnectionManager
    {
        void ConnectToSiblings();
        void CloseOutcomingConnections();
        bool TryReplicate(string aggregateId, int version, object e);
    }

    public class ConnectionManager : IConnectionManager
    {
        private readonly List<ManualQueueProxy> m_Siblings = new List<ManualQueueProxy>();

        private bool m_IsConnectionOpened = false;

        public void ConnectToSiblings()
        {
            foreach (var node in Server.Configuration.Nodes)
            {
                var proxy = new ManualQueueProxy(node.Address);
                m_Siblings.Add(proxy);
                proxy.Open();
                Server.Log.Info("Connected to \t{0}", node.Address);
            }
            m_IsConnectionOpened = true;
        }

        //todo: find a better name, structure code better

        public void CloseOutcomingConnections()
        {
            foreach (var proxy in m_Siblings)
            {
                proxy.Close();
            }
            m_IsConnectionOpened = false;
        }

        public bool TryReplicate(string aggregateId, int version, object e)
        {
            if (!m_IsConnectionOpened)
            {
                ConnectToSiblings();
            }
            int replicas = 0;
            foreach (var proxy in m_Siblings)
            {
                try
                {
                    proxy.Push(aggregateId, version, e);
                    replicas++;
                }
                catch(Exception ex)
                {
                    Server.Log.Warn("Cannot push.", ex);
                }
            }
            return replicas > 0;
        }
    }
}
