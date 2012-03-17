using System.Collections.Generic;
using System.ServiceModel;

namespace FloatingQueue.Common.Proxy.QueueServiceProxy
{
    public class QueueServiceProxyBase : ProxyBase<IQueueService>, IQueueService
    {
        public QueueServiceProxyBase(string address)
        {
            EndpointAddress = new EndpointAddress(address);
        }

        public virtual void Push(string aggregateId, int version, object e)
        {
            Client.Push(aggregateId, version, e);
        }

        public virtual bool TryGetNext(string aggregateId, int version, out object next)
        {
            return Client.TryGetNext(aggregateId, version, out next);
        }

        public virtual IEnumerable<object> GetAllNext(string aggregateId, int version)
        {
            return Client.GetAllNext(aggregateId, version);
        }

        public virtual ClusterMetadata GetClusterMetadata()
        {
            return Client.GetClusterMetadata();
        }
    }
}
