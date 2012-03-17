using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using FloatingQueue.Common.Proxy;
using FloatingQueue.Server.Exceptions;

namespace FloatingQueue.Server.Services.Proxy
{
    public class InternalQueueServiceProxy : ProxyBase<IInternalQueueService>, IInternalQueueServiceProxy
    {
        public InternalQueueServiceProxy(string address)
        {
            EndpointAddress = new EndpointAddress(address);
        }

        #region Standard Queue Service Functionality

        //note MM: multiple inheritance would be useful here

        public void Push(string aggregateId, int version, object e)
        {
            Client.Push(aggregateId, version, e);
        }

        public bool TryGetNext(string aggregateId, int version, out object next)
        {
            return Client.TryGetNext(aggregateId, version, out next);
        }

        public IEnumerable<object> GetAllNext(string aggregateId, int version)
        {
            return Client.GetAllNext(aggregateId, version);
        }

        public ClusterMetadata GetClusterMetadata()
        {
            return Client.GetClusterMetadata();
        }


        #endregion

        #region Additional Functionality

        public int Ping()
        {
            // todo create enumeration for fault reasons
            try
            {
                return Client.Ping();
            }
            catch (CommunicationException)
            {
                return 1;
            }
            catch (IOException)
            {
                return 1;
            }
            catch (TimeoutException)
            {
                return 2;
            }
        }

        public void IntroduceNewNode(NodeInfo nodeInfo)
        {
            if (Core.Server.Configuration.IsSynced)
                throw new BusinessLogicException("Only Unsynced Node can introduce herself to others as a new node");

            Client.IntroduceNewNode(nodeInfo);
        }

        public void RequestSynchronization(int serverId, IDictionary<string, int> aggregateVersions)
        {
            if (Core.Server.Configuration.IsSynced)
                throw new BusinessLogicException("Request for synchronization can be initiated only by Unsynced Node");

            Core.Server.Configuration.IsSyncing = true;
            Client.RequestSynchronization(serverId, aggregateVersions);
        }

        public void NotificateNodeIsSynchronized(int serverId)
        {
            if (Core.Server.Configuration.IsSyncing)
                throw new BusinessLogicException("If node is still syncing she can't notify otehr nodes that she's already synced");

            if (Core.Server.Configuration.IsSynced)
                throw new BusinessLogicException("Only Unsynced Node can notificate siblings that she's synchronized");

            Client.NotificateNodeIsSynchronized(serverId);
        }

        public void ReceiveAggregateEvents(string aggregateId, int version, IEnumerable<object> events)
        {
            if (!Core.Server.Configuration.IsSynced)
                throw new BusinessLogicException("Only Synced Node can push all aggregate events");

            Client.ReceiveAggregateEvents(aggregateId, version, events);
        }

        public void NotificateAllAggregatesSent(IDictionary<string, int> writtenAggregatesVersions)
        {
            if (!Core.Server.Configuration.IsSynced)
                throw new BusinessLogicException("Only Synced Node can notificate about aggregates sending completeness");

            Client.NotificateAllAggregatesSent(writtenAggregatesVersions);
        }


        #endregion

        public void Open()
        {
            DoOpen();
        }

        public void Close()
        {
            DoClose();
        }
    }
}
