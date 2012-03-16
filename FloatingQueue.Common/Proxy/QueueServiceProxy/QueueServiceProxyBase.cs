using System.Collections.Generic;

namespace FloatingQueue.Common.Proxy.QueueServiceProxy
{
    public abstract class QueueServiceProxyBase : ProxyBase<IQueueService>, IQueueService
    {
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
