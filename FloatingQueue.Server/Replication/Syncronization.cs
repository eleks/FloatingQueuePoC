using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FloatingQueue.Common;
using FloatingQueue.Common.Proxy;
using FloatingQueue.Server.Core;
using FloatingQueue.Server.EventsLogic;

namespace FloatingQueue.Server.Replication
{
    public static class Syncronization
    {
        public static void Init()
        {
            if (!Core.Server.Configuration.IsSynced)
            {
                // tell everyone that i'm new node in cluster,
                foreach (var node in Core.Server.Configuration.Nodes.SyncedSiblings)
                {
                    var res = node.Proxy.Ping(PingHelper.IntroductionOfNewNodePingParams);
                    // todo: do something if connection failed?
                }

                // ask master for all the data
                Core.Server.Configuration.Nodes.Master.Proxy.Ping(PingHelper.RequestForSyncPingParams);
            }
        }


        public static void ValidateSyncRequest(PingParams pingParams)
        {
            var requester = Core.Server.Configuration.Nodes.Siblings.
                    Single(n => n.ServerId == pingParams.NodeInfo.ServerId);
            
            // todo MM: consider introducing new state - Initial, and let sync only this state
            if (requester.IsSynced)
                throw new ApplicationException("Only unsynced node can request syncronization");

            var aggregateIds = AggregateRepository.Instance.GetAllIds();
            // push to reqeuster

        }


    }
}
