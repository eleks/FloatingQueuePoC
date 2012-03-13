using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FloatingQueue.Server.Core
{
    public class Replication
    {
        public static void  Init()
        {
            //Core.Server.Resolve<IConnectionManager>().OpenOutcomingConnections();
            Core.Server.Resolve<IConnectionManager>().OnConnectionLoss += OnConnectionLoss;
        }

        private static void OnConnectionLoss(int lostServerId)
        {
            if (Server.Configuration.ServerId == lostServerId)
            {
                throw new ApplicationException("Server can't loose connection with himself");
            }

            if (!Server.Configuration.IsMaster && lostServerId == MasterId)
            {
                ChooseNextJediMaster();
            }
            Server.Configuration.Nodes.RemoveDeadNode(lostServerId);
        }

        private static int MasterId
        {
            get { return Server.Configuration.Nodes.Master.ServerId; }
        }

        private static void ChooseNextJediMaster()
        {
            var sortedSiblings = Server.Configuration.Nodes.All.ToList();
                sortedSiblings.Sort((a,b) => a.ServerId - b.ServerId);

            var oldMaster = Server.Configuration.Nodes.Master;
            var newMaster = sortedSiblings.First(n => n.ServerId > oldMaster.ServerId);

            newMaster.DeclareAsNewMaster();

            Server.Log.Warn("Declaring {0} as new Jedi Master",
                Server.Configuration.IsMaster
                ? "Myself"
                : string.Format("server no {0} at {1}", newMaster.ServerId, newMaster.Address));
        }
    }
}
