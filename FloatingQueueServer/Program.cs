using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using FloatingQueueServer.Core;
using NDesk.Options;

namespace FloatingQueueServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Initialize(args);
            RunHost();
        }

        private static void Initialize(string[] args)
        {
            var configuration = ParseConfiguration(args);

            var componentsManager = new ComponentsManager();
            var container = componentsManager.GetContainer(configuration);
            Server.Init(container);
        }

        private static Configuration ParseConfiguration(string[] args)
        {
            var configuration = new Configuration {Port = 80};
            int port;
            var p = new OptionSet()
                    {
                        {"p|port=", v => configuration.Port = int.TryParse(v, out port) ? port : 80},
                        {"m|master", v => configuration.IsMaster = !string.IsNullOrEmpty(v) }
                    };
            p.Parse(args);
            return configuration;
        }

        private static void RunHost()
        {
            var serviceType = typeof (QueueService);
            var serviceUri = new Uri(string.Format("http://localhost:{0}/", Server.Configuration.Port));

            var host = new ServiceHost(serviceType, serviceUri);

            host.Open();

            Server.Log.Info("I am {0}", Server.Configuration.IsMaster ? "master" : "slave");
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
