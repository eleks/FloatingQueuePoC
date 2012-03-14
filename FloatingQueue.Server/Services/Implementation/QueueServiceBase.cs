using System;
using System.Collections.Generic;
using FloatingQueue.Common;
using FloatingQueue.Server.EventsLogic;
using FloatingQueue.Server.Replication;

namespace FloatingQueue.Server.Services.Implementation
{
    public class QueueServiceBase : IQueueService
    {
        public virtual void Push(string aggregateId, int version, object e)
        {
            Core.Server.Log.Info("Command: push {0} {1} {2}", aggregateId, version, e);
            var aggregate = GetEventAggregate(aggregateId);
            try
            {
                aggregate.Push(version, e);
                if (Core.Server.Configuration.IsMaster)
                {
                    var replicated = Core.Server.Resolve<IConnectionManager>().TryReplicate(aggregateId, version, e);
                    if (!replicated)
                    {
                        throw new ApplicationException("Cannot replicate the data.");
                    }
                }
                else
                {
                    // todo: find a better place to open connections from slaves
                    Core.Server.Resolve<IConnectionManager>().OpenOutcomingConnections();
                }
                aggregate.Commit();
            }
            catch
            {
                aggregate.Rollback();
                throw;
            }
        }

        public virtual bool TryGetNext(string aggregateId, int version, out object next)
        {
            var aggregate = GetEventAggregate(aggregateId);
            return aggregate.TryGetNext(version, out next);
        }

        public virtual IEnumerable<object> GetAllNext(string aggregateId, int version)
        {
            var aggregate = GetEventAggregate(aggregateId);
            return aggregate.GetAllNext(version);
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

    }
}
