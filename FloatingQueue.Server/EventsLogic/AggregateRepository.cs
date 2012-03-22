using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FloatingQueue.Server.EventsLogic
{
    public interface IAggregateRepository
    {
        bool TryGetEventAggregate(string aggregateId, out IEventAggregate aggregate);
        IEventAggregate CreateAggregate(string aggreagateId);
        List<string> GetAllIds();
        Dictionary<string, int> GetLastVersions();
    }

    public class AggregateRepository : IAggregateRepository
    {
        public static IAggregateRepository Instance
        {
            get { return Core.Server.Resolve<IAggregateRepository>(); } // todo: it makes sense to cache it in static variable due to performance reasons
        }

        private readonly Dictionary<string, IEventAggregate> m_InternalStorage = new Dictionary<string, IEventAggregate>();
        private readonly ReaderWriterLockSlim m_Lock = new ReaderWriterLockSlim();

        public bool TryGetEventAggregate(string aggregateId, out IEventAggregate aggregate)
        {
            try
            {
                m_Lock.EnterReadLock();
                return m_InternalStorage.TryGetValue(aggregateId, out aggregate);
            }
            finally
            {
                m_Lock.ExitReadLock();
            }
        }

        public IEventAggregate CreateAggregate(string aggreagateId)
        {
            try
            {
                m_Lock.EnterWriteLock();
                if (m_InternalStorage.ContainsKey(aggreagateId)) // prevent race condition
                {
                    return m_InternalStorage[aggreagateId];
                }
                else
                {
                    var aggregate = Core.Server.Resolve<IEventAggregate>();
                    m_InternalStorage.Add(aggreagateId, aggregate);
                    return aggregate;
                }
            }
            finally
            {
                m_Lock.ExitWriteLock();
            }
        }

        public List<string> GetAllIds()
        {
            try
            {
                m_Lock.EnterReadLock();
                return m_InternalStorage.Keys.ToList();
            }
            finally
            {
                m_Lock.ExitReadLock();
            }
        }

        public Dictionary<string, int> GetLastVersions()
        {
            try
            {
                m_Lock.EnterReadLock();
                return m_InternalStorage.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.LastVersion);
            }
            finally
            {
                m_Lock.ExitReadLock();
            }
        }
    }
}
