
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FloatingQueue.ServiceProxy;

namespace FloatingQueue.Server.Core
{
    public class ProxyCollection
    {
        private readonly List<ManualQueueProxy> m_Proxies = new List<ManualQueueProxy>();
        private readonly List<bool> m_DeadProxies = new List<bool>();
        private readonly object m_SyncRoot = new object();

        public void OpenProxy(string address)
        {
            lock (m_SyncRoot)
            {
                var proxy = new ManualQueueProxy(address);
                m_Proxies.Add(proxy);
                m_DeadProxies.Add(false);
                proxy.Open();
            }
        }

        public void CloseAllProxies()
        {
            lock (m_SyncRoot)
            {
                foreach (var proxy in m_Proxies)
                {
                    proxy.Close();
                }
            }
        }

        public IEnumerable<ManualQueueProxy> LiveProxies
        {
            get
            {
                lock (m_SyncRoot)
                {
                    return m_Proxies.Where((t, i) => !m_DeadProxies[i]);
                }
            }
        }

        public void MarkAsDead(ManualQueueProxy proxy)
        {
            lock (m_SyncRoot)
            {
                proxy.Close();
                var index = m_Proxies.IndexOf(proxy);
                m_DeadProxies[index] = true;
            }
        }

        public void RemoveDeadProxies()
        {
            lock (m_SyncRoot)
            {
                for (int i = 0; i < m_DeadProxies.Count; i++)
                {
                    if (m_DeadProxies[i])
                    {
                        m_Proxies.RemoveAt(i);
                        m_DeadProxies.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

    }
}
