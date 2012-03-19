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

        public void IntroduceNewNode(ExtendedNodeInfo nodeInfo)
        {
            if (nodeInfo.IsMaster)
                throw new InvalidOperationException("New node cannot be introduced as Master");

            Core.Server.Log.Info("New node introduced in system. Id = {0}, internal address = {1}",
                nodeInfo.ServerId, nodeInfo.InternalAddress);

            var newNode = ProxyHelper.TranslateNodeInfo(nodeInfo);
            Core.Server.Configuration.Nodes.AddNewNode(newNode);

            ProxyHelper.EnsureNodesConfigurationIsValid();

            newNode.CreateProxy();
            newNode.Proxy.Open();
        }

        public void RequestSynchronization(ExtendedNodeInfo nodeInfo, Dictionary<string, int> aggregateVersions)
        {
            Core.Server.Log.Info("Request for synchronization arrived from {0}", nodeInfo.InternalAddress);
            
            var sync = Core.Server.Resolve<INodeSynchronizer>();
            sync.StartBackgroundSync(nodeInfo, aggregateVersions);
        }

        public void ReceiveSingleAggregate(string aggregateId, int version, IEnumerable<object> events)
        {
            Core.Server.Log.Info("Receiving aggregate events. AggregateId = {0}, version = {1}",
                aggregateId, version);

            if (Core.Server.Configuration.IsSynced)
                throw new BusinessLogicException("Only Unsynced Node can receive aggregate events in 1 batch");

            var aggregate = GetEventAggregate(aggregateId);
            aggregate.PushMany(version, events);
        }

        public bool NotificateSynchronizationFinished(Dictionary<string, int> writtenAggregatesVersions)
        {
            if (Core.Server.Configuration.IsSynced)
                throw new InvalidOperationException("Notification about finish of synchronization shouldn't come to synchronized node.");

            //todo MM: identify server, which we requested for sync and check if this notification came from him
            Core.Server.Log.Info("Synchronization has finished. Exiting readonly mode");

            var currentVersions = AggregateRepository.Instance.GetLastVersions();
            if (!currentVersions.AreEqualToVersions(writtenAggregatesVersions))
            {
                Core.Server.Log.Warn("Incoming versions don't match current versions.");
                return false;
            }

            Core.Server.Configuration.DeclareAsSyncedNode();
            Core.Server.Configuration.ExitReadonlyMode();
            Core.Server.Resolve<INodeInitializer>().CollectClusterMetadata(
                Core.Server.Configuration.Nodes.Siblings.Select(n => n.InternalAddress));
            Core.Server.Resolve<IConnectionManager>().OpenOutcomingConnections();

            return true;
        }

        public List<ExtendedNodeInfo> GetExtendedMetadata()
        {
            var metadata = Core.Server.Configuration.Nodes.All.Select(n => new ExtendedNodeInfo
            {
                InternalAddress = n.InternalAddress,
                PublicAddress = n.PublicAddress,
                ServerId = n.ServerId,
                IsMaster = n.IsMaster
            }).ToList();
            return metadata;
        }
    }
}
