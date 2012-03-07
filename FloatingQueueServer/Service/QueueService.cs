using System.Collections.Generic;
using FloatingQueue.Server.Core;
using FloatingQueue.Server.EventsLogic;

namespace FloatingQueue.Server.Service
{
    public class QueueService : IQueueService
    {
        public void Push(string aggregateId, int version, object e)
        {
            Core.Server.Log.Info("Command: push {0} {1} {2}", aggregateId, version, e);
            var aggregate = GetEventAggregate(aggregateId);
            aggregate.Push(version, e);
            if (Core.Server.Configuration.IsMaster)
                Core.Server.Resolve<IConnectionManager>().Broadcast(aggregateId, version, e);
        }

        private static IEventAggregate GetEventAggregate(string aggregateId)
        {
            IEventAggregate aggregate;
            if (!AggregateRepository.Instance.TryGetEventAggregate(aggregateId, out aggregate))
            {
                aggregate = AggregateRepository.Instance.CreateAggregate(aggregateId);
            }
            return aggregate;
        }

        public bool TryGetNext(string aggregateId, int version, out object next)
        {
            var aggregate = GetEventAggregate(aggregateId);
            return aggregate.TryGetNext(version, out next);
        }

        public IEnumerable<object> GetAllNext(string aggregateId, int version)
        {
            var aggregate = GetEventAggregate(aggregateId);
            return aggregate.GetAllNext(version);
        }
    }
}
