using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using FloatingQueueServer.Core;

namespace FloatingQueueServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Initialize();
            RunHost();
        }

        private static void Initialize()
        {
            var componentsManager = new ComponentsManager();
            var container = componentsManager.GetContainer();
            Server.Init(container);
        }

        private static void RunHost()
        {
            var serviceType = typeof (QueueService);
            var serviceUri = new Uri("http://localhost:8080/");

            var host = new ServiceHost(serviceType, serviceUri);

            host.Open();

            Server.Log.Info("Listening:");
            foreach (var uri in host.BaseAddresses)
            {
                Server.Log.Info("\t{0}", uri);
            }

            Server.Log.Info("Press <ENTER> to terminate Host");
            Console.ReadLine();
        }
    }
}
