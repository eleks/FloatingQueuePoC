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

        private static void OnConnectionLoss(string lostConnectionAddress)
        {
            if (!Server.Configuration.IsMaster && lostConnectionAddress == MasterAddress)
            {
                ChooseNextJediMaster();
            }
            CleanupBrokenServer(lostConnectionAddress);
        }

        private static void CleanupBrokenServer(string lostConnectionAddress)
        {
            Server.Configuration.Nodes.RemoveAll(n => n.Address == lostConnectionAddress);
        }

        private static string MasterAddress
        {
            get { return Server.Configuration.Nodes.Master.Address; }
        }

        private static void ChooseNextJediMaster()
        {
            Server.Configuration.Nodes.Sort((a,b) => a.ServerId - b.ServerId);

            var oldMaster = Server.Configuration.Nodes.Master;
            var newMaster = Server.Configuration.Nodes.First(n => n.ServerId > oldMaster.ServerId);

            oldMaster.DeclareAsDeadMaster();
            newMaster.DeclareAsNewMaster();

            Server.Log.Warn("Declaring {0} as new Jedi Master",
                Server.Configuration.IsMaster
                ? "Myself"
                : string.Format("server no {0} at {1}", newMaster.ServerId, newMaster.Address));
        }
    }
}
