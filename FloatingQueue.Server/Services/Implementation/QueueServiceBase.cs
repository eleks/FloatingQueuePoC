using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FloatingQueue.Common;
using FloatingQueue.Server.EventsLogic;
using FloatingQueue.Server.Exceptions;
using FloatingQueue.Server.Replication;

namespace FloatingQueue.Server.Services.Implementation
{
    public abstract class QueueServiceBase : IQueueService
    {
        public virtual void Push(string aggregateId, int version, object e)
        {
            Core.Server.Log.Debug("Command: push {0} {1} {2}", aggregateId, version, e);

            if (Core.Server.Configuration.IsReadonly)
                throw new ReadOnlyException("Server is currently in readonly mode. Writes are not allowed");

            if (!Core.Server.Configuration.IsSynced)
                throw new InvalidOperationException("Write is not allowed to unsynced server");

            // note MM: potential bug - if rollback is required, an aggregate who has just been created will not be deleted
            var aggregate = GetEventAggregate(aggregateId);
            using(var transaction = aggregate.BeginTransaction())
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
                transaction.Commit();
            }
        }

        public virtual bool TryGetNext(string aggregateId, int version, out object next)
        {
            if (!Core.Server.Configuration.IsSynced)
                throw new BusinessLogicException("Cannot read from node, who's in syncing state");

            var aggregate = GetEventAggregate(aggregateId);
            return aggregate.TryGetNext(version, out next);
        }

        public virtual IEnumerable<object> GetAllNext(string aggregateId, int version)
        {
            if (!Core.Server.Configuration.IsSynced)
                throw new BusinessLogicException("Cannot read from node, who's in syncing state");

            var aggregate = GetEventAggregate(aggregateId);
            return aggregate.GetAllNext(version);
        }

        public ClusterMetadata GetClusterMetadata()
        {
            //todo: pinging other servers here would make client's life a bit easier
            var nodes = Core.Server.Configuration.Nodes.All
                .Select(n => new NodeInfo {Address = n.PublicAddress, IsMaster = n.IsMaster}).ToList();
            return new ClusterMetadata(nodes);
        }

        protected static IEventAggregate GetEventAggregate(string aggregateId)
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
