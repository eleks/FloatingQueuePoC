using System.Collections.Generic;
using System.Threading;

namespace FloatingQueue.Server.EventsLogic
{
    public interface IAggregateRepository
    {
        bool TryGetEventAggregate(string aggregateId, out IEventAggregate aggregate);
        IEventAggregate CreateAggregate(string aggreagateId);
    }

    public class AggregateRepository : IAggregateRepository
    {
        private static readonly AggregateRepository ms_Instance = new AggregateRepository(); // todo: replace singleton with IoC

        public static AggregateRepository Instance
        {
            get { return ms_Instance; }
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
                var aggregate = Core.Server.Resolve<IEventAggregate>(); // todo : use factory/IoC here
                m_InternalStorage.Add(aggreagateId, aggregate);
                return aggregate;
            }
            finally
            {
                m_Lock.ExitWriteLock();
            }
        }
    }
}
