using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FloatingQueue.Server.EventsLogic;
using FloatingQueue.Server.Exceptions;
using FloatingQueue.Server.Services;
using FloatingQueue.Server.Services.Implementation;
using FloatingQueue.Server.Services.Proxy;

namespace FloatingQueue.Server.Replication
{
    public interface INodeSynchronizer
    {
        void StartBackgroundSync(ExtendedNodeInfo nodeInfo, Dictionary<string, int> aggregateVersions);
    }

    public class NodeSynchronizer : INodeSynchronizer
    {
        public void StartBackgroundSync(ExtendedNodeInfo nodeInfo, Dictionary<string, int> aggregateVersions)
        {
            //todo: pushing each aggregate in separate thread would enhance performance
            //todo: ensure single background task
            //note: if update speed is to big, an epsilon can be taken
            Task.Factory.StartNew(() =>
            {
                Dictionary<string, int> writtenVersions = aggregateVersions,
                                        currentVersions = AggregateRepository.Instance.GetLastVersions();

                var requester = new InternalQueueServiceProxy(nodeInfo.InternalAddress);
                try
                {
                    requester.Open();
                    while (!writtenVersions.AreEqualToVersions(currentVersions))
                    {
                        writtenVersions = DoSync(requester, aggregateVersions);
                        currentVersions = AggregateRepository.Instance.GetLastVersions();
                    }
                }
                finally
                {
                    requester.Close();
                }

                OnSynchronizationFinished(nodeInfo, writtenVersions);
            });
        }

        private void OnSynchronizationFinished(ExtendedNodeInfo nodeInfo, Dictionary<string, int> writtenVersions)
        {
            // todo: wrap this into transaction

            Core.Server.Log.Info("Sync with {0} almost finished. Switching to readonly mode", nodeInfo.InternalAddress);
            Core.Server.Configuration.EnterReadonlyMode();

            var syncedNode = ProxyHelper.TranslateNodeInfo(nodeInfo);
                syncedNode.CreateProxy();

            // handle last updates
            var currentVersions = AggregateRepository.Instance.GetLastVersions();
            if (!writtenVersions.AreEqualToVersions(currentVersions))
                writtenVersions = DoSync(syncedNode.Proxy, currentVersions);

            // introduce newbie to everyone, including myself
            foreach (var node in Core.Server.Configuration.Nodes.Siblings)
                node.Proxy.IntroduceNewNode(nodeInfo);
            Core.Server.Configuration.Nodes.AddNewNode(syncedNode);
            syncedNode.Proxy.Open();

            ProxyHelper.EnsureNodesConfigurationIsValid();

            // notify requester 
            if (!syncedNode.Proxy.NotificateSynchronizationFinished(writtenVersions))
                throw new ApplicationException("Synced node version didn't match current version, after all the sync process");

            Core.Server.Configuration.ExitReadonlyMode();
            Core.Server.Log.Info("Sync with {0} has finished successfully. Exiting readonly mode", nodeInfo.InternalAddress);
        }

        private Dictionary<string, int> DoSync(IInternalQueueServiceProxy proxy, IDictionary<string, int> unsyncedNodeVersions)
        {
            var aggregateIds = AggregateRepository.Instance.GetAllIds();
            var writtenAggregatesVersions = new Dictionary<string, int>();

            foreach (var aggregateId in aggregateIds)
            {
                int unsyncedNodeLastVersion, localNodeLastVersion;
                IEventAggregate aggregate;

                if (!AggregateRepository.Instance.TryGetEventAggregate(aggregateId, out aggregate))
                    throw new CriticalException("If there's an aggregate id, there must be an aggregate");

                if (!unsyncedNodeVersions.TryGetValue(aggregateId, out unsyncedNodeLastVersion))
                    unsyncedNodeLastVersion = -1;

                localNodeLastVersion = aggregate.LastVersion;
                if (unsyncedNodeLastVersion > localNodeLastVersion)
                    throw new CriticalException("Fatal desynchronization issue came up - unsynced node last version is bigger than local(synced) node last version");

                var events = aggregate.GetAllNext(unsyncedNodeLastVersion + 1);
                proxy.ReceiveSingleAggregate(aggregateId, unsyncedNodeLastVersion, events);

                writtenAggregatesVersions[aggregateId] = localNodeLastVersion;
            }
            return writtenAggregatesVersions;
        }
    }

    public static class SyncHelper
    {
        public static bool AreEqualToVersions(this IDictionary<string, int> currentVersions, IDictionary<string, int> incomingVersions)
        {
            if (incomingVersions.Count != currentVersions.Count)
                return false; // throw new IncompatibleDataException("Aggregate numbers don't match");

            foreach (KeyValuePair<string, int> kvp in currentVersions)
            {
                int incomingValue;
                if (!incomingVersions.TryGetValue(kvp.Key, out incomingValue))
                    return false; // throw new IncompatibleDataException("Missing aggregate in incomingAggregates");
                if (kvp.Value != incomingValue)
                    return false; // throw new IncompatibleDataException("Versions don't match");
            }

            return true;
        }
    }

}
