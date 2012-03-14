using System.Collections.Generic;
using System.Linq;
using FloatingQueue.Server.Core;

namespace FloatingQueue.Server.Replication
{
    public interface INodeCollection
    {
        IEnumerable<INodeConfiguration> Siblings { get; }
        IEnumerable<INodeConfiguration> All { get; } //todo MM: consider inheriting this interface from IEnumerable
        INodeConfiguration Self { get; }
        INodeConfiguration Master { get; }
        void RemoveDeadNode(int nodeId);
        // void AddNewSlave(INodeConfiguration slave);
    }


    public class NodeCollection : INodeCollection
    {
        private readonly List<INodeConfiguration> m_Nodes;
        private readonly List<bool> m_DeadNodes;
        private readonly object m_SyncRoot = new object();

        public NodeCollection(List<INodeConfiguration> nodes)
        {
            m_Nodes = nodes;
            m_DeadNodes = new List<bool>();
            m_DeadNodes.AddRange(new bool[nodes.Count]);
        }

        //public void OpenProxy(string address)
        //{
        //    lock (m_SyncRoot)
        //    {
        //        var proxy = new ManualQueueServiceProxy(address);
        //        m_Proxies.Add(proxy);
        //        m_DeadProxies.Add(false);
        //        proxy.Open();
        //    }
        //}

        //public void CloseAllProxies()
        //{
        //    lock (m_SyncRoot)
        //    {
        //        // note MM: if server is closed in small interval after another server has died and before it's been noticed(by ping or by method call), then wcf would fire exceptions. But at the moment they are handled by Proxy class. this may be subject to fix.
        //        RemoveDeadProxies();
        //        foreach (var proxy in m_Proxies)
        //        {
        //            proxy.Close();
        //        }
        //    }
        //}

        public IEnumerable<INodeConfiguration> Siblings
        {
            get
            {
                lock (m_SyncRoot)
                {
                    return LiveProxies.Where(n => n.ServerId != Core.Server.Configuration.ServerId);
                }
            }
        }

        public INodeConfiguration Self
        {
            get
            {
                lock (m_SyncRoot)
                {
                    return LiveProxies.Single(n => n.ServerId == Core.Server.Configuration.ServerId);
                }
            }
        }

        public INodeConfiguration Master
        {
            get
            {
                lock (m_SyncRoot)
                {
                    return LiveProxies.Single(n => n.IsMaster);
                }
            }
        }

        public void RemoveDeadNode(int nodeId)
        {
            lock (m_SyncRoot)
            {
                var node = m_Nodes.Single(p => p.ServerId == nodeId);
                var index = m_Nodes.IndexOf(node);
                node.Proxy.Close();
                m_DeadNodes[index] = true;
            }
            //m_Proxies.RemoveAll(node => node.ServerId == nodeId);
        }


        public IEnumerable<INodeConfiguration> All
        {
            get
            {
                lock (m_SyncRoot)
                {
                    return m_Nodes;
                }
            }
        }

        public void RemoveDeadProxies()
        {
            lock (m_SyncRoot)
            {
                for (int i = 0; i < m_DeadNodes.Count; i++)
                {
                    if (m_DeadNodes[i])
                    {
                        m_Nodes.RemoveAt(i);
                        m_DeadNodes.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        // note MM: this approach is used to let remove nodes from collection while iterating through it(it seems like it's important right now
        private IEnumerable<INodeConfiguration> LiveProxies
        {
            get
            {
                return m_Nodes.Where((t, i) => !m_DeadNodes[i]);
            }
        }
    }
}
