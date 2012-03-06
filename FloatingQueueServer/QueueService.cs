using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using FloatingQueueServer.Core;

namespace FloatingQueueServer
{
    [ServiceContract]
    public interface IQueueService
    {
        [OperationContract]
        void Push(string aggregateId, int version, object e);
        [OperationContract]
        bool TryGetNext(string aggregateId, int version, out object next);
        [OperationContract]
        IEnumerable<object> GetAllNext(string aggregateId, int version);
    }

    public class QueueService : IQueueService
    {
        public void Push(string aggregateId, int version, object e)
        {
            Server.Log.Info("Command: push {0} {1} {2}", aggregateId, version, e);
            var aggregate = GetEventAggregate(aggregateId);
            aggregate.Push(version, e);
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
