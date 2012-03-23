using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FloatingQueue.Common;
using FloatingQueue.Common.Proxy;

namespace FloatingQueue.Server.Services.Proxy
{
    public class SafeInternalQueueServiceProxy : SafeServiceProxyBase<InternalQueueServiceProxy, IInternalQueueService>, IInternalQueueServiceProxy
    {
        private const int MaxRetry = 2;

        public SafeInternalQueueServiceProxy(string address)
            : base(new InternalQueueServiceProxy(address), true, false, MaxRetry)
        {
        }

        public void Push(string aggregateId, int version, object e)
        {
            SafeCall(() => Proxy.Push(aggregateId, version, e));
        }

        public bool TryGetNext(string aggregateId, int version, out object next)
        {
            // can't use ref or out inside lambda
            next = null;
            object hack = null;
            bool result = false;

            SafeCall(() =>
            {
                object n;
                result = Proxy.TryGetNext(aggregateId, version, out n);
                hack = n;
            });
            next = hack;
            return result;
        }

        public IEnumerable<object> GetAllNext(string aggregateId, int version)
        {
            IEnumerable<object> result = null;
            SafeCall(() => result = Proxy.GetAllNext(aggregateId, version));
            return result;
        }

        public ClusterMetadata GetClusterMetadata()
        {
            ClusterMetadata result = null;
            SafeCall(() => result = Proxy.GetClusterMetadata());
            return result;
        }

        public void Ping()
        {
            SafeCall(Proxy.Ping);
        }

        public void IntroduceNewNode(ExtendedNodeInfo nodeInfo)
        {
            SafeCall(() => Proxy.IntroduceNewNode(nodeInfo));
        }

        public void RequestSynchronization(ExtendedNodeInfo nodeInfo, Dictionary<string, int> currentAggregateVersions)
        {
            SafeCall(() => Proxy.RequestSynchronization(nodeInfo, currentAggregateVersions));
        }

        public void ReceiveSingleAggregate(string aggregateId, int version, IEnumerable<object> events)
        {
            SafeCall(() => Proxy.ReceiveSingleAggregate(aggregateId, version, events));
        }

        public bool NotificateSynchronizationFinished(Dictionary<string, int> writtenAggregatesVersions)
        {
            bool result = false;
            SafeCall(() => result = Proxy.NotificateSynchronizationFinished(writtenAggregatesVersions));
            return result;
        }

        public List<ExtendedNodeInfo> GetExtendedMetadata()
        {
            List<ExtendedNodeInfo> result = null;
            SafeCall(() => result = Proxy.GetExtendedMetadata());
            return result;
        }

        protected override void ReConnect()
        {
            CommunicationProvider.Instance.SafeNetworkCall(() => Proxy.ReopenClient());
        }

        public void Open()
        {
            CommunicationProvider.Instance.SafeNetworkCall(() => Proxy.Open());
        }

        public void Close()
        {
            CommunicationProvider.Instance.SafeNetworkCall(() => Proxy.Close());
        }
    }
}
