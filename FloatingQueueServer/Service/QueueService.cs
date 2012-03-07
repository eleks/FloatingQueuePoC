using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using FloatingQueue.Server.Core;
using FloatingQueue.Server.EventsLogic;

namespace FloatingQueue.Server.Service
{
    public class QueueService : IQueueService
    {
        public void Push(string aggregateId, int version, object e)
        {
            Core.Server.Log.Info("Command: push {0} {1} {2}", aggregateId, version, e);
            // todo: ensure that slaves do not allow writes directly from client
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
                aggregate.Commit();
            }
            catch
            {
                aggregate.Rollback();
                throw;
            }
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

        public PingResult Ping()
        {
            return new PingResult();
        }

    }


}
