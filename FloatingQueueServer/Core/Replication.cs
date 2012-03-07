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
            Core.Server.Resolve<IConnectionManager>().OnConnectionLoss += OnConnectionLoss;
        }

        private static void OnConnectionLoss(string lostConnectionAddress)
        {
            if (!Server.Configuration.IsMaster && lostConnectionAddress == MasterAddress)
            {
                ChooseNextJediMaster();
            }
        }

        private static string MasterAddress
        {
            get { return Server.Configuration.Nodes.Where(n => n.IsMaster).Single().Address; }
        }

        private static void ChooseNextJediMaster()
        {
            //todo: expand this logic in case when slave, who's become a master can also become dead
            if (Server.Configuration.Priority == 1)
            {
                Server.Configuration.DeclareServerAsNewMaster();
                Server.Log.Info("Declaring server as new Jedi Master");
            }
            if (Server.Configuration.Priority > 1)
            {
                var oldMaster = Server.Configuration.Nodes.Where(n => n.IsMaster).Single();
                var newMaster = Server.Configuration.Nodes.Where(n => n.Priority == 1).Single();
                oldMaster.DeclareAsDeadMaster();
                newMaster.DeclareAsNewMaster();
                Server.Log.Info("Declaring server no {0} at {1} as new Jedi Master", newMaster.Priority, newMaster.Address);
            }
        }
    }
}
