using System;
using System.Collections.Generic;
using System.Linq;
using FloatingQueue.Server.Core;
using FloatingQueue.Server.EventsLogic;
using FloatingQueue.Server.Exceptions;
using FloatingQueue.Server.Replication;
using FloatingQueue.Server.Services.Proxy;

namespace FloatingQueue.Server.Services.Implementation
{
    public class InternalQueueService : QueueServiceBase, IInternalQueueService
    {
        public int Ping()
        {
            return 0;
        }

        public void IntroduceNewNode(NodeInfo nodeInfo)
        {
            Core.Server.Log.Info("New node introduced in system(still not synchronized). Id = {0}, address = {1}",
                nodeInfo.ServerId, nodeInfo.Address);

            var newNode = new NodeConfiguration
            {
                InternalAddress = nodeInfo.Address,
                ServerId = nodeInfo.ServerId,
                IsSynced = false,
                IsMaster = false,
                IsReadonly = false
            };

            Core.Server.Configuration.Nodes.AddNewNode(newNode);
            newNode.CreateProxy();
            newNode.Proxy.Open();
        }

        public void RequestSynchronization(int serverId, IDictionary<string, int> aggregateVersions)
        {
            Core.Server.Log.Info("Request for synchronization arrived from server no {0}", serverId);
            Core.Server.Resolve<INodeSynchronizer>().StartBackgroundSync(serverId, aggregateVersions);
        }

        public void ReceiveAggregateEvents(string aggregateId, int version, IEnumerable<object> events)
        {
            Core.Server.Log.Info("Receiving aggregate events. AggregateId = {0}, version = {1}",
                aggregateId, version);

            if (Core.Server.Configuration.IsSynced)
                throw new BusinessLogicException("Only Unsynced Node can receive aggregate events in 1 batch");

            var aggregate = GetEventAggregate(aggregateId);
            aggregate.PushMany(version, events);
        }

        public void NotificateAllAggregatesSent(IDictionary<string, int> writtenAggregatesVersions)
        {
            Core.Server.Log.Info("All aggregates received. Merging sync writes and switching to Synced mode");

            //todo MM: identify server, which we requested for sync and check if this notification came from him

            Core.Server.Resolve<INodeSynchronizer>().EnsureAggregatesAreCompatible(writtenAggregatesVersions);

            // TODO MM CRITICAL: merge copied aggregates with writed to temporary collection

            Core.Server.Configuration.IsSyncing = false;

            foreach (var node in Core.Server.Configuration.Nodes.Siblings)
            {
                node.Proxy.NotificateNodeIsSynchronized(ProxyHelper.CurrentServerId);
            }

            Core.Server.Configuration.Nodes.Self.DeclareAsSyncedNode();

            Core.Server.Log.Info("All nodes are notified about finish of My synchronization process");
        }

        public void NotificateNodeIsSynchronized(int serverId)
        {
            Core.Server.Log.Info("Node no {0} has synchronized with other nodes", serverId);

            Core.Server.Configuration.Nodes.Siblings
                       .Single(n => n.ServerId == serverId)
                       .DeclareAsSyncedNode();
        }
    }
}
