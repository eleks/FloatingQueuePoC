using System;
using System.Linq;
using FloatingQueue.Server.EventsLogic;
using FloatingQueue.Server.Services;
using FloatingQueue.Server.Services.Proxy;

namespace FloatingQueue.Server.Replication
{
    public static class Synchronization
    {
        public static void Init()
        {
            if (!Core.Server.Configuration.IsSynced)
            {
                // tell everyone that i'm new node in cluster,
                foreach (var node in Core.Server.Configuration.Nodes.SyncedSiblings)
                {
                    node.Proxy.IntroduceNewNode(ProxyHelper.CurrentNodeInfo);
                    // todo: do something if connection failed?
                }

                // ask master for all the data
                Core.Server.Configuration.Nodes.Master.Proxy.RequestSynchronization(ProxyHelper.CurrentNodeInfo);
            }
        }


        public static void ValidateSyncRequest(NodeInfo nodeInfo)
        {
            var requester = Core.Server.Configuration.Nodes.Siblings.
                    Single(n => n.ServerId == nodeInfo.ServerId);
            
            if (requester.IsSynced)
                throw new ApplicationException("Only unsynced node can request synchronization");

            var aggregateIds = AggregateRepository.Instance.GetAllIds();
            // push to reqeuster

        }


    }
}
