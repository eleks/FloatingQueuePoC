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
        private static readonly object m_SyncRoot = new object();

        public void StartBackgroundSync(ExtendedNodeInfo nodeInfo, Dictionary<string, int> aggregateVersions)
        {
            //todo MM: ensure single background task
            Task.Factory.StartNew(() =>
            {
                Dictionary<string, int> writtenVersions = aggregateVersions,
                                        currentVersions = AggregateRepository.Instance.GetLastVersions();

                using(var requester = new SafeInternalQueueServiceProxy(nodeInfo.InternalAddress))
                {
                    requester.Open();
                    while (!writtenVersions.AreEqualToVersions(currentVersions))
                    {
                        writtenVersions = DoSync(requester, aggregateVersions);
                        currentVersions = AggregateRepository.Instance.GetLastVersions();
                    }
                }

                OnSynchronizationFinished(nodeInfo, writtenVersions);
            });
        }

        private void OnSynchronizationFinished(ExtendedNodeInfo nodeInfo, Dictionary<string, int> writtenVersions)
        {
            // todo: wrap this into transaction

            lock (m_SyncRoot)
            {
                try
                {
                    Core.Server.Log.Debug("Sync with {0} almost finished. Switching to readonly mode", nodeInfo.InternalAddress);
                    Core.Server.Configuration.EnterReadonlyMode();
                    try
                    {
                        var syncedNode = ProxyHelper.TranslateNodeInfo(nodeInfo);
                        syncedNode.CreateProxy();
                        syncedNode.Proxy.Open();

                        Core.Server.Log.Debug("Handling last updates...");
                        var currentVersions = AggregateRepository.Instance.GetLastVersions();
                        if (!writtenVersions.AreEqualToVersions(currentVersions))
                            writtenVersions = DoSync(syncedNode.Proxy, currentVersions);

                        Core.Server.Log.Debug("Introducing {0} to everyone, including myself...",
                                              nodeInfo.InternalAddress);
                        foreach (var node in Core.Server.Configuration.Nodes.Siblings)
                            node.Proxy.IntroduceNewNode(nodeInfo);
                        Core.Server.Configuration.Nodes.AddNewNode(syncedNode);

                        Core.Server.Log.Debug("Checking current configuration...");
                        ProxyHelper.EnsureNodesConfigurationIsValid();

                        Core.Server.Log.Debug("Notifying {0} about finish of sync", nodeInfo.InternalAddress);
                        if (!syncedNode.Proxy.NotificateSynchronizationFinished(writtenVersions))
                            throw new ApplicationException(
                                "Synced node version didn't match current version, after all the sync process");

                        Core.Server.Log.Debug("Sync with {0} has finished successfully. Exiting readonly mode", nodeInfo.InternalAddress);
                    }
                    finally
                    {
                        Core.Server.Configuration.ExitReadonlyMode();
                    }
                }
                catch (Exception ex)
                {
                    Core.Server.Log.Error("Error while finishing sync: {0}{1}{0}{2}", Environment.NewLine, ex.Message, ex.StackTrace);
                }
            }

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

                var events = aggregate.GetAllNext(unsyncedNodeLastVersion);
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
