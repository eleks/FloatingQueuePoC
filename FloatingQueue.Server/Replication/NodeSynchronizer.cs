using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FloatingQueue.Server.EventsLogic;
using FloatingQueue.Server.Exceptions;
using FloatingQueue.Server.Services;
using FloatingQueue.Server.Services.Proxy;

namespace FloatingQueue.Server.Replication
{
    public interface INodeSynchronizer
    {
        void Init();
        void StartBackgroundSync(int serverId, IDictionary<string, int> aggregateVersions);
        void EnsureAggregatesAreCompatible(IDictionary<string, int> aggregateVersions);
    }

    public class NodeSynchronizer : INodeSynchronizer
    {
        public void Init()
        {
            if (!Core.Server.Configuration.IsSynced)
            {
                Core.Server.Resolve<IConnectionManager>().OpenOutcomingConnections();

                // tell everyone that i'm new node in cluster,
                foreach (var node in Core.Server.Configuration.Nodes.Siblings)
                {
                    node.Proxy.IntroduceNewNode(ProxyHelper.CurrentNodeInfo);
                    // todo: do something if connection failed?
                }

                // ask master for all the data
                Core.Server.Configuration.Nodes.Master.Proxy
                    .RequestSynchronization(ProxyHelper.CurrentServerId, ProxyHelper.CurrentAggregateVersions);
            }
        }

        private void ValidateSyncRequest(int serverId)
        {
            var requester = Core.Server.Configuration.Nodes.Siblings.
                    Single(n => n.ServerId == serverId);

            if (requester.IsSynced)
                throw new ApplicationException("Only Unsynced Node can request synchronization");

            if (!Core.Server.Configuration.IsSynced)
                throw new ApplicationException("Only Synced Node can reply to synchronization request");
        }

        public void StartBackgroundSync(int serverId, IDictionary<string, int> aggregateVersions)
        {
            ValidateSyncRequest(serverId);

            //todo: pushing each aggregate in separate thread would enhance performance
            //todo: ensure single background task
            Task.Factory.StartNew(() =>
            {
                var aggregateIds = AggregateRepository.Instance.GetAllIds();
                var requester = Core.Server.Configuration.Nodes.Siblings.Single(n => n.ServerId == serverId);
                var versionsSnapshot = AggregateRepository.Instance.GetLastVersions();
                var writtenAggregatesVersions = new Dictionary<string, int>();

                foreach (var aggregateId in aggregateIds)
                {
                    int unsyncedNodeLastVersion, localNodeLastVersion, snapshotVersion;
                    IEventAggregate aggregate;

                    if (!AggregateRepository.Instance.TryGetEventAggregate(aggregateId, out aggregate))
                        throw new CriticalException("If there's an aggregate id, there must be an aggregate");

                    if (!versionsSnapshot.TryGetValue(aggregateId, out snapshotVersion))
                        throw new CriticalException("All aggregates, created after start of sync, must be handled by replication mechanism");

                    if (!aggregateVersions.TryGetValue(aggregateId, out unsyncedNodeLastVersion))
                        unsyncedNodeLastVersion = -1;

                    localNodeLastVersion = aggregate.LastVersion;
                    if (unsyncedNodeLastVersion > localNodeLastVersion)
                        throw new CriticalException("Fatal desynchronization issue came up - unsynced node last version is bigger than local(synced) node last version");

                    var events = aggregate.GetRange(unsyncedNodeLastVersion + 1, snapshotVersion - (unsyncedNodeLastVersion + 1));

                    requester.Proxy.ReceiveAggregateEvents(aggregateId, unsyncedNodeLastVersion, events);
                    writtenAggregatesVersions[aggregateId] = snapshotVersion;
                }

                requester.Proxy.NotificateAllAggregatesSent(writtenAggregatesVersions);
            });
        }

        public void EnsureAggregatesAreCompatible(IDictionary<string, int> incomingVersions)
        {
            var currentVersions = AggregateRepository.Instance.GetLastVersions();

            if (incomingVersions.Count != currentVersions.Count)
                throw new IncompatibleDataException("Aggregate numbers don't match");

            foreach (KeyValuePair<string, int> kvp in currentVersions)
            {
                int incomingValue;
                if (!incomingVersions.TryGetValue(kvp.Key, out incomingValue))
                    throw new IncompatibleDataException("Missing aggregate in incomingAggregates");
                if (kvp.Value != incomingValue)
                    throw new IncompatibleDataException("Versions don't match");
            }

        }
    }
}
