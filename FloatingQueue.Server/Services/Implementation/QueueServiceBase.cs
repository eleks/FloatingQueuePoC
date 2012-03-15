using System;
using System.Collections.Generic;
using FloatingQueue.Common;
using FloatingQueue.Server.EventsLogic;
using FloatingQueue.Server.Exceptions;
using FloatingQueue.Server.Replication;

namespace FloatingQueue.Server.Services.Implementation
{
    public class QueueServiceBase : IQueueService
    {
        public virtual void Push(string aggregateId, int version, object e)
        {
            Core.Server.Log.Info("Command: push {0} {1} {2}", aggregateId, version, e);

            // note MM: potential bug - if rollback is required, an aggregate who has just been created will not be deleted
            var aggregate = GetEventAggregate(aggregateId);
            try
            {
                if (!HandleSynchronizationPush(aggregate, version, e))
                    return;
                
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
            if (Core.Server.Configuration.IsSyncing)
                throw new BusinessLogicException("Cannot read from node, who's in syncing state");

            var aggregate = GetEventAggregate(aggregateId);
            return aggregate.TryGetNext(version, out next);
        }

        public virtual IEnumerable<object> GetAllNext(string aggregateId, int version)
        {
            if (Core.Server.Configuration.IsSyncing)
                throw new BusinessLogicException("Cannot read from node, who's in syncing state");

            var aggregate = GetEventAggregate(aggregateId);
            return aggregate.GetAllNext(version);
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

        private bool HandleSynchronizationPush(IEventAggregate aggregate, int version, object e)
        {
            if (!Core.Server.Configuration.IsSyncing)
                return true;
            if (version > aggregate.ExpectedLastVersion)
            {
                throw new NotImplementedException("Here a write to temporary storage is required");
            }
            else
            {// dont write at all - this push will be handled by synchronization mechanism
            }
            return false;
        }

    }
}
