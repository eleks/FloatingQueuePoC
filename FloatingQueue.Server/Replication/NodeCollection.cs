using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FloatingQueue.Server.Core;

namespace FloatingQueue.Server.Replication
{
    public interface INodeCollection
    {
        ReadOnlyCollection<INodeConfiguration> All { get; }
        ReadOnlyCollection<INodeConfiguration> Siblings { get; }
        INodeConfiguration Self { get; }
        INodeConfiguration Master { get; }
        void MarkAsDead(int nodeId);
        void AddNewNode(INodeConfiguration slave);
    }

    public class NodeCollection : INodeCollection
    {
        private readonly object m_SyncRoot = new object();

        private readonly List<INodeConfiguration> m_Nodes;
        private readonly List<bool> m_DeadNodes;

        public NodeCollection(List<INodeConfiguration> nodes)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException("nodes");
            }
            if (nodes.Count() == 0)
            {
                throw new ArgumentException("nodes must contain at least 1 node");
            }
            if (nodes.Distinct().Count() != nodes.Count())
            {
                throw new ArgumentException("nodes contains duplicated ID");
            }

            m_Nodes = nodes;

            m_DeadNodes = new List<bool>();
            m_DeadNodes.AddRange(new bool[nodes.Count]);
        }

        public ReadOnlyCollection<INodeConfiguration> All
        {
            get
            {
                lock (m_SyncRoot)
                {
                    return LiveProxies.ToList().AsReadOnly();
                }
            }
        }

        public ReadOnlyCollection<INodeConfiguration> Siblings
        {
            get
            {
                lock (m_SyncRoot)
                {
                    return LiveProxies.Where(n => n.ServerId != Core.Server.Configuration.ServerId).ToList().AsReadOnly();
                }
            }
        }

        public INodeConfiguration Self
        {
            get
            {
                lock (m_SyncRoot)
                {
                    var self = LiveProxies.SingleOrDefault(n => n.ServerId == Core.Server.Configuration.ServerId);
                    if (self != null)
                    {
                        return self;
                    }

                    throw new ApplicationException("Inconsistent server state. Configuration and actual ID mismatch");
                }
            }
        }

        public INodeConfiguration Master
        {
            get
            {
                lock (m_SyncRoot)
                {
                    try
                    {
                        return LiveProxies.Single(n => n.IsMaster);
                    }
                    catch (InvalidOperationException ex)
                    {
                        throw new ApplicationException("There must be exactly 1 master", ex);
                    }
                }
            }
        }

        public void AddNewNode(INodeConfiguration slave)
        {
            if (slave == null)
            {
                throw new ArgumentNullException("slave");
            }
            lock (m_SyncRoot)
            {
                RemoveDeadNodes();
                if (m_Nodes.Contains(slave))
                {
                    throw new ArgumentException("cannot add node. Server ID already exists");
                }
                //
                if (slave.IsMaster && m_Nodes.Any(n => n.IsMaster))
                {
                    throw new ArgumentException("Cannot add second master node");
                }
                //
                m_Nodes.Add(slave);
                m_DeadNodes.Add(false);
            }
        }

        public void MarkAsDead(int nodeId)
        {
            lock (m_SyncRoot)
            {
                var node = m_Nodes.SingleOrDefault(p => p.ServerId == nodeId);
                if (node != null)
                {
                    var index = m_Nodes.IndexOf(node);
                    node.Proxy.Close();
                    m_DeadNodes[index] = true;
                }
            }
            //m_Proxies.RemoveAll(node => node.ServerId == nodeId);
        }

        public void RemoveDeadNodes()
        {
            lock (m_SyncRoot)
            {
                for (int i = m_DeadNodes.Count - 1; i >= 0; i--)
                {
                    if (m_DeadNodes[i])
                    {
                        m_Nodes.RemoveAt(i);
                        m_DeadNodes.RemoveAt(i);
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

        public int DeadNodesCount
        {
            get
            {
                lock (m_SyncRoot)
                {
                    return m_DeadNodes.Count(i => i);
                }
            }
        }
    }
}
