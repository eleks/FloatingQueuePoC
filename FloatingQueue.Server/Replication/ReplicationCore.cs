using System;
using System.Linq;

namespace FloatingQueue.Server.Replication
{
    public class ReplicationCore
    {
        public static void  Init()
        {
            //Core.Server.Resolve<IConnectionManager>().OpenOutcomingConnections();
            Core.Server.Resolve<IConnectionManager>().OnConnectionLoss += OnConnectionLoss;
        }

        private static void OnConnectionLoss(int lostServerId)
        {
            if (Core.Server.Configuration.ServerId == lostServerId)
            {
                throw new ApplicationException("Server can't loose connection with himself");
            }

            // first get master id, as he can be deleted
            int masterId = Core.Server.Configuration.Nodes.Master.ServerId;

            Core.Server.Configuration.Nodes.RemoveDeadNode(lostServerId);

            if (!Core.Server.Configuration.IsMaster && lostServerId == masterId)
            {
                ChooseNextJediMaster(masterId);
            }
        }

        private static void ChooseNextJediMaster(int oldMasterId)
        {
            var sortedSiblings = Core.Server.Configuration.Nodes.All.ToList();
                sortedSiblings.Sort((a,b) => a.ServerId - b.ServerId);

            var newMaster = sortedSiblings.First(n => n.ServerId > oldMasterId);
            newMaster.DeclareAsNewMaster();

            Core.Server.Log.Warn("Declaring {0} as new Jedi Master",
                Core.Server.Configuration.IsMaster
                ? "Myself"
                : string.Format("server no {0} at {1}", newMaster.ServerId, newMaster.Address));
        }
    }
}
