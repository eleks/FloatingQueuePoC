using System.Collections.Generic;
using System.Linq;
using FloatingQueue.Server.Exceptions;

namespace FloatingQueue.Server.EventsLogic
{
    public interface IEventAggregate
    {
        void Push(int version, object e);
        bool TryGetNext(int version, out object next);
        IEnumerable<object> GetAllNext(int version);
        string AggregateId { get; }
    }

    public class EventAggregate : IEventAggregate
    {
        private readonly List<object> m_InternalStorage = new List<object>();
        private readonly object m_SyncRoot = new object();
        private readonly string m_AggregateId;
		// consider avoding aggrigate id in this class
        public EventAggregate(string aggregateId)
        {
            m_AggregateId = aggregateId;
        }

        public void Push(int version, object e)
        {
            lock (m_SyncRoot)
            {
                if (version != -1 && version != m_InternalStorage.Count)
                {
                    throw new OptimisticLockException();
                }
                {   // todo: wrap this into transaction
                    m_InternalStorage.Add(e);
                    if (Core.Server.Configuration.IsMaster)
                        Core.Server.Broadcast(m_AggregateId, version, e);
                }
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

        public string AggregateId
        {
            get { return m_AggregateId; }
        }
    }
}
