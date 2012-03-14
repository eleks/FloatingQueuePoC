using System.Linq;
using FloatingQueue.Server.Core;
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
            Core.Server.Configuration.Nodes.AddNewNode(new NodeConfiguration
            {
                Address = nodeInfo.Address,
                Proxy = new InternalQueueServiceProxy(nodeInfo.Address),
                ServerId = nodeInfo.ServerId,
                IsSynced = false,
                IsMaster = false,
                IsReadonly = false
            });
        }

        public void RequestSynchronization(NodeInfo nodeInfo)
        {
            // validate request
            // create task to share all data with this node
        }

        public void NotificateSlaveSynchronized(NodeInfo nodeInfo)
        {
            Core.Server.Configuration.Nodes.Siblings
                        .Single(n => n.ServerId == nodeInfo.ServerId)
                        .DeclareAsSyncedNode();
        }

        
    }
}
