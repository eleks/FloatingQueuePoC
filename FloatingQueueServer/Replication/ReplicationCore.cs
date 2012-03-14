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

            if (!Core.Server.Configuration.IsMaster && lostServerId == MasterId)
            {
                ChooseNextJediMaster();
            }
            Core.Server.Configuration.Nodes.RemoveDeadNode(lostServerId);
        }

        private static int MasterId
        {
            get { return Core.Server.Configuration.Nodes.Master.ServerId; }
        }

        private static void ChooseNextJediMaster()
        {
            var sortedSiblings = Core.Server.Configuration.Nodes.All.ToList();
                sortedSiblings.Sort((a,b) => a.ServerId - b.ServerId);

            var oldMaster = Core.Server.Configuration.Nodes.Master;
            var newMaster = sortedSiblings.First(n => n.ServerId > oldMaster.ServerId);

            newMaster.DeclareAsNewMaster();

            Core.Server.Log.Warn("Declaring {0} as new Jedi Master",
                Core.Server.Configuration.IsMaster
                ? "Myself"
                : string.Format("server no {0} at {1}", newMaster.ServerId, newMaster.Address));
        }
    }
}
