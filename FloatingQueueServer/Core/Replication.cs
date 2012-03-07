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
        }

        private static string MasterAddress
        {
            get { return Server.Configuration.Nodes.Master.Address; }
        }

        private static void ChooseNextJediMaster()
        {
            var oldMaster = Server.Configuration.Nodes.Master;
            var newMaster = Server.Configuration.Nodes.First(n => n.ServerId > oldMaster.ServerId); // todo: sort first

            oldMaster.DeclareAsDeadMaster();
            newMaster.DeclareAsNewMaster();

            Server.Log.Info("Declaring server no {0} at {1} as new Jedi Master", newMaster.ServerId, newMaster.Address);
        }
    }
}
