using System.Collections.Generic;
using System.Linq;

namespace FloatingQueueServer
{
    public interface IEventAggregate
    {
        void Push(int version, object e);
        bool TryGetNext(int version, out object next);
        IEnumerable<object> GetAllNext(int version);
    }

    public class EventAggregate : IEventAggregate
    {
        private readonly List<object> m_InternalStorage = new List<object>();
        private readonly object m_SyncRoot = new object();

        public void Push(int version, object e)
        {
            lock (m_SyncRoot)
            {
                if (version != -1 && version != m_InternalStorage.Count)
                {
                    throw new OptimisticLockException();
                }
                m_InternalStorage.Add(e);
            }
        }

        public bool TryGetNext(int version, out object next)
        {
            lock (m_SyncRoot)
            {
                if(version < m_InternalStorage.Count)
                {
                    next = m_InternalStorage[version];
                    return true;
                }
                else
                {
                    next = null;
                    return false;
                }
            }
        }

        public IEnumerable<object> GetAllNext(int version)
        {
            lock (m_SyncRoot)
            {
                return m_InternalStorage.Skip(version + 1).ToList();
            }
        }
    }
}
