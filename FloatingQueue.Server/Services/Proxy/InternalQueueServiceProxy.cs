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
        public InternalQueueServiceProxy(string address) : base(address)
        {
        }

        #region Standard Queue Service Functionality

        public void Push(string aggregateId, int version, object e)
        {
            if (!Core.Server.Configuration.IsMaster)
                throw new BusinessLogicException("Only Master can initiate push to other nodes");

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

        public void IntroduceNewNode(ExtendedNodeInfo nodeInfo)
        {
            if (!Core.Server.Configuration.IsMaster)
                throw new BusinessLogicException("Only Master can introduce new node to others");

            Client.IntroduceNewNode(nodeInfo);
        }

        public void RequestSynchronization(ExtendedNodeInfo nodeInfo, Dictionary<string, int> aggregateVersions)
        {
            if (Core.Server.Configuration.IsSynced)
                throw new BusinessLogicException("Request for synchronization can be initiated only by Unsynced Node");

            Core.Server.Log.Info("Requesting synchronization from Master at {0}",this.EndpointAddress.Uri);

            Client.RequestSynchronization(nodeInfo, aggregateVersions);
        }

        public void ReceiveSingleAggregate(string aggregateId, int version, IEnumerable<object> events)
        {
            if (!Core.Server.Configuration.IsMaster)
                throw new BusinessLogicException("Only Master can push all aggregate events");

            Client.ReceiveSingleAggregate(aggregateId, version, events);
        }

        public bool NotificateSynchronizationFinished(Dictionary<string, int> writtenAggregatesVersions)
        {
            if (!Core.Server.Configuration.IsMaster)
                throw new BusinessLogicException("Only Master can notificate about aggregates sending completion");

            return Client.NotificateSynchronizationFinished(writtenAggregatesVersions);
        }

        public List<ExtendedNodeInfo> GetExtendedMetadata()
        {
            return Client.GetExtendedMetadata();
        }

        #endregion

        public void Open()
        {
            OpenClient();
        }

        public void Close()
        {
            CloseClient();
        }
    }
}
