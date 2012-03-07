using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FloatingQueue.ServiceProxy;

namespace FloatingQueue.Server.Core
{
    public class Communicator
    {
        // todo: cleanup this code.
        // todo: consider synchronizations issues
        
        private static readonly List<ManualQueueProxy> m_Workers = new List<ManualQueueProxy>();
        private static bool m_IsConnectionOpened = false;

        public static void Broadcast(string aggregateId, int version, object e)
        {
            if (!m_IsConnectionOpened)
            {
                OpenOutcomingConnections();
                m_IsConnectionOpened = true;
            }
            foreach (var worker in m_Workers)
                worker.Push(aggregateId, version, e);
        }

        private static void OpenOutcomingConnections()
        {
            foreach (var node in Server.Configuration.Nodes)
            {
                var proxy = new ManualQueueProxy(node.Address);
                m_Workers.Add(proxy);
                proxy.Open();
                Server.Log.Info("Connected to \t{0}", node.Address);
            }
        }

        private static void CloseOutcomingConnections()
        {
            foreach (var worker in m_Workers)
                worker.Close();
            m_IsConnectionOpened = false;
        }

        public static void Dispose()
        {
            CloseOutcomingConnections();
        }
    }
}
